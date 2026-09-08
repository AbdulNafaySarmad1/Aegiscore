using AegisCore.Application.Admissions;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Admissions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AegisCore.Tests;

public sealed class AdmissionTestEngineTests
{
    private static async Task<AegisDbContext> NewDbAsync(SqliteConnection connection)
    {
        var db = new AegisDbContext(new DbContextOptionsBuilder<AegisDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private static Guid Subject(AegisDbContext db, string name, string code)
    {
        var id = Guid.NewGuid();
        db.AdmissionSubjects.Add(new AdmissionSubject { Id = id, TenantId = Guid.Empty, Name = name, Code = code, IsActive = true });
        return id;
    }

    private static void AddApprovedQuestions(AegisDbContext db, Guid subjectId, int count, AdmissionQuestionStatus status = AdmissionQuestionStatus.Approved)
    {
        for (var i = 0; i < count; i++)
        {
            db.AdmissionQuestions.Add(new AdmissionQuestion
            {
                Id = Guid.NewGuid(), TenantId = Guid.Empty, SubjectId = subjectId,
                QuestionText = $"Q{i}", OptionA = "a", OptionB = "b", OptionC = "c", OptionD = "d",
                CorrectOption = "B", DifficultyLevel = "medium", Status = status
            });
        }
    }

    [Fact]
    public async Task Program_Test_Profiles_Are_Isolated_Per_Program()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = await NewDbAsync(connection);

        var biology = Subject(db, "Biology", "BIO");
        var maths = Subject(db, "Mathematics", "MATH");
        var english = Subject(db, "English", "ENG");
        await db.SaveChangesAsync();

        var config = new AdmissionTestConfigService(db);
        var mbbsIntake = Guid.NewGuid(); var mbbsProgram = Guid.NewGuid();
        var bcsIntake = Guid.NewGuid(); var bcsProgram = Guid.NewGuid();

        await config.UpsertProfileAsync(new TestProfileInput(mbbsIntake, mbbsProgram, 40, 90,
            [new TestSubjectSlot(biology, 40, 1), new TestSubjectSlot(english, 10, 1)]));
        await config.UpsertProfileAsync(new TestProfileInput(bcsIntake, bcsProgram, 35, 60,
            [new TestSubjectSlot(maths, 40, 1), new TestSubjectSlot(english, 20, 1)]));

        var mbbs = await config.GetProfileAsync(mbbsIntake, mbbsProgram, default);
        var bcs = await config.GetProfileAsync(bcsIntake, bcsProgram, default);

        Assert.NotNull(mbbs);
        Assert.NotNull(bcs);
        Assert.Contains(mbbs!.Subjects, s => s.SubjectId == biology);
        Assert.DoesNotContain(mbbs.Subjects, s => s.SubjectId == maths);
        Assert.Contains(bcs!.Subjects, s => s.SubjectId == maths);
        Assert.DoesNotContain(bcs.Subjects, s => s.SubjectId == biology);
    }

    [Fact]
    public async Task Scheduling_Uses_Only_Approved_Questions_And_Fails_When_Pool_Too_Small()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = await NewDbAsync(connection);

        var subject = Subject(db, "Biology", "BIO");
        AddApprovedQuestions(db, subject, 3, AdmissionQuestionStatus.Approved);
        AddApprovedQuestions(db, subject, 10, AdmissionQuestionStatus.Draft);

        var intakeId = Guid.NewGuid(); var programId = Guid.NewGuid();
        var applicant = new ApplicantProfile
        {
            Id = Guid.NewGuid(), TenantId = Guid.Empty, ApplicantId = "APP-2025F-MBBS-AAAABBBB",
            Email = "m@example.com", IntakeId = intakeId, ProgramId = programId, Status = ApplicantStatus.UnderReview
        };
        db.ApplicantProfiles.Add(applicant);
        await db.SaveChangesAsync();

        await new AdmissionTestConfigService(db).UpsertProfileAsync(
            new TestProfileInput(intakeId, programId, 2, 30, [new TestSubjectSlot(subject, 5, 1)]));

        var scheduler = new TestSchedulingService(db);
        // Only 3 approved, profile wants 5 -> fail.
        await Assert.ThrowsAsync<InvalidOperationException>(() => scheduler.ScheduleAsync(applicant.Id, null));

        AddApprovedQuestions(db, subject, 5, AdmissionQuestionStatus.Approved);
        await db.SaveChangesAsync();

        var result = await scheduler.ScheduleAsync(applicant.Id, null);
        Assert.Equal(5, result.TotalQuestions);
        Assert.Equal(64, result.PaperHash.Length); // sha-256 hex
    }

    [Fact]
    public async Task Terminal_Scores_Paper_And_Auto_Submits_On_Expiry()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = await NewDbAsync(connection);

        var subject = Subject(db, "English", "ENG");
        AddApprovedQuestions(db, subject, 4, AdmissionQuestionStatus.Approved); // all correct answer = "B"

        var intakeId = Guid.NewGuid(); var programId = Guid.NewGuid();
        var applicant = new ApplicantProfile
        {
            Id = Guid.NewGuid(), TenantId = Guid.Empty, ApplicantId = "APP-2025F-BBA-CCCCDDDD",
            Email = "b@example.com", IntakeId = intakeId, ProgramId = programId, Status = ApplicantStatus.UnderReview
        };
        db.ApplicantProfiles.Add(applicant);
        await db.SaveChangesAsync();

        await new AdmissionTestConfigService(db).UpsertProfileAsync(
            new TestProfileInput(intakeId, programId, 3, 60, [new TestSubjectSlot(subject, 4, 1)]));
        await new TestSchedulingService(db).ScheduleAsync(applicant.Id, null);

        var tenantResolver = new FixedTenant(Guid.Empty);
        var terminal = new TestTerminalService(db, tenantResolver, NullLogger<TestTerminalService>.Instance);

        var start = await terminal.StartAsync("APP-2025F-BBA-CCCCDDDD");
        Assert.Equal(4, start.TotalQuestions);
        Assert.All(start.Questions, q => Assert.DoesNotContain(q.Options, o => o.Key == "correct"));

        // Answer the first 3 correctly ("B"), leave the 4th.
        foreach (var q in start.Questions.Take(3))
        {
            await terminal.RecordAnswerAsync(start.ApplicantTestId, q.QuestionId, "B", default);
        }

        var submit = await terminal.SubmitAsync(start.ApplicantTestId, autoSubmitted: true, default);
        Assert.True(submit.Passed); // 3 correct >= pass mark 3

        var test = await db.ApplicantTests.IgnoreQueryFilters().SingleAsync(t => t.Id == start.ApplicantTestId);
        Assert.Equal(3, test.Score);
        Assert.Equal(ApplicantTestStatus.Submitted, test.Status);
        Assert.NotNull(test.CompletedAt);
    }

    [Fact]
    public async Task Terminal_Rejects_Start_After_Window_Expires()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = await NewDbAsync(connection);

        var subject = Subject(db, "Physics", "PHY");
        AddApprovedQuestions(db, subject, 2, AdmissionQuestionStatus.Approved);
        var intakeId = Guid.NewGuid(); var programId = Guid.NewGuid();
        var applicant = new ApplicantProfile
        {
            Id = Guid.NewGuid(), TenantId = Guid.Empty, ApplicantId = "APP-2025F-BCS-EEEEFFFF",
            Email = "p@example.com", IntakeId = intakeId, ProgramId = programId, Status = ApplicantStatus.UnderReview
        };
        db.ApplicantProfiles.Add(applicant);
        await db.SaveChangesAsync();

        await new AdmissionTestConfigService(db).UpsertProfileAsync(
            new TestProfileInput(intakeId, programId, 1, 30, [new TestSubjectSlot(subject, 2, 1)]));
        await new TestSchedulingService(db).ScheduleAsync(applicant.Id, DateTimeOffset.UtcNow.AddMinutes(-1));

        var terminal = new TestTerminalService(db, new FixedTenant(Guid.Empty), NullLogger<TestTerminalService>.Instance);
        await Assert.ThrowsAsync<InvalidOperationException>(() => terminal.StartAsync("APP-2025F-BCS-EEEEFFFF"));

        var test = await db.ApplicantTests.IgnoreQueryFilters().SingleAsync(t => t.ApplicantId == applicant.Id);
        Assert.Equal(ApplicantTestStatus.Expired, test.Status);
    }

    [Fact]
    public void Question_Generator_Parses_Fenced_And_Varied_Json()
    {
        const string raw = """
            Here are the questions:
            ```json
            [
              {"question":"What is 2+2?","options":["3","4","5","6"],"correct":"B","difficulty":"Easy"},
              {"question":"Capital of Pakistan?","A":"Karachi","B":"Lahore","C":"Islamabad","D":"Multan","answer":"C"},
              {"question":"Missing options","correct":"A"}
            ]
            ```
            """;

        var parsed = AdmissionQuestionGenerator.Parse(raw);

        Assert.Equal(2, parsed.Count);
        Assert.Equal("B", parsed[0].CorrectOption);
        Assert.Equal("4", parsed[0].OptionB);
        Assert.Equal("easy", parsed[0].DifficultyLevel);
        Assert.Equal("Islamabad", parsed[1].OptionC);
        Assert.Equal("C", parsed[1].CorrectOption);
    }

    [Fact]
    public async Task Question_Bank_Ingest_Saves_Draft_Questions_From_Configured_Prompt()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var db = await NewDbAsync(connection);

        var subject = Subject(db, "Chemistry", "CHEM");
        await db.SaveChangesAsync();

        var bank = new QuestionBankService(db, new InMemoryStorage(), new StubOcr("Some syllabus text about atoms and bonds."),
            new StubGenerator(6), NullLogger<QuestionBankService>.Instance);

        var result = await bank.IngestSyllabusAsync(subject, new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46 }), "chem.pdf", 6, Guid.NewGuid());

        Assert.Equal(6, result.QuestionsGenerated);
        var drafts = await bank.ListAsync(subject, AdmissionQuestionStatus.Draft, default);
        Assert.Equal(6, drafts.Count);
        Assert.All(drafts, q => Assert.Equal(AdmissionQuestionStatus.Draft, q.Status));

        // Prompt was seeded from the default and is retrievable/editable.
        var prompt = await bank.GetActivePromptAsync(default);
        Assert.Contains("multiple choice questions", prompt);
    }

    private sealed class FixedTenant(Guid id) : IApplicantTenantResolver
    {
        public Guid ResolveTenantId() => id;
    }

    private sealed class InMemoryStorage : AegisCore.Application.Foundation.IStorageProvider
    {
        public Task<string> UploadAsync(string path, Stream content, string contentType, CancellationToken ct = default) => Task.FromResult(path);
        public Task<Stream> DownloadAsync(string path, CancellationToken ct = default) => Task.FromResult<Stream>(new MemoryStream());
        public Task DeleteAsync(string path, CancellationToken ct = default) => Task.CompletedTask;
        public Task<bool> ExistsAsync(string path, CancellationToken ct = default) => Task.FromResult(true);
        public Task<IReadOnlyList<string>> ListAsync(string prefix, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<string>>([]);
    }

    private sealed class StubOcr(string text) : ISyllabusOcrService
    {
        public Task<SyllabusOcrOutcome> ExtractAsync(Stream pdf, string fileName, CancellationToken ct = default)
            => Task.FromResult(new SyllabusOcrOutcome(text, "digital"));
    }

    private sealed class StubGenerator(int perCall) : IAdmissionQuestionGenerator
    {
        public Task<IReadOnlyList<GeneratedQuestion>> GenerateAsync(Guid tenantId, string systemPrompt, string syllabusChunk, int count, CancellationToken ct = default)
        {
            var list = Enumerable.Range(0, Math.Min(perCall, count))
                .Select(i => new GeneratedQuestion($"Q{i}", "a", "b", "c", "d", "A", "medium"))
                .ToList();
            return Task.FromResult<IReadOnlyList<GeneratedQuestion>>(list);
        }
    }
}
