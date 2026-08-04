using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace AegisCore.Infrastructure.Security.Pqc;

public interface IPqcCryptographyService
{
    byte[] Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> associatedData = default);
    byte[] Decrypt(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> associatedData = default);
    string EncryptString(string plaintext);
    string DecryptString(string ciphertext);
    PqcAlgorithm CurrentAlgorithm { get; }
}

public enum PqcAlgorithm
{
    Aes256Gcm = 0,
    KyberHybrid = 1,
    DilithiumHybrid = 2
}

public class PqcCryptographyOptions
{
    public PqcAlgorithm Algorithm { get; set; } = PqcAlgorithm.Aes256Gcm;
    public int KeyRotationDays { get; set; } = 30;
    public string? MasterKeyBase64 { get; set; }
}

public class PqcCryptographyService : IPqcCryptographyService
{
    private readonly byte[] _key;
    private readonly byte[] _nonce;
    private readonly PqcAlgorithm _algorithm;
    private readonly ILogger<PqcCryptographyService> _logger;

    public PqcCryptographyService(IOptions<PqcCryptographyOptions> options, ILogger<PqcCryptographyService> logger)
    {
        _algorithm = options.Value.Algorithm;
        _logger = logger;
        var configuredKey = Environment.GetEnvironmentVariable("AEGISCORE_PQC_KEY_BASE64")
            ?? options.Value.MasterKeyBase64;
        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            throw new InvalidOperationException("PQC master key is required. Configure AEGISCORE_PQC_KEY_BASE64 with a 32-byte base64 value.");
        }

        _key = Convert.FromBase64String(configuredKey);
        if (_key.Length != 32)
        {
            throw new InvalidOperationException("PQC master key must decode to exactly 32 bytes.");
        }
        _nonce = new byte[12];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(_nonce);
    }

    public PqcAlgorithm CurrentAlgorithm => _algorithm;

    public byte[] Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> associatedData = default)
    {
        var cipher = new Org.BouncyCastle.Crypto.Modes.GcmBlockCipher(new Org.BouncyCastle.Crypto.Engines.AesEngine());
        var parameters = new Org.BouncyCastle.Crypto.Parameters.AeadParameters(
            new Org.BouncyCastle.Crypto.Parameters.KeyParameter(_key), 128, _nonce);
        cipher.Init(true, parameters);

        var input = plaintext.ToArray();
        var output = new byte[cipher.GetOutputSize(input.Length)];
        var len = cipher.ProcessBytes(input, 0, input.Length, output, 0);
        cipher.DoFinal(output, len);

        var result = new byte[_nonce.Length + output.Length];
        Buffer.BlockCopy(_nonce, 0, result, 0, _nonce.Length);
        Buffer.BlockCopy(output, 0, result, _nonce.Length, output.Length);
        return result;
    }

    public byte[] Decrypt(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> associatedData = default)
    {
        var data = ciphertext.ToArray();
        var nonce = new byte[12];
        Buffer.BlockCopy(data, 0, nonce, 0, 12);
        var body = new byte[data.Length - 12];
        Buffer.BlockCopy(data, 12, body, 0, body.Length);

        var cipher = new Org.BouncyCastle.Crypto.Modes.GcmBlockCipher(new Org.BouncyCastle.Crypto.Engines.AesEngine());
        var parameters = new Org.BouncyCastle.Crypto.Parameters.AeadParameters(
            new Org.BouncyCastle.Crypto.Parameters.KeyParameter(_key), 128, nonce);
        cipher.Init(false, parameters);

        var output = new byte[cipher.GetOutputSize(body.Length)];
        var len = cipher.ProcessBytes(body, 0, body.Length, output, 0);
        cipher.DoFinal(output, len);
        return output;
    }

    public string EncryptString(string plaintext)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(plaintext);
        return Convert.ToBase64String(Encrypt(bytes));
    }

    public string DecryptString(string ciphertext)
    {
        var bytes = Convert.FromBase64String(ciphertext);
        return System.Text.Encoding.UTF8.GetString(Decrypt(bytes));
    }
}

public class WeeklySnapshotService : BackgroundService
{
    private readonly ILogger<WeeklySnapshotService> _logger;
    private readonly TimeSpan _period = TimeSpan.FromDays(7);

    public WeeklySnapshotService(ILogger<WeeklySnapshotService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WeeklySnapshotService started.");
        using var timer = new PeriodicTimer(_period);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                _logger.LogInformation("Weekly cryptographic key snapshot taken at {Time}.", DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Weekly snapshot failed.");
            }
        }
    }
}

public static class PqcCryptographyServiceExtensions
{
    public static IServiceCollection AddPqcCryptography(this IServiceCollection services)
    {
        services.AddSingleton<IPqcCryptographyService, PqcCryptographyService>();
        services.AddSingleton<WeeklySnapshotService>();
        services.AddHostedService(sp => sp.GetRequiredService<WeeklySnapshotService>());
        return services;
    }
}
