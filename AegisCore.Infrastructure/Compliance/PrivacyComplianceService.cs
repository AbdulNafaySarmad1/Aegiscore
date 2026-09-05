using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AegisCore.Application.Compliance;
using AegisCore.Contracts.Abstractions.Cloud;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure.Security.Pqc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Compliance;

public sealed class PrivacyComplianceService(
    AegisDbContext db,
    IPqcCryptographyService encryption,
    IStorageService storage,
    IConfiguration configuration,
    ILogger<PrivacyComplianceService> logger) : IPrivacyComplianceService
{
    private static readonly Regex CategoryPattern = new("^[a-z0-9_]{2,80}$", RegexOptions.Compiled);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string _profileBucket = configuration["STORAGE_BUCKET"] ?? "aegiscore-profiles";

    public async Task<ConsentLedger> RecordConsentAsync(
        Guid userId,
        string dataCategory,
        string consentText,
        DateTime? expiresAt,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (!CategoryPattern.IsMatch(dataCategory))
            throw new ArgumentException("Data category must use lowercase letters, numbers, or underscores.", nameof(dataCategory));
        if (string.IsNullOrWhiteSpace(consentText) || consentText.Length > 8000)
            throw new ArgumentException("The exact consent text is required and must not exceed 8,000 characters.", nameof(consentText));
        if (expiresAt is not null && expiresAt <= DateTime.UtcNow)
            throw new ArgumentException("Consent expiry must be in the future.", nameof(expiresAt));
        if (!await db.Users.AnyAsync(user => user.Id == userId, cancellationToken))
            throw new KeyNotFoundException("User was not found.");

        var ledger = new ConsentLedger
        {
            Id = Guid.NewGuid(),
            TenantId = RequireTenant(),
            UserId = userId,
            DataCategory = dataCategory,
            ConsentedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            ConsentText = consentText
        };
        if (!string.IsNullOrWhiteSpace(ipAddress))
        {
            var aad = Encoding.UTF8.GetBytes($"{ledger.TenantId:N}:{ledger.Id:N}:consent-ip");
            ledger.SetEncryptedIpAddress(ipAddress, encryption.Encrypt(Encoding.UTF8.GetBytes(ipAddress), aad));
        }

        db.ConsentLedgers.Add(ledger);
        await db.SaveChangesAsync(cancellationToken);
        return ledger;
    }

    public async Task RevokeConsentAsync(Guid userId, Guid consentId, CancellationToken cancellationToken = default)
    {
        var ledger = await db.ConsentLedgers.SingleOrDefaultAsync(
            item => item.Id == consentId && item.UserId == userId, cancellationToken)
            ?? throw new KeyNotFoundException("Consent record was not found.");
        ledger.Revoke(DateTime.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ErasureRequest> RequestErasureAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (!await db.Users.AnyAsync(user => user.Id == userId, cancellationToken))
            throw new KeyNotFoundException("User was not found.");

        var existing = await db.ErasureRequests.FirstOrDefaultAsync(
            request => request.UserId == userId
                && (request.Status == ErasureRequestStatus.Pending || request.Status == ErasureRequestStatus.InProgress),
            cancellationToken);
        if (existing is not null) return existing;

        var requestedAt = DateTime.UtcNow;
        var request = new ErasureRequest
        {
            Id = Guid.NewGuid(),
            TenantId = RequireTenant(),
            UserId = userId,
            RequestedAt = requestedAt,
            DeadlineAt = requestedAt.AddDays(30),
            Status = ErasureRequestStatus.Pending
        };
        db.ErasureRequests.Add(request);
        await db.SaveChangesAsync(cancellationToken);
        return request;
    }

    public async Task ProcessPendingErasureRequestsAsync(CancellationToken cancellationToken = default)
    {
        var ids = await db.ErasureRequests.IgnoreQueryFilters()
            .Where(request => request.Status == ErasureRequestStatus.Pending || request.Status == ErasureRequestStatus.InProgress)
            .OrderBy(request => request.RequestedAt)
            .Select(request => request.Id)
            .ToListAsync(cancellationToken);
        foreach (var id in ids)
        {
            await ProcessErasureAsync(id, cancellationToken);
        }
    }

    public async Task ProcessErasureAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        var request = await db.ErasureRequests.IgnoreQueryFilters().SingleOrDefaultAsync(
            item => item.Id == requestId, cancellationToken)
            ?? throw new KeyNotFoundException("Erasure request was not found.");
        if (request.Status == ErasureRequestStatus.Completed) return;

        request.Status = ErasureRequestStatus.InProgress;
        request.FailureReason = null;
        await db.SaveChangesAsync(cancellationToken);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var tenantId = request.TenantId;
            var userId = request.UserId;
            var erasedName = $"ERASED_USER_{userId:N}";
            var user = await db.Users.IgnoreQueryFilters().SingleOrDefaultAsync(
                item => item.TenantId == tenantId && item.Id == userId, cancellationToken);
            var originalEmail = user?.Email;

            if (user is not null)
            {
                user.Username = erasedName;
                user.Email = $"{userId:N}@erased.invalid";
                user.FirstName = erasedName;
                user.LastName = string.Empty;
                user.PasswordHash = null;
                user.ExternalId = null;
                user.ExternalClaimsJson = null;
                user.RefreshToken = null;
                user.RefreshTokenExpiry = null;
                user.DeactivatedAt = DateTime.UtcNow;
            }

            var student = await db.Students.IgnoreQueryFilters().SingleOrDefaultAsync(
                item => item.TenantId == tenantId && item.Id == userId, cancellationToken);
            if (student is not null)
            {
                student.FirstName = erasedName;
                student.LastName = string.Empty;
                student.UniversityId = erasedName;
                student.DirectoryOptOut = true;

                var profiles = await db.StudentProfiles.IgnoreQueryFilters()
                    .Where(item => item.TenantId == tenantId && item.StudentId == student.Id)
                    .ToListAsync(cancellationToken);
                foreach (var profile in profiles) profile.ProfileJson = "{}";

                var guardians = await db.StudentGuardians.IgnoreQueryFilters()
                    .Where(item => item.TenantId == tenantId && item.StudentId == student.Id)
                    .ToListAsync(cancellationToken);
                foreach (var guardian in guardians)
                {
                    guardian.Name = "ERASED";
                    guardian.Phone = null;
                    guardian.Email = null;
                }
            }

            if (!string.IsNullOrWhiteSpace(originalEmail))
            {
                var applications = await db.AdmissionApplications.IgnoreQueryFilters()
                    .Where(item => item.TenantId == tenantId && item.Email == originalEmail)
                    .ToListAsync(cancellationToken);
                foreach (var application in applications)
                {
                    application.StudentName = erasedName;
                    application.Email = $"{userId:N}@erased.invalid";
                    application.Phone = string.Empty;
                    application.Cnic = "00000-0000000-0";
                    application.DateOfBirth = default;
                }
            }

            var audits = await db.FerpaAuditLogs.IgnoreQueryFilters()
                .Where(item => item.TenantId == tenantId && (item.UserId == userId || item.StudentId == userId))
                .ToListAsync(cancellationToken);
            foreach (var audit in audits) audit.EraseIdentifiers(userId);

            var consents = await db.ConsentLedgers.IgnoreQueryFilters()
                .Where(item => item.TenantId == tenantId && item.UserId == userId)
                .ToListAsync(cancellationToken);
            foreach (var consent in consents) consent.Revoke(DateTime.UtcNow);

            var sessions = await db.SsoSessions.IgnoreQueryFilters()
                .Where(item => item.TenantId == tenantId && item.UserId == userId)
                .ToListAsync(cancellationToken);
            db.SsoSessions.RemoveRange(sessions);

            var pictures = await db.ProfilePictureRecords.IgnoreQueryFilters()
                .Where(item => item.TenantId == tenantId && item.UserId == userId)
                .ToListAsync(cancellationToken);
            foreach (var picture in pictures)
            {
                if (!string.IsNullOrWhiteSpace(picture.StorageKey))
                    await storage.DeleteAsync(_profileBucket, $"{picture.StorageKey}.jpg", cancellationToken);
            }
            db.ProfilePictureRecords.RemoveRange(pictures);

            request.Status = ErasureRequestStatus.Completed;
            request.ErasedAt = DateTime.UtcNow;
            request.FailureReason = null;
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            logger.LogInformation("Completed GDPR erasure request {RequestId} for tenant {TenantId}.", request.Id, tenantId);
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            db.ChangeTracker.Clear();
            var failed = await db.ErasureRequests.IgnoreQueryFilters().SingleAsync(item => item.Id == requestId, cancellationToken);
            failed.Status = ErasureRequestStatus.Failed;
            failed.FailureReason = exception.Message.Length <= 4000 ? exception.Message : exception.Message[..4000];
            await db.SaveChangesAsync(cancellationToken);
            logger.LogError(exception, "GDPR erasure request {RequestId} failed.", requestId);
        }
    }

    public async Task<DsarArchive> CreateDsarArchiveAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenant();
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(item => item.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("User was not found.");
        var student = await db.Students.AsNoTracking().SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
        var profile = student is null ? null : await db.StudentProfiles.AsNoTracking()
            .Where(item => item.StudentId == student.Id)
            .Select(item => item.ProfileJson)
            .FirstOrDefaultAsync(cancellationToken);

        var grades = await db.CourseGrades.AsNoTracking()
            .Where(item => item.StudentId == userId)
            .OrderBy(item => item.CourseId)
            .Select(item => new { item.Id, item.CourseId, item.SemesterId, item.TotalMarksObtained, item.PercentageObtained, item.LetterGrade, item.GradePoints, item.ResultStatus })
            .ToListAsync(cancellationToken);
        var attendance = await db.Attendances.AsNoTracking()
            .Where(item => item.Enrollment.StudentId == userId)
            .OrderBy(item => item.Date)
            .Select(item => new { item.Id, item.CourseId, item.Date, item.Status, item.LocationStatus, item.IsManualOverride })
            .ToListAsync(cancellationToken);
        var consents = await db.ConsentLedgers.AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderBy(item => item.ConsentedAt)
            .Select(item => new { item.Id, item.DataCategory, item.ConsentedAt, item.ExpiresAt, item.RevokedAt, item.ConsentText })
            .ToListAsync(cancellationToken);
        var accessLogs = await db.FerpaAuditLogs.AsNoTracking()
            .Where(item => item.UserId == userId || item.StudentId == userId)
            .OrderBy(item => item.AccessedAt)
            .Select(item => new { item.Id, item.UserId, item.UserRole, item.ResourceType, item.ResourceId, item.StudentId, item.Action, item.AccessedAt, item.Justified })
            .ToListAsync(cancellationToken);

        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            await WriteJsonAsync(archive, "student.json", new
            {
                user = new { user.Id, user.Username, user.Email, user.FirstName, user.LastName, user.Status, user.CreatedAt, user.LastLoginAt },
                student,
                profile = profile is null ? null : JsonSerializer.Deserialize<object>(profile)
            }, cancellationToken);
            await WriteCsvAsync(archive, "grades.csv",
                ["id", "courseId", "semesterId", "totalMarks", "percentage", "letterGrade", "gradePoints", "status"],
                grades.Select(item => new[] { item.Id.ToString(), item.CourseId.ToString(), item.SemesterId.ToString(), item.TotalMarksObtained.ToString(), item.PercentageObtained.ToString(), item.LetterGrade ?? "", item.GradePoints.ToString(), item.ResultStatus }),
                cancellationToken);
            await WriteCsvAsync(archive, "attendance.csv",
                ["id", "courseId", "date", "status", "locationStatus", "manualOverride"],
                attendance.Select(item => new[] { item.Id.ToString(), item.CourseId.ToString(), item.Date.ToString("O"), item.Status.ToString(), item.LocationStatus.ToString(), item.IsManualOverride.ToString() }),
                cancellationToken);
            await WriteJsonAsync(archive, "consent.json", consents, cancellationToken);
            await WriteJsonAsync(archive, "access_log.json", accessLogs, cancellationToken);
        }

        return new DsarArchive(output.ToArray(), DateTime.UtcNow.AddHours(24));
    }

    public async Task<int> ApplyRetentionAsync(
        Guid tenantId,
        DateTime nowUtc,
        int auditRetentionYears,
        CancellationToken cancellationToken = default)
    {
        auditRetentionYears = Math.Clamp(auditRetentionYears, 1, 100);
        var auditCutoff = nowUtc.AddYears(-auditRetentionYears);
        var erasureCutoff = nowUtc.AddYears(-7);
        var audits = await db.FerpaAuditLogs.IgnoreQueryFilters()
            .Where(item => item.TenantId == tenantId && item.AccessedAt < auditCutoff)
            .ToListAsync(cancellationToken);
        var consents = await db.ConsentLedgers.IgnoreQueryFilters()
            .Where(item => item.TenantId == tenantId && item.RevokedAt != null && item.RevokedAt < auditCutoff)
            .ToListAsync(cancellationToken);
        var erasures = await db.ErasureRequests.IgnoreQueryFilters()
            .Where(item => item.TenantId == tenantId && item.Status == ErasureRequestStatus.Completed
                && item.ErasedAt != null && item.ErasedAt < erasureCutoff)
            .ToListAsync(cancellationToken);

        db.RemoveRange(audits);
        db.RemoveRange(consents);
        db.RemoveRange(erasures);
        await db.SaveChangesAsync(cancellationToken);
        return audits.Count + consents.Count + erasures.Count;
    }

    private Guid RequireTenant() => db.CurrentTenantId != Guid.Empty
        ? db.CurrentTenantId
        : throw new InvalidOperationException("Tenant context is required.");

    private static async Task WriteJsonAsync(ZipArchive archive, string name, object value, CancellationToken cancellationToken)
    {
        await using var stream = archive.CreateEntry(name, CompressionLevel.Fastest).Open();
        await JsonSerializer.SerializeAsync(stream, value, JsonOptions, cancellationToken);
    }

    private static async Task WriteCsvAsync(
        ZipArchive archive,
        string name,
        IEnumerable<string> headers,
        IEnumerable<string[]> rows,
        CancellationToken cancellationToken)
    {
        await using var stream = archive.CreateEntry(name, CompressionLevel.Fastest).Open();
        await using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: false);
        await writer.WriteLineAsync(string.Join(",", headers.Select(EscapeCsv)).AsMemory(), cancellationToken);
        foreach (var row in rows)
            await writer.WriteLineAsync(string.Join(",", row.Select(EscapeCsv)).AsMemory(), cancellationToken);
    }

    private static string EscapeCsv(string value) =>
        value.IndexOfAny([',', '"', '\r', '\n']) >= 0 ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
}
