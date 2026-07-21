using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace AegisCore.API.Extensions;

public static class ApiServicesExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();

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

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "AegisCore API",
                Version = "v1",
                Description = "Production-grade API Gateway for AegisCore — university ERP ecosystem.",
                Contact = new OpenApiContact { Name = "AegisCore Team", Email = "api@aegiscore.edu" }
            });

            options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
            {
                Name = "x-api-key",
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Description = "API Key for accessing AegisCore endpoints. Register at /api/developer/keys."
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT Bearer token for authenticated access."
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

        services.AddHealthChecks();

        services.AddControllers();
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
        });

        return services;
    }

    public static IApplicationBuilder UseSwaggerPortal(this IApplicationBuilder app)
    {
        return app.UseSwagger()
                  .UseSwaggerUI(options =>
                  {
                      options.SwaggerEndpoint("/swagger/v1/swagger.json", "AegisCore API v1");
                      options.RoutePrefix = "swagger";
                      options.DocumentTitle = "AegisCore Developer Portal";
                      options.EnablePersistAuthorization();
                      options.EnableFilter();
                      options.EnableTryItOutByDefault();
                      options.DisplayRequestDuration();
                  });
    }
}
