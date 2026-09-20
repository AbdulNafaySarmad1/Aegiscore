using AegisCore.Domain.Entities;

namespace AegisCore.Application.Admissions;

/// <summary>
/// Resolves the installation tenant for the public applicant portal, which has no
/// authenticated tenant context at signup time.
/// </summary>
public interface IApplicantTenantResolver
{
    Guid ResolveTenantId();
}

/// <summary>Generates collision-checked Applicant IDs: <c>APP-{YEAR}{SEM}-{PROGRAM_CODE}-{NANOID8}</c>.</summary>
public interface IApplicantIdGenerator
{
    Task<string> GenerateAsync(AdmissionIntake intake, DegreeProgram program, CancellationToken ct = default);
}

/// <summary>Passwordless email OTP for applicant signup. Codes live in Redis, never in the database.</summary>
public interface IApplicantOtpService
{
    /// <summary>Issues a code, stores it in Redis, and returns it so the caller can deliver it.</summary>
    Task<string> IssueAsync(string email, CancellationToken ct = default);
    Task<bool> VerifyAsync(string email, string code, CancellationToken ct = default);
}

/// <summary>Issues and revokes applicant session tokens (JWT audience <c>applicant</c>).</summary>
public interface IApplicantTokenService
{
    string IssueToken(ApplicantProfile profile);
    Task RevokeAsync(Guid applicantProfileId, CancellationToken ct = default);
    Task<bool> IsRevokedAsync(Guid applicantProfileId, CancellationToken ct = default);
}

public sealed record PhotoValidationResult(
    bool Passed,
    double? Confidence,
    string Reason,
    bool RequiresManualReview);

/// <summary>
/// Calls the out-of-process CLIP microservice to detect filtered / AI-generated photos.
/// Fails open (Passed = true, RequiresManualReview = true) when the service is unreachable.
/// </summary>
public interface IPhotoValidationService
{
    Task<PhotoValidationResult> ValidateAsync(Stream image, string fileName, string contentType, CancellationToken ct = default);
}

// ---- Applicant-facing orchestration ----

public sealed record ApplicantSignupResult(bool Sent);
public sealed record ApplicantVerifyResult(string ApplicantId, string Token, ApplicantStatus Status);

public sealed record ApplicantDocumentRequirementView(
    Guid Id,
    AdmissionDocumentKind DocumentType,
    bool IsRequired,
    AdmissionDocumentFormat AcceptedFormat,
    int MaxFileSizeMb,
    AdmissionDocumentValidationStatus? UploadStatus,
    string? RejectionReason);

public sealed record ApplicantProfileView(
    string ApplicantId,
    string Email,
    ApplicantStatus Status,
    Guid IntakeId,
    Guid ProgramId,
    string? FullName,
    DateOnly? DateOfBirth,
    string? Gender,
    string? Nationality,
    string? Phone,
    IReadOnlyList<ApplicantDocumentRequirementView> Documents);

/// <summary>
/// What the applicant confirms at submission. <see cref="AcceptProcessing"/> must be true. When the
/// applicant is under 18 on the day of submission, <see cref="Guardian"/> is required and the
/// guardian's <see cref="ApplicantGuardianConsent.Consents"/> must be true.
/// </summary>
public sealed record ApplicantSubmission(
    bool AcceptProcessing,
    ApplicantGuardianConsent? Guardian = null,
    string? IpAddress = null);

public sealed record ApplicantGuardianConsent(
    string Name,
    string Relationship,
    string Phone,
    string Email,
    bool Consents);

/// <summary>The exact texts shown; each is copied verbatim into the consent ledger when accepted.</summary>
public sealed record ApplicantConsentTexts(
    string ProcessingText,
    string ProcessingVersion,
    string GuardianText,
    string GuardianVersion,
    bool GuardianRequired,
    int AgeOfMajority);

public sealed record ApplicantPersonalDetails(
    string FullName,
    DateOnly DateOfBirth,
    string Gender,
    string Nationality,
    string Phone);

public sealed record ApplicantDocumentUploadResult(
    AdmissionDocumentKind DocumentType,
    AdmissionDocumentValidationStatus ValidationStatus,
    string? RejectionReason,
    double? Confidence);

public interface IApplicantPortalService
{
    Task<ApplicantSignupResult> SignupAsync(string email, Guid intakeId, Guid programId, CancellationToken ct = default);
    Task<ApplicantVerifyResult> VerifyAsync(string email, string otp, Guid intakeId, Guid programId, CancellationToken ct = default);

    Task<ApplicantProfileView> GetProfileAsync(Guid applicantProfileId, CancellationToken ct = default);
    Task<ApplicantProfileView> UpdatePersonalDetailsAsync(Guid applicantProfileId, ApplicantPersonalDetails details, CancellationToken ct = default);

    Task<ApplicantDocumentUploadResult> UploadDocumentAsync(
        Guid applicantProfileId, AdmissionDocumentKind documentType, Stream content, string fileName, string contentType, long length, CancellationToken ct = default);

    Task<ApplicantDocumentUploadResult> UploadPhotoAsync(
        Guid applicantProfileId, Stream content, string fileName, string contentType, long length, CancellationToken ct = default);

    Task<ApplicantProfileView> SubmitAsync(Guid applicantProfileId, ApplicantSubmission? submission = null, CancellationToken ct = default);
    /// <summary>The consent texts the applicant (and, for a minor, the guardian) must see before submitting.</summary>
    Task<ApplicantConsentTexts> GetConsentTextsAsync(Guid applicantProfileId, CancellationToken ct = default);
}

// ---- Open intake catalogue (public) ----

public sealed record OpenProgramView(Guid ProgramId, string Name, string Code);
public sealed record OpenIntakeView(Guid IntakeId, int Year, string Semester, string Name, IReadOnlyList<OpenProgramView> Programs);

public interface IAdmissionCatalogService
{
    Task<IReadOnlyList<OpenIntakeView>> GetOpenIntakesAsync(CancellationToken ct = default);
}

// ---- Admin configuration ----

public sealed record IntakeInput(int Year, IntakeSemester Semester, string Name, DateTimeOffset? OpensAt, DateTimeOffset? ClosesAt, Guid? BatchId);
public sealed record DocumentRequirementInput(Guid IntakeId, Guid ProgramId, AdmissionDocumentKind DocumentType, bool IsRequired, AdmissionDocumentFormat AcceptedFormat, int MaxFileSizeMb);

public interface IAdmissionConfigService
{
    Task<AdmissionIntake> CreateIntakeAsync(IntakeInput input, CancellationToken ct = default);
    Task<AdmissionIntake> UpdateIntakeStatusAsync(Guid intakeId, IntakeStatus status, CancellationToken ct = default);
    Task<IReadOnlyList<AdmissionIntake>> ListIntakesAsync(CancellationToken ct = default);

    Task SetProgramCodeAsync(Guid programId, string code, CancellationToken ct = default);

    Task<AdmissionDocumentRequirement> UpsertDocumentRequirementAsync(DocumentRequirementInput input, CancellationToken ct = default);
    Task DeleteDocumentRequirementAsync(Guid requirementId, CancellationToken ct = default);
    Task<IReadOnlyList<AdmissionDocumentRequirement>> ListDocumentRequirementsAsync(Guid intakeId, Guid programId, CancellationToken ct = default);
}

// ---- Admin admissions dashboard ----

public sealed record AdmissionApplicantRow(
    Guid Id,
    string ApplicantId,
    string? FullName,
    string Email,
    Guid ProgramId,
    Guid IntakeId,
    ApplicantStatus Status,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset CreatedAt);

public sealed record AdmissionApplicantDocument(
    Guid Id,
    AdmissionDocumentKind DocumentType,
    string MimeType,
    long FileSize,
    AdmissionDocumentValidationStatus ValidationStatus,
    string? RejectionReason,
    double? ValidationConfidence,
    string? ValidationMethod,
    DateTimeOffset UploadedAt);

public sealed record AdmissionApplicantDetail(
    AdmissionApplicantRow Applicant,
    IReadOnlyList<AdmissionApplicantDocument> Documents,
    AdmissionApplicantTest? Test);

public sealed record AdmissionApplicantTest(int? Score, bool? Passed, ApplicantTestStatus Status, DateTimeOffset? CompletedAt);

public sealed record DocumentDownload(Stream Content, string ContentType, string FileName);

public interface IAdmissionsAdminService
{
    Task<IReadOnlyList<AdmissionApplicantRow>> ListAsync(Guid? intakeId, Guid? programId, ApplicantStatus? status, CancellationToken ct = default);
    Task<AdmissionApplicantDetail?> GetAsync(Guid applicantProfileId, CancellationToken ct = default);
    Task<DocumentDownload?> DownloadDocumentAsync(Guid applicantProfileId, Guid documentId, CancellationToken ct = default);

    Task ReviewDocumentAsync(Guid applicantProfileId, Guid documentId, bool approve, string? rejectionReason, Guid reviewerId, CancellationToken ct = default);
    Task TransitionAsync(Guid applicantProfileId, ApplicantStatus target, string? note, CancellationToken ct = default);
    Task<int> BulkTransitionAsync(IReadOnlyList<Guid> applicantProfileIds, ApplicantStatus target, string? note, CancellationToken ct = default);
}
