using AegisCore.Application.Admissions;
using AegisCore.Application.Workflow;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Admissions;
using AegisCore.Infrastructure.Config;
using MediatR;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace AegisCore.Tests;

public sealed class AdmissionMigrationTests
{
    private static async Task<AegisDbContext> NewDbAsync(SqliteConnection connection)
    {
        var db = new AegisDbContext(new DbContextOptionsBuilder<AegisDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private sealed record Seeded(AdmissionIntake Intake, DegreeProgram Program, Batch Batch, Role StudentRole);

    private static Seeded SeedCore(AegisDbContext db, string programCode = "BCS")
    {
        var department = new Department { Id = Guid.NewGuid(), Name = "Computing", TenantId = Guid.Empty };
        var batch = new Batch { Id = Guid.NewGuid(), Name = "2025", StartYear = 2025, EndYear = 2029, DepartmentId = department.Id, TenantId = Guid.Empty };
        var program = new DegreeProgram { Id = Guid.NewGuid(), Name = "BS CS", Code = programCode, DepartmentId = department.Id, IsActive = true, TenantId = Guid.Empty };
        var intake = new AdmissionIntake { Id = Guid.NewGuid(), Year = 2025, Semester = IntakeSemester.Fall, Status = IntakeStatus.Open, Name = "Fall 2025", BatchId = batch.Id, TenantId = Guid.Empty };
        var role = new Role { Id = Guid.NewGuid(), Name = "Student", TenantId = Guid.Empty };
        db.Departments.Add(department);
        db.Batches.Add(batch);
        db.DegreePrograms.Add(program);
        db.AdmissionIntakes.Add(intake);
        db.Roles.Add(role);
        db.SaveChanges();
        return new Seeded(intake, program, batch, role);
    }

    private static ApplicantProfile SeedApplicant(AegisDbContext db, Seeded s, string appId = "APP-2025F-BCS-AAAABBBB")
    {
        var profile = new ApplicantProfile
        {
            Id = Guid.NewGuid(), TenantId = Guid.Empty, ApplicantId = appId, Email = "aisha.khan@gmail.com",
            IntakeId = s.Intake.Id, ProgramId = s.Program.Id, Status = ApplicantStatus.UnderReview,
            FullName = "Aisha Khan"
        };
        db.ApplicantProfiles.Add(profile);
        db.SaveChanges();
        return profile;
    }

    private sealed class CapturingPublisher : IPublisher
    {
        public readonly List<object> Published = [];
        public Task Publish(object notification, CancellationToken cancellationToken = default) { Published.Add(notification); return Task.CompletedTask; }
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
        { Published.Add(notification); return Task.CompletedTask; }
    }

    private sealed class CapturingNotifications : INotificationService
    {
        public readonly List<(string To, string Subject, string Body)> Emails = [];
        public Task SendEmailAsync(string to, string subject, string body, CancellationToken ct = default) { Emails.Add((to, subject, body)); return Task.CompletedTask; }
        public Task SendSmsAsync(string phoneNumber, string message, CancellationToken ct = default) => Task.CompletedTask;
        public Task SendInAppAsync(Guid userId, string title, string message, CancellationToken ct = default) => Task.CompletedTask;
        public Task SendWhatsAppAsync(string phoneNumber, string message, CancellationToken ct = default) => Task.CompletedTask;
        public Task<NotificationRecord> SendAsync(NotificationRecord record, CancellationToken ct = default) => Task.FromResult(record);
    }

    private sealed class CapturingApplicantTokens : IApplicantTokenService
    {
        public readonly List<Guid> Revoked = [];
        public string IssueToken(ApplicantProfile profile) => "t";
        public Task RevokeAsync(Guid applicantProfileId, CancellationToken ct = default) { Revoked.Add(applicantProfileId); return Task.CompletedTask; }
        public Task<bool> IsRevokedAsync(Guid applicantProfileId, CancellationToken ct = default) => Task.FromResult(Revoked.Contains(applicantProfileId));
    }

    private static TenantConfigService TenantConfig()
        => new(Mock.Of<AegisCore.Application.ConfigCenter.IConfigurationCenterService>(), NullLogger<TenantConfigService>.Instance);

    [Fact]
    public async Task Roll_Numbers_Increment_Per_Program_Intake()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = await NewDbAsync(connection);
        var s = SeedCore(db);

        var service = new RollNumberService(db);
        var first = await service.AllocateAsync(s.Intake, s.Program);
        var second = await service.AllocateAsync(s.Intake, s.Program);
        var third = await service.AllocateAsync(s.Intake, s.Program);

        Assert.Equal("2025F-BCS-1", first);
        Assert.Equal("2025F-BCS-2", second);
        Assert.Equal("2025F-BCS-3", third);
    }

    [Fact]
    public async Task Roll_Number_Allocation_Is_Collision_Safe_Across_Contexts()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using (var seed = await NewDbAsync(connection))
        {
            SeedCore(seed);
        }

        // Two independent contexts allocate in turn; neither may reuse a value.
        var issued = new HashSet<string>();
        for (var i = 0; i < 10; i++)
        {
            await using var ctx = new AegisDbContext(new DbContextOptionsBuilder<AegisDbContext>().UseSqlite(connection).Options);
            var intake = await ctx.AdmissionIntakes.IgnoreQueryFilters().FirstAsync();
            var program = await ctx.DegreePrograms.IgnoreQueryFilters().FirstAsync();
            var roll = await new RollNumberService(ctx).AllocateAsync(intake, program);
            Assert.True(issued.Add(roll), $"Duplicate roll number {roll}");
        }

        Assert.Contains("2025F-BCS-10", issued);
    }

    [Fact]
    public async Task Admit_Creates_Student_And_User_Retains_App_Id_And_Publishes_Event()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = await NewDbAsync(connection);
        var s = SeedCore(db);
        var applicant = SeedApplicant(db, s);

        var publisher = new CapturingPublisher();
        var service = new AdmissionDecisionService(db, new RollNumberService(db), publisher, NullLogger<AdmissionDecisionService>.Instance);

        var result = await service.AdmitAsync(applicant.Id, Guid.NewGuid(), "aisha.khan@testuniv.edu.pk");

        Assert.Equal("2025F-BCS-1", result.RollNumber);

        var student = await db.Students.IgnoreQueryFilters().SingleAsync(x => x.Id == result.StudentId);
        Assert.Equal("2025F-BCS-1", student.RollNumber);
        Assert.Equal("2025F-BCS-1", student.UniversityId);
        Assert.Equal("APP-2025F-BCS-AAAABBBB", student.ApplicantId);
        Assert.Equal(applicant.Id, student.AdmissionApplicantProfileId);
        Assert.Equal(s.Batch.Id, student.BatchId);

        var user = await db.Users.IgnoreQueryFilters().SingleAsync(x => x.Id == result.UserId);
        Assert.Equal(student.Id, user.Id);
        Assert.Equal("aisha.khan@gmail.com", user.RecoveryEmail);   // personal retained
        Assert.True(await db.UserRoles.IgnoreQueryFilters().AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == s.StudentRole.Id));

        var reloaded = await db.ApplicantProfiles.IgnoreQueryFilters().SingleAsync(x => x.Id == applicant.Id);
        Assert.Equal(ApplicantStatus.Admitted, reloaded.Status);
        Assert.Equal(student.Id, reloaded.ConvertedStudentId);

        Assert.True(await db.DomainEvents.IgnoreQueryFilters().AnyAsync(e => e.EventType == "AdmissionConfirmed" && e.AggregateId == applicant.Id.ToString()));
        Assert.Single(publisher.Published);
        Assert.IsType<AdmissionConfirmedNotification>(publisher.Published[0]);

        // Second admit is rejected.
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AdmitAsync(applicant.Id, Guid.NewGuid(), "x@y.edu.pk"));
    }

    [Fact]
    public async Task Handler_Migrates_Account_Personal_Email_Becomes_Recovery_Manual_Mode()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = await NewDbAsync(connection);

        var user = new User
        {
            Id = Guid.NewGuid(), TenantId = Guid.Empty, Username = "APP-2025F-BCS-AAAABBBB",
            Email = "aisha.khan@gmail.com", RecoveryEmail = "aisha.khan@gmail.com", FirstName = "Aisha", LastName = "Khan",
            Status = UserStatus.Active
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var tokens = new CapturingApplicantTokens();
        var emails = new CapturingNotifications();
        var factory = new InstitutionalEmailProvisionerFactory([new ManualEmailProvisioner()]);
        var protector = new EmailCredentialProtector(new EphemeralDataProtectionProvider());
        var handler = new AdmissionConfirmedHandler(db, protector, factory, tokens, emails, TenantConfig(), NullLogger<AdmissionConfirmedHandler>.Instance);

        var applicantProfileId = Guid.NewGuid();
        await handler.Handle(new AdmissionConfirmedNotification(
            Guid.Empty, applicantProfileId, Guid.NewGuid(), user.Id, "2025F-BCS-1", "aisha.khan@gmail.com",
            "Aisha", "Khan", Guid.NewGuid(), Guid.NewGuid(), "aisha.khan@testuniv.edu.pk"), CancellationToken.None);

        var reloaded = await db.Users.IgnoreQueryFilters().SingleAsync(u => u.Id == user.Id);
        Assert.Equal("aisha.khan@testuniv.edu.pk", reloaded.Email);
        Assert.Equal("aisha.khan@gmail.com", reloaded.RecoveryEmail);

        Assert.Contains(applicantProfileId, tokens.Revoked);
        Assert.Contains(emails.Emails, e => e.To == "aisha.khan@testuniv.edu.pk" && e.Subject.Contains("Welcome"));
        Assert.Contains(emails.Emails, e => e.To == "aisha.khan@gmail.com" && e.Body.Contains("recovery address only"));
    }

    [Fact]
    public async Task Email_Provisioning_Config_Encrypts_Credentials()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = await NewDbAsync(connection);

        var protector = new EmailCredentialProtector(new EphemeralDataProtectionProvider());
        var service = new EmailProvisioningConfigService(db, protector);

        const string creds = "{\"tenantId\":\"t\",\"clientId\":\"c\",\"clientSecret\":\"s\"}";
        var view = await service.SetAsync(new EmailProvisioningConfigInput(EmailProvisioningMode.Microsoft365, "univ.edu.pk", creds), Guid.NewGuid());

        Assert.Equal(EmailProvisioningMode.Microsoft365, view.Mode);
        Assert.True(view.HasCredentials);

        var stored = await db.EmailProvisioningConfigs.IgnoreQueryFilters().SingleAsync();
        Assert.NotNull(stored.CredentialsCipher);
        Assert.DoesNotContain("clientSecret", stored.CredentialsCipher!);
        Assert.Equal(creds, protector.Unprotect(stored.CredentialsCipher!));
    }

    [Fact]
    public async Task Pipeline_Board_Maps_Status_To_Columns()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = await NewDbAsync(connection);
        var s = SeedCore(db);

        void Add(string appId, ApplicantStatus status, Guid? student = null) => db.ApplicantProfiles.Add(new ApplicantProfile
        {
            Id = Guid.NewGuid(), TenantId = Guid.Empty, ApplicantId = appId, Email = $"{appId}@x.com",
            IntakeId = s.Intake.Id, ProgramId = s.Program.Id, Status = status, ConvertedStudentId = student, FullName = appId
        });
        Add("APP-2025F-BCS-00000001", ApplicantStatus.Submitted);
        Add("APP-2025F-BCS-00000002", ApplicantStatus.UnderReview);
        Add("APP-2025F-BCS-00000003", ApplicantStatus.InterviewScheduled);
        Add("APP-2025F-BCS-00000004", ApplicantStatus.Admitted, Guid.NewGuid());
        Add("APP-2025F-BCS-00000005", ApplicantStatus.Draft);
        await db.SaveChangesAsync();

        var board = new AdmissionPipelineService(db, null!, null!);
        var result = await board.GetBoardAsync(null);

        Assert.Equal(AdmissionPipelineService.BoardColumns, result.Columns);
        Assert.Equal("Applied", result.Cards.Single(c => c.ApplicantId.EndsWith("01")).Column);
        Assert.Equal("Documents Verified", result.Cards.Single(c => c.ApplicantId.EndsWith("02")).Column);
        Assert.Equal("Interview", result.Cards.Single(c => c.ApplicantId.EndsWith("03")).Column);
        Assert.Equal("Enrolled", result.Cards.Single(c => c.ApplicantId.EndsWith("04")).Column);
        Assert.DoesNotContain(result.Cards, c => c.ApplicantId.EndsWith("05")); // Draft not on the board
    }
}
