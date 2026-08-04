using AegisCore.Application.Hr;
using Microsoft.Extensions.DependencyInjection;

namespace AegisCore.Infrastructure.Hr;

public static class VisitingFacultyServiceExtensions
{
    public static IServiceCollection AddVisitingFaculty(this IServiceCollection services)
    {
        services.AddScoped<IVisitingFacultyService, VisitingFacultyService>();
        return services;
    }
}
