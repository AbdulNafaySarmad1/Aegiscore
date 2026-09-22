using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure.Security;
using AegisCore.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AegisCore.Tests;

/// <summary>
/// Compliance control AC-54: anomalies reach a person. Detection already existed but its findings
/// were discarded, so these tests hold the alert on the path and keep it from becoming noise.
/// </summary>
public sealed class SecurityAlertTests
{
    private static SecurityAlertService Service(AegisCore.Infrastructure.AegisDbContext db,
        AegisCore.Application.Workflow.INotificationService? notifications = null, string? alertEmail = null, int dedupeMinutes = 60)
    {
        var settings = new Dictionary<string, string?> { ["Security:AlertDeduplicationMinutes"] = dedupeMinutes.ToString() };
        if (alertEmail is not null) settings["Security:AlertEmail"] = alertEmail;
        return new SecurityAlertService(db, new ConfigurationBuilder().AddInMemoryCollection(settings).Build(),
            NullLogger<SecurityAlertService>.Instance, notifications);
    }

    [Theory, DatabaseProviders]
    public async Task An_alert_is_recorded_once_per_window_and_high_severity_is_emailed(bool postgres)
    {
        var tenant = Guid.NewGuid();
        await using var database = await TestDatabase.CreateAsync(postgres);
        await database.EnsureSchemaAsync(tenant);
        await using var db = database.CreateContext(tenant);
        var notifications = new Mock<AegisCore.Application.Workflow.INotificationService>();
        var service = Service(db, notifications.Object, "security@example.edu");
        var subject = Guid.NewGuid();

        var first = await service.RaiseAsync(tenant, "BruteForce", "High", "7 failed sign-ins in 10 minutes.", subject, 7);
        var duplicate = await service.RaiseAsync(tenant, "BruteForce", "High", "9 failed sign-ins in 10 minutes.", subject, 9);
        var otherSubject = await service.RaiseAsync(tenant, "BruteForce", "High", "6 failed sign-ins.", Guid.NewGuid(), 6);
        var otherType = await service.RaiseAsync(tenant, "NewCountryLogin", "Medium", "Sign-in from a new country.", subject, 1);

        Assert.NotNull(first);
        Assert.Null(duplicate);
        Assert.NotNull(otherSubject);
        Assert.NotNull(otherType);
        Assert.Equal(3, await db.SecurityAlerts.IgnoreQueryFilters().CountAsync());

        // Two High alerts were emailed; the Medium one was not.
        notifications.Verify(n => n.SendEmailAsync("security@example.edu", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Theory, DatabaseProviders]
    public async Task A_failed_sign_in_run_raises_an_alert_through_the_login_audit(bool postgres)
    {
        var tenant = Guid.NewGuid();
        await using var database = await TestDatabase.CreateAsync(postgres);
        await database.EnsureSchemaAsync(tenant);
        await using var db = database.CreateContext(tenant);
        var userId = Guid.NewGuid();
        var repository = new AegisCore.Infrastructure.Repositories.LoginAuditRecordRepository(db);
        var geoIp = new Mock<IGeoIpService>();
        geoIp.Setup(g => g.LookupAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new GeoIpResult("Pakistan", "Lahore"));
        var service = new LoginAuditService(repository, geoIp.Object, NullLogger<LoginAuditService>.Instance, Service(db));

        LoginAuditRecord Failure() => new()
        {
            AuditId = Guid.NewGuid(), TenantId = tenant, UserId = userId, UserEmail = "target@example.edu", UserRole = "Admin",
            EventType = LoginEvent.LoginFailed, Provider = LoginProvider.Local, IpAddress = "198.51.100.4",
            IpCountry = "Pakistan", IsSuccessful = false, OccurredAt = DateTime.UtcNow,
        };

        for (var attempt = 0; attempt < 6; attempt++) await service.RecordAsync(Failure());

        var alert = await db.SecurityAlerts.IgnoreQueryFilters().SingleAsync();
        Assert.Equal("BruteForce", alert.Type);
        Assert.Equal("High", alert.Severity);
        Assert.Equal(userId, alert.SubjectUserId);
        Assert.Null(alert.AcknowledgedAt);
    }
}
