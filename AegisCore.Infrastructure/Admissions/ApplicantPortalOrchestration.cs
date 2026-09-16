using System.Text.RegularExpressions;
using AegisCore.Application.Admissions;
using AegisCore.Application.Foundation;
using AegisCore.Application.Workflow;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Admissions;

public sealed class ApplicantPortalService(
    AegisDbContext db,
    IApplicantTenantResolver tenantResolver,
    IApplicantIdGenerator idGenerator,
    IApplicantOtpService otp,
    IApplicantTokenService tokens,
    IPhotoValidationService photoValidation,
    IStorageProvider storage,
    INotificationService notifications,
    ILogger<ApplicantPortalService> logger) : IApplicantPortalService
{
    private Guid TenantId => tenantResolver.ResolveTenantId();

    public async Task<ApplicantSignupResult> SignupAsync(string email, Guid intakeId, Guid programId, CancellationToken ct = default)
    {
        email = email.Trim();
        var (intake, program) = await ResolveIntakeProgramAsync(intakeId, programId, ct);
        _ = program;
        if (intake.Status != IntakeStatus.Open)
        {
            throw new InvalidOperationException("This intake is not currently open for applications.");
        }

        var code = await otp.IssueAsync(email, ct);
        await notifications.SendEmailAsync(
            email,
            "Your AegisCore application verification code",
            $"<p>Your verification code is <strong>{code}</strong>. It expires in 10 minutes.</p>",
            ct);

        return new ApplicantSignupResult(true);
    }

    public async Task<ApplicantVerifyResult> VerifyAsync(string email, string otpCode, Guid intakeId, Guid programId, CancellationToken ct = default)
    {
        email = email.Trim();
        if (!await otp.VerifyAsync(email, otpCode, ct))
        {
            throw new InvalidOperationException("The verification code is invalid or has expired.");
        }

        var tenantId = TenantId;
        var (intake, program) = await ResolveIntakeProgramAsync(intakeId, programId, ct);

        var profile = await db.ApplicantProfiles.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Email == email && p.IntakeId == intakeId, ct);

        if (profile is null)
        {
            profile = new ApplicantProfile
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Email = email,
                IntakeId = intakeId,
                ProgramId = programId,
                Status = ApplicantStatus.Draft,
                ApplicantId = await idGenerator.GenerateAsync(intake, program, ct),
                EmailVerifiedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            db.ApplicantProfiles.Add(profile);
        }
        else
        {
            profile.EmailVerifiedAt ??= DateTimeOffset.UtcNow;
            profile.ProgramId = programId;
            profile.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        return new ApplicantVerifyResult(profile.ApplicantId, tokens.IssueToken(profile), profile.Status);
    }

    public async Task<ApplicantProfileView> GetProfileAsync(Guid applicantProfileId, CancellationToken ct = default)
        => ToView(await RequireProfileAsync(applicantProfileId, ct), await LoadRequirementViewsAsync(applicantProfileId, ct));

    public async Task<ApplicantProfileView> UpdatePersonalDetailsAsync(Guid applicantProfileId, ApplicantPersonalDetails details, CancellationToken ct = default)
    {
        var profile = await RequireProfileAsync(applicantProfileId, ct);
        RequireEditable(profile);

        profile.FullName = details.FullName.Trim();
        profile.DateOfBirth = details.DateOfBirth;
        profile.Gender = details.Gender.Trim();
        profile.Nationality = details.Nationality.Trim();
        profile.Phone = details.Phone.Trim();
        profile.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return ToView(profile, await LoadRequirementViewsAsync(applicantProfileId, ct));
    }

    public async Task<ApplicantDocumentUploadResult> UploadDocumentAsync(
        Guid applicantProfileId, AdmissionDocumentKind documentType, Stream content, string fileName, string contentType, long length, CancellationToken ct = default)
    {
        if (documentType == AdmissionDocumentKind.Photo)
        {
            throw new InvalidOperationException("Use the photo upload endpoint for passport photos.");
        }

        var profile = await RequireProfileAsync(applicantProfileId, ct);
        RequireEditable(profile);

        var requirement = await db.AdmissionDocumentRequirements.IgnoreQueryFilters().FirstOrDefaultAsync(
            r => r.TenantId == profile.TenantId && r.IntakeId == profile.IntakeId && r.ProgramId == profile.ProgramId && r.DocumentType == documentType, ct)
            ?? throw new InvalidOperationException("This document is not requested for the selected program.");

        var (ext, mime) = ValidateFormat(requirement.AcceptedFormat, fileName, contentType);
        if (length <= 0 || length > requirement.MaxFileSizeMb * 1024L * 1024L)
        {
            throw new InvalidOperationException($"File must be between 1 byte and {requirement.MaxFileSizeMb} MB.");
        }

        await using var buffered = await BufferAndVerifySignatureAsync(content, ext, ct);

        var path = $"tenants/{profile.TenantId}/applicants/{profile.ApplicantId}/{documentType.ToString().ToLowerInvariant()}.{ext}";
        await ReplaceStoredFileAsync(applicantProfileId, documentType, path, buffered, mime, ct);
        buffered.Position = 0;
        var storedPath = await storage.UploadAsync(path, buffered, mime, ct);

        var doc = await UpsertDocumentRowAsync(profile, documentType, storedPath, buffered.Length, mime, AdmissionDocumentValidationStatus.Pending, null, null, null, ct);
        await db.SaveChangesAsync(ct);

        return new ApplicantDocumentUploadResult(documentType, doc.ValidationStatus, doc.RejectionReason, doc.ValidationConfidence);
    }

    public async Task<ApplicantDocumentUploadResult> UploadPhotoAsync(
        Guid applicantProfileId, Stream content, string fileName, string contentType, long length, CancellationToken ct = default)
    {
        var profile = await RequireProfileAsync(applicantProfileId, ct);
        RequireEditable(profile);

        var requirement = await db.AdmissionDocumentRequirements.IgnoreQueryFilters().FirstOrDefaultAsync(
            r => r.TenantId == profile.TenantId && r.IntakeId == profile.IntakeId && r.ProgramId == profile.ProgramId && r.DocumentType == AdmissionDocumentKind.Photo, ct);
        var maxMb = requirement?.MaxFileSizeMb ?? 5;

        var (ext, mime) = ValidateFormat(AdmissionDocumentFormat.Image, fileName, contentType);
        if (length <= 0 || length > maxMb * 1024L * 1024L)
        {
            throw new InvalidOperationException($"Photo must be between 1 byte and {maxMb} MB.");
        }

        await using var buffered = await BufferAndVerifySignatureAsync(content, ext, ct);

        buffered.Position = 0;
        var validation = await photoValidation.ValidateAsync(buffered, fileName, mime, ct);

        var status = validation switch
        {
            { RequiresManualReview: true } => AdmissionDocumentValidationStatus.ManualReview,
            { Passed: true } => AdmissionDocumentValidationStatus.Approved,
            _ => AdmissionDocumentValidationStatus.Rejected
        };

        var path = $"tenants/{profile.TenantId}/applicants/{profile.ApplicantId}/photo.{ext}";
        buffered.Position = 0;
        await ReplaceStoredFileAsync(applicantProfileId, AdmissionDocumentKind.Photo, path, buffered, mime, ct);
        buffered.Position = 0;
        var storedPath = await storage.UploadAsync(path, buffered, mime, ct);

        var method = validation.RequiresManualReview ? "failopen" : "clip";
        var doc = await UpsertDocumentRowAsync(profile, AdmissionDocumentKind.Photo, storedPath, buffered.Length, mime, status,
            status == AdmissionDocumentValidationStatus.Rejected ? validation.Reason : null, validation.Confidence, method, ct);
        await db.SaveChangesAsync(ct);

        if (validation.RequiresManualReview)
        {
            logger.LogWarning("Applicant {ApplicantId} photo flagged for manual review (CLIP unavailable).", profile.ApplicantId);
            await NotifyAdmissionsStaffAsync(profile.TenantId,
                "Applicant photo needs manual review",
                $"The photo verification service was unavailable for applicant {profile.ApplicantId}. Review the uploaded photo manually.", ct);
        }

        return new ApplicantDocumentUploadResult(AdmissionDocumentKind.Photo, doc.ValidationStatus, doc.RejectionReason, doc.ValidationConfidence);
    }

    public async Task<ApplicantProfileView> SubmitAsync(Guid applicantProfileId, CancellationToken ct = default)
    {
        var profile = await RequireProfileAsync(applicantProfileId, ct);
        RequireEditable(profile);

        if (string.IsNullOrWhiteSpace(profile.FullName) || profile.DateOfBirth is null)
        {
            throw new InvalidOperationException("Complete your personal details before submitting.");
        }

        var requirements = await db.AdmissionDocumentRequirements.IgnoreQueryFilters()
            .Where(r => r.TenantId == profile.TenantId && r.IntakeId == profile.IntakeId && r.ProgramId == profile.ProgramId)
            .ToListAsync(ct);
        var uploaded = await db.ApplicantDocuments.IgnoreQueryFilters()
            .Where(d => d.TenantId == profile.TenantId && d.ApplicantId == profile.Id)
            .ToListAsync(ct);

        foreach (var required in requirements.Where(r => r.IsRequired))
        {
            var match = uploaded.FirstOrDefault(d => d.DocumentType == required.DocumentType);
            if (match is null || match.ValidationStatus == AdmissionDocumentValidationStatus.Rejected)
            {
                throw new InvalidOperationException($"Required document '{required.DocumentType}' is missing or was rejected.");
            }
        }

        profile.Status = ApplicantStatus.Submitted;
        profile.SubmittedAt = DateTimeOffset.UtcNow;
        profile.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        await notifications.SendEmailAsync(
            profile.Email,
            "Your AegisCore application has been submitted",
            $"<p>Your application is submitted. Your Applicant ID is <strong>{profile.ApplicantId}</strong> — keep it safe; you will need it for tests and status checks.</p>",
            ct);

        return ToView(profile, await LoadRequirementViewsAsync(applicantProfileId, ct));
    }

    // ---- helpers ----

    private async Task<(AdmissionIntake Intake, DegreeProgram Program)> ResolveIntakeProgramAsync(Guid intakeId, Guid programId, CancellationToken ct)
    {
        var tenantId = TenantId;
        var intake = await db.AdmissionIntakes.IgnoreQueryFilters().FirstOrDefaultAsync(i => i.TenantId == tenantId && i.Id == intakeId, ct)
            ?? throw new KeyNotFoundException("Intake was not found.");
        var program = await db.DegreePrograms.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Id == programId && p.IsActive, ct)
            ?? throw new KeyNotFoundException("Program was not found.");
        return (intake, program);
    }

    private async Task<ApplicantProfile> RequireProfileAsync(Guid applicantProfileId, CancellationToken ct)
    {
        var tenantId = TenantId;
        return await db.ApplicantProfiles.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Id == applicantProfileId, ct)
            ?? throw new KeyNotFoundException("Applicant profile was not found.");
    }

    private static void RequireEditable(ApplicantProfile profile)
    {
        if (profile.Status is not (ApplicantStatus.Draft or ApplicantStatus.Submitted))
        {
            throw new InvalidOperationException("This application can no longer be edited.");
        }

        if (profile.Status == ApplicantStatus.Submitted)
        {
            // Submitted applications are locked; only staff can move them forward.
            throw new InvalidOperationException("Your application has already been submitted.");
        }
    }

    private async Task<IReadOnlyList<ApplicantDocumentRequirementView>> LoadRequirementViewsAsync(Guid applicantProfileId, CancellationToken ct)
    {
        var profile = await RequireProfileAsync(applicantProfileId, ct);
        var requirements = await db.AdmissionDocumentRequirements.IgnoreQueryFilters()
            .Where(r => r.TenantId == profile.TenantId && r.IntakeId == profile.IntakeId && r.ProgramId == profile.ProgramId)
            .OrderBy(r => r.DocumentType)
            .ToListAsync(ct);
        var uploaded = await db.ApplicantDocuments.IgnoreQueryFilters()
            .Where(d => d.TenantId == profile.TenantId && d.ApplicantId == profile.Id)
            .ToListAsync(ct);

        return requirements.Select(r =>
        {
            var up = uploaded.FirstOrDefault(d => d.DocumentType == r.DocumentType);
            return new ApplicantDocumentRequirementView(r.Id, r.DocumentType, r.IsRequired, r.AcceptedFormat, r.MaxFileSizeMb,
                up?.ValidationStatus, up?.RejectionReason);
        }).ToList();
    }

    private static ApplicantProfileView ToView(ApplicantProfile p, IReadOnlyList<ApplicantDocumentRequirementView> docs)
        => new(p.ApplicantId, p.Email, p.Status, p.IntakeId, p.ProgramId, p.FullName, p.DateOfBirth, p.Gender, p.Nationality, p.Phone, docs);

    private async Task<ApplicantDocument> UpsertDocumentRowAsync(
        ApplicantProfile profile, AdmissionDocumentKind type, string storagePath, long size, string mime,
        AdmissionDocumentValidationStatus status, string? rejectionReason, double? confidence, string? method, CancellationToken ct)
    {
        var doc = await db.ApplicantDocuments.IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.TenantId == profile.TenantId && d.ApplicantId == profile.Id && d.DocumentType == type, ct);

        if (doc is null)
        {
            doc = new ApplicantDocument
            {
                Id = Guid.NewGuid(),
                TenantId = profile.TenantId,
                ApplicantId = profile.Id,
                DocumentType = type
            };
            db.ApplicantDocuments.Add(doc);
        }

        doc.StoragePath = storagePath;
        doc.FileSize = size;
        doc.MimeType = mime;
        doc.UploadedAt = DateTimeOffset.UtcNow;
        doc.ValidationStatus = status;
        doc.RejectionReason = rejectionReason;
        doc.ValidationConfidence = confidence;
        doc.ValidationMethod = method;
        doc.ReviewedBy = null;
        doc.ReviewedAt = null;
        return doc;
    }

    private async Task ReplaceStoredFileAsync(Guid applicantProfileId, AdmissionDocumentKind type, string newPath, Stream _, string __, CancellationToken ct)
    {
        var existing = await db.ApplicantDocuments.IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.ApplicantId == applicantProfileId && d.DocumentType == type, ct);
        if (existing is not null && !string.Equals(existing.StoragePath, newPath, StringComparison.Ordinal))
        {
            try { await storage.DeleteAsync(existing.StoragePath, ct); }
            catch (Exception ex) { logger.LogWarning(ex, "Could not delete replaced applicant document {Path}", existing.StoragePath); }
        }
    }

    private static (string Extension, string MimeType) ValidateFormat(AdmissionDocumentFormat format, string fileName, string contentType)
    {
        var ext = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
        return format switch
        {
            AdmissionDocumentFormat.Pdf when ext == "pdf" => ("pdf", "application/pdf"),
            AdmissionDocumentFormat.Image when ext is "jpg" or "jpeg" => ("jpg", "image/jpeg"),
            AdmissionDocumentFormat.Image when ext == "png" => ("png", "image/png"),
            AdmissionDocumentFormat.Pdf => throw new InvalidOperationException("This document must be a PDF file."),
            _ => throw new InvalidOperationException("This document must be a JPEG or PNG image.")
        };
    }

    private static async Task<MemoryStream> BufferAndVerifySignatureAsync(Stream content, string ext, CancellationToken ct)
    {
        var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, ct);
        buffer.Position = 0;

        var header = new byte[8];
        var read = await buffer.ReadAsync(header, ct);
        buffer.Position = 0;
        if (!ValidFileSignature(ext, header, read))
        {
            await buffer.DisposeAsync();
            throw new InvalidOperationException("File content does not match its declared type.");
        }

        return buffer;
    }

    private static bool ValidFileSignature(string ext, byte[] header, int length)
        => ext == "pdf" && length >= 4 && header[..4].SequenceEqual(new byte[] { 0x25, 0x50, 0x44, 0x46 })
           || ext is "jpg" or "jpeg" && length >= 3 && header[..3].SequenceEqual(new byte[] { 0xff, 0xd8, 0xff })
           || ext == "png" && length >= 8 && header.SequenceEqual(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a });

    private async Task NotifyAdmissionsStaffAsync(Guid tenantId, string title, string message, CancellationToken ct)
    {
        var staff = await db.Users.IgnoreQueryFilters()
            .Where(u => u.TenantId == tenantId && u.Status == UserStatus.Active)
            .Join(db.UserRoles.IgnoreQueryFilters(), u => u.Id, ur => ur.UserId, (u, ur) => new { u, ur })
            .Join(db.Roles.IgnoreQueryFilters(), x => x.ur.RoleId, r => r.Id, (x, r) => new { x.u, r })
            .Where(x => x.r.Name.Contains("Admission") || x.r.Name.Contains("Admin"))
            .Select(x => x.u.Id)
            .Distinct()
            .ToListAsync(ct);

        foreach (var userId in staff)
        {
            await notifications.SendInAppAsync(userId, title, message, ct);
        }
    }
}
