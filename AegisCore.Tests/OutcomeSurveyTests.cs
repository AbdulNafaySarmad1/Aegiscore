using AegisCore.Application.Obe;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Integrations.Forms;
using AegisCore.Infrastructure.Obe;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Tests;

public sealed class OutcomeSurveyTests
{
    private sealed record World(AegisDbContext Db, SqliteConnection Connection, Course Pf, Course Oop, CourseLearningOutcome PfClo1, CourseLearningOutcome PfClo2, CourseLearningOutcome OopClo1,
        ProgramOutcome Plo1, Guid Aisha, Guid Bilal, Guid Outsider) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync() { await Db.DisposeAsync(); await Connection.DisposeAsync(); }
    }

    private static async Task<World> WorldAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var db = new AegisDbContext(new DbContextOptionsBuilder<AegisDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var department = new Department { Id = Guid.NewGuid(), Name = "Computing" };
        var program = new DegreeProgram { Id = Guid.NewGuid(), Name = "BS CS", Code = "BSCS", DepartmentId = department.Id, IsActive = true };
        var pf = new Course { Title = "Programming Fundamentals", Code = "CS101", DepartmentId = department.Id };
        var oop = new Course { Title = "Object-Oriented Programming", Code = "CS102", DepartmentId = department.Id };
        var faculty = new Faculty { FirstName = "Farhan", LastName = "Malik", DepartmentId = department.Id };
        var pfClo1 = new CourseLearningOutcome { Id = Guid.NewGuid(), CourseId = pf.Id, Code = "CLO1", Description = "Write structured programs", SortOrder = 1 };
        var pfClo2 = new CourseLearningOutcome { Id = Guid.NewGuid(), CourseId = pf.Id, Code = "CLO2", Description = "Trace and debug programs", SortOrder = 2 };
        var oopClo1 = new CourseLearningOutcome { Id = Guid.NewGuid(), CourseId = oop.Id, Code = "CLO1", Description = "Design class hierarchies", SortOrder = 1 };
        var plo1 = new ProgramOutcome { Id = Guid.NewGuid(), ProgramId = program.Id, Code = "PLO1", Description = "Computing knowledge" };
        db.AddRange(department, program, pf, oop, faculty, pfClo1, pfClo2, oopClo1, plo1);
        db.CourseAssignments.Add(new CourseAssignment { CourseId = pf.Id, FacultyId = faculty.Id });
        Guid aisha = Guid.NewGuid(), bilal = Guid.NewGuid(), outsider = Guid.NewGuid();
        var batch = new Batch { Id = Guid.NewGuid(), Name = "2026", StartYear = 2026, EndYear = 2030, DepartmentId = department.Id };
        db.Batches.Add(batch);
        foreach (var (id, first) in new[] { (aisha, "Aisha"), (bilal, "Bilal"), (outsider, "Omar") })
            db.Students.Add(new Student { Id = id, FirstName = first, LastName = "Test", BatchId = batch.Id, EnrollmentStatus = EnrollmentStatus.Active });
        foreach (var student in new[] { aisha, bilal }) db.Enrollments.Add(new Enrollment { StudentId = student, CourseId = pf.Id, EnrollmentStatus = EnrollmentStatus.Active });
        db.Enrollments.Add(new Enrollment { StudentId = aisha, CourseId = oop.Id, EnrollmentStatus = EnrollmentStatus.Active });
        await db.SaveChangesAsync();
        return new World(db, connection, pf, oop, pfClo1, pfClo2, oopClo1, plo1, aisha, bilal, outsider);
    }

    private static SurveyDefinition Definition(World w) => new("Course evaluation", "Answer honestly.", "Fall 2026", 5, true, null, null,
    [
        new("The instructor", "The instructor came to class prepared", "Rating", "Instructor", null),
        new("The course", "The course developed my problem-solving skills", "Rating", null, w.Plo1.Id),
        new("Overall", "Any other comments?", "Comment", null, null),
    ]);

    [Fact]
    public async Task A_student_sees_the_CLOs_of_the_course_being_evaluated_then_the_fixed_questions()
    {
        await using var w = await WorldAsync();
        var service = new OutcomeSurveyService(w.Db);
        var survey = await service.SaveAsync(null, Definition(w), null);
        await service.SetStatusAsync(survey.Id, "Open");

        var mine = await service.ForStudentAsync(w.Aisha);
        Assert.Equal(new[] { "CS101", "CS102" }, mine.Select(m => m.CourseCode).ToArray());
        Assert.Equal(5, mine.Single(m => m.CourseCode == "CS101").Questions); // 2 CLOs + 3 fixed

        var form = await service.FormAsync(w.Aisha, survey.Id, w.Pf.Id);
        Assert.Equal("Course learning outcomes", form.Sections[0].Title);
        Assert.Equal(new[] { "Write structured programs", "Trace and debug programs" }, form.Sections[0].Questions.Select(q => q.Text).ToArray());
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, form.Sections.SelectMany(s => s.Questions).Select(q => q.Number).ToArray());
        Assert.Equal("Farhan Malik", form.Instructor);

        var other = await service.FormAsync(w.Aisha, survey.Id, w.Oop.Id);
        Assert.Equal(new[] { "Design class hierarchies" }, other.Sections[0].Questions.Select(q => q.Text).ToArray());
    }

    [Fact]
    public async Task A_submission_records_each_answer_against_the_right_CLO_PLO_or_heading_without_the_student()
    {
        await using var w = await WorldAsync();
        var service = new OutcomeSurveyService(w.Db);
        var survey = await service.SaveAsync(null, Definition(w), null);
        await service.SetStatusAsync(survey.Id, "Open");
        var form = await service.FormAsync(w.Aisha, survey.Id, w.Pf.Id);
        var keys = form.Sections.SelectMany(s => s.Questions).ToList();

        var receipt = await service.SubmitAsync(w.Aisha, survey.Id, w.Pf.Id, new Dictionary<string, string?>
        {
            [keys[0].Key] = "5", [keys[1].Key] = "3", [keys[2].Key] = "4", [keys[3].Key] = "2", [keys[4].Key] = "Labs were useful.",
        });

        Assert.Equal(4, receipt.Ratings);
        Assert.Equal(1, receipt.Comments);
        var rows = await w.Db.OutcomeSurveyResponses.ToListAsync();
        Assert.Equal(5m, rows.Single(r => r.CourseLearningOutcomeId == w.PfClo1.Id).Score);
        Assert.Equal(3m, rows.Single(r => r.CourseLearningOutcomeId == w.PfClo2.Id).Score);
        Assert.Equal(4m, rows.Single(r => r.Category == "Instructor").Score);
        Assert.Equal(2m, rows.Single(r => r.ProgramOutcomeId == w.Plo1.Id).Score);
        Assert.All(rows, r => { Assert.Equal("native", r.Channel); Assert.Equal(w.Pf.Id, r.CourseId); Assert.Equal("Fall 2026", r.Semester); Assert.Equal("Farhan Malik", r.InstructorName); });
        Assert.Single(rows.Select(r => r.ResponseKey).Distinct());
        Assert.DoesNotContain(w.Aisha.ToString("N"), rows[0].ResponseKey);
        Assert.Equal("Labs were useful.", (await w.Db.OutcomeSurveyComments.SingleAsync()).Text);

        var report = await new IndirectAssessmentReportService(w.Db).GetReportAsync(null, null);
        Assert.Equal(1, report.Respondents);
        Assert.Equal("native", report.Channels.Single().Channel);
        Assert.Equal(new[] { "CLO1", "CLO2" }, report.Courses.Single().Clos.Select(c => c.Code).ToArray());
    }

    [Fact]
    public async Task A_student_answers_once_per_course_and_only_for_courses_they_take()
    {
        await using var w = await WorldAsync();
        var service = new OutcomeSurveyService(w.Db);
        var survey = await service.SaveAsync(null, Definition(w) with { Questions = [] }, null);
        await service.SetStatusAsync(survey.Id, "Open");
        var keys = (await service.FormAsync(w.Aisha, survey.Id, w.Pf.Id)).Sections.SelectMany(s => s.Questions).Select(q => q.Key).ToList();
        var answers = keys.ToDictionary(k => k, _ => (string?)"4");

        await service.SubmitAsync(w.Aisha, survey.Id, w.Pf.Id, answers);
        var again = await Assert.ThrowsAsync<SurveyRuleException>(() => service.SubmitAsync(w.Aisha, survey.Id, w.Pf.Id, answers));
        Assert.Equal(409, again.Status);
        var outsider = await Assert.ThrowsAsync<SurveyRuleException>(() => service.FormAsync(w.Outsider, survey.Id, w.Pf.Id));
        Assert.Equal(403, outsider.Status);
        Assert.True((await service.ForStudentAsync(w.Aisha)).Single(e => e.CourseId == w.Pf.Id).Completed);
        Assert.False((await service.ForStudentAsync(w.Bilal)).Single().Completed);
        Assert.Equal(1, (await service.GetAsync(survey.Id)).Courses.Single(c => c.Code == "CS101").Completed);
        Assert.Equal(2, (await service.GetAsync(survey.Id)).Courses.Single(c => c.Code == "CS101").Enrolled);
    }

    [Fact]
    public async Task Unanswered_required_questions_and_out_of_scale_ratings_are_refused_and_nothing_is_recorded()
    {
        await using var w = await WorldAsync();
        var service = new OutcomeSurveyService(w.Db);
        var survey = await service.SaveAsync(null, Definition(w), null);
        await service.SetStatusAsync(survey.Id, "Open");
        var keys = (await service.FormAsync(w.Bilal, survey.Id, w.Pf.Id)).Sections.SelectMany(s => s.Questions).Select(q => q.Key).ToList();

        var missing = await Assert.ThrowsAsync<SurveyRuleException>(() => service.SubmitAsync(w.Bilal, survey.Id, w.Pf.Id, new Dictionary<string, string?> { [keys[0]] = "4" }));
        Assert.Contains("3 questions are not answered yet: 2, 3, 4", missing.Message);
        var outOfScale = keys.Take(4).ToDictionary(k => k, _ => (string?)"6");
        await Assert.ThrowsAsync<SurveyRuleException>(() => service.SubmitAsync(w.Bilal, survey.Id, w.Pf.Id, outOfScale));
        Assert.Empty(await w.Db.OutcomeSurveyResponses.ToListAsync());
        Assert.Empty(await w.Db.OutcomeSurveyCompletions.ToListAsync());
    }

    [Fact]
    public async Task A_draft_cannot_be_answered_and_an_open_survey_cannot_be_edited()
    {
        await using var w = await WorldAsync();
        var service = new OutcomeSurveyService(w.Db);
        var survey = await service.SaveAsync(null, Definition(w), null);
        Assert.Empty(await service.ForStudentAsync(w.Aisha));
        Assert.Equal(409, (await Assert.ThrowsAsync<SurveyRuleException>(() => service.FormAsync(w.Aisha, survey.Id, w.Pf.Id))).Status);
        await service.SetStatusAsync(survey.Id, "Open");
        Assert.Equal(409, (await Assert.ThrowsAsync<SurveyRuleException>(() => service.SaveAsync(survey.Id, Definition(w), null))).Status);
        await service.SetStatusAsync(survey.Id, "Closed");
        Assert.Empty(await service.ForStudentAsync(w.Aisha));
    }
    [Fact]
    public async Task A_survey_limited_to_some_courses_is_offered_only_for_those()
    {
        await using var w = await WorldAsync();
        var service = new OutcomeSurveyService(w.Db);
        var survey = await service.SaveAsync(null, Definition(w) with { CourseIds = [w.Pf.Id] }, null);
        await service.SetStatusAsync(survey.Id, "Open");

        Assert.Equal(new[] { "CS101" }, (await service.ForStudentAsync(w.Aisha)).Select(e => e.CourseCode).ToArray());
        Assert.Equal(403, (await Assert.ThrowsAsync<SurveyRuleException>(() => service.FormAsync(w.Aisha, survey.Id, w.Oop.Id))).Status);
        var detail = await service.GetAsync(survey.Id);
        Assert.False(detail.AllCourses);
        Assert.Equal(new[] { "CS101" }, detail.Courses.Select(c => c.Code).ToArray());
        Assert.Equal(2, (await service.ListAsync()).Single().Eligible);
    }
    [Fact]
    public async Task Only_a_draft_or_closed_survey_can_be_deleted_and_its_answers_go_with_it()
    {
        await using var w = await WorldAsync();
        var service = new OutcomeSurveyService(w.Db);
        var survey = await service.SaveAsync(null, Definition(w) with { Questions = [] }, null);
        await service.SetStatusAsync(survey.Id, "Open");
        var keys = (await service.FormAsync(w.Aisha, survey.Id, w.Pf.Id)).Sections.SelectMany(s => s.Questions).Select(q => q.Key).ToList();
        await service.SubmitAsync(w.Aisha, survey.Id, w.Pf.Id, keys.ToDictionary(k => k, _ => (string?)"4"));

        Assert.Equal(409, (await Assert.ThrowsAsync<SurveyRuleException>(() => service.DeleteAsync(survey.Id))).Status);
        await service.SetStatusAsync(survey.Id, "Closed");
        Assert.Equal(2, await service.DeleteAsync(survey.Id));
        Assert.Empty(await w.Db.OutcomeSurveys.ToListAsync());
        Assert.Empty(await w.Db.OutcomeSurveyResponses.ToListAsync());
        Assert.Empty(await w.Db.OutcomeSurveyCompletions.ToListAsync());
    }
}
