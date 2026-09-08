using AegisCore.Domain.Entities;
using MediatR;

namespace AegisCore.Application.Admissions;

/// <summary>
/// Raised (and persisted as a <see cref="Domain.Entities.DomainEvent"/>) when an admin admits an
/// applicant and the Student record has been created. Handlers provision the institutional
/// email, migrate the account, and invalidate the applicant session.
/// </summary>
public sealed record AdmissionConfirmedNotification(
    Guid TenantId,
    Guid ApplicantProfileId,
    Guid StudentId,
    Guid UserId,
    string RollNumber,
    string ApplicantEmail,
    string FirstName,
    string LastName,
    Guid ProgramId,
    Guid IntakeId,
    string? ManualInstitutionalEmail) : INotification;

/// <summary>Allocates the next collision-safe roll number for a program + intake.</summary>
public interface IRollNumberService
{
    Task<string> AllocateAsync(AdmissionIntake intake, DegreeProgram program, CancellationToken ct = default);
}

public sealed record AdmitResult(Guid StudentId, string RollNumber, Guid UserId);

public interface IAdmissionDecisionService
{
    Task<AdmitResult> AdmitAsync(Guid applicantProfileId, Guid adminUserId, string? manualInstitutionalEmail, CancellationToken ct = default);
}

// ---- Institutional email provisioning ----

public sealed record ProvisionRequest(string FirstName, string LastName, int Sequence, string DomainName, string? CredentialsJson);
public sealed record ProvisionedEmail(string Address, bool Created);

public interface IInstitutionalEmailProvisioner
{
    EmailProvisioningMode Mode { get; }
    Task<ProvisionedEmail> ProvisionAsync(ProvisionRequest request, CancellationToken ct = default);
}

public interface IInstitutionalEmailProvisionerFactory
{
    IInstitutionalEmailProvisioner Resolve(EmailProvisioningMode mode);
}

public sealed record EmailProvisioningConfigInput(EmailProvisioningMode Mode, string? DomainName, string? CredentialsJson);
public sealed record EmailProvisioningConfigView(EmailProvisioningMode Mode, string? DomainName, bool HasCredentials, DateTimeOffset UpdatedAt);

public interface IEmailProvisioningConfigService
{
    Task<EmailProvisioningConfigView?> GetAsync(CancellationToken ct = default);
    Task<EmailProvisioningConfigView> SetAsync(EmailProvisioningConfigInput input, Guid actorId, CancellationToken ct = default);
}

// ---- Admissions pipeline board (22D) ----

public sealed record PipelineCard(Guid ApplicantProfileId, string ApplicantId, string? Name, Guid ProgramId, DateTimeOffset LastUpdated, string Column);
public sealed record PipelineBoard(IReadOnlyList<string> Columns, IReadOnlyList<PipelineCard> Cards);

public interface IAdmissionPipelineService
{
    Task<PipelineBoard> GetBoardAsync(Guid? intakeId, CancellationToken ct = default);
    Task MoveAsync(Guid applicantProfileId, string targetColumn, Guid adminUserId, string? manualInstitutionalEmail, CancellationToken ct = default);
    Task<int> BulkMoveAsync(IReadOnlyList<Guid> applicantProfileIds, string targetColumn, Guid adminUserId, CancellationToken ct = default);
}
