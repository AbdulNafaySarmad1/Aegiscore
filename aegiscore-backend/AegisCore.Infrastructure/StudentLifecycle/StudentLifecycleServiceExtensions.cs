using AegisCore.Application.StudentLifecycle;
using Microsoft.Extensions.DependencyInjection;

namespace AegisCore.Infrastructure.StudentLifecycle;

public static class StudentLifecycleServiceExtensions
{
    public static IServiceCollection AddStudentLifecycle(this IServiceCollection services)
    {
        services.AddScoped<IStudentLifecycleService, StudentLifecycleService>();
        return services;
    }
}
