using AegisCore.Application.Finance;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Cloud.R2;
using AegisCore.Infrastructure.Finance;
using AegisCore.Infrastructure.Security.Pqc;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Tests;

public sealed class FinanceAuditComplianceTests
{
    [Fact]
    public async Task ControlLibrary_Has_AtLeast_Fifty_Unique_Executable_Controls()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        await using var db = await DatabaseAsync(connection, accessor);
        var service = Service(db, accessor);

        var controls = service.GetLibrary();

        Assert.True(controls.Count >= 50);
        Assert.Equal(controls.Count, controls.Select(x => x.Code).Distinct(StringComparer.Ordinal).Count());
        Assert.All(controls, control => Assert.Contains(control.Operator, new[] { "eq", "lte", "gte" }));
    }

    [Fact]
    public async Task AuditorToken_Is_Hashed_Scoped_And_Revocable()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        await using var db = await DatabaseAsync(connection, accessor);
        var service = Service(db, accessor);
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();

        var created = await service.CreateEngagementAsync(tenantId, actorId,
            new CreateAuditorEngagementCommand("External Auditor", "auditor@example.test", DateTime.UtcNow.AddDays(7), ["controls:read", "ledger:read"]), CancellationToken.None);

        Assert.NotEqual(created.Token, created.Engagement.TokenHash);
        Assert.DoesNotContain(created.Token, created.Engagement.ScopesJson, StringComparison.Ordinal);
        Assert.Null(await service.AuthenticateAsync("wrong-token-value", "controls:read", CancellationToken.None));
        Assert.Null(await service.AuthenticateAsync(created.Token, "evidence:read", CancellationToken.None));
        Assert.Equal(created.Engagement.Id, (await service.AuthenticateAsync(created.Token, "ledger:read", CancellationToken.None))?.Id);
        Assert.Equal(tenantId, accessor.HttpContext.Items["TenantId"]);

        await service.RevokeEngagementAsync(tenantId, created.Engagement.Id, actorId, CancellationToken.None);

        Assert.Null(await service.AuthenticateAsync(created.Token, "controls:read", CancellationToken.None));
    }

    [Theory]
    [InlineData("bad-email", 7)]
    [InlineData("auditor@example.test", 91)]
    public async Task AuditorEngagement_Rejects_Invalid_Trust_Window(string email, int days)
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        await using var db = await DatabaseAsync(connection, accessor);
        var service = Service(db, accessor);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateEngagementAsync(Guid.NewGuid(), Guid.NewGuid(),
            new CreateAuditorEngagementCommand("External Auditor", email, DateTime.UtcNow.AddDays(days), ["controls:read"]), CancellationToken.None));
    }

    private static FinanceContinuousControlService Service(AegisDbContext db, IHttpContextAccessor accessor)
        => new(db, Mock.Of<IPqcSignatureService>(), Mock.Of<IR2StorageService>(), accessor);

    private static async Task<AegisDbContext> DatabaseAsync(SqliteConnection connection, IHttpContextAccessor accessor)
    {
        var db = new AegisDbContext(new DbContextOptionsBuilder<AegisDbContext>().UseSqlite(connection).Options, accessor);
        await db.Database.EnsureCreatedAsync();
        return db;
    }
}
