using AegisCore.Application.MicrosoftEcosystem;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AegisCore.Infrastructure.MicrosoftEcosystem;

public static class MicrosoftEcosystemServiceExtensions
{
    public static IServiceCollection AddMicrosoftEcosystem(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ITeamsProvisioningService, TeamsProvisioningService>();
        services.AddScoped<IMoodleLmsService, MoodleLmsService>();
        services.AddScoped<IPowerBiEmbeddingService, PowerBiEmbeddingService>();
        services.AddHttpClient("graph", c => c.BaseAddress = RequireHttpsUri(configuration["MicrosoftGraph:BaseUrl"], "https://graph.microsoft.com/v1.0/"));
        services.AddHttpClient("moodle");
        return services;
    }

    private static Uri RequireHttpsUri(string? configured, string fallback)
    {
        var value = string.IsNullOrWhiteSpace(configured) ? fallback : configured;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && !uri.IsLoopback))
            throw new InvalidOperationException("Microsoft Graph BaseUrl must use HTTPS (localhost is allowed for development).");
        return uri;
    }
}
