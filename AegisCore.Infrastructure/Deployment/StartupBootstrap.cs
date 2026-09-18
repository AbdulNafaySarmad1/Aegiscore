using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using AegisCore.Infrastructure.Database;
using AegisCore.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Deployment;

/// <summary>
/// What a container does on its own before it serves a request, so that `docker run` against
/// empty volumes ends in a working system with nothing copied in by hand:
/// <list type="number">
/// <item>the Data Protection certificate (PKCS#12) is generated into the config volume if absent;</item>
/// <item>the RSA token-signing pair is generated into the config volume if absent;</item>
/// <item>the database is waited for, and its schema is brought to — or verified against — the
/// code's expectation by <see cref="SchemaPolicyAsync"/>.</item>
/// </list>
/// Every step either succeeds, is a no-op because the artefact already exists, or throws a
/// <see cref="BootstrapException"/> whose message says what to do. Nothing is regenerated on a
/// restart; nothing boots half-configured in silence.
/// </summary>
public static class StartupBootstrap
{
    public sealed class BootstrapException(string message) : Exception(message);

    /// <summary>Generates the key-ring certificate when a path is configured and no file is there yet. Returns true when one was written.</summary>
    public static bool EnsureDataProtectionCertificate(IConfiguration configuration, ILogger logger)
    {
        var path = configuration["DataProtection:CertificatePath"];
        if (string.IsNullOrWhiteSpace(path) || File.Exists(path)) return false;
        var password = configuration["DataProtection:CertificatePassword"];
        if (string.IsNullOrWhiteSpace(password))
            throw new BootstrapException($"DataProtection:CertificatePath points at '{path}', which does not exist, and DataProtection:CertificatePassword is not set, so the certificate cannot be generated. Set DATA_PROTECTION_CERTIFICATE_PASSWORD (any long random value; it protects the key ring at rest) and start again.");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            using var rsa = RSA.Create(3072);
            var request = new CertificateRequest("CN=AegisCore Data Protection", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DataEncipherment, critical: false));
            using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(20));
            File.WriteAllBytes(path, certificate.Export(X509ContentType.Pkcs12, password));
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            logger.LogInformation("[AEGISCORE] Generated the Data Protection key-ring certificate at {Path}. It is protected by DATA_PROTECTION_CERTIFICATE_PASSWORD; keep both with your backups.", path);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new BootstrapException($"The Data Protection certificate could not be written to '{path}': {ex.Message}. The config volume must be writable by the application user.");
        }
    }

    /// <summary>Generates the token-signing pair when its paths are configured and no files are there yet.</summary>
    public static bool EnsureTokenSigningKeys(IConfiguration configuration, ILogger logger)
    {
        try
        {
            if (!JwtKeyMaterial.EnsureGenerated(configuration)) return false;
            logger.LogInformation("[AEGISCORE] Generated the RSA token-signing key pair at {Path}. It is reused on every restart; keep the config volume with your backups.", configuration[JwtKeyMaterial.PrivateKeyPathKey]);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new BootstrapException($"The token-signing keys could not be written to '{configuration[JwtKeyMaterial.PrivateKeyPathKey]}': {ex.Message}. The config volume must be writable by the application user.");
        }
    }

    public sealed record SchemaOutcome(string Action, string Detail);

    /// <summary>
    /// Waits for the database (containers start together), then applies the schema policy:
    /// an uninitialised database is migrated from zero; pending migrations are applied when
    /// <c>Database:AutoMigrate</c> is true and refused with the list otherwise; drift that no
    /// migration would fix (missing or mistyped columns) is always refused with the names.
    /// </summary>
    public static async Task<SchemaOutcome> SchemaPolicyAsync(DbContext db, IConfiguration configuration, ILogger logger, TimeSpan? wait = null, CancellationToken ct = default)
    {
        var deadline = DateTime.UtcNow + (wait ?? TimeSpan.FromSeconds(int.TryParse(configuration["Database:StartupWaitSeconds"], out var seconds) ? seconds : 90));
        Exception? last = null;
        while (true)
        {
            try { await db.Database.OpenConnectionAsync(ct); await db.Database.CloseConnectionAsync(); break; }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                last = ex;
                if (DateTime.UtcNow >= deadline)
                    throw new BootstrapException($"The database did not answer within the startup wait: {ex.GetBaseException().Message.Split('\n')[0]}. Check that the database server is running and reachable from this container and that AEGISCORE_DB_CONNECTION (or DB_HOST/DB_PORT/DB_NAME/DB_USER/DB_PASS) is correct. Database:StartupWaitSeconds lengthens the wait.");
                logger.LogWarning("[AEGISCORE] Waiting for the database: {Reason}", ex.GetBaseException().Message.Split('\n')[0]);
                await Task.Delay(TimeSpan.FromSeconds(3), ct);
            }
        }

        var applied = (await db.Database.GetAppliedMigrationsAsync(ct)).ToList();
        var pending = (await db.Database.GetPendingMigrationsAsync(ct)).ToList();
        var autoMigrate = configuration.GetValue<bool?>("Database:AutoMigrate") ?? true;

        if (applied.Count == 0)
        {
            logger.LogInformation("[AEGISCORE] The database is empty: applying all {Count} migrations from zero.", pending.Count);
            await db.Database.MigrateAsync(ct);
            return new SchemaOutcome("initialised", $"Empty database initialised with {pending.Count} migrations.");
        }
        if (pending.Count > 0)
        {
            if (!autoMigrate)
                throw new BootstrapException($"The database is {pending.Count} migration(s) behind this version ({string.Join(", ", pending.Take(5))}{(pending.Count > 5 ? ", …" : "")}) and Database__AutoMigrate is false, so nothing was changed. Take a database backup, then either start once with Database__AutoMigrate=true or run `dotnet AegisCore.API.dll migrate`, and start again.");
            logger.LogInformation("[AEGISCORE] Applying {Count} pending migration(s): {Names}", pending.Count, string.Join(", ", pending));
            await db.Database.MigrateAsync(ct);
            var after = await SchemaVerifier.VerifyAsync(db, ct);
            if (!after.IsCurrent) throw new BootstrapException("Migrations were applied but the schema still differs from the code: " + after.Describe() + " This build's migrations do not describe its model; do not serve requests from it.");
            return new SchemaOutcome("migrated", $"{pending.Count} migration(s) applied: {string.Join(", ", pending)}.");
        }
        var verification = await SchemaVerifier.VerifyAsync(db, ct);
        if (!verification.IsCurrent)
            throw new BootstrapException("The database schema differs from what this version expects, and no migration would fix it: " + verification.Describe()
                + " This happens when a column was altered by hand or a migration was edited after it ran. Restore the database from a backup taken with the matching version, or contact the vendor with this message.");
        return new SchemaOutcome("verified", verification.Describe());
    }
}
