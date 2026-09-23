namespace AegisCore.Application.Obe;

/// <summary>Course evaluation surveys run in AegisCore, answered by students per enrolled course.</summary>
public interface IOutcomeSurveyService
{
    Task<IReadOnlyList<SurveySummary>> ListAsync(CancellationToken ct = default);
    Task<SurveyDetail> GetAsync(Guid surveyId, CancellationToken ct = default);
    Task<SurveyDetail> SaveAsync(Guid? surveyId, SurveyDefinition definition, Guid? userId, CancellationToken ct = default);
    Task<SurveyDetail> SetStatusAsync(Guid surveyId, string status, CancellationToken ct = default);
    /// <summary>Removes a draft or closed survey with everything answered in it. An open survey must be closed first.</summary>
    Task<int> DeleteAsync(Guid surveyId, CancellationToken ct = default);

    Task<IReadOnlyList<StudentEvaluation>> ForStudentAsync(Guid studentId, CancellationToken ct = default);
    Task<EvaluationForm> FormAsync(Guid studentId, Guid surveyId, Guid courseId, CancellationToken ct = default);
    Task<EvaluationReceipt> SubmitAsync(Guid studentId, Guid surveyId, Guid courseId, IReadOnlyDictionary<string, string?> answers, CancellationToken ct = default);
}

/// <summary>A survey as written by the registrar. Questions replace the existing ones while the survey is a draft.</summary>
public sealed record SurveyDefinition(
    string Title, string? Instructions, string Semester, int ScaleMax, bool AskCourseClos,
    string? CloSection, string? CloPrompt, IReadOnlyList<SurveyQuestionDefinition> Questions, IReadOnlyList<Guid>? CourseIds = null);

public sealed record SurveyQuestionDefinition(string Section, string Text, string Kind, string? Category, Guid? ProgramOutcomeId, bool Required = true);

public sealed record SurveySummary(Guid Id, string Title, string Semester, string Status, int Questions, bool AskCourseClos,
    int Eligible, int Completed, DateTime CreatedAt, DateTime? OpenedAt, DateTime? ClosedAt);

public sealed record SurveyDetail(Guid Id, string Title, string? Instructions, string Semester, string Status, int ScaleMax,
    bool AskCourseClos, string CloSection, string CloPrompt, IReadOnlyList<SurveyQuestionView> Questions,
    IReadOnlyList<SurveyCourseProgress> Courses, DateTime? OpenedAt, DateTime? ClosedAt, bool AllCourses);

public sealed record SurveyQuestionView(Guid Id, int SortOrder, string Section, string Text, string Kind, string? Category, Guid? ProgramOutcomeId, string? ProgramOutcomeCode, bool Required);

/// <summary>Response rate for one course: students enrolled, students who have answered, and the CLOs the survey will ask about.</summary>
public sealed record SurveyCourseProgress(Guid CourseId, string Code, string Title, string? Instructor, int Enrolled, int Completed, IReadOnlyList<string> Clos);

public sealed record StudentEvaluation(Guid SurveyId, string Title, string Semester, Guid CourseId, string CourseCode, string CourseTitle,
    string? Instructor, bool Completed, DateTime? CompletedAt, int Questions);

public sealed record EvaluationForm(Guid SurveyId, string Title, string? Instructions, string Semester, int ScaleMax,
    Guid CourseId, string CourseCode, string CourseTitle, string? Instructor, IReadOnlyList<EvaluationSection> Sections, int Questions);

public sealed record EvaluationSection(string Title, string? Prompt, IReadOnlyList<EvaluationQuestion> Questions);

/// <summary><paramref name="Key"/> is what the answer is submitted under: <c>clo:{id}</c> or <c>q:{id}</c>.</summary>
public sealed record EvaluationQuestion(string Key, int Number, string Text, string Kind, bool Required, string? Code);

public sealed record EvaluationReceipt(Guid SurveyId, Guid CourseId, DateTime CompletedAt, int Ratings, int Comments);

/// <summary>A request the service refuses; the message is written for the person who made it.</summary>
public sealed class SurveyRuleException(string message, int status = 400) : Exception(message)
{
    public int Status { get; } = status;
}
