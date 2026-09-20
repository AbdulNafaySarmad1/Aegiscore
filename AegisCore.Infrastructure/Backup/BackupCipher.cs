using System.Security.Cryptography;
using System.Text;

namespace AegisCore.Infrastructure.Backup;

/// <summary>
/// Encrypts and decrypts backup files in the exact format produced by
/// <c>openssl enc -aes-256-cbc -salt -pbkdf2 -pass pass:KEY</c>: a "Salted__" header, an 8-byte
/// salt, then AES-256-CBC with PKCS#7 padding, key and IV derived with PBKDF2-HMAC-SHA256 over
/// 10,000 iterations. One format means a dump written by <c>deploy/scripts/backup.sh</c> restores
/// through this service and a dump written by this service restores with
/// <c>deploy/scripts/restore.sh</c> — there is no backup an operator cannot open with tools they
/// already have.
/// </summary>
public static class BackupCipher
{
    private static readonly byte[] Magic = Encoding.ASCII.GetBytes("Salted__");
    private const int SaltLength = 8;
    private const int KeyLength = 32;
    private const int IvLength = 16;
    private const int Pbkdf2Iterations = 10_000;

    public static async Task EncryptAsync(Stream plaintext, Stream ciphertext, string passphrase, CancellationToken ct = default)
    {
        Validate(passphrase);
        var salt = RandomNumberGenerator.GetBytes(SaltLength);
        var (key, iv) = Derive(passphrase, salt);

        await ciphertext.WriteAsync(Magic, ct);
        await ciphertext.WriteAsync(salt, ct);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        await using var crypto = new CryptoStream(ciphertext, aes.CreateEncryptor(), CryptoStreamMode.Write, leaveOpen: true);
        await plaintext.CopyToAsync(crypto, ct);
        await crypto.FlushFinalBlockAsync(ct);
    }

    public static async Task DecryptAsync(Stream ciphertext, Stream plaintext, string passphrase, CancellationToken ct = default)
    {
        Validate(passphrase);
        var header = new byte[Magic.Length + SaltLength];
        await ciphertext.ReadExactlyAsync(header, ct);
        if (!header.AsSpan(0, Magic.Length).SequenceEqual(Magic))
            throw new InvalidDataException("The file is not an encrypted AegisCore backup (missing OpenSSL 'Salted__' header).");

        var (key, iv) = Derive(passphrase, header.AsSpan(Magic.Length, SaltLength).ToArray());
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        try
        {
            await using var crypto = new CryptoStream(ciphertext, aes.CreateDecryptor(), CryptoStreamMode.Read, leaveOpen: true);
            await crypto.CopyToAsync(plaintext, ct);
        }
        catch (CryptographicException ex)
        {
            throw new InvalidDataException("The backup could not be decrypted: wrong key or corrupted file.", ex);
        }
    }

    public static async Task EncryptFileAsync(string plaintextPath, string ciphertextPath, string passphrase, CancellationToken ct = default)
    {
        await using var input = File.OpenRead(plaintextPath);
        await using var output = File.Create(ciphertextPath);
        await EncryptAsync(input, output, passphrase, ct);
    }

    public static async Task DecryptFileAsync(string ciphertextPath, string plaintextPath, string passphrase, CancellationToken ct = default)
    {
        await using var input = File.OpenRead(ciphertextPath);
        await using var output = File.Create(plaintextPath);
        await DecryptAsync(input, output, passphrase, ct);
    }

    private static (byte[] Key, byte[] Iv) Derive(string passphrase, byte[] salt)
    {
        var material = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(passphrase), salt, Pbkdf2Iterations, HashAlgorithmName.SHA256, KeyLength + IvLength);
        return (material[..KeyLength], material[KeyLength..]);
    }

    private static void Validate(string passphrase)
    {
        if (string.IsNullOrEmpty(passphrase) || passphrase.Length < 16)
            throw new InvalidOperationException("The backup encryption key must be at least 16 characters.");
    }
}
