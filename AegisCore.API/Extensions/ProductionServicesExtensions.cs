using System.Security.Cryptography;
using System.Text;
using AegisCore.API.Authorization;
using AegisCore.API.Background;
using AegisCore.API.Hubs;
using AegisCore.Application.Onboarding;
using AegisCore.Infrastructure.Installation;
using AegisCore.Infrastructure.Onboarding;
using AegisCore.Infrastructure.Payments.Raast;
using AegisCore.Infrastructure.Cloud.R2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Extensions.Http;

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
        services.AddHsts(options => { options.MaxAge = TimeSpan.FromDays(365); options.IncludeSubDomains = true; options.Preload = true; });
        services.AddCors(options => options.AddPolicy("CloudflarePages", policy =>
        {
            var origins = configuration.GetSection("AllowedOrigins").Get<string[]>()
                ?? configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? ["http://localhost:5000"];
            policy.WithOrigins(origins).SetIsOriginAllowedToAllowWildcardSubdomains().AllowAnyMethod().AllowAnyHeader().AllowCredentials();
        }));
        services.AddRouting();
        services.AddAuthentication();
        services.AddAuthorization(options => options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        services.AddSwaggerGen(options =>
        {
            options.CustomSchemaIds(type => (type.FullName ?? type.Name).Replace('+', '.'));
            options.ResolveConflictingActions(api => api.First());
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "Nocturne API", Version = "v1", Description = "Nocturne multi-tenant university ERP API." });
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT", In = ParameterLocation.Header });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = []
            });
        });
        services.AddHealthChecks();
        services.AddControllers();
        return services;
    }
}

public static class SetupServicesExtensions
{
    public static IServiceCollection AddSetupServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<WizardStateService>();
        services.AddSingleton<OnboardingService>();
        services.AddSingleton<EnhancedSetupService>();
        services.AddControllers();
        return services;
    }
}

public static class ProductionServicesExtensions
{
    public static IServiceCollection AddProductionServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<EnhancedSetupService>();
        return services;
    }
}

public static class AuthenticationServicesExtensions
{
    public static IServiceCollection AddAuthenticationServices(this IServiceCollection services, IConfiguration configuration)
    {
        var authority = configuration["Auth:Authority"];
        var audience = configuration["Auth:Audience"];
        if (string.IsNullOrWhiteSpace(audience) || audience.StartsWith("${", StringComparison.Ordinal)) audience = "aegiscore-api";
        services.AddAuthentication().AddJwtBearer(options =>
        {
            options.Audience = audience;
            if (Uri.TryCreate(authority, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps && !authority!.StartsWith("${", StringComparison.Ordinal))
            {
                options.Authority = authority; options.RequireHttpsMetadata = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true, ValidIssuer = authority, ValidateAudience = true, ValidAudience = audience,
                    ValidateLifetime = true, ValidateIssuerSigningKey = true, ClockSkew = TimeSpan.FromMinutes(2),
                    ValidAlgorithms = [SecurityAlgorithms.RsaSha256, SecurityAlgorithms.EcdsaSha256]
                };
                return;
            }
            var secret = configuration["NOCTURNE_JWT_SECRET"] ?? configuration["Jwt:Secret"];
            if (string.IsNullOrWhiteSpace(secret))
            {
                if (string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Production", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("NOCTURNE_JWT_SECRET must be configured in production.");
                secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            }
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false, ValidateAudience = true, ValidAudience = audience, ValidateLifetime = true,
                ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                ClockSkew = TimeSpan.FromMinutes(2), ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
            };
        });
        services.AddAuthorization();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, ModuleAuthorizationHandler>();
        services.AddSingleton<INotificationRealtimePublisher, SignalRNotificationRealtimePublisher>();
        return services;
    }
}

public static class BackgroundWorkersExtensions
{
    public static IServiceCollection AddBackgroundWorkers(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRaastPayments();
        services.AddR2Storage(configuration);
        services.AddSignalR(options => { options.EnableDetailedErrors = true; options.MaximumReceiveMessageSize = 64 * 1024; options.StreamBufferCapacity = 10; });
        services.AddHostedService<LicenseHeartbeatHostedService>();
        services.AddHttpClient<RaastApiClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["Raast:BaseUrl"] ?? "https://api.raast.gov.pk/v1/");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.Timeout = TimeSpan.FromSeconds(30);
        }).AddPolicyHandler(HttpPolicyExtensions.HandleTransientHttpError().OrResult(x => x.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(3, retry => TimeSpan.FromSeconds(Math.Pow(2, retry))))
          .AddPolicyHandler(HttpPolicyExtensions.HandleTransientHttpError().CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));
        return services;
    }
}
