using AegisCore.Application.Compliance;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure.Compliance;
using AegisCore.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AegisCore.Tests;

/// <summary>
/// The three decisions a supplier cannot make for an institution — who its data protection contact
/// is, where its data lives, how long records are kept — are settings the administrator owns and
/// can change at any time. These tests hold that: the product ships starting values, never answers,
/// and a value changed today is the one applied tonight.
/// </summary>
public sealed class ComplianceSettingsTests
{
    private static ComplianceSettingsService Service(AegisCore.Infrastructure.AegisDbContext db) =>
        new(db, NullLogger<ComplianceSettingsService>.Instance);

    [Theory, DatabaseProviders]
    public async Task A_fresh_installation_names_nobody_and_says_so(bool postgres)
    {
        var tenant = Guid.NewGuid();
        await using var database = await TestDatabase.CreateAsync(postgres);
        await database.EnsureSchemaAsync(tenant);
        await using var db = database.CreateContext(tenant);
        var service = Service(db);

        var settings = await service.GetAsync(tenant);

        // The product must not invent an accountable person or a location.
        Assert.Null(settings.Contact.Name);
        Assert.Null(settings.Contact.Email);
        Assert.False(settings.Contact.IsComplete);
        Assert.Null(settings.Residency.DatabaseLocation);
        Assert.False(settings.Residency.ApprovedForProduction);

        // Retention has usable starting values so the worker never deletes nothing or everything.
        Assert.Equal(6, settings.Retention.AuditRetentionYears);
        Assert.Equal(365, settings.Retention.LoginAuditRetentionDays);
        Assert.Equal(10, settings.Retention.FinancialRetentionYears);

        var gaps = service.Gaps(settings);
        Assert.Contains(gaps, gap => gap.Setting == "Data protection contact");
        Assert.Contains(gaps, gap => gap.Setting == "Data residency");
    }

    [Theory, DatabaseProviders]
    public async Task An_administrator_sets_them_changes_them_again_and_each_change_sticks(bool postgres)
    {
        var tenant = Guid.NewGuid();
        await using var database = await TestDatabase.CreateAsync(postgres);
        await database.EnsureSchemaAsync(tenant);
        await using var db = database.CreateContext(tenant);
        var service = Service(db);
        var actor = Guid.NewGuid();

        var saved = await service.SaveAsync(tenant, new ComplianceSettingsUpdate(
            Contact: new DataProtectionContact("Dr Sana Malik", "Registrar", "privacy@university.edu.pk", "+92 42 111 1111"),
            Residency: new DataResidency("On-premise", "Lahore, Pakistan", "Lahore, Pakistan", "Islamabad, Pakistan", null, true),
            Retention: new RetentionSettingsUpdate(LoginAuditRetentionDays: 90, WelfareRetentionYears: 10)), actor);

        Assert.Equal("Dr Sana Malik", saved.Contact.Name);
        Assert.True(saved.Contact.IsComplete);
        Assert.Equal("Lahore, Pakistan", saved.Residency.DatabaseLocation);
        Assert.True(saved.Residency.ApprovedForProduction);
        Assert.Equal(90, saved.Retention.LoginAuditRetentionDays);
        Assert.Equal(10, saved.Retention.WelfareRetentionYears);
        Assert.Equal(6, saved.Retention.AuditRetentionYears);   // untouched period keeps its value
        Assert.Empty(service.Gaps(saved));

        // The institution revises one period later; nothing else moves.
        var revised = await service.SaveAsync(tenant, new ComplianceSettingsUpdate(
            Retention: new RetentionSettingsUpdate(LoginAuditRetentionDays: 180)), actor);
        Assert.Equal(180, revised.Retention.LoginAuditRetentionDays);
        Assert.Equal("Dr Sana Malik", revised.Contact.Name);
        Assert.Equal("Lahore, Pakistan", revised.Residency.DatabaseLocation);

        // And a fresh read of the store agrees — the values are persisted, not cached in the instance.
        Assert.Equal(180, (await Service(db).GetAsync(tenant)).Retention.LoginAuditRetentionDays);
    }

    [Theory, DatabaseProviders]
    public async Task A_period_cannot_be_set_to_something_that_would_erase_a_register(bool postgres)
    {
        var tenant = Guid.NewGuid();
        await using var database = await TestDatabase.CreateAsync(postgres);
        await database.EnsureSchemaAsync(tenant);
        await using var db = database.CreateContext(tenant);
        var service = Service(db);

        var saved = await service.SaveAsync(tenant, new ComplianceSettingsUpdate(
            Retention: new RetentionSettingsUpdate(
                AuditRetentionYears: 0, LoginAuditRetentionDays: 1, FinancialRetentionYears: 9999)), Guid.NewGuid());

        Assert.Equal(1, saved.Retention.AuditRetentionYears);        // 0 would mean "delete now"
        Assert.Equal(30, saved.Retention.LoginAuditRetentionDays);   // floor
        Assert.Equal(100, saved.Retention.FinancialRetentionYears);  // ceiling
    }

    [Theory, DatabaseProviders]
    public async Task The_retention_worker_applies_the_period_the_administrator_set(bool postgres)
    {
        var tenant = Guid.NewGuid();
        await using var database = await TestDatabase.CreateAsync(postgres);
        await database.EnsureSchemaAsync(tenant);
        await using var db = database.CreateContext(tenant);
        var settingsService = Service(db);
        var now = DateTime.UtcNow;

        db.LoginAuditRecords.AddRange(
            new LoginAuditRecord { AuditId = Guid.NewGuid(), TenantId = tenant, UserId = Guid.NewGuid(), UserEmail = "d100@x", UserRole = "S", EventType = LoginEvent.Login, Provider = LoginProvider.Local, IpAddress = "1.1.1.1", IsSuccessful = true, OccurredAt = now.AddDays(-100) },
            new LoginAuditRecord { AuditId = Guid.NewGuid(), TenantId = tenant, UserId = Guid.NewGuid(), UserEmail = "d20@x", UserRole = "S", EventType = LoginEvent.Login, Provider = LoginProvider.Local, IpAddress = "1.1.1.2", IsSuccessful = true, OccurredAt = now.AddDays(-20) });
        await db.SaveChangesAsync();

        // Default is 365 days: nothing is old enough yet.
        var privacy = new PrivacyComplianceService(db, null!, null!, new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build(), NullLogger<PrivacyComplianceService>.Instance);
        await privacy.ApplyRetentionAsync(tenant, now, (await settingsService.GetAsync(tenant)).Retention);
        Assert.Equal(2, await db.LoginAuditRecords.IgnoreQueryFilters().CountAsync());

        // The institution decides 30 days. The next run honours it.
        await settingsService.SaveAsync(tenant, new ComplianceSettingsUpdate(Retention: new RetentionSettingsUpdate(LoginAuditRetentionDays: 30)), Guid.NewGuid());
        db.ChangeTracker.Clear();
        await privacy.ApplyRetentionAsync(tenant, now, (await settingsService.GetAsync(tenant)).Retention);

        var remaining = await db.LoginAuditRecords.IgnoreQueryFilters().Select(r => r.UserEmail).ToListAsync();
        Assert.Equal(["d20@x"], remaining);
    }
}
