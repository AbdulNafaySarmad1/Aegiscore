using AegisCore.Application.SyncLag;
using AegisCore.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.API.Controllers;

[ApiController][Route("api/admin/sync-lag")]
[AegisCore.API.Authorization.RequirePermission("System", "Write")]
public class SyncLagController : ControllerBase
{
    private readonly ISyncLagService _svc;
    public SyncLagController(ISyncLagService svc) => _svc = svc;

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        var pending = await _svc.GetPendingEntriesAsync(null, ct);
        var now = DateTime.UtcNow;
        var dto = new SyncLagDashboardDto
        {
            TotalPending = pending.Count,
            ExpiringWithin5Min = pending.Count(e => e.GraceExpiresAt <= now.AddMinutes(5)),
            DefaultGraceSeconds = (await _svc.GetConfigAsync(ct)).DefaultGracePeriodSeconds,
            AutoCommitEnabled = (await _svc.GetConfigAsync(ct)).AutoCommitOnGraceExpiry,
            RecentEntries = pending.Take(20).Select(e => new SyncLagEntryDto
            {
                Id = e.Id, EntityType = e.EntityType, FieldName = e.FieldName,
                OriginalValue = e.OriginalValue, WrittenValue = e.WrittenValue,
                Status = e.Status, WrittenAt = e.WrittenAt, GraceExpiresAt = e.GraceExpiresAt,
                SecondsRemaining = Math.Max(0, (int)(e.GraceExpiresAt - now).TotalSeconds),
                IsLocked = e.IsLocked, CanCorrect = !e.IsLocked && e.Status == "PendingSync"
            }).ToList()
        };
        return Ok(dto);
    }

    [HttpPost("record")]
    public async Task<IActionResult> RecordWrite([FromQuery] Guid replicaId, [FromQuery] string entityType, [FromQuery] Guid entityId, [FromQuery] string? fieldName, [FromQuery] string? originalValue, [FromQuery] string writtenValue, [FromQuery] Guid writtenByUserId, CancellationToken ct)
        => Ok(await _svc.RecordWriteAsync(replicaId, entityType, entityId, fieldName, originalValue, writtenValue, writtenByUserId, ct));

    /// <summary>
    /// Correcting a write before it commits. Who did it is taken from the signed-in user: it used to
    /// be a query parameter, and the screen passed the all-zero id, so every correction on the record
    /// was made by nobody.
    /// </summary>
    [HttpPost("entries/{id:guid}/correct")]
    public async Task<IActionResult> Correct(Guid id, [FromQuery] string correctedValue, [FromQuery] string reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return BadRequest(new { error = "A correction needs a reason; it is kept with the entry." });

        var ok = await _svc.CorrectEntryAsync(id, correctedValue, reason, Actor(), ct);
        return ok ? Ok(new { message = "Corrected. The new value is what will reach the central database." }) : BadRequest(new { error = "That entry can no longer be corrected: its grace period has passed and it is committed." });
    }

    [HttpPost("entries/{id:guid}/revert")]
    public async Task<IActionResult> Revert(Guid id, CancellationToken ct)
    {
        var ok = await _svc.RevertEntryAsync(id, Actor(), ct);
        return ok ? Ok(new { message = "Reverted. Nothing will be sent to the central database for this write." }) : BadRequest(new { error = "That entry can no longer be reverted: its grace period has passed and it is committed." });
    }

    private Guid Actor()
        => Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty;

    [HttpPost("commit-expired")]
    public async Task<IActionResult> CommitExpired(CancellationToken ct)
    {
        var count = await _svc.CommitExpiredEntriesAsync(ct);
        return Ok(new { committed = count, message = $"{count} expired entries committed to central DB and locked." });
    }

    [HttpGet("pending")]
    public async Task<IActionResult> Pending([FromQuery] Guid? replicaId, CancellationToken ct)
    { var p = await _svc.GetPendingEntriesAsync(replicaId, ct); return Ok(new { total = p.Count, entries = p }); }

    [HttpGet("config")]
    public async Task<IActionResult> GetConfig(CancellationToken ct) => Ok(await _svc.GetConfigAsync(ct));

    [HttpPut("config")]
    public async Task<IActionResult> UpdateConfig([FromBody] SyncLagConfig config, CancellationToken ct) => Ok(await _svc.UpdateConfigAsync(config, ct));
}
