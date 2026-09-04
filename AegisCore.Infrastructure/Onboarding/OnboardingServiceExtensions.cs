using AegisCore.Application.Onboarding;
using AegisCore.Infrastructure.Onboarding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AegisCore.Infrastructure.Onboarding;

public static class OnboardingServiceExtensions
{
    public static IServiceCollection AddOnboardingWizard(this IServiceCollection services)
    {
        services.TryAddScoped<IWizardStateStore, EfWizardStateStore>();
        services.TryAddScoped<WizardStateService>();
        services.TryAddSingleton<OnboardingService>();
        services.TryAddScoped<SetupImportService>();
        return services;
    }
}
