using System.Text.Json;
using AegisCore.Application.Governance;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure.Governance;
using AegisCore.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AegisCore.Tests;

/// <summary>
/// The readiness meter must score from recorded evidence, persist what it scored, and refuse to
/// invent a number. Each test runs on SQLite and, when the pilot server is configured, on PostgreSQL.
/// </summary>
public sealed class ReadinessMeterTests
{
    private static readonly Guid Tenant = Guid.NewGuid();

    [Theory, DatabaseProviders]
    public async Task Score_Is_Weighted_From_Evidence_And_Persists(bool postgres)
    {
        await using var database = await TestDatabase.CreateAsync(postgres);
        await database.EnsureSchemaAsync(Tenant);
        var framework = await SeedFrameworkAsync(database);

        Guid scoreId;
        await using (var db = database.CreateContext(Tenant))
        {
            var score = await new ReadinessMeterService(db).ComputeScoreAsync(framework, CancellationToken.None);
            scoreId = score.Id;
            // Weights: A1 (2.0, current evidence) = 2.0; A2 (1.0, only expired evidence) = 0.5; B1 (1.0, no evidence) = 0 → 2.5 / 4.0
            Assert.Equal(62.5, score.OverallScore);
            Assert.Equal(ReadinessStatus.Satisfactory, score.Status);
            Assert.Equal(0, score.ComplianceVelocity);
        }

        // Read back through a fresh context: the row must be in the database, not just in the tracker.
        await using (var db = database.CreateContext(Tenant))
        {
            var saved = await db.ReadinessScores.AsNoTracking().SingleAsync(s => s.Id == scoreId);
            Assert.Equal(62.5, saved.OverallScore);
            var criteria = JsonSerializer.Deserialize<JsonElement>(saved.CriterionScoresJson);
            Assert.Equal(3, criteria.GetArrayLength());
            var byCode = criteria.EnumerateArray().ToDictionary(c => c.GetProperty("code").GetString()!);
            Assert.Equal(ReadinessScoring.FullyMet, byCode["A1"].GetProperty("status").GetString());
            Assert.Equal(ReadinessScoring.PartiallyMet, byCode["A2"].GetProperty("status").GetString());
            Assert.Equal(ReadinessScoring.NotMet, byCode["B1"].GetProperty("status").GetString());
            Assert.Equal(2.0m, byCode["A1"].GetProperty("score").GetDecimal());
            Assert.Equal(2.0m, byCode["A1"].GetProperty("maxScore").GetDecimal());

            var categories = JsonSerializer.Deserialize<Dictionary<string, decimal>>(saved.CategoryScoresJson)!;
            Assert.Equal(83.33m, categories["Governance"]);
            Assert.Equal(0m, categories["Faculty"]);

            var gaps = await db.ReadinessGaps.AsNoTracking().Where(g => g.ScoreId == scoreId).OrderBy(g => g.PriorityRank).ToListAsync();
            Assert.Equal(new[] { "B1", "A2" }, gaps.Select(g => g.CriterionId).ToArray());
            Assert.Equal(25.0, gaps[0].ImpactOnScore);
            Assert.Equal(12.5, gaps[1].ImpactOnScore);
            Assert.Equal(GapEffort.QuickWin, gaps[1].Effort);

            var alerts = await db.ReadinessAlerts.AsNoTracking().Where(a => a.ScoreId == scoreId).ToListAsync();
            Assert.Contains(alerts, a => a.Category == "WARNING" && a.Title.Contains("expired", StringComparison.Ordinal));
            Assert.Contains(alerts, a => a.Category == "INFO" && a.Title.StartsWith("1 of 3 criteria", StringComparison.Ordinal));

            // The management model reports the same number.
            var fw = await db.AccreditationFrameworks.AsNoTracking().SingleAsync(f => f.Id == framework);
            Assert.Equal(62.5m, fw.CurrentScore);
            Assert.Equal(1, await db.AccreditationScoreSnapshots.CountAsync(s => s.FrameworkId == framework));
            Assert.Equal(1.0m, (await db.Set<ReadinessCriterion>().AsNoTracking().SingleAsync(c => c.CriterionCode == "A1")).Score);
        }
    }

    [Theory, DatabaseProviders]
    public async Task Framework_Without_Criteria_Is_Refused_Not_Estimated(bool postgres)
    {
        await using var database = await TestDatabase.CreateAsync(postgres);
        await database.EnsureSchemaAsync(Tenant);
        var empty = Guid.NewGuid();
        await using (var db = database.CreateContext(Tenant))
        {
            db.AccreditationFrameworks.Add(new AccreditationFramework { Id = empty, TenantId = Tenant, FrameworkCode = "EMPTY", FrameworkName = "Empty framework" });
            await db.SaveChangesAsync();
        }
        await using (var db = database.CreateContext(Tenant))
        {
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => new ReadinessMeterService(db).ComputeScoreAsync(empty, CancellationToken.None));
            Assert.Contains("no criteria", error.Message, StringComparison.Ordinal);
            await Assert.ThrowsAsync<KeyNotFoundException>(() => new ReadinessMeterService(db).ComputeScoreAsync(Guid.NewGuid(), CancellationToken.None));
        }
        await using (var db = database.CreateContext(Tenant))
            Assert.Equal(0, await db.ReadinessScores.CountAsync());
    }

    [Theory, DatabaseProviders]
    public async Task Prediction_Never_Persists_And_Velocity_Uses_History(bool postgres)
    {
        await using var database = await TestDatabase.CreateAsync(postgres);
        await database.EnsureSchemaAsync(Tenant);
        var framework = await SeedFrameworkAsync(database);
        await using (var db = database.CreateContext(Tenant))
        {
            // A month-old score of 50 → today's 62.5 is +12.5 in ~30 days ≈ 12.7 points per month.
            db.ReadinessScores.Add(new ReadinessScore { Id = Guid.NewGuid(), TenantId = Tenant, FrameworkId = framework, FrameworkName = "HEC", OverallScore = 50, Status = ReadinessStatus.NeedsImprovement, GeneratedAt = DateTime.UtcNow.AddDays(-30) });
            await db.SaveChangesAsync();
            var service = new ReadinessMeterService(db);
            var score = await service.ComputeScoreAsync(framework, CancellationToken.None);
            Assert.InRange(score.ComplianceVelocity, 12.0, 13.5);
            var history = JsonSerializer.Deserialize<JsonElement>(score.ScoreHistoryJson);
            Assert.Equal(2, history.GetArrayLength());

            var forecast = await service.PredictReadinessAsync(framework, DateTime.UtcNow.AddMonths(2), CancellationToken.None);
            Assert.Equal(Guid.Empty, forecast.Id);
            Assert.True(forecast.OverallScore > score.OverallScore);
            Assert.True(forecast.OverallScore <= 100);
        }
        await using (var db = database.CreateContext(Tenant))
            Assert.Equal(2, await db.ReadinessScores.CountAsync(s => s.FrameworkId == framework));
    }

    [Theory, DatabaseProviders]
    public async Task Freshness_Check_Grades_Every_Evidence_Item(bool postgres)
    {
        await using var database = await TestDatabase.CreateAsync(postgres);
        await database.EnsureSchemaAsync(Tenant);
        await SeedFrameworkAsync(database);
        await using (var db = database.CreateContext(Tenant))
            await new ReadinessMeterService(db).CheckEvidenceFreshnessAsync(CancellationToken.None);
        await using (var db = database.CreateContext(Tenant))
        {
            var logs = await db.EvidenceFreshnessLogs.AsNoTracking().ToListAsync();
            Assert.Equal(2, logs.Count);
            Assert.Contains(logs, l => l.Status == EvidenceFreshness.Current);
            Assert.Contains(logs, l => l.Status == EvidenceFreshness.Expired);
            Assert.True(await db.CriterionEvidences.AnyAsync(e => e.EvidenceTitle == "Old faculty roster" && e.IsStale));
        }
    }

    [Theory, DatabaseProviders]
    public async Task Management_Service_Agrees_With_The_Meter(bool postgres)
    {
        await using var database = await TestDatabase.CreateAsync(postgres);
        await database.EnsureSchemaAsync(Tenant);
        var framework = await SeedFrameworkAsync(database);
        await using var db = database.CreateContext(Tenant);
        var management = new AccreditationService(db, NullLogger<AccreditationService>.Instance);
        var dto = await management.ComputeFrameworkScoreAsync(framework);
        Assert.Equal(62.5m, dto.OverallScore);
        Assert.Equal("satisfactory", dto.Interpretation);
        var gaps = await management.GetGapAnalysisAsync(framework);
        Assert.Equal(new[] { "B1", "A2" }, gaps.Gaps.Select(g => g.CriterionCode).ToArray());
        Assert.Equal(0, await db.GapAnalysisItems.CountAsync()); // reads do not write analysis rows
        var predictive = await management.GetPredictiveReadinessAsync(framework);
        Assert.Equal(100m, predictive.BestCaseScore);
        Assert.Equal(62.5m, predictive.WorstCaseScore);
    }

    [Fact]
    public void Scoring_Rule_Is_Evidence_Only()
    {
        var now = DateTimeOffset.UtcNow;
        Assert.Equal(ReadinessScoring.NotMet, ReadinessScoring.Evaluate([], now).Status);
        Assert.Equal(ReadinessScoring.PartiallyMet, ReadinessScoring.Evaluate([new CriterionEvidence { IsStale = true }], now).Status);
        Assert.Equal(ReadinessScoring.PartiallyMet, ReadinessScoring.Evaluate([new CriterionEvidence { ExpiryDate = now.AddDays(-1) }], now).Status);
        Assert.Equal(ReadinessScoring.FullyMet, ReadinessScoring.Evaluate([new CriterionEvidence { ExpiryDate = now.AddDays(-1) }, new CriterionEvidence()], now).Status);
        Assert.Null(ReadinessScoring.WeightedPercent([]));
        Assert.Equal(ReadinessStatus.CriticalRisk, ReadinessScoring.StatusFor(44.99m));
        Assert.Equal(ReadinessStatus.Excellent, ReadinessScoring.StatusFor(90m));
    }

    private static async Task<Guid> SeedFrameworkAsync(TestDatabase database)
    {
        var frameworkId = Guid.NewGuid();
        var a1 = Guid.NewGuid(); var a2 = Guid.NewGuid(); var b1 = Guid.NewGuid();
        await using var db = database.CreateContext(Tenant);
        db.AccreditationFrameworks.Add(new AccreditationFramework
        {
            Id = frameworkId, TenantId = Tenant, FrameworkCode = "HEC-GEN", FrameworkName = "HEC General", AccreditationBody = "HEC",
            NextAuditDate = DateTimeOffset.UtcNow.AddDays(200),
        });
        db.Set<ReadinessCriterion>().AddRange(
            new ReadinessCriterion { Id = a1, TenantId = Tenant, FrameworkId = frameworkId, CriterionCode = "A1", SectionName = "Governance", Description = "Statutes approved", Weight = 2.0m },
            new ReadinessCriterion { Id = a2, TenantId = Tenant, FrameworkId = frameworkId, CriterionCode = "A2", SectionName = "Governance", Description = "Board minutes", Weight = 1.0m },
            new ReadinessCriterion { Id = b1, TenantId = Tenant, FrameworkId = frameworkId, CriterionCode = "B1", SectionName = "Faculty", Description = "Faculty roster current", Weight = 1.0m, EvidenceSource = "HR register" });
        db.CriterionEvidences.AddRange(
            new CriterionEvidence { Id = Guid.NewGuid(), TenantId = Tenant, CriterionId = a1, EvidenceTitle = "Approved statutes", EvidenceDate = DateTimeOffset.UtcNow.AddDays(-10), ExpiryDate = DateTimeOffset.UtcNow.AddYears(1) },
            new CriterionEvidence { Id = Guid.NewGuid(), TenantId = Tenant, CriterionId = a2, EvidenceTitle = "Old faculty roster", EvidenceDate = DateTimeOffset.UtcNow.AddYears(-2), ExpiryDate = DateTimeOffset.UtcNow.AddDays(-5) });
        await db.SaveChangesAsync();
        return frameworkId;
    }
}
