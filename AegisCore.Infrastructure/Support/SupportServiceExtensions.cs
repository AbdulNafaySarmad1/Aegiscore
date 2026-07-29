using Microsoft.Extensions.DependencyInjection;

namespace AegisCore.Infrastructure.Support;

public static class SupportServiceExtensions
{
    public static IServiceCollection AddAiSupportTicketing(this IServiceCollection services)
    {
        services.AddScoped<SupportTicketService>();
        return services;
    }
}

