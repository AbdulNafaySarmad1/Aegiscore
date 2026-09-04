using AegisCore.API.Logging;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Prometheus;
using Serilog;
using AegisCore.API.Background;
using AegisCore.API.Extensions;
using AegisCore.API.Hubs;
using AegisCore.API.Finance;
using AegisCore.API.Middleware;
using Casbin;
using Casbin.Persist;
using AegisCore.Application;
using AegisCore.Application.Maintenance;
using AegisCore.Application.ResearchLabs;
using AegisCore.Application.Security;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Repositories;
using AegisCore.Application.Common.Interfaces;
using AegisCore.Infrastructure.AI;
using AegisCore.Infrastructure.ApiGateway;
using AegisCore.Infrastructure.Backup;
using AegisCore.Infrastructure.Campus;
using AegisCore.Infrastructure.Cloud;
using AegisCore.Infrastructure.Config;
using AegisCore.Infrastructure.ConfigCenter;
using AegisCore.Infrastructure.Configuration;
using AegisCore.Infrastructure.CoreERP;
using AegisCore.Infrastructure.DegreeAudit;
using AegisCore.Infrastructure.Deployment;
using AegisCore.Infrastructure.DigitalTwin;
using AegisCore.Infrastructure.Forms;
using AegisCore.Infrastructure.I18n;
using AegisCore.Infrastructure.Identity;
using AegisCore.Infrastructure.Installation;
using AegisCore.Infrastructure.Marketplace;
using AegisCore.Infrastructure.Onboarding;
using AegisCore.Infrastructure.Platform;
using AegisCore.Infrastructure.RuleEngine;
using AegisCore.Infrastructure.Search;
using AegisCore.Infrastructure.Security;
using AegisCore.Infrastructure.Security.Abac;
using AegisCore.Infrastructure.Security.Pqc;
using AegisCore.Infrastructure.Payments.Raast;
using AegisCore.Infrastructure.Cloud.R2;
using AegisCore.Infrastructure.Workflow;
using AegisCore.Infrastructure.Workflow.StateMachine;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentSubstitution();

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxConcurrentConnections = 1000;
    options.Limits.MaxConcurrentUpgradedConnections = 100;
    options.Limits.MaxRequestBodySize = 52_428_800;
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(15);
    options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(120);
    options.Limits.MinRequestBodyDataRate = new MinDataRate(bytesPerSecond: 100, gracePeriod: TimeSpan.FromSeconds(10));
    options.Limits.MinResponseDataRate = new MinDataRate(bytesPerSecond: 100, gracePeriod: TimeSpan.FromSeconds(10));
    options.AddServerHeader = false;
});

var demoMode = builder.Configuration.GetValue<bool>("Runtime:DemoMode");
if (demoMode && builder.Environment.IsProduction())
{
    throw new InvalidOperationException(
        "AegisCore cannot start: Runtime:DemoMode is enabled in a Production environment. "
        + "Set AEGISCORE_DEMO_MODE=false for production use.");
}

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Destructure.With<SensitiveDataDestructuringPolicy>()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
    .Enrich.WithProperty("Version", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0")
    .WriteTo.Console(builder.Environment.IsProduction()
        ? new Serilog.Formatting.Json.JsonFormatter()
        : new Serilog.Formatting.Display.MessageTemplateTextFormatter("{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}", null))
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddInstallationState();

var isInstalled = InstallationStateService.HasPersistentMarker(builder.Configuration, builder.Environment.ContentRootPath);

if (!isInstalled)
{
    var setupToken = SetupBootstrapToken.RequireValid(builder.Configuration);
    var setupUrl = (builder.Configuration["APP_URL"] ?? "http://localhost:3000").TrimEnd('/');
    Log.Information("AegisCore starting in installation mode - no external dependencies required");
    Log.Warning("[AEGISCORE] Setup token: {SetupToken}", setupToken);
    Log.Information("[AEGISCORE] Navigate to {SetupUrl}/setup to begin installation.", setupUrl);

    builder.Services.AddCoreServices(builder.Configuration);
    builder.Services.AddSetupServices(builder.Configuration);
}

else
{
    Log.Information("AegisCore starting in production mode");

    builder.Services.AddCoreServices(builder.Configuration);
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddProductionServices(builder.Configuration);
    builder.Services.AddAuthenticationServices(builder.Configuration);
    builder.Services.AddBackgroundWorkers(builder.Configuration);
}

var app = builder.Build();

app.Use(async (context, next) =>
{
    if (string.Equals(context.Request.Path.Value, "/health", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.ContentType = "text/plain";
        await context.Response.WriteAsync("Healthy");
        return;
    }

    await next();
});

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex) when (context.Request.Path.StartsWithSegments("/swagger"))
    {
        app.Logger.LogError(ex, "Swagger generation failed for {Path}", context.Request.Path);
        throw;
    }
});
app.UseSwagger();

app.MapGet("/health/installation", (IInstallationStateService installation) =>
    Results.Ok(new { isInstalled = installation.IsInstalled() }))
    .AllowAnonymous();

if (!isInstalled)
{
    app.UseMiddleware<GlobalExceptionMiddleware>();
    app.UseSerilogRequestLogging();
    app.UseRouting();
    app.UseCors("CloudflarePages");
    app.UseSetupRequired();
    app.UseMiddleware<SetupRateLimitMiddleware>();
    app.UseMiddleware<SetupBootstrapTokenMiddleware>();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    if (app.Configuration.GetValue<bool>("Database:AutoMigrate"))
        await ApplyDatabaseMigrationsAsync(app);
    app.MapGet("/health", () => Results.Text("Healthy")).AllowAnonymous();
    app.MapGet("/health/live", () => Results.Text("Healthy")).AllowAnonymous();
    app.MapGet("/health/ready", () => Results.Ok(new
    {
        status = "setup_required",
        message = "AegisCore is healthy and waiting for the guided installation to finish."
    })).AllowAnonymous();

    app.Run();
}
else
{
    if (app.Environment.IsProduction())
    {
        app.UseHsts();
        app.UseHttpsRedirection();
    }

    app.UseMiddleware<GlobalExceptionMiddleware>();
    app.UseSerilogRequestLogging();
    app.UseMiddleware<RequestLoggingMiddleware>();
    app.UseMiddleware<RateLimitingMiddleware>();
    app.UseMiddleware<RateLimitAuthMiddleware>();
    app.UseMiddleware<AuthenticatedOriginPullsMiddleware>();
    app.UseMiddleware<DemoExpiryMiddleware>();
    app.UseMiddleware<HybridPqcTransportMiddleware>();
    app.UseMiddleware<SessionBindingMiddleware>();

    app.UseCors("CloudflarePages");
    app.UseAuthentication();
    app.UseMiddleware<DemoIdentityMiddleware>();
    app.UseMiddleware<GlobalRateLimitMiddleware>();
    app.UseMiddleware<LicenseLockoutMiddleware>();
    app.UseMiddleware<TenantContextMiddleware>();
    app.UseAuthorization();

    app.UseMiddleware<TurnstileMiddleware>();
    app.UseMiddleware<AbacMiddleware>();
    app.UseMiddleware<CasbinAuthorizationMiddleware>();

    app.MapGet("/health", () => Results.Text("Healthy")).AllowAnonymous();
    app.MapGet("/health/live", () => Results.Text("Healthy")).AllowAnonymous();
    app.MapGet("/health/ready", async (DeploymentReadinessChecker checker, CancellationToken ct) =>
    {
        var checks = await checker.RunAllAsync(ct);
        if (checker.AllPassed)
        {
            return Results.Ok(new { status = "ready", checks });
        }

        return Results.Json(
            new { status = "not_ready", checks },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }).AllowAnonymous();
    app.MapHealthChecks("/health/detail").RequireAuthorization();

    app.MapGet("/health/deep", async (AegisDbContext db, IConnectionMultiplexer redis, IConfiguration config, CancellationToken ct) =>
    {
        var report = new System.Dynamic.ExpandoObject() as IDictionary<string, object?>;
        report["timestamp"] = DateTime.UtcNow.ToString("O");
        report["environment"] = app.Environment.EnvironmentName;
        report["version"] = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0";

        try { await db.Database.ExecuteSqlRawAsync("SELECT 1", ct); report["postgres"] = "healthy"; }
        catch (Exception ex) { report["postgres"] = $"unhealthy: {ex.Message}"; }

        try { await redis.GetDatabase().PingAsync(); report["redis"] = "healthy"; }
        catch (Exception ex) { report["redis"] = $"unhealthy: {ex.Message}"; }

        try
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            report["memory_mb"] = process.WorkingSet64 / 1_048_576;
            report["thread_count"] = process.Threads.Count;
        }
        catch (Exception ex) { report["resources"] = $"error: {ex.Message}"; }

        try
        {
            var drives = System.IO.DriveInfo.GetDrives();
            report["disk"] = drives.Where(d => d.IsReady).Select(d => new
            {
                name = d.Name,
                total_gb = Math.Round(d.TotalSize / 1_073_741_824.0, 1),
                free_gb = Math.Round(d.AvailableFreeSpace / 1_073_741_824.0, 1),
                free_pct = Math.Round((double)d.AvailableFreeSpace / d.TotalSize * 100, 1)
            });
        }
        catch (Exception ex) { report["disk"] = $"error: {ex.Message}"; }

        try
        {
            var health = config.GetValue<bool>("Runtime:DemoMode");
            var licenseOk = true;
            report["demo_mode"] = health;
            report["license"] = licenseOk ? "valid" : "expired";
        }
        catch { report["license"] = "unknown"; }

        return Results.Ok(report);
    }).RequireAuthorization();

    app.UseHttpMetrics(options =>
    {
        options.AddCustomLabel("tenant", context => context.User.FindFirst("tenant_id")?.Value ?? "anonymous");
    });
    app.MapMetrics().AllowAnonymous();
    app.MapGet("/api/session", (HttpContext context) => Results.Ok(new
    {
        userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
        tenantId = context.User.FindFirst("tenant_id")?.Value ?? context.User.FindFirst("tid")?.Value,
        studentId = context.User.FindFirst("student_id")?.Value,
        departmentId = context.User.FindFirst("department_id")?.Value,
        name = context.User.Identity?.Name ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "AegisCore user",
        roles = context.User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(x => x.Value).Distinct().Order().ToArray()
    })).RequireAuthorization();

    app.MapHub<StateMachineHub>("/hubs/statemachine");
    app.MapHub<NotificationHub>("/hubs/notifications");
    app.MapHub<FinanceHub>("/hubs/finance");
    app.MapControllers();
    app.MapFinanceModuleEndpoints();
    app.MapDefaultPredictionEndpoints();
    app.MapVisitingFacultyEndpoints();
    app.MapAccreditationEndpoints();
    app.MapBoardPortalEndpoints();
    app.MapVoiceAssistantEndpoints();
    app.MapGamificationEndpoints();
    app.MapMentalHealthEndpoints();
    app.MapForeignStudentEndpoints();
    app.MapElectionEndpoints();
    app.MapCafeteriaEndpoints();
    app.MapTransportEndpoints();

    try
    {
        Log.Information("Starting AegisCore API...");
        await ApplyDatabaseMigrationsAsync(app);
        await app.RunDeploymentReadinessCheckAsync();
        Log.Information("AegisCore API started successfully.");
        app.Run();
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Application terminated unexpectedly");
        throw;
    }
    finally
    {
        Log.CloseAndFlush();
    }
}


static async Task ApplyDatabaseMigrationsAsync(WebApplication app)
{
    if (!app.Configuration.GetValue("Database:AutoMigrate", false))
        return;

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AegisDbContext>();
    await db.Database.MigrateAsync();
}
