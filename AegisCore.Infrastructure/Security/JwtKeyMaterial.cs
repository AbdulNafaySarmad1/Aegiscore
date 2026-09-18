using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AegisCore.Infrastructure.Security;

/// <summary>
/// Resolves the RSA key pair that signs and validates access tokens.
/// <para>
/// Production signs with RSA only; an HMAC secret is accepted in Development alone. The material
/// comes either inline (<c>Jwt:PrivateKey</c> / <c>Jwt:PublicKey</c>, PEM) or from files named by
/// <c>Jwt:PrivateKeyPath</c> / <c>Jwt:PublicKeyPath</c>. The client image points those at the
/// persistent config volume, and installation mode generates the pair there on first start, so a
/// fresh container is self-provisioning and every restart reuses the same keys.
/// </para>
/// <para>
/// Rotation: every token carries a <c>kid</c> derived from the public key. A retired public key
/// supplied as <c>Jwt:PreviousPublicKey</c> / <c>Jwt:PreviousPublicKeyPath</c> keeps validating the
/// tokens it signed until they expire (access tokens live minutes; refresh tokens are opaque and
/// live in Redis), so a rotation is: move the current .pub to the previous path, generate a new
/// pair, restart. Nobody is signed out.
/// </para>
/// </summary>
public static class JwtKeyMaterial
{
    public const string PrivateKeyKey = "Jwt:PrivateKey";
    public const string PublicKeyKey = "Jwt:PublicKey";
    public const string PrivateKeyPathKey = "Jwt:PrivateKeyPath";
    public const string PublicKeyPathKey = "Jwt:PublicKeyPath";
    public const string PreviousPublicKeyKey = "Jwt:PreviousPublicKey";
    public const string PreviousPublicKeyPathKey = "Jwt:PreviousPublicKeyPath";

    public static string? PrivateKeyPem(IConfiguration configuration) => Read(configuration, PrivateKeyKey, PrivateKeyPathKey);
    public static string? PublicKeyPem(IConfiguration configuration) => Read(configuration, PublicKeyKey, PublicKeyPathKey);
    public static string? PreviousPublicKeyPem(IConfiguration configuration)
    {
        var inline = configuration[PreviousPublicKeyKey];
        if (!string.IsNullOrWhiteSpace(inline)) return inline;
        var path = PreviousPublicKeyPath(configuration);
        if (path is null || !File.Exists(path)) return null;
        var pem = File.ReadAllText(path);
        return string.IsNullOrWhiteSpace(pem) ? null : pem;
    }

    /// <summary>The retired public key's path: configured, or the current one's name with ".previous" before the extension.</summary>
    public static string? PreviousPublicKeyPath(IConfiguration configuration)
    {
        var configured = configuration[PreviousPublicKeyPathKey];
        if (!string.IsNullOrWhiteSpace(configured)) return configured;
        var current = configuration[PublicKeyPathKey];
        if (string.IsNullOrWhiteSpace(current)) return null;
        return Path.ChangeExtension(current, ".previous" + Path.GetExtension(current));
    }

    /// <summary>
    /// Rotates the file-based pair: the current public key becomes the previous one, a new pair is
    /// written, and the caller restarts the application so every replica loads it. Tokens signed
    /// before the rotation keep validating until they expire. Run as
    /// <c>dotnet AegisCore.API.dll rotate-jwt-keys</c> inside the container.
    /// </summary>
    public static string Rotate(IConfiguration configuration)
    {
        var privatePath = configuration[PrivateKeyPathKey];
        var publicPath = configuration[PublicKeyPathKey];
        if (string.IsNullOrWhiteSpace(privatePath) || string.IsNullOrWhiteSpace(publicPath))
            throw new InvalidOperationException("Rotation works on file-based keys: set Jwt:PrivateKeyPath and Jwt:PublicKeyPath.");
        if (!File.Exists(privatePath) || !File.Exists(publicPath))
            throw new InvalidOperationException($"No current key pair at '{privatePath}' / '{publicPath}' to rotate.");
        var previousPath = PreviousPublicKeyPath(configuration)!;
        File.Copy(publicPath, previousPath, overwrite: true);
        File.Delete(privatePath);
        File.Delete(publicPath);
        EnsureGenerated(configuration);
        using var rsa = RSA.Create();
        rsa.ImportFromPem(File.ReadAllText(publicPath));
        return $"New signing key {KeyId(rsa)} written to '{privatePath}' / '{publicPath}'; the previous public key is kept at '{previousPath}' and still validates tokens it signed. Restart the application to load the new key; delete the previous key once every token it signed has expired (SESSION_LIFETIME, 15 minutes by default).";
    }

    public static bool IsProduction(IConfiguration configuration) => string.Equals(
        configuration["ASPNETCORE_ENVIRONMENT"] ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
        "Production", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The key tokens are signed with, or null when no RSA material is configured. See
    /// <see cref="ProductionKeyProblem"/> for the startup check that names a missing key file.
    /// </summary>
    public static RsaSecurityKey? SigningKey(IConfiguration configuration)
    {
        var pem = PrivateKeyPem(configuration);
        if (string.IsNullOrWhiteSpace(pem)) return null;
        var rsa = RSA.Create();
        rsa.ImportFromPem(pem.ToCharArray());
        return new RsaSecurityKey(rsa) { KeyId = KeyId(rsa) };
    }

    /// <summary>The current public key followed by the previous one, when configured.</summary>
    public static IReadOnlyList<RsaSecurityKey> ValidationKeys(IConfiguration configuration)
    {
        var keys = new List<RsaSecurityKey>();
        foreach (var pem in new[] { PublicKeyPem(configuration), PreviousPublicKeyPem(configuration) })
        {
            if (string.IsNullOrWhiteSpace(pem)) continue;
            var rsa = RSA.Create();
            rsa.ImportFromPem(pem.ToCharArray());
            keys.Add(new RsaSecurityKey(rsa) { KeyId = KeyId(rsa) });
        }
        return keys;
    }

    /// <summary>
    /// The message that explains why production cannot issue or validate tokens, or null when it can.
    /// </summary>
    public static string? ProductionKeyProblem(IConfiguration configuration)
    {
        if (!IsProduction(configuration)) return null;
        if (!string.IsNullOrWhiteSpace(configuration["Auth:Authority"])) return null;
        var privatePath = configuration[PrivateKeyPathKey];
        var publicPath = configuration[PublicKeyPathKey];
        if (string.IsNullOrWhiteSpace(configuration[PrivateKeyKey]) && !string.IsNullOrWhiteSpace(privatePath) && !File.Exists(privatePath))
            return $"The RSA token-signing private key is missing: {PrivateKeyPathKey} points at '{privatePath}' and no such file exists. It is generated during installation into the config volume; if that volume was recreated, restore it from backup or remove the installation marker to reinstall.";
        if (string.IsNullOrWhiteSpace(configuration[PublicKeyKey]) && !string.IsNullOrWhiteSpace(publicPath) && !File.Exists(publicPath))
            return $"The RSA token-signing public key is missing: {PublicKeyPathKey} points at '{publicPath}' and no such file exists.";
        if (string.IsNullOrWhiteSpace(PrivateKeyPem(configuration)) || string.IsNullOrWhiteSpace(PublicKeyPem(configuration)))
            return "Production signs access tokens with RSA only. Configure Jwt:PrivateKeyPath and Jwt:PublicKeyPath (generated during installation), or Jwt:PrivateKey and Jwt:PublicKey (PEM), or an external Auth:Authority. An HMAC secret is not accepted in production.";
        return null;
    }

    /// <summary>A stable, URL-safe identifier for a public key: the first 16 bytes of the SHA-256 of its SPKI encoding.</summary>
    public static string KeyId(RSA rsa)
        => Base64UrlEncoder.Encode(SHA256.HashData(rsa.ExportSubjectPublicKeyInfo()).AsSpan(0, 16).ToArray());

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
