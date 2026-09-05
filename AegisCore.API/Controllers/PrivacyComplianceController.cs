using System.Security.Claims;
using AegisCore.Application.Compliance;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.API.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public sealed class PrivacyComplianceController(AegisDbContext db, IPrivacyComplianceService privacy) : ControllerBase
{
    [HttpGet("admin/ferpa-audit")]
    [AegisCore.API.Authorization.RequirePermission("System", "Admin")]
    public async Task<IActionResult> FerpaAudit(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] Guid? userId,
        [FromQuery] Guid? studentId,
        [FromQuery] string? resourceType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (dateFrom is not null && dateTo is not null && dateFrom > dateTo)
            return BadRequest(new { error = "dateFrom must not be after dateTo." });
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = db.FerpaAuditLogs.AsNoTracking();
        if (dateFrom is not null) query = query.Where(item => item.AccessedAt >= dateFrom.Value);
        if (dateTo is not null) query = query.Where(item => item.AccessedAt <= dateTo.Value);
        if (userId is not null) query = query.Where(item => item.UserId == userId.Value);
        if (studentId is not null) query = query.Where(item => item.StudentId == studentId.Value);
        if (!string.IsNullOrWhiteSpace(resourceType))
            query = query.Where(item => item.ResourceType == resourceType);

        var total = await query.CountAsync(cancellationToken);
        var entries = await query.OrderByDescending(item => item.AccessedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new
            {
                item.Id,
                item.UserId,
                item.UserRole,
                item.ResourceType,
                item.ResourceId,
                item.StudentId,
                item.Action,
                item.AccessedAt,
                item.Justified,
                item.UserErased,
                item.StudentErased
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            page,
            pageSize,
            total,
            entries = entries.Select(item => new
            {
                item.Id,
                userId = item.UserErased ? null : (Guid?)item.UserId,
                user = item.UserErased ? "ERASED" : item.UserId.ToString(),
                item.UserRole,
                item.ResourceType,
                item.ResourceId,
                studentId = item.StudentErased ? null : (Guid?)item.StudentId,
                student = item.StudentErased ? "ERASED" : item.StudentId.ToString(),
                item.Action,
                item.AccessedAt,
                item.Justified,
                summary = $"{(item.UserErased ? "ERASED" : item.UserId)} ({item.UserRole}) {item.Action.ToLowerInvariant()} {item.ResourceType} record {item.ResourceId} for student {(item.StudentErased ? "ERASED" : item.StudentId)} at {item.AccessedAt:O}; justified: {item.Justified}."
            })
        });
    }

    [HttpPost("users/{id:guid}/consents")]
    public async Task<IActionResult> RecordConsent(Guid id, [FromBody] RecordConsentRequest request, CancellationToken cancellationToken)
    {
        if (!CanActFor(id)) return Forbid();
        try
        {
            var ledger = await privacy.RecordConsentAsync(
                id,
                request.DataCategory,
                request.ConsentText,
                request.ExpiresAt,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                cancellationToken);
            return Ok(new { ledger.Id, ledger.UserId, ledger.DataCategory, ledger.ConsentedAt, ledger.ExpiresAt, ledger.RevokedAt, ledger.ConsentText });
        }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpDelete("users/{id:guid}/consents/{consentId:guid}")]
    public async Task<IActionResult> RevokeConsent(Guid id, Guid consentId, CancellationToken cancellationToken)
    {
        if (!CanActFor(id)) return Forbid();
        try { await privacy.RevokeConsentAsync(id, consentId, cancellationToken); }
        catch (KeyNotFoundException) { return NotFound(); }
        return NoContent();
    }

    [HttpPost("users/{id:guid}/erasure-request")]
    public async Task<IActionResult> RequestErasure(Guid id, CancellationToken cancellationToken)
    {
        if (!CanActFor(id)) return Forbid();
        try
        {
            var request = await privacy.RequestErasureAsync(id, cancellationToken);
            return Accepted(new
            {
                request.Id,
                request.UserId,
                request.RequestedAt,
                request.DeadlineAt,
                status = request.Status.ToString()
            });
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpGet("users/{id:guid}/dsar-export")]
    public async Task<IActionResult> DsarExport(Guid id, CancellationToken cancellationToken)
    {
        if (!CanActFor(id)) return Forbid();
        try
        {
            var archive = await privacy.CreateDsarArchiveAsync(id, cancellationToken);
            Response.Headers["X-Download-Expires-At"] = archive.ExpiresAt.ToString("O");
            Response.Headers.CacheControl = "private, no-store, max-age=0";
            return File(archive.Content, "application/zip", $"dsar-{id:N}.zip");
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    private bool CanActFor(Guid userId)
    {
        var currentUserId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsed)
            ? parsed
            : Guid.Empty;
        return currentUserId == userId || User.IsInRole("Admin") || User.IsInRole("SuperAdmin");
    }
}

public sealed record RecordConsentRequest(string DataCategory, string ConsentText, DateTime? ExpiresAt);
