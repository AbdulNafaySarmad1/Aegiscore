using System.Text.Json;
using AegisCore.Application.Admissions;
using AegisCore.Application.Workflow;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure.Config;
using MediatR;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Admissions;

public sealed class RollNumberService(AegisDbContext db) : IRollNumberService
{
    public async Task<string> AllocateAsync(AdmissionIntake intake, DegreeProgram program, CancellationToken ct = default)
    {
        var code = (program.Code ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(code))
        {
            throw new InvalidOperationException($"Program '{program.Name}' has no short code configured.");
        }

        for (var attempt = 0; attempt < 8; attempt++)
        {
            var row = await db.ProgramIntakeSequences
                .FirstOrDefaultAsync(s => s.IntakeId == intake.Id && s.ProgramId == program.Id, ct);

            int value;
            if (row is null)
            {
                value = 1;
                db.ProgramIntakeSequences.Add(new ProgramIntakeSequence
                {
                    Id = Guid.NewGuid(),
                    TenantId = db.CurrentTenantId,
                    IntakeId = intake.Id,
                    ProgramId = program.Id,
                    NextValue = value + 1,
                    ConcurrencyStamp = Guid.NewGuid()
                });
            }
            else
            {
                value = row.NextValue;
                row.NextValue = value + 1;
                row.ConcurrencyStamp = Guid.NewGuid();
            }

            try
            {
                await db.SaveChangesAsync(ct);
                return $"{intake.Year}{intake.SemesterCode}-{code}-{value}";
            }
            catch (DbUpdateException) when (attempt < 7)
            {
                // Concurrency token mismatch or unique-index race — reset and retry.
                foreach (var entry in db.ChangeTracker.Entries<ProgramIntakeSequence>().ToList())
                {
                    await entry.ReloadAsync(ct);
                    entry.State = EntityState.Detached;
                }
            }
        }

        throw new InvalidOperationException("Could not allocate a roll number under concurrent load.");
    }
}

public sealed class AdmissionDecisionService(
    AegisDbContext db,
    IRollNumberService rollNumbers,
    IPublisher publisher,
    ILogger<AdmissionDecisionService> logger) : IAdmissionDecisionService
{
    public async Task<AdmitResult> AdmitAsync(Guid applicantProfileId, Guid adminUserId, string? manualInstitutionalEmail, CancellationToken ct = default)
    {
        var profile = await db.ApplicantProfiles.FirstOrDefaultAsync(p => p.Id == applicantProfileId, ct)
            ?? throw new InvalidOperationException("Applicant profile was not found.");

        if (profile.ConvertedStudentId is not null || profile.Status == ApplicantStatus.Admitted)
        {
            throw new InvalidOperationException("This applicant has already been admitted.");
        }

        if (profile.Status is ApplicantStatus.Rejected or ApplicantStatus.Withdrawn or ApplicantStatus.Draft)
        {
            throw new InvalidOperationException($"An applicant in status {profile.Status} cannot be admitted.");
        }

        var intake = await db.AdmissionIntakes.FirstOrDefaultAsync(i => i.Id == profile.IntakeId, ct)
            ?? throw new InvalidOperationException("The applicant's intake was not found.");
        var program = await db.DegreePrograms.FirstOrDefaultAsync(p => p.Id == profile.ProgramId, ct)
            ?? throw new InvalidOperationException("The applicant's program was not found.");

        if (intake.BatchId is not { } batchId || batchId == Guid.Empty)
        {
            throw new InvalidOperationException("Set the enrolment batch on this intake before admitting applicants.");
        }

        var rollNumber = await rollNumbers.AllocateAsync(intake, program, ct);
        var (firstName, lastName) = SplitName(profile.FullName);

        var student = new Student
        {
            Id = Guid.NewGuid(),
            TenantId = profile.TenantId,
            FirstName = firstName,
            LastName = lastName,
            BatchId = batchId,
            RollNumber = rollNumber,
            UniversityId = rollNumber,
            ApplicantId = profile.ApplicantId,
            AdmissionApplicantProfileId = profile.Id,
            EnrollmentStatus = EnrollmentStatus.Active
        };
        db.Students.Add(student);

        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = profile.TenantId,
            Username = profile.ApplicantId,
            Email = profile.Email,          // personal for now — the handler swaps in the institutional address
            RecoveryEmail = profile.Email,  // personal email is retained as recovery
            FirstName = firstName,
            LastName = lastName,
            Status = UserStatus.Active,
            AuthProvider = AuthProvider.Local
        };
        db.Users.Add(user);

        var studentRole = await db.Roles.FirstOrDefaultAsync(r => r.TenantId == profile.TenantId && r.Name == "Student", ct);
        if (studentRole is not null)
        {
            db.UserRoles.Add(new UserRole
            {
                Id = Guid.NewGuid(), TenantId = profile.TenantId, UserId = user.Id, RoleId = studentRole.Id,
                AssignedByUserId = adminUserId, AssignedAt = DateTime.UtcNow
            });
        }
        else
        {
            logger.LogWarning("No 'Student' role found for tenant {TenantId}; student {UserId} created without a role.", profile.TenantId, user.Id);
        }

        profile.Status = ApplicantStatus.Admitted;
        profile.ConvertedStudentId = student.Id;
        profile.UpdatedAt = DateTimeOffset.UtcNow;

        var notification = new AdmissionConfirmedNotification(
            profile.TenantId, profile.Id, student.Id, user.Id, rollNumber, profile.Email,
            firstName, lastName, program.Id, intake.Id, manualInstitutionalEmail);

        db.DomainEvents.Add(new DomainEvent
        {
            Id = Guid.NewGuid(),
            TenantId = profile.TenantId,
            EventType = "AdmissionConfirmed",
            AggregateId = profile.Id.ToString(),
            SourceModule = "Admissions",
            PayloadJson = JsonSerializer.Serialize(notification),
            Status = EventStatus.Published,
            OccurredAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(ct);

        await publisher.Publish(notification, ct);

        return new AdmitResult(student.Id, rollNumber, user.Id);
    }

    private static (string First, string Last) SplitName(string? fullName)
    {
        var parts = (fullName ?? string.Empty).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => ("Student", "Applicant"),
            1 => (parts[0], parts[0]),
            _ => (parts[0], string.Join(' ', parts[1..]))
        };
    }
}

/// <summary>
/// The single handler for <see cref="AdmissionConfirmedNotification"/>: provisions the
/// institutional email, migrates the account (institutional primary, personal → recovery),
/// invalidates the applicant session, and sends the dual welcome emails.
/// </summary>
public sealed class AdmissionConfirmedHandler(
    AegisDbContext db,
    IEmailCredentialProtector protector,
    IInstitutionalEmailProvisionerFactory provisionerFactory,
    IApplicantTokenService applicantTokens,
    INotificationService notifications,
    TenantConfigService tenantConfig,
    ILogger<AdmissionConfirmedHandler> logger) : INotificationHandler<AdmissionConfirmedNotification>
{
    public async Task Handle(AdmissionConfirmedNotification n, CancellationToken ct)
    {
        try
        {
            var config = await db.EmailProvisioningConfigs.IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.TenantId == n.TenantId, ct);
            var mode = config?.Mode ?? EmailProvisioningMode.Manual;

            string institutionalEmail;
            if (mode == EmailProvisioningMode.Manual)
            {
                institutionalEmail = n.ManualInstitutionalEmail?.Trim()
                    ?? throw new InvalidOperationException("Manual provisioning requires an institutional email at admission time.");
            }
            else
            {
                var credsJson = string.IsNullOrEmpty(config?.CredentialsCipher) ? null : protector.Unprotect(config!.CredentialsCipher!);
                var provisioner = provisionerFactory.Resolve(mode);
                var provisioned = await provisioner.ProvisionAsync(
                    new ProvisionRequest(n.FirstName, n.LastName, 0, config?.DomainName ?? string.Empty, credsJson), ct);
                institutionalEmail = provisioned.Address;
            }

            var user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == n.UserId, ct);
            if (user is not null)
            {
                user.Email = institutionalEmail;          // primary login becomes institutional
                user.Username = institutionalEmail;
                user.RecoveryEmail = n.ApplicantEmail;    // personal email retained for reset only
                await db.SaveChangesAsync(ct);
            }

            // Applicant portal session is invalidated.
            await applicantTokens.RevokeAsync(n.ApplicantProfileId, ct);

            var university = tenantConfig.GetUniversityName(n.TenantId);
            await notifications.SendEmailAsync(institutionalEmail,
                $"Welcome to {university}",
                $"<p>Welcome to {university}. Your student account is ready. Your roll number is <strong>{n.RollNumber}</strong>.</p>", ct);
            await notifications.SendEmailAsync(n.ApplicantEmail,
                "Your application has been accepted",
                $"<p>Your application has been accepted. Your university email is <strong>{institutionalEmail}</strong>. " +
                "Your personal email is now your recovery address only.</p>", ct);
        }
        catch (Exception ex)
        {
            // The admission itself is already committed; provisioning can be retried by an admin.
            logger.LogError(ex, "Post-admission migration failed for applicant {ApplicantProfileId}", n.ApplicantProfileId);
        }
    }
}

public sealed class EmailProvisioningConfigService(AegisDbContext db, IEmailCredentialProtector protector) : IEmailProvisioningConfigService
{
    private Guid TenantId => db.CurrentTenantId;

    public async Task<EmailProvisioningConfigView?> GetAsync(CancellationToken ct = default)
    {
        var row = await db.EmailProvisioningConfigs.FirstOrDefaultAsync(c => c.TenantId == TenantId, ct);
        return row is null ? null : new EmailProvisioningConfigView(row.Mode, row.DomainName, !string.IsNullOrEmpty(row.CredentialsCipher), row.UpdatedAt);
    }

    public async Task<EmailProvisioningConfigView> SetAsync(EmailProvisioningConfigInput input, Guid actorId, CancellationToken ct = default)
    {
        if (input.Mode != EmailProvisioningMode.Manual && string.IsNullOrWhiteSpace(input.DomainName))
        {
            throw new InvalidOperationException("Google Workspace and Microsoft 365 modes require a domain name.");
        }

        var row = await db.EmailProvisioningConfigs.FirstOrDefaultAsync(c => c.TenantId == TenantId, ct);
        if (row is null)
        {
            row = new EmailProvisioningConfig { Id = Guid.NewGuid(), TenantId = TenantId };
            db.EmailProvisioningConfigs.Add(row);
        }

        row.Mode = input.Mode;
        row.DomainName = input.DomainName?.Trim();
        if (!string.IsNullOrWhiteSpace(input.CredentialsJson))
        {
            row.CredentialsCipher = protector.Protect(input.CredentialsJson);
        }
        else if (input.Mode == EmailProvisioningMode.Manual)
        {
            row.CredentialsCipher = null;
        }

        row.UpdatedAt = DateTimeOffset.UtcNow;
        row.UpdatedBy = actorId;
        await db.SaveChangesAsync(ct);
        return new EmailProvisioningConfigView(row.Mode, row.DomainName, !string.IsNullOrEmpty(row.CredentialsCipher), row.UpdatedAt);
    }
}

/// <summary>Protects institutional-email provider credentials at rest with the ASP.NET Data Protection key ring.</summary>
public interface IEmailCredentialProtector
{
    string Protect(string plaintext);
    string Unprotect(string ciphertext);
}

public sealed class EmailCredentialProtector : IEmailCredentialProtector
{
    private readonly IDataProtector _protector;
    public EmailCredentialProtector(IDataProtectionProvider provider) => _protector = provider.CreateProtector("AegisCore.Admissions.EmailProvisioning.v1");
    public string Protect(string plaintext) => _protector.Protect(plaintext);
    public string Unprotect(string ciphertext) => _protector.Unprotect(ciphertext);
}

public sealed class AdmissionPipelineService(
    AegisDbContext db,
    IAdmissionsAdminService admin,
    IAdmissionDecisionService decisions) : IAdmissionPipelineService
{
    public static readonly string[] BoardColumns =
        ["Applied", "Documents Verified", "Test Scheduled", "Test Completed", "Interview", "Admitted", "Enrolled", "Rejected"];

    public async Task<PipelineBoard> GetBoardAsync(Guid? intakeId, CancellationToken ct = default)
    {
        var query = db.ApplicantProfiles.Where(p => p.Status != ApplicantStatus.Draft);
        if (intakeId is { } i) query = query.Where(p => p.IntakeId == i);

        var applicants = await query
            .Select(p => new { p.Id, p.ApplicantId, p.FullName, p.ProgramId, p.Status, p.UpdatedAt, p.ConvertedStudentId })
            .ToListAsync(ct);

        var completedTestApplicants = await db.ApplicantTests
            .Where(t => t.Status == ApplicantTestStatus.Submitted || t.Status == ApplicantTestStatus.ForceSubmitted)
            .Select(t => t.ApplicantId)
            .ToListAsync(ct);
        var completed = completedTestApplicants.ToHashSet();

        var cards = applicants
            .Where(a => a.Status != ApplicantStatus.Withdrawn)
            .Select(a => new PipelineCard(a.Id, a.ApplicantId, a.FullName, a.ProgramId, a.UpdatedAt,
                ColumnFor(a.Status, a.ConvertedStudentId is not null, completed.Contains(a.Id))))
            .ToList();

        return new PipelineBoard(BoardColumns, cards);
    }

    public async Task MoveAsync(Guid applicantProfileId, string targetColumn, Guid adminUserId, string? manualInstitutionalEmail, CancellationToken ct = default)
    {
        switch (targetColumn)
        {
            case "Admitted":
            case "Enrolled":
                await decisions.AdmitAsync(applicantProfileId, adminUserId, manualInstitutionalEmail, ct);
                return;
            case "Test Completed":
                throw new InvalidOperationException("Test completion is recorded automatically when a candidate finishes their paper.");
            default:
                var status = targetColumn switch
                {
                    "Applied" => ApplicantStatus.Submitted,
                    "Documents Verified" => ApplicantStatus.UnderReview,
                    "Test Scheduled" => ApplicantStatus.TestScheduled,
                    "Interview" => ApplicantStatus.InterviewScheduled,
                    "Rejected" => ApplicantStatus.Rejected,
                    _ => throw new InvalidOperationException($"Unknown pipeline column '{targetColumn}'.")
                };
                await admin.TransitionAsync(applicantProfileId, status, $"Moved to {targetColumn} on the pipeline board", ct);
                return;
        }
    }

    public async Task<int> BulkMoveAsync(IReadOnlyList<Guid> applicantProfileIds, string targetColumn, Guid adminUserId, CancellationToken ct = default)
    {
        var moved = 0;
        foreach (var id in applicantProfileIds)
        {
            try
            {
                await MoveAsync(id, targetColumn, adminUserId, null, ct);
                moved++;
            }
            catch (InvalidOperationException)
            {
                // skip applicants the move is invalid for
            }
        }

        return moved;
    }

    private static string ColumnFor(ApplicantStatus status, bool hasStudent, bool testCompleted) => status switch
    {
        ApplicantStatus.Submitted => "Applied",
        ApplicantStatus.UnderReview => "Documents Verified",
        ApplicantStatus.TestScheduled => testCompleted ? "Test Completed" : "Test Scheduled",
        ApplicantStatus.InterviewScheduled => "Interview",
        ApplicantStatus.Admitted => hasStudent ? "Enrolled" : "Admitted",
        ApplicantStatus.Rejected => "Rejected",
        _ => "Applied"
    };
}
