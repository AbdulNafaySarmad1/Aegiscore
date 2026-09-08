using System.Security.Claims;
using AegisCore.API.Authorization;
using AegisCore.Application.Admissions;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.API.Controllers;

/// <summary>
/// Staff-facing admission test configuration: subjects, per-program test profiles, syllabus
/// ingestion, the LLM question bank, and per-applicant test scheduling.
/// </summary>
[ApiController]
[Authorize]
[Route("api/admissions")]
public sealed class AdmissionsTestController(
    IAdmissionTestConfigService config,
    IQuestionBankService questionBank,
    ITestSchedulingService scheduling,
    ITestProctorService proctor) : ControllerBase
{
    private Guid CurrentUserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

    // ---- Master subjects ----

    [RequirePermission("Admissions", "Read")]
    [HttpGet("subjects")]
    public async Task<IReadOnlyList<AdmissionSubject>> ListSubjects([FromQuery] bool includeInactive, CancellationToken ct)
        => await config.ListSubjectsAsync(includeInactive, ct);

    [RequirePermission("Admissions", "Configure")]
    [HttpPost("subjects")]
    public async Task<ActionResult<AdmissionSubject>> CreateSubject([FromBody] SubjectInput input, CancellationToken ct)
        => Ok(await config.CreateSubjectAsync(input, ct));

    [RequirePermission("Admissions", "Configure")]
    [HttpPatch("subjects/{subjectId:guid}/active")]
    public async Task<ActionResult<AdmissionSubject>> SetSubjectActive(Guid subjectId, [FromBody] SubjectActiveRequest request, CancellationToken ct)
    {
        try { return Ok(await config.SetSubjectActiveAsync(subjectId, request.IsActive, ct)); }
        catch (InvalidOperationException ex) { return NotFound(new { error = ex.Message }); }
    }

    // ---- Per-program test profile ----

    [RequirePermission("Admissions", "Read")]
    [HttpGet("test-config")]
    public async Task<ActionResult<TestProfileView>> GetProfile([FromQuery] Guid intakeId, [FromQuery] Guid programId, CancellationToken ct)
        => await config.GetProfileAsync(intakeId, programId, ct) is { } view ? Ok(view) : NotFound();

    [RequirePermission("Admissions", "Configure")]
    [HttpPut("test-config")]
    public async Task<ActionResult<TestProfileView>> UpsertProfile([FromBody] TestProfileInput input, CancellationToken ct)
    {
        try { return Ok(await config.UpsertProfileAsync(input, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    // ---- Syllabus ingestion + question bank ----

    [RequirePermission("Admissions", "Configure")]
    [HttpPost("syllabus/{subjectId:guid}")]
    [RequestSizeLimit(40 * 1024 * 1024)]
    public async Task<ActionResult<SyllabusIngestResult>> IngestSyllabus(
        Guid subjectId, IFormFile file, [FromForm] int questions, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = "A syllabus PDF is required." });
        }

        if (questions is < 1 or > 200)
        {
            return BadRequest(new { error = "Request between 1 and 200 questions." });
        }

        await using var stream = file.OpenReadStream();
        try
        {
            return Ok(await questionBank.IngestSyllabusAsync(subjectId, stream, file.FileName, questions, CurrentUserId, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [RequirePermission("Admissions", "Read")]
    [HttpGet("question-bank")]
    public async Task<IReadOnlyList<AdmissionQuestion>> ListQuestions(
        [FromQuery] Guid? subjectId, [FromQuery] AdmissionQuestionStatus? status, CancellationToken ct)
        => await questionBank.ListAsync(subjectId, status, ct);

    [RequirePermission("Admissions", "ApproveQuestions")]
    [HttpPost("question-bank/{questionId:guid}/review")]
    public async Task<IActionResult> ReviewQuestion(Guid questionId, [FromBody] QuestionReviewRequest request, CancellationToken ct)
    {
        try { await questionBank.ReviewAsync(questionId, request.Approve, CurrentUserId, ct); return NoContent(); }
        catch (InvalidOperationException ex) { return NotFound(new { error = ex.Message }); }
    }

    [RequirePermission("Admissions", "ApproveQuestions")]
    [HttpPut("question-bank/{questionId:guid}")]
    public async Task<ActionResult<AdmissionQuestion>> EditQuestion(Guid questionId, [FromBody] QuestionEditInput input, CancellationToken ct)
    {
        try { return Ok(await questionBank.EditAsync(questionId, input, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [RequirePermission("Admissions", "Read")]
    [HttpGet("question-prompt")]
    public async Task<ActionResult<object>> GetPrompt(CancellationToken ct)
        => Ok(new { systemPrompt = await questionBank.GetActivePromptAsync(ct) });

    [RequirePermission("Admissions", "Configure")]
    [HttpPut("question-prompt")]
    public async Task<IActionResult> SetPrompt([FromBody] PromptRequest request, CancellationToken ct)
    {
        await questionBank.SetActivePromptAsync(request.SystemPrompt, CurrentUserId, ct);
        return NoContent();
    }

    // ---- Scheduling ----

    [RequirePermission("Admissions", "Approve")]
    [HttpPost("applicants/{applicantId:guid}/schedule-test")]
    public async Task<ActionResult<ScheduleTestResult>> ScheduleTest(Guid applicantId, [FromBody] ScheduleTestRequest? request, CancellationToken ct)
    {
        try { return Ok(await scheduling.ScheduleAsync(applicantId, request?.ExpiresAt, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    // ---- Proctor ----

    [RequirePermission("Admissions", "Proctor")]
    [HttpGet("proctor")]
    public async Task<IReadOnlyList<ProctorRow>> ProctorLive(CancellationToken ct)
        => await proctor.LiveAsync(ct);

    [RequirePermission("Admissions", "Proctor")]
    [HttpPost("proctor/{applicantTestId:guid}/force-submit")]
    public async Task<IActionResult> ForceSubmit(Guid applicantTestId, CancellationToken ct)
    {
        try { await proctor.ForceSubmitAsync(applicantTestId, CurrentUserId, ct); return NoContent(); }
        catch (InvalidOperationException ex) { return NotFound(new { error = ex.Message }); }
    }

    [RequirePermission("Admissions", "Proctor")]
    [HttpPost("proctor/{applicantTestId:guid}/flag")]
    public async Task<IActionResult> Flag(Guid applicantTestId, [FromBody] ProctorFlagRequest request, CancellationToken ct)
    {
        try { await proctor.FlagAsync(applicantTestId, CurrentUserId, request.Note, ct); return NoContent(); }
        catch (InvalidOperationException ex) { return NotFound(new { error = ex.Message }); }
    }

    public sealed record SubjectActiveRequest(bool IsActive);
    public sealed record QuestionReviewRequest(bool Approve);
    public sealed record PromptRequest(string SystemPrompt);
    public sealed record ScheduleTestRequest(DateTimeOffset? ExpiresAt);
    public sealed record ProctorFlagRequest(string Note);
}
