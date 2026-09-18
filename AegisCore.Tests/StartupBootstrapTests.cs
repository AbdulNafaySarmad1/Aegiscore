using AegisCore.Infrastructure.Deployment;
using AegisCore.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Cryptography.X509Certificates;

namespace AegisCore.Tests;

/// <summary>
/// A container started against empty volumes provisions its own certificate and keys, initialises
/// an empty database, applies pending migrations when allowed, and otherwise refuses to start with
/// a sentence that says what to do. Nothing is regenerated on a restart.
/// </summary>
public sealed class StartupBootstrapTests
{
    private static IConfiguration Config(params (string Key, string? Value)[] values)
        => new ConfigurationBuilder().AddInMemoryCollection(values.ToDictionary(v => v.Key, v => v.Value)).Build();

    [Fact]
    public void Certificate_Is_Generated_Once_And_Opens_With_The_Password()
    {
        var directory = Path.Combine(Path.GetTempPath(), "aegis-boot-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "data-protection.pfx");
        var configuration = Config(("DataProtection:CertificatePath", path), ("DataProtection:CertificatePassword", "correct horse battery staple"));
        try
        {
            Assert.True(StartupBootstrap.EnsureDataProtectionCertificate(configuration, NullLogger.Instance));
            using var certificate = X509CertificateLoader.LoadPkcs12FromFile(path, "correct horse battery staple");
            Assert.Contains("AegisCore Data Protection", certificate.Subject);
            Assert.True(certificate.HasPrivateKey);
            var bytes = File.ReadAllBytes(path);
            Assert.False(StartupBootstrap.EnsureDataProtectionCertificate(configuration, NullLogger.Instance)); // second start: untouched
            Assert.Equal(bytes, File.ReadAllBytes(path));
        }
        finally { Directory.Delete(directory, recursive: true); }

        var missingPassword = Config(("DataProtection:CertificatePath", Path.Combine(directory, "x.pfx")));
        var ex = Assert.Throws<StartupBootstrap.BootstrapException>(() => StartupBootstrap.EnsureDataProtectionCertificate(missingPassword, NullLogger.Instance));
        Assert.Contains("DATA_PROTECTION_CERTIFICATE_PASSWORD", ex.Message);
    }

    [PilotPostgresFact]
    public async Task Empty_Database_Is_Initialised_And_A_Current_One_Is_Verified()
    {
        await using var database = await TestDatabase.CreateAsync(postgres: true);
        await using var db = database.CreateContext(Guid.NewGuid());
        var initialised = await StartupBootstrap.SchemaPolicyAsync(db, Config(("Database:AutoMigrate", "false")), NullLogger.Instance, TimeSpan.FromSeconds(30));
        Assert.Equal("initialised", initialised.Action); // AutoMigrate=false does not stop an empty database from being set up
        var verified = await StartupBootstrap.SchemaPolicyAsync(db, Config(("Database:AutoMigrate", "false")), NullLogger.Instance, TimeSpan.FromSeconds(30));
        Assert.Equal("verified", verified.Action);
        Assert.Contains("Schema is current", verified.Detail);
    }

    [PilotPostgresFact]
    public async Task Pending_Migrations_Are_Applied_When_Allowed_And_Refused_By_Name_When_Not()
    {
        await using var database = await TestDatabase.CreateAsync(postgres: true);
        await using var db = database.CreateContext(Guid.NewGuid());
        await db.GetService<IMigrator>().MigrateAsync("20260908002929_AddAdmissionMigration");

        var refused = await Assert.ThrowsAsync<StartupBootstrap.BootstrapException>(() =>
            StartupBootstrap.SchemaPolicyAsync(db, Config(("Database:AutoMigrate", "false")), NullLogger.Instance, TimeSpan.FromSeconds(30)));
        Assert.Contains("migration(s) behind", refused.Message);
        Assert.Contains("AddMissingModuleTables", refused.Message);
        Assert.Contains("dotnet AegisCore.API.dll migrate", refused.Message);

        var migrated = await StartupBootstrap.SchemaPolicyAsync(db, Config(("Database:AutoMigrate", "true")), NullLogger.Instance, TimeSpan.FromSeconds(30));
        Assert.Equal("migrated", migrated.Action);
        Assert.Contains("AddMissingModuleTables", migrated.Detail);
    }

    [PilotPostgresFact]
    public async Task Drift_No_Migration_Would_Fix_Is_Refused_With_The_Column_Named()
    {
        await using var database = await TestDatabase.CreateAsync(postgres: true);
        await using var db = database.CreateContext(Guid.NewGuid());
        await database.MigrateAsync(db);
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"Students\" DROP COLUMN \"RollNumber\"");
        var refused = await Assert.ThrowsAsync<StartupBootstrap.BootstrapException>(() =>
            StartupBootstrap.SchemaPolicyAsync(db, Config(), NullLogger.Instance, TimeSpan.FromSeconds(30)));
        Assert.Contains("Students.RollNumber", refused.Message);
        Assert.Contains("no migration would fix it", refused.Message);
    }

    [Fact]
    public async Task Unreachable_Database_Is_Refused_With_The_Settings_To_Check()
    {
        var options = new DbContextOptionsBuilder<AegisCore.Infrastructure.AegisDbContext>().UseNpgsql("Host=127.0.0.1;Port=1;Database=x;Username=x;Password=x;Timeout=1").Options;
        await using var db = new AegisCore.Infrastructure.AegisDbContext(options);
        var refused = await Assert.ThrowsAsync<StartupBootstrap.BootstrapException>(() =>
            StartupBootstrap.SchemaPolicyAsync(db, Config(), NullLogger.Instance, TimeSpan.FromSeconds(2)));
        Assert.Contains("did not answer within the startup wait", refused.Message);
        Assert.Contains("AEGISCORE_DB_CONNECTION", refused.Message);
    }
}

public sealed class GeneratedSecretsTests
{
    [Fact]
    public void Missing_Secrets_Are_Generated_Once_And_Supplied_Values_Are_Never_Overwritten()
    {
        var directory = Path.Combine(Path.GetTempPath(), "aegis-secrets-" + Guid.NewGuid().ToString("N"));
        try
        {
            var first = new ConfigurationManager();
            first.AddInMemoryCollection(new Dictionary<string, string?> { ["APP_SECRET"] = "supplied-by-operator" });
            var generated = GeneratedSecrets.Ensure(first, first, directory, NullLogger.Instance);
            Assert.Contains("AEGISCORE_SETUP_TOKEN", generated);
            Assert.Contains("TENANT_ID", generated);
            Assert.DoesNotContain("APP_SECRET", generated); // supplied
            Assert.Equal("supplied-by-operator", first["APP_SECRET"]);
            Assert.True(Guid.TryParse(first["TENANT_ID"], out _));
            Assert.True(AegisCore.API.Middleware.SetupBootstrapToken.IsStrong(first["AEGISCORE_SETUP_TOKEN"]!));
            Assert.Equal(32, Convert.FromBase64String(first["AEGISCORE_PQC_KEY_BASE64"]!).Length);
            Assert.Equal(first["Governance:FeedbackEncryptionKeyBase64"], Environment.GetEnvironmentVariable("Governance__FeedbackEncryptionKeyBase64"));
            var file = File.ReadAllText(Path.Combine(directory, GeneratedSecrets.FileName));
            Assert.DoesNotContain("supplied-by-operator", file);

            // A restart reads the same values back and generates nothing.
            var second = new ConfigurationManager();
            second.AddInMemoryCollection(new Dictionary<string, string?> { ["APP_SECRET"] = "supplied-by-operator" });
            Assert.Empty(GeneratedSecrets.Ensure(second, second, directory, NullLogger.Instance));
            Assert.Equal(first["TENANT_ID"], second["TENANT_ID"]);
            Assert.Equal(first["AEGISCORE_SETUP_TOKEN"], second["AEGISCORE_SETUP_TOKEN"]);
            Assert.Equal(first["DataProtection:CertificatePassword"], second["DataProtection:CertificatePassword"]);
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Fact]
    public void Config_Volume_Lock_Is_Exclusive_And_Released()
    {
        var directory = Path.Combine(Path.GetTempPath(), "aegis-lock-" + Guid.NewGuid().ToString("N"));
        try
        {
            using (ConfigVolumeLock.Acquire(directory))
                Assert.Throws<StartupBootstrap.BootstrapException>(() => ConfigVolumeLock.Acquire(directory, TimeSpan.FromMilliseconds(600)));
            using (ConfigVolumeLock.Acquire(directory, TimeSpan.FromSeconds(1))) { }
        }
        finally { Directory.Delete(directory, recursive: true); }
    }
}
