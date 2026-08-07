using AegisCore.Application.DocumentVault;
using AegisCore.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.API.Controllers;

[ApiController][Route("api/document-vault")]
[Authorize]
public class DocumentVaultController : ControllerBase
{
    private readonly IDocumentVaultService _svc;
    public DocumentVaultController(IDocumentVaultService svc) => _svc = svc;

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        var tiers = await _svc.GetTiersAsync(ct);
        var dto = new DocumentVaultDashboardDto { Tiers = new(), RecentUploads = new() };

        foreach (var tier in tiers)
        {
            var docs = await _svc.GetDocumentsByTierAsync(tier.TierLevel, ct);
            dto.Tiers.Add(new TierSummaryDto { Id = tier.Id, Name = tier.Name, Level = tier.TierLevel, RetentionYears = tier.RetentionYears, DocumentCount = docs.Count, TotalSizeBytes = docs.Sum(d => d.FileSizeBytes) });
            dto.TotalDocuments += docs.Count;
            dto.TotalSizeBytes += docs.Sum(d => d.FileSizeBytes);
            dto.PqcVerifiedCount += docs.Count(d => d.IsPqcVerified);
            dto.LockedCount += docs.Count(d => d.IsRetentionLocked);
        }

        var recent = tiers.SelectMany(t => t.TierLevel switch { 1 => new List<DocumentVaultEntry>(), _ => new List<DocumentVaultEntry>() }).ToList();
        foreach (var tier in tiers)
        {
            var docs = await _svc.GetDocumentsByTierAsync(tier.TierLevel, ct);
            recent.AddRange(docs.Take(5));
        }
        dto.RecentUploads = recent.OrderByDescending(d => d.UploadedAt).Take(10).Select(d => new DocumentVaultEntryDto
        {
            Id = d.Id, FileName = d.FileName, Category = d.DocumentCategory, TierLevel = d.TierLevel,
            TierName = tiers.FirstOrDefault(t => t.TierLevel == d.TierLevel)?.Name ?? "",
            FileSizeBytes = d.FileSizeBytes, IsPqcVerified = d.IsPqcVerified, IsRetentionLocked = d.IsRetentionLocked,
            UploadedAt = d.UploadedAt, RetentionExpiresAt = d.RetentionExpiresAt,
            YearsRemaining = Math.Max(0, (int)(d.RetentionExpiresAt - DateTime.UtcNow).TotalDays / 365)
        }).ToList();

        return Ok(dto);
    }

    [RequestSizeLimit(50 * 1024 * 1024)]
    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromQuery] string fileName, [FromQuery] string contentType, [FromQuery] string category, [FromQuery] Guid uploadedByUserId, [FromQuery] string uploadedByRole, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        await Request.Body.CopyToAsync(ms, ct);
        ms.Position = 0;
        var entry = await _svc.UploadDocumentAsync(ms, fileName, contentType, category, uploadedByUserId, uploadedByRole, ct);
        return Ok(new { id = entry.Id, tier = entry.TierLevel, retentionYears = (int)(entry.RetentionExpiresAt - DateTime.UtcNow).TotalDays / 365, pqcVerified = entry.IsPqcVerified, sha512 = entry.Sha512Hash[..16] });
    }

    [HttpGet("download/{id:guid}")]
    public async Task<IActionResult> Download(Guid id, [FromQuery] Guid requestedByUserId, [FromQuery] string requestedByRole, CancellationToken ct)
    {
        var stream = await _svc.DownloadDocumentAsync(id, requestedByUserId, requestedByRole, ct);
        if (stream == null) return NotFound(new { error = "Document not found or PQC verification failed." });
        var doc = await _svc.GetDocumentAsync(id, ct);
        return File(stream, doc?.ContentType ?? "application/octet-stream", doc?.FileName ?? "document");
    }

    [HttpPost("verify/{id:guid}")]
    public async Task<IActionResult> Verify(Guid id, [FromQuery] Guid verifiedByUserId, CancellationToken ct)
        => Ok(new { documentId = id, verified = await _svc.VerifyDocumentAsync(id, verifiedByUserId, ct) });

    [HttpGet("tiers")]
    public async Task<IActionResult> Tiers(CancellationToken ct) => Ok(await _svc.GetTiersAsync(ct));

    [HttpPut("tiers")]
    public async Task<IActionResult> UpdateTier([FromBody] Domain.Entities.DocumentTier tier, CancellationToken ct) => Ok(await _svc.UpdateTierAsync(tier, ct));

    [HttpPost("seed-tiers")]
    public async Task<IActionResult> SeedTiers(CancellationToken ct)
    { await _svc.SeedDefaultTiersAsync(Guid.Empty, ct); return Ok(new { message = "Default tiers and retention policies seeded." }); }

    [HttpGet("documents/tier/{tierLevel:int}")]
    public async Task<IActionResult> GetByTier(int tierLevel, CancellationToken ct)
    { var docs = await _svc.GetDocumentsByTierAsync(tierLevel, ct); return Ok(new { total = docs.Count, documents = docs.Select(d => new { d.Id, d.FileName, d.DocumentCategory, d.TierLevel, d.IsPqcVerified, d.IsRetentionLocked, d.UploadedAt, d.RetentionExpiresAt }) }); }
}
