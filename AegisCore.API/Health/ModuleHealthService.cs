using System.Diagnostics;
using System.Net.Sockets;
using System.Reflection;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using StackExchange.Redis;

namespace AegisCore.API.Health;

/// <summary>
/// One row per module, answering the question an administrator asks when a screen is broken: is
/// this module's code wired, are its tables there, and are the services it depends on reachable?
/// A module is every controller group under /api (the first route segment), and its dependencies
/// are what its constructors ask for. Repositories are followed to their entity's table, so a
/// table that never got migrated shows up under the module that needs it.
/// </summary>
public sealed record ModuleHealth(string Module, string Status, IReadOnlyList<string> Problems, int Dependencies, int Tables);

public sealed record InfrastructureHealth(string Name, string Status, string Detail);

public sealed record ModuleHealthReport(string Status, IReadOnlyList<InfrastructureHealth> Infrastructure, IReadOnlyList<ModuleHealth> Modules, string Summary);

public sealed class ModuleHealthService(IServiceProvider services, ApplicationPartManager parts, IConfiguration configuration, ILogger<ModuleHealthService> logger)
{
    public async Task<ModuleHealthReport> ReportAsync(CancellationToken ct)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var infrastructure = new List<InfrastructureHealth>();

        // Database: reachable, and the schema the code expects.
        SchemaVerification? schema = null;
        var db = provider.GetRequiredService<AegisDbContext>();
        try
        {
            var watch = Stopwatch.StartNew();
            await db.Database.ExecuteSqlRawAsync("SELECT 1", ct);
            schema = await SchemaVerifier.VerifyAsync(db, ct);
            infrastructure.Add(new("database", schema.IsCurrent ? "healthy" : "degraded",
                schema.IsCurrent ? $"Reachable in {watch.ElapsedMilliseconds} ms; {schema.Describe()}" : schema.Describe()));
        }
        catch (Exception ex)
        {
            infrastructure.Add(new("database", "unavailable",
                $"The database did not answer: {Root(ex)}. Check that the database server is running and that AEGISCORE_DB_CONNECTION (or DB_HOST/DB_PORT/DB_NAME/DB_USER/DB_PASS) is correct."));
        }

        // Redis: sessions, refresh tokens, rate limits and activation links live here.
        try
        {
            var redis = provider.GetRequiredService<IConnectionMultiplexer>();
            var latency = await redis.GetDatabase().PingAsync();
            infrastructure.Add(new("redis", "healthy", $"Reachable in {latency.TotalMilliseconds:0} ms."));
        }
        catch (Exception ex)
        {
            infrastructure.Add(new("redis", "unavailable",
                $"Redis did not answer: {Root(ex)}. Sign-in, token refresh and rate limiting need it; check that Redis is running and AEGISCORE_REDIS_CONNECTION points at it."));
        }

        infrastructure.Add(await TcpAsync("smtp", configuration["Email:SmtpHost"], configuration.GetValue<int?>("Email:SmtpPort") ?? 587,
            "Outgoing e-mail (activation links, notifications) is configured with Email__SmtpHost/Email__SmtpPort.", ct));

        // Storage: written, read back and compared through the configured provider. Checking that a
        // directory is writable proved nothing while the provider itself discarded every upload.
        var objects = provider.GetRequiredService<AegisCore.Contracts.Abstractions.Cloud.IStorageService>();
        var probeKey = $"health-probe/{Guid.NewGuid():N}";
        var probeBytes = Guid.NewGuid().ToByteArray();
        try
        {
            await objects.CreateBucketIfNotExistsAsync("aegis-health-check", ct);
            await objects.UploadAsync("aegis-health-check", probeKey, new MemoryStream(probeBytes), "application/octet-stream", cancellationToken: ct);
            await using var stored = await objects.DownloadAsync("aegis-health-check", probeKey, ct);
            using var readBack = new MemoryStream();
            await stored.CopyToAsync(readBack, ct);
            infrastructure.Add(readBack.ToArray().SequenceEqual(probeBytes)
                ? new("storage", "healthy", $"A {probeBytes.Length}-byte object was written and read back unchanged via {objects.GetType().Name}.")
                : new("storage", "unavailable", $"{objects.GetType().Name} accepted a {probeBytes.Length}-byte object and returned {readBack.Length} bytes. Documents uploaded here are being lost."));
        }
        catch (Exception ex)
        {
            infrastructure.Add(new("storage", "unavailable", $"Object storage is not usable: {Root(ex)}. Documents, uploads and exports need AEGISCORE_STORAGE_PROVIDER (local or s3) and, for local storage, AEGISCORE_STORAGE_LOCAL_PATH pointing at a writable mounted volume."));
        }
        finally
        {
            try { await objects.DeleteAsync("aegis-health-check", probeKey, ct); } catch { /* the probe is disposable */ }
        }

        var licenseUrl = configuration["License:HeartbeatUrl"] ?? configuration["LICENSE_SERVER_URL"] ?? configuration["License:ServerUrl"];
        var offline = configuration.GetValue<bool>("License:OfflineMode") || string.Equals(configuration["LICENSE_OFFLINE_MODE"], "true", StringComparison.OrdinalIgnoreCase);
        if (offline) infrastructure.Add(new("license-authority", "healthy", "Offline licence mode: the licence key is verified locally and no authority is contacted."));
        else if (Uri.TryCreate(licenseUrl, UriKind.Absolute, out var uri))
            infrastructure.Add(await TcpAsync("license-authority", uri.Host, uri.Port, "The licence heartbeat needs the authority; after two failed heartbeats the installation becomes read-only. Enter an offline licence key on /admin/license if this network cannot reach it.", ct));
        else infrastructure.Add(new("license-authority", "degraded", "No licence authority is configured (LICENSE_SERVER_URL) and offline mode is off: heartbeats will fail and the installation will become read-only. Enter an offline licence key on /admin/license."));

        // Modules: every controller group, its constructor dependencies, its repositories' tables.
        var feature = new ControllerFeature();
        parts.PopulateFeature(feature);
        var missingTables = new HashSet<string>(schema?.MissingTables ?? [], StringComparer.Ordinal);
        var entityTables = db.Model.GetEntityTypes().Where(e => e.GetTableName() is not null).ToDictionary(e => e.ClrType, e => e.GetTableName()!);
        var modules = new List<ModuleHealth>();
        foreach (var group in feature.Controllers.Select(c => c.AsType()).GroupBy(ModuleName).OrderBy(g => g.Key))
        {
            var problems = new List<string>();
            var dependencies = new HashSet<Type>();
            var tables = new HashSet<string>();
            foreach (var controller in group)
            {
                var constructor = controller.GetConstructors().OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();
                foreach (var parameter in constructor?.GetParameters() ?? [])
                {
                    if (parameter.HasDefaultValue) continue;
                    dependencies.Add(parameter.ParameterType);
                    object? resolved = null;
                    try { resolved = provider.GetService(parameter.ParameterType); }
                    catch (Exception ex) { problems.Add($"{controller.Name} needs {parameter.ParameterType.Name}, which failed to construct: {Root(ex)}"); continue; }
                    if (resolved is null) { problems.Add($"{controller.Name} needs {parameter.ParameterType.Name}, which is not registered; every request to this module answers 500."); continue; }
                    foreach (var table in TablesOf(parameter.ParameterType, entityTables)) tables.Add(table);
                }
            }
            foreach (var table in tables.Where(missingTables.Contains).OrderBy(t => t))
                problems.Add($"Table \"{table}\" does not exist in the database; apply the pending migrations.");
            modules.Add(new ModuleHealth(group.Key, problems.Count == 0 ? "healthy" : "unavailable", problems, dependencies.Count, tables.Count));
        }

        var infrastructureDown = infrastructure.Where(i => i.Status == "unavailable").ToList();
        var modulesDown = modules.Where(m => m.Status != "healthy").ToList();
        var status = infrastructureDown.Count > 0 || modulesDown.Count > 0 ? "unhealthy" : infrastructure.Any(i => i.Status == "degraded") ? "degraded" : "healthy";
        var summary = status == "healthy"
            ? $"All {modules.Count} modules are wired and every dependency answers."
            : string.Join(" ", infrastructureDown.Select(i => $"{i.Name}: {i.Detail}").Concat(modulesDown.Select(m => $"Module '{m.Module}': {m.Problems[0]}")));
        if (status != "healthy") logger.LogWarning("Module health {Status}: {Summary}", status, summary);
        return new ModuleHealthReport(status, infrastructure, modules, summary);
    }

    private static string ModuleName(Type controller)
    {
        var route = controller.GetCustomAttribute<RouteAttribute>()?.Template ?? controller.Name;
        var segments = route.Split('/', StringSplitOptions.RemoveEmptyEntries).Where(s => !s.StartsWith('[') && !s.StartsWith('{')).ToList();
        if (segments.Count > 0 && segments[0].Equals("api", StringComparison.OrdinalIgnoreCase)) segments.RemoveAt(0);
        return segments.Count > 0 ? segments[0].ToLowerInvariant() : controller.Name.Replace("Controller", "").ToLowerInvariant();
    }

    /// <summary>The table behind a repository dependency: IRepository&lt;T&gt; directly, or a named repository through the IRepository&lt;T&gt; it extends.</summary>
    private static IEnumerable<string> TablesOf(Type dependency, Dictionary<Type, string> entityTables)
    {
        foreach (var candidate in new[] { dependency }.Concat(dependency.GetInterfaces()))
        {
            if (!candidate.IsGenericType || candidate.GetGenericTypeDefinition() != typeof(AegisCore.Contracts.Common.Interfaces.IRepository<>)) continue;
            if (entityTables.TryGetValue(candidate.GetGenericArguments()[0], out var table)) yield return table;
        }
    }

    private static async Task<InfrastructureHealth> TcpAsync(string name, string? host, int port, string purpose, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(host)) return new(name, "degraded", $"Not configured. {purpose}");
        try
        {
            using var client = new TcpClient();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(3));
            await client.ConnectAsync(host, port, timeout.Token);
            return new(name, "healthy", $"{host}:{port} accepts connections.");
        }
        catch (Exception ex)
        {
            return new(name, "unavailable", $"{host}:{port} did not accept a connection ({Root(ex)}). {purpose}");
        }
    }

    private static string Root(Exception ex) => ex.GetBaseException().Message.Split('\n')[0].Trim();
}
