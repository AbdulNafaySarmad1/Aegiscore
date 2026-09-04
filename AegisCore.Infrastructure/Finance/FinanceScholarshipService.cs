using AegisCore.Application.Finance;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Finance;

public sealed class FinanceScholarshipService(AegisDbContext db, IFinanceModuleService finance)
{
    private static readonly IReadOnlyDictionary<string, string[]> AllowedTransitions = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        ["APPLIED"] = ["SHORTLISTED", "REJECTED"],
        ["SHORTLISTED"] = ["COMMITTEE_REVIEW", "REJECTED"],
        ["COMMITTEE_REVIEW"] = ["APPROVED", "REJECTED"],
        ["APPROVED"] = ["DISBURSED"],
        ["DISBURSED"] = ["ACTIVE"],
        ["ACTIVE"] = ["RENEWED", "EXPIRED", "REVOKED"],
        ["RENEWED"] = ["ACTIVE", "EXPIRED", "REVOKED"]
    };

    public async Task<FinanceScholarshipProgram> CreateProgramAsync(Guid tenantId, Guid actorId, CreateScholarshipProgramCommand command, CancellationToken ct)
    {
        var type = Required(command.ScholarshipType, 40, "Scholarship type");
        var coverage = Required(command.CoverageType, 20, "Coverage type").ToUpperInvariant();
        var method = Required(command.DisbursementMethod, 20, "Disbursement method").ToUpperInvariant();
        if (tenantId == Guid.Empty || actorId == Guid.Empty || command.CoverageValue <= 0m || command.DurationMonths <= 0 || command.ClosesAt <= command.OpensAt)
            throw new ArgumentException("Scholarship program fields are invalid.");
        if (coverage is not ("FULL_TUITION" or "PARTIAL_PERCENT" or "FIXED_AMOUNT" or "FEE_HEAD") || method is not ("FEE_CREDIT" or "CASH_STIPEND" or "REIMBURSEMENT"))
            throw new ArgumentException("Scholarship coverage or disbursement method is unsupported.");
        if (coverage == "PARTIAL_PERCENT" && command.CoverageValue > 100m) throw new ArgumentException("Partial coverage cannot exceed 100 percent.");
        if (command.MinimumCgpa is < 0m or > 4m) throw new ArgumentException("Minimum CGPA must be between 0 and 4.");
        if (command.GrantId is not null && !await db.FinanceGrants.AnyAsync(x => x.TenantId == tenantId && x.Id == command.GrantId && x.Status == "ACTIVE", ct))
            throw new InvalidOperationException("Active grant was not found.");

        var program = new FinanceScholarshipProgram
        {
            Id = Guid.NewGuid(), TenantId = tenantId, GrantId = command.GrantId,
            Name = Required(command.Name, 200, "Program name"), ScholarshipType = type, CoverageType = coverage,
            CoverageValue = command.CoverageValue, DisbursementMethod = method, MinimumCgpa = command.MinimumCgpa,
            AllowsStacking = command.AllowsStacking, OpensAt = command.OpensAt, ClosesAt = command.ClosesAt,
            DurationMonths = command.DurationMonths, CreatedByUserId = actorId
        };
        db.FinanceScholarshipPrograms.Add(program);
        await db.SaveChangesAsync(ct);
        return program;
    }

    public async Task<FinanceScholarshipApplication> ApplyAsync(Guid tenantId, ApplyForScholarshipCommand command, CancellationToken ct)
    {
        if (tenantId == Guid.Empty || command.ProgramId == Guid.Empty || command.StudentId == Guid.Empty || command.Cgpa is < 0m or > 4m)
            throw new ArgumentException("Scholarship application fields are invalid.");
        var now = DateTime.UtcNow;
        var program = await db.FinanceScholarshipPrograms.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == command.ProgramId, ct)
            ?? throw new InvalidOperationException("Scholarship program was not found.");
        if (program.Status != "ANNOUNCED" || now < program.OpensAt || now > program.ClosesAt) throw new InvalidOperationException("Scholarship applications are closed.");
        if (program.MinimumCgpa is not null && command.Cgpa < program.MinimumCgpa) throw new InvalidOperationException("Applicant does not meet the minimum CGPA.");
        if (!await db.StudentProfiles.AnyAsync(x => x.TenantId == tenantId && x.StudentId == command.StudentId, ct)) throw new InvalidOperationException("Tenant student profile was not found.");
        if (!program.AllowsStacking && await db.FinanceScholarshipAwards.AnyAsync(x => x.TenantId == tenantId && x.StudentId == command.StudentId && (x.Status == "ACTIVE" || x.Status == "DISBURSED"), ct))
            throw new InvalidOperationException("This scholarship cannot be combined with another active award.");

        var application = new FinanceScholarshipApplication
        {
            Id = Guid.NewGuid(), TenantId = tenantId, ProgramId = program.Id, StudentId = command.StudentId,
            Cgpa = command.Cgpa, ApplicantStatement = Optional(command.ApplicantStatement, 1000)
        };
        db.FinanceScholarshipApplications.Add(application);
        await db.SaveChangesAsync(ct);
        return application;
    }

    public async Task<FinanceScholarshipApplication> TransitionAsync(Guid tenantId, Guid actorId, Guid applicationId, TransitionScholarshipApplicationCommand command, CancellationToken ct)
    {
        if (tenantId == Guid.Empty || actorId == Guid.Empty || applicationId == Guid.Empty) throw new ArgumentException("Application and actor are required.");
        var target = Required(command.Status, 20, "Status").ToUpperInvariant();
        var application = await db.FinanceScholarshipApplications.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == applicationId, ct)
            ?? throw new InvalidOperationException("Scholarship application was not found.");
        if (!AllowedTransitions.TryGetValue(application.Status, out var allowed) || !allowed.Contains(target, StringComparer.Ordinal)) throw new InvalidOperationException($"Transition from {application.Status} to {target} is not allowed.");
        if (target == "REJECTED" && string.IsNullOrWhiteSpace(command.Note)) throw new ArgumentException("A rejection note is required.");
        if (target == "DISBURSED") throw new InvalidOperationException("Use the disbursement operation to post an approved award.");

        db.FinanceScholarshipApplicationEvents.Add(new FinanceScholarshipApplicationEvent
        {
            Id = Guid.NewGuid(), TenantId = tenantId, ApplicationId = application.Id, FromStatus = application.Status,
            ToStatus = target, Note = Optional(command.Note, 1000), ActorUserId = actorId
        });
        application.Status = target;
        application.DecisionNote = Optional(command.Note, 1000);
        application.DecidedByUserId = actorId;
        application.DecidedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return application;
    }

    public async Task<FinanceScholarshipAward> DisburseAsync(Guid tenantId, Guid actorId, Guid applicationId, CancellationToken ct)
    {
        var application = await db.FinanceScholarshipApplications.Include(x => x.Program).SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == applicationId, ct)
            ?? throw new InvalidOperationException("Scholarship application was not found.");
        if (application.Status != "APPROVED" || application.AwardId is not null) throw new InvalidOperationException("Only an approved, undistributed application may be disbursed.");
        var program = application.Program;
        var method = program.DisbursementMethod == "REIMBURSEMENT" ? "CASH_STIPEND" : program.DisbursementMethod;
        var award = await finance.AwardScholarshipAsync(tenantId, actorId, new AwardFinanceScholarshipCommand(application.StudentId, program.GrantId, program.ScholarshipType, program.CoverageType, program.CoverageValue, method), ct);
        award.ApplicationId = application.Id;
        award.Status = "ACTIVE";
        award.RenewalReviewAt = DateTime.UtcNow.AddMonths(program.DurationMonths);
        application.AwardId = award.Id;
        application.Status = "ACTIVE";
        application.DecidedAt = DateTime.UtcNow;
        db.FinanceScholarshipApplicationEvents.Add(new FinanceScholarshipApplicationEvent
        {
            Id = Guid.NewGuid(), TenantId = tenantId, ApplicationId = application.Id, FromStatus = "APPROVED", ToStatus = "DISBURSED", ActorUserId = actorId
        });
        db.FinanceScholarshipApplicationEvents.Add(new FinanceScholarshipApplicationEvent
        {
            Id = Guid.NewGuid(), TenantId = tenantId, ApplicationId = application.Id, FromStatus = "DISBURSED", ToStatus = "ACTIVE", ActorUserId = actorId
        });
        await db.SaveChangesAsync(ct);
        return award;
    }

    public async Task<ScholarshipDonorReport> DonorReportAsync(Guid tenantId, Guid grantId, CancellationToken ct)
    {
        var grant = await db.FinanceGrants.AsNoTracking().SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == grantId, ct)
            ?? throw new InvalidOperationException("Grant was not found.");
        var programIds = await db.FinanceScholarshipPrograms.AsNoTracking().Where(x => x.TenantId == tenantId && x.GrantId == grantId).Select(x => x.Id).ToArrayAsync(ct);
        var applications = await db.FinanceScholarshipApplications.AsNoTracking().CountAsync(x => x.TenantId == tenantId && programIds.Contains(x.ProgramId), ct);
        var awards = await db.FinanceScholarshipAwards.AsNoTracking().CountAsync(x => x.TenantId == tenantId && x.GrantId == grantId, ct);
        return new ScholarshipDonorReport(grant.Id, grant.DonorName, grant.Currency, grant.AwardAmount, grant.ReceivedAmount, grant.SpentAmount, grant.ReceivedAmount - grant.SpentAmount, applications, awards);
    }

    private static string Required(string value, int maxLength, string field)
    {
        var result = value?.Trim() ?? string.Empty;
        if (result.Length is 0 || result.Length > maxLength) throw new ArgumentException($"{field} is invalid.");
        return result;
    }

    private static string? Optional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var result = value.Trim();
        if (result.Length > maxLength) throw new ArgumentException("Text exceeds its maximum length.");
        return result;
    }
}
