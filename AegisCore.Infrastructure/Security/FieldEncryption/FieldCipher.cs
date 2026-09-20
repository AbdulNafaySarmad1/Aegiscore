using System.Security.Cryptography;
using System.Text;

namespace AegisCore.Infrastructure.Security.FieldEncryption;

/// <summary>
/// Application-level encryption for individual columns: AES-256-GCM under the installation's
/// master key (<c>AEGISCORE_PQC_KEY_BASE64</c>, the same key the secret vault and the audit-log IP
/// encryption use), stored in the existing string column as <c>enc1r:</c> or <c>enc1d:</c> plus
/// base64(nonce ‖ tag ‖ ciphertext). The column label is authenticated data, so a ciphertext
/// moved to another column will not decrypt.
///
/// Two modes. <b>Randomised</b> (fresh nonce) for free text and contact details. <b>Deterministic</b>
/// (nonce derived from HMAC of the plaintext) for identifiers the application looks up by equality —
/// CNIC, passport number, IP address — so <c>Where(a => a.Cnic == x)</c> keeps working with no
/// query changes: EF Core runs the parameter through the same converter. Equal plaintexts therefore
/// produce equal ciphertexts in those columns, which is the accepted trade-off for a searchable
/// identifier; nothing else about the plaintext is revealed.
///
/// Values without a prefix are returned as they are, so a database written before this existed
/// keeps reading; <c>dotnet AegisCore.API.dll encrypt-fields</c> rewrites those rows.
/// (Compliance control AC-20.)
/// </summary>
public static class FieldCipher
{
    public const string RandomisedPrefix = "enc1r:";
    public const string DeterministicPrefix = "enc1d:";
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private static byte[]? _key;

    public static bool IsConfigured => _key is not null;

    /// <summary>
    /// Installs the master key from the base64 value in configuration. A missing value leaves any
    /// key already installed in place, so a secondary container (a command, a test helper) built
    /// without the variable cannot switch a process back to plaintext.
    /// </summary>
    public static void Configure(string? masterKeyBase64)
    {
        if (string.IsNullOrWhiteSpace(masterKeyBase64)) return;
        var key = Convert.FromBase64String(masterKeyBase64);
        if (key.Length != 32) throw new InvalidOperationException("The field-encryption master key must decode to exactly 32 bytes.");
        _key = key;
    }

    public static string? Protect(string? plaintext, string label, bool deterministic)
    {
        if (plaintext is null) return null;
        if (_key is null) return plaintext;                       // no key: development/test contexts store plaintext
        if (IsProtected(plaintext)) return plaintext;             // already ciphertext: never double-encrypt

        var bytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = deterministic ? DeriveNonce(bytes, label) : RandomNumberGenerator.GetBytes(NonceSize);
        var output = new byte[NonceSize + TagSize + bytes.Length];
        nonce.CopyTo(output, 0);
        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, bytes, output.AsSpan(NonceSize + TagSize), output.AsSpan(NonceSize, TagSize), Encoding.UTF8.GetBytes(label));
        return (deterministic ? DeterministicPrefix : RandomisedPrefix) + Convert.ToBase64String(output);
    }

    public static string? Unprotect(string? stored, string label)
    {
        if (stored is null || !IsProtected(stored)) return stored;
        if (_key is null) throw new InvalidOperationException($"Column '{label}' holds encrypted data but no field-encryption key is configured (AEGISCORE_PQC_KEY_BASE64).");

        var payload = Convert.FromBase64String(stored[RandomisedPrefix.Length..]);
        if (payload.Length < NonceSize + TagSize) throw new CryptographicException($"Column '{label}' holds a malformed ciphertext.");
        var plaintext = new byte[payload.Length - NonceSize - TagSize];
        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(payload.AsSpan(0, NonceSize), payload.AsSpan(NonceSize + TagSize), payload.AsSpan(NonceSize, TagSize), plaintext, Encoding.UTF8.GetBytes(label));
        return Encoding.UTF8.GetString(plaintext);
    }

    public static bool IsProtected(string value) =>
        value.StartsWith(RandomisedPrefix, StringComparison.Ordinal) || value.StartsWith(DeterministicPrefix, StringComparison.Ordinal);

    private static byte[] DeriveNonce(byte[] plaintext, string label)
    {
        var mac = HMACSHA256.HashData(_key!, Encoding.UTF8.GetBytes("nonce:" + label + ":").Concat(plaintext).ToArray());
        return mac[..NonceSize];
    }
}
