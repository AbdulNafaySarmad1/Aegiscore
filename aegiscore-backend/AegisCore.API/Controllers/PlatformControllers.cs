using AegisCore.Application.Platform;
using AegisCore.Application.Common.Interfaces;
using AegisCore.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace AegisCore.API.Controllers;

[ApiController] [Route("api/notifications")] [Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationPlatformService _svc;
    public NotificationController(INotificationPlatformService s) => _svc = s;

    [HttpPost("send")] public async Task<IActionResult> Send([FromBody] SendNotificationRequest r, CancellationToken ct) { await _svc.SendAsync(r, ct); return Ok(new { message = "Notification queued." }); }
    [HttpPost("templates")] [Authorize(Roles = "Admin,SuperAdmin")] public async Task<IActionResult> CreateTemplate([FromBody] NotificationTemplateDto r, CancellationToken ct) { var t = await _svc.CreateTemplateAsync(r, ct); return Ok(t); }
    [HttpGet("deliveries")] public async Task<IActionResult> GetDeliveries(CancellationToken ct) { var d = await _svc.GetDeliveryStatusAsync(null, ct); return Ok(new { total = d.Count, deliveries = d }); }
}

[ApiController] [Route("api/feature-flags")] [Authorize(Roles = "Admin,SuperAdmin")]
public class FeatureFlagController : ControllerBase
{
    private readonly IFeatureFlagService _svc;
    public FeatureFlagController(IFeatureFlagService s) => _svc = s;

    [HttpPost] public async Task<IActionResult> SetFlag([FromQuery] string name, [FromQuery] bool enabled, [FromQuery] string? description, CancellationToken ct) { var f = await _svc.SetFlagAsync(name, enabled, description, ct); return Ok(f); }
    [HttpPost("override")] public async Task<IActionResult> TenantOverride([FromQuery] string name, [FromQuery] Guid tenantId, [FromQuery] bool enabled, CancellationToken ct) { var f = await _svc.SetTenantOverrideAsync(name, tenantId, enabled, ct); return Ok(f); }
    [HttpGet("check/{name}")] public async Task<IActionResult> Check([FromRoute] string name, [FromQuery] Guid? tenantId, CancellationToken ct) { var enabled = await _svc.IsEnabledAsync(name, tenantId, ct); return Ok(new { name, enabled }); }
    [HttpGet] public async Task<IActionResult> List(CancellationToken ct) { var flags = await _svc.GetAllFlagsAsync(ct); return Ok(new { total = flags.Count, flags }); }
}

[ApiController] [Route("api/scheduler")]
public class SchedulerController : ControllerBase
{
    private readonly ISchedulerService _svc;
    private readonly IScheduledJobRepository _jobRepo;
    private readonly IJobExecutionRepository _execRepo;
    public SchedulerController(ISchedulerService s, IScheduledJobRepository j, IJobExecutionRepository e) { _svc = s; _jobRepo = j; _execRepo = e; }

    [HttpPost("jobs")] public async Task<IActionResult> Register([FromBody] JobDto r, CancellationToken ct) { var job = new Domain.Entities.ScheduledJob { Id = Guid.NewGuid(), Name = r.Name, CronExpression = r.CronExpression, Category = JobCategory.ReportGeneration }; await _svc.RegisterJobAsync(job, ct); return Ok(new JobDto { Id = job.Id, Name = job.Name }); }
    [HttpGet("jobs")] public async Task<IActionResult> List(CancellationToken ct) { var jobs = await _jobRepo.GetEnabledAsync(ct); return Ok(new { total = jobs.Count, jobs = jobs.Select(j => new JobDto { Id = j.Id, Name = j.Name, CronExpression = j.CronExpression, IsEnabled = j.IsEnabled, LastRunAt = j.LastRunAt, NextRunAt = j.NextRunAt }) }); }
    [HttpPost("jobs/{jobId:guid}/trigger")] public async Task<IActionResult> Trigger(Guid jobId, CancellationToken ct) { var exec = await _svc.TriggerJobAsync(jobId, "Manual", ct); return Ok(new JobExecutionDto { Id = exec.Id, Status = exec.Status.ToString(), DurationMs = exec.DurationMs }); }
    [HttpGet("jobs/{jobId:guid}/history")] public async Task<IActionResult> History(Guid jobId, CancellationToken ct) { var h = await _svc.GetJobHistoryAsync(jobId, 50, ct); return Ok(new { total = h.Count, executions = h.Select(e => new JobExecutionDto { Id = e.Id, Status = e.Status.ToString(), StartedAt = e.StartedAt, DurationMs = e.DurationMs, ErrorMessage = e.ErrorMessage }) }); }
    [HttpPost("executions/{execId:guid}/retry")] public async Task<IActionResult> Retry(Guid execId, CancellationToken ct) { await _svc.RetryFailedJobAsync(execId, ct); return Ok(new { message = "Job queued for retry." }); }
    [HttpGet("failures")] public async Task<IActionResult> Failures(CancellationToken ct) { var f = await _execRepo.GetRecentFailuresAsync(20, ct); return Ok(new { total = f.Count, failures = f }); }
}

[ApiController] [Route("api/import")]
public class ImportController : ControllerBase
{
    private readonly IImportService _svc;
    private readonly IImportBatchRepository _repo;
    public ImportController(IImportService s, IImportBatchRepository r) { _svc = s; _repo = r; }

    [HttpPost] public async Task<IActionResult> Import([FromQuery] string domain, [FromQuery] string fileName, CancellationToken ct) { var batch = await _svc.ImportAsync(Stream.Null, fileName, domain, new(), Guid.Empty, ct); return Ok(new ImportBatchDto { Id = batch.Id, FileName = batch.FileName, Domain = batch.Domain, Status = batch.Status }); }
    [HttpGet] public async Task<IActionResult> History([FromQuery] string? domain, CancellationToken ct) { var h = await _svc.GetHistoryAsync(domain, ct); return Ok(new { total = h.Count, batches = h.Select(b => new ImportBatchDto { Id = b.Id, FileName = b.FileName, Domain = b.Domain, TotalRows = b.TotalRows, SuccessCount = b.SuccessCount, ErrorCount = b.ErrorCount, Status = b.Status }) }); }
    [HttpPost("{batchId:guid}/rollback")] public async Task<IActionResult> Rollback(Guid batchId, CancellationToken ct) { await _svc.RollbackAsync(batchId, ct); return Ok(new { message = "Rollback completed." }); }
}

[ApiController] [Route("api/export")]
public class ExportController : ControllerBase
{
    private readonly IExportService _svc;
    public ExportController(IExportService s) => _svc = s;

    [HttpPost] public async Task<IActionResult> Export([FromQuery] string domain, [FromQuery] string format, CancellationToken ct) { var log = await _svc.ExportAsync(domain, format, new(), new(), Guid.Empty, null, ct); return Ok(new ExportLogDto { Id = log.Id, Domain = log.Domain, Format = log.Format, CreatedAt = log.CreatedAt }); }
    [HttpGet("history")] public async Task<IActionResult> History([FromQuery] Guid? userId, CancellationToken ct) { var h = await _svc.GetExportHistoryAsync(userId, ct); return Ok(new { total = h.Count, exports = h.Select(l => new ExportLogDto { Id = l.Id, Domain = l.Domain, Format = l.Format, RowCount = l.RowCount, CreatedAt = l.CreatedAt }) }); }
}

[ApiController] [Route("api/command-center")]
public class CommandCenterController : ControllerBase
{
    private readonly ICommandCenterService _svc;
    public CommandCenterController(ICommandCenterService s) => _svc = s;

    [HttpGet("health")] public async Task<IActionResult> Health(CancellationToken ct) { var h = await _svc.GetHealthAsync(ct); return Ok(h); }
    [HttpPost("metrics")] public async Task<IActionResult> Record([FromQuery] string name, [FromQuery] string category, [FromQuery] decimal value, [FromQuery] string? unit, CancellationToken ct) { await _svc.RecordMetricAsync(name, category, value, unit, ct); return Ok(new { message = "Metric recorded." }); }
}
