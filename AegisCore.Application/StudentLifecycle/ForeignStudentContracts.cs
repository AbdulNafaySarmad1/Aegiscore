namespace AegisCore.Application.StudentLifecycle;

public interface IForeignStudentService
{
    Task<IReadOnlyList<ExpiryAlertDto>> GetDocumentExpiryAlertsAsync(Guid tenantId, CancellationToken ct = default);
    Task<MoraComplianceDto> GetMoraComplianceAsync(Guid semesterId, CancellationToken ct = default);
    Task<EnrollmentLetterDto> GenerateEnrollmentLetterAsync(Guid studentId, string purpose, CancellationToken ct = default);
    Task<VisaRenewalWorkflowDto> InitiateVisaRenewalAsync(Guid studentId, CancellationToken ct = default);
    Task<IReadOnlyList<ComplianceBreachDto>> GetComplianceBreachesAsync(Guid tenantId, CancellationToken ct = default);
    Task<IoDashboardDto> GetIoDashboardAsync(Guid tenantId, CancellationToken ct = default);
}

public sealed record ForeignStudentRegistryDto(
    Guid StudentId,
    string Nationality,
    string PassportNumber,
    DateTimeOffset PassportExpiry,
    string VisaType,
    string? VisaNumber,
    DateTimeOffset VisaExpiry,
    string MoraRegistrationStatus,
    string ComplianceStatus);

public sealed record ExpiryAlertDto(
    Guid StudentId,
    string Nationality,
    string DocumentType,
    DateTimeOffset ExpiryDate,
    int DaysRemaining,
    string UrgencyLevel,
    bool IsExpired);

public sealed record MoraComplianceDto(
    Guid SemesterId,
    int TotalForeignStudents,
    int MoraRegistered,
    int PendingRegistration,
    string SubmissionStatus,
    DateTimeOffset? LastSubmittedAt,
    string? MoraReferenceNumber);

public sealed record EnrollmentLetterDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    string Nationality,
    string Program,
    int CurrentYear,
    string? PdfUrl,
    string? QrVerificationCode,
    DateTimeOffset IssuedAt);

public sealed record VisaRenewalWorkflowDto(
    Guid WorkflowId,
    Guid StudentId,
    string Status,
    DateTimeOffset ApplicationDeadline,
    List<string> RequiredDocuments,
    DateTimeOffset? SubmittedAt,
    string? RenewalOutcome);

public sealed record ComplianceBreachDto(
    Guid Id,
    Guid StudentId,
    string BreachType,
    string? Description,
    string Status,
    DateTimeOffset OccurredAt,
    string? ResolutionType);

public sealed record IoDashboardDto(
    int TotalForeignStudents,
    Dictionary<string, int> StudentsByNationality,
    int FullyCompliant,
    int ActionRequired,
    int NonCompliant,
    int ExpiringNext30Days,
    int ExpiringNext60Days,
    int ExpiringNext90Days,
    string MoraSubmissionStatus);
