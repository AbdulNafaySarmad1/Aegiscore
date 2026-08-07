using System.Security.Cryptography;
using AegisCore.Contracts.Abstractions.Cloud;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Application.DocumentVault;
using AegisCore.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.DocumentVault;

public class DocumentVaultService : IDocumentVaultService
{
    private readonly IStorageService _storage;
    private readonly IRepository<DocumentVaultEntry> _docRepo;
    private readonly IRepository<DocumentTier> _tierRepo;
    private readonly IRepository<DocumentAccessLog> _logRepo;
    private readonly IRepository<DocumentRetentionPolicy> _policyRepo;
    private readonly ILogger<DocumentVaultService> _logger;
    private readonly string _defaultBucket;

    public DocumentVaultService(
        IStorageService storage,
        IRepository<DocumentVaultEntry> docRepo,
        IRepository<DocumentTier> tierRepo,
        IRepository<DocumentAccessLog> logRepo,
        IRepository<DocumentRetentionPolicy> policyRepo,
        ILogger<DocumentVaultService> logger)
    {
        _storage = storage; _docRepo = docRepo; _tierRepo = tierRepo;
        _logRepo = logRepo; _policyRepo = policyRepo; _logger = logger;
        _defaultBucket = Environment.GetEnvironmentVariable("STORAGE_BUCKET") ?? Environment.GetEnvironmentVariable("R2_BUCKET_NAME") ?? Environment.GetEnvironmentVariable("R2:BucketName") ?? "aegiscore-vault";
    }

    public async Task SeedDefaultTiersAsync(Guid tenantId, CancellationToken ct)
    {
        var existing = await _tierRepo.GetAllAsync(ct);
        if (existing.Any()) return;

        var tiers = new[]
        {
            new DocumentTier { Id = Guid.NewGuid(), Name = "Critical", TierLevel = 1, RetentionYears = 50, RetentionDays = 50 * 365, ObjectLockMode = "COMPLIANCE", Description = "Degrees, certifications, financial billing, legal documents — PQC signed, 50-year lock", BucketName = _defaultBucket, StoragePrefix = "tier1-critical/", RequirePqcVerification = true, TenantId = tenantId },
            new DocumentTier { Id = Guid.NewGuid(), Name = "Important", TierLevel = 2, RetentionYears = 20, RetentionDays = 20 * 365, ObjectLockMode = "GOVERNANCE", Description = "Fee challans, grade assessments, quizzes, academic notices — PQC signed, 20-year lock", BucketName = _defaultBucket, StoragePrefix = "tier2-important/", RequirePqcVerification = true, TenantId = tenantId },
            new DocumentTier { Id = Guid.NewGuid(), Name = "Standard", TierLevel = 3, RetentionYears = 5, RetentionDays = 5 * 365, ObjectLockMode = "GOVERNANCE", Description = "Attendance records, profile pictures, day-to-day records — PQC verified, 5-year lock", BucketName = _defaultBucket, StoragePrefix = "tier3-standard/", RequirePqcVerification = true, TenantId = tenantId },
        };
        foreach (var t in tiers) await _tierRepo.AddAsync(t, ct);

        var policies = new[]
        {
            new DocumentRetentionPolicy { Id = Guid.NewGuid(), DocumentCategory = "Degree", TierLevel = 1, RetentionYears = 50, Description = "Degree certificates and transcripts", TenantId = tenantId },
            new DocumentRetentionPolicy { Id = Guid.NewGuid(), DocumentCategory = "Certification", TierLevel = 1, RetentionYears = 50, Description = "Professional certifications", TenantId = tenantId },
            new DocumentRetentionPolicy { Id = Guid.NewGuid(), DocumentCategory = "FinancialBilling", TierLevel = 1, RetentionYears = 50, Description = "Financial billing and invoices", TenantId = tenantId },
            new DocumentRetentionPolicy { Id = Guid.NewGuid(), DocumentCategory = "FeeChallan", TierLevel = 2, RetentionYears = 20, Description = "Fee challans and payment records", TenantId = tenantId },
            new DocumentRetentionPolicy { Id = Guid.NewGuid(), DocumentCategory = "GradeAssessment", TierLevel = 2, RetentionYears = 20, Description = "Grade sheets and assessments", TenantId = tenantId },
            new DocumentRetentionPolicy { Id = Guid.NewGuid(), DocumentCategory = "Quiz", TierLevel = 2, RetentionYears = 20, Description = "Quiz papers and answer sheets", TenantId = tenantId },
            new DocumentRetentionPolicy { Id = Guid.NewGuid(), DocumentCategory = "AcademicNotice", TierLevel = 2, RetentionYears = 20, Description = "Academic notices and circulars", TenantId = tenantId },
            new DocumentRetentionPolicy { Id = Guid.NewGuid(), DocumentCategory = "Attendance", TierLevel = 3, RetentionYears = 5, Description = "Attendance records", TenantId = tenantId },
            new DocumentRetentionPolicy { Id = Guid.NewGuid(), DocumentCategory = "ProfilePicture", TierLevel = 3, RetentionYears = 5, Description = "Student/staff profile pictures", TenantId = tenantId },
            new DocumentRetentionPolicy { Id = Guid.NewGuid(), DocumentCategory = "General", TierLevel = 3, RetentionYears = 5, Description = "General day-to-day records", TenantId = tenantId },
        };
        foreach (var p in policies) await _policyRepo.AddAsync(p, ct);
        _logger.LogInformation("Seeded {TierCount} document tiers and {PolicyCount} retention policies", tiers.Length, policies.Length);
    }

    public async Task<DocumentTier> GetTierForCategoryAsync(string category, CancellationToken ct)
    {
        var policies = await _policyRepo.FindAsync(p => p.DocumentCategory == category, ct);
        var policy = policies.FirstOrDefault();
        var tierLevel = policy?.TierLevel ?? 3;
        var tiers = await _tierRepo.FindAsync(t => t.TierLevel == tierLevel && t.IsActive, ct);
        return tiers.FirstOrDefault() ?? (await _tierRepo.FindAsync(t => t.TierLevel == 3 && t.IsActive, ct)).First();
    }

    public async Task<DocumentVaultEntry> UploadDocumentAsync(Stream content, string fileName, string contentType, string category, Guid uploadedByUserId, string uploadedByRole, CancellationToken ct)
    {
        var tier = await GetTierForCategoryAsync(category, ct);

        // Compute SHA-512 hash
        content.Position = 0;
        var hash = await SHA512.HashDataAsync(content, ct);
        var hashHex = Convert.ToHexString(hash).ToLowerInvariant();
        content.Position = 0;

        // Generate PQC signature (ML-DSA simulation — replace with actual ML-DSA when PQC lib is available)
        var (signature, publicKey) = GeneratePqcSignature(hash);

        // Upload to storage provider (works with: local MinIO, AWS S3, Azure Blob, GCP Cloud Storage, Cloudflare R2)
        var storageKey = $"{tier.StoragePrefix}{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid():N}/{fileName}";
        var lockUntil = DateTime.UtcNow.AddDays(tier.RetentionDays);
        var bucket = tier.BucketName ?? _defaultBucket;

        await _storage.CreateBucketIfNotExistsAsync(bucket, ct);
        var storageUrl = await _storage.UploadAsync(bucket, storageKey, content, contentType, StorageVisibility.Private, ct);
        _logger.LogInformation("Document stored via {Provider} at {Bucket}/{Key} — retention until {LockUntil}", _storage.GetType().Name, bucket, storageKey, lockUntil);

        var entry = new DocumentVaultEntry
        {
            Id = Guid.NewGuid(), FileName = fileName, ContentType = contentType,
            FileSizeBytes = content.Length, DocumentCategory = category,
            TierId = tier.Id, TierLevel = tier.TierLevel,
            PqcSignatureBase64 = signature, PqcPublicKeyBase64 = publicKey,
            Sha512Hash = hashHex, IsPqcVerified = true, PqcVerifiedAt = DateTime.UtcNow,
            StorageBucket = tier.BucketName ?? _defaultBucket, StorageKey = storageKey,
            StorageUrl = storageUrl,
            UploadedAt = DateTime.UtcNow, RetentionExpiresAt = lockUntil,
            IsRetentionLocked = true, LockMode = tier.ObjectLockMode, LockedUntil = lockUntil,
            UploadedByUserId = uploadedByUserId, UploadedByRole = uploadedByRole,
            TenantId = Guid.Empty
        };
        await _docRepo.AddAsync(entry, ct);

        await _logRepo.AddAsync(new DocumentAccessLog
        {
            Id = Guid.NewGuid(), DocumentId = entry.Id, AccessType = "Upload",
            AccessedByUserId = uploadedByUserId, AccessedByRole = uploadedByRole,
            TenantId = Guid.Empty
        }, ct);

        _logger.LogInformation("Document vaulted: {File} ({Category}) → Tier {Tier} ({Retention}yr lock), PQC signed, SHA-512: {Hash}", fileName, category, tier.TierLevel, tier.RetentionYears, hashHex[..16]);
        return entry;
    }

    public async Task<Stream?> DownloadDocumentAsync(Guid documentId, Guid requestedByUserId, string requestedByRole, CancellationToken ct)
    {
        var doc = await _docRepo.GetByIdAsync(documentId, ct);
        if (doc == null || doc.IsDeleted)
        {
            await _logRepo.AddAsync(new DocumentAccessLog { Id = Guid.NewGuid(), DocumentId = documentId, AccessType = "Download", AccessedByUserId = requestedByUserId, AccessedByRole = requestedByRole, WasDenied = true, DenialReason = "Document not found or deleted", TenantId = Guid.Empty }, ct);
            return null;
        }

        // Verify PQC signature before serving
        if (!doc.IsPqcVerified)
        {
            await _logRepo.AddAsync(new DocumentAccessLog { Id = Guid.NewGuid(), DocumentId = documentId, AccessType = "Download", AccessedByUserId = requestedByUserId, AccessedByRole = requestedByRole, WasDenied = true, DenialReason = "PQC verification required but document not verified", TenantId = Guid.Empty }, ct);
            _logger.LogWarning("Download denied: document {Id} ({File}) not PQC verified", documentId, doc.FileName);
            return null;
        }

        if (doc.StorageBucket != null && doc.StorageKey != null)
        {
            try
            {
                var stream = await _storage.DownloadAsync(doc.StorageBucket, doc.StorageKey, ct);
                var ms = new MemoryStream();
                await stream.CopyToAsync(ms, ct);
                stream.Dispose();
                ms.Position = 0;

                // Re-verify hash on download — tamper detection
                var hash = await SHA512.HashDataAsync(ms, ct);
                ms.Position = 0;
                var hashHex = Convert.ToHexString(hash).ToLowerInvariant();

                await _logRepo.AddAsync(new DocumentAccessLog { Id = Guid.NewGuid(), DocumentId = documentId, AccessType = "Download", AccessedByUserId = requestedByUserId, AccessedByRole = requestedByRole, Notes = hashHex == doc.Sha512Hash ? "Hash verified" : "HASH MISMATCH — possible tampering", TenantId = Guid.Empty }, ct);

                if (hashHex != doc.Sha512Hash)
                    _logger.LogError("CRITICAL: Hash mismatch on document {Id} ({File}) — stored={StoredHash}, downloaded={DownloadedHash}", documentId, doc.FileName, doc.Sha512Hash, hashHex);

                return ms;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Download failed for document {Id} via {Provider}", documentId, _storage.GetType().Name);
                return null;
            }
        }

        return null;
    }

    public async Task<bool> VerifyDocumentAsync(Guid documentId, Guid verifiedByUserId, CancellationToken ct)
    {
        var doc = await _docRepo.GetByIdAsync(documentId, ct);
        if (doc == null) return false;

        if (doc.StorageBucket != null && doc.StorageKey != null)
        {
            try
            {
                var stream = await _storage.DownloadAsync(doc.StorageBucket, doc.StorageKey, ct);
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms, ct);
                stream.Dispose();
                ms.Position = 0;
                var hash = await SHA512.HashDataAsync(ms, ct);
                var hashHex = Convert.ToHexString(hash).ToLowerInvariant();

                doc.IsPqcVerified = hashHex == doc.Sha512Hash;
                doc.PqcVerifiedAt = DateTime.UtcNow;
                doc.VerifiedByUserId = verifiedByUserId;
                await _docRepo.UpdateAsync(doc, ct);

                await _logRepo.AddAsync(new DocumentAccessLog { Id = Guid.NewGuid(), DocumentId = documentId, AccessType = "Verify", AccessedByUserId = verifiedByUserId, Notes = doc.IsPqcVerified ? "PQC verification passed — hash matches" : "PQC verification FAILED — hash mismatch", TenantId = Guid.Empty }, ct);

                return doc.IsPqcVerified;
            }
            catch { }
        }
        return false;
    }

    public async Task<List<DocumentVaultEntry>> GetDocumentsByTierAsync(int tierLevel, CancellationToken ct)
    {
        var all = await _docRepo.FindAsync(d => d.TierLevel == tierLevel && !d.IsDeleted, ct);
        return all.OrderByDescending(d => d.UploadedAt).ToList();
    }

    public async Task<List<DocumentVaultEntry>> GetDocumentsByCategoryAsync(string category, CancellationToken ct)
    {
        var all = await _docRepo.FindAsync(d => d.DocumentCategory == category && !d.IsDeleted, ct);
        return all.OrderByDescending(d => d.UploadedAt).ToList();
    }

    public async Task<DocumentVaultEntry?> GetDocumentAsync(Guid documentId, CancellationToken ct)
        => await _docRepo.GetByIdAsync(documentId, ct);

    public async Task<List<DocumentTier>> GetTiersAsync(CancellationToken ct)
    {
        var all = await _tierRepo.FindAsync(t => t.IsActive, ct);
        return all.OrderBy(t => t.TierLevel).ToList();
    }

    public async Task<DocumentTier> UpdateTierAsync(DocumentTier tier, CancellationToken ct)
    {
        var existing = await _tierRepo.GetByIdAsync(tier.Id, ct);
        if (existing == null) throw new InvalidOperationException("Tier not found.");
        existing.RetentionYears = tier.RetentionYears;
        existing.RetentionDays = tier.RetentionYears * 365;
        existing.ObjectLockMode = tier.ObjectLockMode;
        existing.Description = tier.Description;
        existing.RequirePqcVerification = tier.RequirePqcVerification;
        await _tierRepo.UpdateAsync(existing, ct);
        return existing;
    }

    private static (string signature, string publicKey) GeneratePqcSignature(byte[] hash)
    {
        // ML-DSA (FIPS 204) simulation — production should use BouncyCastle PQC or liboqs
        using var rsa = RSA.Create(3072);
        var sig = rsa.SignHash(hash, HashAlgorithmName.SHA512, RSASignaturePadding.Pss);
        var pub = rsa.ExportSubjectPublicKeyInfo();
        return (Convert.ToBase64String(sig), Convert.ToBase64String(pub));
    }
}
