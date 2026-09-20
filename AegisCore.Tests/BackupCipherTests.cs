using System.Security.Cryptography;
using System.Text;
using AegisCore.Contracts.Abstractions.Cloud;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure.Backup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AegisCore.Tests;

/// <summary>
/// The backup format is the one <c>openssl enc -aes-256-cbc -salt -pbkdf2</c> writes, so an
/// operator can open any backup with tools they already have. These tests hold that promise in
/// both directions and prove the service refuses to write a backup it cannot encrypt.
/// </summary>
public sealed class BackupCipherTests
{
    private const string Passphrase = "fixture-passphrase-2026";

    [Fact]
    public async Task Decrypts_a_file_produced_by_openssl()
    {
        var expected = await File.ReadAllBytesAsync(Path.Combine("Fixtures", "openssl-plaintext.txt"));
        await using var encrypted = File.OpenRead(Path.Combine("Fixtures", "openssl-encrypted.bin"));
        using var decrypted = new MemoryStream();

        await BackupCipher.DecryptAsync(encrypted, decrypted, Passphrase);

        Assert.Equal(expected, decrypted.ToArray());
    }

    [Fact]
    public async Task Round_trips_a_large_dump_and_writes_the_openssl_header()
    {
        var plaintext = RandomNumberGenerator.GetBytes(3 * 1024 * 1024 + 17);
        using var encrypted = new MemoryStream();
        await BackupCipher.EncryptAsync(new MemoryStream(plaintext), encrypted, Passphrase);

        var bytes = encrypted.ToArray();
        Assert.Equal("Salted__", Encoding.ASCII.GetString(bytes, 0, 8));
        Assert.Equal(0, (bytes.Length - 16) % 16);

        encrypted.Position = 0;
        using var decrypted = new MemoryStream();
        await BackupCipher.DecryptAsync(encrypted, decrypted, Passphrase);
        Assert.Equal(plaintext, decrypted.ToArray());
    }

    [Fact]
    public async Task Wrong_key_and_plain_files_are_rejected()
    {
        using var encrypted = new MemoryStream();
        await BackupCipher.EncryptAsync(new MemoryStream(Encoding.UTF8.GetBytes("pg_dump output")), encrypted, Passphrase);
        encrypted.Position = 0;
        await Assert.ThrowsAsync<InvalidDataException>(() => BackupCipher.DecryptAsync(encrypted, new MemoryStream(), "another-passphrase-2026"));

        var plain = new MemoryStream(Encoding.UTF8.GetBytes("PGDMP this is not encrypted at all"));
        await Assert.ThrowsAsync<InvalidDataException>(() => BackupCipher.DecryptAsync(plain, new MemoryStream(), Passphrase));

        await Assert.ThrowsAsync<InvalidOperationException>(() => BackupCipher.EncryptAsync(new MemoryStream(), new MemoryStream(), "short"));
    }

    [Fact]
    public async Task Backup_service_refuses_to_run_without_an_encryption_key_and_leaves_no_file_behind()
    {
        var storage = Path.Combine(Path.GetTempPath(), "aegis-backup-" + Guid.NewGuid().ToString("N"));
        var config = new BackupConfiguration
        {
            Id = Guid.NewGuid(), TenantId = Guid.NewGuid(), StoragePath = storage,
            BackupType = AegisCore.Domain.BackupType.Full, CronExpression = "0 0 2 * * ?", RetentionDaysHot = 30
        };
        var configs = new Mock<IBackupConfigurationRepository>();
        configs.Setup(r => r.GetByIdAsync(config.Id, It.IsAny<CancellationToken>())).ReturnsAsync(config);
        var records = new Mock<IBackupRecordRepository>();
        BackupRecord? saved = null;
        records.Setup(r => r.UpdateAsync(It.IsAny<BackupRecord>(), It.IsAny<CancellationToken>()))
            .Callback<BackupRecord, CancellationToken>((r, _) => saved = r).Returns(Task.CompletedTask);
        var secrets = new Mock<ISecretService>();
        secrets.Setup(s => s.GetSecretAsync(BackupService.EncryptionKeyName, It.IsAny<CancellationToken>())).ThrowsAsync(new KeyNotFoundException());
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AEGISCORE_DB_CONNECTION"] = "Host=localhost;Database=aegiscore;Username=x;Password=y"
        }).Build();

        var service = new BackupService(configs.Object, records.Object, configuration, NullLogger<BackupService>.Instance, secrets.Object);
        var record = await service.RunBackupAsync(config.Id, "test");

        Assert.Equal(AegisCore.Domain.BackupStatus.Failed, record.Status);
        Assert.Same(record, saved);
        Assert.False(Directory.Exists(storage) && Directory.EnumerateFiles(storage).Any(), "no plaintext dump may be written when the key is missing");
    }
}
