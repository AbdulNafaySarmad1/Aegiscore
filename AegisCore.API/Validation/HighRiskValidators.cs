using System.Text;
using System.Text.Json;
using AegisCore.API.Controllers;
using AegisCore.Application.Examination;
using AegisCore.Application.Finance;
using AegisCore.Application.Identity;
using AegisCore.Application.Onboarding;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Onboarding;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.API.Validation;

public sealed record HighRiskEndpoint(string Method, string Route, Type RequestType, Type ValidatorType);

public static class HighRiskEndpointRegistry
{
    public static IReadOnlyList<HighRiskEndpoint> Endpoints { get; } =
    [
        new("POST", "/api/auth/login", typeof(AuthController.LoginRequest), typeof(LoginRequestValidator)),
        new("POST", "/api/auth/refresh", typeof(AuthController.RefreshRequest), typeof(RefreshRequestValidator)),
        new("POST", "/api/wizard/step2/test", typeof(WizardDatabaseRequest), typeof(WizardDatabaseRequestValidator)),
        new("POST", "/api/wizard/step3/configure", typeof(SsoConfigDto), typeof(SsoConfigDtoValidator)),
        new("POST", "/api/wizard/step5/configure", typeof(AiConfigDto), typeof(AiConfigDtoValidator)),
        new("POST", "/api/wizard/step1/template/write", typeof(DocumentTemplateWriteRequest), typeof(DocumentTemplateWriteRequestValidator)),
        new("POST", "/api/wizard/step4/permissions", typeof(SaveWizardPermissionDefinitionsRequest), typeof(SaveWizardPermissionDefinitionsRequestValidator)),
        new("POST", "/api/wizard/validate-license-key", typeof(ValidateLicenseRequest), typeof(ValidateLicenseRequestValidator)),
        new("POST", "/api/wizard/mode", typeof(ChangeWizardModeRequest), typeof(ChangeWizardModeRequestValidator)),
        new("POST", "/api/wizard/step/{step}/save", typeof(SaveStepRequest), typeof(SaveStepRequestValidator)),
        new("POST", "/api/wizard/import-demo-data", typeof(DemoImportRequest), typeof(DemoImportRequestValidator)),
        new("POST", "/api/wizard/complete", typeof(CompleteWizardRequest), typeof(CompleteWizardRequestValidator)),
        new("POST", "/api/identity/users", typeof(CreateUserRequest), typeof(CreateUserRequestValidator)),
        new("POST", "/api/identity/roles", typeof(CreateRoleRequest), typeof(CreateRoleRequestValidator)),
        new("POST", "/api/identity/users/assign-role", typeof(AssignRoleRequest), typeof(AssignRoleRequestValidator)),
        new("POST", "/api/examination/schemes", typeof(S3ExaminationController.AssessmentSchemeInput), typeof(AssessmentSchemeValidator)),
        new("PUT", "/api/examination/marks", typeof(MarksEntryRequest), typeof(MarksEntryRequestValidator)),
        new("POST", "/api/examination/moderation/submit", typeof(ModerationSubmitRequest), typeof(ModerationSubmitRequestValidator)),
        new("POST", "/api/examination/moderation/{id}/review", typeof(ModerationReviewRequest), typeof(ModerationReviewRequestValidator)),
        new("POST", "/api/examination/grades/{courseId}/grace", typeof(GraceMarksRequest), typeof(GraceMarksRequestValidator)),
        new("POST", "/api/examination/courses/{courseId}/publish", typeof(PublishResultsRequest), typeof(PublishResultsRequestValidator)),
        new("POST", "/api/examination/transcripts/{studentId}/request", typeof(TranscriptRequest), typeof(TranscriptRequestValidator)),
        new("POST", "/api/attendance/mark", typeof(MarkAttendanceRequest), typeof(MarkAttendanceRequestValidator)),
        new("POST", "/api/finance/fee-structures", typeof(FeeStructure), typeof(FeeStructureValidator)),
        new("POST", "/api/finance/dues", typeof(FeeDueRequest), typeof(FeeDueRequestValidator)),
        new("POST", "/api/finance/dues/{id}/payments", typeof(PaymentRequest), typeof(PaymentRequestValidator)),
        new("POST", "/api/finance/dues/{id}/scholarship", typeof(ScholarshipRequest), typeof(ScholarshipRequestValidator)),
        new("POST", "/api/finance/procurement/requisitions", typeof(ProcurementRequisition), typeof(ProcurementRequisitionValidator)),
        new("POST", "/api/data-import/connections", typeof(ExternalDbConnection), typeof(ExternalDbConnectionValidator)),
        new("POST", "/api/data-import/jobs", typeof(DataImportController.StartImportRequest), typeof(StartImportRequestValidator))
    ];
}

public abstract class SecureValidator<T> : AbstractValidator<T>
{
    protected SecureValidator() => RuleLevelCascadeMode = CascadeMode.Stop;
    protected static bool IsTrimmed(string? value) => value is not null && value == value.Trim();
    protected static Task<bool> ExistsAsync<TEntity>(AegisDbContext db, Guid id, CancellationToken ct) where TEntity : class
        => id == Guid.Empty ? Task.FromResult(false) : db.Set<TEntity>().AnyAsync(entity => EF.Property<Guid>(entity, "Id") == id, ct);
}

public sealed class LoginRequestValidator : SecureValidator<AuthController.LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256).Must(IsTrimmed).WithMessage("Email must not have leading or trailing spaces.");
        PasswordRules(RuleFor(x => x.Password));
    }
    internal static void PasswordRules(IRuleBuilderInitial<AuthController.LoginRequest, string> rule)
        => rule.NotEmpty().MinimumLength(12).MaximumLength(256)
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a number.")
            .Matches("[^A-Za-z0-9]").WithMessage("Password must contain a symbol.");
}

public sealed class RefreshRequestValidator : SecureValidator<AuthController.RefreshRequest>
{
    public RefreshRequestValidator() => RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(4096).Must(IsTrimmed);
}

public sealed class WizardDatabaseRequestValidator : SecureValidator<WizardDatabaseRequest>
{
    public WizardDatabaseRequestValidator()
    {
        RuleFor(x => x.Dialect).NotEmpty().Must(value => value is "postgres" or "mysql" or "mssql" or "oracle");
        RuleFor(x => x.Host).NotEmpty().MaximumLength(253).Must(IsTrimmed);
        RuleFor(x => x.Port).InclusiveBetween(1, 65535);
        RuleFor(x => x.DatabaseName).NotEmpty().MaximumLength(128).Matches("^[A-Za-z0-9_.-]+$");
        RuleFor(x => x.Username).NotEmpty().MaximumLength(128).Must(IsTrimmed);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(1024);
    }
}

public sealed class SsoConfigDtoValidator : SecureValidator<SsoConfigDto>
{
    private static readonly string[] Providers = ["entra", "google", "okta", "saml", "local"];
    public SsoConfigDtoValidator()
    {
        RuleFor(x => x.Provider).NotEmpty().Must(value => Providers.Contains(value, StringComparer.OrdinalIgnoreCase)).WithMessage("Provider must be entra, google, okta, saml, or local.");
        RuleFor(x => x.ClientId).NotEmpty().MaximumLength(500).When(x => ValidationRules.IsProvider(x.Provider, "entra", "google", "okta"));
        // The secret is required to pass the step, not to save a change to the mapping: the vault
        // keeps the one entered earlier, and the step-3 check refuses to continue without one.
        RuleFor(x => x.ClientSecret).MaximumLength(4096);
        RuleFor(x => x.TenantId).NotEmpty().MaximumLength(200).When(x => ValidationRules.IsProvider(x.Provider, "entra"));
        RuleFor(x => x.Domain).NotEmpty().MaximumLength(253).Matches("^[A-Za-z0-9.-]+$").When(x => ValidationRules.IsProvider(x.Provider, "google"));
        RuleFor(x => x.Authority).NotEmpty().MaximumLength(500).Must(ValidationRules.IsHttpsUrl).WithMessage("The Okta org URL must be an HTTPS URL.").When(x => ValidationRules.IsProvider(x.Provider, "okta"));
        RuleFor(x => x.Scopes).MaximumLength(500).Matches("^[A-Za-z0-9:/._ -]*$").When(x => x.Scopes is not null);
        RuleFor(x => x.DefaultRole).MaximumLength(100);
        RuleFor(x => x.GroupRoleMapping).Must(mapping => mapping is null || (mapping.Count <= 100 && mapping.All(pair => pair.Key.Length is > 0 and <= 300 && pair.Value.Length <= 100)))
            .WithMessage("The group-to-role mapping may hold up to 100 entries; group names up to 300 characters, role names up to 100.");
        RuleFor(x => x.EntityId).NotEmpty().MaximumLength(1000).When(x => ValidationRules.IsProvider(x.Provider, "saml"));
        RuleFor(x => x.AcsUrl).NotEmpty().MaximumLength(2048).Must(ValidationRules.IsHttpsUrl).When(x => ValidationRules.IsProvider(x.Provider, "saml"));
        RuleFor(x => x).Must(x => !ValidationRules.IsProvider(x.Provider, "saml") || !string.IsNullOrWhiteSpace(x.MetadataXml) || ValidationRules.IsHttpsUrl(x.MetadataUrl)).WithMessage("SAML metadata XML or an HTTPS metadata URL is required.");
    }
}

public sealed class AiConfigDtoValidator : SecureValidator<AiConfigDto>
{
    private static readonly string[] Providers = ["ollama-local", "ollama-cloud", "openai", "granite", "custom"];
    public AiConfigDtoValidator()
    {
        RuleFor(x => x.Provider).NotEmpty().Must(value => Providers.Contains(value, StringComparer.OrdinalIgnoreCase));
        RuleFor(x => x.Model).NotEmpty().MaximumLength(200).Must(IsTrimmed);
        RuleFor(x => x.Endpoint).NotEmpty().MaximumLength(2048).Must(ValidationRules.IsHttpsOrLoopbackUrl).When(x => ValidationRules.IsProvider(x.Provider, "ollama-cloud", "custom"));
        RuleFor(x => x.ApiKey).NotEmpty().MaximumLength(4096).When(x => ValidationRules.IsProvider(x.Provider, "openai", "granite", "ollama-cloud"));
    }
}

public sealed class DocumentTemplateWriteRequestValidator : SecureValidator<DocumentTemplateWriteRequest>
{
    public DocumentTemplateWriteRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200).Must(IsTrimmed);
        RuleFor(x => x.RawHtml).NotEmpty().Must(value => Encoding.UTF8.GetByteCount(value) <= 10 * 1024 * 1024).WithMessage("Template HTML must not exceed 10 MiB.");
        RuleFor(x => x.LogoUrl).Must(value => value is null || ValidationRules.IsHttpsUrl(value)).WithMessage("Logo URL must use HTTPS.");
    }
}

public sealed class SaveWizardPermissionDefinitionsRequestValidator : SecureValidator<SaveWizardPermissionDefinitionsRequest>
{
    private static readonly string[] Resources = ["Students", "Grades", "Attendance", "Finance", "Timetable", "Admissions", "Reports", "Settings", "System"];
    private static readonly string[] Actions = ["Read", "Write", "Delete", "Approve", "Export"];

    public SaveWizardPermissionDefinitionsRequestValidator()
    {
        RuleFor(x => x.Permissions).NotNull().NotEmpty().Must(items => items.Count <= 5000);
        RuleForEach(x => x.Permissions).ChildRules(item =>
        {
            item.RuleFor(x => x.Resource).NotEmpty().Must(value => Resources.Contains(value, StringComparer.OrdinalIgnoreCase));
            item.RuleFor(x => x.Action).NotEmpty().Must(value => Actions.Contains(value, StringComparer.OrdinalIgnoreCase));
        });
    }
}

public sealed class SaveWizardRolesRequestValidator : SecureValidator<SaveWizardRolesRequest>
{
    public SaveWizardRolesRequestValidator()
    {
        RuleFor(x => x.Roles).NotNull().NotEmpty().Must(roles => roles.Count <= 500)
            .Must(roles => roles.Distinct(StringComparer.OrdinalIgnoreCase).Count() == roles.Count)
            .WithMessage("Role names must be unique.");
        RuleForEach(x => x.Roles).NotEmpty().MaximumLength(64).Must(IsTrimmed);
    }
}

public sealed class SaveWizardPermissionMatrixRequestValidator : SecureValidator<SaveWizardPermissionMatrixRequest>
{
    private static readonly string[] Resources = ["Students", "Grades", "Attendance", "Finance", "Timetable", "Admissions", "Reports", "Settings", "System"];
    private static readonly string[] Actions = ["Read", "Write", "Delete", "Approve", "Export"];

    public SaveWizardPermissionMatrixRequestValidator()
    {
        RuleFor(x => x.Entries).NotNull().Must(entries => entries.Count <= 10_000);
        RuleForEach(x => x.Entries).ChildRules(entry =>
        {
            entry.RuleFor(x => x.Role).NotEmpty().MaximumLength(64).Must(IsTrimmed);
            entry.RuleFor(x => x.Resource).NotEmpty().Must(value => Resources.Contains(value, StringComparer.OrdinalIgnoreCase));
            entry.RuleFor(x => x.Action).NotEmpty().Must(value => Actions.Contains(value, StringComparer.OrdinalIgnoreCase));
        });
    }
}

public sealed class ValidateLicenseRequestValidator : SecureValidator<ValidateLicenseRequest>
{
    public ValidateLicenseRequestValidator() => RuleFor(x => x.LicenseKey).NotEmpty().MaximumLength(500).Must(IsTrimmed);
}

public sealed class ChangeWizardModeRequestValidator : SecureValidator<ChangeWizardModeRequest>
{
    public ChangeWizardModeRequestValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.Mode).NotEmpty().Must(value => value is "demo" or "paid" or "live");
        RuleFor(x => x.LicenseKey).NotEmpty().MaximumLength(500).When(x => x.Mode is "paid" or "live");
    }
}

public sealed class SaveStepRequestValidator : SecureValidator<SaveStepRequest>
{
    public SaveStepRequestValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.Data).Must(ValidationRules.IsBoundedObject).WithMessage("Wizard step data must be a JSON object no larger than 128 KiB.");
    }
}

public sealed class DemoImportRequestValidator : SecureValidator<DemoImportRequest>
{
    public DemoImportRequestValidator()
    {
        RuleFor(x => x.ConnectionString).NotEmpty().MaximumLength(2000).Must(IsTrimmed);
        RuleFor(x => x.DbType).NotEmpty().Must(value => value is "PostgreSQL" or "MySQL" or "MSSQL" or "Oracle");
    }
}

public sealed class CompleteWizardRequestValidator : SecureValidator<CompleteWizardRequest>
{
    public CompleteWizardRequestValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.ConfirmedBy).NotEmpty().EmailAddress().MaximumLength(256).Must(IsTrimmed);
        RuleFor(x => x.Timestamp).Must(value => value > DateTimeOffset.UtcNow.AddYears(-1) && value < DateTimeOffset.UtcNow.AddMinutes(5));
    }
}

public sealed class CreateUserRequestValidator : SecureValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(100).Matches("^[A-Za-z0-9_.@-]+$").Must(IsTrimmed);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256).Must(IsTrimmed);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100).Must(IsTrimmed);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100).Must(IsTrimmed);
    }
}

public sealed class CreateRoleRequestValidator : SecureValidator<CreateRoleRequest>
{
    public CreateRoleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(64).Must(IsTrimmed);
        RuleFor(x => x.Description).MaximumLength(1000).Must(IsTrimmed).When(x => !string.IsNullOrEmpty(x.Description));
        RuleFor(x => x.PermissionIds).NotNull().Must(ids => ids.Count <= 500 && ids.All(id => id != Guid.Empty) && ids.Distinct().Count() == ids.Count);
    }
}

public sealed class AssignRoleRequestValidator : SecureValidator<AssignRoleRequest>
{
    public AssignRoleRequestValidator(AegisDbContext db)
    {
        RuleFor(x => x.UserId).NotEmpty().MustAsync((id, ct) => ExistsAsync<User>(db, id, ct)).WithMessage("User {PropertyValue} does not exist.");
        RuleFor(x => x.RoleId).NotEmpty().MustAsync((id, ct) => ExistsAsync<Role>(db, id, ct)).WithMessage("Role {PropertyValue} does not exist.");
    }
}

// The scheme is described by a request record now, not by the entity: the entity carries a required
// navigation from each component back to its scheme, which no caller can supply.
public sealed class AssessmentSchemeValidator : SecureValidator<S3ExaminationController.AssessmentSchemeInput>
{
    public AssessmentSchemeValidator(AegisDbContext db)
    {
        RuleFor(x => x.CourseId).NotEmpty().MustAsync((id, ct) => ExistsAsync<Course>(db, id, ct)).WithMessage("Course {PropertyValue} does not exist.");
        RuleFor(x => x.SemesterId).NotEmpty().MustAsync((id, ct) => ExistsAsync<Semester>(db, id, ct)).WithMessage("Semester {PropertyValue} does not exist.");
        RuleFor(x => x.ExamControllerId).NotEmpty().MustAsync((id, ct) => ExistsAsync<User>(db, id, ct)).WithMessage("Exam controller {PropertyValue} does not exist.");
        RuleFor(x => x.Components).NotNull().NotEmpty().Must(items => items.Count <= 50 && items.Sum(item => item.WeightPercentage) == 100);
        RuleForEach(x => x.Components).ChildRules(component =>
        {
            component.RuleFor(x => x.ComponentType).NotEmpty().MaximumLength(50).Must(IsTrimmed);
            component.RuleFor(x => x.WeightPercentage).InclusiveBetween(0, 100);
            component.RuleFor(x => x.TotalMarks).InclusiveBetween(1, 100000);
            component.RuleFor(x => x.PassingMarks).InclusiveBetween(0, 100000).When(x => x.PassingMarks.HasValue);
        });
    }
}

public sealed class MarksEntryRequestValidator : SecureValidator<MarksEntryRequest>
{
    public MarksEntryRequestValidator(AegisDbContext db)
    {
        RuleFor(x => x.CourseId).NotEmpty().MustAsync((id, ct) => ExistsAsync<Course>(db, id, ct)).WithMessage("Course {PropertyValue} does not exist.");
        RuleFor(x => x.StudentId).NotEmpty().MustAsync((id, ct) => ExistsAsync<Student>(db, id, ct)).WithMessage("Student {PropertyValue} does not exist.");
        RuleFor(x => x.AssessmentComponentId).NotEmpty().MustAsync((id, ct) => ExistsAsync<AssessmentComponent>(db, id, ct)).WithMessage("Assessment component {PropertyValue} does not exist.");
        RuleFor(x => x.ObtainedMarks).GreaterThanOrEqualTo(0);
        RuleFor(x => x).MustAsync(async (request, ct) => await db.AssessmentComponents.AnyAsync(component => component.Id == request.AssessmentComponentId && request.ObtainedMarks <= component.TotalMarks, ct)).WithMessage("Obtained marks exceed the assessment component total.");
    }
}

public sealed class ModerationSubmitRequestValidator : SecureValidator<ModerationSubmitRequest>
{
    public ModerationSubmitRequestValidator(AegisDbContext db)
    {
        RuleFor(x => x.CourseId).NotEmpty().MustAsync((id, ct) => ExistsAsync<Course>(db, id, ct)).WithMessage("Course {PropertyValue} does not exist.");
        RuleFor(x => x.SemesterId).NotEmpty().MustAsync((id, ct) => ExistsAsync<Semester>(db, id, ct)).WithMessage("Semester {PropertyValue} does not exist.");
        RuleFor(x => x.ActorId).NotEmpty().MustAsync((id, ct) => ExistsAsync<User>(db, id, ct)).WithMessage("User {PropertyValue} does not exist.");
    }
}

public sealed class ModerationReviewRequestValidator : SecureValidator<ModerationReviewRequest>
{
    public ModerationReviewRequestValidator(AegisDbContext db)
    {
        RuleFor(x => x.Status).NotEmpty().Must(value => value is "approved" or "rejected");
        RuleFor(x => x.ActorId).NotEmpty().MustAsync((id, ct) => ExistsAsync<User>(db, id, ct)).WithMessage("User {PropertyValue} does not exist.");
        RuleFor(x => x.Comments).MaximumLength(4000).Must(IsTrimmed).When(x => !string.IsNullOrEmpty(x.Comments));
    }
}

public sealed class GraceMarksRequestValidator : SecureValidator<GraceMarksRequest>
{
    public GraceMarksRequestValidator(AegisDbContext db)
    {
        RuleFor(x => x.ActorId).NotEmpty().MustAsync((id, ct) => ExistsAsync<User>(db, id, ct)).WithMessage("User {PropertyValue} does not exist.");
        RuleFor(x => x.Entries).NotNull().NotEmpty().Must(items => items.Count <= 10000);
        RuleForEach(x => x.Entries).ChildRules(entry =>
        {
            entry.RuleFor(x => x.StudentId).NotEmpty().MustAsync((id, ct) => ExistsAsync<Student>(db, id, ct)).WithMessage("Student {PropertyValue} does not exist.");
            entry.RuleFor(x => x.Marks).InclusiveBetween(0, 10);
        });
    }
}

public sealed class PublishResultsRequestValidator : SecureValidator<PublishResultsRequest>
{
    public PublishResultsRequestValidator(AegisDbContext db)
    {
        RuleFor(x => x.SemesterId).NotEmpty().MustAsync((id, ct) => ExistsAsync<Semester>(db, id, ct)).WithMessage("Semester {PropertyValue} does not exist.");
        RuleFor(x => x.ActorId).NotEmpty().MustAsync((id, ct) => ExistsAsync<User>(db, id, ct)).WithMessage("User {PropertyValue} does not exist.");
    }
}

public sealed class TranscriptRequestValidator : SecureValidator<TranscriptRequest>
{
    public TranscriptRequestValidator(AegisDbContext db)
    {
        RuleFor(x => x.RequestedBy).NotEmpty().MustAsync((id, ct) => ExistsAsync<User>(db, id, ct)).WithMessage("User {PropertyValue} does not exist.");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000).Must(IsTrimmed);
    }
}

public sealed class MarkAttendanceRequestValidator : SecureValidator<MarkAttendanceRequest>
{
    public MarkAttendanceRequestValidator(AegisDbContext db)
    {
        RuleFor(x => x.CourseId).NotEmpty().MustAsync((id, ct) => ExistsAsync<Course>(db, id, ct)).WithMessage("Course {PropertyValue} does not exist.");
        // Optional: left out, the controller records the roll against whoever teaches the course.
        RuleFor(x => x.MarkedByFacultyId!.Value).MustAsync((id, ct) => ExistsAsync<Faculty>(db, id, ct))
            .When(x => x.MarkedByFacultyId is { } id && id != Guid.Empty)
            .WithMessage("Faculty {PropertyValue} does not exist.");
        RuleFor(x => x.SessionDate).Must(date => date > DateTime.UtcNow.AddYears(-1) && date <= DateTime.UtcNow.AddDays(1));
        RuleFor(x => x.Entries).NotNull().NotEmpty().Must(items => items.Count <= 10000 && items.Select(item => item.EnrollmentId).Distinct().Count() == items.Count);
        RuleForEach(x => x.Entries).ChildRules(entry =>
        {
            entry.RuleFor(x => x.EnrollmentId).NotEmpty().MustAsync((id, ct) => ExistsAsync<Enrollment>(db, id, ct)).WithMessage("Enrollment {PropertyValue} does not exist.");
            entry.RuleFor(x => x.Status).Must(value => Enum.IsDefined(typeof(AttendanceStatus), value));
        });
    }
}

public sealed class FeeStructureValidator : SecureValidator<FeeStructure>
{
    public FeeStructureValidator(AegisDbContext db)
    {
        RuleFor(x => x.AcademicYearId).NotEmpty().MustAsync((id, ct) => ExistsAsync<AcademicYear>(db, id, ct)).WithMessage("Academic year {PropertyValue} does not exist.");
        RuleFor(x => x.ProgramId).NotEmpty().MustAsync((id, ct) => ExistsAsync<DegreeProgram>(db, id, ct)).WithMessage("Program {PropertyValue} does not exist.");
        RuleFor(x => x.Semester).InclusiveBetween(1, 20);
        RuleFor(x => x.FeeType).NotEmpty().MaximumLength(50).Must(IsTrimmed);
        RuleFor(x => x.TotalFee).InclusiveBetween(0m, 10_000_000m);
        RuleFor(x => x.Components).NotNull().NotEmpty().Must(items => items.Count <= 100);
        RuleForEach(x => x.Components).ChildRules(component =>
        {
            component.RuleFor(x => x.ComponentName).NotEmpty().MaximumLength(100).Must(IsTrimmed);
            component.RuleFor(x => x.Amount).InclusiveBetween(0m, 10_000_000m);
        });
        RuleFor(x => x).Must(value => value.Components.Sum(component => component.Amount) == value.TotalFee).WithMessage("Fee component amounts must equal the total fee.");
    }
}

public sealed class FeeDueRequestValidator : SecureValidator<FeeDueRequest>
{
    public FeeDueRequestValidator(AegisDbContext db)
    {
        RuleFor(x => x.StudentId).NotEmpty().MustAsync((id, ct) => ExistsAsync<Student>(db, id, ct)).WithMessage("Student {PropertyValue} does not exist.");
        RuleFor(x => x.SemesterId).NotEmpty().MustAsync((id, ct) => ExistsAsync<Semester>(db, id, ct)).WithMessage("Semester {PropertyValue} does not exist.");
        RuleFor(x => x.FeeStructureId).NotEmpty().MustAsync((id, ct) => ExistsAsync<FeeStructure>(db, id, ct)).WithMessage("Fee structure {PropertyValue} does not exist.");
    }
}

public sealed class PaymentRequestValidator : SecureValidator<PaymentRequest>
{
    public PaymentRequestValidator()
    {
        RuleFor(x => x.Method).NotEmpty().MaximumLength(50).Matches("^[A-Za-z0-9_. -]+$").Must(IsTrimmed);
        RuleFor(x => x.Amount).InclusiveBetween(0.01m, 10_000_000m);
        RuleFor(x => x.ReferenceNumber).MaximumLength(100).Must(IsTrimmed).When(x => !string.IsNullOrEmpty(x.ReferenceNumber));
    }
}

public sealed class ScholarshipRequestValidator : SecureValidator<ScholarshipRequest>
{
    public ScholarshipRequestValidator(AegisDbContext db)
    {
        RuleFor(x => x.Amount).InclusiveBetween(0.01m, 10_000_000m);
        RuleFor(x => x.ScholarshipId).NotEmpty().MustAsync((id, ct) => ExistsAsync<StudentScholarship>(db, id, ct)).WithMessage("Scholarship {PropertyValue} does not exist.");
    }
}

public sealed class ProcurementRequisitionValidator : SecureValidator<ProcurementRequisition>
{
    public ProcurementRequisitionValidator(AegisDbContext db)
    {
        RuleFor(x => x.RequestingDepartmentId).NotEmpty().MustAsync((id, ct) => ExistsAsync<Department>(db, id, ct)).WithMessage("Department {PropertyValue} does not exist.");
        RuleFor(x => x.RequisitionDate).Must(date => date >= DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)));
        RuleFor(x => x.RequiredByDate).GreaterThanOrEqualTo(x => x.RequisitionDate).When(x => x.RequiredByDate.HasValue);
        RuleFor(x => x.RequisitionNumber).MaximumLength(50).Must(IsTrimmed).When(x => !string.IsNullOrEmpty(x.RequisitionNumber));
        RuleFor(x => x.LineItems).NotNull().NotEmpty().Must(items => items.Count <= 1000);
        RuleForEach(x => x.LineItems).ChildRules(line =>
        {
            line.RuleFor(x => x.Description).NotEmpty().MaximumLength(1000).Must(IsTrimmed);
            line.RuleFor(x => x.Quantity).InclusiveBetween(1, 1_000_000);
            line.RuleFor(x => x.UnitPrice).InclusiveBetween(0m, 10_000_000m);
            line.RuleFor(x => x.ItemCode).MaximumLength(100).Must(IsTrimmed).When(x => !string.IsNullOrEmpty(x.ItemCode));
        });
        RuleFor(x => x).Must(value => value.LineItems.Sum(item => item.Quantity * item.UnitPrice) <= 10_000_000m).WithMessage("Requisition total must not exceed 10,000,000.");
    }
}

public sealed class ExternalDbConnectionValidator : SecureValidator<ExternalDbConnection>
{
    public ExternalDbConnectionValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200).Must(IsTrimmed);
        RuleFor(x => x.DbType).NotEmpty().Must(value => value is "PostgreSQL" or "MySQL" or "MSSQL" or "Oracle");
        RuleFor(x => x.ConnectionString).NotEmpty().MaximumLength(2000).Must(IsTrimmed);
        RuleFor(x => x.Schema).MaximumLength(200).Matches("^[A-Za-z0-9_$.-]+$").When(x => !string.IsNullOrEmpty(x.Schema));
    }
}

public sealed class StartImportRequestValidator : SecureValidator<DataImportController.StartImportRequest>
{
    public StartImportRequestValidator(AegisDbContext db)
    {
        RuleFor(x => x.ConnectionId).NotEmpty().MustAsync((id, ct) => ExistsAsync<ExternalDbConnection>(db, id, ct)).WithMessage("Import connection {PropertyValue} does not exist.");
        RuleFor(x => x.EntityType).NotEmpty().MaximumLength(200).Matches("^[A-Za-z][A-Za-z0-9_.-]*$");
        RuleFor(x => x.SourceQuery).NotEmpty().MaximumLength(10000).Must(DataImportController.IsReadOnlySelect).WithMessage("Source query must contain one read-only SELECT or WITH statement using approved SQL syntax.");
        RuleFor(x => x.FieldMappingsJson).Must(ValidationRules.IsJsonObject).When(x => !string.IsNullOrWhiteSpace(x.FieldMappingsJson));
    }
}

internal static class ValidationRules
{
    public static bool IsProvider(string? value, params string[] expected)
        => expected.Contains(value, StringComparer.OrdinalIgnoreCase);
    public static bool IsHttpsUrl(string? value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
    public static bool IsHttpsOrLoopbackUrl(string? value)
        => Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || (uri.IsLoopback && uri.Scheme == Uri.UriSchemeHttp));
    public static bool IsBoundedObject(JsonElement value)
    {
        try { return value.ValueKind == JsonValueKind.Object && Encoding.UTF8.GetByteCount(value.GetRawText()) <= 128 * 1024; }
        catch (InvalidOperationException) { return false; }
    }
    public static bool IsJsonObject(string? value)
    {
        try { using var document = JsonDocument.Parse(value!); return document.RootElement.ValueKind == JsonValueKind.Object; }
        catch (JsonException) { return false; }
    }
}
