using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AegisCore.Application.Licensing;
using AegisCore.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AegisCore.API.Controllers;

[ApiController]
public sealed class AegisCoreLicenseController(
    ILicenseService licenses,
    AegisDbContext db,
    IConfiguration configuration) : ControllerBase
{
    [HttpGet("/api/license/status")]
    public Task<LicenseRuntimeStatus> Status(CancellationToken ct) => licenses.GetStatusAsync(ct);

    [AegisCore.API.Authorization.RequirePermission("System", "Approve")]
    [HttpPost("/api/admin/license/activate")]
    public async Task<IActionResult> Activate([FromBody] ActivateLicenseRequest request, CancellationToken ct)
    {
        try
        {
            await licenses.ActivateAsync(request.LicenseKey, request.UniversityId, ct);
        }
        catch (InvalidOperationException ex)
        {
            // The validation message names what is wrong with the key (signature, expiry, university).
            return UnprocessableEntity(new { error = ex.Message, code = "license_rejected" });
        }
        // A heartbeat straight away, so the status page shows the new key's standing rather than the old key's failures.
        var heartbeat = await licenses.SendHeartbeatAsync(ct);
        var status = await licenses.GetStatusAsync(ct);
        return Ok(new { activated = true, heartbeat = heartbeat.Status, message = heartbeat.Message, status });
    }

    [AegisCore.API.Authorization.RequirePermission("System", "Approve")]
    [HttpPost("/api/admin/license/override")]
    public async Task<IActionResult> Override([FromBody] LicenseOverrideRequest request, CancellationToken ct)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(actor, out var actorId))
        {
            return Forbid();
        }

        await licenses.ManualOverrideAsync(actorId, request.Reason, request.ValidUntil, ct);
        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("/api/license/heartbeat")]
    public async Task<IActionResult> Heartbeat([FromBody] HeartbeatRequest request, CancellationToken ct)
    {
        var configuredToken = configuration["License:HeartbeatServerToken"];
        var suppliedToken = Request.Headers["X-AegisCore-License-Token"].ToString();
        if (string.IsNullOrWhiteSpace(configuredToken)
            || string.IsNullOrWhiteSpace(suppliedToken)
            || !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(configuredToken),
                Encoding.UTF8.GetBytes(suppliedToken)))
        {
            return Unauthorized();
        }

        if (request.Timestamp < DateTimeOffset.UtcNow.AddMinutes(-5) || request.Timestamp > DateTimeOffset.UtcNow.AddMinutes(5))
        {
            return Ok(new { status = "invalid", message = "Heartbeat timestamp is outside the accepted window." });
        }

        var state = await db.LicenseStates.SingleOrDefaultAsync(l => l.UniversityId == request.UniversityId, ct);
        if (state is null)
        {
            return Ok(new { status = "invalid", message = "License not found", timestamp = DateTimeOffset.UtcNow });
        }

        if (state.Status is "revoked" or "expired")
        {
            return Ok(new { status = state.Status, message = $"License {state.Status}", timestamp = DateTimeOffset.UtcNow });
        }

        return Ok(new { status = "valid", message = "OK", timestamp = DateTimeOffset.UtcNow });
    }
}

public sealed record ActivateLicenseRequest(string LicenseKey, Guid UniversityId);
public sealed record LicenseOverrideRequest(string Reason, DateTimeOffset ValidUntil);
public sealed record HeartbeatRequest(Guid UniversityId, string UniversityName, int CurrentUserCount, IReadOnlyList<string> ActiveModules, DateTimeOffset Timestamp);
