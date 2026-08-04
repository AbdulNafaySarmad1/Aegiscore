using AegisCore.Contracts.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AegisCore.Infrastructure.CoreERP;

public static class CoreErpServiceExtensions
{
    public static IServiceCollection AddCoreErpModules(this IServiceCollection services)
    {
        return services;
    }
}

// NOTE: All DbSets already registered in AegisDbContext.
// Repositories use the generic Repository<T> via IUnitOfWork or direct DI.
// Services and controllers are registered via the standard AddScoped pattern where needed.
// The DI registrations in Program.cs (lines 126-380) already reference all these modules.
// Adding entity class files makes the existing DI registrations compile.
