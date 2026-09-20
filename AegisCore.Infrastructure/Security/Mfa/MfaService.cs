using System.Security.Cryptography;
using System.Text;
using AegisCore.Application.SsoHitl;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure.Security.Pqc;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Security.Mfa;

public sealed record MfaEnrollment(string Secret, string OtpAuthUri, IReadOnlyList<string> BackupCodes);
public sealed record MfaStatus(bool Enrolled, bool Enabled, int BackupCodesLeft, bool RequiredForRole);

/// <summary>
/// Time-based one-time passwords for the local sign-in (compliance control AC-27). The secret is
/// stored encrypted under the installation master key; backup codes are stored as SHA-256 hashes
/// and each works once. Which roles must have a factor is configuration
/// (<c>Security:MfaRequiredRoles</c>, default Admin and SuperAdmin) and is enforced by
/// <c>MfaEnforcementMiddleware</c> on every request, not only at sign-in.
/// </summary>
public sealed class MfaService(AegisDbContext db, ITwoFactorService totp, IPqcCryptographyService encryption)
{
    public const string Issuer = "AegisCore";
    public const string Method = "authenticator";
    public static readonly string[] DefaultRequiredRoles = ["Admin", "SuperAdmin", "Super Admin"];

    public static bool RoleRequiresMfa(IEnumerable<string> roles, IEnumerable<string>? requiredRoles) =>
        roles.Any(role => (requiredRoles ?? DefaultRequiredRoles).Contains(role, StringComparer.OrdinalIgnoreCase));

    public async Task<MfaStatus> StatusAsync(User user, IEnumerable<string> roles, IEnumerable<string>? requiredRoles, CancellationToken ct = default)
    {
        var factor = await FactorAsync(user, ct);
        var codesLeft = factor is null ? 0 : factor.BackupCodesHashCsv.Split(',', StringSplitOptions.RemoveEmptyEntries).Length;
        return new MfaStatus(factor is not null, factor?.IsEnabled == true, codesLeft, RoleRequiresMfa(roles, requiredRoles));
    }

    /// <summary>Creates (or replaces an unconfirmed) factor. Nothing is enforced until <see cref="ConfirmAsync"/>.</summary>
    public async Task<MfaEnrollment> EnrollAsync(User user, CancellationToken ct = default)
    {
        var existing = await FactorAsync(user, ct);
        if (existing?.IsEnabled == true)
            throw new InvalidOperationException("An authenticator is already enabled. Disable it with a current code before enrolling a new one.");

        var secret = totp.GenerateSecret();
        var backupCodes = totp.GenerateBackupCodes();
        var factor = existing ?? new UserSecondFactor { Id = Guid.NewGuid(), TenantId = user.TenantId, UserId = user.Id, Method = Method };
        factor.SecretEncrypted = Protect(user, factor.Id, secret);
        factor.BackupCodesHashCsv = string.Join(',', backupCodes.Select(Hash));
        factor.IsEnabled = false;
        if (existing is null) db.UserSecondFactors.Add(factor);
        await db.SaveChangesAsync(ct);

        var label = Uri.EscapeDataString($"{Issuer}:{user.Email}");
        var uri = $"otpauth://totp/{label}?secret={secret}&issuer={Issuer}&algorithm=SHA1&digits=6&period=30";
        return new MfaEnrollment(secret, uri, backupCodes);
    }

    /// <summary>Turns the factor on once the person proves their authenticator produces the right code.</summary>
    public async Task ConfirmAsync(User user, string code, CancellationToken ct = default)
    {
        var factor = await FactorAsync(user, ct) ?? throw new InvalidOperationException("Enrol an authenticator first.");
        if (!totp.VerifyTotp(Unprotect(user, factor), code.Trim(), DateTimeOffset.UtcNow))
            throw new InvalidOperationException("The code is not valid. Check the time on your phone and try again.");
        factor.IsEnabled = true;
        await db.SaveChangesAsync(ct);
    }

    public async Task DisableAsync(User user, string code, CancellationToken ct = default)
    {
        var factor = await FactorAsync(user, ct) ?? throw new InvalidOperationException("No authenticator is enrolled.");
        if (!await VerifyAsync(user, code, ct))
            throw new InvalidOperationException("The code is not valid.");
        db.UserSecondFactors.Remove(factor);
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> HasEnabledFactorAsync(User user, CancellationToken ct = default)
        => (await FactorAsync(user, ct))?.IsEnabled == true;

    /// <summary>A current authenticator code, or an unused backup code (which is consumed).</summary>
    public async Task<bool> VerifyAsync(User user, string code, CancellationToken ct = default)
    {
        var factor = await FactorAsync(user, ct);
        if (factor is null || !factor.IsEnabled) return false;
        code = code.Trim();
        if (totp.VerifyTotp(Unprotect(user, factor), code, DateTimeOffset.UtcNow)) return true;

        var hashes = factor.BackupCodesHashCsv.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        var used = Hash(code);
        var index = hashes.FindIndex(h => CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(h), Encoding.ASCII.GetBytes(used)));
        if (index < 0) return false;
        hashes.RemoveAt(index);
        factor.BackupCodesHashCsv = string.Join(',', hashes);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private Task<UserSecondFactor?> FactorAsync(User user, CancellationToken ct) =>
        db.UserSecondFactors.IgnoreQueryFilters()
            .SingleOrDefaultAsync(f => f.TenantId == user.TenantId && f.UserId == user.Id && f.Method == Method, ct);

    private string Protect(User user, Guid factorId, string secret) =>
        Convert.ToBase64String(encryption.Encrypt(Encoding.UTF8.GetBytes(secret), Aad(user, factorId)));

    private string Unprotect(User user, UserSecondFactor factor) =>
        Encoding.UTF8.GetString(encryption.Decrypt(Convert.FromBase64String(factor.SecretEncrypted), Aad(user, factor.Id)));

    private static byte[] Aad(User user, Guid factorId) => Encoding.UTF8.GetBytes($"{user.TenantId:N}:{user.Id:N}:{factorId:N}:totp");

    private static string Hash(string code) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code.Trim())));
}
