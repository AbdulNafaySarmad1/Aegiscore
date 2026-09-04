using System.Security.Cryptography;
using System.Text;
using AegisCore.Contracts.Abstractions.Cloud;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Security;

namespace AegisCore.Infrastructure.Security.Pqc;

public sealed record PqcSignatureResult(string Algorithm, string KeyId, byte[] Signature, byte[] PublicKey);

public static class PqcKeyHealth
{
    public static bool IsDead(DateTime createdAt, DateTime? lastUsedAt, DateTime now, int inactivityDays)
    {
        if (inactivityDays < 1) throw new ArgumentOutOfRangeException(nameof(inactivityDays));
        var cutoff = now.AddDays(-inactivityDays);
        return lastUsedAt.HasValue ? lastUsedAt.Value < cutoff : createdAt < cutoff;
    }
}

public interface IPqcSignatureService
{
    Task<PqcSignatureResult> SignAsync(Guid tenantId, string purpose, ReadOnlyMemory<byte> payload, bool archival, CancellationToken ct = default);
    Task<string> RotateAsync(Guid tenantId, string purpose, bool archival, CancellationToken ct = default);
    bool Verify(string algorithm, ReadOnlySpan<byte> payload, ReadOnlySpan<byte> signature, ReadOnlySpan<byte> publicKey);
}

public sealed class PqcSignatureService : IPqcSignatureService
{
    private readonly ISecretService _secrets;
    private readonly AegisDbContext? _db;
    private readonly IPqcSigningQueue _queue;

    public PqcSignatureService(ISecretService secrets)
    {
        _secrets = secrets;
        _queue = InlinePqcSigningQueue.Instance;
    }
    [ActivatorUtilitiesConstructor]
    public PqcSignatureService(ISecretService secrets, AegisDbContext db, IPqcSigningQueue queue)
    {
        _secrets = secrets;
        _db = db;
        _queue = queue;
    }

    public async Task<PqcSignatureResult> SignAsync(Guid tenantId, string purpose, ReadOnlyMemory<byte> payload, bool archival, CancellationToken ct = default)
    {
        var algorithm = archival ? "SLH-DSA-SHA2-128s" : "ML-DSA-65";
        var normalizedPurpose = NormalizePurpose(purpose);
        var key = await LoadOrCreateAsync(tenantId, normalizedPurpose, archival, ct);
        var payloadBytes = payload.ToArray();
        var signed = await _queue.ExecuteAsync(() => new(algorithm, KeyId(key.PublicKey), Sign(algorithm, key.PrivateKey, payloadBytes), key.PublicKey), ct);
        if (_db is not null)
        {
            var keyId = KeyId(key.PublicKey);
            var registry = await _db.FinancePqcKeys.SingleAsync(x => x.TenantId == tenantId && x.Purpose == normalizedPurpose && x.KeyId == keyId, ct);
            registry.LastUsedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
        return signed;
    }

    public bool Verify(string algorithm, ReadOnlySpan<byte> payload, ReadOnlySpan<byte> signature, ReadOnlySpan<byte> publicKey)
        => VerifyDetached(algorithm, payload, signature, publicKey);

    public static bool VerifyDetached(string algorithm, ReadOnlySpan<byte> payload, ReadOnlySpan<byte> signature, ReadOnlySpan<byte> publicKey)
    {
        if (algorithm == "SLH-DSA-SHA2-128s")
            return VerifySlhDsa(payload, signature, publicKey);
        if (algorithm == "ML-DSA-65")
            return VerifyMlDsa(payload, signature, publicKey);
        return false;
    }

    public async Task<string> RotateAsync(Guid tenantId, string purpose, bool archival, CancellationToken ct = default)
    {
        if (_db is null) throw new InvalidOperationException("A database-backed PQC registry is required for key rotation.");
        var normalizedPurpose = NormalizePurpose(purpose);
        var pair = await Task.Run(() => Generate(archival), ct);
        await PersistNewKeyAsync(tenantId, normalizedPurpose, archival, pair, ct);
        return KeyId(pair.PublicKey);
    }

    private async Task<KeyMaterial> LoadOrCreateAsync(Guid tenantId, string purpose, bool archival, CancellationToken ct)
    {
        var algorithm = archival ? "slh-dsa-sha2-128s" : "ml-dsa-65";
        if (_db is not null)
        {
            var active = await _db.FinancePqcKeys
                .Where(x => x.TenantId == tenantId && x.Purpose == purpose && x.IsActive && x.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(ct);
            if (active is not null && await _secrets.SecretExistsAsync($"{active.SecretNamespace}/PRIVATE_KEY", ct) && await _secrets.SecretExistsAsync($"{active.SecretNamespace}/PUBLIC_KEY", ct))
                return new KeyMaterial(DecodePrivate(archival, await _secrets.GetSecretAsync($"{active.SecretNamespace}/PRIVATE_KEY", ct)), Convert.FromBase64String(await _secrets.GetSecretAsync($"{active.SecretNamespace}/PUBLIC_KEY", ct)));

            if (await _db.FinancePqcKeys.AnyAsync(x => x.TenantId == tenantId && x.Purpose == purpose, ct))
                throw new InvalidOperationException("The signing key is expired or unavailable; an approved key ceremony is required.");

            var pair = await Task.Run(() => Generate(archival), ct);
            await PersistNewKeyAsync(tenantId, purpose, archival, pair, ct);
            return pair;
        }

        var legacyPrefix = $"/tenants/{tenantId:D}/pqc/{purpose}/{algorithm}";
        if (await _secrets.SecretExistsAsync($"{legacyPrefix}/PRIVATE_KEY", ct) && await _secrets.SecretExistsAsync($"{legacyPrefix}/PUBLIC_KEY", ct))
        {
            return new KeyMaterial(
                DecodePrivate(archival, await _secrets.GetSecretAsync($"{legacyPrefix}/PRIVATE_KEY", ct)),
                Convert.FromBase64String(await _secrets.GetSecretAsync($"{legacyPrefix}/PUBLIC_KEY", ct)));
        }

        var legacyPair = await Task.Run(() => Generate(archival), ct);
        await _secrets.SetSecretAsync($"{legacyPrefix}/PRIVATE_KEY", EncodePrivate(archival, legacyPair.PrivateKey), ct);
        await _secrets.SetSecretAsync($"{legacyPrefix}/PUBLIC_KEY", Convert.ToBase64String(legacyPair.PublicKey), ct);
        return legacyPair;
    }

    private async Task PersistNewKeyAsync(Guid tenantId, string purpose, bool archival, KeyMaterial pair, CancellationToken ct)
    {
        var algorithm = archival ? "slh-dsa-sha2-128s" : "ml-dsa-65";
        var keyId = KeyId(pair.PublicKey);
        var prefix = $"/tenants/{tenantId:D}/pqc/{purpose}/{algorithm}/{keyId}";
        await _secrets.SetSecretAsync($"{prefix}/PRIVATE_KEY", EncodePrivate(archival, pair.PrivateKey), ct);
        await _secrets.SetSecretAsync($"{prefix}/PUBLIC_KEY", Convert.ToBase64String(pair.PublicKey), ct);
        var existing = await _db!.FinancePqcKeys.Where(x => x.TenantId == tenantId && x.Purpose == purpose && x.IsActive).ToListAsync(ct);
        foreach (var old in existing) old.IsActive = false;
        _db.FinancePqcKeys.Add(new FinancePqcKey
        {
            Id = Guid.NewGuid(), TenantId = tenantId, Purpose = purpose, Algorithm = algorithm, KeyId = keyId, SecretNamespace = prefix,
            PublicKey = pair.PublicKey, CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddDays(archival ? 1825 : 90), IsActive = true
        });
        await _db.SaveChangesAsync(ct);
    }

    private static KeyMaterial Generate(bool archival)
    {
        var random = new SecureRandom();
        AsymmetricCipherKeyPair pair;
        if (archival)
        {
            var generator = new SlhDsaKeyPairGenerator();
            generator.Init(new SlhDsaKeyGenerationParameters(random, SlhDsaParameters.slh_dsa_sha2_128s));
            pair = generator.GenerateKeyPair();
        }
        else
        {
            var generator = new MLDsaKeyPairGenerator();
            generator.Init(new MLDsaKeyGenerationParameters(random, MLDsaParameters.ml_dsa_65));
            pair = generator.GenerateKeyPair();
        }
        if (archival)
        {
            var privateKey = (SlhDsaPrivateKeyParameters)pair.Private;
            var publicKey = (SlhDsaPublicKeyParameters)pair.Public;
            return new KeyMaterial(privateKey, publicKey.GetEncoded());
        }
        var mlDsaPrivate = (MLDsaPrivateKeyParameters)pair.Private;
        var mlDsaPublic = (MLDsaPublicKeyParameters)pair.Public;
        return new KeyMaterial(mlDsaPrivate, mlDsaPublic.GetEncoded());
    }

    private static byte[] Sign(string algorithm, ICipherParameters privateKey, byte[] payload)
    {
        if (algorithm == "SLH-DSA-SHA2-128s")
        {
            var signer = new SlhDsaSigner(SlhDsaParameters.slh_dsa_sha2_128s, deterministic: true);
            signer.Init(true, privateKey);
            signer.BlockUpdate(payload, 0, payload.Length);
            return signer.GenerateSignature();
        }
        if (algorithm == "ML-DSA-65")
        {
            var signer = new MLDsaSigner(MLDsaParameters.ml_dsa_65, deterministic: true);
            signer.Init(true, privateKey);
            signer.BlockUpdate(payload, 0, payload.Length);
            return signer.GenerateSignature();
        }
        throw new ArgumentException("Unsupported PQC signature algorithm.", nameof(algorithm));
    }

    private static ICipherParameters DecodePrivate(bool archival, string encoded)
    {
        var prefix = archival ? "fips205:" : "fips204:";
        if (!encoded.StartsWith(prefix, StringComparison.Ordinal))
            throw new InvalidOperationException("A legacy draft PQC signing key cannot be used; complete an approved key rotation ceremony.");
        var bytes = Convert.FromBase64String(encoded[prefix.Length..]);
        try
        {
            return archival
                ? SlhDsaPrivateKeyParameters.FromEncoding(SlhDsaParameters.slh_dsa_sha2_128s, bytes)
                : MLDsaPrivateKeyParameters.FromEncoding(MLDsaParameters.ml_dsa_65, bytes);
        }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }

    private static string EncodePrivate(bool archival, ICipherParameters key)
        => (archival ? "fips205:" : "fips204:") + Convert.ToBase64String(archival
            ? ((SlhDsaPrivateKeyParameters)key).GetEncoded()
            : ((MLDsaPrivateKeyParameters)key).GetEncoded());

    private static bool VerifyMlDsa(ReadOnlySpan<byte> payload, ReadOnlySpan<byte> signature, ReadOnlySpan<byte> publicKey)
    {
        try
        {
            var signer = new MLDsaSigner(MLDsaParameters.ml_dsa_65, deterministic: true);
            signer.Init(false, MLDsaPublicKeyParameters.FromEncoding(MLDsaParameters.ml_dsa_65, publicKey.ToArray()));
            signer.BlockUpdate(payload);
            return signer.VerifySignature(signature.ToArray());
        }
        catch (ArgumentException) { return false; }
    }

    private static bool VerifySlhDsa(ReadOnlySpan<byte> payload, ReadOnlySpan<byte> signature, ReadOnlySpan<byte> publicKey)
    {
        try
        {
            var signer = new SlhDsaSigner(SlhDsaParameters.slh_dsa_sha2_128s, deterministic: true);
            signer.Init(false, SlhDsaPublicKeyParameters.FromEncoding(SlhDsaParameters.slh_dsa_sha2_128s, publicKey.ToArray()));
            signer.BlockUpdate(payload);
            return signer.VerifySignature(signature.ToArray());
        }
        catch (ArgumentException) { return false; }
    }

    private static string KeyId(ReadOnlySpan<byte> publicKey) => Convert.ToHexString(SHA256.HashData(publicKey)).ToLowerInvariant()[..32];
    private static string NormalizePurpose(string purpose)
    {
        var value = purpose.Trim().ToLowerInvariant();
        if (value.Length is < 1 or > 50 || value.Any(c => !(char.IsAsciiLetterOrDigit(c) || c == '-'))) throw new ArgumentException("PQC key purpose is invalid.");
        return value;
    }

    private sealed record KeyMaterial(ICipherParameters PrivateKey, byte[] PublicKey);
}
