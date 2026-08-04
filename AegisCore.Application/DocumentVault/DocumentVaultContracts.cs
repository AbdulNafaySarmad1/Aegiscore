using AegisCore.Domain.Entities;

namespace AegisCore.Application.DocumentVault;

public interface IDocumentVaultService
{
    Task<DocumentVaultEntry> UploadDocumentAsync(Stream content, string fileName, string contentType, string category, Guid uploadedByUserId, string uploadedByRole, CancellationToken ct);
    Task<Stream?> DownloadDocumentAsync(Guid documentId, Guid requestedByUserId, string requestedByRole, CancellationToken ct);
    Task<bool> VerifyDocumentAsync(Guid documentId, Guid verifiedByUserId, CancellationToken ct);
    Task<DocumentTier> GetTierForCategoryAsync(string category, CancellationToken ct);
    Task<List<DocumentVaultEntry>> GetDocumentsByTierAsync(int tierLevel, CancellationToken ct);
    Task<List<DocumentVaultEntry>> GetDocumentsByCategoryAsync(string category, CancellationToken ct);
    Task<DocumentVaultEntry?> GetDocumentAsync(Guid documentId, CancellationToken ct);
    Task<List<DocumentTier>> GetTiersAsync(CancellationToken ct);
    Task<DocumentTier> UpdateTierAsync(DocumentTier tier, CancellationToken ct);
    Task SeedDefaultTiersAsync(Guid tenantId, CancellationToken ct);
}

public class DocumentVaultDashboardDto
{
    public long TotalDocuments { get; set; }
    public long TotalSizeBytes { get; set; }
    public int PqcVerifiedCount { get; set; }
    public int LockedCount { get; set; }
    public List<TierSummaryDto> Tiers { get; set; } = new();
    public List<DocumentVaultEntryDto> RecentUploads { get; set; } = new();
}

public class TierSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public int Level { get; set; }
    public int RetentionYears { get; set; }
    public int DocumentCount { get; set; }
    public long TotalSizeBytes { get; set; }
}

public class DocumentVaultEntryDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = "";
    public string Category { get; set; } = "";
    public int TierLevel { get; set; }
    public string TierName { get; set; } = "";
    public long FileSizeBytes { get; set; }
    public bool IsPqcVerified { get; set; }
    public bool IsRetentionLocked { get; set; }
    public DateTime UploadedAt { get; set; }
    public DateTime RetentionExpiresAt { get; set; }
    public int YearsRemaining { get; set; }
}
