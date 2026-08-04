using AegisCore.Application.DocumentVault;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AegisCore.Infrastructure.DocumentVault;

public static class DocumentVaultServiceExtensions
{
    public static IServiceCollection AddDocumentVault(this IServiceCollection services)
    {
        services.TryAddScoped<IDocumentVaultService, DocumentVaultService>();
        return services;
    }
}
