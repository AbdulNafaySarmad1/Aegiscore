using AegisCore.Application.ApiGateway;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AegisCore.Infrastructure.ApiGateway;

public static class ApiGatewayServiceExtensions
{
    public static IServiceCollection AddApiGateway(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddScoped<IApiKeyRepository, ApiKeyRepository>();
        services.TryAddScoped<IWebhookSubscriptionRepository, WebhookSubscriptionRepository>();
        services.TryAddScoped<IApiKeyService, ApiKeyService>();

        return services;
    }
}
