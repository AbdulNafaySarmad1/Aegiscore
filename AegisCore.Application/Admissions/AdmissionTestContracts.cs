using AegisCore.Domain.Entities;

namespace AegisCore.Application.Admissions;

// ---- Syllabus OCR (delegates to the aegiscore-clip sidecar) ----

public sealed record SyllabusOcrOutcome(string Text, string Method);

public interface ISyllabusOcrService
{
    Task<SyllabusOcrOutcome> ExtractAsync(Stream pdf, string fileName, CancellationToken ct = default);
}

// ---- Master subjects + per-program test config ----

public sealed record SubjectInput(string Name, string Code);
public sealed record TestSubjectSlot(Guid SubjectId, int QuestionCount, int MarksPerQuestion);
public sealed record TestProfileInput(Guid IntakeId, Guid ProgramId, int PassMark, int TimeLimitMinutes, IReadOnlyList<TestSubjectSlot> Subjects);

public sealed record TestProfileView(
    Guid Id,
    Guid IntakeId,
    Guid ProgramId,
    int PassMark,
    int TimeLimitMinutes,
    IReadOnlyList<TestSubjectSlot> Subjects);

public interface IAdmissionTestConfigService
{
    Task<AdmissionSubject> CreateSubjectAsync(SubjectInput input, CancellationToken ct = default);
    Task<AdmissionSubject> SetSubjectActiveAsync(Guid subjectId, bool isActive, CancellationToken ct = default);
    Task<IReadOnlyList<AdmissionSubject>> ListSubjectsAsync(bool includeInactive, CancellationToken ct = default);

    Task<TestProfileView> UpsertProfileAsync(TestProfileInput input, CancellationToken ct = default);
    Task<TestProfileView?> GetProfileAsync(Guid intakeId, Guid programId, CancellationToken ct = default);
}

// ---- Syllabus ingestion + question bank ----

public sealed record SyllabusIngestResult(Guid SyllabusDocumentId, string OcrMethod, int QuestionsGenerated);

public sealed record QuestionEditInput(
    string QuestionText, string OptionA, string OptionB, string OptionC, string OptionD, string CorrectOption, string DifficultyLevel);

public interface IQuestionBankService
{
    Task<SyllabusIngestResult> IngestSyllabusAsync(
        Guid subjectId, Stream pdf, string fileName, int questionsToGenerate, Guid actorId, CancellationToken ct = default);

    Task<IReadOnlyList<AdmissionQuestion>> ListAsync(Guid? subjectId, AdmissionQuestionStatus? status, CancellationToken ct = default);
    Task ReviewAsync(Guid questionId, bool approve, Guid actorId, CancellationToken ct = default);
    Task<AdmissionQuestion> EditAsync(Guid questionId, QuestionEditInput input, CancellationToken ct = default);

    Task<string> GetActivePromptAsync(CancellationToken ct = default);
    Task SetActivePromptAsync(string systemPrompt, Guid actorId, CancellationToken ct = default);
}

/// <summary>Wraps the tenant's configured LLM to turn a syllabus chunk into MCQ JSON.</summary>
public interface IAdmissionQuestionGenerator
{
    Task<IReadOnlyList<GeneratedQuestion>> GenerateAsync(
        Guid tenantId, string systemPrompt, string syllabusChunk, int count, CancellationToken ct = default);
}

public sealed record GeneratedQuestion(
    string QuestionText, string OptionA, string OptionB, string OptionC, string OptionD, string CorrectOption, string DifficultyLevel);

// ---- Test scheduling ----

public sealed record ScheduleTestResult(Guid ApplicantTestId, DateTimeOffset ExpiresAt, string PaperHash, int TotalQuestions);

public interface ITestSchedulingService
{
    Task<ScheduleTestResult> ScheduleAsync(Guid applicantProfileId, DateTimeOffset? expiresAt, CancellationToken ct = default);
}

// ---- On-campus terminal ----

public sealed record TerminalQuestion(int Index, Guid QuestionId, string QuestionText, IReadOnlyList<TerminalOption> Options);
public sealed record TerminalOption(string Key, string Text);

public sealed record TerminalStartResult(
    Guid ApplicantTestId,
    string ApplicantName,
    int TimeLimitMinutes,
    DateTimeOffset ExpiresAt,
    int TotalQuestions,
    IReadOnlyList<TerminalQuestion> Questions);

public sealed record TerminalSubmitResult(bool Passed);

public interface ITestTerminalService
{
    Task<TerminalStartResult> StartAsync(string applicantId, CancellationToken ct = default);
    Task RecordAnswerAsync(Guid applicantTestId, Guid questionId, string optionKey, CancellationToken ct = default);
    Task<TerminalSubmitResult> SubmitAsync(Guid applicantTestId, bool autoSubmitted, CancellationToken ct = default);
}

// ---- Proctor ----

public sealed record ProctorRow(
    Guid ApplicantTestId,
    string ApplicantId,
    string ApplicantName,
    DateTimeOffset? StartedAt,
    DateTimeOffset ExpiresAt,
    string Status,
    string? ProctorNote);

public interface ITestProctorService
{
    Task<IReadOnlyList<ProctorRow>> LiveAsync(CancellationToken ct = default);
    Task ForceSubmitAsync(Guid applicantTestId, Guid proctorId, CancellationToken ct = default);
    Task FlagAsync(Guid applicantTestId, Guid proctorId, string note, CancellationToken ct = default);
}
