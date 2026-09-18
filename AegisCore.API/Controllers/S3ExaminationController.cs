using System.Security.Claims;
using AegisCore.Application.Examination;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.API.Controllers;

[ApiController]
[Route("api/examination")]
[AegisCore.API.Authorization.RequirePermission("Grades", "Read")]
public sealed class S3ExaminationController(AegisDbContext db, IGradingService grading, ITranscriptService transcripts) : ControllerBase
{
    /// <summary>
    /// The scheme as a caller describes it. The entity could not be bound from a request: its
    /// component carries a required navigation back to the scheme, so model binding demanded a
    /// "scheme" field inside every component and refused every well-formed payload. No assessment
    /// scheme could be created through the API at all.
    /// </summary>
    public sealed record AssessmentSchemeInput(Guid CourseId, Guid SemesterId, Guid ExamControllerId, List<AssessmentComponentInput> Components);
    public sealed record AssessmentComponentInput(string ComponentType, int WeightPercentage, int TotalMarks, int? PassingMarks, bool IsMandatory, int OrderIndex);

    /// <summary>
    /// The scheme as it is read back. The entity itself cannot be written to a response: a component
    /// holds a reference back to its scheme, so serialising one walked the cycle and failed halfway
    /// through, after the response had already begun — a 500 with a truncated body.
    /// </summary>
    public sealed record AssessmentSchemeView(Guid Id, Guid CourseId, Guid SemesterId, Guid ExamControllerId, DateTimeOffset CreatedAt, List<AssessmentComponentView> Components);
    public sealed record AssessmentComponentView(Guid Id, string ComponentType, int WeightPercentage, int TotalMarks, int? PassingMarks, bool IsMandatory, int OrderIndex);

    private static AssessmentSchemeView Describe(AssessmentScheme scheme) => new(
        scheme.Id, scheme.CourseId, scheme.SemesterId, scheme.ExamControllerId, scheme.CreatedAt,
        scheme.Components.OrderBy(c => c.OrderIndex)
            .Select(c => new AssessmentComponentView(c.Id, c.ComponentType, c.WeightPercentage, c.TotalMarks, c.PassingMarks, c.IsMandatory, c.OrderIndex))
            .ToList());

    [AegisCore.API.Authorization.RequirePermission("Grades", "Write")]
    [HttpPost("schemes")]
    public async Task<ActionResult<AssessmentSchemeView>> CreateScheme([FromBody] AssessmentSchemeInput input, CancellationToken ct)
    {
        if (input.Components.Count == 0 || input.Components.Any(c => c.TotalMarks <= 0 || c.WeightPercentage < 0)
            || input.Components.Sum(c => c.WeightPercentage) != 100)
        {
            return BadRequest(new { error = "An assessment scheme's components must add to 100% of the course, and each must be marked out of a positive number." });
        }

        if (!await db.Courses.AnyAsync(c => c.Id == input.CourseId, ct))
        {
            return NotFound(new { error = "That course is not on file." });
        }

        var scheme = new AssessmentScheme
        {
            Id = Guid.NewGuid(),
            CourseId = input.CourseId,
            SemesterId = input.SemesterId,
            ExamControllerId = input.ExamControllerId,
            Components = input.Components.Select(c => new AssessmentComponent
            {
                Id = Guid.NewGuid(),
                ComponentType = c.ComponentType,
                WeightPercentage = c.WeightPercentage,
                TotalMarks = c.TotalMarks,
                PassingMarks = c.PassingMarks,
                IsMandatory = c.IsMandatory,
                OrderIndex = c.OrderIndex,
            }).ToList(),
        };

        foreach (var component in scheme.Components)
        {
            component.SchemeId = scheme.Id;
        }

        db.AssessmentSchemes.Add(scheme);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetScheme), new { id = scheme.Id }, Describe(scheme));
    }

    [HttpGet("schemes/{id:guid}")]
    public async Task<ActionResult<AssessmentSchemeView>> GetScheme(Guid id, CancellationToken ct)
        => await db.AssessmentSchemes.Include(s => s.Components).AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct) is { } scheme
            ? Describe(scheme) : NotFound(new { error = "No assessment scheme with that id." });

    /// <summary>
    /// A course's scheme. The marks screen knows which course it is entering marks for, not which
    /// scheme id that is; it used to ask for the scheme by the course id and always got nothing.
    /// </summary>
    [HttpGet("schemes/by-course/{courseId:guid}")]
    public async Task<ActionResult<AssessmentSchemeView>> GetSchemeForCourse(Guid courseId, CancellationToken ct)
        => await db.AssessmentSchemes.Include(s => s.Components).AsNoTracking()
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(s => s.CourseId == courseId, ct) is { } scheme
            ? Describe(scheme)
            : NotFound(new { error = "This course has no assessment scheme yet, so there is nothing to enter marks against." });

    [HttpPut("marks")]
    public async Task<IActionResult> EnterMarks([FromBody] MarksEntryRequest request, CancellationToken ct)
    {
        // A mark is signed by whoever is signed in. It used to be whoever the caller said, and the
        // portal said the all-zero id, so the validator refused every entry from the grade screen
        // and no mark could be entered through it at all.
        if (!Guid.TryParse(User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier), out var enteredBy))
        {
            return Unauthorized(new { error = "Marks are recorded against the person entering them; sign in again." });
        }

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
        mark.EnteredBy = enteredBy;
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

    [AegisCore.API.Authorization.RequirePermission("Grades", "Approve")]
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
    [AegisCore.API.Authorization.RequireFerpaAudit("Grade", "studentId")]
    public async Task<IActionResult> Calculate(Guid courseId, Guid studentId, CancellationToken ct)
    {
        await grading.CalculateCourseGradeAsync(courseId, studentId, ct);
        return NoContent();
    }

    [AegisCore.API.Authorization.RequireFerpaAudit("Grade", "studentId")]
    [HttpGet("grades/{studentId:guid}/sgpa/{semesterId:guid}")]
    public Task<SemesterGpa> Sgpa(Guid studentId, Guid semesterId, CancellationToken ct) => grading.CalculateSGPAAsync(studentId, semesterId, ct);

    [AegisCore.API.Authorization.RequireFerpaAudit("Grade", "studentId")]
    [HttpGet("grades/{studentId:guid}/cgpa")]
    public Task<decimal> Cgpa(Guid studentId, CancellationToken ct) => grading.CalculateCGPAAsync(studentId, ct);

    [AegisCore.API.Authorization.RequirePermission("Grades", "Write")]
    [HttpPost("grades/{courseId:guid}/grace")]
    public async Task<IActionResult> Grace(Guid courseId, [FromBody] GraceMarksRequest request, CancellationToken ct)
    {
        var actorId = Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty;
        if (actorId == Guid.Empty)
            return Unauthorized(new { error = "Authenticated user identity is required." });
        await grading.ApplyGracemarksAsync(courseId, request.Entries, actorId, ct);
        return NoContent();
    }

    [AegisCore.API.Authorization.RequirePermission("Grades", "Approve")]
    [AegisCore.API.Authorization.RequireFerpaAudit("Grade", "courseId")]
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

    [AegisCore.API.Authorization.RequireFerpaAudit("Transcript", "studentId")]
    [HttpPost("transcripts/{studentId:guid}/request")]
    public async Task<ActionResult<string>> RequestTranscript(Guid studentId, [FromBody] TranscriptRequest request, CancellationToken ct)
        => Ok(await transcripts.RequestTranscriptAsync(studentId, request.RequestedBy, request.Reason, ct));

    [AllowAnonymous]
    [HttpGet("transcripts/verify/{hash}")]
    public Task<bool> VerifyTranscript(string hash, CancellationToken ct) => transcripts.VerifyTranscriptAsync(hash, ct);
}

public sealed record MarksEntryRequest(Guid CourseId, Guid StudentId, Guid AssessmentComponentId, int ObtainedMarks);
public sealed record GraceMarksRequest(IReadOnlyList<GracemarkEntry> Entries, Guid ActorId);
public sealed record PublishResultsRequest(Guid SemesterId, Guid ActorId);
public sealed record TranscriptRequest(Guid RequestedBy, string Reason);
public sealed record ModerationSubmitRequest(Guid CourseId, Guid SemesterId, Guid ActorId);
public sealed record ModerationReviewRequest(string Status, Guid ActorId, string? Comments);
