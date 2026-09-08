using AegisCore.Application.Admissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.API.Controllers;

/// <summary>
/// On-campus locked-down test terminal (<c>/terminal</c>). No login — the applicant enters
/// their APP ID at a supervised kiosk. Anonymous; the installation tenant is resolved from
/// configuration.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/terminal")]
public sealed class TestTerminalController(ITestTerminalService terminal) : ControllerBase
{
    [HttpPost("start")]
    public async Task<ActionResult<TerminalStartResult>> Start([FromBody] StartRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ApplicantId))
        {
            return BadRequest(new { error = "Enter your Applicant ID." });
        }

        try
        {
            return Ok(await terminal.StartAsync(request.ApplicantId, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{applicantTestId:guid}/answer")]
    public async Task<IActionResult> Answer(Guid applicantTestId, [FromBody] AnswerRequest request, CancellationToken ct)
    {
        try
        {
            await terminal.RecordAnswerAsync(applicantTestId, request.QuestionId, request.OptionKey, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{applicantTestId:guid}/submit")]
    public async Task<ActionResult<TerminalSubmitResult>> Submit(Guid applicantTestId, [FromBody] SubmitRequest? request, CancellationToken ct)
    {
        try
        {
            return Ok(await terminal.SubmitAsync(applicantTestId, request?.Auto ?? false, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    public sealed record StartRequest(string ApplicantId);
    public sealed record AnswerRequest(Guid QuestionId, string OptionKey);
    public sealed record SubmitRequest(bool Auto);
}
