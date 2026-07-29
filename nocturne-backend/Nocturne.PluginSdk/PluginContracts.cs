namespace Nocturne.PluginSdk;

public interface INocturnePlugin
{
    string Id { get; }
    string Name { get; }
    string Version { get; }
    Task ExecuteAsync(INocturnePluginContext context, CancellationToken cancellationToken = default);
}

public interface INocturnePluginContext
{
    Guid TenantId { get; }
    Guid ActorId { get; }
    IDictionary<string, object> Data { get; }
    void Log(string message);
}

public sealed record NocturnePluginManifest(
    string Name,
    string Version,
    string Description,
    string Category,
    string PricingModel,
    decimal Price,
    string DeveloperName,
    string DeveloperEmail,
    string[] RequiredPermissions);
