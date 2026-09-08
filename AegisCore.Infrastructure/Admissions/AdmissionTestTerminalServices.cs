using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AegisCore.Application.Admissions;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Admissions;

internal sealed record PaperEntry(Guid QuestionId, Guid SubjectId, int Marks, string[] Order);

internal static class TestPaper
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    public static string Serialize(IEnumerable<PaperEntry> entries) => JsonSerializer.Serialize(entries, Json);

    public static List<PaperEntry> Deserialize(string paperJson)
        => JsonSerializer.Deserialize<List<PaperEntry>>(paperJson, Json) ?? [];

    public static string Hash(IEnumerable<PaperEntry> entries)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join(",", entries.Select(e => e.QuestionId.ToString("N")))))).ToLowerInvariant();

    public static string[] ShuffledKeys()
    {
        var keys = new[] { "A", "B", "C", "D" };
        for (var i = keys.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(0, i + 1);
            (keys[i], keys[j]) = (keys[j], keys[i]);
        }
        return keys;
    }

    public static string OptionText(AdmissionQuestion q, string key) => key switch
    {
        "A" => q.OptionA,
        "B" => q.OptionB,
        "C" => q.OptionC,
        "D" => q.OptionD,
        _ => string.Empty
    };
}

internal static class TestScoring
{
    /// <summary>Scores the paper against the recorded responses, marks the test complete, and returns pass/fail.</summary>
    public static async Task<bool> FinalizeAsync(AegisDbContext db, ApplicantTest test, ApplicantTestStatus status, CancellationToken ct)
    {
        var testProfile = await db.ProgramTestProfiles.IgnoreQueryFilters().FirstAsync(p => p.Id == test.TestProfileId, ct);
        var entries = TestPaper.Deserialize(test.PaperJson);
        var responses = JsonSerializer.Deserialize<Dictionary<string, string>>(test.ResponsesJson) ?? new();

        var questionIds = entries.Select(e => e.QuestionId).ToList();
        var correct = await db.AdmissionQuestions.IgnoreQueryFilters()
            .Where(q => questionIds.Contains(q.Id))
            .Select(q => new { q.Id, q.CorrectOption })
            .ToDictionaryAsync(q => q.Id, q => q.CorrectOption, ct);

        var score = 0;
        foreach (var entry in entries)
        {
            if (responses.TryGetValue(entry.QuestionId.ToString(), out var chosen)
                && correct.TryGetValue(entry.QuestionId, out var answer)
                && string.Equals(chosen, answer, StringComparison.OrdinalIgnoreCase))
            {
                score += entry.Marks;
            }
        }

        test.Score = score;
        test.Passed = score >= testProfile.PassMark;
        test.CompletedAt = DateTimeOffset.UtcNow;
        test.Status = status;
        await db.SaveChangesAsync(ct);
        return test.Passed.Value;
    }
}

public sealed class TestSchedulingService(AegisDbContext db) : ITestSchedulingService
{
    public async Task<ScheduleTestResult> ScheduleAsync(Guid applicantProfileId, DateTimeOffset? expiresAt, CancellationToken ct = default)
    {
        var profile = await db.ApplicantProfiles.FirstOrDefaultAsync(p => p.Id == applicantProfileId, ct)
            ?? throw new InvalidOperationException("Applicant profile was not found.");

        if (await db.ApplicantTests.AnyAsync(t => t.ApplicantId == applicantProfileId, ct))
        {
            throw new InvalidOperationException("This applicant already has a scheduled test.");
        }

        var testProfile = await db.ProgramTestProfiles.Include(p => p.Subjects)
            .FirstOrDefaultAsync(p => p.IntakeId == profile.IntakeId && p.ProgramId == profile.ProgramId, ct)
            ?? throw new InvalidOperationException("No test profile is configured for this applicant's program and intake.");

        if (testProfile.Subjects.Count == 0)
        {
            throw new InvalidOperationException("The test profile has no subjects configured.");
        }

        var entries = new List<PaperEntry>();
        foreach (var slot in testProfile.Subjects)
        {
            var pool = await db.AdmissionQuestions
                .Where(q => q.SubjectId == slot.SubjectId && q.Status == AdmissionQuestionStatus.Approved)
                .Select(q => q.Id)
                .ToListAsync(ct);

            if (pool.Count < slot.QuestionCount)
            {
                throw new InvalidOperationException(
                    $"Subject {slot.SubjectId} has only {pool.Count} approved questions but the profile requires {slot.QuestionCount}.");
            }

            foreach (var questionId in Shuffle(pool).Take(slot.QuestionCount))
            {
                entries.Add(new PaperEntry(questionId, slot.SubjectId, slot.MarksPerQuestion, TestPaper.ShuffledKeys()));
            }
        }

        // Interleave subjects so the paper is not grouped by subject, then keep that order.
        entries = Shuffle(entries).ToList();

        var test = new ApplicantTest
        {
            Id = Guid.NewGuid(),
            TenantId = profile.TenantId,
            ApplicantId = applicantProfileId,
            TestProfileId = testProfile.Id,
            GeneratedAt = DateTimeOffset.UtcNow,
            ExpiresAt = expiresAt ?? DateTimeOffset.UtcNow.AddDays(7),
            PaperHash = TestPaper.Hash(entries),
            PaperJson = TestPaper.Serialize(entries),
            ResponsesJson = "{}",
            Status = ApplicantTestStatus.Scheduled
        };
        db.ApplicantTests.Add(test);

        if (profile.Status is ApplicantStatus.Submitted or ApplicantStatus.UnderReview)
        {
            profile.Status = ApplicantStatus.TestScheduled;
            profile.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        return new ScheduleTestResult(test.Id, test.ExpiresAt, test.PaperHash, entries.Count);
    }

    private static IEnumerable<T> Shuffle<T>(IReadOnlyList<T> source)
    {
        var copy = source.ToArray();
        for (var i = copy.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(0, i + 1);
            (copy[i], copy[j]) = (copy[j], copy[i]);
        }
        return copy;
    }
}

public sealed class TestTerminalService(
    AegisDbContext db,
    IApplicantTenantResolver tenantResolver,
    ILogger<TestTerminalService> logger) : ITestTerminalService
{
    private Guid TenantId => tenantResolver.ResolveTenantId();

    public async Task<TerminalStartResult> StartAsync(string applicantId, CancellationToken ct = default)
    {
        var tenantId = TenantId;
        applicantId = applicantId.Trim().ToUpperInvariant();

        var profile = await db.ApplicantProfiles.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.ApplicantId == applicantId, ct)
            ?? throw new InvalidOperationException("No applicant was found for that ID.");

        var test = await db.ApplicantTests.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.ApplicantId == profile.Id, ct)
            ?? throw new InvalidOperationException("No test is scheduled for this applicant.");

        if (test.Status is ApplicantTestStatus.Submitted or ApplicantTestStatus.ForceSubmitted or ApplicantTestStatus.Expired)
        {
            throw new InvalidOperationException("This test has already been completed.");
        }

        if (DateTimeOffset.UtcNow >= test.ExpiresAt)
        {
            test.Status = ApplicantTestStatus.Expired;
            await db.SaveChangesAsync(ct);
            throw new InvalidOperationException("The scheduled test window has passed.");
        }

        var testProfile = await db.ProgramTestProfiles.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == test.TestProfileId, ct)
            ?? throw new InvalidOperationException("The test profile is missing.");

        if (test.Status == ApplicantTestStatus.Scheduled)
        {
            test.StartedAt = DateTimeOffset.UtcNow;
            test.Status = ApplicantTestStatus.InProgress;
            await db.SaveChangesAsync(ct);
        }

        var deadline = (test.StartedAt ?? DateTimeOffset.UtcNow).AddMinutes(testProfile.TimeLimitMinutes);
        var entries = TestPaper.Deserialize(test.PaperJson);
        var questionIds = entries.Select(e => e.QuestionId).ToList();
        var questions = await db.AdmissionQuestions.IgnoreQueryFilters()
            .Where(q => questionIds.Contains(q.Id))
            .ToDictionaryAsync(q => q.Id, ct);

        var terminalQuestions = new List<TerminalQuestion>();
        for (var i = 0; i < entries.Count; i++)
        {
            if (!questions.TryGetValue(entries[i].QuestionId, out var q)) continue;
            var options = entries[i].Order
                .Select(key => new TerminalOption(key, TestPaper.OptionText(q, key)))
                .ToList();
            terminalQuestions.Add(new TerminalQuestion(i + 1, q.Id, q.QuestionText, options));
        }

        return new TerminalStartResult(
            test.Id, profile.FullName ?? profile.ApplicantId, testProfile.TimeLimitMinutes, deadline,
            terminalQuestions.Count, terminalQuestions);
    }

    public async Task RecordAnswerAsync(Guid applicantTestId, Guid questionId, string optionKey, CancellationToken ct = default)
    {
        var tenantId = TenantId;
        var test = await db.ApplicantTests.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.Id == applicantTestId, ct)
            ?? throw new InvalidOperationException("Test was not found.");

        if (test.Status != ApplicantTestStatus.InProgress)
        {
            throw new InvalidOperationException("This test is not in progress.");
        }

        var testProfile = await db.ProgramTestProfiles.IgnoreQueryFilters().FirstAsync(p => p.Id == test.TestProfileId, ct);
        var deadline = (test.StartedAt ?? test.GeneratedAt).AddMinutes(testProfile.TimeLimitMinutes);
        if (DateTimeOffset.UtcNow > deadline)
        {
            await TestScoring.FinalizeAsync(db, test, ApplicantTestStatus.Submitted, ct);
            throw new InvalidOperationException("Time is up — the test has been submitted.");
        }

        optionKey = optionKey.Trim().ToUpperInvariant();
        if (optionKey is not ("A" or "B" or "C" or "D"))
        {
            throw new InvalidOperationException("Option must be A, B, C or D.");
        }

        var entries = TestPaper.Deserialize(test.PaperJson);
        if (entries.All(e => e.QuestionId != questionId))
        {
            throw new InvalidOperationException("That question is not part of this paper.");
        }

        var responses = JsonSerializer.Deserialize<Dictionary<string, string>>(test.ResponsesJson) ?? new();
        responses[questionId.ToString()] = optionKey;
        test.ResponsesJson = JsonSerializer.Serialize(responses);
        await db.SaveChangesAsync(ct);
    }

    public async Task<TerminalSubmitResult> SubmitAsync(Guid applicantTestId, bool autoSubmitted, CancellationToken ct = default)
    {
        var tenantId = TenantId;
        var test = await db.ApplicantTests.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.Id == applicantTestId, ct)
            ?? throw new InvalidOperationException("Test was not found.");

        if (test.Status is ApplicantTestStatus.Submitted or ApplicantTestStatus.ForceSubmitted)
        {
            return new TerminalSubmitResult(test.Passed ?? false);
        }

        var passed = await TestScoring.FinalizeAsync(db, test, ApplicantTestStatus.Submitted, ct);
        logger.LogInformation("Applicant test {TestId} submitted (auto={Auto}) — passed={Passed}", applicantTestId, autoSubmitted, passed);
        return new TerminalSubmitResult(passed);
    }
}

public sealed class TestProctorService(AegisDbContext db) : ITestProctorService
{
    public async Task<IReadOnlyList<ProctorRow>> LiveAsync(CancellationToken ct = default)
    {
        var rows = await db.ApplicantTests
            .Where(t => t.Status == ApplicantTestStatus.InProgress)
            .Join(db.ApplicantProfiles, t => t.ApplicantId, p => p.Id, (t, p) => new ProctorRow(
                t.Id, p.ApplicantId, p.FullName ?? p.ApplicantId, t.StartedAt, t.ExpiresAt, t.Status.ToString(), t.ProctorNote))
            .ToListAsync(ct);
        return rows;
    }

    public async Task ForceSubmitAsync(Guid applicantTestId, Guid proctorId, CancellationToken ct = default)
    {
        var test = await db.ApplicantTests.FirstOrDefaultAsync(t => t.Id == applicantTestId, ct)
            ?? throw new InvalidOperationException("Test was not found.");
        if (test.Status is ApplicantTestStatus.Submitted or ApplicantTestStatus.ForceSubmitted)
        {
            return;
        }

        test.ProctorId = proctorId;
        await TestScoring.FinalizeAsync(db, test, ApplicantTestStatus.ForceSubmitted, ct);
    }

    public async Task FlagAsync(Guid applicantTestId, Guid proctorId, string note, CancellationToken ct = default)
    {
        var test = await db.ApplicantTests.FirstOrDefaultAsync(t => t.Id == applicantTestId, ct)
            ?? throw new InvalidOperationException("Test was not found.");
        test.ProctorId = proctorId;
        test.ProctorNote = note.Length > 500 ? note[..500] : note;
        await db.SaveChangesAsync(ct);
    }
}
