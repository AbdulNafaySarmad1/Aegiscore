using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Platform;
using AegisCore.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AegisCore.Tests;

/// <summary>
/// Registration is the one moment this system is asked to do everything at once, and it did none of
/// it: the enrolment call wrote a row without checking that the window was open, that the student
/// was not already registered, that they were within their course limit, or that a seat existed.
/// Fifty students racing for ten seats is the test that matters.
/// </summary>
public class RegistrationSpikeTests
{
    private static readonly Guid Tenant = Guid.Parse("3b1c9a64-17d6-4f9a-9a1e-0c0e8b2c77f1");

    /// <remarks>
    /// PostgreSQL only: SQLite serialises writers with a lock and refuses the losers, so it cannot
    /// say anything about what fifty simultaneous callers do to a seat count.
    /// </remarks>
    [PilotPostgresFact]
    public async Task Fifty_Students_Racing_For_Ten_Seats_Fill_Ten_And_Wait_Forty()
    {
        const bool postgres = true;
        await using var database = await TestDatabase.CreateAsync(postgres);
        await database.EnsureSchemaAsync(Tenant);
        await using var db = database.CreateContext(Tenant);

        var (window, courseId) = await SeedAsync(db, seats: 10);
        var students = Enumerable.Range(0, 50).Select(_ => Guid.NewGuid()).ToList();

        // One context per caller, as one request per caller would have.
        var results = await Task.WhenAll(students.Select(async studentId =>
        {
            await using var scoped = database.CreateContext(Tenant);
            var service = Service(scoped);
            try { return (await service.EnrollAsync(window, studentId, courseId, default)).IsWaitlisted ? "waiting" : "seated"; }
            catch (Exception ex) { return "refused: " + ex.Message; }
        }));

        Assert.Equal(10, results.Count(r => r == "seated"));
        Assert.Equal(40, results.Count(r => r == "waiting"));
        Assert.DoesNotContain(results, r => r.StartsWith("refused", StringComparison.Ordinal));

        var offering = await db.CourseOfferings.AsNoTracking().SingleAsync(o => o.CourseId == courseId);
        Assert.Equal(10, offering.EnrolledCount);

        {
            // Everyone waiting knows where they stand, and no two stand in the same place.
            var positions = await db.RegistrationEnrollments.AsNoTracking()
                .Where(e => e.IsWaitlisted).Select(e => e.WaitlistPosition).ToListAsync();
            Assert.Equal(40, positions.Distinct().Count());
            Assert.Equal(Enumerable.Range(1, 40), positions.Order());
        }
    }

    [Theory, DatabaseProviders]
    public async Task The_Same_Student_Cannot_Register_Twice(bool postgres)
    {
        await using var database = await TestDatabase.CreateAsync(postgres);
        await database.EnsureSchemaAsync(Tenant);
        await using var db = database.CreateContext(Tenant);
        var (window, courseId) = await SeedAsync(db, seats: 5);
        var studentId = Guid.NewGuid();
        var service = Service(db);

        await service.EnrollAsync(window, studentId, courseId, default);
        var refused = await Assert.ThrowsAsync<AegisCore.Infrastructure.Deployment.BusinessRuleException>(
            () => service.EnrollAsync(window, studentId, courseId, default));
        Assert.Contains("already registered", refused.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, await db.RegistrationEnrollments.CountAsync());
    }

    [Theory, DatabaseProviders]
    public async Task A_Closed_Window_Refuses_With_Its_Dates(bool postgres)
    {
        await using var database = await TestDatabase.CreateAsync(postgres);
        await database.EnsureSchemaAsync(Tenant);
        await using var db = database.CreateContext(Tenant);
        var (window, courseId) = await SeedAsync(db, seats: 5, closed: true);

        var refused = await Assert.ThrowsAsync<AegisCore.Infrastructure.Deployment.BusinessRuleException>(
            () => Service(db).EnrollAsync(window, Guid.NewGuid(), courseId, default));
        Assert.Contains("not open", refused.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, await db.RegistrationEnrollments.CountAsync());
    }

    [Theory, DatabaseProviders]
    public async Task A_Student_Cannot_Exceed_The_Windows_Course_Limit(bool postgres)
    {
        await using var database = await TestDatabase.CreateAsync(postgres);
        await database.EnsureSchemaAsync(Tenant);
        await using var db = database.CreateContext(Tenant);
        var (window, first) = await SeedAsync(db, seats: 50, maxCourses: 2);
        var service = Service(db);
        var studentId = Guid.NewGuid();

        var others = new List<Guid>();
        for (var i = 0; i < 3; i++)
        {
            var course = new Course { Id = Guid.NewGuid(), TenantId = Tenant, Title = $"Course {i}", Code = $"C{i}", CreditHours = 3, DepartmentId = (await db.Departments.Select(d => d.Id).FirstAsync()) };
            db.Courses.Add(course);
            others.Add(course.Id);
        }
        await db.SaveChangesAsync();

        await service.EnrollAsync(window, studentId, first, default);
        await service.EnrollAsync(window, studentId, others[0], default);
        var refused = await Assert.ThrowsAsync<AegisCore.Infrastructure.Deployment.BusinessRuleException>(
            () => service.EnrollAsync(window, studentId, others[1], default));
        Assert.Contains("the most", refused.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static RegistrationService Service(AegisDbContext db)
        => new(new AegisCore.Infrastructure.Repositories.RegistrationWindowRepository(db),
               new AegisCore.Infrastructure.Repositories.RegistrationEnrollmentRepository(db),
               db);

    private static async Task<(Guid Window, Guid Course)> SeedAsync(AegisDbContext db, int seats, bool closed = false, int maxCourses = 6)
    {
        var department = new Department { Id = Guid.NewGuid(), TenantId = Tenant, Name = "Computer Science", Code = "CS" };
        db.Departments.Add(department);
        var course = new Course { Id = Guid.NewGuid(), TenantId = Tenant, Title = "Programming", Code = "CS101", CreditHours = 3, DepartmentId = department.Id };
        var window = new RegistrationWindow
        {
            Id = Guid.NewGuid(), TenantId = Tenant, Name = "Autumn registration", Semester = "F2026",
            Status = RegistrationStatus.Open, MaxCoursesPerStudent = maxCourses,
            OpensAt = closed ? DateTime.UtcNow.AddDays(-30) : DateTime.UtcNow.AddHours(-1),
            ClosesAt = closed ? DateTime.UtcNow.AddDays(-29) : DateTime.UtcNow.AddHours(1),
        };
        db.Courses.Add(course);
        db.RegistrationWindows.Add(window);
        db.CourseOfferings.Add(new CourseOffering
        {
            Id = Guid.NewGuid(), TenantId = Tenant, CourseId = course.Id, AcademicYear = "2026",
            Semester = "F2026", SectionNumber = 1, Capacity = seats, EnrolledCount = 0,
        });
        await db.SaveChangesAsync();
        return (window.Id, course.Id);
    }
}
