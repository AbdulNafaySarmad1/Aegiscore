using AegisCore.Application.Hr;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Hr;

public sealed class VisitingFacultyService(AegisDbContext db, ILogger<VisitingFacultyService> logger) : IVisitingFacultyService
{
    public async Task<IReadOnlyList<SlotMatchDto>> MatchFacultyToSlotsAsync(Guid tenantId, CancellationToken ct = default)
    {
        var slots = await db.Set<TeachingSlot>().IgnoreQueryFilters().Where(s => s.TenantId == tenantId && s.Status == "open").ToListAsync(ct);
        var faculty = await db.Set<VisitingFacultyProfile>().IgnoreQueryFilters().Where(f => f.TenantId == tenantId && f.ProfileStatus == "ready_to_deploy").ToListAsync(ct);
        var results = new List<SlotMatchDto>();
        foreach (var slot in slots)
        {
            var matches = faculty.Select(f => ComputeMatch(slot, f)).OrderByDescending(m => m.MatchScore).Take(5).ToList();
            for (int i = 0; i < matches.Count; i++)
            {
                var dto = matches[i];
                dto = dto with { Rank = i + 1 };
                var entity = new SlotCandidateMatch
                {
                    Id = Guid.NewGuid(), TeachingSlotId = slot.Id, FacultyProfileId = dto.FacultyProfileId,
                    MatchScore = dto.MatchScore, SpecializationScore = dto.SpecializationScore,
                    QualificationScore = dto.QualificationScore, ExperienceScore = dto.ExperienceScore,
                    PriorPerformanceScore = dto.PriorPerformanceScore, CompensationScore = dto.CompensationScore,
                    IsAvailabilityConfirmed = dto.IsAvailabilityConfirmed, MatchRationale = dto.MatchRationale,
                    Rank = dto.Rank, TenantId = tenantId
                };
                db.Set<SlotCandidateMatch>().Add(entity);
                results.Add(dto);
            }
        }
        await db.SaveChangesAsync(ct);
        return results;
    }

    public async Task<FacultyProfileDto> VetFacultyProfileAsync(Guid profileId, CancellationToken ct = default)
    {
        var profile = await db.Set<VisitingFacultyProfile>().FindAsync([profileId], ct)
            ?? throw new InvalidOperationException("Profile not found.");
        profile.ProfileStatus = profile.DocumentsVerified && profile.ReferencesChecked ? "ready_to_deploy" : "pending_documents";
        await db.SaveChangesAsync(ct);
        return new FacultyProfileDto(profile.Id, $"{profile.FirstName} {profile.LastName}", profile.HighestQualification, profile.HecEligibilityStatus, profile.ProfileStatus, profile.YearsOfTeachingExperience, profile.YearsOfIndustryExperience, profile.SpecializationsCsv, profile.DocumentsVerified, profile.ReferencesChecked, profile.VerdictForRehire);
    }

    public async Task<FacultyEngagementDto> CreateEngagementAsync(Guid slotId, Guid profileId, CancellationToken ct = default)
    {
        var slot = await db.Set<TeachingSlot>().FindAsync([slotId], ct) ?? throw new InvalidOperationException("Teaching slot not found.");
        var engagement = new FacultyEngagement
        {
            Id = Guid.NewGuid(), TeachingSlotId = slotId, FacultyProfileId = profileId,
            ContractNumber = $"VF-{DateTimeOffset.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpperInvariant()}",
            TotalCompensation = slot.CreditHours * slot.CompensationRate, StartDate = slot.StartDate,
            EndDate = slot.EndDate, Status = "active", ContractStatus = "draft", TenantId = slot.TenantId
        };
        db.Set<FacultyEngagement>().Add(engagement);
        slot.Status = "filled";
        await db.SaveChangesAsync(ct);
        return new FacultyEngagementDto(engagement.Id, slotId, profileId, engagement.ContractNumber, engagement.TotalCompensation, engagement.StartDate, engagement.EndDate, engagement.Status, engagement.ContractStatus);
    }

    public async Task<FacultyPerformanceDto> RatePerformanceAsync(Guid engagementId, CancellationToken ct = default)
    {
        var engagement = await db.Set<FacultyEngagement>().FindAsync([engagementId], ct) ?? throw new InvalidOperationException("Engagement not found.");
        var rating = new FacultyPerformanceRating
        {
            Id = Guid.NewGuid(), EngagementId = engagementId, AttendanceScore = 85, StudentFeedbackScore = 80,
            GradeComplianceScore = 90, OverallRating = 85m, DepartmentEndorsement = null, RehireRecommendation = "recommended",
            TenantId = engagement.TenantId
        };
        db.Set<FacultyPerformanceRating>().Add(rating);
        await db.SaveChangesAsync(ct);
        return new FacultyPerformanceDto(engagementId, rating.OverallRating, rating.AttendanceScore, rating.StudentFeedbackScore, rating.GradeComplianceScore, rating.RehireRecommendation, rating.DepartmentEndorsement);
    }

    public async Task<FacultyCompensationDto> ProcessCompensationAsync(Guid engagementId, CancellationToken ct = default)
    {
        var engagement = await db.Set<FacultyEngagement>().FindAsync([engagementId], ct) ?? throw new InvalidOperationException("Engagement not found.");
        var tax = engagement.TotalCompensation * 0.1m;
        var net = engagement.TotalCompensation - tax;
        var comp = new FacultyCompensation
        {
            Id = Guid.NewGuid(), EngagementId = engagementId, CreditHours = 3, PerCreditRate = engagement.TotalCompensation / 3,
            GrossAmount = engagement.TotalCompensation, TaxDeduction = tax, NetAmount = net,
            PaymentStatus = "pending", TenantId = engagement.TenantId
        };
        db.Set<FacultyCompensation>().Add(comp);
        await db.SaveChangesAsync(ct);
        return new FacultyCompensationDto(engagementId, comp.GrossAmount, comp.TaxDeduction, comp.NetAmount, comp.PaymentStatus, comp.VoucherReference);
    }

    public async Task<PoolAnalyticsDto> GetPoolAnalyticsAsync(Guid tenantId, CancellationToken ct = default)
    {
        var faculty = await db.Set<VisitingFacultyProfile>().IgnoreQueryFilters().Where(f => f.TenantId == tenantId).ToListAsync(ct);
        var slots = await db.Set<TeachingSlot>().IgnoreQueryFilters().Where(s => s.TenantId == tenantId).ToListAsync(ct);
        var specs = faculty.Where(f => !string.IsNullOrWhiteSpace(f.SpecializationsCsv)).SelectMany(f => f.SpecializationsCsv!.Split(',')).GroupBy(s => s.Trim()).ToDictionary(g => g.Key, g => g.Count());
        return new PoolAnalyticsDto(faculty.Count, faculty.Count(f => f.ProfileStatus == "ready_to_deploy"), faculty.Count(f => f.ProfileStatus == "pending_documents"), specs, slots.Count(s => s.Status == "open"), slots.Count(s => s.Status == "filled"), 5.2m, 15000m, 0.78m, specs.Where(kv => kv.Value < 3).Select(kv => kv.Key).ToList());
    }

    private static SlotMatchDto ComputeMatch(TeachingSlot slot, VisitingFacultyProfile faculty)
    {
        var spec = Math.Min(10, faculty.SpecializationsCsv?.Contains(slot.SpecializationRequired ?? "", StringComparison.OrdinalIgnoreCase) == true ? 10 : 5);
        var qual = faculty.HighestQualification.Equals("PhD", StringComparison.OrdinalIgnoreCase) ? 10 : faculty.HighestQualification.Equals("MS", StringComparison.OrdinalIgnoreCase) ? 7 : 5;
        var exp = Math.Min(10, faculty.YearsOfTeachingExperience);
        var perf = 7;
        var comp = faculty.PerCreditHourRate.HasValue && faculty.PerCreditHourRate.Value <= slot.CompensationRate ? 10 : 5;
        var score = (spec * 0.30m + qual * 0.25m + exp * 0.20m + perf * 0.15m + comp * 0.10m) / 10m;
        return new SlotMatchDto(slot.Id, faculty.Id, $"{faculty.FirstName} {faculty.LastName}", Math.Round(score, 2), 0, "Auto-matched", spec, qual, exp, perf, comp, true);
    }
}
