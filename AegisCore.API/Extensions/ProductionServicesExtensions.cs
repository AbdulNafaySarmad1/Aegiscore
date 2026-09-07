using System.Security.Cryptography;
using System.Text;
using AegisCore.Application.Common.Interfaces;
using AegisCore.API.Authorization;
using AegisCore.API.Background;
using AegisCore.API.Hubs;
using AegisCore.API.Validation;
using AegisCore.Application.Onboarding;
using AegisCore.Application.Platform;
using AegisCore.Application.Finance;
using AegisCore.Infrastructure.Installation;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Onboarding;
using AegisCore.Infrastructure.Security;
using AegisCore.Infrastructure.Security.Pqc;
using AegisCore.Infrastructure.Payments.Raast;
using AegisCore.Infrastructure.Finance;
using AegisCore.Infrastructure.Cloud.R2;
using AegisCore.Infrastructure.Cloud;
using AegisCore.Infrastructure.Cloud.Azure;
using AegisCore.Infrastructure.Deployment;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Extensions.Http;
using System.Security.Cryptography.X509Certificates;
using FluentValidation;

namespace AegisCore.API.Extensions;

public static class CoreServicesExtensions
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services, IConfiguration configuration)
    {
        ConfigureDataProtection(services, configuration);
        services.AddMemoryCache();
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-XSRF-TOKEN";
            options.Cookie.Name = "__Host-XSRF-TOKEN";
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        });
        // CSRF is not a concern for JWT Bearer auth — portal uses Authorization: Bearer header
        // Antiforgery tokens are configured but only enforced on cookie-authenticated endpoints if any exist
        services.AddResponseCompression(options => { options.EnableForHttps = true; options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>(); options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>(); });
        services.AddResponseCaching();
        services.AddHsts(options => { options.MaxAge = TimeSpan.FromDays(365); options.IncludeSubDomains = true; options.Preload = true; });
        services.AddCors(options => options.AddPolicy("CloudflarePages", policy =>
        {
            var configuredOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>()
                ?? configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? [];
            var frontendOrigin = configuration["AEGISCORE_FRONTEND_URL"];
            var applicationOrigin = configuration["APP_URL"];
            var origins = configuredOrigins
                .Append(frontendOrigin ?? string.Empty)
                .Append(applicationOrigin ?? string.Empty)
                .Where(IsValidOrigin)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (origins.Length == 0 && !string.Equals(configuration["ASPNETCORE_ENVIRONMENT"], "Production", StringComparison.OrdinalIgnoreCase))
                origins = ["http://localhost:5000"];
            policy.WithOrigins(origins).SetIsOriginAllowedToAllowWildcardSubdomains().AllowAnyMethod().AllowAnyHeader().AllowCredentials();
        }));
        services.AddRouting();
        services.AddAuthentication();
        services.AddAuthorization(options => options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        services.AddSwaggerGen(options =>
        {
            options.CustomSchemaIds(type => (type.FullName ?? type.Name).Replace('+', '.'));
            options.ResolveConflictingActions(api => api.First());
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "AegisCore API", Version = "v1", Description = "AegisCore multi-tenant university ERP API." });
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT", In = ParameterLocation.Header });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = []
            });
        });
        services.AddHealthChecks();
        services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>(ServiceLifetime.Scoped);
        services.AddScoped<AsyncFluentValidationFilter>();
        services.AddControllers(options => options.Filters.AddService<AsyncFluentValidationFilter>());
        return services;
    }

    private static void ConfigureDataProtection(IServiceCollection services, IConfiguration configuration)
    {
        var keyPath = configuration["DataProtection:KeyPath"];
        if (string.IsNullOrWhiteSpace(keyPath)) keyPath = Path.Combine(AppContext.BaseDirectory, ".data-protection");
        var builder = services.AddDataProtection().SetApplicationName("AegisCore").PersistKeysToFileSystem(new DirectoryInfo(keyPath));
        var certificatePath = configuration["DataProtection:CertificatePath"];
        if (!string.IsNullOrWhiteSpace(certificatePath))
        {
            var password = configuration["DataProtection:CertificatePassword"]
                ?? throw new InvalidOperationException("Data Protection certificate password is required.");
            builder.ProtectKeysWithCertificate(X509CertificateLoader.LoadPkcs12FromFile(certificatePath, password, X509KeyStorageFlags.EphemeralKeySet));
            return;
        }

        if (string.Equals(configuration["ASPNETCORE_ENVIRONMENT"] ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Production", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Production requires an encrypted Data Protection key ring certificate.");
    }

    private static bool IsValidOrigin(string origin)
    {
        if (string.IsNullOrWhiteSpace(origin))
            return false;

        return Uri.TryCreate(origin, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
            && !string.IsNullOrWhiteSpace(uri.Host);
    }
}

public static class SetupServicesExtensions
{
    public static IServiceCollection AddSetupServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IEnvironmentValidator, EnvironmentValidator>();
        services.AddAegisDbContext(configuration);
        services.AddCloudProvider(configuration);
        services.AddHttpClient("infisical", client =>
        {
            if (Uri.TryCreate(configuration["INFISICAL_URL"], UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)
                client.BaseAddress = uri;
        });
        services.AddHttpClient("wizard-providers", client => client.Timeout = TimeSpan.FromSeconds(20));
        services.AddSingleton<IPasswordHashingService, PasswordHashingService>();
        services.AddPqcSigningQueue();
        services.AddScoped<IPqcSignatureService, PqcSignatureService>();
        services.AddScoped<IAccountingStandardsEngine, AccountingStandardsEngine>();
        services.AddScoped<IWizardStateStore, EfWizardStateStore>();
        services.AddScoped<WizardStateService>();
        services.AddSingleton<OnboardingService>();
        services.AddSingleton<EnhancedSetupService>();
        services.AddScoped<WizardSessionService>();
        services.AddScoped<WizardProvisioningService>();
        services.AddScoped<WizardActivationService>();
        services.AddScoped<WizardGatewayService>();
        services.AddScoped<SetupImportService>();
        services.AddTransient<AzureAdIdentityService>();
        services.AddTransient<ISsoProvider, LocalSsoProvider>();
        services.AddTransient<ISsoProvider, EntraSsoProvider>();
        services.AddTransient<ISsoProvider, GoogleWorkspaceSsoProvider>();
        services.AddTransient<ISsoProvider, GenericSamlSsoProvider>();
        services.AddTransient<IAiSetupProvider, OllamaLocalSetupProvider>();
        services.AddTransient<IAiSetupProvider, OllamaCloudSetupProvider>();
        services.AddTransient<IAiSetupProvider, OpenAiSetupProvider>();
        services.AddTransient<IAiSetupProvider, GraniteProvider>();
        services.AddTransient<IAiSetupProvider, CustomAiSetupProvider>();
        services.AddScoped<IWizardProviderConfigurationService, WizardProviderConfigurationService>();
        services.AddScoped<DocumentTemplateService>();
        services.AddScoped<WizardRolePermissionService>();
        services.AddControllers();
        return services;
    }
}

public static class ProductionServicesExtensions
{
    public static IServiceCollection AddProductionServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IPasswordHashingService, PasswordHashingService>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<EnhancedSetupService>();
        services.AddFinanceModule();
        return services;
    }
}

public static class AuthenticationServicesExtensions
{
    public static IServiceCollection AddAuthenticationServices(this IServiceCollection services, IConfiguration configuration)
    {
        var authority = configuration["Auth:Authority"];
        var audience = configuration["Auth:Audience"];
        if (string.IsNullOrWhiteSpace(audience)) audience = "aegiscore-api";
        services.AddAuthentication().AddJwtBearer(options =>
        {
            options.Audience = audience;
            if (Uri.TryCreate(authority, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)
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
            var secret = configuration["AEGISCORE_JWT_SECRET"] ?? configuration["Jwt:Secret"];
            var isProduction = string.Equals(configuration["ASPNETCORE_ENVIRONMENT"] ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Production", StringComparison.OrdinalIgnoreCase);
            var publicKeyPem = configuration["Jwt:PublicKey"];
            if (isProduction && string.IsNullOrWhiteSpace(publicKeyPem))
                throw new InvalidOperationException("Production requires an RSA JWT authority or Jwt:PublicKey.");
            if (string.IsNullOrWhiteSpace(secret))
            {
                if (isProduction)
                    throw new InvalidOperationException("AEGISCORE_JWT_SECRET must be configured in production.");
                secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            }
            options.RequireHttpsMetadata = false;
            if (!string.IsNullOrWhiteSpace(publicKeyPem))
            {
                var publicKey = RSA.Create();
                publicKey.ImportFromPem(publicKeyPem.ToCharArray());
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true, ValidIssuer = "https://aegiscore.pk", ValidateAudience = true, ValidAudience = audience,
                    ValidateLifetime = true, ValidateIssuerSigningKey = true, IssuerSigningKey = new RsaSecurityKey(publicKey),
                    ClockSkew = TimeSpan.FromMinutes(2), ValidAlgorithms = [SecurityAlgorithms.RsaSha256]
                };
                return;
            }
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false, ValidateAudience = true, ValidAudience = audience, ValidateLifetime = true,
                ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                ClockSkew = TimeSpan.FromMinutes(2), ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
            };
        })
        .AddJwtBearer(AegisCore.Infrastructure.Admissions.ApplicantAuth.Scheme, options =>
        {
            // Applicant portal tokens: distinct audience so they can never satisfy staff/student routes.
            var validationKey = AegisCore.Infrastructure.Admissions.ApplicantTokenService.ResolveValidationKey(configuration);
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = AegisCore.Infrastructure.Admissions.ApplicantAuth.Issuer,
                ValidateAudience = true,
                ValidAudience = AegisCore.Infrastructure.Admissions.ApplicantAuth.Audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = validationKey,
                ClockSkew = TimeSpan.FromMinutes(2),
                ValidAlgorithms = validationKey is RsaSecurityKey
                    ? [SecurityAlgorithms.RsaSha256]
                    : [SecurityAlgorithms.HmacSha256]
            };
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async ctx =>
                {
                    var sub = ctx.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                        ?? ctx.Principal?.FindFirst("sub")?.Value;
                    if (!Guid.TryParse(sub, out var applicantId))
                    {
                        ctx.Fail("Invalid applicant subject.");
                        return;
                    }

                    var revocation = ctx.HttpContext.RequestServices
                        .GetRequiredService<AegisCore.Application.Admissions.IApplicantTokenService>();
                    if (await revocation.IsRevokedAsync(applicantId, ctx.HttpContext.RequestAborted))
                    {
                        ctx.Fail("Applicant session has been ended.");
                    }
                }
            };
        });
        services.AddAuthorization();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddScoped<ICasbinPolicySyncService, CasbinPolicySyncService>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, PermissionAuthorizationResultHandler>();
        services.AddSingleton<INotificationRealtimePublisher, SignalRNotificationRealtimePublisher>();
        services.AddSingleton<IFinanceRealtimePublisher, SignalRFinanceRealtimePublisher>();
        return services;
    }

}

public static class BackgroundWorkersExtensions
{
    public static IServiceCollection AddBackgroundWorkers(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRaastPayments();
        services.AddR2Storage(configuration);
        services.AddSignalR(options => { options.EnableDetailedErrors = false; options.MaximumReceiveMessageSize = 64 * 1024; options.StreamBufferCapacity = 10; });
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
