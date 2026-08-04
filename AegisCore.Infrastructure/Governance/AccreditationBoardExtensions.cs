using AegisCore.Application.Governance;
using Microsoft.Extensions.DependencyInjection;

namespace AegisCore.Infrastructure.Governance;

public static class AccreditationServiceExtensions
{
    public static IServiceCollection AddAccreditation(this IServiceCollection services)
    {
        services.AddScoped<IAccreditationService, AccreditationService>();
        return services;
    }
}

public static class BoardPortalServiceExtensions
{
    public static IServiceCollection AddBoardPortal(this IServiceCollection services)
    {
        services.AddScoped<IBoardPortalService, BoardPortalService>();
        return services;
    }
}
