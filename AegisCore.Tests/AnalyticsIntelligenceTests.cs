using AegisCore.Application.Analytics;
using AegisCore.API.Controllers;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Analytics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AegisCore.Tests;

public sealed class AnalyticsIntelligenceTests
{
    [Fact]
    public async Task GradeExplanation_ReproducesWeightedArithmeticAndBoundary()
    {
        await using var fixture = await AnalyticsFixture.CreateAsync(minimumCohortSize: 2);
        var seeded = await fixture.SeedGradeAsync(78.4m, "B+", 80, 96);

        var result = await fixture.Service.GetGradeExplanationAsync(seeded.GradeId, seeded.StudentId);

        Assert.NotNull(result);
        Assert.Equal(AvailabilityStatus.Available, result.Availability.Status);
        Assert.Equal(ExplanationSource.Rule, result.DecisionSource);
        Assert.Equal(78.4m, result.Data!.TotalWeightedScore);
        Assert.Equal("B+", result.Data.FinalGrade);
        Assert.Equal(40m, result.Data.AssessmentComponents[0].WeightedContribution);
        Assert.Equal(38.4m, result.Data.AssessmentComponents[1].WeightedContribution);
        Assert.Equal(77m, result.Data.AppliedGradeBoundary.MinimumInclusive);
        Assert.Equal(80m, result.Data.NextGradeBoundary!.MinimumInclusive);
        Assert.Equal(1.6m, result.Data.DistanceToNextGrade);
        Assert.StartsWith("sha256:", result.Data.GradingPolicyVersion);
        Assert.Equal(AvailabilityStatus.Available, result.Data.StatisticalContext.Availability.Status);
    }

    [Fact]
    public async Task GradeExplanation_RefusesCurrentDataThatDoesNotReproducePersistedGrade()
    {
        await using var fixture = await AnalyticsFixture.CreateAsync();
        var seeded = await fixture.SeedGradeAsync(79m, "B+", 80, 96);

        var result = await fixture.Service.GetGradeExplanationAsync(seeded.GradeId, seeded.StudentId);

        Assert.NotNull(result);
        Assert.Equal(AvailabilityStatus.Unavailable, result.Availability.Status);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task GradeExplanation_HandlesExactBoundaryInclusively()
    {
        await using var fixture = await AnalyticsFixture.CreateAsync();
        var seeded = await fixture.SeedGradeAsync(80m, "A", 80, 100);

        var result = await fixture.Service.GetGradeExplanationAsync(seeded.GradeId, seeded.StudentId);

        Assert.Equal(AvailabilityStatus.Available, result!.Availability.Status);
        Assert.Equal(80m, result.Data!.AppliedGradeBoundary.MinimumInclusive);
        Assert.Null(result.Data.NextGradeBoundary);
        Assert.Null(result.Data.DistanceToNextGrade);
    }

    [Fact]
    public async Task GradeExplanation_DoesNotAllowStudentToInspectPeer()
    {
        await using var fixture = await AnalyticsFixture.CreateAsync();
        var seeded = await fixture.SeedGradeAsync(78.4m, "B+", 80, 96);

        var result = await fixture.Service.GetGradeExplanationAsync(seeded.GradeId, Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task AdmissionExplanation_UsesPersistedMeritAndNonPromissoryCutoffArithmetic()
    {
        await using var fixture = await AnalyticsFixture.CreateAsync();
        var applicationId = await fixture.SeedAdmissionAsync();

        var result = await fixture.Service.GetAdmissionExplanationAsync(applicationId);

        Assert.NotNull(result);
        Assert.Equal(AvailabilityStatus.Available, result.Availability.Status);
        Assert.Equal("waitlist", result.Data!.Decision);
        Assert.Equal(80m, result.Data.AggregateScore);
        Assert.Equal(81.73m, result.Data.ClosingMerit);
        Assert.Equal(-1.73m, result.Data.DistanceFromClosingMerit);
        Assert.Equal(1, result.Data.WaitlistPosition);
        Assert.Null(result.Data.PolicyVersion);
        Assert.Contains(result.Data.Limitations, item => item.Contains("does not promise", StringComparison.Ordinal));
    }

    [Fact]
    public async Task PredictionSnapshot_RequiresGenuineModelForShapAndPreservesVersionedHistory()
    {
        await using var fixture = await AnalyticsFixture.CreateAsync();
        var predictionId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;
        var v1 = Prediction(predictionId, "1.0", timestamp);

        await fixture.Service.PersistPredictionSnapshotAsync(v1);
        var explanation = await fixture.Service.GetPredictionExplanationAsync(predictionId, v1.SubjectId, null, false);

        Assert.Equal("1.0", explanation!.Data!.ModelVersion);
        Assert.Equal("Factors influencing this model prediction", explanation.Data.Label);
        Assert.Equal(2, explanation.Data.RankedLocalContributions.Count);
        Assert.Contains("not causes", explanation.Data.Disclaimer, StringComparison.OrdinalIgnoreCase);

        var rewritten = v1 with { ModelVersion = "2.0" };
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.PersistPredictionSnapshotAsync(rewritten));

        var v2 = Prediction(Guid.NewGuid(), "2.0", timestamp.AddDays(1));
        await fixture.Service.PersistPredictionSnapshotAsync(v2);
        Assert.Equal(2, await fixture.Db.AnalyticsModelPredictionSnapshots.CountAsync());
        Assert.Equal("1.0", (await fixture.Service.GetPredictionExplanationAsync(predictionId, v1.SubjectId, null, false))!.Data!.ModelVersion);
    }

    [Fact]
    public async Task PredictionSnapshot_RejectsShapForHeuristicAndFailedAdditivity()
    {
        await using var fixture = await AnalyticsFixture.CreateAsync();
        var valid = Prediction(Guid.NewGuid(), "1.0", DateTimeOffset.UtcNow);

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.PersistPredictionSnapshotAsync(valid with { ModelKind = ModelKind.Heuristic }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.PersistPredictionSnapshotAsync(valid with { RawPrediction = 0.9m }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.PersistPredictionSnapshotAsync(valid with
        {
            FeatureValues = new Dictionary<string, decimal> { ["gender"] = 1 },
            ShapValues = new Dictionary<string, decimal> { ["gender"] = 0.2m }
        }));
    }

    [Fact]
    public async Task ModelQualityAndGlobalShap_AreVersionedAggregateSnapshots()
    {
        await using var fixture = await AnalyticsFixture.CreateAsync();
        var now = DateTimeOffset.UtcNow;
        var quality = new ModelQuality("academic-risk", "1.0", now.AddYears(-1), now.AddMonths(-1), 1000,
            0.03m, 0.84m, 0.72m, 0.68m, 0.88m, 0.70m, 68, 792, 28, 112, 0.6m, 0.02m, now,
            ["Proxy review completed by model-risk committee"],
            new Dictionary<string, decimal> { ["equal_opportunity_difference"] = 0.04m },
            "Measured evidence; this is not a claim that the model is bias free.");
        await fixture.Service.PersistModelQualitySnapshotAsync(quality, new string('a', 64));

        var global = new GlobalModelExplanation(Guid.NewGuid(), "academic-risk", "1.0", "risk-features-v1", "Authorized 2026 cohort", 100,
            now.AddMonths(-6), now, new Dictionary<string, decimal> { ["attendance"] = 0.15m, ["gpa"] = 0.08m },
            "TreeSHAP", "0.47.0", now, "Non-causal model attribution.");
        await fixture.Service.PersistGlobalExplanationSnapshotAsync(global, new string('b', 64), ModelKind.GenuinePredictiveModel);

        Assert.Equal(0.84m, (await fixture.Service.GetLatestModelQualityAsync("academic-risk", "1.0"))!.AuRoc);
        Assert.Equal(100, (await fixture.Service.GetLatestGlobalExplanationAsync("academic-risk", "1.0"))!.SampleSize);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.PersistGlobalExplanationSnapshotAsync(
            global with { SnapshotId = Guid.NewGuid() }, new string('c', 64), ModelKind.Heuristic));
    }

    [Fact]
    public async Task PredictionRead_IsTenantAndSubjectIsolated()
    {
        await using var fixture = await AnalyticsFixture.CreateAsync();
        var prediction = Prediction(Guid.NewGuid(), "1.0", DateTimeOffset.UtcNow);
        await fixture.Service.PersistPredictionSnapshotAsync(prediction);

        Assert.Null(await fixture.Service.GetPredictionExplanationAsync(prediction.PredictionId, Guid.NewGuid(), null, false));

        fixture.SetTenant(Guid.NewGuid());
        Assert.Null(await fixture.Service.GetPredictionExplanationAsync(prediction.PredictionId, prediction.SubjectId, null, false));
    }

    [Fact]
    public async Task MissingTenantContext_FailsClosed()
    {
        await using var fixture = await AnalyticsFixture.CreateAsync();
        fixture.SetTenant(Guid.Empty);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.Service.GetPredictionExplanationAsync(Guid.NewGuid(), Guid.NewGuid(), null, false));
    }

    [Fact]
    public void Controller_DeclaresNarrowRolePoliciesForConsequentialExplanations()
    {
        var gradeRoles = typeof(AnalyticsController).GetMethod(nameof(AnalyticsController.GetMyGradeExplanation))!
            .GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Single().Roles;
        var admissionRoles = typeof(AnalyticsController).GetMethod(nameof(AnalyticsController.GetAdmissionDecisionExplanation))!
            .GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Single().Roles;
        var predictionRoles = typeof(AnalyticsController).GetMethod(nameof(AnalyticsController.GetPredictionExplanation))!
            .GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Single().Roles;

        Assert.Equal("Student", gradeRoles);
        Assert.Equal("Admin,SuperAdmin,Registrar,AdmissionsHead", admissionRoles);
        Assert.Equal("Student,HOD,Dean,Admin,SuperAdmin,Registrar,ExamController,VC", predictionRoles);
    }

    [Fact]
    public async Task Snapshots_AreAppendOnlyInApplicationAndDatabaseContext()
    {
        await using var fixture = await AnalyticsFixture.CreateAsync();
        var prediction = Prediction(Guid.NewGuid(), "1.0", DateTimeOffset.UtcNow);
        await fixture.Service.PersistPredictionSnapshotAsync(prediction);

        var entity = await fixture.Db.AnalyticsModelPredictionSnapshots.SingleAsync();
        entity.ModelVersion = "mutated";
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Db.SaveChangesAsync());
    }

    [Fact]
    public async Task Telemetry_EnforcesSuppressionAndDeduplicatesEquivalentSnapshot()
    {
        await using var fixture = await AnalyticsFixture.CreateAsync();
        var studentId = await fixture.SeedStudentAsync();
        var now = DateTimeOffset.UtcNow;
        var suppressed = new StudentTelemetry(studentId, null, "2026-Fall", "Program cohort", 10,
        [
            new TelemetryMetric("CoursePerformance", 72, 72, null, null, null, null, null, 68, 4, null,
                TrendDirection.Up, PerformanceStatus.Developing, ImprovementStatus.Improving, 3, "2026-Fall", "Program cohort", now,
                new Availability(AvailabilityStatus.Suppressed, "Minimum cohort size not met."))
        ], now, now);

        var first = await fixture.Service.PersistTelemetrySnapshotAsync(suppressed);
        var second = await fixture.Service.PersistTelemetrySnapshotAsync(suppressed);

        Assert.Equal(first, second);
        Assert.Single(await fixture.Db.AnalyticsStudentTelemetrySnapshots.ToListAsync());

        var leaking = suppressed with
        {
            Metrics = [suppressed.Metrics[0] with { CohortMean = 70 }]
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.PersistTelemetrySnapshotAsync(leaking));
    }

    private static PredictionSnapshotWrite Prediction(Guid id, string version, DateTimeOffset timestamp) => new(
        id,
        Guid.NewGuid(),
        "Student",
        ModelKind.GenuinePredictiveModel,
        "academic-risk",
        version,
        "risk-features-v1",
        timestamp,
        timestamp.AddHours(-1),
        0.7m,
        0.68m,
        0.5m,
        new Dictionary<string, decimal> { ["attendance"] = 0.6m, ["gpa"] = 2.4m },
        new Dictionary<string, decimal> { ["attendance"] = 0.15m, ["gpa"] = 0.05m },
        "TreeSHAP",
        "0.47.0",
        true);

    private sealed class AnalyticsFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly FixedHttpContextAccessor _accessor;
        public AegisDbContext Db { get; }
        public AnalyticsIntelligenceService Service { get; }
        public Guid TenantId { get; }

        private AnalyticsFixture(SqliteConnection connection, FixedHttpContextAccessor accessor, AegisDbContext db,
            AnalyticsIntelligenceService service, Guid tenantId)
        {
            _connection = connection;
            _accessor = accessor;
            Db = db;
            Service = service;
            TenantId = tenantId;
        }

        public static async Task<AnalyticsFixture> CreateAsync(int minimumCohortSize = 10)
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();
            var tenantId = Guid.NewGuid();
            var context = new DefaultHttpContext();
            context.Items["TenantId"] = tenantId;
            var accessor = new FixedHttpContextAccessor { HttpContext = context };
            var db = new AegisDbContext(new DbContextOptionsBuilder<AegisDbContext>().UseSqlite(connection).Options, accessor);
            await db.Database.EnsureCreatedAsync();
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Analytics:MinimumCohortSize"] = minimumCohortSize.ToString()
            }).Build();
            return new AnalyticsFixture(connection, accessor, db,
                new AnalyticsIntelligenceService(db, new StatisticalAnalyticsEngine(), configuration), tenantId);
        }

        public void SetTenant(Guid tenantId) => _accessor.HttpContext!.Items["TenantId"] = tenantId;

        public async Task<Guid> SeedStudentAsync()
        {
            var department = new Department { Id = Guid.NewGuid(), Name = "Analytics", TenantId = TenantId };
            var batch = new Batch { Id = Guid.NewGuid(), Name = "2026", DepartmentId = department.Id, Department = department };
            var student = new Student { Id = Guid.NewGuid(), FirstName = "A", LastName = "Student", BatchId = batch.Id, Batch = batch };
            Db.AddRange(department, batch, student);
            await Db.SaveChangesAsync();
            return student.Id;
        }

        public async Task<(Guid GradeId, Guid StudentId)> SeedGradeAsync(decimal persistedScore, string letterGrade, int firstMark, int secondMark)
        {
            var department = new Department { Id = Guid.NewGuid(), Name = $"Computing-{Guid.NewGuid():N}", TenantId = TenantId };
            var batch = new Batch { Id = Guid.NewGuid(), Name = $"2026-{Guid.NewGuid():N}", DepartmentId = department.Id, Department = department };
            var student = new Student { Id = Guid.NewGuid(), FirstName = "Grade", LastName = "Owner", BatchId = batch.Id, Batch = batch };
            var peer = new Student { Id = Guid.NewGuid(), FirstName = "Grade", LastName = "Peer", BatchId = batch.Id, Batch = batch };
            var course = new Course { Id = Guid.NewGuid(), Title = "Explainable Systems", Code = $"EX-{Guid.NewGuid():N}"[..20], CreditHours = 3, DepartmentId = department.Id, Department = department };
            var semesterId = Guid.NewGuid();
            var scheme = new AssessmentScheme { Id = Guid.NewGuid(), CourseId = course.Id, SemesterId = semesterId };
            var first = new AssessmentComponent { Id = Guid.NewGuid(), SchemeId = scheme.Id, ComponentType = "Coursework", WeightPercentage = 50, TotalMarks = 100, OrderIndex = 1 };
            var second = new AssessmentComponent { Id = Guid.NewGuid(), SchemeId = scheme.Id, ComponentType = "Final", WeightPercentage = 50, TotalMarks = 125, OrderIndex = 2 };
            scheme.Components = [first, second];
            var scale = new GradeScale { Id = Guid.NewGuid(), UniversityId = TenantId, Name = "2026 Scale" };
            var bands = new[]
            {
                new GradeBand { Id = Guid.NewGuid(), GradeScaleId = scale.Id, GradeScale = scale, LetterGrade = "B", GpaPoints = 3m, PercentageFrom = 70, PercentageTo = 76 },
                new GradeBand { Id = Guid.NewGuid(), GradeScaleId = scale.Id, GradeScale = scale, LetterGrade = "B+", GpaPoints = 3.5m, PercentageFrom = 77, PercentageTo = 79 },
                new GradeBand { Id = Guid.NewGuid(), GradeScaleId = scale.Id, GradeScale = scale, LetterGrade = "A", GpaPoints = 4m, PercentageFrom = 80, PercentageTo = 100 }
            };
            scale.Grades = bands;
            var grade = new CourseGrade { Id = Guid.NewGuid(), CourseId = course.Id, SemesterId = semesterId, StudentId = student.Id,
                TotalMarksObtained = firstMark + secondMark, PercentageObtained = persistedScore, LetterGrade = letterGrade,
                GradePoints = 3.5m, IsPassing = true, ResultStatus = "published", CalculatedAt = DateTimeOffset.UtcNow.AddDays(-1), PublishedAt = DateTimeOffset.UtcNow.AddHours(-1) };
            var peerGrade = new CourseGrade { Id = Guid.NewGuid(), CourseId = course.Id, SemesterId = semesterId, StudentId = peer.Id,
                TotalMarksObtained = 180, PercentageObtained = 80m, LetterGrade = "A", GradePoints = 4m,
                IsPassing = true, ResultStatus = "published", CalculatedAt = DateTimeOffset.UtcNow.AddDays(-1), PublishedAt = DateTimeOffset.UtcNow.AddHours(-1) };
            Db.AddRange(department, batch, student, peer, course, scheme, scale);
            Db.CourseMarks.AddRange(
                new CourseMark { Id = Guid.NewGuid(), CourseId = course.Id, StudentId = student.Id, AssessmentComponentId = first.Id, ObtainedMarks = firstMark },
                new CourseMark { Id = Guid.NewGuid(), CourseId = course.Id, StudentId = student.Id, AssessmentComponentId = second.Id, ObtainedMarks = secondMark });
            Db.CourseGrades.AddRange(grade, peerGrade);
            await Db.SaveChangesAsync();
            return (grade.Id, student.Id);
        }

        public async Task<Guid> SeedAdmissionAsync()
        {
            var program = new DegreeProgram { Id = Guid.NewGuid(), Name = "BS Analytics", Code = "BSA", DepartmentId = Guid.NewGuid(), TenantId = TenantId };
            var accepted = new AdmissionApplication { Id = Guid.NewGuid(), StudentName = "Accepted", Email = "accepted@example.edu", Phone = "1", Cnic = "00000-0000000-1", DateOfBirth = new DateOnly(2005, 1, 1), ProgramId = program.Id, MatricPercentage = 82, FscPercentage = 82, EntryTestScore = 82, ApplicationStatus = "verified" };
            var waitlisted = new AdmissionApplication { Id = Guid.NewGuid(), StudentName = "Waitlisted", Email = "wait@example.edu", Phone = "2", Cnic = "00000-0000000-2", DateOfBirth = new DateOnly(2005, 1, 1), ProgramId = program.Id, MatricPercentage = 80, FscPercentage = 80, EntryTestScore = 80, ApplicationStatus = "verified" };
            var list = new MeritList { Id = Guid.NewGuid(), ProgramId = program.Id, SemesterId = Guid.NewGuid(), TotalSeats = 1, RoundNumber = 1, Status = "published", PublishedAt = DateTimeOffset.UtcNow };
            var first = new MeritListEntry { Id = Guid.NewGuid(), MeritListId = list.Id, MeritList = list, ApplicationId = accepted.Id, Application = accepted,
                MeritRank = 1, MeritScore = 81.73m, MatricContribution = 24.6m, FscContribution = 24.6m, EntryTestContribution = 32.53m, SeatAllocated = true, AllocationStatus = "allocated" };
            var second = new MeritListEntry { Id = Guid.NewGuid(), MeritListId = list.Id, MeritList = list, ApplicationId = waitlisted.Id, Application = waitlisted,
                MeritRank = 2, MeritScore = 80m, MatricContribution = 24m, FscContribution = 24m, EntryTestContribution = 32m, AllocationStatus = "waitlist" };
            list.Entries = [first, second];
            Db.AddRange(program, accepted, waitlisted, list);
            await Db.SaveChangesAsync();
            return waitlisted.Id;
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

    private sealed class FixedHttpContextAccessor : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; }
    }
}
