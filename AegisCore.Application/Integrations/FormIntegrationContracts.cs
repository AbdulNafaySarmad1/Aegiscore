namespace AegisCore.Application.Integrations;

/// <summary>
/// A submission from any form tool, reduced to what AegisCore needs: which form, which submission,
/// and field id → answer pairs. Vendor adapters produce this; nothing downstream sees a vendor's
/// wire format (docs/integrations/external-forms.md, "The contract").
/// </summary>
public sealed record FormSubmissionEnvelope(string? ExternalFormId, string ExternalSubmissionId, IReadOnlyList<FormAnswer> Answers);

/// <summary>One answer. <paramref name="Label"/> is the question text when the tool sends it.</summary>
public sealed record FormAnswer(string FieldId, string? Label, string Value);

/// <summary>Turns one form tool's webhook body into an envelope. Throws <see cref="FormatException"/> for a body it cannot read.</summary>
public interface IFormPayloadAdapter
{
    string Provider { get; }
    FormSubmissionEnvelope Parse(string contentType, string body, IReadOnlyDictionary<string, string> formFields);
}

public sealed record FormIngestionResult(Guid SubmissionId, string Status, bool Duplicate, int FieldCount, int RecordedCount, int UnmappedCount, IReadOnlyList<string> Problems);

public sealed record FormReprocessResult(int Submissions, int Recorded, int Partial, int Held, int Answers);

public interface IFormIngestionService
{
    /// <summary>Stores the submission, records every newly seen field as unmapped, and applies the source's mapping.</summary>
    Task<FormIngestionResult> IngestAsync(Guid sourceId, FormSubmissionEnvelope envelope, CancellationToken ct = default);
    /// <summary>Re-applies the current mapping to every stored submission of a source.</summary>
    Task<FormReprocessResult> ReprocessAsync(Guid sourceId, CancellationToken ct = default);
}

public interface IIndirectAssessmentReportService
{
    Task<IndirectAssessmentReport> GetReportAsync(Guid? courseId, string? semester, CancellationToken ct = default, Guid? surveyId = null);
}

public sealed record IndirectAssessmentReport(
    DateTime GeneratedAt,
    int Respondents,
    int Answers,
    DateTime? LastRecordedAt,
    IReadOnlyList<ChannelCount> Channels,
    IReadOnlyList<CourseIndirectResult> Courses,
    IReadOnlyList<InstructorIndirectResult> Instructors,
    IReadOnlyList<ProgramOutcomeIndirectResult> ProgramOutcomes);

public sealed record ChannelCount(string Channel, int Respondents, int Answers);

/// <summary>Mean is on the form's own scale; Favourable is the share of answers in the top two points of it.</summary>
public sealed record RatingSummary(int Answers, decimal Mean, int ScaleMax, decimal FavourablePercent);

public sealed record CourseIndirectResult(
    Guid CourseId, string Code, string Title, IReadOnlyList<string> Instructors, int Respondents,
    RatingSummary Overall, IReadOnlyList<OutcomeIndirectResult> Clos, IReadOnlyList<CategoryIndirectResult> Satisfaction);

public sealed record OutcomeIndirectResult(Guid OutcomeId, string Code, string Description, RatingSummary Rating);

public sealed record CategoryIndirectResult(string Category, RatingSummary Rating);

public sealed record InstructorIndirectResult(string Instructor, IReadOnlyList<string> Courses, int Respondents, RatingSummary Rating);

/// <summary>
/// A programme outcome's indirect attainment: answers rating it directly, plus answers about the
/// CLOs mapped to it, weighted by mapping strength exactly as the direct attainment is.
/// </summary>
public sealed record ProgramOutcomeIndirectResult(Guid OutcomeId, string Code, string Description, int Answers, decimal PercentOfScale, IReadOnlyList<string> ContributingClos);
