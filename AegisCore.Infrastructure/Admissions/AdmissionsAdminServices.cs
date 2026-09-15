using AegisCore.Application.Admissions;
using AegisCore.Application.Foundation;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Admissions;

/// <summary>Public catalogue of open intakes and their programs for the applicant portal Step 1.</summary>
public sealed class AdmissionCatalogService(AegisDbContext db, IApplicantTenantResolver tenantResolver) : IAdmissionCatalogService
{
    public async Task<IReadOnlyList<OpenIntakeView>> GetOpenIntakesAsync(CancellationToken ct = default)
    {
        var tenantId = tenantResolver.ResolveTenantId();

        var intakes = await db.AdmissionIntakes.IgnoreQueryFilters()
            .Where(i => i.TenantId == tenantId && i.Status == IntakeStatus.Open && i.IsActive)
            .OrderByDescending(i => i.Year)
            .ToListAsync(ct);

        if (intakes.Count == 0)
        {
            return [];
        }

        var intakeIds = intakes.Select(i => i.Id).ToList();
        var programIds = await db.AdmissionDocumentRequirements.IgnoreQueryFilters()
            .Where(r => r.TenantId == tenantId && intakeIds.Contains(r.IntakeId))
            .Select(r => new { r.IntakeId, r.ProgramId })
            .Distinct()
            .ToListAsync(ct);

        var allProgramIds = programIds.Select(p => p.ProgramId).Distinct().ToList();
        var programs = await db.DegreePrograms.IgnoreQueryFilters()
            .Where(p => p.TenantId == tenantId && allProgramIds.Contains(p.Id) && p.IsActive)
            .ToListAsync(ct);

        return intakes.Select(i => new OpenIntakeView(
            i.Id, i.Year, i.SemesterCode, i.Name,
            programIds.Where(p => p.IntakeId == i.Id)
                .Join(programs, p => p.ProgramId, pr => pr.Id, (_, pr) => new OpenProgramView(pr.Id, pr.Name, pr.Code))
                .OrderBy(p => p.Name)
                .ToList())).ToList();
    }
}

public sealed class AdmissionConfigService(AegisDbContext db) : IAdmissionConfigService
{
    private Guid TenantId => db.CurrentTenantId;

    public async Task<AdmissionIntake> CreateIntakeAsync(IntakeInput input, CancellationToken ct = default)
    {
        var intake = new AdmissionIntake
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            Year = input.Year,
            Semester = input.Semester,
            Name = string.IsNullOrWhiteSpace(input.Name) ? $"{input.Year} {input.Semester}" : input.Name.Trim(),
            Status = IntakeStatus.Draft,
            OpensAt = input.OpensAt,
            ClosesAt = input.ClosesAt,
            BatchId = input.BatchId,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.AdmissionIntakes.Add(intake);
        await db.SaveChangesAsync(ct);
        return intake;
    }

    public async Task<AdmissionIntake> UpdateIntakeStatusAsync(Guid intakeId, IntakeStatus status, CancellationToken ct = default)
    {
        var intake = await db.AdmissionIntakes.FirstOrDefaultAsync(i => i.Id == intakeId, ct)
            ?? throw new InvalidOperationException("Intake was not found.");
        intake.Status = status;
        await db.SaveChangesAsync(ct);
        return intake;
    }

    public async Task<IReadOnlyList<AdmissionIntake>> ListIntakesAsync(CancellationToken ct = default)
        => await db.AdmissionIntakes.OrderByDescending(i => i.Year).ThenBy(i => i.Semester).ToListAsync(ct);

    public async Task SetProgramCodeAsync(Guid programId, string code, CancellationToken ct = default)
    {
        var program = await db.DegreePrograms.FirstOrDefaultAsync(p => p.Id == programId, ct)
            ?? throw new InvalidOperationException("Program was not found.");
        program.Code = code.Trim().ToUpperInvariant();
        await db.SaveChangesAsync(ct);
    }

    public async Task<AdmissionDocumentRequirement> UpsertDocumentRequirementAsync(DocumentRequirementInput input, CancellationToken ct = default)
    {
        var existing = await db.AdmissionDocumentRequirements.FirstOrDefaultAsync(
            r => r.IntakeId == input.IntakeId && r.ProgramId == input.ProgramId && r.DocumentType == input.DocumentType, ct);

        if (existing is null)
        {
            existing = new AdmissionDocumentRequirement
            {
                Id = Guid.NewGuid(),
                TenantId = TenantId,
                IntakeId = input.IntakeId,
                ProgramId = input.ProgramId,
                DocumentType = input.DocumentType,
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.AdmissionDocumentRequirements.Add(existing);
        }

        existing.IsRequired = input.IsRequired;
        existing.AcceptedFormat = input.AcceptedFormat;
        existing.MaxFileSizeMb = input.MaxFileSizeMb;
        existing.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task DeleteDocumentRequirementAsync(Guid requirementId, CancellationToken ct = default)
    {
        var requirement = await db.AdmissionDocumentRequirements.FirstOrDefaultAsync(r => r.Id == requirementId, ct);
        if (requirement is not null)
        {
            db.AdmissionDocumentRequirements.Remove(requirement);
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<IReadOnlyList<AdmissionDocumentRequirement>> ListDocumentRequirementsAsync(Guid intakeId, Guid programId, CancellationToken ct = default)
        => await db.AdmissionDocumentRequirements
            .Where(r => r.IntakeId == intakeId && r.ProgramId == programId)
            .OrderBy(r => r.DocumentType)
            .ToListAsync(ct);
}

public sealed class AdmissionsAdminService(AegisDbContext db, IStorageProvider storage) : IAdmissionsAdminService
{
    public async Task<IReadOnlyList<AdmissionApplicantRow>> ListAsync(Guid? intakeId, Guid? programId, ApplicantStatus? status, CancellationToken ct = default)
    {
        var query = db.ApplicantProfiles.AsQueryable();
        if (intakeId is { } i) query = query.Where(p => p.IntakeId == i);
        if (programId is { } pr) query = query.Where(p => p.ProgramId == pr);
        if (status is { } s) query = query.Where(p => p.Status == s);

        return await query
            .OrderByDescending(p => p.SubmittedAt ?? p.CreatedAt)
            .Select(p => new AdmissionApplicantRow(
                p.Id, p.ApplicantId, p.FullName, p.Email, p.ProgramId, p.IntakeId, p.Status, p.SubmittedAt, p.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<AdmissionApplicantDetail?> GetAsync(Guid applicantProfileId, CancellationToken ct = default)
    {
        var profile = await db.ApplicantProfiles.FirstOrDefaultAsync(p => p.Id == applicantProfileId, ct);
        if (profile is null) return null;

        var docs = await db.ApplicantDocuments
            .Where(d => d.ApplicantId == applicantProfileId)
            .OrderBy(d => d.DocumentType)
            .Select(d => new AdmissionApplicantDocument(
                d.Id, d.DocumentType, d.MimeType, d.FileSize, d.ValidationStatus, d.RejectionReason,
                d.ValidationConfidence, d.ValidationMethod, d.UploadedAt))
            .ToListAsync(ct);

        var row = new AdmissionApplicantRow(profile.Id, profile.ApplicantId, profile.FullName, profile.Email,
            profile.ProgramId, profile.IntakeId, profile.Status, profile.SubmittedAt, profile.CreatedAt);
        var test = await db.ApplicantTests.Where(t => t.ApplicantId == applicantProfileId)
            .Select(t => new AdmissionApplicantTest(t.Score, t.Passed, t.Status, t.CompletedAt)).SingleOrDefaultAsync(ct);
        return new AdmissionApplicantDetail(row, docs, test);
    }

    public async Task<DocumentDownload?> DownloadDocumentAsync(Guid applicantProfileId, Guid documentId, CancellationToken ct = default)
    {
        var doc = await db.ApplicantDocuments.FirstOrDefaultAsync(d => d.Id == documentId && d.ApplicantId == applicantProfileId, ct);
        if (doc is null) return null;

        var stream = await storage.DownloadAsync(doc.StoragePath, ct);
        var ext = Path.GetExtension(doc.StoragePath).TrimStart('.');
        return new DocumentDownload(stream, doc.MimeType, $"{doc.DocumentType}.{ext}");
    }

    public async Task ReviewDocumentAsync(Guid applicantProfileId, Guid documentId, bool approve, string? rejectionReason, Guid reviewerId, CancellationToken ct = default)
    {
        var doc = await db.ApplicantDocuments.FirstOrDefaultAsync(d => d.Id == documentId && d.ApplicantId == applicantProfileId, ct)
            ?? throw new InvalidOperationException("Document was not found.");

        doc.ValidationStatus = approve ? AdmissionDocumentValidationStatus.Approved : AdmissionDocumentValidationStatus.Rejected;
        doc.RejectionReason = approve ? null : (rejectionReason ?? "Rejected by reviewer");
        doc.ReviewedBy = reviewerId;
        doc.ReviewedAt = DateTimeOffset.UtcNow;
        doc.ValidationMethod = "manual";
        await db.SaveChangesAsync(ct);
    }

    private static readonly IReadOnlyDictionary<ApplicantStatus, ApplicantStatus[]> Allowed = new Dictionary<ApplicantStatus, ApplicantStatus[]>
    {
        [ApplicantStatus.Draft] = [ApplicantStatus.Submitted, ApplicantStatus.Withdrawn],
        [ApplicantStatus.Submitted] = [ApplicantStatus.UnderReview, ApplicantStatus.Rejected, ApplicantStatus.Withdrawn],
        [ApplicantStatus.UnderReview] = [ApplicantStatus.TestScheduled, ApplicantStatus.InterviewScheduled, ApplicantStatus.Rejected, ApplicantStatus.Admitted, ApplicantStatus.Withdrawn],
        [ApplicantStatus.TestScheduled] = [ApplicantStatus.InterviewScheduled, ApplicantStatus.UnderReview, ApplicantStatus.Rejected, ApplicantStatus.Admitted, ApplicantStatus.Withdrawn],
        [ApplicantStatus.InterviewScheduled] = [ApplicantStatus.UnderReview, ApplicantStatus.Admitted, ApplicantStatus.Rejected, ApplicantStatus.Withdrawn],
        [ApplicantStatus.Admitted] = [ApplicantStatus.Withdrawn],
        [ApplicantStatus.Rejected] = [ApplicantStatus.UnderReview],
        [ApplicantStatus.Withdrawn] = []
    };

    public async Task TransitionAsync(Guid applicantProfileId, ApplicantStatus target, string? note, CancellationToken ct = default)
    {
        var profile = await db.ApplicantProfiles.FirstOrDefaultAsync(p => p.Id == applicantProfileId, ct)
            ?? throw new InvalidOperationException("Applicant profile was not found.");

        if (profile.Status != target && !Allowed.GetValueOrDefault(profile.Status, []).Contains(target))
        {
            throw new InvalidOperationException($"Cannot move an applicant from {profile.Status} to {target}.");
        }

        profile.Status = target;
        profile.DecisionNote = note;
        profile.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task<int> BulkTransitionAsync(IReadOnlyList<Guid> applicantProfileIds, ApplicantStatus target, string? note, CancellationToken ct = default)
    {
        var profiles = await db.ApplicantProfiles.Where(p => applicantProfileIds.Contains(p.Id)).ToListAsync(ct);
        var moved = 0;
        foreach (var profile in profiles)
        {
            if (profile.Status == target || Allowed.GetValueOrDefault(profile.Status, []).Contains(target))
            {
                profile.Status = target;
                profile.DecisionNote = note;
                profile.UpdatedAt = DateTimeOffset.UtcNow;
                moved++;
            }
        }

        await db.SaveChangesAsync(ct);
        return moved;
    }
}
