using AegisCore.Contracts.Abstractions.Cloud;
using AegisCore.Infrastructure.Cloud.Azure;
using AegisCore.Infrastructure.Cloud.Aws;
using AegisCore.Infrastructure.Cloud.Gcp;
using AegisCore.Infrastructure.Cloud.Infisical;
using AegisCore.Infrastructure.Cloud.OnPremise;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Cloud;

public static class CloudProviderExtensions
{
    public static IServiceCollection AddCloudProvider(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = (configuration["AEGISCORE_CLOUD_PROVIDER"] ?? "ONPREMISE").ToUpperInvariant();

        services.AddSingleton<ISecretService>(sp =>
            provider switch
            {
                "AZURE" => new AzureSecretService(configuration, sp.GetRequiredService<ILogger<AzureSecretService>>()),
                "AWS" => new AwsSecretsManagerService(configuration, sp.GetRequiredService<ILogger<AwsSecretsManagerService>>()),
                "GCP" => new GcpSecretManagerService(configuration, sp.GetRequiredService<ILogger<GcpSecretManagerService>>()),
                "INFISICAL" => new InfisicalSecretService(sp.GetRequiredService<IHttpClientFactory>(), configuration),
                _ => new EnvironmentVariableSecretService(sp.GetRequiredService<ILogger<EnvironmentVariableSecretService>>())
            });

        services.AddSingleton<IStorageService>(sp =>
            provider switch
            {
                "AZURE" or "R2" => new AzureStorageService(configuration, sp.GetRequiredService<ILogger<AzureStorageService>>()),
                "AWS" => new S3StorageService(configuration, sp.GetRequiredService<ILogger<S3StorageService>>()),
                "GCP" => new GcsStorageService(configuration, sp.GetRequiredService<ILogger<GcsStorageService>>()),
                _ => new MinioStorageService(configuration, sp.GetRequiredService<ILogger<MinioStorageService>>())
            });

        services.AddScoped<IEmailService>(sp =>
            provider switch
            {
                "AZURE" => new MicrosoftGraphEmailService(configuration, sp.GetRequiredService<ILogger<MicrosoftGraphEmailService>>()),
                "AWS" => new SesEmailService(configuration, sp.GetRequiredService<ILogger<SesEmailService>>()),
                "GCP" => new GmailApiEmailService(configuration, sp.GetRequiredService<ILogger<GmailApiEmailService>>()),
                _ => new SmtpEmailService(configuration, sp.GetRequiredService<ILogger<SmtpEmailService>>())
            });

        services.AddScoped<ICalendarService>(sp =>
            provider switch
            {
                "AZURE" => new MicrosoftGraphCalendarService(configuration, sp.GetRequiredService<ILogger<MicrosoftGraphCalendarService>>()),
                "GCP" => new GoogleWorkspaceCalendarService(configuration, sp.GetRequiredService<ILogger<GoogleWorkspaceCalendarService>>()),
                _ => new NoOpCalendarService(sp.GetRequiredService<ILogger<NoOpCalendarService>>())
            });

        services.AddScoped<IIdentityProviderService>(sp =>
            provider switch
            {
                "AZURE" => new AzureAdIdentityService(configuration, sp.GetRequiredService<ILogger<AzureAdIdentityService>>()),
                "AWS" => new CognitoIdentityService(configuration, sp.GetRequiredService<ILogger<CognitoIdentityService>>()),
                "GCP" => new GoogleWorkspaceIdentityService(configuration, sp.GetRequiredService<ILogger<GoogleWorkspaceIdentityService>>()),
                _ => new LocalIdentityService(sp.GetRequiredService<ILogger<LocalIdentityService>>())
            });

        services.AddSingleton<INotificationPushService, FcmPushService>();

        return services;
    }
}
