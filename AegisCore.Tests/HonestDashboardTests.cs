using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Platform;
using AegisCore.Infrastructure.Repositories;
using AegisCore.Tests.Infrastructure;

namespace AegisCore.Tests;

/// <summary>
/// The board dashboard, the grade distribution and the leaderboard answered with numbers typed into
/// the source (82.5, mean 72.5, a random student at 95.5). They now come from the registers, and a
/// register with nothing in it produces "not measured" or "no results", never a figure.
/// </summary>
public sealed class HonestDashboardTests
{
    private static async Task<(TestDatabase Database, AegisDbContext Db, Guid Tenant)> DatabaseAsync()
    {
        var tenant = Guid.NewGuid();
        var database = await TestDatabase.CreateAsync(postgres: false);
        await database.EnsureSchemaAsync(tenant);
        return (database, database.CreateContext(tenant), tenant);
    }

    private static async Task<Guid> SeedResultsAsync(AegisDbContext db, Guid tenant, params decimal[] percentages)
    {
        var department = new Department { Id = Guid.NewGuid(), TenantId = tenant, Name = "Computing", Code = "CMP-" + Guid.NewGuid().ToString("N")[..6] };
        var batch = new Batch { Id = Guid.NewGuid(), TenantId = tenant, Name = "2026", StartYear = 2026, EndYear = 2030, DepartmentId = department.Id };
        var course = new Course { Id = Guid.NewGuid(), TenantId = tenant, Title = "Algorithms", Code = "CS" + Guid.NewGuid().ToString("N")[..5], CreditHours = 3, DepartmentId = department.Id };
        db.Departments.Add(department); db.Batches.Add(batch); db.Courses.Add(course);
        foreach (var percent in percentages)
        {
            var student = new Student { Id = Guid.NewGuid(), TenantId = tenant, FirstName = "S", LastName = percent.ToString(), BatchId = batch.Id };
            var enrollment = new Enrollment { Id = Guid.NewGuid(), TenantId = tenant, StudentId = student.Id, CourseId = course.Id, EnrollmentStatus = EnrollmentStatus.Active };
            db.Students.Add(student);
            db.Enrollments.Add(enrollment);
            db.Results.Add(new Result { Id = Guid.NewGuid(), TenantId = tenant, EnrollmentId = enrollment.Id, CourseId = course.Id, TotalMarks = 100, ObtainedMarks = percent });
        }
        await db.SaveChangesAsync();
        return course.Id;
    }

    [Fact]
    public async Task Grade_Distribution_Is_Computed_From_The_Recorded_Marks()
    {
        var (database, db, tenant) = await DatabaseAsync();
        await using var _ = database; await using var __ = db;
        var course = await SeedResultsAsync(db, tenant, 95, 85, 75, 65, 45);
        var service = new GradeDistributionService(new Repository<GradeDistribution>(db), new Repository<DistributionAnomalyAlert>(db), db);

        var dist = await service.AnalyzeAsync(course, "Fall 2026", null, CancellationToken.None);
        Assert.Equal(73, dist.Mean);
        Assert.Equal(75, dist.Median);
        Assert.Equal(5, dist.EnrollmentCount);
        Assert.Equal(0.8, dist.PassRate, 3);
        Assert.Equal(0.4, dist.DistinctionRate, 3);
        Assert.Equal(0.2, dist.FailureRate, 3);
        var letters = JsonSerializer.Deserialize<Dictionary<string, int>>(dist.LetterDistributionJson)!;
        Assert.Equal(1, letters["A+"]); Assert.Equal(1, letters["F"]);
        Assert.Single(db.GradeDistributions); // persisted

        var empty = new Course { Id = Guid.NewGuid(), TenantId = tenant, Title = "No marks yet", Code = "CS999", CreditHours = 3, DepartmentId = db.Departments.Select(d => d.Id).First() };
        db.Courses.Add(empty); await db.SaveChangesAsync();
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => service.AnalyzeAsync(empty.Id, "Fall 2026", null, CancellationToken.None));
        Assert.Contains("No results are recorded", ex.Message);
    }

    [Fact]
    public async Task Leaderboard_And_Deans_List_Rank_Real_Students()
    {
        var (database, db, tenant) = await DatabaseAsync();
        await using var _ = database; await using var __ = db;
        await SeedResultsAsync(db, tenant, 95, 80, 60);
        var service = new AchievementService(new Repository<StudentBadge>(db), new Repository<AchievementBadge>(db), new Repository<DeansListEntry>(db), new Repository<LeaderboardEntry>(db), new Repository<AchievementRoadmap>(db), db);

        var board = await service.GenerateLeaderboardAsync(LeaderboardScope.Class, "BSCS", "Fall 2026", CancellationToken.None);
        Assert.Equal(3, board.Count);
        Assert.Equal(new[] { 1, 2, 3 }, board.Select(b => b.Rank).ToArray());
        Assert.Equal(3.8m, board[0].Score); // 95% → 3.80 on the 4.0 scale
        Assert.Contains(board[0].StudentId, db.Students.Select(s => s.Id)); // a real student, not a fresh Guid

        var deans = await service.ProcessDeansListAsync("Fall 2026", CancellationToken.None);
        Assert.Single(deans); // only the 95% student clears 3.50
        Assert.Equal(board[0].StudentId, deans[0].StudentId);

        // Regenerating replaces the previous board instead of stacking a second one.
        await service.GenerateLeaderboardAsync(LeaderboardScope.Class, "BSCS", "Fall 2026", CancellationToken.None);
        Assert.Equal(3, db.LeaderboardEntries.Count());
    }

    [Fact]
    public async Task Board_Dashboard_Reports_Unmeasured_Indices_Instead_Of_Inventing_Them()
    {
        var (database, db, tenant) = await DatabaseAsync();
        await using var _ = database; await using var __ = db;
        var service = new BoardPortalService(new Repository<ExecutiveDashboard>(db), new Repository<BoardMeetingPack>(db), new Repository<BoardAlert>(db), new Repository<StrategicPlanGoal>(db), new Repository<BenchmarkComparison>(db), db);

        var empty = await service.GenerateDashboardAsync("chancellor", "2026", CancellationToken.None);
        Assert.Equal(0, empty.CompositeRankingScore);
        var kpis = JsonSerializer.Deserialize<JsonElement>(empty.KPIsJson);
        Assert.Equal(0, kpis.GetProperty("enrolment").GetInt32());
        Assert.Contains("academicExcellence", kpis.GetProperty("unmeasured").EnumerateArray().Select(e => e.GetString()));
        Assert.Contains("financialStability", kpis.GetProperty("unmeasured").EnumerateArray().Select(e => e.GetString()));

        await SeedResultsAsync(db, tenant, 90, 70, 40);
        var measured = await service.GenerateDashboardAsync("chancellor", "2026", CancellationToken.None);
        Assert.Equal(66.7, measured.AcademicExcellenceScore);
        Assert.Equal(66.7, measured.StudentSuccessScore); // 2 of 3 pass
        Assert.Equal(0, measured.FinancialStabilityScore); // nothing billed → unmeasured, not 78.0
        var pack = await service.GenerateBoardPackAsync("Q3", DateTime.UtcNow, CancellationToken.None);
        Assert.Contains("3 students", pack.ExecutiveSummary);
        Assert.Contains("no fees billed yet", pack.ExecutiveSummary);
    }
}
