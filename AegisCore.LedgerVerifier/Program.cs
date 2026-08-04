using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AegisCore.Infrastructure.Security.Pqc;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Security;

if (args is ["--generate-ml-dsa-fixture", var fixturePath])
{
    var generator = new MLDsaKeyPairGenerator();
    generator.Init(new MLDsaKeyGenerationParameters(new SecureRandom(), MLDsaParameters.ml_dsa_65));
    var pair = generator.GenerateKeyPair();
    var payload = Encoding.UTF8.GetBytes("AegisCore ML-DSA-65 interoperability");
    var signer = new MLDsaSigner(MLDsaParameters.ml_dsa_65, deterministic: true);
    signer.Init(true, pair.Private);
    signer.BlockUpdate(payload);
    var fixture = new InteropFixture(
        "ML-DSA-65",
        Convert.ToBase64String(payload),
        Convert.ToBase64String(signer.GenerateSignature()),
        Convert.ToBase64String(((MLDsaPublicKeyParameters)pair.Public).GetEncoded()));
    await File.WriteAllTextAsync(fixturePath, JsonSerializer.Serialize(fixture));
    return 0;
}

if (args.Length != 1 || !File.Exists(args[0]))
{
    Console.Error.WriteLine("Usage: aegiscore-ledger-verifier <ledger-archive.json> | --generate-ml-dsa-fixture <output.json>");
    return 2;
}

try
{
    await using var stream = File.OpenRead(args[0]);
    var archive = await JsonSerializer.DeserializeAsync<LedgerArchive>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
        ?? throw new InvalidDataException("Archive is empty.");
    byte[] previous = new byte[32];
    foreach (var entry in archive.Entries)
    {
        var declaredPrevious = Convert.FromBase64String(entry.PreviousHash);
        var declaredHash = Convert.FromBase64String(entry.EntryHash);
        if (!CryptographicOperations.FixedTimeEquals(previous, declaredPrevious)) throw new InvalidDataException($"Chain link failed at {entry.Id}.");
        var calculated = SHA256.HashData([.. declaredPrevious, .. Encoding.UTF8.GetBytes(entry.CanonicalPayload)]);
        if (!CryptographicOperations.FixedTimeEquals(calculated, declaredHash)) throw new InvalidDataException($"Entry hash failed at {entry.Id}.");
        if (!PqcSignatureService.VerifyDetached(entry.Algorithm, declaredHash, Convert.FromBase64String(entry.Signature), Convert.FromBase64String(entry.PublicKey)))
            throw new InvalidDataException($"Archival signature failed at {entry.Id}.");
        previous = declaredHash;
    }
    Console.WriteLine($"Verified {archive.Entries.Count} ledger entries for tenant {archive.TenantId:D}.");
    return 0;
}
catch (Exception ex) when (ex is JsonException or FormatException or IOException or CryptographicException)
{
    Console.Error.WriteLine($"Verification failed: {ex.Message}");
    return 1;
}

internal sealed record LedgerArchive(Guid TenantId, DateTime GeneratedAt, IReadOnlyList<LedgerArchiveEntry> Entries);
internal sealed record LedgerArchiveEntry(Guid Id, string CanonicalPayload, string PreviousHash, string EntryHash, string Algorithm, string Signature, string PublicKey);
internal sealed record InteropFixture(string Algorithm, string Payload, string Signature, string PublicKey);
