using AegisCore.Application.Replication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.API.Controllers;

[ApiController][Route("api/admin/replication")]
[AegisCore.API.Authorization.RequirePermission("System", "Write")]
public class ReplicationController : ControllerBase
{
    private readonly IReplicaManagerService _svc;
    public ReplicationController(IReplicaManagerService svc) => _svc = svc;

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        var replicas = await _svc.GetActiveReplicasAsync(ct);
        var config = await _svc.GetScalingConfigAsync(ct);
        var alerts = await _svc.GetActiveAlertsAsync(ct);

        var dto = new ReplicationDashboardDto
        {
            PrimaryHealthy = true,
            AutoScalingEnabled = config.AutoScalingEnabled,
            ReadRoutingEnabled = config.ReadRoutingEnabled,
            OnlineReplicas = replicas.Count(r => r.Status == Domain.ReplicaStatus.Online),
            TotalReplicas = replicas.Count,
            MaxReplicas = config.MaxReplicas,
            MaxLagSeconds = config.DefaultMaxLagSeconds,
            CriticalLagSeconds = config.CriticalLagSeconds,
            ActiveAlertsCount = alerts.Count,
            Replicas = replicas.Select(r => new ReplicaHealthDto
            {
                Id = r.Id, Name = r.Name, Status = r.Status.ToString(),
                Priority = r.Priority, Weight = r.Weight,
                CurrentLagSeconds = r.CurrentLagSeconds, MaxLagSeconds = r.MaxLagSeconds,
                IsLagAcceptable = r.CurrentLagSeconds <= r.MaxLagSeconds,
                LastHealthCheckAt = r.LastHealthCheckAt
            }).ToList()
        };
        return Ok(dto);
    }

    [HttpPost("replicas")]
    public async Task<IActionResult> AddReplica([FromQuery] string name, [FromQuery] string connectionString, [FromQuery] int priority, [FromQuery] int weight, CancellationToken ct)
        => Ok(await _svc.RegisterReplicaAsync(name, connectionString, priority, weight, ct));

    [HttpPost("replicas/{id:guid}/test")]
    public async Task<IActionResult> TestReplica(Guid id, CancellationToken ct)
        => Ok(new { replicaId = id, healthy = await _svc.TestReplicaConnectionAsync(id, ct) });

    [HttpPost("replicas/{id:guid}/check-lag")]
    public async Task<IActionResult> CheckLag(Guid id, CancellationToken ct)
        => Ok(new { replicaId = id, lagSeconds = await _svc.CheckReplicaLagAsync(id, ct) });

    [HttpPost("health-check-all")]
    public async Task<IActionResult> HealthCheckAll(CancellationToken ct)
    { await _svc.CheckAllReplicaHealthAsync(ct); return Ok(new { message = "Health check complete." }); }

    [HttpPost("auto-scale")]
    public async Task<IActionResult> AutoScale(CancellationToken ct)
    { await _svc.AutoScaleAsync(ct); return Ok(new { message = "Auto-scale evaluation complete." }); }

    [HttpPost("replicas/{id:guid}/promote")]
    public async Task<IActionResult> Promote(Guid id, CancellationToken ct)
        => Ok(new { replicaId = id, promoted = await _svc.PromoteReplicaAsync(id, ct) });

    [HttpGet("route-read")]
    public async Task<IActionResult> RouteRead(CancellationToken ct)
    {
        var conn = await _svc.RouteReadConnectionAsync(ct);
        return Ok(new { routed = conn != null, maskedConnection = conn != null ? conn.Split(';').Select(p => p.TrimStart().StartsWith("Password=") || p.TrimStart().StartsWith("PWD=") ? "Password=***" : p).Aggregate((a,b) => a + ";" + b) : "primary" });
    }

    [HttpGet("config")]
    public async Task<IActionResult> GetConfig(CancellationToken ct) => Ok(await _svc.GetScalingConfigAsync(ct));

    [HttpPut("config")]
    public async Task<IActionResult> UpdateConfig([FromBody] Domain.Entities.ReplicationScalingConfig config, CancellationToken ct) => Ok(await _svc.UpdateScalingConfigAsync(config, ct));

    [HttpGet("scale-history")]
    public async Task<IActionResult> ScaleHistory([FromQuery] int limit = 20, CancellationToken ct = default) => Ok(await _svc.GetScaleHistoryAsync(limit, ct));

    [HttpGet("alerts")]
    public async Task<IActionResult> Alerts(CancellationToken ct) { var a = await _svc.GetActiveAlertsAsync(ct); return Ok(new { total = a.Count, alerts = a }); }
}
