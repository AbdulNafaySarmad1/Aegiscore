using AegisCore.Application.StudentLifecycle;
using Microsoft.Extensions.DependencyInjection;

namespace AegisCore.Infrastructure.StudentLifecycle;

public static class StudentLifecycleModuleExtensions
{
    public static IServiceCollection AddVoiceAssistant(this IServiceCollection services)
    {
        services.AddScoped<IVoiceAssistantService, VoiceAssistantService>();
        return services;
    }

    public static IServiceCollection AddGamification(this IServiceCollection services)
    {
        services.AddScoped<IGamificationService, GamificationService>();
        return services;
    }

    public static IServiceCollection AddMentalHealth(this IServiceCollection services)
    {
        services.AddScoped<IMentalHealthService, MentalHealthService>();
        return services;
    }

    public static IServiceCollection AddForeignStudent(this IServiceCollection services)
    {
        services.AddScoped<IForeignStudentService, ForeignStudentService>();
        return services;
    }
}
