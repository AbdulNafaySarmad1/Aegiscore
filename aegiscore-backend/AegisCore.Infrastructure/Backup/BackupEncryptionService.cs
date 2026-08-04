using System.Security.Cryptography;
using AegisCore.Application.Backup;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Backup;

// P-FIX-1: AES-256-GCM backup encryption
public class BackupEncryptionService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<BackupEncryptionService> _logger;
    private const string MagicHeader = "AEGISBACKUP_V1\n";
    private const int ChunkSize = 64 * 1024;

    public BackupEncryptionService(IConfiguration configuration, ILogger<BackupEncryptionService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> EncryptBackupFileAsync(string sourceFilePath, CancellationToken ct = default)
    {
        var encryptedPath = Path.ChangeExtension(sourceFilePath, ".backup.enc");

        var encryptionKeyBase64 = _configuration["Backup:EncryptionKey"]
            ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        var keyBytes = Convert.FromBase64String(encryptionKeyBase64);

        await using var inputStream = File.OpenRead(sourceFilePath);
        await using var outputStream = File.Create(encryptedPath);

        await outputStream.WriteAsync(System.Text.Encoding.UTF8.GetBytes(MagicHeader), ct);

        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
        RandomNumberGenerator.Fill(nonce);
        await outputStream.WriteAsync(nonce, ct);

        var buffer = new byte[ChunkSize];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];
        int bytesRead;

        using var aes = new AesGcm(keyBytes, AesGcm.TagByteSizes.MaxSize);

        while ((bytesRead = await inputStream.ReadAsync(buffer, ct)) > 0)
        {
            var ciphertext = new byte[bytesRead];
            aes.Encrypt(nonce, buffer.AsSpan(0, bytesRead), ciphertext, tag);
            await outputStream.WriteAsync(ciphertext, ct);
            await outputStream.WriteAsync(tag, ct);
        }

        File.Delete(sourceFilePath);
        _logger.LogInformation("Backup encrypted: {Source} → {Encrypted}", sourceFilePath, encryptedPath);
        return encryptedPath;
    }

    public static byte[] GenerateEncryptionKey()
    {
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        return key;
    }
}
