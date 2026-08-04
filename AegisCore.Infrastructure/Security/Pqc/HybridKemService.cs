using System.Security.Cryptography;
using System.Text;
using AegisCore.Contracts.Abstractions.Cloud;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Kems;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace AegisCore.Infrastructure.Security.Pqc;

public sealed record HybridKemPublicKey(byte[] MLKemPublicKey, byte[] X25519PublicKey, string Algorithm = "ML-KEM-1024+X25519");
public sealed record HybridKemEnvelope(byte[] MLKemCiphertext, byte[] EphemeralX25519PublicKey, string Algorithm = "ML-KEM-1024+X25519");
public sealed record HybridKemEncapsulation(HybridKemEnvelope Envelope, byte[] SharedSecret);

public interface IHybridKemService
{
    Task<HybridKemPublicKey> GetOrCreatePublicKeyAsync(Guid tenantId, CancellationToken ct = default);
    HybridKemEncapsulation Encapsulate(HybridKemPublicKey recipient, ReadOnlySpan<byte> context);
    Task<byte[]> DecapsulateAsync(Guid tenantId, HybridKemEnvelope envelope, ReadOnlyMemory<byte> context, CancellationToken ct = default);
}

public sealed class HybridKemService(ISecretService secrets) : IHybridKemService
{
    private const string AlgorithmPath = "ml-kem-1024-x25519-v1";

    public async Task<HybridKemPublicKey> GetOrCreatePublicKeyAsync(Guid tenantId, CancellationToken ct = default)
    {
        RequireTenant(tenantId);
        var prefix = Prefix(tenantId);
        if (await secrets.SecretExistsAsync($"{prefix}/ML_KEM_PRIVATE_KEY", ct))
            return new HybridKemPublicKey(
                Convert.FromBase64String(await secrets.GetSecretAsync($"{prefix}/ML_KEM_PUBLIC_KEY", ct)),
                Convert.FromBase64String(await secrets.GetSecretAsync($"{prefix}/X25519_PUBLIC_KEY", ct)));

        var random = new SecureRandom();
        var mlKemGenerator = new MLKemKeyPairGenerator();
        mlKemGenerator.Init(new MLKemKeyGenerationParameters(random, MLKemParameters.ml_kem_1024));
        var mlKem = mlKemGenerator.GenerateKeyPair();
        var xPrivate = new X25519PrivateKeyParameters(random);
        var xPublic = xPrivate.GeneratePublicKey();
        var mlKemPrivate = ((MLKemPrivateKeyParameters)mlKem.Private).GetEncoded();
        var mlKemPublic = ((MLKemPublicKeyParameters)mlKem.Public).GetEncoded();
        var xPrivateBytes = xPrivate.GetEncoded();
        var xPublicBytes = xPublic.GetEncoded();
        await secrets.SetSecretAsync($"{prefix}/ML_KEM_PRIVATE_KEY", Convert.ToBase64String(mlKemPrivate), ct);
        await secrets.SetSecretAsync($"{prefix}/ML_KEM_PUBLIC_KEY", Convert.ToBase64String(mlKemPublic), ct);
        await secrets.SetSecretAsync($"{prefix}/X25519_PRIVATE_KEY", Convert.ToBase64String(xPrivateBytes), ct);
        await secrets.SetSecretAsync($"{prefix}/X25519_PUBLIC_KEY", Convert.ToBase64String(xPublicBytes), ct);
        CryptographicOperations.ZeroMemory(mlKemPrivate);
        CryptographicOperations.ZeroMemory(xPrivateBytes);
        return new HybridKemPublicKey(mlKemPublic, xPublicBytes);
    }

    public HybridKemEncapsulation Encapsulate(HybridKemPublicKey recipient, ReadOnlySpan<byte> context)
    {
        if (recipient.MLKemPublicKey.Length == 0 || recipient.X25519PublicKey.Length != 32) throw new ArgumentException("Hybrid recipient key is invalid.");
        var random = new SecureRandom();
        var encapsulator = new MLKemEncapsulator(MLKemParameters.ml_kem_1024);
        encapsulator.Init(new ParametersWithRandom(MLKemPublicKeyParameters.FromEncoding(MLKemParameters.ml_kem_1024, recipient.MLKemPublicKey), random));
        var ciphertext = new byte[encapsulator.EncapsulationLength];
        var postQuantumSecret = new byte[encapsulator.SecretLength];
        encapsulator.Encapsulate(ciphertext, postQuantumSecret);
        var ephemeralPrivate = new X25519PrivateKeyParameters(random);
        var ephemeralPublic = ephemeralPrivate.GeneratePublicKey();
        var classicalSecret = X25519(ephemeralPrivate, new X25519PublicKeyParameters(recipient.X25519PublicKey));
        var combined = Derive(postQuantumSecret, classicalSecret, context);
        CryptographicOperations.ZeroMemory(postQuantumSecret);
        CryptographicOperations.ZeroMemory(classicalSecret);
        return new HybridKemEncapsulation(new HybridKemEnvelope(ciphertext, ephemeralPublic.GetEncoded()), combined);
    }

    public async Task<byte[]> DecapsulateAsync(Guid tenantId, HybridKemEnvelope envelope, ReadOnlyMemory<byte> context, CancellationToken ct = default)
    {
        RequireTenant(tenantId);
        if (envelope.MLKemCiphertext.Length == 0 || envelope.EphemeralX25519PublicKey.Length != 32) throw new ArgumentException("Hybrid envelope is invalid.");
        var prefix = Prefix(tenantId);
        var mlKemPrivateBytes = Convert.FromBase64String(await secrets.GetSecretAsync($"{prefix}/ML_KEM_PRIVATE_KEY", ct));
        var xPrivateBytes = Convert.FromBase64String(await secrets.GetSecretAsync($"{prefix}/X25519_PRIVATE_KEY", ct));
        try
        {
            var decapsulator = new MLKemDecapsulator(MLKemParameters.ml_kem_1024);
            decapsulator.Init(MLKemPrivateKeyParameters.FromEncoding(MLKemParameters.ml_kem_1024, mlKemPrivateBytes));
            var postQuantumSecret = new byte[decapsulator.SecretLength];
            decapsulator.Decapsulate(envelope.MLKemCiphertext, postQuantumSecret);
            var classicalSecret = X25519(new X25519PrivateKeyParameters(xPrivateBytes), new X25519PublicKeyParameters(envelope.EphemeralX25519PublicKey));
            try { return Derive(postQuantumSecret, classicalSecret, context.Span); }
            finally { CryptographicOperations.ZeroMemory(postQuantumSecret); CryptographicOperations.ZeroMemory(classicalSecret); }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(mlKemPrivateBytes);
            CryptographicOperations.ZeroMemory(xPrivateBytes);
        }
    }

    private static byte[] X25519(X25519PrivateKeyParameters privateKey, X25519PublicKeyParameters publicKey)
    {
        var agreement = new X25519Agreement();
        agreement.Init(privateKey);
        var secret = new byte[agreement.AgreementSize];
        agreement.CalculateAgreement(publicKey, secret, 0);
        return secret;
    }

    private static byte[] Derive(ReadOnlySpan<byte> postQuantum, ReadOnlySpan<byte> classical, ReadOnlySpan<byte> context)
    {
        var input = new byte[postQuantum.Length + classical.Length];
        postQuantum.CopyTo(input);
        classical.CopyTo(input.AsSpan(postQuantum.Length));
        try
        {
            var hkdf = new HkdfBytesGenerator(new Sha256Digest());
            hkdf.Init(new HkdfParameters(input, SHA256.HashData(context), Encoding.UTF8.GetBytes("AegisCore hybrid KEM v1")));
            var output = new byte[32];
            hkdf.GenerateBytes(output, 0, output.Length);
            return output;
        }
        finally { CryptographicOperations.ZeroMemory(input); }
    }

    private static string Prefix(Guid tenantId) => $"/tenants/{tenantId:D}/pqc/transport/{AlgorithmPath}";
    private static void RequireTenant(Guid tenantId) { if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required."); }
}
