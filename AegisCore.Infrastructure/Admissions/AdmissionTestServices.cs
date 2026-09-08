using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AegisCore.Application.Admissions;
using AegisCore.Application.Foundation;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Admissions;

/// <summary>Default LLM system prompt seeded into <see cref="QuestionGenerationPrompt"/>; admin can edit it.</summary>
public static class AdmissionQuestionPrompt
{
    public const string Default =
        "You are an exam question generator. Generate {N} multiple choice questions from the provided syllabus content. " +
        "Each question must have 4 options (A/B/C/D) with exactly one correct answer. Difficulty must be appropriate for " +
        "Pakistani HSSC (Grade 11-12) level. Return only valid JSON array, no explanation.";
}

public sealed class SyllabusOcrService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<SyllabusOcrService> logger) : ISyllabusOcrService
{
    public async Task<SyllabusOcrOutcome> ExtractAsync(Stream pdf, string fileName, CancellationToken ct = default)
    {
        var baseUrl = configuration["CLIP_SERVICE_URL"]
            ?? throw new InvalidOperationException("CLIP_SERVICE_URL is not configured — syllabus OCR is unavailable.");

        using var client = httpClientFactory.CreateClient("clip");
        client.BaseAddress = new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/");
        client.Timeout = TimeSpan.FromMinutes(3);

        using var form = new MultipartFormDataContent();
        using var buffer = new MemoryStream();
        await pdf.CopyToAsync(buffer, ct);
        buffer.Position = 0;
        var fileContent = new StreamContent(buffer);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(fileContent, "file", string.IsNullOrWhiteSpace(fileName) ? "syllabus.pdf" : fileName);

        using var response = await client.PostAsync("ocr", form, ct);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<OcrResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("The OCR service returned no result.");

        logger.LogInformation("Syllabus OCR extracted {Chars} chars via {Method}", body.Text?.Length ?? 0, body.Method);
        return new SyllabusOcrOutcome(body.Text ?? string.Empty, body.Method ?? "unknown");
    }

    private sealed record OcrResponse(string? Text, int Pages, string? Method);
}

public sealed class AdmissionQuestionGenerator(LlmProviderFactory llmFactory, ILogger<AdmissionQuestionGenerator> logger)
    : IAdmissionQuestionGenerator
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    public async Task<IReadOnlyList<GeneratedQuestion>> GenerateAsync(
        Guid tenantId, string systemPrompt, string syllabusChunk, int count, CancellationToken ct = default)
    {
        var provider = llmFactory.GetLlmProvider(tenantId);
        var prompt = systemPrompt.Replace("{N}", count.ToString(), StringComparison.Ordinal);
        var raw = await provider.CompleteAsync(prompt, syllabusChunk, new AegisCore.Application.AI.LlmOptions { Temperature = 0.4f, MaxTokens = 2048 }, ct);

        var parsed = Parse(raw);
        if (parsed.Count == 0)
        {
            logger.LogWarning("LLM question generation returned no usable questions.");
        }

        return parsed;
    }

    /// <summary>Parses an LLM completion (JSON array, possibly wrapped in prose/fences) into questions.</summary>
    public static IReadOnlyList<GeneratedQuestion> Parse(string raw)
    {
        var json = ExtractJsonArray(raw);
        if (json is null)
        {
            return [];
        }

        List<RawQuestion>? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<List<RawQuestion>>(json, Json);
        }
        catch (JsonException)
        {
            return [];
        }

        var result = new List<GeneratedQuestion>();
        foreach (var q in parsed ?? [])
        {
            var (a, b, c, d) = ResolveOptions(q);
            var correct = NormalizeCorrect(q.Correct ?? q.CorrectOption ?? q.Answer);
            if (string.IsNullOrWhiteSpace(q.Question) || a is null || b is null || c is null || d is null || correct is null)
            {
                continue;
            }

            result.Add(new GeneratedQuestion(
                q.Question!.Trim(), a, b, c, d, correct,
                string.IsNullOrWhiteSpace(q.Difficulty) ? "medium" : q.Difficulty!.Trim().ToLowerInvariant()));
        }

        return result;
    }

    private static (string? A, string? B, string? C, string? D) ResolveOptions(RawQuestion q)
    {
        if (q.Options is { Count: >= 4 })
        {
            // list form ["...", "...", "...", "..."]
            return (q.Options[0], q.Options[1], q.Options[2], q.Options[3]);
        }

        return (q.A ?? q.OptionA, q.B ?? q.OptionB, q.C ?? q.OptionC, q.D ?? q.OptionD);
    }

    private static string? NormalizeCorrect(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var v = value.Trim().TrimEnd(')', '.', ':').ToUpperInvariant();
        return v is "A" or "B" or "C" or "D" ? v : null;
    }

    private static string? ExtractJsonArray(string raw)
    {
        var start = raw.IndexOf('[');
        var end = raw.LastIndexOf(']');
        return start >= 0 && end > start ? raw[start..(end + 1)] : null;
    }

    private sealed class RawQuestion
    {
        public string? Question { get; set; }
        public List<string>? Options { get; set; }
        public string? A { get; set; }
        public string? B { get; set; }
        public string? C { get; set; }
        public string? D { get; set; }
        public string? OptionA { get; set; }
        public string? OptionB { get; set; }
        public string? OptionC { get; set; }
        public string? OptionD { get; set; }
        public string? Correct { get; set; }
        public string? CorrectOption { get; set; }
        public string? Answer { get; set; }
        public string? Difficulty { get; set; }
    }
}

public sealed class AdmissionTestConfigService(AegisDbContext db) : IAdmissionTestConfigService
{
    private Guid TenantId => db.CurrentTenantId;

    public async Task<AdmissionSubject> CreateSubjectAsync(SubjectInput input, CancellationToken ct = default)
    {
        var subject = new AdmissionSubject
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            Name = input.Name.Trim(),
            Code = input.Code.Trim().ToUpperInvariant(),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.AdmissionSubjects.Add(subject);
        await db.SaveChangesAsync(ct);
        return subject;
    }

    public async Task<AdmissionSubject> SetSubjectActiveAsync(Guid subjectId, bool isActive, CancellationToken ct = default)
    {
        var subject = await db.AdmissionSubjects.FirstOrDefaultAsync(s => s.Id == subjectId, ct)
            ?? throw new InvalidOperationException("Subject was not found.");
        subject.IsActive = isActive;
        await db.SaveChangesAsync(ct);
        return subject;
    }

    public async Task<IReadOnlyList<AdmissionSubject>> ListSubjectsAsync(bool includeInactive, CancellationToken ct = default)
        => await db.AdmissionSubjects
            .Where(s => includeInactive || s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

    public async Task<TestProfileView> UpsertProfileAsync(TestProfileInput input, CancellationToken ct = default)
    {
        if (input.Subjects.Count == 0)
        {
            throw new InvalidOperationException("A test profile needs at least one subject.");
        }

        var subjectIds = input.Subjects.Select(s => s.SubjectId).ToList();
        var known = await db.AdmissionSubjects.Where(s => subjectIds.Contains(s.Id)).Select(s => s.Id).ToListAsync(ct);
        if (known.Count != subjectIds.Distinct().Count())
        {
            throw new InvalidOperationException("One or more subjects are not in the master list.");
        }

        var profile = await db.ProgramTestProfiles.Include(p => p.Subjects)
            .FirstOrDefaultAsync(p => p.IntakeId == input.IntakeId && p.ProgramId == input.ProgramId, ct);

        if (profile is null)
        {
            profile = new ProgramTestProfile
            {
                Id = Guid.NewGuid(), TenantId = TenantId, IntakeId = input.IntakeId, ProgramId = input.ProgramId,
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.ProgramTestProfiles.Add(profile);
        }
        else
        {
            db.ProgramTestSubjects.RemoveRange(profile.Subjects);
        }

        profile.PassMark = input.PassMark;
        profile.TimeLimitMinutes = input.TimeLimitMinutes;
        profile.UpdatedAt = DateTimeOffset.UtcNow;
        profile.Subjects = input.Subjects.Select(s => new ProgramTestSubject
        {
            Id = Guid.NewGuid(), TenantId = TenantId, TestProfileId = profile.Id,
            SubjectId = s.SubjectId, QuestionCount = s.QuestionCount, MarksPerQuestion = s.MarksPerQuestion
        }).ToList();

        await db.SaveChangesAsync(ct);
        return ToView(profile);
    }

    public async Task<TestProfileView?> GetProfileAsync(Guid intakeId, Guid programId, CancellationToken ct = default)
    {
        var profile = await db.ProgramTestProfiles.Include(p => p.Subjects)
            .FirstOrDefaultAsync(p => p.IntakeId == intakeId && p.ProgramId == programId, ct);
        return profile is null ? null : ToView(profile);
    }

    private static TestProfileView ToView(ProgramTestProfile p) => new(
        p.Id, p.IntakeId, p.ProgramId, p.PassMark, p.TimeLimitMinutes,
        p.Subjects.Select(s => new TestSubjectSlot(s.SubjectId, s.QuestionCount, s.MarksPerQuestion)).ToList());
}

public sealed class QuestionBankService(
    AegisDbContext db,
    IStorageProvider storage,
    ISyllabusOcrService ocr,
    IAdmissionQuestionGenerator generator,
    ILogger<QuestionBankService> logger) : IQuestionBankService
{
    private const int ChunkSize = 3000;
    private Guid TenantId => db.CurrentTenantId;

    public async Task<SyllabusIngestResult> IngestSyllabusAsync(
        Guid subjectId, Stream pdf, string fileName, int questionsToGenerate, Guid actorId, CancellationToken ct = default)
    {
        if (!await db.AdmissionSubjects.AnyAsync(s => s.Id == subjectId, ct))
        {
            throw new InvalidOperationException("Subject was not found.");
        }

        using var buffer = new MemoryStream();
        await pdf.CopyToAsync(buffer, ct);
        buffer.Position = 0;

        var storagePath = $"tenants/{TenantId}/syllabus/{subjectId}/{Guid.NewGuid()}.pdf";
        await storage.UploadAsync(storagePath, buffer, "application/pdf", ct);

        buffer.Position = 0;
        var doc = new SyllabusDocument
        {
            Id = Guid.NewGuid(), TenantId = TenantId, SubjectId = subjectId,
            StoragePath = storagePath, OriginalFileName = fileName, UploadedAt = DateTimeOffset.UtcNow,
            OcrStatus = SyllabusOcrStatus.Pending, CreatedBy = actorId
        };
        db.SyllabusDocuments.Add(doc);
        await db.SaveChangesAsync(ct);

        SyllabusOcrOutcome extraction;
        try
        {
            buffer.Position = 0;
            extraction = await ocr.ExtractAsync(buffer, fileName, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Syllabus OCR failed for {SubjectId}", subjectId);
            doc.OcrStatus = SyllabusOcrStatus.Failed;
            await db.SaveChangesAsync(ct);
            throw new InvalidOperationException("Could not extract text from the syllabus PDF.");
        }

        doc.ExtractedText = extraction.Text;
        doc.OcrMethod = extraction.Method;
        doc.OcrStatus = string.IsNullOrWhiteSpace(extraction.Text) ? SyllabusOcrStatus.Failed : SyllabusOcrStatus.Extracted;
        await db.SaveChangesAsync(ct);

        if (doc.OcrStatus == SyllabusOcrStatus.Failed)
        {
            throw new InvalidOperationException("The syllabus PDF produced no readable text.");
        }

        var systemPrompt = await GetActivePromptAsync(ct);
        var chunks = Chunk(extraction.Text).ToList();
        var generated = 0;
        var perChunk = Math.Max(1, (int)Math.Ceiling(questionsToGenerate / (double)Math.Max(1, chunks.Count)));

        foreach (var chunk in chunks)
        {
            if (generated >= questionsToGenerate) break;
            var want = Math.Min(perChunk, questionsToGenerate - generated);
            var questions = await generator.GenerateAsync(TenantId, systemPrompt, chunk, want, ct);

            foreach (var q in questions.Take(want))
            {
                db.AdmissionQuestions.Add(new AdmissionQuestion
                {
                    Id = Guid.NewGuid(), TenantId = TenantId, SubjectId = subjectId,
                    QuestionText = Trunc(q.QuestionText, 2000),
                    OptionA = Trunc(q.OptionA, 600), OptionB = Trunc(q.OptionB, 600),
                    OptionC = Trunc(q.OptionC, 600), OptionD = Trunc(q.OptionD, 600),
                    CorrectOption = q.CorrectOption, DifficultyLevel = Trunc(q.DifficultyLevel, 30),
                    SourceChunk = chunk, GeneratedAt = DateTimeOffset.UtcNow, Status = AdmissionQuestionStatus.Draft
                });
                generated++;
            }
        }

        await db.SaveChangesAsync(ct);
        return new SyllabusIngestResult(doc.Id, extraction.Method, generated);
    }

    public async Task<IReadOnlyList<AdmissionQuestion>> ListAsync(Guid? subjectId, AdmissionQuestionStatus? status, CancellationToken ct = default)
    {
        var query = db.AdmissionQuestions.AsQueryable();
        if (subjectId is { } s) query = query.Where(q => q.SubjectId == s);
        if (status is { } st) query = query.Where(q => q.Status == st);
        var rows = await query.ToListAsync(ct);
        return rows.OrderByDescending(q => q.GeneratedAt).ToList();
    }

    public async Task ReviewAsync(Guid questionId, bool approve, Guid actorId, CancellationToken ct = default)
    {
        var question = await db.AdmissionQuestions.FirstOrDefaultAsync(q => q.Id == questionId, ct)
            ?? throw new InvalidOperationException("Question was not found.");
        question.Status = approve ? AdmissionQuestionStatus.Approved : AdmissionQuestionStatus.Rejected;
        question.ApprovedAt = approve ? DateTimeOffset.UtcNow : null;
        question.ApprovedBy = approve ? actorId : null;
        await db.SaveChangesAsync(ct);
    }

    public async Task<AdmissionQuestion> EditAsync(Guid questionId, QuestionEditInput input, CancellationToken ct = default)
    {
        var question = await db.AdmissionQuestions.FirstOrDefaultAsync(q => q.Id == questionId, ct)
            ?? throw new InvalidOperationException("Question was not found.");

        var correct = input.CorrectOption.Trim().ToUpperInvariant();
        if (correct is not ("A" or "B" or "C" or "D"))
        {
            throw new InvalidOperationException("CorrectOption must be A, B, C or D.");
        }

        question.QuestionText = Trunc(input.QuestionText, 2000);
        question.OptionA = Trunc(input.OptionA, 600);
        question.OptionB = Trunc(input.OptionB, 600);
        question.OptionC = Trunc(input.OptionC, 600);
        question.OptionD = Trunc(input.OptionD, 600);
        question.CorrectOption = correct;
        question.DifficultyLevel = Trunc(input.DifficultyLevel, 30);
        await db.SaveChangesAsync(ct);
        return question;
    }

    public async Task<string> GetActivePromptAsync(CancellationToken ct = default)
    {
        // At most one prompt is active at a time (SetActivePromptAsync deactivates the rest).
        var active = await db.QuestionGenerationPrompts.Where(p => p.IsActive).ToListAsync(ct);
        var row = active.OrderByDescending(p => p.UpdatedAt).FirstOrDefault();
        if (row is not null)
        {
            return row.SystemPrompt;
        }

        var seeded = new QuestionGenerationPrompt
        {
            Id = Guid.NewGuid(), TenantId = TenantId, Name = "Default",
            SystemPrompt = AdmissionQuestionPrompt.Default, IsActive = true, UpdatedAt = DateTimeOffset.UtcNow
        };
        db.QuestionGenerationPrompts.Add(seeded);
        await db.SaveChangesAsync(ct);
        return seeded.SystemPrompt;
    }

    public async Task SetActivePromptAsync(string systemPrompt, Guid actorId, CancellationToken ct = default)
    {
        await foreach (var existing in db.QuestionGenerationPrompts.Where(p => p.IsActive).AsAsyncEnumerable().WithCancellation(ct))
        {
            existing.IsActive = false;
        }

        db.QuestionGenerationPrompts.Add(new QuestionGenerationPrompt
        {
            Id = Guid.NewGuid(), TenantId = TenantId, Name = "Custom",
            SystemPrompt = systemPrompt.Trim(), IsActive = true, UpdatedAt = DateTimeOffset.UtcNow, UpdatedBy = actorId
        });
        await db.SaveChangesAsync(ct);
    }

    private static IEnumerable<string> Chunk(string text)
    {
        text = text.Replace("\f", "\n");
        for (var i = 0; i < text.Length; i += ChunkSize)
        {
            yield return text.Substring(i, Math.Min(ChunkSize, text.Length - i));
        }
    }

    private static string Trunc(string value, int max) => value.Length <= max ? value : value[..max];
}
