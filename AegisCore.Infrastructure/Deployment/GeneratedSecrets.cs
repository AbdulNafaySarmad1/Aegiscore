using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Deployment;

/// <summary>
/// The secrets an installation needs but nobody should have to invent by hand. Any of these that
/// is not supplied through the environment is generated on the first start, written to
/// <c>generated-secrets.env</c> in the config volume (owner-only), and read back on every later
/// start, so a container started with only the database, Redis and its public URL configured
/// comes up with a complete, stable configuration. A value supplied through the environment
/// always wins and is never written here.
/// </summary>
public static class GeneratedSecrets
{
    public const string FileName = "generated-secrets.env";

    /// <summary>Configuration key → how to generate it. Keys use the configuration spelling; the file keeps it too.</summary>
    private static readonly (string Key, Func<string> Generate, string Purpose)[] Catalogue =
    [
        ("TENANT_ID", () => Guid.NewGuid().ToString(), "the installation's tenant id"),
        ("AEGISCORE_SETUP_TOKEN", () => Hex(32), "the setup token typed into the installation wizard (printed in the log at first start)"),
        ("APP_SECRET", () => Hex(32), "application secret"),
        ("AEGISCORE_ENCRYPTION_KEY", () => Base64(32), "field encryption key"),
        ("AEGISCORE_PQC_KEY_BASE64", () => Base64(32), "PQC master key"),
        ("Governance:FeedbackEncryptionKeyBase64", () => Base64(32), "governance feedback encryption key"),
        ("PQC_MLKEM_PUBLIC_KEY", () => Base64(32), "PQC ML-KEM key material"),
        ("PQC_MLKEM_PRIVATE_KEY", () => Base64(32), "PQC ML-KEM key material"),
        ("PQC_MLDSA_PUBLIC_KEY", () => Base64(32), "PQC ML-DSA key material"),
        ("PQC_MLDSA_PRIVATE_KEY", () => Base64(32), "PQC ML-DSA key material"),
        ("License:HmacSecret", () => Hex(32), "licence verification secret for online keys"),
        ("License:HeartbeatServerToken", () => Base64(32), "licence heartbeat token"),
        ("DataProtection:CertificatePassword", () => Hex(32), "password of the key-ring certificate"),
    ];

    /// <summary>The config directory: where the certificate lives, else where the signing keys live, else /app/config, else the content root's .config.</summary>
    public static string ConfigDirectory(IConfiguration configuration, string contentRoot)
    {
        foreach (var key in new[] { "AEGISCORE_CONFIG_DIR", "DataProtection:CertificatePath", "Jwt:PrivateKeyPath", "AEGISCORE_LOCAL_SECRET_PATH" })
        {
            var value = configuration[key];
            if (string.IsNullOrWhiteSpace(value)) continue;
            return key == "AEGISCORE_CONFIG_DIR" ? value : Path.GetDirectoryName(Path.GetFullPath(value))!;
        }
        return Directory.Exists("/app/config") ? "/app/config" : Path.Combine(contentRoot, ".config");
    }

    /// <summary>
    /// Fills every catalogue entry the configuration lacks, from the file first and by generation
    /// second, and adds the result to <paramref name="builder"/> (and the process environment, for
    /// the one reader that bypasses configuration). Returns the keys that were newly generated.
    /// </summary>
    public static IReadOnlyList<string> Ensure(IConfigurationBuilder builder, IConfiguration configuration, string directory, ILogger logger)
    {
        var path = Path.Combine(directory, FileName);
        var stored = File.Exists(path) ? Read(path) : new Dictionary<string, string>(StringComparer.Ordinal);
        var supplied = new Dictionary<string, string?>(StringComparer.Ordinal);
        var generated = new List<string>();
        foreach (var (key, generate, _) in Catalogue)
        {
            if (!string.IsNullOrWhiteSpace(configuration[key])) continue;
            if (!stored.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
            {
                value = generate();
                stored[key] = value;
                generated.Add(key);
            }
            supplied[key] = value;
            Environment.SetEnvironmentVariable(key.Replace(":", "__"), value);
        }
        if (generated.Count > 0)
        {
            try
            {
                Directory.CreateDirectory(directory);
                File.WriteAllLines(path, stored.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => $"{kv.Key}={kv.Value}"));
                if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                throw new StartupBootstrap.BootstrapException($"Generated secrets could not be written to '{path}': {ex.Message}. The config volume must be writable by the application user, or supply the values through the environment.");
            }
            logger.LogInformation("[AEGISCORE] Generated {Count} secret(s) not supplied through the environment and stored them in {Path}: {Keys}. Keep the config volume with your backups.",
                generated.Count, path, string.Join(", ", generated));
        }
        if (supplied.Count > 0) builder.AddInMemoryCollection(supplied);
        return generated;
    }

    private static Dictionary<string, string> Read(string path)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in File.ReadAllLines(path))
        {
            var separator = line.IndexOf('=');
            if (separator <= 0 || line.StartsWith('#')) continue;
            result[line[..separator].Trim()] = line[(separator + 1)..].Trim();
        }
        return result;
    }

    private static string Hex(int bytes) => Convert.ToHexString(RandomNumberGenerator.GetBytes(bytes)).ToLowerInvariant();
    private static string Base64(int bytes) => Convert.ToBase64String(RandomNumberGenerator.GetBytes(bytes));
}
