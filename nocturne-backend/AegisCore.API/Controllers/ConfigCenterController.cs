using AegisCore.Application.Common.Interfaces;
using AegisCore.Application.ConfigCenter;
using AegisCore.Domain;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.API.Controllers;

[ApiController]
[Route("api/config")]
public class ConfigCenterController : ControllerBase
{
    private readonly IConfigurationCenterService _configService;
    private readonly ILogger<ConfigCenterController> _logger;

    public ConfigCenterController(
        IConfigurationCenterService configService,
        ILogger<ConfigCenterController> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> SetConfig([FromBody] SetConfigRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ConfigurationDomain>(request.Domain, true, out var domain))
            return BadRequest(new { error = "invalid_domain" });

        var entry = await _configService.SetConfigAsync(
            request.Key, request.ValueJson, Guid.Empty, "Admin",
            domain, request.Description, cancellationToken);

        return Ok(new ConfigDto { Id = entry.Id, Key = entry.Key, Domain = entry.Domain.ToString(), CurrentVersion = entry.CurrentVersion, Status = entry.Status.ToString() });
    }

    [HttpGet("{key}")]
    public async Task<IActionResult> GetConfig(string key, CancellationToken cancellationToken)
    {
        var config = await _configService.GetConfigAsync(key, cancellationToken);
        return Ok(config);
    }

    [HttpGet("domain/{domain}")]
    public async Task<IActionResult> GetDomainConfigs(string domain, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ConfigurationDomain>(domain, true, out var d))
            return BadRequest(new { error = "invalid_domain" });

        var configs = await _configService.GetDomainConfigsAsync(d, cancellationToken);
        return Ok(new { domain, total = configs.Count, configs });
    }

    [HttpGet("{entryId:guid}/versions")]
    public async Task<IActionResult> GetVersionHistory(Guid entryId, CancellationToken cancellationToken)
    {
        var versions = await _configService.GetVersionHistoryAsync(entryId, cancellationToken);
        return Ok(new { total = versions.Count, versions });
    }

    [HttpGet("{entryId:guid}/diff")]
    public async Task<IActionResult> GetDiff(Guid entryId, [FromQuery] int vA, [FromQuery] int vB, CancellationToken cancellationToken)
    {
        var diff = await _configService.GetDiffAsync(entryId, vA, vB, cancellationToken);
        return Ok(diff);
    }

    [HttpPost("{entryId:guid}/rollback")]
    public async Task<IActionResult> Rollback(Guid entryId, [FromBody] RollbackRequest request, CancellationToken cancellationToken)
    {
        var entry = await _configService.RollbackAsync(entryId, request.TargetVersion, Guid.Empty, "Admin", cancellationToken);
        return Ok(new ConfigDto { Id = entry.Id, Key = entry.Key, CurrentVersion = entry.CurrentVersion, Status = entry.Status.ToString() });
    }

    [HttpGet("audit-log")]
    public async Task<IActionResult> GetAuditLog(
        [FromQuery] Guid? entryId, [FromQuery] Guid? userId,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var logs = await _configService.GetAuditLogAsync(entryId, userId, from, to, cancellationToken);
        return Ok(new { total = logs.Count, logs });
    }
}
