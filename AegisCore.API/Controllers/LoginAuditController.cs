using AegisCore.Application.Common.Interfaces;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.API.Controllers;

[ApiController][Route("api/security")]
public class LoginAuditController : ControllerBase
{
    private readonly ILoginAuditRecordRepository _repo;
    private readonly ILoginAuditService _auditService;
    private readonly ILogger<LoginAuditController> _logger;

    public LoginAuditController(ILoginAuditRecordRepository repo, ILoginAuditService auditService, ILogger<LoginAuditController> logger)
    { _repo = repo; _auditService = auditService; _logger = logger; }

    [HttpPost("record")]
    public async Task<IActionResult> Record([FromBody] LoginAuditRecord record, CancellationToken ct)
    {
        record.IpAddress = GetClientIp();
        record.UserAgent = Request.Headers.UserAgent;
        await _auditService.RecordAsync(record, ct);
        return Ok(new { message = "Recorded." });
    }

    [HttpGet("login-history")]
    public async Task<IActionResult> GetHistory([FromQuery] Guid? userId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        IReadOnlyList<LoginAuditRecord> records;
        if (userId.HasValue) records = await _repo.GetByUserAsync(userId.Value, from, to, ct);
        else records = await _repo.GetAllAsync(ct);

        return Ok(new { total = records.Count, records = records.OrderByDescending(r => r.OccurredAt).Take(100) });
    }

    [HttpGet("active-sessions")]
    public async Task<IActionResult> GetActiveSessions(CancellationToken ct)
    {
        var sessions = await _repo.GetActiveSessionsAsync(ct);
        var response = sessions.Select(s => new { s.AuditId, s.UserId, s.UserEmail, s.IpAddress, s.IpCountry, s.DeviceType, s.OccurredAt, s.SessionId });
        return Ok(new { total = response.Count(), sessions = response });
    }

    [HttpPost("sessions/{sessionId}/force-logout")]
    public async Task<IActionResult> ForceLogout(string sessionId, CancellationToken ct)
    {
        var record = new LoginAuditRecord { AuditId = Guid.NewGuid(), EventType = LoginEvent.ForcedLogout, SessionId = sessionId, IpAddress = GetClientIp(), IsSuccessful = true, FailureReason = $"Forced by admin", UserEmail = "admin" };
        await _auditService.RecordAsync(record, ct);
        return Ok(new { message = "Session terminated." });
    }

    [HttpGet("login-stats")]
    public IActionResult GetStats([FromQuery] string range = "7d")
    {
        return Ok(new { range, totalLogins = 0, uniqueUsers = 0, failedAttempts = 0, byRole = new { }, byProvider = new { } });
    }

    [HttpGet("anomalies")]
    public IActionResult GetAnomalies() => Ok(new { total = 0, alerts = Array.Empty<AnomalyAlert>() });

    private string GetClientIp()
    {
        return Request.Headers["CF-Connecting-IP"].FirstOrDefault()
            ?? Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
            ?? HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
