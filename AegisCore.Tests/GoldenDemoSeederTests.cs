using AegisCore.Application.Analytics;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Analytics;
using AegisCore.Infrastructure.Deployment;
using AegisCore.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace AegisCore.Tests;

public sealed class GoldenDemoSeederTests
{
    [Fact]
    public async Task GoldenSeeder_IsIdempotentAndCreatesCoherentShowcaseRelationships()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var tenantId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext();
        httpContext.Items["TenantId"] = tenantId;
        var accessor = new FixedHttpContextAccessor { HttpContext = httpContext };
        await using var db = new AegisDbContext(
            new DbContextOptionsBuilder<AegisDbContext>().UseSqlite(connection).Options,
            accessor);
        await db.Database.EnsureCreatedAsync();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Runtime:DemoMode"] = "true",
            ["ASPNETCORE_ENVIRONMENT"] = "Development",
            ["AEGISCORE_DEMO_PASSWORD"] = "GoldenDemo!2026",
            ["Analytics:MinimumCohortSize"] = "10"
        }).Build();
        var statistics = new StatisticalAnalyticsEngine();
        var seeder = new GoldenDemoSeeder(db, new PasswordHashingService(), statistics, new TelemetryPolicyEngine(),
            configuration, NullLogger<GoldenDemoSeeder>.Instance);

        await seeder.SeedAsync(tenantId);
        var firstCounts = await CountsAsync(db, tenantId);
        var firstPasswordHash = await db.Users.IgnoreQueryFilters()
            .Where(item => item.Email == "student@iobm.demo")
            .Select(item => item.PasswordHash)
            .SingleAsync();
        await seeder.SeedAsync(tenantId);
        var secondCounts = await CountsAsync(db, tenantId);
        var secondPasswordHash = await db.Users.IgnoreQueryFilters()
            .Where(item => item.Email == "student@iobm.demo")
            .Select(item => item.PasswordHash)
            .SingleAsync();

        Assert.Equal(firstCounts, secondCounts);
        Assert.Equal(firstPasswordHash, secondPasswordHash);
        Assert.Equal(GoldenDemoSeeder.ExpectedGoldenUsers, firstCounts.Users);
        Assert.Equal(GoldenDemoSeeder.ExpectedCohortSize, firstCounts.Students);

        var goldenStudentId = GoldenDemoSeeder.StableId(tenantId, "student:golden");
        var teacherId = GoldenDemoSeeder.StableId(tenantId, "faculty:golden-teacher");
        var courseId = GoldenDemoSeeder.StableId(tenantId, "course:dsa");
        var gradeId = GoldenDemoSeeder.StableId(tenantId, "course-grade:golden:dsa");
        var user = await db.Users.IgnoreQueryFilters().SingleAsync(item => item.Email == "student@iobm.demo");
        Assert.Equal(goldenStudentId, user.StudentId);
        Assert.True(await db.CourseAssignments.AnyAsync(item => item.CourseId == courseId && item.FacultyId == teacherId));
        Assert.True(await db.Enrollments.AnyAsync(item => item.CourseId == courseId && item.StudentId == goldenStudentId));

        var analytics = new AnalyticsIntelligenceService(db, statistics, configuration);
        var explanation = await analytics.GetGradeExplanationAsync(gradeId, goldenStudentId);
        Assert.NotNull(explanation?.Data);
        Assert.Equal(explanation.Data.FinalScore,
            Math.Round(explanation.Data.AssessmentComponents.Sum(item => item.WeightedContribution), 2, MidpointRounding.AwayFromZero));
        Assert.Equal(36, explanation.Data.StatisticalContext.SampleSize);
        var telemetry = await analytics.GetLatestTelemetryAsync(goldenStudentId, courseId);
        Assert.Equal(goldenStudentId, telemetry?.StudentId);
        Assert.Equal(courseId, telemetry?.CourseId);
        Assert.Empty(db.AnalyticsModelPredictionSnapshots);
        Assert.Empty(db.AnalyticsModelQualitySnapshots);
        Assert.Empty(db.AnalyticsGlobalExplanationSnapshots);

        var gradeBands = await db.GradeBands.AsNoTracking().ToListAsync();
        var grades = await db.CourseGrades.AsNoTracking().ToListAsync();
        Assert.Equal(GoldenDemoSeeder.ExpectedCohortSize, grades.Count);
        Assert.All(grades, grade => Assert.Single(gradeBands.Where(band =>
            band.LetterGrade == grade.LetterGrade
            && grade.PercentageObtained >= band.PercentageFrom
            && grade.PercentageObtained < band.PercentageTo + 1m)));

        var admissionTerm = await (
            from meritList in db.MeritLists.AsNoTracking()
            join semester in db.Semesters.AsNoTracking() on meritList.SemesterId equals semester.Id
            where meritList.Id == GoldenDemoSeeder.StableId(tenantId, "merit-list:bscs-2022")
            select new { semester.Name, semester.StartDate, meritList.PublishedAt }).SingleAsync();
        Assert.Equal("Fall 2022", admissionTerm.Name);
        Assert.True(DateOnly.FromDateTime(admissionTerm.PublishedAt!.Value.UtcDateTime) < admissionTerm.StartDate);
    }

    [Fact]
    public async Task GoldenSeeder_RefusesProduction()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var accessor = new FixedHttpContextAccessor { HttpContext = new DefaultHttpContext() };
        await using var db = new AegisDbContext(new DbContextOptionsBuilder<AegisDbContext>().UseSqlite(connection).Options, accessor);
        await db.Database.EnsureCreatedAsync();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Runtime:DemoMode"] = "true",
            ["ASPNETCORE_ENVIRONMENT"] = "Production",
            ["AEGISCORE_DEMO_PASSWORD"] = "GoldenDemo!2026"
        }).Build();
        var seeder = new GoldenDemoSeeder(db, new PasswordHashingService(), new StatisticalAnalyticsEngine(),
            new TelemetryPolicyEngine(), configuration, NullLogger<GoldenDemoSeeder>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => seeder.SeedAsync(Guid.NewGuid()));
    }

    [Theory]
    [InlineData("Staging")]
    [InlineData("production")]
    [InlineData("Production ")]
    public async Task GoldenSeeder_RefusesEveryNonDevelopmentEnvironment(string environment)
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var accessor = new FixedHttpContextAccessor { HttpContext = new DefaultHttpContext() };
        await using var db = new AegisDbContext(new DbContextOptionsBuilder<AegisDbContext>().UseSqlite(connection).Options, accessor);
        await db.Database.EnsureCreatedAsync();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Runtime:DemoMode"] = "true",
            ["ASPNETCORE_ENVIRONMENT"] = environment,
            ["AEGISCORE_DEMO_PASSWORD"] = "GoldenDemo!2026"
        }).Build();
        var seeder = new GoldenDemoSeeder(db, new PasswordHashingService(), new StatisticalAnalyticsEngine(),
            new TelemetryPolicyEngine(), configuration, NullLogger<GoldenDemoSeeder>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => seeder.SeedAsync(Guid.NewGuid()));
    }

    private static async Task<(int Users, int Students, int Enrollments, int Grades, int Telemetry)> CountsAsync(
        AegisDbContext db,
        Guid tenantId) =>
        (
            await db.Users.IgnoreQueryFilters().CountAsync(item => item.TenantId == tenantId),
            await db.Students.IgnoreQueryFilters().CountAsync(),
            await db.Enrollments.IgnoreQueryFilters().CountAsync(),
            await db.CourseGrades.IgnoreQueryFilters().CountAsync(),
            await db.AnalyticsStudentTelemetrySnapshots.IgnoreQueryFilters().CountAsync(item => item.TenantId == tenantId)
        );

    private sealed class FixedHttpContextAccessor : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; }
    }
}
