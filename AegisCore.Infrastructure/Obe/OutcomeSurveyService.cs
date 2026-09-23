using AegisCore.Application.Obe;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Obe;

public sealed class OutcomeSurveyService(AegisDbContext db) : IOutcomeSurveyService
{
    public const int MaxQuestions = 300;
    private static readonly string[] Kinds = ["Rating", "Comment"];

    // ── Registrar ────────────────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<SurveySummary>> ListAsync(CancellationToken ct = default)
    {
        var surveys = await db.OutcomeSurveys.AsNoTracking().OrderByDescending(s => s.CreatedAt).ToListAsync(ct);
        var questionCounts = await db.OutcomeSurveyQuestions.AsNoTracking().GroupBy(q => q.SurveyId).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, ct);
        var completed = await db.OutcomeSurveyCompletions.AsNoTracking().GroupBy(c => c.SurveyId).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, ct);
        var enrolled = await db.Enrollments.AsNoTracking().Where(e => e.EnrollmentStatus == EnrollmentStatus.Active)
            .GroupBy(e => e.CourseId).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, ct);
        var scopes = await db.OutcomeSurveyCourses.AsNoTracking().ToListAsync(ct);
        return surveys.Select(s =>
        {
            var scoped = scopes.Where(c => c.SurveyId == s.Id).Select(c => c.CourseId).ToList();
            var eligible = scoped.Count == 0 ? enrolled.Values.Sum() : scoped.Sum(c => enrolled.GetValueOrDefault(c));
            return new SurveySummary(s.Id, s.Title, s.Semester, s.Status, questionCounts.GetValueOrDefault(s.Id), s.AskCourseClos,
                eligible, completed.GetValueOrDefault(s.Id), s.CreatedAt, s.OpenedAt, s.ClosedAt);
        }).ToList();
    }

    public async Task<SurveyDetail> GetAsync(Guid surveyId, CancellationToken ct = default)
    {
        var survey = await db.OutcomeSurveys.AsNoTracking().SingleOrDefaultAsync(s => s.Id == surveyId, ct)
            ?? throw new SurveyRuleException("The survey was not found.", 404);
        var questions = await db.OutcomeSurveyQuestions.AsNoTracking().Where(q => q.SurveyId == surveyId).OrderBy(q => q.SortOrder).ToListAsync(ct);
        var poIds = questions.Where(q => q.ProgramOutcomeId != null).Select(q => q.ProgramOutcomeId!.Value).ToList();
        var poCodes = await db.ProgramOutcomes.AsNoTracking().Where(p => poIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Code, ct);

        var enrolled = await db.Enrollments.AsNoTracking().Where(e => e.EnrollmentStatus == EnrollmentStatus.Active)
            .GroupBy(e => e.CourseId).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, ct);
        var completed = await db.OutcomeSurveyCompletions.AsNoTracking().Where(c => c.SurveyId == surveyId)
            .GroupBy(c => c.CourseId).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, ct);
        var scoped = await ScopeAsync(surveyId, ct);
        var courseIds = enrolled.Keys.Where(c => scoped is null || scoped.Contains(c)).ToList();
        var courses = await db.Courses.AsNoTracking().Where(c => courseIds.Contains(c.Id)).OrderBy(c => c.Code).Select(c => new { c.Id, c.Code, c.Title }).ToListAsync(ct);
        var clos = await db.CourseLearningOutcomes.AsNoTracking().Where(c => courseIds.Contains(c.CourseId)).OrderBy(c => c.SortOrder).ThenBy(c => c.Code)
            .Select(c => new { c.CourseId, c.Code }).ToListAsync(ct);
        var instructors = await InstructorsAsync(courseIds, ct);

        return new SurveyDetail(survey.Id, survey.Title, survey.Instructions, survey.Semester, survey.Status, survey.ScaleMax, survey.AskCourseClos,
            survey.CloSection, survey.CloPrompt,
            questions.Select(q => new SurveyQuestionView(q.Id, q.SortOrder, q.Section, q.Text, q.Kind, q.Category, q.ProgramOutcomeId,
                q.ProgramOutcomeId is Guid po && poCodes.TryGetValue(po, out var code) ? code : null, q.Required)).ToList(),
            courses.Select(c => new SurveyCourseProgress(c.Id, c.Code ?? "", c.Title, instructors.GetValueOrDefault(c.Id), enrolled[c.Id], completed.GetValueOrDefault(c.Id),
                clos.Where(o => o.CourseId == c.Id).Select(o => o.Code).ToList())).ToList(),
            survey.OpenedAt, survey.ClosedAt, scoped is null);
    }

    public async Task<SurveyDetail> SaveAsync(Guid? surveyId, SurveyDefinition definition, Guid? userId, CancellationToken ct = default)
    {
        Validate(definition);
        var poIds = definition.Questions.Where(q => q.ProgramOutcomeId != null).Select(q => q.ProgramOutcomeId!.Value).Distinct().ToList();
        if (poIds.Count > 0 && await db.ProgramOutcomes.CountAsync(p => poIds.Contains(p.Id), ct) != poIds.Count)
            throw new SurveyRuleException("A question is linked to a programme outcome that does not exist.");

        var courseIds = definition.CourseIds?.Distinct().ToList() ?? [];
        if (courseIds.Count > 0 && await db.Courses.CountAsync(c => courseIds.Contains(c.Id), ct) != courseIds.Count)
            throw new SurveyRuleException("A chosen course does not exist.");

        OutcomeSurvey survey;
        if (surveyId is Guid id)
        {
            survey = await db.OutcomeSurveys.SingleOrDefaultAsync(s => s.Id == id, ct) ?? throw new SurveyRuleException("The survey was not found.", 404);
            if (survey.Status != "Draft") throw new SurveyRuleException("An open or closed survey cannot be edited; students may already have answered it.", 409);
            await db.OutcomeSurveyQuestions.Where(q => q.SurveyId == id).ExecuteDeleteAsync(ct);
            await db.OutcomeSurveyCourses.Where(c => c.SurveyId == id).ExecuteDeleteAsync(ct);
        }
        else
        {
            survey = new OutcomeSurvey { Id = Guid.NewGuid(), CreatedByUserId = userId };
            db.OutcomeSurveys.Add(survey);
        }
        survey.Title = definition.Title.Trim();
        survey.Instructions = string.IsNullOrWhiteSpace(definition.Instructions) ? null : definition.Instructions.Trim();
        survey.Semester = definition.Semester.Trim();
        survey.ScaleMax = definition.ScaleMax;
        survey.AskCourseClos = definition.AskCourseClos;
        survey.CloSection = string.IsNullOrWhiteSpace(definition.CloSection) ? "Course learning outcomes" : definition.CloSection.Trim();
        survey.CloPrompt = string.IsNullOrWhiteSpace(definition.CloPrompt) ? "By the end of this course I am able to:" : definition.CloPrompt.Trim();
        foreach (var courseId in courseIds)
            db.OutcomeSurveyCourses.Add(new OutcomeSurveyCourse { Id = Guid.NewGuid(), SurveyId = survey.Id, CourseId = courseId });
        var order = 0;
        foreach (var q in definition.Questions)
        {
            db.OutcomeSurveyQuestions.Add(new OutcomeSurveyQuestion
            {
                Id = Guid.NewGuid(), SurveyId = survey.Id, SortOrder = order++, Section = q.Section.Trim(), Text = q.Text.Trim(),
                Kind = q.Kind, Required = q.Kind == "Rating" && q.Required,
                Category = q.Kind == "Rating" && q.ProgramOutcomeId is null ? (string.IsNullOrWhiteSpace(q.Category) ? q.Section.Trim() : q.Category.Trim()) : null,
                ProgramOutcomeId = q.Kind == "Rating" ? q.ProgramOutcomeId : null,
            });
        }
        await db.SaveChangesAsync(ct);
        return await GetAsync(survey.Id, ct);
    }

    public async Task<SurveyDetail> SetStatusAsync(Guid surveyId, string status, CancellationToken ct = default)
    {
        var survey = await db.OutcomeSurveys.SingleOrDefaultAsync(s => s.Id == surveyId, ct) ?? throw new SurveyRuleException("The survey was not found.", 404);
        switch (status)
        {
            case "Open":
                if (survey.Status == "Closed") throw new SurveyRuleException("A closed survey stays closed; copy it into a new survey instead.", 409);
                if (!survey.AskCourseClos && !await db.OutcomeSurveyQuestions.AnyAsync(q => q.SurveyId == surveyId, ct))
                    throw new SurveyRuleException("Add at least one question before opening the survey.");
                survey.Status = "Open";
                survey.OpenedAt ??= DateTime.UtcNow;
                break;
            case "Closed":
                if (survey.Status != "Open") throw new SurveyRuleException("Only an open survey can be closed.", 409);
                survey.Status = "Closed";
                survey.ClosedAt = DateTime.UtcNow;
                break;
            default:
                throw new SurveyRuleException("The status must be Open or Closed.");
        }
        await db.SaveChangesAsync(ct);
        return await GetAsync(surveyId, ct);
    }

    public async Task<int> DeleteAsync(Guid surveyId, CancellationToken ct = default)
    {
        var survey = await db.OutcomeSurveys.SingleOrDefaultAsync(s => s.Id == surveyId, ct) ?? throw new SurveyRuleException("The survey was not found.", 404);
        if (survey.Status == "Open") throw new SurveyRuleException("Close the survey before deleting it.", 409);
        var answers = await db.OutcomeSurveyResponses.Where(r => r.SurveyId == surveyId).ExecuteDeleteAsync(ct);
        await db.OutcomeSurveyComments.Where(c => c.SurveyId == surveyId).ExecuteDeleteAsync(ct);
        await db.OutcomeSurveyCompletions.Where(c => c.SurveyId == surveyId).ExecuteDeleteAsync(ct);
        await db.OutcomeSurveyQuestions.Where(q => q.SurveyId == surveyId).ExecuteDeleteAsync(ct);
        await db.OutcomeSurveyCourses.Where(c => c.SurveyId == surveyId).ExecuteDeleteAsync(ct);
        db.OutcomeSurveys.Remove(survey);
        await db.SaveChangesAsync(ct);
        return answers;
    }

    // ── Student ──────────────────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<StudentEvaluation>> ForStudentAsync(Guid studentId, CancellationToken ct = default)
    {
        var surveys = await db.OutcomeSurveys.AsNoTracking().Where(s => s.Status == "Open").OrderBy(s => s.OpenedAt).ToListAsync(ct);
        if (surveys.Count == 0) return [];
        var courses = await db.Enrollments.AsNoTracking().Where(e => e.StudentId == studentId && e.EnrollmentStatus == EnrollmentStatus.Active)
            .Select(e => new { e.CourseId, e.Course.Code, e.Course.Title }).OrderBy(c => c.Code).ToListAsync(ct);
        var courseIds = courses.Select(c => c.CourseId).ToList();
        var surveyIds = surveys.Select(s => s.Id).ToList();
        var scopes = await db.OutcomeSurveyCourses.AsNoTracking().Where(c => surveyIds.Contains(c.SurveyId)).ToListAsync(ct);
        bool Covers(Guid survey, Guid course) => !scopes.Any(c => c.SurveyId == survey) || scopes.Any(c => c.SurveyId == survey && c.CourseId == course);
        var done = await db.OutcomeSurveyCompletions.AsNoTracking().Where(c => c.StudentId == studentId && surveyIds.Contains(c.SurveyId))
            .ToDictionaryAsync(c => (c.SurveyId, c.CourseId), c => c.CompletedAt, ct);
        var instructors = await InstructorsAsync(courseIds, ct);
        var fixedCounts = await db.OutcomeSurveyQuestions.AsNoTracking().Where(q => surveyIds.Contains(q.SurveyId)).GroupBy(q => q.SurveyId)
            .Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, ct);
        var cloCounts = await db.CourseLearningOutcomes.AsNoTracking().Where(c => courseIds.Contains(c.CourseId)).GroupBy(c => c.CourseId)
            .Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, ct);
        return surveys.SelectMany(s => courses.Where(c => Covers(s.Id, c.CourseId)).Select(c => new StudentEvaluation(s.Id, s.Title, s.Semester, c.CourseId, c.Code ?? "", c.Title,
            instructors.GetValueOrDefault(c.CourseId), done.ContainsKey((s.Id, c.CourseId)), done.TryGetValue((s.Id, c.CourseId), out var at) ? at : null,
            fixedCounts.GetValueOrDefault(s.Id) + (s.AskCourseClos ? cloCounts.GetValueOrDefault(c.CourseId) : 0)))).ToList();
    }

    public async Task<EvaluationForm> FormAsync(Guid studentId, Guid surveyId, Guid courseId, CancellationToken ct = default)
    {
        var (survey, course) = await EligibleAsync(studentId, surveyId, courseId, ct);
        if (await db.OutcomeSurveyCompletions.AnyAsync(c => c.SurveyId == surveyId && c.CourseId == courseId && c.StudentId == studentId, ct))
            throw new SurveyRuleException("You have already submitted this evaluation. Thank you.", 409);
        var sections = await SectionsAsync(survey, courseId, ct);
        var instructor = (await InstructorsAsync([courseId], ct)).GetValueOrDefault(courseId);
        return new EvaluationForm(survey.Id, survey.Title, survey.Instructions, survey.Semester, survey.ScaleMax, course.Id, course.Code ?? "", course.Title,
            instructor, sections, sections.Sum(s => s.Questions.Count));
    }

    public async Task<EvaluationReceipt> SubmitAsync(Guid studentId, Guid surveyId, Guid courseId, IReadOnlyDictionary<string, string?> answers, CancellationToken ct = default)
    {
        var (survey, _) = await EligibleAsync(studentId, surveyId, courseId, ct);
        if (await db.OutcomeSurveyCompletions.AnyAsync(c => c.SurveyId == surveyId && c.CourseId == courseId && c.StudentId == studentId, ct))
            throw new SurveyRuleException("You have already submitted this evaluation.", 409);

        var sections = await SectionsAsync(survey, courseId, ct);
        var questions = sections.SelectMany(s => s.Questions.Select(q => (Section: s, Question: q))).ToList();
        var known = questions.Select(q => q.Question.Key).ToHashSet(StringComparer.Ordinal);
        var unknown = answers.Keys.Where(k => !known.Contains(k)).ToList();
        if (unknown.Count > 0) throw new SurveyRuleException("The evaluation changed while it was open; reload it and answer again.");

        var missing = new List<int>();
        var ratings = new List<(EvaluationQuestion Question, EvaluationSection Section, int Score)>();
        var comments = new List<(EvaluationQuestion Question, string Text)>();
        foreach (var (section, question) in questions)
        {
            answers.TryGetValue(question.Key, out var raw);
            var value = raw?.Trim();
            if (question.Kind == "Comment")
            {
                if (!string.IsNullOrEmpty(value)) comments.Add((question, value.Length > 4000 ? value[..4000] : value));
                continue;
            }
            if (string.IsNullOrEmpty(value)) { if (question.Required) missing.Add(question.Number); continue; }
            if (!int.TryParse(value, out var score) || score < 1 || score > survey.ScaleMax)
                throw new SurveyRuleException($"Question {question.Number} needs a rating from 1 to {survey.ScaleMax}.");
            ratings.Add((question, section, score));
        }
        if (missing.Count > 0)
            throw new SurveyRuleException(missing.Count == 1 ? $"Question {missing[0]} is not answered yet." : $"{missing.Count} questions are not answered yet: {string.Join(", ", missing.Take(12))}{(missing.Count > 12 ? "…" : "")}.");

        var fixedQuestions = await db.OutcomeSurveyQuestions.AsNoTracking().Where(q => q.SurveyId == surveyId).ToDictionaryAsync(q => q.Id, ct);
        var instructor = (await InstructorsAsync([courseId], ct)).GetValueOrDefault(courseId);
        var now = DateTime.UtcNow;
        // The answers are keyed to a random response id, not to the student.
        var responseKey = $"survey:{surveyId:N}:{Guid.NewGuid():N}";
        foreach (var (question, _, score) in ratings)
        {
            var isClo = question.Key.StartsWith("clo:", StringComparison.Ordinal);
            var fixedQuestion = isClo ? null : fixedQuestions[Guid.Parse(question.Key[2..])];
            db.OutcomeSurveyResponses.Add(new OutcomeSurveyResponse
            {
                Id = Guid.NewGuid(), Channel = "native", SurveyId = surveyId, ResponseKey = responseKey, CourseId = courseId,
                CourseLearningOutcomeId = isClo ? Guid.Parse(question.Key[4..]) : null,
                ProgramOutcomeId = fixedQuestion?.ProgramOutcomeId,
                Category = fixedQuestion?.Category,
                QuestionKey = question.Key, QuestionText = question.Text.Length > 1000 ? question.Text[..1000] : question.Text,
                Score = score, ScaleMax = survey.ScaleMax, InstructorName = instructor, Semester = survey.Semester, RecordedAt = now,
            });
        }
        foreach (var (question, text) in comments)
            db.OutcomeSurveyComments.Add(new OutcomeSurveyComment { Id = Guid.NewGuid(), SurveyId = surveyId, CourseId = courseId, QuestionId = Guid.Parse(question.Key[2..]), Text = text, RecordedAt = now });
        db.OutcomeSurveyCompletions.Add(new OutcomeSurveyCompletion { Id = Guid.NewGuid(), SurveyId = surveyId, CourseId = courseId, StudentId = studentId, CompletedAt = now });
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // A second tab submitted first; the unique completion index kept exactly one.
            throw new SurveyRuleException("You have already submitted this evaluation.", 409);
        }
        return new EvaluationReceipt(surveyId, courseId, now, ratings.Count, comments.Count);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────────────────

    private async Task<(OutcomeSurvey Survey, Course Course)> EligibleAsync(Guid studentId, Guid surveyId, Guid courseId, CancellationToken ct)
    {
        var survey = await db.OutcomeSurveys.AsNoTracking().SingleOrDefaultAsync(s => s.Id == surveyId, ct) ?? throw new SurveyRuleException("The evaluation was not found.", 404);
        if (survey.Status != "Open") throw new SurveyRuleException(survey.Status == "Closed" ? "This evaluation has closed." : "This evaluation is not open yet.", 409);
        var enrolled = await db.Enrollments.AsNoTracking().AnyAsync(e => e.StudentId == studentId && e.CourseId == courseId && e.EnrollmentStatus == EnrollmentStatus.Active, ct);
        if (!enrolled) throw new SurveyRuleException("You can only evaluate a course you are enrolled in.", 403);
        if (await ScopeAsync(surveyId, ct) is { } scoped && !scoped.Contains(courseId)) throw new SurveyRuleException("This evaluation does not cover that course.", 403);
        var course = await db.Courses.AsNoTracking().SingleAsync(c => c.Id == courseId, ct);
        return (survey, course);
    }

    /// <summary>The questions a student sees for one course: that course's CLOs first (if asked), then the fixed questions by section.</summary>
    private async Task<List<EvaluationSection>> SectionsAsync(OutcomeSurvey survey, Guid courseId, CancellationToken ct)
    {
        var sections = new List<EvaluationSection>();
        var number = 1;
        if (survey.AskCourseClos)
        {
            var clos = await db.CourseLearningOutcomes.AsNoTracking().Where(c => c.CourseId == courseId).OrderBy(c => c.SortOrder).ThenBy(c => c.Code).ToListAsync(ct);
            if (clos.Count > 0)
                sections.Add(new EvaluationSection(survey.CloSection, survey.CloPrompt,
                    clos.Select(c => new EvaluationQuestion($"clo:{c.Id}", number++, c.Description, "Rating", true, c.Code)).ToList()));
        }
        var questions = await db.OutcomeSurveyQuestions.AsNoTracking().Where(q => q.SurveyId == survey.Id).OrderBy(q => q.SortOrder).ToListAsync(ct);
        foreach (var group in questions.GroupBy(q => q.Section))
            sections.Add(new EvaluationSection(group.Key, null, group.Select(q => new EvaluationQuestion($"q:{q.Id}", number++, q.Text, q.Kind, q.Required, null)).ToList()));
        return sections;
    }

    /// <summary>The courses a survey is limited to, or null when it covers every course.</summary>
    private async Task<HashSet<Guid>?> ScopeAsync(Guid surveyId, CancellationToken ct)
    {
        var ids = await db.OutcomeSurveyCourses.AsNoTracking().Where(c => c.SurveyId == surveyId).Select(c => c.CourseId).ToListAsync(ct);
        return ids.Count == 0 ? null : ids.ToHashSet();
    }

    private async Task<Dictionary<Guid, string>> InstructorsAsync(IReadOnlyCollection<Guid> courseIds, CancellationToken ct)
    {
        var rows = await db.CourseAssignments.AsNoTracking().Where(a => courseIds.Contains(a.CourseId)).OrderBy(a => a.CreatedAt)
            .Select(a => new { a.CourseId, Name = a.Faculty.FirstName + " " + a.Faculty.LastName }).ToListAsync(ct);
        return rows.GroupBy(r => r.CourseId).ToDictionary(g => g.Key, g => g.First().Name);
    }

    private static void Validate(SurveyDefinition d)
    {
        if (string.IsNullOrWhiteSpace(d.Title) || d.Title.Trim().Length > 200) throw new SurveyRuleException("Give the survey a title of at most 200 characters.");
        if (string.IsNullOrWhiteSpace(d.Semester) || d.Semester.Trim().Length > 50) throw new SurveyRuleException("Name the semester (at most 50 characters).");
        if (d.ScaleMax is < 2 or > 10) throw new SurveyRuleException("The rating scale must top out between 2 and 10.");
        if (d.Questions.Count > MaxQuestions) throw new SurveyRuleException($"A survey may have at most {MaxQuestions} questions.");
        if (!d.AskCourseClos && d.Questions.Count == 0) throw new SurveyRuleException("Add questions, or ask about each course's CLOs.");
        for (var i = 0; i < d.Questions.Count; i++)
        {
            var q = d.Questions[i];
            if (string.IsNullOrWhiteSpace(q.Text) || q.Text.Trim().Length > 1000) throw new SurveyRuleException($"Question {i + 1} needs text of at most 1000 characters.");
            if (string.IsNullOrWhiteSpace(q.Section) || q.Section.Trim().Length > 200) throw new SurveyRuleException($"Question {i + 1} needs a section.");
            if (!Kinds.Contains(q.Kind)) throw new SurveyRuleException($"Question {i + 1}: the kind must be Rating or Comment.");
            if (q.Category?.Trim().Length > 100) throw new SurveyRuleException($"Question {i + 1}: the heading is longer than 100 characters.");
        }
    }
}
