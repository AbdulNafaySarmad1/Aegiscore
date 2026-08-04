namespace AegisCore.PluginSdk;

public interface IAegisCorePlugin
{
    string Id { get; }
    string Name { get; }
    string Version { get; }
    Task ExecuteAsync(IAegisCorePluginContext context, CancellationToken cancellationToken = default);
}

public interface IAegisCorePluginContext
{
    Guid TenantId { get; }
    Guid ActorId { get; }
    IDictionary<string, object> Data { get; }
    void Log(string message);
}

public sealed record AegisCorePluginManifest(
    string Name,
    string Version,
    string Description,
    string Category,
    string PricingModel,
    decimal Price,
    string DeveloperName,
    string DeveloperEmail,
    string[] RequiredPermissions);
