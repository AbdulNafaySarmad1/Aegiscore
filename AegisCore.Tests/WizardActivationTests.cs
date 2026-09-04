using AegisCore.Application.Common.Interfaces;
using AegisCore.Application.Onboarding;
using AegisCore.Contracts.Abstractions.Cloud;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Finance;
using AegisCore.Infrastructure.Onboarding;
using AegisCore.Infrastructure.Security.Pqc;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace AegisCore.Tests;

public sealed class WizardActivationTests
{
    [Fact]
    public async Task ModeSelection_CompletesStepZeroAndPersistsOnlyServerSideState()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = new AegisDbContext(new DbContextOptionsBuilder<AegisDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        const string token = "mode-selection-test-token-with-at-least-32-bytes";
        var states = WizardTestState.Create(db);
        var session = await states.GetOrCreateStateAsync(token);
        var service = new WizardSessionService(db, Mock.Of<IPasswordHashingService>(), states);

        var changed = await service.ChangeModeAsync(token, session.SessionId, "live", "AE-LICENSE", CancellationToken.None);

        Assert.Equal(1, changed.CurrentStep);
        Assert.Equal("paid", changed.LicenseMode);
        Assert.Equal("AE-LICENSE", changed.LicenseKey);
        Assert.Contains("\"licenseValidated\":true", changed.StepData, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Activation_RollsBackInvalidConfirmation_ThenCreatesTenantUsersAndPqcRegistry()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var tenantId = Guid.NewGuid();
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        accessor.HttpContext.Items["TenantId"] = tenantId;
        await using var db = new AegisDbContext(new DbContextOptionsBuilder<AegisDbContext>().UseSqlite(connection).Options, accessor);
        await db.Database.EnsureCreatedAsync();
        const string token = "activation-test-token-with-at-least-32-bytes";
        var secrets = new TestSecretService();
        var states = WizardTestState.Create(db, secrets);
        var session = new WizardStateDto
        {
            SessionId = Guid.NewGuid(), TenantId = tenantId, CurrentStep = 8, LicenseMode = "demo", StepData = CompleteStepData
        };
        await states.SaveState(token, session);
        db.WizardAdminCredentials.AddRange(
            Credential(session, tenantId, "admin@aegiscore.test", "Super Admin"),
            Credential(session, tenantId, "finance@aegiscore.test", "Finance Controller"));
        await db.SaveChangesAsync();
        var passwords = new Mock<IPasswordHashingService>();
        var sessions = new WizardSessionService(db, passwords.Object, states);
        var queue = new PqcSigningQueue();
        await queue.StartAsync(CancellationToken.None);
        try
        {
            var signer = new PqcSignatureService(secrets, db, queue);
            var activation = new WizardActivationService(db, sessions, new WizardProvisioningService(db, signer, new AccountingStandardsEngine(db)));

            await Assert.ThrowsAsync<InvalidOperationException>(() => activation.ActivateAsync(token, session.SessionId, "attacker@invalid.test", CancellationToken.None));
            Assert.False(await db.Tenants.AnyAsync());
            Assert.Null((await db.WizardSessions.IgnoreQueryFilters().SingleAsync()).CompletedAt);

            await activation.ActivateAsync(token, session.SessionId, "admin@aegiscore.test", CancellationToken.None);

            var tenant = await db.Tenants.SingleAsync();
            Assert.Equal("AEGIS", tenant.ShortName);
            Assert.True(tenant.IsActive);
            Assert.Equal(2, await db.Users.IgnoreQueryFilters().CountAsync(x => x.TenantId == tenantId));
            Assert.Contains(await db.FinancePqcKeys.IgnoreQueryFilters().ToListAsync(), key => key.TenantId == tenantId && key.Purpose == "voucher-approval" && key.IsActive);
            Assert.NotNull((await db.WizardSessions.IgnoreQueryFilters().SingleAsync()).CompletedAt);
        }
        finally { await queue.StopAsync(CancellationToken.None); }
    }

    private static WizardAdminCredential Credential(WizardStateDto session, Guid tenantId, string email, string role)
        => new() { Id = Guid.NewGuid(), SessionId = session.SessionId, TenantId = tenantId, FullName = role, Email = email, RoleName = role, PasswordHash = "argon2id-test-hash" };

    private const string CompleteStepData = """
        {
          "step0":{"mode":"demo"},
          "step1":{"name":"Aegis University","shortName":"AEGIS","street":"1 University Road","city":"Karachi","country":"Pakistan","timezone":"Asia/Karachi","language":"English","contactName":"Admin User","contactEmail":"admin@aegiscore.test"},
          "step2":{"host":"database","databaseName":"aegiscore","username":"installer","connectionTested":true},
          "step3":{"provider":"local"},
          "step4":{"roles":[{"name":"Admin","permissions":["System.Admin"]}]},
          "step5":{"provider":"ollama","model":"mistral"},
          "step6":{"skipImport":true},
          "step7":{"confirmed":true,"users":[{"name":"Admin User","email":"admin@aegiscore.test","role":"Super Admin"},{"name":"Finance User","email":"finance@aegiscore.test","role":"Finance Controller"}]}
        }
        """;

}
