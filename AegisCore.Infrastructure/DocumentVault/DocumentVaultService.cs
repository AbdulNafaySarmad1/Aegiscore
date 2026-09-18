using System.Security.Cryptography;
using AegisCore.Contracts.Abstractions.Cloud;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Application.DocumentVault;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure.Security.Pqc;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.DocumentVault;

public class DocumentVaultService : IDocumentVaultService
{
    private readonly IStorageService _storage;
    private readonly IRepository<DocumentVaultEntry> _docRepo;
    private readonly IRepository<DocumentTier> _tierRepo;
    private readonly IRepository<DocumentAccessLog> _logRepo;
    private readonly IRepository<DocumentRetentionPolicy> _policyRepo;
    private readonly IPqcSignatureService _signatures;
    private readonly ILogger<DocumentVaultService> _logger;
    private readonly string _defaultBucket;

    public DocumentVaultService(
        IStorageService storage,
        IRepository<DocumentVaultEntry> docRepo,
        IRepository<DocumentTier> tierRepo,
        IRepository<DocumentAccessLog> logRepo,
        IRepository<DocumentRetentionPolicy> policyRepo,
        IPqcSignatureService signatures,
        ILogger<DocumentVaultService> logger)
    {
        _storage = storage; _docRepo = docRepo; _tierRepo = tierRepo;
        _logRepo = logRepo; _policyRepo = policyRepo; _signatures = signatures; _logger = logger;
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

    public async Task<DocumentVaultEntry> UploadDocumentAsync(Guid tenantId, Stream content, string fileName, string contentType, string category, Guid uploadedByUserId, string uploadedByRole, CancellationToken ct)
    {
        var tier = await GetTierForCategoryAsync(category, ct);

        // Compute SHA-512 hash
        content.Position = 0;
        var hash = await SHA512.HashDataAsync(content, ct);
        var hashHex = Convert.ToHexString(hash).ToLowerInvariant();
        content.Position = 0;

        // Sign the content hash with the installation's registered post-quantum key. Tier 1 keeps
        // documents for fifty years, which outlives an ML-DSA key's rotation window, so those are
        // signed with the stateless archival algorithm instead.
        var signed = await _signatures.SignAsync(tenantId, "document-vault", hash, archival: tier.TierLevel == 1, ct);

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
            PqcSignatureBase64 = Convert.ToBase64String(signed.Signature),
            PqcPublicKeyBase64 = Convert.ToBase64String(signed.PublicKey),
            PqcAlgorithm = signed.Algorithm, PqcKeyId = signed.KeyId,
            Sha512Hash = hashHex, IsPqcVerified = true, PqcVerifiedAt = DateTime.UtcNow,
            StorageBucket = tier.BucketName ?? _defaultBucket, StorageKey = storageKey,
            StorageUrl = storageUrl,
            UploadedAt = DateTime.UtcNow, RetentionExpiresAt = lockUntil,
            IsRetentionLocked = true, LockMode = tier.ObjectLockMode, LockedUntil = lockUntil,
            UploadedByUserId = uploadedByUserId, UploadedByRole = uploadedByRole,
            TenantId = tenantId
        };
        await _docRepo.AddAsync(entry, ct);

        await _logRepo.AddAsync(new DocumentAccessLog
        {
            Id = Guid.NewGuid(), DocumentId = entry.Id, AccessType = "Upload",
            AccessedByUserId = uploadedByUserId, AccessedByRole = uploadedByRole,
            TenantId = tenantId
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

        // A document that has failed verification stays withheld until someone restores it: serving
        // it would hand out bytes this installation can no longer vouch for.
        if (!doc.IsPqcVerified)
        {
            await _logRepo.AddAsync(new DocumentAccessLog { Id = Guid.NewGuid(), DocumentId = documentId, AccessType = "Download", AccessedByUserId = requestedByUserId, AccessedByRole = requestedByRole, WasDenied = true, DenialReason = "Withheld: the document is not verified", TenantId = doc.TenantId }, ct);
            _logger.LogWarning("Download withheld: document {Id} ({File}) is not verified", documentId, doc.FileName);
            throw new InvalidDataException($"'{doc.FileName}' is in the vault but is not verified, so it has been withheld. Its stored copy no longer matches the hash recorded when it was vaulted — restore it from a backup and verify it again.");
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
                {
                    // Serving bytes that do not match the recorded hash would hand someone a
                    // corrupted or substituted document under a fifty-year retention lock.
                    _logger.LogError("Hash mismatch on document {Id} ({File}) — stored={StoredHash}, downloaded={DownloadedHash}", documentId, doc.FileName, doc.Sha512Hash, hashHex);
                    doc.IsPqcVerified = false;
                    await _docRepo.UpdateAsync(doc, ct);
                    await ms.DisposeAsync();
                    throw new InvalidDataException($"'{doc.FileName}' does not match the hash recorded when it was vaulted, so it has been withheld and marked unverified. The stored copy has been altered or damaged; restore it from a backup.");
                }

                return ms;
            }
            catch (InvalidDataException) { throw; }
            catch (FileNotFoundException ex)
            {
                _logger.LogError(ex, "Document {Id} is recorded in the vault but absent from storage at {Bucket}/{Key}", documentId, doc.StorageBucket, doc.StorageKey);
                await _logRepo.AddAsync(new DocumentAccessLog { Id = Guid.NewGuid(), DocumentId = documentId, AccessType = "Download", AccessedByUserId = requestedByUserId, AccessedByRole = requestedByRole, WasDenied = true, DenialReason = "Recorded in the vault but absent from storage", TenantId = Guid.Empty }, ct);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Download failed for document {Id} via {Provider}", documentId, _storage.GetType().Name);
                throw;
            }
        }

        return null;
    }

    public async Task<bool> VerifyDocumentAsync(Guid tenantId, Guid documentId, Guid verifiedByUserId, CancellationToken ct)
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

                // Two separate questions: are these the bytes that were vaulted (hash), and did
                // this installation's key sign them (signature)? Both must answer yes.
                var hashMatches = hashHex == doc.Sha512Hash;
                var signatureValid = hashMatches
                    && doc.PqcAlgorithm is not null && doc.PqcSignatureBase64 is not null && doc.PqcPublicKeyBase64 is not null
                    && _signatures.Verify(doc.PqcAlgorithm, hash, Convert.FromBase64String(doc.PqcSignatureBase64), Convert.FromBase64String(doc.PqcPublicKeyBase64));

                doc.IsPqcVerified = hashMatches && signatureValid;
                doc.PqcVerifiedAt = DateTime.UtcNow;
                doc.VerifiedByUserId = verifiedByUserId;
                await _docRepo.UpdateAsync(doc, ct);

                var note = !hashMatches
                    ? "Failed: the stored bytes no longer match the hash recorded at upload"
                    : !signatureValid
                        ? $"Failed: the {doc.PqcAlgorithm ?? "post-quantum"} signature over that hash did not verify"
                        : $"Passed: hash matches and the {doc.PqcAlgorithm} signature verifies against key {doc.PqcKeyId}";
                await _logRepo.AddAsync(new DocumentAccessLog { Id = Guid.NewGuid(), DocumentId = documentId, AccessType = "Verify", AccessedByUserId = verifiedByUserId, Notes = note, TenantId = Guid.Empty }, ct);
                _logger.LogInformation("Vault verification of {File}: {Note}", doc.FileName, note);

                return doc.IsPqcVerified;
            }
            catch (Exception ex) { _logger.LogError(ex, "Verification of document {Id} could not complete", documentId); throw; }
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
        if (existing == null) throw new KeyNotFoundException("Tier not found.");
        existing.RetentionYears = tier.RetentionYears;
        existing.RetentionDays = tier.RetentionYears * 365;
        existing.ObjectLockMode = tier.ObjectLockMode;
        existing.Description = tier.Description;
        existing.RequirePqcVerification = tier.RequirePqcVerification;
        await _tierRepo.UpdateAsync(existing, ct);
        return existing;
    }

}
