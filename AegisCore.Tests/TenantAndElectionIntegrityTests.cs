using System.Security.Cryptography;
using System.Text;
using AegisCore.API.Middleware;
using AegisCore.Application.CampusOperations;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.CampusOperations;
using AegisCore.Infrastructure.StudentLifecycle;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AegisCore.Tests;

public sealed class TenantAndElectionIntegrityTests
{
    [Theory]
    [InlineData(typeof(UnauthorizedAccessException), StatusCodes.Status403Forbidden)]
    [InlineData(typeof(ArgumentException), StatusCodes.Status400BadRequest)]
    public async Task GlobalExceptionHandler_MapsSecurityAndValidationFailures(Type exceptionType, int expectedStatus)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType)!;
        var middleware = new GlobalExceptionHandlerMiddleware(_ => throw exception, NullLogger<GlobalExceptionHandlerMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(expectedStatus, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        Assert.DoesNotContain(exceptionType.Name, await reader.ReadToEndAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Leaderboard_Rejects_MismatchedTenant_AndFiltersRows()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var accessor = CreateTenantAccessor(tenantId);
        await using var db = await CreateDatabaseAsync(connection, accessor);

        db.LeaderboardEntries.AddRange(
            new LeaderboardEntry { Id = Guid.NewGuid(), StudentId = Guid.NewGuid(), SemesterId = Guid.NewGuid(), Scope = "department", Score = 4m, Rank = 1, TenantId = tenantId },
            new LeaderboardEntry { Id = Guid.NewGuid(), StudentId = Guid.NewGuid(), SemesterId = Guid.NewGuid(), Scope = "department", Score = 3.9m, Rank = 1, TenantId = otherTenantId });
        await db.SaveChangesAsync();

        var service = new GamificationService(db, NullLogger<GamificationService>.Instance);
        var leaderboard = await service.GenerateLeaderboardAsync("department", tenantId);

        Assert.Single(leaderboard.Entries);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GenerateLeaderboardAsync("department", otherTenantId));
    }

    [Fact]
    public async Task Ballot_RequiresOneTimeToken_AndPersistsOnlyHash()
    {
        var tenantId = Guid.NewGuid();
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var accessor = CreateTenantAccessor(tenantId);
        await using var db = await CreateDatabaseAsync(connection, accessor);

        var electionId = Guid.NewGuid();
        var positionId = Guid.NewGuid();
        var candidateId = Guid.NewGuid();
        db.Elections.Add(new Election { Id = electionId, Title = "Council election", TenantId = tenantId });
        db.ElectionPositions.Add(new ElectionPosition { Id = positionId, ElectionId = electionId, PositionName = "President", TenantId = tenantId });
        db.ElectionCandidates.Add(new ElectionCandidate { Id = candidateId, ElectionId = electionId, PositionId = positionId, StudentId = Guid.NewGuid(), EligibilityVerified = true, TenantId = tenantId });
        await db.SaveChangesAsync();

        var service = new ElectionService(db, NullLogger<ElectionService>.Instance);
        var issued = await service.IssueVoterTokenAsync(electionId, Guid.NewGuid());
        var persisted = await db.VoterTokens.SingleAsync(t => t.Id == issued.TokenId);

        Assert.NotEqual(issued.TokenHash, persisted.TokenHash);
        Assert.Equal(HashToken(issued.TokenHash), persisted.TokenHash);

        var request = new CastBallotRequest(electionId, [new BallotChoice(positionId, candidateId)], issued.TokenHash);
        await service.CastBallotAsync(request);

        Assert.True(persisted.HasVoted);
        Assert.Single(await db.Ballots.ToListAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CastBallotAsync(request));
    }

    private static HttpContextAccessor CreateTenantAccessor(Guid tenantId)
    {
        var context = new DefaultHttpContext();
        context.Items["TenantId"] = tenantId;
        return new HttpContextAccessor { HttpContext = context };
    }

    private static async Task<AegisDbContext> CreateDatabaseAsync(SqliteConnection connection, IHttpContextAccessor accessor)
    {
        var db = new AegisDbContext(new DbContextOptionsBuilder<AegisDbContext>().UseSqlite(connection).Options, accessor);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
