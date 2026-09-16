using System.ComponentModel.DataAnnotations;
using AegisCore.API.Authorization;
using AegisCore.Application.Governance;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.API.Controllers;

/// <summary>
/// Data entry for accreditation readiness: the frameworks an institution is assessed against,
/// their weighted criteria, and the evidence recorded per criterion. This is what the readiness
/// meter scores; without these rows there is nothing to score and no number is shown.
/// </summary>
[ApiController]
[Route("api/readiness/frameworks")]
[Authorize]
public sealed class ReadinessFrameworkController(AegisDbContext db) : ControllerBase
{
    public sealed record FrameworkRequest(
        [Required, MaxLength(50)] string FrameworkCode,
        [Required, MaxLength(200)] string FrameworkName,
        [MaxLength(50)] string? AccreditationBody,
        DateTimeOffset? LastAuditDate,
        DateTimeOffset? NextAuditDate);

    public sealed record CriterionRequest(
        [Required, MaxLength(20)] string CriterionCode,
        [MaxLength(100)] string? SectionName,
        [MaxLength(500)] string? Description,
        [Range(0.01, 9.99)] decimal Weight,
        [MaxLength(200)] string? EvidenceSource);

    public sealed record EvidenceRequest(
        [Required, MaxLength(300)] string EvidenceTitle,
        [MaxLength(500)] string? EvidenceUrl,
        [MaxLength(2000)] string? EvidenceDescription,
        DateTimeOffset? EvidenceDate,
        DateTimeOffset? ExpiryDate);

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var frameworks = await db.AccreditationFrameworks.AsNoTracking().OrderBy(f => f.FrameworkName).ToListAsync(ct);
        var ids = frameworks.Select(f => f.Id).ToArray();
        var criteria = await db.Set<ReadinessCriterion>().AsNoTracking().Where(c => ids.Contains(c.FrameworkId)).ToListAsync(ct);
        var criterionIds = criteria.Select(c => c.Id).ToArray();
        var evidence = await db.CriterionEvidences.AsNoTracking().Where(e => criterionIds.Contains(e.CriterionId)).ToListAsync(ct);
        var latestScores = new Dictionary<Guid, ReadinessScore>();
        foreach (var id in ids)
        {
            var latest = await db.ReadinessScores.AsNoTracking().Where(s => s.FrameworkId == id)
                .OrderByDescending(s => s.GeneratedAt).ThenByDescending(s => s.Id).FirstOrDefaultAsync(ct);
            if (latest is not null) latestScores[id] = latest;
        }

        return Ok(new
        {
            total = frameworks.Count,
            frameworks = frameworks.Select(f => new
            {
                f.Id, f.FrameworkCode, f.FrameworkName, f.AccreditationBody, f.LastAuditDate, f.NextAuditDate,
                criteriaCount = criteria.Count(c => c.FrameworkId == f.Id),
                latestScore = latestScores.TryGetValue(f.Id, out var s) ? new { s.Id, s.OverallScore, status = s.Status.ToString(), s.GeneratedAt } : null,
                criteria = criteria.Where(c => c.FrameworkId == f.Id).OrderBy(c => c.SectionName).ThenBy(c => c.CriterionCode).Select(c => new
                {
                    c.Id, c.CriterionCode, c.SectionName, c.Description, c.Weight, c.EvidenceSource,
                    c.ComplianceStatus, c.Score,
                    evidence = evidence.Where(e => e.CriterionId == c.Id).OrderByDescending(e => e.EvidenceDate).Select(e => new
                    {
                        e.Id, e.EvidenceTitle, e.EvidenceUrl, e.EvidenceDescription, e.EvidenceDate, e.ExpiryDate, e.IsStale,
                        isCurrent = ReadinessScoring.IsCurrent(e, now),
                    }),
                }),
            }),
            asOf = now,
        });
    }

    [HttpPost]
    [RequirePermission("Reports", "Write")]
    public async Task<IActionResult> Create([FromBody] FrameworkRequest request, CancellationToken ct)
    {
        var tenantId = db.CurrentTenantId;
        if (tenantId == Guid.Empty) return Forbid();
        var code = request.FrameworkCode.Trim();
        if (await db.AccreditationFrameworks.AnyAsync(f => f.FrameworkCode == code, ct))
            return Conflict(new { message = $"A framework with code '{code}' already exists." });
        var framework = new AccreditationFramework
        {
            Id = Guid.NewGuid(), TenantId = tenantId, FrameworkCode = code, FrameworkName = request.FrameworkName.Trim(),
            AccreditationBody = request.AccreditationBody?.Trim(), LastAuditDate = request.LastAuditDate, NextAuditDate = request.NextAuditDate,
        };
        db.AccreditationFrameworks.Add(framework);
        await db.SaveChangesAsync(ct);
        return Created($"/api/readiness/frameworks/{framework.Id}", new { framework.Id, framework.FrameworkCode, framework.FrameworkName, framework.AccreditationBody, framework.NextAuditDate });
    }

    [HttpPut("{frameworkId:guid}")]
    [RequirePermission("Reports", "Write")]
    public async Task<IActionResult> Update(Guid frameworkId, [FromBody] FrameworkRequest request, CancellationToken ct)
    {
        var framework = await db.AccreditationFrameworks.SingleOrDefaultAsync(f => f.Id == frameworkId, ct);
        if (framework is null) return NotFound();
        framework.FrameworkCode = request.FrameworkCode.Trim();
        framework.FrameworkName = request.FrameworkName.Trim();
        framework.AccreditationBody = request.AccreditationBody?.Trim();
        framework.LastAuditDate = request.LastAuditDate;
        framework.NextAuditDate = request.NextAuditDate;
        await db.SaveChangesAsync(ct);
        return Ok(new { framework.Id, framework.FrameworkCode, framework.FrameworkName, framework.AccreditationBody, framework.NextAuditDate });
    }

    [HttpPost("{frameworkId:guid}/criteria")]
    [RequirePermission("Reports", "Write")]
    public async Task<IActionResult> AddCriterion(Guid frameworkId, [FromBody] CriterionRequest request, CancellationToken ct)
    {
        var framework = await db.AccreditationFrameworks.AsNoTracking().SingleOrDefaultAsync(f => f.Id == frameworkId, ct);
        if (framework is null) return NotFound();
        var code = request.CriterionCode.Trim();
        if (await db.Set<ReadinessCriterion>().AnyAsync(c => c.FrameworkId == frameworkId && c.CriterionCode == code, ct))
            return Conflict(new { message = $"Criterion '{code}' already exists on this framework." });
        var criterion = new ReadinessCriterion
        {
            Id = Guid.NewGuid(), TenantId = framework.TenantId, FrameworkId = frameworkId, CriterionCode = code,
            SectionName = request.SectionName?.Trim(), Description = request.Description?.Trim(), Weight = request.Weight,
            EvidenceSource = request.EvidenceSource?.Trim(), ComplianceStatus = ReadinessScoring.NotMet, Score = 0m,
        };
        db.Set<ReadinessCriterion>().Add(criterion);
        await db.SaveChangesAsync(ct);
        return Created($"/api/readiness/frameworks/{frameworkId}", new { criterion.Id, criterion.CriterionCode, criterion.SectionName, criterion.Description, criterion.Weight, criterion.EvidenceSource });
    }

    [HttpDelete("{frameworkId:guid}/criteria/{criterionId:guid}")]
    [RequirePermission("Reports", "Write")]
    public async Task<IActionResult> RemoveCriterion(Guid frameworkId, Guid criterionId, CancellationToken ct)
    {
        var criterion = await db.Set<ReadinessCriterion>().SingleOrDefaultAsync(c => c.Id == criterionId && c.FrameworkId == frameworkId, ct);
        if (criterion is null) return NotFound();
        var evidence = await db.CriterionEvidences.Where(e => e.CriterionId == criterionId).ToListAsync(ct);
        db.CriterionEvidences.RemoveRange(evidence);
        db.Set<ReadinessCriterion>().Remove(criterion);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{frameworkId:guid}/criteria/{criterionId:guid}/evidence")]
    [RequirePermission("Reports", "Write")]
    public async Task<IActionResult> AddEvidence(Guid frameworkId, Guid criterionId, [FromBody] EvidenceRequest request, CancellationToken ct)
    {
        var criterion = await db.Set<ReadinessCriterion>().AsNoTracking().SingleOrDefaultAsync(c => c.Id == criterionId && c.FrameworkId == frameworkId, ct);
        if (criterion is null) return NotFound();
        var evidenceDate = request.EvidenceDate ?? DateTimeOffset.UtcNow;
        if (request.ExpiryDate is { } expiry && expiry <= evidenceDate)
            return BadRequest(new { message = "Expiry must be after the evidence date." });
        var evidence = new CriterionEvidence
        {
            Id = Guid.NewGuid(), TenantId = criterion.TenantId, CriterionId = criterionId, EvidenceTitle = request.EvidenceTitle.Trim(),
            EvidenceUrl = request.EvidenceUrl?.Trim(), EvidenceDescription = request.EvidenceDescription?.Trim(),
            EvidenceDate = evidenceDate, ExpiryDate = request.ExpiryDate, IsStale = request.ExpiryDate is { } x && x <= DateTimeOffset.UtcNow,
        };
        db.CriterionEvidences.Add(evidence);
        await db.SaveChangesAsync(ct);
        return Created($"/api/readiness/frameworks/{frameworkId}", new { evidence.Id, evidence.EvidenceTitle, evidence.EvidenceDate, evidence.ExpiryDate, evidence.IsStale });
    }

    [HttpDelete("{frameworkId:guid}/criteria/{criterionId:guid}/evidence/{evidenceId:guid}")]
    [RequirePermission("Reports", "Write")]
    public async Task<IActionResult> RemoveEvidence(Guid frameworkId, Guid criterionId, Guid evidenceId, CancellationToken ct)
    {
        var owned = await db.Set<ReadinessCriterion>().AnyAsync(c => c.Id == criterionId && c.FrameworkId == frameworkId, ct);
        var evidence = owned ? await db.CriterionEvidences.SingleOrDefaultAsync(e => e.Id == evidenceId && e.CriterionId == criterionId, ct) : null;
        if (evidence is null) return NotFound();
        db.CriterionEvidences.Remove(evidence);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
