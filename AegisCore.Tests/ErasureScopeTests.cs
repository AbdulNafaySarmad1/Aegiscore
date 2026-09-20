using AegisCore.Application.Compliance;
using AegisCore.Application.Foundation;
using AegisCore.Contracts.Abstractions.Cloud;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure.Compliance;
using AegisCore.Infrastructure.Security.Pqc;
using AegisCore.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace AegisCore.Tests;

/// <summary>
/// Compliance control AC-38 (docs/compliance/09-retention-policy.md): erasure reaches welfare
/// notes, applicant identity and documents, travel documents and sign-in records; a subject
/// restored from an older backup is erased again; retention honours legal holds.
/// </summary>
public sealed class ErasureScopeTests
{
    private static PqcCryptographyService Encryption() =>
        new(Options.Create(new PqcCryptographyOptions { MasterKeyBase64 = TestKeys.MasterKeyBase64 }), NullLogger<PqcCryptographyService>.Instance);

    private static PrivacyComplianceService Service(AegisCore.Infrastructure.AegisDbContext db, IStorageProvider? files = null) =>
        new(db, Encryption(), Mock.Of<IStorageService>(), new ConfigurationBuilder().Build(), NullLogger<PrivacyComplianceService>.Instance, files);

    private static async Task<Guid> SeedStudentAsync(AegisCore.Infrastructure.AegisDbContext db, Guid tenant)
    {
        var department = new Department { Id = Guid.NewGuid(), TenantId = tenant, Name = "Computing", Code = "CMP-" + Guid.NewGuid().ToString("N")[..6] };
        var batch = new Batch { Id = Guid.NewGuid(), TenantId = tenant, Name = "2026", StartYear = 2026, EndYear = 2030, DepartmentId = department.Id };
        var student = new Student { Id = Guid.NewGuid(), TenantId = tenant, FirstName = "Sana", LastName = "Malik", BatchId = batch.Id, EnrollmentStatus = EnrollmentStatus.Active };
        db.AddRange(department, batch, student);
        db.Users.Add(new User { Id = student.Id, TenantId = tenant, Username = "sana", Email = "sana@example.edu", FirstName = "Sana", LastName = "Malik", Status = UserStatus.Active });
        await db.SaveChangesAsync();
        return student.Id;
    }

    [Theory, DatabaseProviders]
    public async Task Erasure_removes_welfare_notes_applicant_identity_travel_documents_and_sign_in_records(bool postgres)
    {
        var tenant = Guid.NewGuid();
        await using var database = await TestDatabase.CreateAsync(postgres);
        await database.EnsureSchemaAsync(tenant);
        await using var db = database.CreateContext(tenant);
        var student = await SeedStudentAsync(db, tenant);
        var files = new Mock<IStorageProvider>();
        files.Setup(f => f.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var service = Service(db, files.Object);

        db.StudentCounselings.Add(new StudentCounseling { Id = Guid.NewGuid(), TenantId = tenant, StudentId = student, Notes = "private session notes", SessionDate = DateTime.UtcNow, CounselorUserId = Guid.NewGuid() });
        var discipline = new StudentDiscipline { Id = Guid.NewGuid(), TenantId = tenant, StudentId = student, Severity = DisciplineSeverity.Minor, Status = DisciplineStatus.Resolved, Description = "narrative of the incident", IncidentDate = DateTime.UtcNow };
        db.StudentDisciplines.Add(discipline);
        var applicant = new ApplicantProfile { Id = Guid.NewGuid(), TenantId = tenant, ApplicantId = "APP-1", Email = "sana@example.edu", IntakeId = Guid.NewGuid(), ProgramId = Guid.NewGuid(), Status = ApplicantStatus.Admitted, FullName = "Sana Malik", Phone = "0300", ConvertedStudentId = student, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
        db.ApplicantProfiles.Add(applicant);
        db.ApplicantDocuments.Add(new ApplicantDocument { Id = Guid.NewGuid(), TenantId = tenant, ApplicantId = applicant.Id, DocumentType = AdmissionDocumentKind.Matric, StoragePath = "tenants/t/applicants/APP-1/matric.pdf", UploadedAt = DateTimeOffset.UtcNow, FileSize = 10, MimeType = "application/pdf", ValidationStatus = AdmissionDocumentValidationStatus.Pending });
        var registry = new ForeignStudentRegistry { Id = Guid.NewGuid(), TenantId = tenant, StudentId = student, Nationality = "Afghan", PassportNumber = "P1234567", VisaNumber = "V99", Program = "BS", YearOfStudy = 1 };
        db.ForeignStudentRegistries.Add(registry);
        db.VisaDocuments.Add(new VisaDocument { Id = Guid.NewGuid(), TenantId = tenant, RegistryId = registry.Id, DocumentType = "visa", FileUrl = "tenants/t/visa/v99.pdf", UploadedAt = DateTime.UtcNow });
        db.LoginAuditRecords.Add(new LoginAuditRecord { AuditId = Guid.NewGuid(), TenantId = tenant, UserId = student, UserEmail = "sana@example.edu", UserRole = "Student", EventType = LoginEvent.Login, Provider = LoginProvider.Local, IpAddress = "198.51.100.8", IsSuccessful = true, OccurredAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var request = await service.RequestErasureAsync(student);
        await service.ProcessErasureAsync(request.Id);
        db.ChangeTracker.Clear();

        Assert.Empty(await db.StudentCounselings.IgnoreQueryFilters().Where(c => c.StudentId == student).ToListAsync());
        Assert.Null((await db.StudentDisciplines.IgnoreQueryFilters().SingleAsync(d => d.Id == discipline.Id)).Description);
        var erasedApplicant = await db.ApplicantProfiles.IgnoreQueryFilters().SingleAsync(a => a.Id == applicant.Id);
        Assert.DoesNotContain("Sana", erasedApplicant.FullName);
        Assert.Null(erasedApplicant.Phone);
        Assert.Empty(await db.ApplicantDocuments.IgnoreQueryFilters().Where(d => d.ApplicantId == applicant.Id).ToListAsync());
        files.Verify(f => f.DeleteAsync("tenants/t/applicants/APP-1/matric.pdf", It.IsAny<CancellationToken>()), Times.Once);
        files.Verify(f => f.DeleteAsync("tenants/t/visa/v99.pdf", It.IsAny<CancellationToken>()), Times.Once);
        var erasedRegistry = await db.ForeignStudentRegistries.IgnoreQueryFilters().SingleAsync(r => r.Id == registry.Id);
        Assert.Null(erasedRegistry.PassportNumber);
        Assert.Null(erasedRegistry.VisaNumber);
        Assert.Empty(await db.VisaDocuments.IgnoreQueryFilters().Where(v => v.RegistryId == registry.Id).ToListAsync());
        Assert.Empty(await db.LoginAuditRecords.IgnoreQueryFilters().Where(l => l.UserId == student).ToListAsync());
        Assert.Equal(ErasureRequestStatus.Completed, (await db.ErasureRequests.IgnoreQueryFilters().SingleAsync(r => r.Id == request.Id)).Status);
    }

    [Theory, DatabaseProviders]
    public async Task A_subject_restored_from_an_older_backup_is_erased_again(bool postgres)
    {
        var tenant = Guid.NewGuid();
        await using var database = await TestDatabase.CreateAsync(postgres);
        await database.EnsureSchemaAsync(tenant);
        await using var db = database.CreateContext(tenant);
        var student = await SeedStudentAsync(db, tenant);
        var service = Service(db);

        var request = await service.RequestErasureAsync(student);
        await service.ProcessErasureAsync(request.Id);
        db.ChangeTracker.Clear();
        Assert.StartsWith("ERASED_USER_", (await db.Students.IgnoreQueryFilters().SingleAsync(s => s.Id == student)).FirstName);

        // "Restore": the identifiable row comes back while the request still says Completed.
        var restored = await db.Students.IgnoreQueryFilters().SingleAsync(s => s.Id == student);
        restored.FirstName = "Ayesha";
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        await service.ProcessPendingErasureRequestsAsync();
        db.ChangeTracker.Clear();

        Assert.StartsWith("ERASED_USER_", (await db.Students.IgnoreQueryFilters().SingleAsync(s => s.Id == student)).FirstName);
        Assert.Equal(ErasureRequestStatus.Completed, (await db.ErasureRequests.IgnoreQueryFilters().SingleAsync(r => r.Id == request.Id)).Status);
    }

    [Theory, DatabaseProviders]
    public async Task Retention_removes_old_sign_ins_visitors_and_closed_applicants_but_honours_legal_holds(bool postgres)
    {
        var tenant = Guid.NewGuid();
        await using var database = await TestDatabase.CreateAsync(postgres);
        await database.EnsureSchemaAsync(tenant);
        await using var db = database.CreateContext(tenant);
        var service = Service(db);
        var now = DateTime.UtcNow;
        var heldUser = Guid.NewGuid();

        db.LoginAuditRecords.AddRange(
            new LoginAuditRecord { AuditId = Guid.NewGuid(), TenantId = tenant, UserId = Guid.NewGuid(), UserEmail = "old@x", UserRole = "S", EventType = LoginEvent.Login, Provider = LoginProvider.Local, IpAddress = "1.1.1.1", IsSuccessful = true, OccurredAt = now.AddDays(-400) },
            new LoginAuditRecord { AuditId = Guid.NewGuid(), TenantId = tenant, UserId = heldUser, UserEmail = "held@x", UserRole = "S", EventType = LoginEvent.Login, Provider = LoginProvider.Local, IpAddress = "1.1.1.2", IsSuccessful = true, OccurredAt = now.AddDays(-400) },
            new LoginAuditRecord { AuditId = Guid.NewGuid(), TenantId = tenant, UserId = Guid.NewGuid(), UserEmail = "new@x", UserRole = "S", EventType = LoginEvent.Login, Provider = LoginProvider.Local, IpAddress = "1.1.1.3", IsSuccessful = true, OccurredAt = now.AddDays(-10) });
        db.VisitorLogs.AddRange(
            new VisitorLog { Id = Guid.NewGuid(), TenantId = tenant, VisitorCNIC = "1", EntryTime = now.AddDays(-120) },
            new VisitorLog { Id = Guid.NewGuid(), TenantId = tenant, VisitorCNIC = "2", EntryTime = now.AddDays(-5) });
        db.ApplicantProfiles.AddRange(
            new ApplicantProfile { Id = Guid.NewGuid(), TenantId = tenant, ApplicantId = "A-old", Email = "a@x", IntakeId = Guid.NewGuid(), ProgramId = Guid.NewGuid(), Status = ApplicantStatus.Rejected, CreatedAt = now.AddYears(-3), UpdatedAt = now.AddYears(-3) },
            new ApplicantProfile { Id = Guid.NewGuid(), TenantId = tenant, ApplicantId = "A-admitted", Email = "b@x", IntakeId = Guid.NewGuid(), ProgramId = Guid.NewGuid(), Status = ApplicantStatus.Admitted, CreatedAt = now.AddYears(-3), UpdatedAt = now.AddYears(-3) });
        await db.SaveChangesAsync();

        var deleted = await service.ApplyRetentionAsync(tenant, now, new RetentionSettings(LegalHoldIds: new HashSet<Guid> { heldUser }));
        db.ChangeTracker.Clear();

        Assert.Equal(3, deleted);
        var logins = await db.LoginAuditRecords.IgnoreQueryFilters().Select(l => l.UserEmail).ToListAsync();
        Assert.Equal(new[] { "held@x", "new@x" }, logins.OrderBy(x => x).ToArray());
        Assert.Equal(new[] { "2" }, await db.VisitorLogs.IgnoreQueryFilters().Select(v => v.VisitorCNIC).ToArrayAsync());
        Assert.Equal(new[] { "A-admitted" }, await db.ApplicantProfiles.IgnoreQueryFilters().Select(a => a.ApplicantId).ToArrayAsync());
    }
}
