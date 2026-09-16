using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;

namespace AegisCore.Infrastructure.Security;

/// <summary>
/// Resolves the RSA key pair that signs and validates access tokens. Production refuses HMAC
/// secrets, so a client container needs RSA material; it comes either inline
/// (<c>Jwt:PrivateKey</c> / <c>Jwt:PublicKey</c>, PEM) or from files named by
/// <c>Jwt:PrivateKeyPath</c> / <c>Jwt:PublicKeyPath</c>. The client image points those at the
/// persistent config volume, and installation mode generates the pair there on first start,
/// so a fresh container is self-provisioning and every restart reuses the same keys.
/// </summary>
public static class JwtKeyMaterial
{
    public const string PrivateKeyKey = "Jwt:PrivateKey";
    public const string PublicKeyKey = "Jwt:PublicKey";
    public const string PrivateKeyPathKey = "Jwt:PrivateKeyPath";
    public const string PublicKeyPathKey = "Jwt:PublicKeyPath";

    public static string? PrivateKeyPem(IConfiguration configuration) => Read(configuration, PrivateKeyKey, PrivateKeyPathKey);
    public static string? PublicKeyPem(IConfiguration configuration) => Read(configuration, PublicKeyKey, PublicKeyPathKey);

    private static string? Read(IConfiguration configuration, string inlineKey, string pathKey)
    {
        var inline = configuration[inlineKey];
        if (!string.IsNullOrWhiteSpace(inline)) return inline;
        var path = configuration[pathKey];
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;
        var pem = File.ReadAllText(path);
        return string.IsNullOrWhiteSpace(pem) ? null : pem;
    }

    /// <summary>
    /// Generates an RSA-3072 pair at the configured paths when both are set and neither file exists.
    /// Returns true when a pair was written. Never overwrites: an existing key ring stays valid across restarts.
    /// </summary>
    public static bool EnsureGenerated(IConfiguration configuration)
    {
        var privatePath = configuration[PrivateKeyPathKey];
        var publicPath = configuration[PublicKeyPathKey];
        if (string.IsNullOrWhiteSpace(privatePath) || string.IsNullOrWhiteSpace(publicPath)) return false;
        if (!string.IsNullOrWhiteSpace(configuration[PrivateKeyKey]) || !string.IsNullOrWhiteSpace(configuration[PublicKeyKey])) return false;
        if (File.Exists(privatePath) || File.Exists(publicPath)) return false;

        using var rsa = RSA.Create(3072);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(privatePath))!);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(publicPath))!);
        File.WriteAllText(privatePath, rsa.ExportRSAPrivateKeyPem() + Environment.NewLine);
        File.WriteAllText(publicPath, rsa.ExportSubjectPublicKeyInfoPem() + Environment.NewLine);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(privatePath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            File.SetUnixFileMode(publicPath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead);
        }
        return true;
    }
}
