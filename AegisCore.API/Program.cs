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
using AegisCore.Contracts.Common.Interfaces;
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

// Operator command: `dotnet AegisCore.API.dll rotate-jwt-keys` rotates the token-signing pair on the
// config volume and exits; restart the application afterwards.
if (AegisCore.API.Health.VendorCommands.TryRun(args, out var vendorExit)) { Environment.ExitCode = vendorExit; return 0; }
// Operator command: `dotnet AegisCore.API.dll migrate` applies pending migrations (or initialises an empty
// database) with the same policy the startup uses, prints the outcome and exits.
if (args.Contains("migrate", StringComparer.OrdinalIgnoreCase))
{
    var migrateServices = new ServiceCollection();
    migrateServices.AddLogging(l => l.AddConsole());
    migrateServices.AddAegisDbContext(builder.Configuration);
    await using var migrateProvider = migrateServices.BuildServiceProvider();
    using var migrateScope = migrateProvider.CreateScope();
    var migrateLogger = migrateScope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("migrate");
    try
    {
        var outcome = await AegisCore.Infrastructure.Deployment.StartupBootstrap.SchemaPolicyAsync(
            migrateScope.ServiceProvider.GetRequiredService<AegisDbContext>(),
            new ConfigurationBuilder().AddConfiguration(builder.Configuration).AddInMemoryCollection(new Dictionary<string, string?> { ["Database:AutoMigrate"] = "true" }).Build(),
            migrateLogger);
        Console.WriteLine($"{outcome.Action}: {outcome.Detail}");
    }
    catch (AegisCore.Infrastructure.Deployment.StartupBootstrap.BootstrapException ex)
    {
        Console.Error.WriteLine(ex.Message);
        Environment.ExitCode = 1;
    }
    return 0;
}

// Operator command: `dotnet AegisCore.API.dll encrypt-fields` rewrites rows written before column
// encryption existed (docs/compliance/04, AC-20). Safe to re-run; prints the row count.
if (args.Contains("encrypt-fields", StringComparer.OrdinalIgnoreCase))
{
    var encryptServices = new ServiceCollection();
    encryptServices.AddLogging(l => l.AddConsole());
    encryptServices.AddAegisDbContext(builder.Configuration);
    await using var encryptProvider = encryptServices.BuildServiceProvider();
    using var encryptScope = encryptProvider.CreateScope();
    var encryptLogger = encryptScope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("encrypt-fields");
    var rows = await AegisCore.Infrastructure.Security.FieldEncryption.FieldEncryptionMigrator.RewriteAsync(
        encryptScope.ServiceProvider.GetRequiredService<AegisDbContext>(), encryptLogger);
    Console.WriteLine($"encrypt-fields: {rows} rows rewritten.");
    return 0;
}

if (args.Contains("rotate-jwt-keys", StringComparer.OrdinalIgnoreCase))
{
    Console.WriteLine(AegisCore.Infrastructure.Security.JwtKeyMaterial.Rotate(builder.Configuration));
    return 0;
}

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

// First-start bootstrap, both modes: secrets nobody should invent by hand (setup token, encryption keys,
// tenant id, certificate password, ...), the key-ring certificate and the token-signing pair are generated
// once into the config volume when the environment does not supply them, and reused on every restart.
try
{
    var secretsLogger = LoggerFactory.Create(l => l.AddSerilog()).CreateLogger("bootstrap");
    var configDirectory = AegisCore.Infrastructure.Deployment.GeneratedSecrets.ConfigDirectory(builder.Configuration, builder.Environment.ContentRootPath);
    // One replica provisions at a time; the other finds the artefacts in place.
    using var bootstrapLock = AegisCore.Infrastructure.Deployment.ConfigVolumeLock.Acquire(configDirectory);
    AegisCore.Infrastructure.Deployment.GeneratedSecrets.Ensure(builder.Configuration, builder.Configuration, configDirectory, secretsLogger);
    AegisCore.Infrastructure.Deployment.StartupBootstrap.EnsureDataProtectionCertificate(builder.Configuration, secretsLogger);
    AegisCore.Infrastructure.Deployment.StartupBootstrap.EnsureTokenSigningKeys(builder.Configuration, secretsLogger);
}
catch (AegisCore.Infrastructure.Deployment.StartupBootstrap.BootstrapException ex)
{
    Log.Fatal("[AEGISCORE] STARTUP BLOCKED: {Message}", ex.Message);
    Log.CloseAndFlush();
    return 1;
}


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

// A redundant HTTP replica must not duplicate singleton scheduled business jobs.
// Signing queues and licence heartbeats remain active on every replica.
if (builder.Configuration.GetValue<bool>("Deployment:PassiveWorkers"))
{
    string[] scheduled = ["FinanceAuditSealWorker", "FinancePaymentPlanWorker", "PqcKeyRotationWorker",
        "FinanceLedgerSignatureWorker", "VendorSanctionsRescreenWorker", "ComplianceRetentionWorker", "SystemAlertService"];
    foreach (var descriptor in builder.Services.Where(d => d.ServiceType == typeof(IHostedService)
        && scheduled.Contains(d.ImplementationType?.Name)).ToArray()) builder.Services.Remove(descriptor);
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
    // Installation mode always tries to bring an empty database up to the current schema: the
    // wizard stores its own session in that database, so with the client default
    // Database:AutoMigrate=false the first wizard request would otherwise fail on a missing
    // table. An unreachable database is not fatal here; the wizard's database step retries.
    await ApplyInstallationSchemaAsync(app);
    app.MapGet("/health", () => Results.Text("Healthy")).AllowAnonymous();
    app.MapGet("/health/live", () => Results.Text("Healthy")).AllowAnonymous();
    app.MapGet("/health/ready", () => Results.Ok(new
    {
        status = "setup_required",
        message = "AegisCore is healthy and waiting for the guided installation to finish."
    })).AllowAnonymous();

    app.Run();
    return 0;
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
    app.UseMiddleware<MfaEnforcementMiddleware>();
    app.UseAuthorization();

    app.UseMiddleware<TurnstileMiddleware>();
    app.UseMiddleware<AbacMiddleware>();
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
    // One row per module: wired, tables present, dependencies answering. Written for the person looking at a broken screen.
    app.MapGet("/health/modules", async (AegisCore.API.Health.ModuleHealthService health, CancellationToken ct) =>
    {
        var report = await health.ReportAsync(ct);
        return Results.Json(report, statusCode: report.Status == "unhealthy" ? StatusCodes.Status503ServiceUnavailable : StatusCodes.Status200OK);
    }).RequireAuthorization();
    // What the live database lacks relative to the code, by name: pending migrations, missing tables, missing columns.
    app.MapGet("/health/schema", async (AegisDbContext db, CancellationToken ct) =>
    {
        var verification = await AegisCore.Infrastructure.Database.SchemaVerifier.VerifyAsync(db, ct);
        return Results.Json(new
        {
            status = verification.IsCurrent ? "current" : "drift",
            summary = verification.Describe(),
            verification.PendingMigrations, verification.MissingTables, verification.MissingColumns, verification.MistypedColumns,
            verification.ModelTables, verification.ModelColumns, verification.Note
        }, statusCode: verification.IsCurrent ? 200 : 503);
    }).RequireAuthorization();

    app.MapGet("/health/deep", async (AegisDbContext db, IConnectionMultiplexer redis, IConfiguration config, CancellationToken ct) =>
    {
        var report = new System.Dynamic.ExpandoObject() as IDictionary<string, object?>;
        report["timestamp"] = DateTime.UtcNow.ToString("O");
        report["environment"] = app.Environment.EnvironmentName;
        report["version"] = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0";

        try { await db.Database.ExecuteSqlRawAsync("SELECT 1", ct); report["database"] = "healthy"; }
        catch (Exception ex) { report["database"] = $"unhealthy: {ex.Message}"; }

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
    app.MapSealedWizard();
    app.MapModuleReadEndpoints();
    // Startup check per module: once the host is listening, report what is not wired or not reachable,
    // in the log an operator reads, before the first user finds out from a broken screen.
    app.Lifetime.ApplicationStarted.Register(() => _ = Task.Run(async () =>
    {
        try
        {
            var report = await app.Services.GetRequiredService<AegisCore.API.Health.ModuleHealthService>().ReportAsync(CancellationToken.None);
            if (report.Status == "healthy") Log.Information("[AEGISCORE] Module health: {Summary}", report.Summary);
            else Log.Warning("[AEGISCORE] Module health {Status}: {Summary}", report.Status, report.Summary);
            foreach (var module in report.Modules.Where(m => m.Status != "healthy"))
                Log.Warning("[AEGISCORE] Module '{Module}' unavailable: {Problem}", module.Module, string.Join(" ", module.Problems));
        }
        catch (Exception ex) { Log.Warning(ex, "[AEGISCORE] Module health check could not run."); }
    }));
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
        try
        {
            using var schemaScope = app.Services.CreateScope();
            var outcome = await AegisCore.Infrastructure.Deployment.StartupBootstrap.SchemaPolicyAsync(
                schemaScope.ServiceProvider.GetRequiredService<AegisDbContext>(), app.Configuration, app.Logger);
            Log.Information("[AEGISCORE] Database schema {Action}: {Detail}", outcome.Action, outcome.Detail);
        }
        catch (AegisCore.Infrastructure.Deployment.StartupBootstrap.BootstrapException ex)
        {
            Log.Fatal("[AEGISCORE] STARTUP BLOCKED: {Message}", ex.Message);
            Log.CloseAndFlush();
            return 1;
        }
        await app.RunDeploymentReadinessCheckAsync();
        Log.Information("AegisCore API started successfully.");
        app.Run();
        return 0;
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



static async Task ApplyInstallationSchemaAsync(WebApplication app)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AegisDbContext>();
        var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
        if (pending.Count == 0) return;
        app.Logger.LogInformation("Installation mode: applying {Count} pending database migrations", pending.Count);
        await db.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Installation mode: the database is not ready yet; the wizard's database step will apply migrations");
    }
}
