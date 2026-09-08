using AegisCore.API.Controllers;
using AegisCore.Application.Admissions;
using AegisCore.Domain.Entities;
using FluentValidation;

namespace AegisCore.API.Validation;

// ---- Applicant portal (public + applicant-scheme) ----

public sealed class ApplicantSignupRequestValidator : SecureValidator<ApplicantPortalController.ApplicantSignupRequest>
{
    public ApplicantSignupRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320).Must(IsTrimmed)
            .WithMessage("Email must not have leading or trailing spaces.");
        RuleFor(x => x.IntakeId).NotEmpty();
        RuleFor(x => x.ProgramId).NotEmpty();
    }
}

public sealed class ApplicantVerifyRequestValidator : SecureValidator<ApplicantPortalController.ApplicantVerifyRequest>
{
    public ApplicantVerifyRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320).Must(IsTrimmed);
        RuleFor(x => x.Otp).NotEmpty().Matches("^[0-9]{6}$").WithMessage("The verification code is 6 digits.");
        RuleFor(x => x.IntakeId).NotEmpty();
        RuleFor(x => x.ProgramId).NotEmpty();
    }
}

public sealed class ApplicantPersonalDetailsValidator : SecureValidator<ApplicantPersonalDetails>
{
    public ApplicantPersonalDetailsValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200).Must(IsTrimmed);
        RuleFor(x => x.DateOfBirth)
            .Must(dob => dob > new DateOnly(1900, 1, 1) && dob < DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-10)))
            .WithMessage("Enter a valid date of birth.");
        RuleFor(x => x.Gender).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Nationality).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(30).Matches(@"^\+?[0-9\-\s]{7,20}$")
            .WithMessage("Enter a valid phone number.");
    }
}

// ---- Admin configuration ----

public sealed class IntakeInputValidator : SecureValidator<IntakeInput>
{
    public IntakeInputValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Semester).IsInEnum();
        RuleFor(x => x.Name).MaximumLength(150);
        RuleFor(x => x)
            .Must(x => x.OpensAt is null || x.ClosesAt is null || x.ClosesAt > x.OpensAt)
            .WithMessage("The intake close date must be after the open date.");
    }
}

public sealed class DocumentRequirementInputValidator : SecureValidator<DocumentRequirementInput>
{
    public DocumentRequirementInputValidator()
    {
        RuleFor(x => x.IntakeId).NotEmpty();
        RuleFor(x => x.ProgramId).NotEmpty();
        RuleFor(x => x.DocumentType).IsInEnum();
        RuleFor(x => x.AcceptedFormat).IsInEnum();
        RuleFor(x => x.MaxFileSizeMb).InclusiveBetween(1, 50);
    }
}

public sealed class IntakeStatusRequestValidator : SecureValidator<AdmissionsAdminController.IntakeStatusRequest>
{
    public IntakeStatusRequestValidator() => RuleFor(x => x.Status).IsInEnum();
}

public sealed class ProgramCodeRequestValidator : SecureValidator<AdmissionsAdminController.ProgramCodeRequest>
{
    public ProgramCodeRequestValidator()
        => RuleFor(x => x.Code).NotEmpty().Matches("^[A-Za-z0-9]{2,8}$")
            .WithMessage("Program code must be 2-8 letters or digits.");
}

public sealed class DocumentReviewRequestValidator : SecureValidator<AdmissionsAdminController.DocumentReviewRequest>
{
    public DocumentReviewRequestValidator()
    {
        RuleFor(x => x.RejectionReason).MaximumLength(1000);
        RuleFor(x => x.RejectionReason).NotEmpty().When(x => !x.Approve)
            .WithMessage("A rejection reason is required when rejecting a document.");
    }
}

public sealed class ApplicantTransitionRequestValidator : SecureValidator<AdmissionsAdminController.ApplicantTransitionRequest>
{
    public ApplicantTransitionRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Note).MaximumLength(1000);
    }
}

public sealed class BulkTransitionRequestValidator : SecureValidator<AdmissionsAdminController.BulkTransitionRequest>
{
    public BulkTransitionRequestValidator()
    {
        RuleFor(x => x.ApplicantIds).NotEmpty().Must(ids => ids.Count <= 500)
            .WithMessage("A bulk action is limited to 500 applicants.");
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Note).MaximumLength(1000);
    }
}

// ---- P21 AI Test Engine ----

public sealed class SubjectInputValidator : SecureValidator<SubjectInput>
{
    public SubjectInputValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120).Must(IsTrimmed);
        RuleFor(x => x.Code).NotEmpty().Matches("^[A-Za-z0-9]{2,30}$").WithMessage("Subject code must be 2-30 letters or digits.");
    }
}

public sealed class TestProfileInputValidator : SecureValidator<TestProfileInput>
{
    public TestProfileInputValidator()
    {
        RuleFor(x => x.IntakeId).NotEmpty();
        RuleFor(x => x.ProgramId).NotEmpty();
        RuleFor(x => x.PassMark).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TimeLimitMinutes).InclusiveBetween(1, 600);
        RuleFor(x => x.Subjects).NotEmpty().Must(s => s.Count <= 20).WithMessage("A test profile allows at most 20 subjects.");
        RuleForEach(x => x.Subjects).ChildRules(slot =>
        {
            slot.RuleFor(s => s.SubjectId).NotEmpty();
            slot.RuleFor(s => s.QuestionCount).InclusiveBetween(1, 200);
            slot.RuleFor(s => s.MarksPerQuestion).InclusiveBetween(1, 20);
        });
    }
}

public sealed class QuestionEditInputValidator : SecureValidator<QuestionEditInput>
{
    public QuestionEditInputValidator()
    {
        RuleFor(x => x.QuestionText).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.OptionA).NotEmpty().MaximumLength(600);
        RuleFor(x => x.OptionB).NotEmpty().MaximumLength(600);
        RuleFor(x => x.OptionC).NotEmpty().MaximumLength(600);
        RuleFor(x => x.OptionD).NotEmpty().MaximumLength(600);
        RuleFor(x => x.CorrectOption).NotEmpty().Must(v => v.Trim().ToUpperInvariant() is "A" or "B" or "C" or "D")
            .WithMessage("CorrectOption must be A, B, C or D.");
        RuleFor(x => x.DifficultyLevel).NotEmpty().MaximumLength(30);
    }
}

public sealed class PromptRequestValidator : SecureValidator<AdmissionsTestController.PromptRequest>
{
    public PromptRequestValidator()
        => RuleFor(x => x.SystemPrompt).NotEmpty().MinimumLength(20).MaximumLength(8000);
}

public sealed class ProctorFlagRequestValidator : SecureValidator<AdmissionsTestController.ProctorFlagRequest>
{
    public ProctorFlagRequestValidator()
        => RuleFor(x => x.Note).NotEmpty().MaximumLength(500);
}

public sealed class TerminalStartRequestValidator : SecureValidator<TestTerminalController.StartRequest>
{
    public TerminalStartRequestValidator()
        => RuleFor(x => x.ApplicantId).NotEmpty().MaximumLength(40)
            .Matches("^APP-[0-9]{4}[FSW]-[A-Za-z0-9]{2,8}-[0-9A-Za-z]{8}$")
            .WithMessage("Enter a valid Applicant ID.");
}

public sealed class TerminalAnswerRequestValidator : SecureValidator<TestTerminalController.AnswerRequest>
{
    public TerminalAnswerRequestValidator()
    {
        RuleFor(x => x.QuestionId).NotEmpty();
        RuleFor(x => x.OptionKey).NotEmpty().Must(v => v.Trim().ToUpperInvariant() is "A" or "B" or "C" or "D")
            .WithMessage("Option must be A, B, C or D.");
    }
}
