using System.Text.Json;
using AegisCore.Application.Common.Interfaces;
using AegisCore.Application.Onboarding;
using AegisCore.Contracts.Abstractions.Cloud;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Onboarding;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace AegisCore.Tests;

public sealed class WizardStatePersistenceTests
{
    [Fact]
    public async Task ResumeWizardAsync_RestoresStepsAndTenantAcrossFreshContainer()
    {
        const string token = "restart-test-bootstrap-token-with-at-least-32-bytes";
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var secrets = new TestSecretService();
        var protection = new EphemeralDataProtectionProvider();
        Guid tenantId;
        string savedStepData;

        await using (var firstContainer = BuildContainer(connection, secrets, protection))
        {
            await using var scope = firstContainer.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AegisDbContext>();
            await db.Database.EnsureCreatedAsync();
            var states = scope.ServiceProvider.GetRequiredService<WizardStateService>();
            var sessions = scope.ServiceProvider.GetRequiredService<WizardSessionService>();
            var initial = await states.GetOrCreateStateAsync(token, "192.0.2.10");

            var step1 = JsonDocument.Parse("""{"name":"Test University","shortName":"TEST"}""").RootElement;
            var afterStep1 = await sessions.SaveAsync(token, initial.SessionId, 1, step1, CancellationToken.None);
            tenantId = afterStep1.TenantId;
            Assert.NotEqual(Guid.Empty, tenantId);

            var step2 = JsonDocument.Parse("""{"host":"db.internal","database":"aegiscore"}""").RootElement;
            await sessions.SaveAsync(token, initial.SessionId, 2, step2, CancellationToken.None);
            var step3 = JsonDocument.Parse("""{"provider":"entra","tenantId":"directory-tenant"}""").RootElement;
            var state = await sessions.SaveAsync(token, initial.SessionId, 3, step3, CancellationToken.None);
            state.UniversityFullName = "Test University";
            state.DbHost = "db.internal";
            state.DbName = "aegiscore";
            state.DbUsername = "installer";
            state.DbPassword = "database-secret";
            state.RedisHost = "redis.internal";
            state.SmtpHost = "smtp.internal";
            state.SmtpPassword = "smtp-secret";
            await states.SaveState(token, state);
            savedStepData = state.StepData;

            var persisted = await db.WizardSessions.IgnoreQueryFilters().SingleAsync();
            Assert.Equal("{}", persisted.StepData);
            Assert.Null(persisted.LicenseKey);
            Assert.NotEqual(token, persisted.BootstrapTokenHash);
            Assert.DoesNotContain("database-secret", persisted.Payload, StringComparison.Ordinal);
            Assert.DoesNotContain("smtp-secret", persisted.Payload, StringComparison.Ordinal);
        }

        await using (var restartedContainer = BuildContainer(connection, secrets, protection))
        {
            await using var scope = restartedContainer.CreateAsyncScope();
            var states = scope.ServiceProvider.GetRequiredService<WizardStateService>();
            var restored = await states.ResumeWizardAsync(token);

            Assert.Equal(tenantId, restored.TenantId);
            Assert.Equal(4, restored.CurrentStep);
            Assert.Equal(savedStepData, restored.StepData);
            Assert.Equal("Test University", restored.UniversityFullName);
            Assert.Equal("db.internal", restored.DbHost);
            Assert.Equal("aegiscore", restored.DbName);
            Assert.Equal("installer", restored.DbUsername);
            Assert.Equal("database-secret", restored.DbPassword);
            Assert.Equal("redis.internal", restored.RedisHost);
            Assert.Equal("smtp.internal", restored.SmtpHost);
            Assert.Equal("smtp-secret", restored.SmtpPassword);

            var initializedAgain = await states.InitializeTenantAtStepOneAsync(token);
            Assert.Equal(tenantId, initializedAgain.TenantId);
        }
    }

    [Fact]
    public async Task ResumeWizardAsync_ReturnsDomainSpecificMissingAndCompletedFailures()
    {
        const string token = "failure-test-bootstrap-token-with-at-least-32-bytes";
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var container = BuildContainer(connection, new TestSecretService(), new EphemeralDataProtectionProvider());
        await using var scope = container.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AegisDbContext>();
        await db.Database.EnsureCreatedAsync();
        var states = scope.ServiceProvider.GetRequiredService<WizardStateService>();

        var missing = await Assert.ThrowsAsync<WizardSessionNotFoundException>(
            () => states.ResumeWizardAsync("unknown-bootstrap-token-with-at-least-32-bytes"));
        Assert.Equal("Setup session not found. Start a new installation.", missing.Message);

        var completed = await states.GetOrCreateStateAsync(token);
        completed.CompletedAt = DateTimeOffset.UtcNow;
        await states.SaveState(token, completed);
        var conflict = await Assert.ThrowsAsync<WizardSessionCompletedException>(() => states.ResumeWizardAsync(token));
        Assert.Equal("This installation is already complete.", conflict.Message);
    }

    private static ServiceProvider BuildContainer(
        SqliteConnection connection,
        ISecretService secrets,
        IDataProtectionProvider protection)
    {
        var services = new ServiceCollection();
        services.AddDbContext<AegisDbContext>(options => options.UseSqlite(connection));
        services.AddSingleton(secrets);
        services.AddSingleton(protection);
        services.AddSingleton(Mock.Of<IPasswordHashingService>());
        services.AddScoped<IWizardStateStore, EfWizardStateStore>();
        services.AddScoped<WizardStateService>();
        services.AddScoped<WizardSessionService>();
        return services.BuildServiceProvider(validateScopes: true);
    }
}
