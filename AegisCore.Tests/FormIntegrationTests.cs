using System.Text.Json;
using AegisCore.Application.Integrations;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Integrations.Forms;
using AegisCore.API.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;

namespace AegisCore.Tests;

public sealed class FormIntegrationTests
{
    private static async Task<AegisDbContext> NewDbAsync(SqliteConnection connection)
    {
        var db = new AegisDbContext(new DbContextOptionsBuilder<AegisDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private sealed record Seeded(Course Pf, Course Oop, CourseLearningOutcome PfClo1, CourseLearningOutcome PfClo2, CourseLearningOutcome OopClo1, ProgramOutcome Plo1, FormIntegrationSource Source);

    private static async Task<Seeded> SeedAsync(AegisDbContext db, string provider = "jotform")
    {
        var department = new Department { Id = Guid.NewGuid(), Name = "Computing", TenantId = Guid.Empty };
        var program = new DegreeProgram { Id = Guid.NewGuid(), Name = "BS CS", Code = "BSCS", DepartmentId = department.Id, IsActive = true, TenantId = Guid.Empty };
        var pf = new Course { Title = "Programming Fundamentals", Code = "CS101", CreditHours = 3, DepartmentId = department.Id };
        var oop = new Course { Title = "Object-Oriented Programming", Code = "CS102", CreditHours = 3, DepartmentId = department.Id };
        var faculty = new Faculty { FirstName = "Farhan", LastName = "Malik", DepartmentId = department.Id };
        var pfClo1 = new CourseLearningOutcome { Id = Guid.NewGuid(), CourseId = pf.Id, Code = "CLO1", Description = "Write programs", BloomsLevel = BloomsLevel.Apply };
        var pfClo2 = new CourseLearningOutcome { Id = Guid.NewGuid(), CourseId = pf.Id, Code = "CLO2", Description = "Trace programs", BloomsLevel = BloomsLevel.Analyze };
        var oopClo1 = new CourseLearningOutcome { Id = Guid.NewGuid(), CourseId = oop.Id, Code = "CLO1", Description = "Design classes", BloomsLevel = BloomsLevel.Create };
        var plo1 = new ProgramOutcome { Id = Guid.NewGuid(), ProgramId = program.Id, Code = "PLO1", Description = "Computing knowledge" };
        db.AddRange(department, program, pf, oop, faculty, pfClo1, pfClo2, oopClo1, plo1);
        db.CourseAssignments.Add(new CourseAssignment { CourseId = pf.Id, FacultyId = faculty.Id });
        db.CLOPOMappings.Add(new CLOPOMapping { Id = Guid.NewGuid(), CourseLearningOutcomeId = pfClo1.Id, ProgramOutcomeId = plo1.Id, Strength = MappingStrength.High });
        db.CLOPOMappings.Add(new CLOPOMapping { Id = Guid.NewGuid(), CourseLearningOutcomeId = oopClo1.Id, ProgramOutcomeId = plo1.Id, Strength = MappingStrength.Low });
        var source = new FormIntegrationSource { Id = Guid.NewGuid(), Name = "Course evaluation", Provider = provider, TokenHash = Guid.NewGuid().ToString("N"), ScaleMax = 5 };
        db.FormIntegrationSources.Add(source);
        await db.SaveChangesAsync();
        return new Seeded(pf, oop, pfClo1, pfClo2, oopClo1, plo1, source);
    }

    private static FormSubmissionEnvelope Envelope(string id, params (string Field, string Value)[] answers)
        => new("form-1", id, answers.Select(a => new FormAnswer(a.Field, null, a.Value)).ToList());

    private static async Task MapAsync(AegisDbContext db, Guid sourceId, params (string Field, string Target, string? Clo, Guid? Po, string? Category)[] rows)
    {
        foreach (var row in rows)
        {
            var mapping = await db.FormFieldMappings.SingleOrDefaultAsync(m => m.SourceId == sourceId && m.ExternalFieldId == row.Field);
            if (mapping is null)
            {
                mapping = new FormFieldMapping { Id = Guid.NewGuid(), SourceId = sourceId, ExternalFieldId = row.Field, QuestionText = row.Field };
                db.FormFieldMappings.Add(mapping);
            }
            mapping.Target = row.Target;
            mapping.CloCode = row.Clo;
            mapping.ProgramOutcomeId = row.Po;
            mapping.Category = row.Category;
        }
        await db.SaveChangesAsync();
    }

    [Fact]
    public void JotForm_adapter_reads_rawRequest_keeps_only_questions_and_splits_matrix_rows()
    {
        var raw = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["slug"] = "submit/123", ["event_id"] = "1_2", ["timeToSubmit"] = "40",
            ["q3_courseCode"] = "CS101",
            ["q5_cloRatings"] = new Dictionary<string, string> { ["CLO1"] = "Agree", ["CLO2"] = "Strongly Agree" },
            ["q7_instructorKnowledge"] = "5",
        });
        var envelope = new JotFormPayloadAdapter().Parse("multipart/form-data", "", new Dictionary<string, string>
        {
            ["formID"] = "241234567890", ["submissionID"] = "5987654321", ["rawRequest"] = raw,
        });

        Assert.Equal("241234567890", envelope.ExternalFormId);
        Assert.Equal("5987654321", envelope.ExternalSubmissionId);
        Assert.Equal(new[] { "q3_courseCode", "q5_cloRatings[CLO1]", "q5_cloRatings[CLO2]", "q7_instructorKnowledge" }, envelope.Answers.Select(a => a.FieldId).ToArray());
        Assert.Equal("Instructor knowledge", envelope.Answers.Single(a => a.FieldId == "q7_instructorKnowledge").Label);
        Assert.Equal("Strongly Agree", envelope.Answers.Single(a => a.FieldId == "q5_cloRatings[CLO2]").Value);
    }

    [Fact]
    public void JotForm_adapter_refuses_a_body_without_rawRequest()
        => Assert.Throws<FormatException>(() => new JotFormPayloadAdapter().Parse("application/json", "{}", new Dictionary<string, string>()));

    [Fact]
    public void Generic_adapter_accepts_both_answer_shapes_and_requires_a_submission_id()
    {
        var adapter = new GenericFormPayloadAdapter();
        var list = adapter.Parse("application/json", """{"submissionId":"a1","formId":"f","answers":[{"fieldId":"q1","label":"Clear goals","value":4}]}""", new Dictionary<string, string>());
        Assert.Equal(new FormAnswer("q1", "Clear goals", "4"), list.Answers.Single());
        var map = adapter.Parse("application/json", """{"submissionId":7,"answers":{"q1":"Agree","q2":true}}""", new Dictionary<string, string>());
        Assert.Equal("7", map.ExternalSubmissionId);
        Assert.Equal(new[] { "q1", "q2" }, map.Answers.Select(a => a.FieldId).ToArray());
        Assert.Throws<FormatException>(() => adapter.Parse("application/json", """{"answers":{}}""", new Dictionary<string, string>()));
    }

    [Theory]
    [InlineData("4", 4)]
    [InlineData("4 - Agree", 4)]
    [InlineData("Strongly agree", 5)]
    [InlineData(" disagree ", 2)]
    public void Ratings_are_read_from_numbers_numbered_options_and_five_point_labels(string value, int expected)
        => Assert.Equal(expected, FormPayloadText.Score(value, 5));

    [Fact]
    public void Labels_are_not_guessed_on_a_scale_other_than_five_points()
        => Assert.Null(FormPayloadText.Score("Agree", 7));

    [Fact]
    public async Task A_submission_with_unmapped_fields_is_held_then_lands_in_the_right_clo_once_mapped()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = await NewDbAsync(connection);
        var seeded = await SeedAsync(db);
        var service = new FormIngestionService(db);

        var first = await service.IngestAsync(seeded.Source.Id, Envelope("s1", ("q3_course", "CS101 - Programming Fundamentals"), ("q5_clo1", "Agree"), ("q6_clo2", "5")));
        Assert.Equal("Held", first.Status);
        Assert.Equal(3, first.UnmappedCount);
        Assert.Equal(3, await db.FormFieldMappings.CountAsync(m => m.Target == FormFieldTargets.Unmapped));
        Assert.Empty(await db.OutcomeSurveyResponses.ToListAsync());

        await MapAsync(db, seeded.Source.Id,
            ("q3_course", FormFieldTargets.Course, null, null, null),
            ("q5_clo1", FormFieldTargets.Clo, "CLO1", null, null),
            ("q6_clo2", FormFieldTargets.Clo, "CLO2", null, null));
        var reprocessed = await service.ReprocessAsync(seeded.Source.Id);
        Assert.Equal(1, reprocessed.Recorded);

        var rows = await db.OutcomeSurveyResponses.OrderBy(r => r.QuestionKey).ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.All(rows, r => Assert.Equal(seeded.Pf.Id, r.CourseId));
        Assert.Equal(seeded.PfClo1.Id, rows[0].CourseLearningOutcomeId);
        Assert.Equal(4m, rows[0].Score);
        Assert.Equal(seeded.PfClo2.Id, rows[1].CourseLearningOutcomeId);
        Assert.Equal(5m, rows[1].Score);
        Assert.All(rows, r => Assert.Equal("Farhan Malik", r.InstructorName));
        Assert.All(rows, r => Assert.Equal("external:jotform", r.Channel));
    }

    [Fact]
    public async Task The_same_clo_code_resolves_to_the_course_each_response_is_about()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = await NewDbAsync(connection);
        var seeded = await SeedAsync(db);
        await MapAsync(db, seeded.Source.Id, ("course", FormFieldTargets.Course, null, null, null), ("clo1", FormFieldTargets.Clo, "CLO1", null, null));
        var service = new FormIngestionService(db);

        await service.IngestAsync(seeded.Source.Id, Envelope("a", ("course", "cs101"), ("clo1", "5")));
        await service.IngestAsync(seeded.Source.Id, Envelope("b", ("course", "Object-Oriented Programming"), ("clo1", "2")));

        var rows = await db.OutcomeSurveyResponses.ToListAsync();
        Assert.Equal(seeded.PfClo1.Id, rows.Single(r => r.CourseId == seeded.Pf.Id).CourseLearningOutcomeId);
        Assert.Equal(seeded.OopClo1.Id, rows.Single(r => r.CourseId == seeded.Oop.Id).CourseLearningOutcomeId);
    }

    [Fact]
    public async Task A_redelivered_submission_is_acknowledged_but_not_counted_twice()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = await NewDbAsync(connection);
        var seeded = await SeedAsync(db);
        await MapAsync(db, seeded.Source.Id, ("course", FormFieldTargets.Course, null, null, null), ("clo1", FormFieldTargets.Clo, "CLO1", null, null));
        var service = new FormIngestionService(db);

        var first = await service.IngestAsync(seeded.Source.Id, Envelope("same", ("course", "CS101"), ("clo1", "4")));
        var again = await service.IngestAsync(seeded.Source.Id, Envelope("same", ("course", "CS101"), ("clo1", "4")));

        Assert.False(first.Duplicate);
        Assert.True(again.Duplicate);
        Assert.Equal(first.SubmissionId, again.SubmissionId);
        Assert.Equal(1, await db.OutcomeSurveyResponses.CountAsync());
    }

    [Fact]
    public async Task Answers_that_cannot_be_placed_are_reported_not_silently_dropped()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = await NewDbAsync(connection);
        var seeded = await SeedAsync(db);
        await MapAsync(db, seeded.Source.Id,
            ("course", FormFieldTargets.Course, null, null, null),
            ("clo9", FormFieldTargets.Clo, "CLO9", null, null),
            ("rating", FormFieldTargets.Satisfaction, null, null, "Instructor"),
            ("comment", FormFieldTargets.Ignore, null, null, null));

        var result = await new FormIngestionService(db).IngestAsync(seeded.Source.Id,
            Envelope("x", ("course", "CS101"), ("clo9", "4"), ("rating", "9"), ("comment", "Great course")));

        Assert.Equal("Partial", result.Status);
        Assert.Equal(0, result.RecordedCount);
        Assert.Equal(0, result.UnmappedCount);
        Assert.Contains(result.Problems, p => p.Contains("CLO9 is not defined for CS101"));
        Assert.Contains(result.Problems, p => p.Contains("not a rating on the 1–5 scale"));
    }

    [Fact]
    public async Task A_77_question_form_is_discovered_bulk_mapped_and_fully_recorded()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = await NewDbAsync(connection);
        var seeded = await SeedAsync(db);
        var service = new FormIngestionService(db);
        var answers = new List<(string, string)> { ("q1_course", "CS101") };
        answers.AddRange(Enumerable.Range(2, 76).Select(i => ($"q{i}_item", ((i % 5) + 1).ToString())));

        var held = await service.IngestAsync(seeded.Source.Id, Envelope("first", answers.ToArray()));
        Assert.Equal(77, held.FieldCount);
        Assert.Equal(77, await db.FormFieldMappings.CountAsync(m => m.SourceId == seeded.Source.Id));

        var rows = new List<(string, string, string?, Guid?, string?)> { ("q1_course", FormFieldTargets.Course, null, null, null) };
        rows.AddRange(Enumerable.Range(2, 76).Select(i => i <= 11
            ? ($"q{i}_item", FormFieldTargets.Clo, i % 2 == 0 ? "CLO1" : "CLO2", (Guid?)null, (string?)null)
            : ($"q{i}_item", FormFieldTargets.Satisfaction, null, null, i <= 40 ? "Instructor" : "Course")));
        await MapAsync(db, seeded.Source.Id, rows.ToArray());
        await service.ReprocessAsync(seeded.Source.Id);

        var submission = await db.FormIntegrationSubmissions.SingleAsync();
        Assert.Equal("Recorded", submission.Status);
        Assert.Equal(76, submission.RecordedCount);
        Assert.Equal(5, await db.OutcomeSurveyResponses.CountAsync(r => r.CourseLearningOutcomeId == seeded.PfClo1.Id));
        Assert.Equal(5, await db.OutcomeSurveyResponses.CountAsync(r => r.CourseLearningOutcomeId == seeded.PfClo2.Id));
    }

    [Fact]
    public async Task The_report_counts_native_and_external_answers_alike_and_rolls_clos_up_to_plos()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = await NewDbAsync(connection);
        var seeded = await SeedAsync(db);
        await MapAsync(db, seeded.Source.Id,
            ("course", FormFieldTargets.Course, null, null, null),
            ("clo1", FormFieldTargets.Clo, "CLO1", null, null),
            ("teach", FormFieldTargets.Satisfaction, null, null, "Instructor"));
        var native = new FormIntegrationSource { Id = Guid.NewGuid(), Name = "Native", Provider = FormIngestionService.NativeProvider, TokenHash = "n", ExternalFormId = "native-form" };
        db.FormIntegrationSources.Add(native);
        await db.SaveChangesAsync();
        await MapAsync(db, native.Id, ("course", FormFieldTargets.Course, null, null, null), ("clo1", FormFieldTargets.Clo, "CLO1", null, null));
        var service = new FormIngestionService(db);

        await service.IngestAsync(seeded.Source.Id, Envelope("e1", ("course", "CS101"), ("clo1", "5"), ("teach", "4")));
        await service.IngestAsync(seeded.Source.Id, Envelope("e2", ("course", "CS101"), ("clo1", "3"), ("teach", "2")));
        await service.IngestAsync(native.Id, Envelope("n1", ("course", "CS101"), ("clo1", "4")));
        await service.IngestAsync(seeded.Source.Id, Envelope("e3", ("course", "CS102"), ("clo1", "1")));

        var report = await new IndirectAssessmentReportService(db).GetReportAsync(null, null);

        Assert.Equal(4, report.Respondents);
        Assert.Equal(new[] { "external:jotform", "native" }, report.Channels.Select(c => c.Channel).ToArray());
        var pf = report.Courses.Single(c => c.Code == "CS101");
        Assert.Equal(3, pf.Respondents);
        var clo1 = pf.Clos.Single();
        Assert.Equal(3, clo1.Rating.Answers);
        Assert.Equal(4m, clo1.Rating.Mean);                 // (5 + 3 + 4) / 3
        Assert.Equal(66.7m, clo1.Rating.FavourablePercent); // 5 and 4 are in the top two points
        Assert.Equal(3m, pf.Satisfaction.Single(s => s.Category == "Instructor").Rating.Mean);
        Assert.Equal(new[] { "Farhan Malik" }, pf.Instructors.ToArray());

        // PLO1 = CS101 CLO1 (High, 1.0) answers 100/60/80 % and CS102 CLO1 (Low, 0.33) answer 20 %.
        var plo = report.ProgramOutcomes.Single();
        Assert.Equal(4, plo.Answers);
        Assert.Equal(Math.Round((100m + 60m + 80m + 20m * 0.33m) / 3.33m, 1, MidpointRounding.AwayFromZero), plo.PercentOfScale);
        Assert.Equal(new[] { "CS101 CLO1 (High)", "CS102 CLO1 (Low)" }, plo.ContributingClos.ToArray());
    }
    [Fact]
    public async Task The_anonymous_webhook_authenticates_by_token_for_a_real_tenant_and_records_a_JotForm_post()
    {
        // As in production: the request carries no tenant context; the tenant row is itself tenant-scoped.
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var tenantId = Guid.NewGuid();
        var http = new DefaultHttpContext();
        var accessor = new HttpContextAccessor { HttpContext = http };
        await using var db = new AegisDbContext(new DbContextOptionsBuilder<AegisDbContext>().UseSqlite(connection).Options, accessor);
        await db.Database.EnsureCreatedAsync();
        http.Items["TenantId"] = tenantId;
        db.Tenants.Add(new Tenant { Id = tenantId, TenantId = tenantId, Name = "Fictional", IsActive = true });
        var department = new Department { Id = Guid.NewGuid(), Name = "Computing" };
        var course = new Course { Title = "Programming Fundamentals", Code = "CS101", DepartmentId = department.Id };
        db.AddRange(department, course, new CourseLearningOutcome { Id = Guid.NewGuid(), CourseId = course.Id, Code = "CLO1", Description = "Write programs" });
        const string token = "acf_test-token";
        var source = new FormIntegrationSource { Id = Guid.NewGuid(), Name = "Evaluation", Provider = "jotform", ExternalFormId = "260000000077", TokenHash = FormIntegrationsController.HashToken(token) };
        db.FormIntegrationSources.Add(source);
        db.FormFieldMappings.AddRange(
            new FormFieldMapping { Id = Guid.NewGuid(), SourceId = source.Id, ExternalFieldId = "q1_courseName", Target = FormFieldTargets.Course },
            new FormFieldMapping { Id = Guid.NewGuid(), SourceId = source.Id, ExternalFieldId = "q3_clo1Achieved", Target = FormFieldTargets.Clo, CloCode = "CLO1" });
        await db.SaveChangesAsync();
        http.Items.Remove("TenantId");

        async Task<IActionResult> PostAsync(string? presented, string formId)
        {
            var request = new DefaultHttpContext();
            request.Request.Method = "POST";
            request.Request.ContentType = "multipart/form-data; boundary=x";
            if (presented is not null) request.Request.QueryString = new QueryString($"?token={presented}");
            request.Request.Form = new FormCollection(new Dictionary<string, StringValues>
            {
                ["formID"] = formId, ["submissionID"] = "5900000000000000001",
                ["rawRequest"] = """{"slug":"submit/260000000077","q1_courseName":"CS101 - Programming Fundamentals","q3_clo1Achieved":"Strongly Agree"}""",
            });
            accessor.HttpContext = request;
            var controller = new FormIntegrationsController(db, new FormIngestionService(db),
                [new JotFormPayloadAdapter(), new GenericFormPayloadAdapter()], new ConfigurationBuilder().Build(), NullLogger<FormIntegrationsController>.Instance)
            { ControllerContext = new ControllerContext { HttpContext = request } };
            return await controller.Receive(tenantId, CancellationToken.None);
        }

        Assert.IsType<UnauthorizedObjectResult>(await PostAsync(null, "260000000077"));
        Assert.IsType<UnauthorizedObjectResult>(await PostAsync("acf_wrong", "260000000077"));
        Assert.IsType<BadRequestObjectResult>(await PostAsync(token, "999"));
        Assert.IsType<OkObjectResult>(await PostAsync(token, "260000000077"));

        http.Items["TenantId"] = tenantId;
        accessor.HttpContext = http;
        var answer = await db.OutcomeSurveyResponses.SingleAsync();
        Assert.Equal(course.Id, answer.CourseId);
        Assert.Equal(5m, answer.Score);
        Assert.Equal(tenantId, answer.TenantId);
    }
}
