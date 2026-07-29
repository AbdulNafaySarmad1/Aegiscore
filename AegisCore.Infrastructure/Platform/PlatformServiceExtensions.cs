using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Application.Platform;
using AegisCore.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AegisCore.Infrastructure.Platform;

public static class PlatformServiceExtensions
{
    public static IServiceCollection AddPlatformServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddScoped<INotificationTemplateRepository, NotificationTemplateRepository>();
        services.TryAddScoped<INotificationDeliveryRepository, NotificationDeliveryRepository>();
        services.TryAddScoped<IFeatureFlagRepository, FeatureFlagRepository>();
        services.TryAddScoped<IScheduledJobRepository, ScheduledJobRepository>();
        services.TryAddScoped<IJobExecutionRepository, JobExecutionRepository>();
        services.TryAddScoped<IImportBatchRepository, ImportBatchRepository>();
        services.TryAddScoped<IExportLogRepository, ExportLogRepository>();
        services.TryAddScoped<IPlatformMetricRepository, PlatformMetricRepository>();

        services.TryAddScoped<INotificationPlatformService, NotificationPlatformService>();
        services.TryAddScoped<IFeatureFlagService, FeatureFlagService>();
        services.TryAddScoped<ISchedulerService, SchedulerService>();
        services.TryAddScoped<IImportService, ImportService>();
        services.TryAddScoped<IExportService, ExportService>();
        services.TryAddScoped<ICommandCenterService, CommandCenterService>();
        services.TryAddScoped<IGrievanceService, GrievanceService>();
        services.TryAddSingleton<INotificationRealtimePublisher, NoopNotificationRealtimePublisher>();
        services.AddHttpClient("whatsapp", client =>
        {
            client.BaseAddress = new Uri(configuration["WhatsApp:GraphBaseUrl"] ?? "https://graph.facebook.com/");
            client.Timeout = TimeSpan.FromSeconds(20);
        });
        services.AddHttpClient("sms");

        return services;
    }
}
