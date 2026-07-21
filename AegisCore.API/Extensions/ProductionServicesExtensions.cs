using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Serilog;

namespace AegisCore.API.Extensions;

public static class CoreServicesExtensions
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();

        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-XSRF-TOKEN";
            options.Cookie.Name = "__Host-XSRF-TOKEN";
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        });

        services.AddHsts(options =>
        {
            options.MaxAge = TimeSpan.FromDays(365);
            options.IncludeSubDomains = true;
            options.Preload = true;
        });

        services.AddCors(options =>
        {
            options.AddPolicy("CloudflarePages", policy =>
            {
                var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                    ?? new[] { "http://localhost:5000" };

                policy.WithOrigins(origins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        services.AddRouting();
        services.AddAuthorization();

        services.AddHealthChecks();

        services.AddControllers();

        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
        });

        return services;
    }
}

public static class SetupServicesExtensions
{
    public static IServiceCollection AddSetupServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "AegisCore Setup",
                Version = "v1",
                Description = "First-time installation wizard for AegisCore ERP"
            });

            options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
            {
                Name = "x-api-key",
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Description = "API Key for accessing AegisCore endpoints"
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT Bearer token for authenticated access"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" }
                    },
                    new List<string>()
                }
            });
        });

        services.AddSingleton<WizardStateService>();
        services.AddSingleton<OnboardingService>();
        services.AddSingleton<Installation.IInstallationStateService>();

        services.AddControllers();

        return services;
    }
}

public static class ProductionServicesExtensions
{
    public static IServiceCollection AddProductionServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext(configuration);
        services.AddCloudProvider(configuration);
        services.AddComplianceCenter(configuration);
        services.AddLoginAudit();
        services.AddPlatformServices(configuration);
        services.AddDisasterRecovery(configuration);
        services.AddExtendedPlatform();
        services.AddRuleEngine(configuration);
        services.AddSystemAlerting();
        services.AddCoreErpModules();
        services.AddFormsBuilder(configuration);
        services.AddDeploymentReadiness();
        services.AddTenantConfig();
        services.AddMarketplace(configuration);
        services.AddMultiCampus(configuration);
        services.AddWorkflowStudio(configuration);
        services.AddInternationalization(configuration);
        services.AddConfigCenter(configuration);
        services.AddIdentityGovernance(configuration);
        services.AddApiGateway(configuration);
        services.AddDegreeAudit(configuration);
        services.AddEnterpriseSearch(configuration);
        services.AddDigitalTwin(configuration);
        services.AddOnboardingWizard();
        services.AddAiPlatform(configuration);

        return services;
    }
}

public static class AuthenticationServicesExtensions
{
    public static IServiceCollection AddAuthenticationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication()
            .AddJwtBearer(options =>
            {
                options.Authority = configuration["Auth:Authority"];
                options.Audience = configuration["Auth:Audience"];
                options.RequireHttpsMetadata = true;
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = configuration["Auth:Authority"],
                    ValidateAudience = true,
                    ValidAudience = configuration["Auth:Audience"],
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.FromMinutes(2),
                    ValidAlgorithms = new[] {
                        Microsoft.IdentityModel.Tokens.SecurityAlgorithms.RsaSha256,
                        Microsoft.IdentityModel.Tokens.SecurityAlgorithms.EcdsaSha256
                    }
                };
            });

        services.AddAuthorization();

        services.AddDbContext(configuration);
        services.AddRedisConnection(configuration);
        services.AddStateMachine();

        return services;
    }
}

public static class BackgroundWorkersExtensions
{
    public static IServiceCollection AddBackgroundWorkers(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRaastPayments();
        services.AddR2Storage(configuration);
        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
            options.MaximumReceiveMessageSize = 64 * 1024;
            options.StreamBufferCapacity = 10;
        });

        services.AddHttpClient<RaastApiClient>(client =>
        {
            var baseUrl = configuration["Raast:BaseUrl"] ?? "https://api.raast.gov.pk/v1/";
            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                3,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                5,
                TimeSpan.FromSeconds(30));
    }
}