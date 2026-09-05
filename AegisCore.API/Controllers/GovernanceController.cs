using AegisCore.Application.Governance;
using AegisCore.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AegisCore.API.Controllers;

[ApiController]
[Route("api/governance")]
[Authorize]
public sealed class GovernanceController(IGovernanceService governance) : ControllerBase
{
    [HttpGet("replication-lag")]
    [AegisCore.API.Authorization.RequirePermission("Settings", "Write")]
    public IActionResult ReplicationLag([FromQuery] int? hours)
        => Ok(new { hours = governance.NormalizeReplicationLag(hours) });

    [HttpPost("grade-overrides")]
    [AegisCore.API.Authorization.RequirePermission("Settings", "Approve")]
    public async Task<IActionResult> RequestGradeOverride([FromBody] GradeOverrideRequest request, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var actor)) return Unauthorized();
        request.RequestedBy = actor;
        return Ok(await governance.RequestGradeOverrideAsync(request, ct));
    }

    [HttpPost("feedback/anonymous")]
    public async Task<IActionResult> AnonymousFeedback([FromBody] AnonymousFeedbackRequest request, CancellationToken ct)
        => Ok(await governance.SubmitAnonymousFeedbackAsync(request, ct));

    [HttpGet("fees/dynamic")]
    public IActionResult DynamicFee([FromQuery] int creditHours, [FromQuery] decimal perCreditHour, [FromQuery] decimal scholarship = 0m, [FromQuery] decimal overrideDeduction = 0m)
        => Ok(new { total = governance.CalculateDynamicFee(creditHours, perCreditHour, scholarship, overrideDeduction) });

    [HttpGet("sections/capacity")]
    public IActionResult SectionCapacity([FromQuery] int regularCount, [FromQuery] int examCount, [FromQuery] int regularCapacity = 60, [FromQuery] int examCapacity = 80)
        => Ok(new { allowed = governance.IsSectionCapacityAllowed(regularCount, examCount, regularCapacity, examCapacity) });
}
