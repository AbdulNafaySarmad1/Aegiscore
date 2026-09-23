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
public sealed class PrivacyComplianceController(
    AegisDbContext db,
    IPrivacyComplianceService privacy,
    IComplianceSettingsService? settings = null) : ControllerBase
{
    /// <summary>
    /// The compliance decisions this installation owns: who its data protection contact is, where
    /// its data lives, and how long each category is kept. Readable by anyone who can open the
    /// compliance screen; changing them needs Settings.Write. The product ships starting values,
    /// never answers — a supplier cannot name a university's data protection officer or decide how
    /// long it keeps disciplinary records.
    /// </summary>
    [HttpGet("admin/compliance-settings")]
    [AegisCore.API.Authorization.RequirePermission("Settings", "Read")]
    public async Task<IActionResult> GetComplianceSettings(CancellationToken cancellationToken = default)
    {
        if (settings is null) return NotFound();
        var tenantId = db.CurrentTenantId;
        if (tenantId == Guid.Empty) return BadRequest(new { error = "Tenant context is required." });
        var current = await settings.GetAsync(tenantId, cancellationToken);
        return Ok(new { settings = current, gaps = settings.Gaps(current) });
    }

    [HttpPut("admin/compliance-settings")]
    [AegisCore.API.Authorization.RequirePermission("Settings", "Write")]
    public async Task<IActionResult> SaveComplianceSettings([FromBody] ComplianceSettingsUpdate update, CancellationToken cancellationToken = default)
    {
        if (settings is null) return NotFound();
        var tenantId = db.CurrentTenantId;
        if (tenantId == Guid.Empty) return BadRequest(new { error = "Tenant context is required." });
        var actorId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsed) ? parsed : Guid.Empty;
        var saved = await settings.SaveAsync(tenantId, update ?? new ComplianceSettingsUpdate(), actorId, cancellationToken);
        return Ok(new { settings = saved, gaps = settings.Gaps(saved) });
    }

    /// <summary>
    /// How many people's records this installation holds, by category — no names, no identifiers.
    /// A breach assessment has 24 hours to answer "whose data, how many" (docs/compliance/11 §4) and
    /// the first tabletop exercise found that question needed a hand-written query at the worst
    /// possible moment (finding F-2). Counts are live, not cached.
    /// </summary>
    [HttpGet("admin/data-subject-counts")]
    [AegisCore.API.Authorization.RequirePermission("System", "Admin")]
    public async Task<IActionResult> DataSubjectCounts(CancellationToken cancellationToken = default)
    {
        var erasedPrefix = "ERASED_USER_";
        var categories = new[]
        {
            new { category = "Students on the register", count = await db.Students.CountAsync(cancellationToken) },
            new { category = "Students already anonymised by erasure", count = await db.Students.CountAsync(s => s.FirstName.StartsWith(erasedPrefix), cancellationToken) },
            new { category = "Guardians", count = await db.StudentGuardians.CountAsync(cancellationToken) },
            new { category = "Applicants (admission applications)", count = await db.AdmissionApplications.CountAsync(cancellationToken) },
            new { category = "Applicants (portal profiles)", count = await db.ApplicantProfiles.CountAsync(cancellationToken) },
            new { category = "Employees", count = await db.Employees.CountAsync(cancellationToken) },
            new { category = "User accounts", count = await db.Users.CountAsync(cancellationToken) },
            new { category = "Foreign students (passport and visa held)", count = await db.ForeignStudentRegistries.CountAsync(cancellationToken) },
            new { category = "Students with counselling records", count = await db.StudentCounselings.Select(c => c.StudentId).Distinct().CountAsync(cancellationToken) },
            new { category = "Students with disciplinary records", count = await db.StudentDisciplines.Select(c => c.StudentId).Distinct().CountAsync(cancellationToken) },
            new { category = "Mental-health tickets", count = await db.MentalHealthTickets.CountAsync(cancellationToken) },
            new { category = "Visitors recorded", count = await db.VisitorLogs.CountAsync(cancellationToken) },
            new { category = "Consent records", count = await db.ConsentLedgers.CountAsync(cancellationToken) },
            new { category = "Erasure requests (all states)", count = await db.ErasureRequests.CountAsync(cancellationToken) },
        };

        return Ok(new
        {
            generatedAt = DateTime.UtcNow,
            purpose = "Breach assessment (docs/compliance/11-breach-notification.md §4). Counts only; no personal data.",
            categories,
            note = "Counts are scoped to the signed-in tenant. Categories overlap: a person may appear as a user, a student and an applicant.",
        });
    }

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
