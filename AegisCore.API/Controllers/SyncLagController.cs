using AegisCore.Application.SyncLag;
using AegisCore.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.API.Controllers;

[ApiController][Route("api/admin/sync-lag")]
[Authorize]
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

    [HttpPost("entries/{id:guid}/correct")]
    public async Task<IActionResult> Correct(Guid id, [FromQuery] string correctedValue, [FromQuery] string reason, [FromQuery] Guid correctedByUserId, CancellationToken ct)
    {
        var ok = await _svc.CorrectEntryAsync(id, correctedValue, reason, correctedByUserId, ct);
        return ok ? Ok(new { message = "Entry corrected — value updated, will sync to central." }) : BadRequest(new { error = "Cannot correct this entry. It may already be locked or committed." });
    }

    [HttpPost("entries/{id:guid}/revert")]
    public async Task<IActionResult> Revert(Guid id, [FromQuery] Guid revertedByUserId, CancellationToken ct)
    {
        var ok = await _svc.RevertEntryAsync(id, revertedByUserId, ct);
        return ok ? Ok(new { message = "Entry reverted — sync cancelled." }) : BadRequest(new { error = "Cannot revert this entry." });
    }

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
