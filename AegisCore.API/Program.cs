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
using AegisCore.Infrastructure.Commission;
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
using AegisCore.Infrastructure.Deployment;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Polly;
using Polly.Extensions.Http;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

var isInstalled = false;

builder.Services.AddInstallationState();

var installationState = builder.Services.BuildServiceProvider().GetRequiredService<Installation.IInstallationStateService>();
isInstalled = installationState.IsInstalled();

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
    builder.Services.AddInfrastructureServices(builder.Configuration);
    builder.Services.AddProductionServices(builder.Configuration);
    builder.Services.AddAuthenticationServices(builder.Configuration);
    builder.Services.AddBackgroundWorkers(builder.Configuration);
}

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

if (!isInstalled)
{
    app.UseSetupRequired();

    app.UseSwaggerPortal();

    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/health");

    app.MapGet("/", async context =>
    {
        context.Response.Redirect("/setup/");
    });

    app.MapGet("/setup", async context =>
    {
        context.Response.Redirect("/setup/");
    });
}
else
{
    if (app.Environment.IsProduction())
    {
        app.UseHsts();
        app.UseHttpsRedirection();
    }

    app.UseSerilogRequestLogging();
    app.UseMiddleware<RequestLoggingMiddleware>();
    app.UseMiddleware<RateLimitingMiddleware>();
    app.UseMiddleware<AuthenticatedOriginPullsMiddleware>();
    app.UseMiddleware<SessionBindingMiddleware>();

    app.UseCors("CloudflarePages");
    app.UseAuthentication();
    app.UseAuthorization();

    app.UseMiddleware<TurnstileMiddleware>();
    app.UseMiddleware<AbacMiddleware>();
    app.UseMiddleware<GlobalExceptionMiddleware>();

    app.MapHealthChecks("/health");
    app.MapHealthChecks("/health/detail").RequireAuthorization();

    app.MapHub<StateMachineHub>("/hubs/statemachine");
    app.MapControllers();

    try
    {
        Log.Information("Starting AegisCore API...");
        await app.RunDeploymentReadinessCheckAsync();
        Log.Information("AegisCore API started successfully.");
        app.Run();
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Application terminated unexpectedly");
    }
    finally
    {
        Log.CloseAndFlush();
    }
}

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(
            3,
            retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryAttempt, context) =>
            {
                Log.Warning(
                    "Retry {RetryAttempt} after {Delay}ms due to {StatusCode}",
                    retryAttempt,
                    timespan.TotalMilliseconds,
                    outcome.Result?.StatusCode);
            });
}

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            5,
            TimeSpan.FromSeconds(30),
            onBreak: (outcome, timespan) =>
            {
                Log.Warning("Circuit breaker opened for {Duration}ms", timespan.TotalMilliseconds);
            },
            onReset: () =>
            {
                Log.Information("Circuit breaker reset");
            });
}