using System.IO.Compression;
using System.Text;
using AegisCore.Application.Compliance;
using AegisCore.Application.Search;
using AegisCore.Contracts.Abstractions.Cloud;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Compliance;
using AegisCore.Infrastructure.Search;
using AegisCore.Infrastructure.Security.Pqc;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AegisCore.Tests;

public sealed class PrivacyComplianceTests
{
    [Fact]
    public async Task FerpaAudit_EnforcesFacultyRelationship_AndLogsAllowedAndDeniedAttempts()
    {
        await using var fixture = await ComplianceFixture.CreateAsync();
        var seed = await fixture.SeedAcademicAsync();
        using var encryption = CreateEncryption();
        var service = new FerpaAuditService(fixture.Db, encryption);

        var allowed = await service.AuthorizeAndAuditAsync(new FerpaAccessRequest(
            seed.Faculty.Id, ["Faculty"], "203.0.113.10", "Attendance", "courseId", seed.Course.Id, "Read"));
        var denied = await service.AuthorizeAndAuditAsync(new FerpaAccessRequest(
            Guid.NewGuid(), ["Faculty"], "203.0.113.11", "Attendance", "courseId", seed.Course.Id, "Read"));

        Assert.True(allowed.IsJustified);
        Assert.False(denied.IsJustified);
        var logs = await fixture.Db.FerpaAuditLogs.OrderBy(item => item.AccessedAt).ToListAsync();
        Assert.Equal(2, logs.Count);
        Assert.True(logs[0].Justified);
        Assert.False(logs[1].Justified);
        Assert.All(logs, item => Assert.Equal(seed.Student.Id, item.StudentId));
        Assert.All(logs, item => Assert.NotNull(item.IpAddressEncrypted));
        Assert.DoesNotContain("203.0.113", Encoding.UTF8.GetString(logs[0].IpAddressEncrypted!), StringComparison.Ordinal);
    }

    [Fact]
    public async Task FullErasureCycle_AnonymizesPii_PreservesAcademicFacts_AndRevokesAccess()
    {
        await using var fixture = await ComplianceFixture.CreateAsync();
        var seed = await fixture.SeedAcademicAsync();
        using var encryption = CreateEncryption();
        var storage = new Mock<IStorageService>();
        storage.Setup(item => item.DeleteAsync("aegiscore-profiles", "profiles/test-picture.jpg", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var service = CreatePrivacyService(fixture.Db, encryption, storage.Object);
        var ferpa = new FerpaAuditService(fixture.Db, encryption);

        var consent = await service.RecordConsentAsync(
            seed.Student.Id,
            "academic_records",
            "I consent to processing my academic records for degree administration.",
            DateTime.UtcNow.AddYears(1),
            "198.51.100.8");
        await ferpa.AuthorizeAndAuditAsync(new FerpaAccessRequest(
            seed.Student.Id, ["Student"], "198.51.100.8", "Grade", "studentId", seed.Student.Id, "Read"));
        fixture.Db.SsoSessions.Add(new SsoSession
        {
            Id = Guid.NewGuid(),
            TenantId = fixture.TenantId,
            UserId = seed.Student.Id,
            Provider = "aegiscore",
            AccessTokenEncrypted = "ciphertext",
            TokenExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        });
        fixture.Db.ProfilePictureRecords.Add(new ProfilePictureRecord
        {
            Id = Guid.NewGuid(),
            TenantId = fixture.TenantId,
            UserId = seed.Student.Id,
            StorageKey = "profiles/test-picture",
            IsActive = true
        });
        fixture.Db.StudentProfiles.Add(new StudentProfile
        {
            Id = Guid.NewGuid(),
            TenantId = fixture.TenantId,
            StudentId = seed.Student.Id,
            ProfileJson = "{\"phone\":\"03001234567\",\"dateOfBirth\":\"2005-01-01\"}"
        });
        await fixture.Db.SaveChangesAsync();

        var dsar = await service.CreateDsarArchiveAsync(seed.Student.Id);
        using (var archive = new ZipArchive(new MemoryStream(dsar.Content), ZipArchiveMode.Read))
        {
            Assert.Equal(
                new[] { "access_log.json", "attendance.csv", "consent.json", "grades.csv", "student.json" },
                archive.Entries.Select(entry => entry.Name).OrderBy(name => name).ToArray());
            var attendanceCsv = await new StreamReader(archive.GetEntry("attendance.csv")!.Open()).ReadToEndAsync();
            Assert.DoesNotContain("latitude", attendanceCsv, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("longitude", attendanceCsv, StringComparison.OrdinalIgnoreCase);
        }
        Assert.InRange(dsar.ExpiresAt, DateTime.UtcNow.AddHours(23.9), DateTime.UtcNow.AddHours(24.1));
        Assert.NotNull(consent.IpAddressEncrypted);

        var request = await service.RequestErasureAsync(seed.Student.Id);
        Assert.Equal(TimeSpan.FromDays(30), request.DeadlineAt - request.RequestedAt);
        await service.ProcessErasureAsync(request.Id);
        fixture.Db.ChangeTracker.Clear();

        var completed = await fixture.Db.ErasureRequests.SingleAsync(item => item.Id == request.Id);
        Assert.Equal(ErasureRequestStatus.Completed, completed.Status);
        Assert.NotNull(completed.ErasedAt);
        var user = await fixture.Db.Users.SingleAsync(item => item.Id == seed.Student.Id);
        Assert.Equal($"{seed.Student.Id:N}@erased.invalid", user.Email);
        Assert.Null(user.RefreshToken);
        var student = await fixture.Db.Students.SingleAsync(item => item.Id == seed.Student.Id);
        Assert.StartsWith("ERASED_USER_", student.FirstName, StringComparison.Ordinal);
        Assert.True(student.DirectoryOptOut);
        Assert.Equal("{}", (await fixture.Db.StudentProfiles.SingleAsync()).ProfileJson);
        Assert.NotNull((await fixture.Db.ConsentLedgers.SingleAsync()).RevokedAt);
        var audit = await fixture.Db.FerpaAuditLogs.SingleAsync();
        Assert.Equal(Guid.Empty, audit.UserId);
        Assert.Equal(Guid.Empty, audit.StudentId);
        Assert.True(audit.UserErased);
        Assert.True(audit.StudentErased);
        Assert.Empty(await fixture.Db.SsoSessions.ToListAsync());
        Assert.Empty(await fixture.Db.ProfilePictureRecords.ToListAsync());
        Assert.Single(await fixture.Db.Attendances.ToListAsync());
        Assert.Single(await fixture.Db.CourseGrades.ToListAsync());
        storage.Verify(item => item.DeleteAsync(
            "aegiscore-profiles", "profiles/test-picture.jpg", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Retention_RemovesOnlyRecordsBeyondConfiguredWindows()
    {
        await using var fixture = await ComplianceFixture.CreateAsync();
        using var encryption = CreateEncryption();
        var service = CreatePrivacyService(fixture.Db, encryption, Mock.Of<IStorageService>());
        var now = new DateTime(2026, 9, 5, 2, 0, 0, DateTimeKind.Utc);
        fixture.Db.FerpaAuditLogs.AddRange(
            Audit(fixture.TenantId, now.AddYears(-7)),
            Audit(fixture.TenantId, now.AddYears(-5)));
        fixture.Db.ConsentLedgers.AddRange(
            Consent(fixture.TenantId, now.AddYears(-7)),
            Consent(fixture.TenantId, now.AddYears(-5)));
        fixture.Db.ErasureRequests.AddRange(
            CompletedErasure(fixture.TenantId, now.AddYears(-8)),
            CompletedErasure(fixture.TenantId, now.AddYears(-6)));
        await fixture.Db.SaveChangesAsync();

        var removed = await service.ApplyRetentionAsync(fixture.TenantId, now, 6);

        Assert.Equal(3, removed);
        Assert.Single(await fixture.Db.FerpaAuditLogs.ToListAsync());
        Assert.Single(await fixture.Db.ConsentLedgers.ToListAsync());
        Assert.Single(await fixture.Db.ErasureRequests.ToListAsync());
    }

    [Fact]
    public async Task DirectoryOptOut_RemovesStudentFromIndexAndSearchResults()
    {
        var hiddenId = Guid.NewGuid();
        var visibleId = Guid.NewGuid();
        var hidden = new Student
        {
            Id = hiddenId,
            TenantId = Guid.NewGuid(),
            FirstName = "Hidden",
            LastName = "Student",
            UniversityId = "HIDDEN-1",
            BatchId = Guid.NewGuid(),
            DirectoryOptOut = true
        };
        var studentIndex = new Mock<ISearchIndex<SearchableStudent>>();
        studentIndex.Setup(index => index.SearchAsync(It.IsAny<SearchQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchResult<SearchableStudent>
            {
                TotalHits = 2,
                Hits =
                [
                    new SearchableStudent { Id = hiddenId.ToString(), FirstName = "Hidden", LastName = "Student" },
                    new SearchableStudent { Id = visibleId.ToString(), FirstName = "Visible", LastName = "Student" }
                ]
            });
        var students = new Mock<IStudentRepository>();
        students.Setup(repository => repository.FindAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Student, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([hidden]);
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.SetupGet(item => item.Students).Returns(students.Object);
        var service = new SearchService(
            studentIndex.Object,
            Mock.Of<ISearchIndex<SearchableCourse>>(),
            unitOfWork.Object,
            NullLogger<SearchService>.Instance);

        var response = await service.SearchAsync("student", new UnifiedSearchRequest { Domains = ["students"] });
        var hit = Assert.Single(Assert.Single(response.Groups).Hits);
        Assert.Equal(visibleId.ToString(), hit.Id);
        await service.IndexEntityAsync("student", hidden);
        studentIndex.Verify(index => index.DeleteAsync(hiddenId.ToString(), It.IsAny<CancellationToken>()), Times.Once);
        studentIndex.Verify(index => index.IndexAsync(It.IsAny<SearchableStudent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static FerpaAuditLog Audit(Guid tenantId, DateTime at) => new()
    {
        TenantId = tenantId,
        UserId = Guid.NewGuid(),
        UserRole = "Student",
        ResourceType = "Grade",
        ResourceId = Guid.NewGuid(),
        StudentId = Guid.NewGuid(),
        Action = "Read",
        AccessedAt = at,
        Justified = true
    };

    private static ConsentLedger Consent(Guid tenantId, DateTime revokedAt) => new()
    {
        TenantId = tenantId,
        UserId = Guid.NewGuid(),
        DataCategory = "academic_records",
        ConsentText = "Consent text",
        ConsentedAt = revokedAt.AddYears(-1),
        RevokedAt = revokedAt
    };

    private static ErasureRequest CompletedErasure(Guid tenantId, DateTime erasedAt) => new()
    {
        TenantId = tenantId,
        UserId = Guid.NewGuid(),
        RequestedAt = erasedAt.AddDays(-1),
        DeadlineAt = erasedAt.AddDays(29),
        ErasedAt = erasedAt,
        Status = ErasureRequestStatus.Completed
    };

    private static PrivacyComplianceService CreatePrivacyService(
        AegisDbContext db,
        IPqcCryptographyService encryption,
        IStorageService storage) =>
        new(
            db,
            encryption,
            storage,
            new ConfigurationBuilder().Build(),
            NullLogger<PrivacyComplianceService>.Instance);

    private static PqcCryptographyService CreateEncryption()
    {
        var key = Convert.ToBase64String(Enumerable.Range(1, 32).Select(value => (byte)value).ToArray());
        return new PqcCryptographyService(
            Options.Create(new PqcCryptographyOptions { MasterKeyBase64 = key }),
            NullLogger<PqcCryptographyService>.Instance);
    }

    private sealed class ComplianceFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        public Guid TenantId { get; }
        public AegisDbContext Db { get; }

        private ComplianceFixture(SqliteConnection connection, Guid tenantId, AegisDbContext db)
        {
            _connection = connection;
            TenantId = tenantId;
            Db = db;
        }

        public static async Task<ComplianceFixture> CreateAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();
            var tenantId = Guid.NewGuid();
            var httpContext = new DefaultHttpContext();
            httpContext.Items["TenantId"] = tenantId;
            var accessor = new TestHttpContextAccessor { HttpContext = httpContext };
            var db = new AegisDbContext(
                new DbContextOptionsBuilder<AegisDbContext>().UseSqlite(connection).Options,
                accessor);
            await db.Database.EnsureCreatedAsync();
            return new ComplianceFixture(connection, tenantId, db);
        }

        public async Task<AcademicSeed> SeedAcademicAsync()
        {
            var department = new Department { TenantId = TenantId, Name = "Science", Code = $"SCI-{Guid.NewGuid():N}"[..12] };
            var batch = new Batch { TenantId = TenantId, Name = "2026", StartYear = 2026, EndYear = 2030, DepartmentId = department.Id };
            var studentId = Guid.NewGuid();
            var student = new Student
            {
                Id = studentId,
                TenantId = TenantId,
                FirstName = "Mariam",
                LastName = "Ahmed",
                UniversityId = $"STU-{studentId:N}"[..20],
                BatchId = batch.Id
            };
            var user = new User
            {
                Id = studentId,
                TenantId = TenantId,
                Username = $"student-{studentId:N}",
                Email = $"{studentId:N}@example.test",
                FirstName = student.FirstName,
                LastName = student.LastName,
                RefreshToken = "active-refresh-token",
                RefreshTokenExpiry = DateTime.UtcNow.AddDays(1)
            };
            var faculty = new Faculty { TenantId = TenantId, FirstName = "Sara", LastName = "Iqbal", DepartmentId = department.Id };
            var course = new Course { TenantId = TenantId, Title = "Data Ethics", Code = $"DE-{Guid.NewGuid():N}"[..10], CreditHours = 3, DepartmentId = department.Id };
            var enrollment = new Enrollment { TenantId = TenantId, StudentId = student.Id, CourseId = course.Id };
            var assignment = new CourseAssignment { TenantId = TenantId, CourseId = course.Id, FacultyId = faculty.Id };
            var attendance = new Attendance
            {
                TenantId = TenantId,
                EnrollmentId = enrollment.Id,
                CourseId = course.Id,
                FacultyId = faculty.Id,
                Date = DateTime.UtcNow.Date,
                Status = AttendanceStatus.Present
            };
            var grade = new CourseGrade
            {
                Id = Guid.NewGuid(),
                TenantId = TenantId,
                CourseId = course.Id,
                SemesterId = Guid.NewGuid(),
                StudentId = student.Id,
                TotalMarksObtained = 85,
                PercentageObtained = 85,
                LetterGrade = "A",
                GradePoints = 4,
                IsPassing = true,
                ResultStatus = "published"
            };
            Db.AddRange(department, batch, student, user, faculty, course, enrollment, assignment, attendance, grade);
            await Db.SaveChangesAsync();
            return new AcademicSeed(student, faculty, course);
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
    private sealed class TestHttpContextAccessor : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; }
    }


    private sealed record AcademicSeed(Student Student, Faculty Faculty, Course Course);
}
