using AegisCore.Application.Curriculum;
using Microsoft.Extensions.DependencyInjection;

namespace AegisCore.Infrastructure.Curriculum;

public static class CurriculumServiceExtensions
{
    public static IServiceCollection AddCurriculumManagement(this IServiceCollection services)
    {
        services.AddScoped<ICurriculumService, CurriculumService>();
        return services;
    }
}
