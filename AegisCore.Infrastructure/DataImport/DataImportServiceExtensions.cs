using AegisCore.Application.DataImport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AegisCore.Infrastructure.DataImport;

public static class DataImportServiceExtensions
{
    public static IServiceCollection AddDataImport(this IServiceCollection services)
    {
        services.TryAddScoped<IDataImportService, DataImportService>();
        return services;
    }
}
