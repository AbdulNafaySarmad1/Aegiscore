using AegisCore.Application.Examination;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.API.Controllers;

[ApiController]
[Route("api/examination")]
[Authorize(Roles = "Faculty,ExamController,HOD,Admin,SuperAdmin")]
public sealed class S3ExaminationController(AegisDbContext db, IGradingService grading, ITranscriptService transcripts) : ControllerBase
{
    [Authorize(Roles = "ExamController,HOD,Admin,SuperAdmin")]
    [HttpPost("schemes")]
    public async Task<ActionResult<AssessmentScheme>> CreateScheme([FromBody] AssessmentScheme scheme, CancellationToken ct)
    {
        if (scheme.Components.Count == 0 || scheme.Components.Any(c => c.TotalMarks <= 0 || c.WeightPercentage < 0)
            || scheme.Components.Sum(c => c.WeightPercentage) != 100)
        {
            return BadRequest("Assessment component weights must total 100 and marks must be positive.");
        }

        scheme.Id = scheme.Id == Guid.Empty ? Guid.NewGuid() : scheme.Id;
        foreach (var component in scheme.Components)
        {
            component.Id = component.Id == Guid.Empty ? Guid.NewGuid() : component.Id;
            component.SchemeId = scheme.Id;
        }

        db.AssessmentSchemes.Add(scheme);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetScheme), new { id = scheme.Id }, scheme);
    }

    [HttpGet("schemes/{id:guid}")]
    public async Task<ActionResult<AssessmentScheme>> GetScheme(Guid id, CancellationToken ct)
        => await db.AssessmentSchemes.Include(s => s.Components).FirstOrDefaultAsync(s => s.Id == id, ct) is { } scheme
            ? scheme : NotFound();

    [HttpPut("marks")]
    public async Task<IActionResult> EnterMarks([FromBody] MarksEntryRequest request, CancellationToken ct)
    {
        var activeModeration = await db.MarkModerations.FirstOrDefaultAsync(m => m.CourseId == request.CourseId && (m.Status == "submitted" || m.Status == "approved"), ct);
        if (activeModeration is not null)
        {
            return Conflict("Marks are locked while moderation is submitted or approved.");
        }

        var component = await db.AssessmentComponents.FindAsync([request.AssessmentComponentId], ct);
        if (component is null || request.ObtainedMarks < 0 || request.ObtainedMarks > component.TotalMarks)
        {
            return BadRequest("Marks are outside the component bounds.");
        }

        var mark = await db.CourseMarks.FirstOrDefaultAsync(m =>
            m.CourseId == request.CourseId && m.StudentId == request.StudentId && m.AssessmentComponentId == request.AssessmentComponentId, ct);
        if (mark is null)
        {
            mark = new CourseMark
            {
                Id = Guid.NewGuid(),
                CourseId = request.CourseId,
                StudentId = request.StudentId,
                AssessmentComponentId = request.AssessmentComponentId
            };
            db.CourseMarks.Add(mark);
        }

        mark.ObtainedMarks = request.ObtainedMarks;
        mark.EnteredBy = request.EnteredBy;
        mark.EntryDate = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("moderation/submit")]
    public async Task<ActionResult<MarkModeration>> SubmitModeration([FromBody] ModerationSubmitRequest request, CancellationToken ct)
    {
        var moderation = await db.MarkModerations.FirstOrDefaultAsync(m => m.CourseId == request.CourseId && m.SemesterId == request.SemesterId, ct);
        if (moderation is not null && moderation.Status == "approved") return Conflict("Approved moderation cannot be resubmitted.");
        moderation ??= new MarkModeration { Id = Guid.NewGuid(), CourseId = request.CourseId, SemesterId = request.SemesterId };
        moderation.Status = "submitted";
        moderation.SubmittedBy = request.ActorId;
        moderation.SubmittedAt = DateTimeOffset.UtcNow;
        if (db.Entry(moderation).State == EntityState.Detached) db.MarkModerations.Add(moderation);
        await db.SaveChangesAsync(ct);
        return Ok(moderation);
    }

    [Authorize(Roles = "ExamController,HOD,Admin,SuperAdmin")]
    [HttpPost("moderation/{id:guid}/review")]
    public async Task<IActionResult> ReviewModeration(Guid id, [FromBody] ModerationReviewRequest request, CancellationToken ct)
    {
        if (request.Status is not ("approved" or "rejected")) return BadRequest("Moderation status is invalid.");
        var moderation = await db.MarkModerations.FindAsync([id], ct);
        if (moderation is null) return NotFound();
        if (moderation.Status != "submitted") return Conflict("Only submitted moderation can be reviewed.");
        moderation.Status = request.Status;
        moderation.ReviewedBy = request.ActorId;
        moderation.ReviewedAt = DateTimeOffset.UtcNow;
        moderation.ReviewComments = request.Comments;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("grades/{courseId:guid}/{studentId:guid}/calculate")]
    public async Task<IActionResult> Calculate(Guid courseId, Guid studentId, CancellationToken ct)
    {
        await grading.CalculateCourseGradeAsync(courseId, studentId, ct);
        return NoContent();
    }

    [HttpGet("grades/{studentId:guid}/sgpa/{semesterId:guid}")]
    public Task<SemesterGpa> Sgpa(Guid studentId, Guid semesterId, CancellationToken ct) => grading.CalculateSGPAAsync(studentId, semesterId, ct);

    [HttpGet("grades/{studentId:guid}/cgpa")]
    public Task<decimal> Cgpa(Guid studentId, CancellationToken ct) => grading.CalculateCGPAAsync(studentId, ct);

    [Authorize(Roles = "ExamController,HOD,Admin,SuperAdmin")]
    [HttpPost("grades/{courseId:guid}/grace")]
    public async Task<IActionResult> Grace(Guid courseId, [FromBody] GraceMarksRequest request, CancellationToken ct)
    {
        var actorId = Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty;
        if (actorId == Guid.Empty)
            return Unauthorized(new { error = "Authenticated user identity is required." });
        await grading.ApplyGracemarksAsync(courseId, request.Entries, actorId, ct);
        return NoContent();
    }

    [Authorize(Roles = "ExamController,HOD,Admin,SuperAdmin")]
    [HttpPost("courses/{courseId:guid}/publish")]
    public async Task<IActionResult> Publish(Guid courseId, [FromBody] PublishResultsRequest request, CancellationToken ct)
    {
        var moderation = await db.MarkModerations.FirstOrDefaultAsync(m => m.CourseId == courseId && m.SemesterId == request.SemesterId, ct);
        if (moderation?.Status != "approved") return Conflict("Marks moderation must be approved before publication.");

        var grades = await db.CourseGrades.Where(g => g.CourseId == courseId && g.SemesterId == request.SemesterId).ToListAsync(ct);
        if (grades.Count == 0) return NotFound();
        foreach (var grade in grades)
        {
            grade.ResultStatus = "published";
            grade.PublishedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("transcripts/{studentId:guid}/request")]
    public async Task<ActionResult<string>> RequestTranscript(Guid studentId, [FromBody] TranscriptRequest request, CancellationToken ct)
        => Ok(await transcripts.RequestTranscriptAsync(studentId, request.RequestedBy, request.Reason, ct));

    [AllowAnonymous]
    [HttpGet("transcripts/verify/{hash}")]
    public Task<bool> VerifyTranscript(string hash, CancellationToken ct) => transcripts.VerifyTranscriptAsync(hash, ct);
}

public sealed record MarksEntryRequest(Guid CourseId, Guid StudentId, Guid AssessmentComponentId, int ObtainedMarks, Guid EnteredBy);
public sealed record GraceMarksRequest(IReadOnlyList<GracemarkEntry> Entries, Guid ActorId);
public sealed record PublishResultsRequest(Guid SemesterId, Guid ActorId);
public sealed record TranscriptRequest(Guid RequestedBy, string Reason);
public sealed record ModerationSubmitRequest(Guid CourseId, Guid SemesterId, Guid ActorId);
public sealed record ModerationReviewRequest(string Status, Guid ActorId, string? Comments);
