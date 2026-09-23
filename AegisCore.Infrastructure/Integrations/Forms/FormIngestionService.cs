using System.Text.Json;
using AegisCore.Application.Integrations;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Integrations.Forms;

/// <summary>
/// Stores an incoming submission and turns its mapped answers into <see cref="OutcomeSurveyResponse"/>
/// rows. The submission is kept as received, so a mapping completed later can be re-applied to it
/// (<see cref="ReprocessAsync"/>) instead of the answers being lost.
/// </summary>
public sealed class FormIngestionService(AegisDbContext db) : IFormIngestionService
{
    public const int MaxAnswersPerSubmission = 500;

    /// <summary>The provider that marks a source as one of AegisCore's own forms rather than an external tool.</summary>
    public const string NativeProvider = "aegiscore-forms";

    public static string ChannelFor(FormIntegrationSource source)
        => source.Provider == NativeProvider ? "native" : $"external:{source.Provider}";

    public async Task<FormIngestionResult> IngestAsync(Guid sourceId, FormSubmissionEnvelope envelope, CancellationToken ct = default)
    {
        var source = await db.FormIntegrationSources.SingleAsync(s => s.Id == sourceId, ct);
        var channel = ChannelFor(source);
        if (envelope.Answers.Count > MaxAnswersPerSubmission) throw new FormatException($"A submission may carry at most {MaxAnswersPerSubmission} answers.");
        var externalId = envelope.ExternalSubmissionId.Trim();
        if (externalId.Length is 0 or > 200) throw new FormatException("The submission id must be 1–200 characters.");

        var existing = await db.FormIntegrationSubmissions.AsNoTracking()
            .SingleOrDefaultAsync(s => s.SourceId == sourceId && s.ExternalSubmissionId == externalId, ct);
        // Form tools retry deliveries; a second copy is acknowledged, never counted twice.
        if (existing is not null)
            return new FormIngestionResult(existing.Id, existing.Status, true, existing.FieldCount, existing.RecordedCount, existing.UnmappedCount, Split(existing.Problems));

        // A field id repeated inside one payload keeps its last answer, as the form tool would show it.
        var answers = envelope.Answers.GroupBy(a => a.FieldId, StringComparer.Ordinal).Select(g => g.Last()).ToList();
        var mappings = await db.FormFieldMappings.Where(m => m.SourceId == sourceId).ToListAsync(ct);
        var known = mappings.Select(m => m.ExternalFieldId).ToHashSet(StringComparer.Ordinal);
        var nextOrder = mappings.Count == 0 ? 0 : mappings.Max(m => m.SortOrder) + 1;
        foreach (var answer in answers.Where(a => !known.Contains(a.FieldId)))
        {
            var mapping = new FormFieldMapping
            {
                Id = Guid.NewGuid(), TenantId = source.TenantId, SourceId = sourceId, ExternalFieldId = answer.FieldId,
                QuestionText = Truncate(answer.Label ?? answer.FieldId, 1000), Target = FormFieldTargets.Unmapped, SortOrder = nextOrder++,
            };
            db.FormFieldMappings.Add(mapping);
            mappings.Add(mapping);
        }
        // Question text the tool sends replaces a placeholder that was only the field id.
        foreach (var answer in answers.Where(a => a.Label is not null))
        {
            var mapping = mappings.First(m => m.ExternalFieldId == answer.FieldId);
            if (mapping.QuestionText == mapping.ExternalFieldId) mapping.QuestionText = Truncate(answer.Label!, 1000);
        }

        var submission = new FormIntegrationSubmission
        {
            Id = Guid.NewGuid(), TenantId = source.TenantId, SourceId = sourceId, ExternalSubmissionId = externalId,
            ReceivedAt = DateTime.UtcNow, FieldCount = answers.Count,
            AnswersJson = JsonSerializer.Serialize(answers.ToDictionary(a => a.FieldId, a => a.Value, StringComparer.Ordinal)),
        };
        db.FormIntegrationSubmissions.Add(submission);
        source.LastReceivedAt = submission.ReceivedAt;

        var context = await ResolutionContext.LoadAsync(db, ct);
        Apply(source, submission, answers.ToDictionary(a => a.FieldId, a => a.Value, StringComparer.Ordinal), mappings, context, channel);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Two deliveries of the same submission raced past the lookup; the unique index kept one.
            db.ChangeTracker.Clear();
            var winner = await db.FormIntegrationSubmissions.AsNoTracking()
                .SingleOrDefaultAsync(s => s.SourceId == sourceId && s.ExternalSubmissionId == externalId, ct);
            if (winner is null) throw;
            return new FormIngestionResult(winner.Id, winner.Status, true, winner.FieldCount, winner.RecordedCount, winner.UnmappedCount, Split(winner.Problems));
        }
        return new FormIngestionResult(submission.Id, submission.Status, false, submission.FieldCount, submission.RecordedCount, submission.UnmappedCount, Split(submission.Problems));
    }

    public async Task<FormReprocessResult> ReprocessAsync(Guid sourceId, CancellationToken ct = default)
    {
        var source = await db.FormIntegrationSources.SingleAsync(s => s.Id == sourceId, ct);
        var mappings = await db.FormFieldMappings.Where(m => m.SourceId == sourceId).ToListAsync(ct);
        var submissions = await db.FormIntegrationSubmissions.Where(s => s.SourceId == sourceId).ToListAsync(ct);
        var ids = submissions.Select(s => s.Id).ToList();
        await db.OutcomeSurveyResponses.Where(r => r.SubmissionId != null && ids.Contains(r.SubmissionId.Value)).ExecuteDeleteAsync(ct);
        var context = await ResolutionContext.LoadAsync(db, ct);
        var channel = ChannelFor(source);
        foreach (var submission in submissions)
        {
            var answers = JsonSerializer.Deserialize<Dictionary<string, string>>(submission.AnswersJson) ?? [];
            Apply(source, submission, answers, mappings, context, channel);
        }
        await db.SaveChangesAsync(ct);
        return new FormReprocessResult(
            submissions.Count,
            submissions.Count(s => s.Status == "Recorded"),
            submissions.Count(s => s.Status == "Partial"),
            submissions.Count(s => s.Status == "Held"),
            submissions.Sum(s => s.RecordedCount));
    }

    private void Apply(FormIntegrationSource source, FormIntegrationSubmission submission, IReadOnlyDictionary<string, string> answers,
        IReadOnlyList<FormFieldMapping> mappings, ResolutionContext context, string channel)
    {
        var problems = new List<string>();
        var byField = mappings.ToDictionary(m => m.ExternalFieldId, StringComparer.Ordinal);
        string? Answer(string target) => mappings.Where(m => m.Target == target)
            .Select(m => answers.TryGetValue(m.ExternalFieldId, out var value) ? value : null)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();

        var courseText = Answer(FormFieldTargets.Course);
        ResolutionContext.CourseRow? course = null;
        if (courseText is not null)
        {
            course = context.FindCourse(courseText);
            if (course is null) problems.Add($"No course matches \"{Truncate(courseText, 80)}\".");
        }
        else if (source.DefaultCourseId is Guid fixedCourse)
        {
            course = context.Courses.FirstOrDefault(c => c.Id == fixedCourse);
            if (course is null) problems.Add("The form's fixed course no longer exists.");
        }
        else problems.Add("No field is mapped to the course, and the form has no fixed course.");

        var instructor = Answer(FormFieldTargets.Instructor) ?? (course is null ? null : context.InstructorFor(course.Id));
        var semester = Answer(FormFieldTargets.Semester) ?? source.DefaultSemester;
        var unmapped = answers.Keys.Count(field => !byField.TryGetValue(field, out var m) || m.Target == FormFieldTargets.Unmapped);

        submission.CourseId = course?.Id;
        submission.InstructorName = instructor is null ? null : Truncate(instructor, 200);
        submission.Semester = semester is null ? null : Truncate(semester, 50);
        submission.UnmappedCount = unmapped;
        submission.ProcessedAt = DateTime.UtcNow;
        submission.RecordedCount = 0;

        if (course is null)
        {
            // Without a course nothing can be reported; the answers wait for the mapping to be fixed.
            submission.Status = "Held";
            submission.Problems = Join(problems);
            return;
        }

        var recorded = 0;
        foreach (var (field, value) in answers)
        {
            if (!byField.TryGetValue(field, out var mapping) || !FormFieldTargets.IsRating(mapping.Target)) continue;
            if (string.IsNullOrWhiteSpace(value)) continue; // A skipped optional question is not a rating of zero.
            var score = FormPayloadText.Score(value, source.ScaleMax);
            if (score is null || score < 1 || score > source.ScaleMax)
            {
                problems.Add($"{field}: \"{Truncate(value, 40)}\" is not a rating on the 1–{source.ScaleMax} scale.");
                continue;
            }

            Guid? cloId = null, poId = null;
            if (mapping.Target == FormFieldTargets.Clo)
            {
                cloId = context.FindClo(course.Id, mapping.CloCode);
                if (cloId is null) { problems.Add($"{field}: {mapping.CloCode ?? "(no code)"} is not defined for {course.Code}."); continue; }
            }
            else if (mapping.Target == FormFieldTargets.Plo)
            {
                poId = mapping.ProgramOutcomeId is Guid po && context.ProgramOutcomeIds.Contains(po) ? po : null;
                if (poId is null) { problems.Add($"{field}: the programme outcome it is mapped to no longer exists."); continue; }
            }

            db.OutcomeSurveyResponses.Add(new OutcomeSurveyResponse
            {
                Id = Guid.NewGuid(), TenantId = submission.TenantId, Channel = channel, SubmissionId = submission.Id,
                ResponseKey = $"{submission.SourceId:N}:{submission.ExternalSubmissionId}",
                CourseId = course.Id, CourseLearningOutcomeId = cloId, ProgramOutcomeId = poId,
                Category = mapping.Target == FormFieldTargets.Satisfaction ? (string.IsNullOrWhiteSpace(mapping.Category) ? "General" : mapping.Category) : null,
                QuestionKey = field, QuestionText = mapping.QuestionText, Score = score.Value, ScaleMax = source.ScaleMax,
                InstructorName = submission.InstructorName, Semester = submission.Semester, RecordedAt = submission.ReceivedAt,
            });
            recorded++;
        }

        submission.RecordedCount = recorded;
        submission.Status = unmapped == 0 && problems.Count == 0 ? "Recorded" : "Partial";
        if (unmapped > 0) problems.Insert(0, $"{unmapped} field(s) are not mapped yet.");
        submission.Problems = Join(problems);
    }

    private static string Truncate(string value, int length) => value.Length <= length ? value : value[..length];
    private static string? Join(List<string> problems) => problems.Count == 0 ? null : Truncate(string.Join("\n", problems), 2000);
    private static IReadOnlyList<string> Split(string? problems) => string.IsNullOrEmpty(problems) ? [] : problems.Split('\n');

    /// <summary>The tenant's courses, outcomes and instructors, loaded once per ingestion or reprocess.</summary>
    private sealed class ResolutionContext
    {
        public sealed record CourseRow(Guid Id, string Code, string Title);

        public required List<CourseRow> Courses { get; init; }
        public required Dictionary<(Guid Course, string Code), Guid> Clos { get; init; }
        public required HashSet<Guid> ProgramOutcomeIds { get; init; }
        public required Dictionary<Guid, string> Instructors { get; init; }

        public static async Task<ResolutionContext> LoadAsync(AegisDbContext db, CancellationToken ct)
        {
            var courses = await db.Courses.AsNoTracking().Select(c => new CourseRow(c.Id, c.Code ?? "", c.Title)).ToListAsync(ct);
            var clos = await db.CourseLearningOutcomes.AsNoTracking().Select(c => new { c.CourseId, c.Code, c.Id }).ToListAsync(ct);
            var pos = await db.ProgramOutcomes.AsNoTracking().Select(p => p.Id).ToListAsync(ct);
            var assignments = await db.CourseAssignments.AsNoTracking()
                .OrderBy(a => a.CreatedAt)
                .Select(a => new { a.CourseId, Name = a.Faculty.FirstName + " " + a.Faculty.LastName })
                .ToListAsync(ct);
            return new ResolutionContext
            {
                Courses = courses,
                Clos = clos.GroupBy(c => (c.CourseId, c.Code.ToUpperInvariant())).ToDictionary(g => g.Key, g => g.First().Id),
                ProgramOutcomeIds = pos.ToHashSet(),
                Instructors = assignments.GroupBy(a => a.CourseId).ToDictionary(g => g.Key, g => g.First().Name),
            };
        }

        /// <summary>"CS101", "cs101", "CS101 - Programming Fundamentals" and the exact title all find CS101.</summary>
        public CourseRow? FindCourse(string text)
        {
            var value = text.Trim();
            var byCode = Courses.FirstOrDefault(c => c.Code.Length > 0 && string.Equals(c.Code, value, StringComparison.OrdinalIgnoreCase));
            if (byCode is not null) return byCode;
            var leading = value.Split([' ', '-', '–', ':', '|'], 2, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            byCode = leading is null ? null : Courses.FirstOrDefault(c => c.Code.Length > 0 && string.Equals(c.Code, leading, StringComparison.OrdinalIgnoreCase));
            return byCode ?? Courses.FirstOrDefault(c => string.Equals(c.Title, value, StringComparison.OrdinalIgnoreCase));
        }

        public Guid? FindClo(Guid courseId, string? code)
            => code is not null && Clos.TryGetValue((courseId, code.Trim().ToUpperInvariant()), out var id) ? id : null;

        public string? InstructorFor(Guid courseId) => Instructors.TryGetValue(courseId, out var name) ? name : null;
    }
}
