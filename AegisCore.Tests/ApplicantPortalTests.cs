using System.Text.RegularExpressions;
using AegisCore.Application.Admissions;
using AegisCore.Application.Foundation;
using AegisCore.Application.Workflow;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Admissions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace AegisCore.Tests;

public sealed class ApplicantPortalTests
{
    private static readonly byte[] PngBytes =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
        .. new byte[64]
    ];

    private static byte[] Pdf(int size)
    {
        var bytes = new byte[size];
        bytes[0] = 0x25; bytes[1] = 0x50; bytes[2] = 0x44; bytes[3] = 0x46; // %PDF
        return bytes;
    }

    private static async Task<AegisDbContext> NewDbAsync(SqliteConnection connection)
    {
        var db = new AegisDbContext(new DbContextOptionsBuilder<AegisDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private sealed class FakeStorage : IStorageProvider
    {
        public readonly Dictionary<string, byte[]> Files = new();

        public async Task<string> UploadAsync(string path, Stream content, string contentType, CancellationToken ct = default)
        {
            using var ms = new MemoryStream();
            await content.CopyToAsync(ms, ct);
            Files[path] = ms.ToArray();
            return path;
        }

        public Task<Stream> DownloadAsync(string path, CancellationToken ct = default) => Task.FromResult<Stream>(new MemoryStream(Files[path]));
        public Task DeleteAsync(string path, CancellationToken ct = default) { Files.Remove(path); return Task.CompletedTask; }
        public Task<bool> ExistsAsync(string path, CancellationToken ct = default) => Task.FromResult(Files.ContainsKey(path));
        public Task<IReadOnlyList<string>> ListAsync(string prefix, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<string>>(Files.Keys.Where(k => k.StartsWith(prefix)).ToList());
    }

    private sealed class StubTenant(Guid id) : IApplicantTenantResolver
    {
        public Guid ResolveTenantId() => id;
    }

    private sealed class StubPhoto(PhotoValidationResult result) : IPhotoValidationService
    {
        public Task<PhotoValidationResult> ValidateAsync(Stream image, string fileName, string contentType, CancellationToken ct = default)
            => Task.FromResult(result);
    }

    private sealed class NoOpTokens : IApplicantTokenService
    {
        public string IssueToken(ApplicantProfile profile) => "test-token";
        public Task RevokeAsync(Guid applicantProfileId, CancellationToken ct = default) => Task.CompletedTask;
        public Task<bool> IsRevokedAsync(Guid applicantProfileId, CancellationToken ct = default) => Task.FromResult(false);
    }

    private sealed class NoOpNotifications : INotificationService
    {
        public Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SendInAppAsync(Guid userId, string title, string message, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SendWhatsAppAsync(string phoneNumber, string message, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<NotificationRecord> SendAsync(NotificationRecord record, CancellationToken cancellationToken = default) => Task.FromResult(record);
    }

    private static (AdmissionIntake Intake, DegreeProgram Program) SeedIntakeProgram(AegisDbContext db, Guid tenantId, string code = "BCS")
    {
        var department = new Department { Id = Guid.NewGuid(), Name = "Computing", TenantId = tenantId };
        var program = new DegreeProgram { Id = Guid.NewGuid(), Name = "BS Computer Science", Code = code, DepartmentId = department.Id, IsActive = true, TenantId = tenantId };
        var intake = new AdmissionIntake { Id = Guid.NewGuid(), Year = 2025, Semester = IntakeSemester.Fall, Status = IntakeStatus.Open, Name = "Fall 2025", TenantId = tenantId };
        db.Departments.Add(department);
        db.DegreePrograms.Add(program);
        db.AdmissionIntakes.Add(intake);
        db.SaveChanges();
        return (intake, program);
    }

    [Fact]
    public async Task ApplicantId_Matches_Format_And_Is_Unique()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = await NewDbAsync(connection);
        var tenantId = Guid.NewGuid();
        var (intake, program) = SeedIntakeProgram(db, tenantId);

        var generator = new ApplicantIdGenerator(db);
        var ids = new HashSet<string>();
        for (var i = 0; i < 50; i++)
        {
            var id = await generator.GenerateAsync(intake, program);
            Assert.Matches(new Regex("^APP-2025F-BCS-[0-9A-HJ-NP-TV-Z]{8}$"), id);
            Assert.True(ids.Add(id), "Applicant IDs must be unique.");
        }
    }

    [Fact]
    public async Task ApplicantId_Retries_On_Collision()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = await NewDbAsync(connection);
        var tenantId = Guid.NewGuid();
        var (intake, program) = SeedIntakeProgram(db, tenantId);

        var first = await new ApplicantIdGenerator(db).GenerateAsync(intake, program);
        db.ApplicantProfiles.Add(new ApplicantProfile
        {
            Id = Guid.NewGuid(), TenantId = tenantId, ApplicantId = first, Email = "a@example.com",
            IntakeId = intake.Id, ProgramId = program.Id
        });
        await db.SaveChangesAsync();

        var second = await new ApplicantIdGenerator(db).GenerateAsync(intake, program);
        Assert.NotEqual(first, second);
    }

    [Fact]
    public async Task Clip_Unreachable_Fails_Open_And_Does_Not_Block_Applicant()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = await NewDbAsync(connection);
        var tenantId = Guid.NewGuid();
        var (intake, program) = SeedIntakeProgram(db, tenantId);
        var profile = new ApplicantProfile
        {
            Id = Guid.NewGuid(), TenantId = tenantId, ApplicantId = "APP-2025F-BCS-ABCDEFGH", Email = "a@example.com",
            IntakeId = intake.Id, ProgramId = program.Id, Status = ApplicantStatus.Draft,
            FullName = "Test Applicant", DateOfBirth = new DateOnly(2005, 1, 1)
        };
        db.ApplicantProfiles.Add(profile);
        db.AdmissionDocumentRequirements.Add(new AdmissionDocumentRequirement
        {
            Id = Guid.NewGuid(), TenantId = tenantId, IntakeId = intake.Id, ProgramId = program.Id,
            DocumentType = AdmissionDocumentKind.Photo, IsRequired = false,
            AcceptedFormat = AdmissionDocumentFormat.Image, MaxFileSizeMb = 5
        });
        await db.SaveChangesAsync();

        var service = new ApplicantPortalService(
            db, new StubTenant(tenantId), new ApplicantIdGenerator(db),
            otp: null!, new NoOpTokens(),
            new StubPhoto(new PhotoValidationResult(true, null, "CLIP unreachable — flagged for manual review", true)),
            new FakeStorage(), new NoOpNotifications(), NullLogger<ApplicantPortalService>.Instance);

        var result = await service.UploadPhotoAsync(profile.Id, new MemoryStream(PngBytes), "photo.png", "image/png", PngBytes.Length);

        Assert.Equal(AdmissionDocumentValidationStatus.ManualReview, result.ValidationStatus);
        var doc = await db.ApplicantDocuments.IgnoreQueryFilters().SingleAsync(d => d.ApplicantId == profile.Id);
        Assert.Equal("failopen", doc.ValidationMethod);

        // Applicant can still submit — a fail-open photo does not block the pipeline.
        var view = await service.SubmitAsync(profile.Id);
        Assert.Equal(ApplicantStatus.Submitted, view.Status);
    }

    [Fact]
    public async Task Document_Upload_Enforces_Admin_Configured_Size_And_Format()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = await NewDbAsync(connection);
        var tenantId = Guid.NewGuid();
        var (intake, program) = SeedIntakeProgram(db, tenantId);
        var profile = new ApplicantProfile
        {
            Id = Guid.NewGuid(), TenantId = tenantId, ApplicantId = "APP-2025F-BCS-ZZZZZZZZ", Email = "a@example.com",
            IntakeId = intake.Id, ProgramId = program.Id, Status = ApplicantStatus.Draft
        };
        db.ApplicantProfiles.Add(profile);
        db.AdmissionDocumentRequirements.Add(new AdmissionDocumentRequirement
        {
            Id = Guid.NewGuid(), TenantId = tenantId, IntakeId = intake.Id, ProgramId = program.Id,
            DocumentType = AdmissionDocumentKind.Matric, IsRequired = true,
            AcceptedFormat = AdmissionDocumentFormat.Pdf, MaxFileSizeMb = 1
        });
        await db.SaveChangesAsync();

        var service = new ApplicantPortalService(
            db, new StubTenant(tenantId), new ApplicantIdGenerator(db),
            otp: null!, new NoOpTokens(),
            new StubPhoto(new PhotoValidationResult(true, 0.99, "", false)),
            new FakeStorage(), new NoOpNotifications(), NullLogger<ApplicantPortalService>.Instance);

        var tooBig = Pdf(2 * 1024 * 1024);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UploadDocumentAsync(
            profile.Id, AdmissionDocumentKind.Matric, new MemoryStream(tooBig), "m.pdf", "application/pdf", tooBig.Length));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UploadDocumentAsync(
            profile.Id, AdmissionDocumentKind.Matric, new MemoryStream(PngBytes), "m.png", "image/png", PngBytes.Length));

        var ok = Pdf(1024);
        var result = await service.UploadDocumentAsync(
            profile.Id, AdmissionDocumentKind.Matric, new MemoryStream(ok), "m.pdf", "application/pdf", ok.Length);
        Assert.Equal(AdmissionDocumentKind.Matric, result.DocumentType);
    }

    [Fact]
    public async Task Admin_Transition_Rejects_Illegal_Pipeline_Moves()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = await NewDbAsync(connection);
        var profile = new ApplicantProfile
        {
            Id = Guid.NewGuid(), TenantId = Guid.Empty, ApplicantId = "APP-2025F-BBA-11112222", Email = "b@example.com",
            IntakeId = Guid.NewGuid(), ProgramId = Guid.NewGuid(), Status = ApplicantStatus.Draft
        };
        db.ApplicantProfiles.Add(profile);
        await db.SaveChangesAsync();

        var service = new AdmissionsAdminService(db, new FakeStorage());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.TransitionAsync(profile.Id, ApplicantStatus.Admitted, null));

        await service.TransitionAsync(profile.Id, ApplicantStatus.Submitted, null);
        await service.TransitionAsync(profile.Id, ApplicantStatus.UnderReview, "reviewing");
        var reloaded = await db.ApplicantProfiles.SingleAsync(p => p.Id == profile.Id);
        Assert.Equal(ApplicantStatus.UnderReview, reloaded.Status);
    }

    [Fact]
    public void Clip_Client_Fails_Open_When_Service_Url_Not_Configured()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var service = new ClipPhotoValidationService(new StubHttpClientFactory(), config, NullLogger<ClipPhotoValidationService>.Instance);

        var result = service.ValidateAsync(new MemoryStream(PngBytes), "photo.png", "image/png").GetAwaiter().GetResult();

        Assert.True(result.Passed);
        Assert.True(result.RequiresManualReview);
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }

    [Fact]
    public async Task Applicant_Token_Carries_Applicant_Audience()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Development", // an HMAC secret is accepted in Development only
            ["AEGISCORE_JWT_SECRET"] = "unit-test-signing-secret-value-that-is-long-enough"
        }).Build();

        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = await NewDbAsync(connection);
        _ = db;

        var tokenService = new ApplicantTokenService(config, redis: null!);
        var token = tokenService.IssueToken(new ApplicantProfile
        {
            Id = Guid.NewGuid(), TenantId = Guid.NewGuid(), ApplicantId = "APP-2025F-BCS-AAAABBBB", Email = "x@example.com"
        });

        var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Contains("applicant", jwt.Audiences);
        Assert.Equal("https://aegiscore.pk", jwt.Issuer);
        Assert.Contains(jwt.Claims, c => c.Type == "applicant_id" && c.Value == "APP-2025F-BCS-AAAABBBB");
    }
}
