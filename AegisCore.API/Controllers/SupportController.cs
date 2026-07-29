using System.Security.Claims;
using AegisCore.Application.Support;
using AegisCore.Infrastructure.Support;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.API.Controllers;

[ApiController]
[Authorize]
[Route("api/support")]
public sealed class SupportController : ControllerBase
{
    private readonly SupportTicketService _service;

    public SupportController(SupportTicketService service) => _service = service;

    [HttpPost("tickets")]
    public async Task<IActionResult> Create([FromBody] CreateSupportTicketRequest request, CancellationToken ct)
    {
        if (!TryIdentity(out var tenant, out var user)) return Forbid();
        try
        {
            var ticket = await _service.CreateAsync(tenant, user, request, ct);
            return CreatedAtAction(nameof(Get), new { id = ticket.Id }, ticket);
        }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("tickets")]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        if (!TryIdentity(out var tenant, out var user)) return Forbid();
        return Ok(await _service.ListAsync(tenant, user, IsSupportStaff(), ct));
    }

    [HttpGet("tickets/{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        if (!TryIdentity(out var tenant, out var user)) return Forbid();
        var ticket = await _service.GetAsync(tenant, user, id, IsSupportStaff(), ct);
        return ticket is null ? NotFound() : Ok(ticket);
    }

    [HttpPost("tickets/{id:guid}/feedback")]
    public async Task<IActionResult> Feedback(Guid id, [FromBody] SupportFeedbackRequest request, CancellationToken ct)
    {
        if (!TryIdentity(out var tenant, out var user)) return Forbid();
        try
        {
            var ticket = await _service.RecordFeedbackAsync(tenant, user, id, request, ct);
            return ticket is null ? NotFound() : Ok(ticket);
        }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("escalations")]
    public async Task<IActionResult> Escalations(CancellationToken ct)
    {
        if (!TryIdentity(out var tenant, out _) || !IsSupportStaff()) return Forbid();
        return Ok(await _service.ListEscalationsAsync(tenant, ct));
    }

    [HttpPost("escalations/{id:guid}/resolve")]
    public async Task<IActionResult> Resolve(Guid id, [FromBody] ResolveEscalationRequest request, CancellationToken ct)
    {
        if (!TryIdentity(out var tenant, out var user) || !IsSupportStaff()) return Forbid();
        try
        {
            return await _service.ResolveEscalationAsync(tenant, id, user, request, ct)
                ? Ok(new { success = true })
                : NotFound();
        }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("knowledge")]
    public async Task<IActionResult> AddKnowledge([FromBody] KnowledgeArticleRequest request, CancellationToken ct)
    {
        if (!TryIdentity(out var tenant, out var user) || !IsSupportStaff()) return Forbid();
        try
        {
            return Ok(new { id = await _service.AddKnowledgeAsync(tenant, user, request, ct) });
        }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("analytics")]
    public async Task<IActionResult> Analytics(CancellationToken ct)
    {
        if (!TryIdentity(out var tenant, out _) || !IsSupportStaff()) return Forbid();
        return Ok(await _service.GetAnalyticsAsync(tenant, ct));
    }

    private bool TryIdentity(out Guid tenantId, out Guid userId)
    {
        var tenant = User.FindFirstValue("tenant_id") ?? User.FindFirstValue("tid");
        var user = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var hasTenant = Guid.TryParse(tenant, out tenantId);
        var hasUser = Guid.TryParse(user, out userId);
        return hasTenant && hasUser;
    }

    private bool IsSupportStaff() =>
        User.IsInRole("Administrator") || User.IsInRole("VC") || User.IsInRole("Support") ||
        User.Claims.Where(x => x.Type is ClaimTypes.Role or "role" or "roles")
            .Any(x => x.Value.Equals("Administrator", StringComparison.OrdinalIgnoreCase)
                || x.Value.Equals("VC", StringComparison.OrdinalIgnoreCase)
                || x.Value.Equals("Support", StringComparison.OrdinalIgnoreCase));
}
