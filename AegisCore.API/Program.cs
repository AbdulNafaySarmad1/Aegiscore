using Serilog;
using AegisCore.API.Background;
using AegisCore.API.Extensions;
using AegisCore.API.Hubs;
using AegisCore.API.Middleware;
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

var demoMode = builder.Configuration.GetValue<bool>("Runtime:DemoMode");
if (demoMode && builder.Environment.IsProduction())
{
    throw new InvalidOperationException(
        "Runtime:DemoMode is forbidden in Production. Configure a real identity provider and license.");
}

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddInstallationState();

var isInstalled = InstallationStateService.HasPersistentMarker(builder.Configuration, builder.Environment.ContentRootPath);

if (!isInstalled)
{
    Log.Information("AegisCore starting in installation mode - no external dependencies required");

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

app.UseDefaultFiles();
app.UseStaticFiles();
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
app.UseSwaggerPortal();

app.MapGet("/health/installation", (IInstallationStateService installation) =>
    Results.Ok(new { isInstalled = installation.IsInstalled() }))
    .AllowAnonymous();

if (!isInstalled)
{
    app.UseSetupRequired();

    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapGet("/health", () => Results.Text("Healthy"));
    app.MapGet("/health/live", () => Results.Text("Healthy"));
    app.MapGet("/health/ready", () => Results.Ok(new
    {
        status = "setup_required",
        message = "AegisCore is healthy and waiting for the guided installation to finish."
    }));

    app.MapGet("/", async context =>
    {
        context.Response.Redirect("/setup/");
    });

    app.MapGet("/setup", async context =>
    {
        context.Response.Redirect("/setup/");
    });

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
    app.UseMiddleware<AuthenticatedOriginPullsMiddleware>();
    app.UseMiddleware<SessionBindingMiddleware>();

    app.UseCors("CloudflarePages");
    app.UseAuthentication();
    app.UseMiddleware<DemoIdentityMiddleware>();
    app.UseAuthorization();
    app.UseMiddleware<LicenseLockoutMiddleware>();

    app.UseMiddleware<TurnstileMiddleware>();
    app.UseMiddleware<AbacMiddleware>();

    app.MapGet("/health", () => Results.Text("Healthy"));
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

    app.MapHub<StateMachineHub>("/hubs/statemachine");
    app.MapHub<NotificationHub>("/hubs/notifications");
    app.MapControllers();

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
