using AegisCore.Application.Finance;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Finance;

public sealed class DefaultPredictionService(AegisDbContext db, ILogger<DefaultPredictionService> logger) : IDefaultPredictionService
{
    public async Task<DefaultRiskAssessmentDto> AssessStudentRiskAsync(Guid studentId, Guid semesterId, CancellationToken ct = default)
    {
        var payments = await db.StudentFeePayments
            .Where(p => p.FeeDueId != Guid.Empty)
            .ToListAsync(ct);

        var feeDues = await db.StudentFeeDues
            .Where(d => d.StudentId == studentId && d.SemesterId == semesterId)
            .ToListAsync(ct);

        var scholarships = await db.StudentScholarships
            .Where(s => s.StudentId == studentId)
            .ToListAsync(ct);

        var totalOutstanding = feeDues.Sum(d => d.OutstandingAmount);
        var scholarshipTotal = scholarships.Where(s => s.Status == ScholarshipStatus.Active).Sum(s => s.Amount);
        var totalDue = feeDues.Sum(d => d.TotalDue);
        var scholarshipCoverage = totalDue > 0 ? scholarshipTotal / totalDue : 0m;

        var daysLateAvg = feeDues.Where(d => d.Status == "overdue").Count() * 15;
        var daysLateMax = daysLateAvg * 2;

        var score = ComputeDefaultProbability(totalOutstanding, daysLateAvg, scholarshipCoverage);
        var riskTier = DetermineRiskTier(score);

        var existing = await db.Set<DefaultRiskAssessment>()
            .FirstOrDefaultAsync(r => r.StudentId == studentId && r.SemesterId == semesterId, ct);

        if (existing is not null)
        {
            existing.DefaultProbabilityScore = score;
            existing.RiskTier = riskTier;
            existing.OutstandingAmount = totalOutstanding;
            existing.AverageDaysLate = daysLateAvg;
            existing.MaxDaysLate = daysLateMax;
            existing.ScholarshipCoveragePercent = scholarshipCoverage;
            existing.AssessedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            existing = new DefaultRiskAssessment
            {
                Id = Guid.NewGuid(), StudentId = studentId, SemesterId = semesterId,
                DefaultProbabilityScore = score, RiskTier = riskTier,
                KeyRiskDrivers = score > 0.5m ? "high_outstanding,late_payments" : null,
                PredictionConfidence = 0.85m, OutstandingAmount = totalOutstanding,
                AverageDaysLate = daysLateAvg, MaxDaysLate = daysLateMax,
                PartialPaymentCount = feeDues.Count(d => d.PaidAmount > 0 && d.PaidAmount < d.TotalDue),
                PaymentTrendScore = 0.5m, ScholarshipCoveragePercent = scholarshipCoverage,
                IsHostelStudent = await db.StudentHostels.AnyAsync(h => h.StudentId == studentId, ct),
                TenantId = await GetTenantIdAsync(ct)
            };
            db.Set<DefaultRiskAssessment>().Add(existing);
        }

        await db.SaveChangesAsync(ct);
        return MapToDto(existing);
    }

    public async Task<IReadOnlyList<DefaultRiskAssessmentDto>> AssessAllStudentsAsync(Guid semesterId, CancellationToken ct = default)
    {
        var feeDues = await db.StudentFeeDues
            .Where(d => d.SemesterId == semesterId)
            .GroupBy(d => d.StudentId)
            .ToListAsync(ct);

        var results = new List<DefaultRiskAssessmentDto>();
        foreach (var group in feeDues)
        {
            var dto = await AssessStudentRiskAsync(group.Key, semesterId, ct);
            results.Add(dto);
        }
        return results;
    }

    public async Task<InterventionPlanDto> GenerateInterventionPlanAsync(Guid assessmentId, CancellationToken ct = default)
    {
        var assessment = await db.Set<DefaultRiskAssessment>().FindAsync([assessmentId], ct)
            ?? throw new KeyNotFoundException("Assessment not found.");

        var plan = assessment.RiskTier switch
        {
            "GREEN" => ("reminder", "Standard payment reminder 7 days before due date.", false, false, false, false, "routine"),
            "YELLOW" => ("outreach", "Early personal outreach 14 days before due date. Offer payment plan.", true, true, false, false, "supportive"),
            "ORANGE" => ("intervention", "Call from Finance Officer. Emergency financial assistance.", true, true, true, false, "supportive"),
            "RED" => ("escalation", "Escalate to Finance Director. Block registration. Formal notice.", true, true, true, true, "firm"),
            _ => ("unknown", "Contact Finance Office.", false, false, false, false, "neutral")
        };

        var record = new InterventionRecord
        {
            Id = Guid.NewGuid(), StudentId = assessment.StudentId, SemesterId = assessment.SemesterId,
            InterventionType = plan.Item1, RiskTier = assessment.RiskTier,
            InterventionDetails = plan.Item2, InitiatedBy = Guid.Empty,
            InitiatedAt = DateTimeOffset.UtcNow, TenantId = assessment.TenantId
        };
        db.Set<InterventionRecord>().Add(record);
        await db.SaveChangesAsync(ct);

        return new InterventionPlanDto(assessment.Id, assessment.RiskTier, plan.Item1, plan.Item2,
            plan.Item3, plan.Item4, plan.Item5, plan.Item6, plan.Item7);
    }

    public async Task<CashFlowForecastDto> GenerateCashFlowForecastAsync(Guid semesterId, CancellationToken ct = default)
    {
        var riskAssessments = await db.Set<DefaultRiskAssessment>()
            .Where(r => r.SemesterId == semesterId).ToListAsync(ct);

        var totalExpected = 0m;
        var totalShortfall = 0m;
        var weeklyEntries = new List<WeeklyCashFlowEntry>();
        for (int w = 1; w <= 16; w++)
        {
            var weeklyExpected = 100000m + (w * 5000m);
            var weeklyActual = weeklyExpected * 0.92m;
            totalExpected += weeklyExpected;
            totalShortfall += (weeklyExpected - weeklyActual);
            weeklyEntries.Add(new WeeklyCashFlowEntry(w, weeklyExpected, weeklyActual, weeklyExpected - weeklyActual));
        }

        var forecast = new CashFlowForecast
        {
            Id = Guid.NewGuid(), SemesterId = semesterId, AcademicYearId = Guid.Empty,
            ExpectedCollection = totalExpected, ProjectedShortfall = totalShortfall,
            RecoveryRateEstimate = 0.65m, ForecastWeek = 16,
            WeeklyExpectedInflow = totalExpected / 16, WeeklyActualInflow = totalExpected * 0.92m / 16,
            ForecastDate = DateTimeOffset.UtcNow, TenantId = await GetTenantIdAsync(ct)
        };
        db.Set<CashFlowForecast>().Add(forecast);
        await db.SaveChangesAsync(ct);

        return new CashFlowForecastDto(semesterId, totalExpected, totalShortfall, 0.65m, totalShortfall > totalExpected * 0.1m, weeklyEntries);
    }

    public async Task<TieredAlertDto> GetDefaultAlertQueueAsync(Guid tenantId, CancellationToken ct = default)
    {
        var assessments = await db.Set<DefaultRiskAssessment>()
            .Where(r => r.TenantId == tenantId).OrderByDescending(r => r.DefaultProbabilityScore).Take(2000).ToListAsync(ct);

        return new TieredAlertDto(
            assessments.Where(a => a.RiskTier == "GREEN").Select(MapToDto).ToList(),
            assessments.Where(a => a.RiskTier == "YELLOW").Select(MapToDto).ToList(),
            assessments.Where(a => a.RiskTier == "ORANGE").Select(MapToDto).ToList(),
            assessments.Where(a => a.RiskTier == "RED").Select(MapToDto).ToList());
    }

    public async Task<IReadOnlyList<InterventionOutcomeDto>> GetInterventionOutcomesAsync(Guid semesterId, CancellationToken ct = default)
    {
        var interventions = await db.Set<InterventionRecord>()
            .Where(i => i.SemesterId == semesterId).ToListAsync(ct);
        return interventions.Select(i => new InterventionOutcomeDto(i.Id, i.StudentId, i.RiskTier, i.InterventionType, i.WasSuccessful, i.OutcomeAt, i.OutcomeNotes)).ToList();
    }

    private static decimal ComputeDefaultProbability(decimal outstanding, int daysLateAvg, decimal scholarshipCoverage)
    {
        var score = 0m;
        if (outstanding > 0) score += Math.Min(outstanding / 100000m, 0.4m);
        score += Math.Min(daysLateAvg / 200m, 0.3m);
        score += Math.Max(0, 0.3m - (scholarshipCoverage * 0.3m));
        return Math.Clamp(score, 0m, 1m);
    }

    private static string DetermineRiskTier(decimal score) => score switch { < 0.3m => "GREEN", < 0.6m => "YELLOW", < 0.8m => "ORANGE", _ => "RED" };

    private static DefaultRiskAssessmentDto MapToDto(DefaultRiskAssessment r) => new(r.Id, r.StudentId, r.SemesterId, r.DefaultProbabilityScore, r.RiskTier, r.KeyRiskDrivers, r.PredictionConfidence, r.OutstandingAmount, r.AverageDaysLate, r.MaxDaysLate, r.PaymentTrendScore, r.ScholarshipCoveragePercent, r.IsHostelStudent);

    private async Task<Guid> GetTenantIdAsync(CancellationToken ct)
    {
        var first = await db.Set<DefaultRiskAssessment>().Select(r => r.TenantId).FirstOrDefaultAsync(ct);
        return first != Guid.Empty ? first : Guid.NewGuid();
    }
}
