using System.Security.Cryptography;
using System.Text;
using AegisCore.Application.SsoHitl;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.SsoHitl;

public sealed class MultiTenantSsoService(AegisDbContext db) : IMultiTenantSsoService
{
    private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lower = "abcdefghijkmnopqrstuvwxyz";
    private const string Digits = "23456789";
    private const string Symbols = "!@#$%";
    private const string Alphabet = Upper + Lower + Digits + Symbols;

    public string GetEmailDomain(string email)
    {
        var value = email.Trim();
        var separator = value.IndexOf('@');
        if (separator <= 0 || separator != value.LastIndexOf('@') || separator == value.Length - 1)
            throw new ArgumentException("Invalid email address.", nameof(email));

        var domain = value[(separator + 1)..].ToLowerInvariant();
        if (!domain.Contains('.') || domain.StartsWith('.') || domain.EndsWith('.') || domain.Contains("..") ||
            domain.Any(c => !(char.IsLetterOrDigit(c) || c is '.' or '-')))
            throw new ArgumentException("Invalid email address.", nameof(email));

        return domain;
    }

    public string BuildRedirectUrl(UniversitySsoProfile profile, string returnUrl)
    {
        if (!Uri.TryCreate(returnUrl, UriKind.Absolute, out var callback) || callback.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException("SSO callback must be an HTTPS URL.", nameof(returnUrl));
        var provider = (profile.SsoProvider ?? string.Empty).Trim().ToLowerInvariant();
        return provider switch
        {
            "microsoft" => $"https://login.microsoftonline.com/{Uri.EscapeDataString(profile.SsoTenantId ?? "common")}/oauth2/v2.0/authorize?client_id={Uri.EscapeDataString(profile.SsoClientId ?? string.Empty)}&redirect_uri={Uri.EscapeDataString(returnUrl)}&response_type=code&scope=openid%20email%20profile",
            "google" => $"https://accounts.google.com/o/oauth2/v2/auth?client_id={Uri.EscapeDataString(profile.SsoClientId ?? string.Empty)}&redirect_uri={Uri.EscapeDataString(returnUrl)}&response_type=code&scope=openid%20email%20profile",
            "okta" when Uri.TryCreate(profile.SsoTenantId, UriKind.Absolute, out var okta) && okta.Scheme == Uri.UriSchemeHttps => $"{okta.ToString().TrimEnd('/')}/oauth2/v1/authorize?client_id={Uri.EscapeDataString(profile.SsoClientId ?? string.Empty)}&redirect_uri={Uri.EscapeDataString(returnUrl)}&response_type=code&scope=openid%20email%20profile",
            "ldap" => "/auth/ldap",
            _ => "/auth/login"
        };
    }

    public string GenerateTemporaryPassword()
    {
        var chars = new[]
        {
            Upper[RandomNumberGenerator.GetInt32(Upper.Length)],
            Lower[RandomNumberGenerator.GetInt32(Lower.Length)],
            Digits[RandomNumberGenerator.GetInt32(Digits.Length)],
            Symbols[RandomNumberGenerator.GetInt32(Symbols.Length)]
        }.Concat(Enumerable.Range(0, 12).Select(_ => Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)])).ToArray();

        return new(chars.OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue)).ToArray());
    }

    public async Task<SsoProviderDetection> DetectProviderAsync(string email, string returnUrl, CancellationToken ct = default)
    {
        var domain = GetEmailDomain(email);
        var profile = await db.UniversitySsoProfiles.FirstOrDefaultAsync(u => u.Domain == domain, ct)
            ?? throw new KeyNotFoundException("University SSO profile was not found.");
        return new SsoProviderDetection(profile.Id, profile.Name, profile.SsoProvider, BuildRedirectUrl(profile, returnUrl), profile.LogoUrl, profile.PrimaryColor);
    }
}

public sealed class TwoFactorService : ITwoFactorService
{
    public string GenerateSecret() => ToBase32(RandomNumberGenerator.GetBytes(20));

    public string[] GenerateBackupCodes(int count = 10)
    {
        if (count is < 1 or > 20) throw new ArgumentOutOfRangeException(nameof(count));
        return Enumerable.Range(0, count).Select(_ => $"{RandomNumberGenerator.GetInt32(1000, 9999)}-{RandomNumberGenerator.GetInt32(1000, 9999)}").ToArray();
    }

    public bool VerifyTotp(string base32Secret, string code, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(base32Secret) || code.Length != 6 || !code.All(char.IsDigit)) return false;
        byte[] key;
        try { key = FromBase32(base32Secret); } catch (FormatException) { return false; }
        var step = now.ToUnixTimeSeconds() / 30;
        return Enumerable.Range(-1, 3).Any(offset => Totp(key, step + offset) == code);
    }

    private static string Totp(byte[] key, long step)
    {
        var counter = BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(step));
        var hash = HMACSHA1.HashData(key, counter);
        var offset = hash[^1] & 0x0f;
        var binary = ((hash[offset] & 0x7f) << 24) | ((hash[offset + 1] & 0xff) << 16) | ((hash[offset + 2] & 0xff) << 8) | (hash[offset + 3] & 0xff);
        return (binary % 1_000_000).ToString("D6");
    }

    private static string ToBase32(byte[] bytes)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var bits = string.Concat(bytes.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));
        return string.Concat(Enumerable.Range(0, (bits.Length + 4) / 5).Select(i =>
        {
            var chunk = bits.Substring(i * 5, Math.Min(5, bits.Length - i * 5)).PadRight(5, '0');
            return alphabet[Convert.ToInt32(chunk, 2)];
        }));
    }

    private static byte[] FromBase32(string value)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var bits = string.Concat(value.TrimEnd('=').ToUpperInvariant().Select(c => Convert.ToString(alphabet.IndexOf(c), 2).PadLeft(5, '0')));
        return Enumerable.Range(0, bits.Length / 8).Select(i => Convert.ToByte(bits.Substring(i * 8, 8), 2)).ToArray();
    }
}

public sealed class AgenticExamService : IAgenticExamService
{
    public string BuildQrPayload(Guid examId, Guid studentId)
    {
        if (examId == Guid.Empty || studentId == Guid.Empty) throw new ArgumentException("Exam and student are required.");
        return $"exam:{examId:N}:student:{studentId:N}";
    }

    public bool RequiresHumanReview(decimal confidence, decimal threshold = 0.75m)
    {
        if (confidence is < 0m or > 1m || threshold is < 0m or > 1m) throw new ArgumentOutOfRangeException(nameof(confidence));
        return confidence < threshold;
    }

    public PaperDestructionManifest CreateDestructionManifest(Guid batchId, int paperCount, Guid witnessedBy)
    {
        if (batchId == Guid.Empty || witnessedBy == Guid.Empty) throw new ArgumentException("Batch and witness are required.");
        if (paperCount is <= 0 or > 10000) throw new ArgumentOutOfRangeException(nameof(paperCount));
        var material = $"{batchId:N}:{paperCount}:{witnessedBy:N}:{DateTimeOffset.UtcNow:O}";
        return new PaperDestructionManifest
        {
            Id = Guid.NewGuid(),
            BatchId = batchId,
            PaperCount = paperCount,
            WitnessedBy = witnessedBy,
            ManifestHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material))).ToLowerInvariant()
        };
    }
}
