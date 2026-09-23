using System.Security.Claims;
using AegisCore.API.Authorization;
using AegisCore.Application.Obe;
using AegisCore.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.API.Controllers;

/// <summary>
/// Course evaluation surveys run in AegisCore. The registrar writes and opens a survey
/// (<c>Grades.Write</c>); students answer it once per enrolled course; the answers feed the
/// CLO/PLO survey report at <c>GET /api/obe/v2/indirect</c>.
/// </summary>
[ApiController]
[Authorize]
[Route("api/obe/v2/surveys")]
public sealed class OutcomeSurveysController(IOutcomeSurveyService surveys, AegisDbContext db, ILogger<OutcomeSurveysController> logger) : ControllerBase
{
    [HttpGet, RequirePermission("Grades", "Read")]
    public Task<IActionResult> List(CancellationToken ct) => Run(async () => Ok(await surveys.ListAsync(ct)));

    [HttpGet("{id:guid}"), RequirePermission("Grades", "Read")]
    public Task<IActionResult> Get(Guid id, CancellationToken ct) => Run(async () => Ok(await surveys.GetAsync(id, ct)));

    [HttpPost, RequirePermission("Grades", "Write")]
    public Task<IActionResult> Create([FromBody] SurveyDefinition definition, CancellationToken ct) => Run(async () =>
    {
        var saved = await surveys.SaveAsync(null, definition, UserId, ct);
        logger.LogInformation("Outcome survey {SurveyId} created by {UserId} with {Questions} fixed question(s).", saved.Id, UserId, saved.Questions.Count);
        return Ok(saved);
    });

    [HttpPut("{id:guid}"), RequirePermission("Grades", "Write")]
    public Task<IActionResult> Update(Guid id, [FromBody] SurveyDefinition definition, CancellationToken ct)
        => Run(async () => Ok(await surveys.SaveAsync(id, definition, UserId, ct)));

    [HttpPost("{id:guid}/open"), RequirePermission("Grades", "Write")]
    public Task<IActionResult> Open(Guid id, CancellationToken ct) => Run(async () =>
    {
        var result = await surveys.SetStatusAsync(id, "Open", ct);
        logger.LogInformation("Outcome survey {SurveyId} opened by {UserId}.", id, UserId);
        return Ok(result);
    });

    [HttpPost("{id:guid}/close"), RequirePermission("Grades", "Write")]
    public Task<IActionResult> Close(Guid id, CancellationToken ct) => Run(async () =>
    {
        var result = await surveys.SetStatusAsync(id, "Closed", ct);
        logger.LogInformation("Outcome survey {SurveyId} closed by {UserId}.", id, UserId);
        return Ok(result);
    });

    [HttpDelete("{id:guid}"), RequirePermission("Grades", "Write")]
    public Task<IActionResult> Delete(Guid id, CancellationToken ct) => Run(async () =>
    {
        var answers = await surveys.DeleteAsync(id, ct);
        logger.LogInformation("Outcome survey {SurveyId} deleted by {UserId} with {Answers} recorded answer(s).", id, UserId, answers);
        return Ok(new { deleted = true, answers });
    });

    // ── The student's side ───────────────────────────────────────────────────────────────────

    /// <summary>Every open survey, once for each course the signed-in student is enrolled in.</summary>
    [HttpGet("mine")]
    public Task<IActionResult> Mine(CancellationToken ct) => Run(async () =>
        await StudentAsync(ct) is Guid student ? Ok(await surveys.ForStudentAsync(student, ct)) : NotStudent());

    [HttpGet("{id:guid}/courses/{courseId:guid}")]
    public Task<IActionResult> Form(Guid id, Guid courseId, CancellationToken ct) => Run(async () =>
        await StudentAsync(ct) is Guid student ? Ok(await surveys.FormAsync(student, id, courseId, ct)) : NotStudent());

    [HttpPost("{id:guid}/courses/{courseId:guid}")]
    public Task<IActionResult> Submit(Guid id, Guid courseId, [FromBody] SubmitEvaluationRequest request, CancellationToken ct) => Run(async () =>
    {
        if (await StudentAsync(ct) is not Guid student) return NotStudent();
        var receipt = await surveys.SubmitAsync(student, id, courseId, request.Answers ?? [], ct);
        // Logged without the student: the survey promises the answers are not tied to them.
        logger.LogInformation("Outcome survey {SurveyId}: a response for course {CourseId} was recorded ({Ratings} ratings).", id, courseId, receipt.Ratings);
        return Ok(receipt);
    });

    private Guid? UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : null;

    private async Task<Guid?> StudentAsync(CancellationToken ct)
        => UserId is Guid id && await db.Students.AsNoTracking().AnyAsync(s => s.Id == id, ct) ? id : null;

    private ObjectResult NotStudent() => StatusCode(StatusCodes.Status403Forbidden, new { error = "Course evaluations are answered from a student account." });

    private async Task<IActionResult> Run(Func<Task<IActionResult>> action)
    {
        try { return await action(); }
        catch (SurveyRuleException ex) { return StatusCode(ex.Status, new { error = ex.Message }); }
    }
}

public sealed record SubmitEvaluationRequest(Dictionary<string, string?>? Answers);
