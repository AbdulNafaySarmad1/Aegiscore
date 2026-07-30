using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AegisCore.Contracts.Abstractions.Cloud;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pqc.Crypto.Crystals.Dilithium;
using Org.BouncyCastle.Pqc.Crypto.SphincsPlus;
using Org.BouncyCastle.Security;

namespace AegisCore.Infrastructure.Security.Pqc;

public sealed record PqcSignatureResult(string Algorithm, string KeyId, byte[] Signature, byte[] PublicKey);

public interface IPqcSignatureService
{
    Task<PqcSignatureResult> SignAsync(Guid tenantId, string purpose, ReadOnlyMemory<byte> payload, bool archival, CancellationToken ct = default);
    bool Verify(string algorithm, ReadOnlySpan<byte> payload, ReadOnlySpan<byte> signature, ReadOnlySpan<byte> publicKey);
}

public sealed class PqcSignatureService(ISecretService secrets) : IPqcSignatureService
{
    public async Task<PqcSignatureResult> SignAsync(Guid tenantId, string purpose, ReadOnlyMemory<byte> payload, bool archival, CancellationToken ct = default)
    {
        var algorithm = archival ? "SLH-DSA-SHA2-128s" : "ML-DSA-65";
        var normalizedPurpose = NormalizePurpose(purpose);
        var key = await LoadOrCreateAsync(tenantId, normalizedPurpose, archival, ct);
        var signature = await Task.Run(() => Sign(algorithm, key.PrivateKey, payload.ToArray()), ct);
        return new PqcSignatureResult(algorithm, KeyId(key.PublicKey), signature, key.PublicKey);
    }

    public bool Verify(string algorithm, ReadOnlySpan<byte> payload, ReadOnlySpan<byte> signature, ReadOnlySpan<byte> publicKey)
    {
        try
        {
            if (algorithm == "SLH-DSA-SHA2-128s")
            {
                var signer = new SphincsPlusSigner();
                signer.Init(false, new SphincsPlusPublicKeyParameters(SphincsPlusParameters.sha2_128s, publicKey.ToArray()));
                return signer.VerifySignature(payload.ToArray(), signature.ToArray());
            }
            if (algorithm == "ML-DSA-65")
            {
                var signer = new DilithiumSigner();
                signer.Init(false, new DilithiumPublicKeyParameters(DilithiumParameters.Dilithium3, publicKey.ToArray()));
                return signer.VerifySignature(payload.ToArray(), signature.ToArray());
            }
            return false;
        }
        catch (ArgumentException) { return false; }
    }

    private async Task<KeyMaterial> LoadOrCreateAsync(Guid tenantId, string purpose, bool archival, CancellationToken ct)
    {
        var algorithm = archival ? "slh-dsa-sha2-128s" : "ml-dsa-65";
        var prefix = $"/tenants/{tenantId:D}/pqc/{purpose}/{algorithm}";
        if (await secrets.SecretExistsAsync($"{prefix}/PRIVATE_KEY", ct) && await secrets.SecretExistsAsync($"{prefix}/PUBLIC_KEY", ct))
        {
            return new KeyMaterial(
                DecodePrivate(archival, await secrets.GetSecretAsync($"{prefix}/PRIVATE_KEY", ct)),
                Convert.FromBase64String(await secrets.GetSecretAsync($"{prefix}/PUBLIC_KEY", ct)));
        }

        var pair = await Task.Run(() => Generate(archival), ct);
        await secrets.SetSecretAsync($"{prefix}/PRIVATE_KEY", EncodePrivate(archival, pair.PrivateKey), ct);
        await secrets.SetSecretAsync($"{prefix}/PUBLIC_KEY", Convert.ToBase64String(pair.PublicKey), ct);
        return pair;
    }

    private static KeyMaterial Generate(bool archival)
    {
        var random = new SecureRandom();
        AsymmetricCipherKeyPair pair;
        if (archival)
        {
            var generator = new SphincsPlusKeyPairGenerator();
            generator.Init(new SphincsPlusKeyGenerationParameters(random, SphincsPlusParameters.sha2_128s));
            pair = generator.GenerateKeyPair();
        }
        else
        {
            var generator = new DilithiumKeyPairGenerator();
            generator.Init(new DilithiumKeyGenerationParameters(random, DilithiumParameters.Dilithium3));
            pair = generator.GenerateKeyPair();
        }
        if (archival)
        {
            var privateKey = (SphincsPlusPrivateKeyParameters)pair.Private;
            var publicKey = (SphincsPlusPublicKeyParameters)pair.Public;
            return new KeyMaterial(privateKey, publicKey.GetEncoded());
        }
        var dilithiumPrivate = (DilithiumPrivateKeyParameters)pair.Private;
        var dilithiumPublic = (DilithiumPublicKeyParameters)pair.Public;
        return new KeyMaterial(dilithiumPrivate, dilithiumPublic.GetEncoded());
    }

    private static byte[] Sign(string algorithm, ICipherParameters privateKey, byte[] payload)
    {
        if (algorithm == "SLH-DSA-SHA2-128s")
        {
            var signer = new SphincsPlusSigner();
            signer.Init(true, privateKey);
            return signer.GenerateSignature(payload);
        }
        if (algorithm == "ML-DSA-65")
        {
            var signer = new DilithiumSigner();
            signer.Init(true, privateKey);
            return signer.GenerateSignature(payload);
        }
        throw new ArgumentException("Unsupported PQC signature algorithm.", nameof(algorithm));
    }

    private static ICipherParameters DecodePrivate(bool archival, string encoded)
    {
        if (archival)
        {
            return new SphincsPlusPrivateKeyParameters(SphincsPlusParameters.sha2_128s, Convert.FromBase64String(encoded));
        }
        var value = JsonSerializer.Deserialize<DilithiumPrivateRecord>(encoded) ?? throw new InvalidOperationException("PQC private key is invalid.");
        var publicKey = new DilithiumPublicKeyParameters(DilithiumParameters.Dilithium3, Convert.FromBase64String(value.PublicKey));
        return new DilithiumPrivateKeyParameters(DilithiumParameters.Dilithium3, Convert.FromBase64String(value.Rho), Convert.FromBase64String(value.K), Convert.FromBase64String(value.Tr), Convert.FromBase64String(value.S1), Convert.FromBase64String(value.S2), Convert.FromBase64String(value.T0), Convert.FromBase64String(value.T1));
    }

    private static string EncodePrivate(bool archival, ICipherParameters key)
        => archival
            ? Convert.ToBase64String(((SphincsPlusPrivateKeyParameters)key).GetEncoded())
            : JsonSerializer.Serialize(new DilithiumPrivateRecord(
                Convert.ToBase64String(((DilithiumPrivateKeyParameters)key).GetPublicKey()),
                Convert.ToBase64String(GetDilithiumBytes(key, "GetRho")),
                Convert.ToBase64String(GetDilithiumBytes(key, "GetK")),
                Convert.ToBase64String(GetDilithiumBytes(key, "GetTr")),
                Convert.ToBase64String(GetDilithiumBytes(key, "GetS1")),
                Convert.ToBase64String(GetDilithiumBytes(key, "GetS2")),
                Convert.ToBase64String(GetDilithiumBytes(key, "GetT0")),
                Convert.ToBase64String(GetDilithiumBytes(key, "GetT1"))));

    private static byte[] GetDilithiumBytes(ICipherParameters key, string methodName)
    {
        var method = typeof(DilithiumPrivateKeyParameters).GetMethod($"get_{methodName[3..]}", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("PQC private key format is unsupported.");
        return (byte[])(method.Invoke(key, null) ?? throw new InvalidOperationException("PQC private key component is missing."));
    }

    private static string KeyId(ReadOnlySpan<byte> publicKey) => Convert.ToHexString(SHA256.HashData(publicKey)).ToLowerInvariant()[..32];
    private static string NormalizePurpose(string purpose)
    {
        var value = purpose.Trim().ToLowerInvariant();
        if (value.Length is < 1 or > 50 || value.Any(c => !(char.IsAsciiLetterOrDigit(c) || c == '-'))) throw new ArgumentException("PQC key purpose is invalid.");
        return value;
    }

    private sealed record KeyMaterial(ICipherParameters PrivateKey, byte[] PublicKey);
    private sealed record DilithiumPrivateRecord(string PublicKey, string Rho, string K, string Tr, string S1, string S2, string T0, string T1);
}
