using AegisCore.Application.Licensing;
using AegisCore.Infrastructure.Licensing;
using System.Security.Cryptography;

namespace AegisCore.API.Health;

/// <summary>
/// Operator commands that run instead of the web host:
/// <list type="bullet">
/// <item><c>issue-license --key issuer.key --university-id GUID --name "University" --expires 2027-09-30 [--users 500] [--tier Basic|Premium] [--issued-by "AegisCore"]</c> (vendor side)</item>
/// <item><c>generate-license-issuer-keys --out DIR</c> (vendor side, once)</item>
/// </list>
/// </summary>
public static class VendorCommands
{
    public static bool TryRun(string[] args, out int exitCode)
    {
        exitCode = 0;
        if (args.Length == 0) return false;
        try
        {
            switch (args[0].ToLowerInvariant())
            {
                case "issue-license":
                    Console.WriteLine(IssueLicense(Options(args)));
                    return true;
                case "generate-license-issuer-keys":
                    Console.WriteLine(GenerateIssuerKeys(Options(args)));
                    return true;
                default:
                    return false;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            exitCode = 2;
            return true;
        }
    }

    private static string IssueLicense(Dictionary<string, string> o)
    {
        var keyPath = Require(o, "key", "path to the issuer private key (PEM)");
        var universityId = Guid.Parse(Require(o, "university-id", "the university id shown on the installation's licence page"));
        var name = Require(o, "name", "the university name");
        var expires = DateOnly.Parse(Require(o, "expires", "expiry date, yyyy-MM-dd"));
        var users = int.Parse(o.GetValueOrDefault("users", "500"));
        var tier = o.GetValueOrDefault("tier", "Basic");
        var issuedBy = o.GetValueOrDefault("issued-by", "AegisCore");
        var info = new LicenseInfo(universityId, name, DateOnly.FromDateTime(DateTime.UtcNow), expires, users, issuedBy);
        return OfflineLicense.Issue(File.ReadAllText(keyPath), info, tier);
    }

    private static string GenerateIssuerKeys(Dictionary<string, string> o)
    {
        var dir = o.GetValueOrDefault("out", ".");
        Directory.CreateDirectory(dir);
        var privatePath = Path.Combine(dir, "license-issuer.key");
        var publicPath = Path.Combine(dir, "license-issuer.pub");
        if (File.Exists(privatePath)) throw new InvalidOperationException($"{privatePath} already exists; refusing to overwrite the issuing key.");
        using var rsa = RSA.Create(3072);
        File.WriteAllText(privatePath, rsa.ExportPkcs8PrivateKeyPem() + Environment.NewLine);
        File.WriteAllText(publicPath, rsa.ExportSubjectPublicKeyInfoPem() + Environment.NewLine);
        return $"Issuer key pair written: {privatePath} (keep private) and {publicPath} (configure on installations as License__IssuerPublicKeyPath, or compile in as OfflineLicense.DefaultIssuerPublicKeyPem).";
    }

    private static Dictionary<string, string> Options(string[] args)
    {
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 1; i < args.Length; i++)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal)) continue;
            var key = args[i][2..];
            var value = i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal) ? args[++i] : "true";
            options[key] = value;
        }
        return options;
    }

    private static string Require(Dictionary<string, string> o, string key, string meaning)
        => o.TryGetValue(key, out var value) ? value : throw new ArgumentException($"--{key} is required: {meaning}.");
}
