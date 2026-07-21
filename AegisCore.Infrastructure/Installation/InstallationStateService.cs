using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text;

namespace AegisCore.Infrastructure.Installation;

public interface IInstallationStateService
{
    bool IsInstalled();
    Task<bool> IsInstalledAsync(CancellationToken ct = default);
    Task MarkAsInstalledAsync(CancellationToken ct = default);
}

public class InstallationStateService : IInstallationStateService
{
    private const string INSTALLATION_MARKER_KEY = "AEGISCORE_INSTALLATION_COMPLETED";
    private const string INSTALLATION_FILE_NAME = "installation.json";
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly IServiceProvider _serviceProvider;

    public InstallationStateService(
        IConfiguration configuration,
        IHostEnvironment environment,
        IServiceProvider serviceProvider)
    {
        _configuration = configuration;
        _environment = environment;
        _serviceProvider = serviceProvider;
    }

    public bool IsInstalled()
    {
        var envValue = Environment.GetEnvironmentVariable(INSTALLATION_MARKER_KEY);
        if (!string.IsNullOrWhiteSpace(envValue) && envValue.Equals("true", StringComparison.OrdinalIgnoreCase))
            return true;

        var configValue = _configuration[INSTALLATION_MARKER_KEY];
        if (!string.IsNullOrWhiteSpace(configValue) && configValue.Equals("true", StringComparison.OrdinalIgnoreCase))
            return true;

        if (HasDatabaseMarker())
            return true;

        return false;
    }

    public async Task<bool> IsInstalledAsync(CancellationToken ct = default)
    {
        return await Task.Run(() => IsInstalled(), ct);
    }

    public async Task MarkAsInstalledAsync(CancellationToken ct = default)
    {
        var appDataPath = Path.Combine(_environment.ContentRootPath, "config");
        Directory.CreateDirectory(appDataPath);
        var filePath = Path.Combine(appDataPath, INSTALLATION_FILE_NAME);

        var marker = new InstallationMarker
        {
            InstalledAt = DateTime.UtcNow,
            Version = typeof(InstallationStateService).Assembly.GetName().Version?.ToString() ?? "1.0.0"
        };

        var json = System.Text.Json.JsonSerializer.Serialize(marker, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(filePath, json, ct);

        Environment.SetEnvironmentVariable(INSTALLATION_MARKER_KEY, "true");
    }

    private bool HasDatabaseMarker()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetService<Domain.AegisDbContext>();
            if (dbContext == null)
                return false;

            return dbContext.Database.CanConnect();
        }
        catch
        {
            return false;
        }
    }
}

public record InstallationMarker
{
    public DateTime InstalledAt { get; init; }
    public string Version { get; init; } = string.Empty;
}

public static class InstallationStateExtensions
{
    public static IServiceCollection AddInstallationState(this IServiceCollection services)
    {
        services.AddSingleton<IInstallationStateService, InstallationStateService>();
        return services;
    }
}