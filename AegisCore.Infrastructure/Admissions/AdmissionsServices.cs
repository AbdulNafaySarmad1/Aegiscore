using AegisCore.Application.Admissions;
using AegisCore.Application.Foundation;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Admissions;

public sealed class IBCCEquivalenceService : IIBCCEquivalenceService
{
    public decimal ConvertOLevelToMatric(decimal oLevelScore) => Math.Clamp(oLevelScore * 0.9m, 0m, 100m);
    public decimal ConvertALevelToFsc(decimal aLevelScore) => Math.Clamp(aLevelScore * 0.85m, 0m, 100m);
    public bool IsIBCCApplicable(AdmissionApplication application) => application.IbccEquivalenceApplied;
}

public sealed class MeritCalculationService(AegisDbContext db, IPolicyService policies) : IMeritCalculationService
{
    public async Task<MeritResult> CalculateAsync(AdmissionApplication application, Guid semesterId, CancellationToken ct = default)
    {
        if (application.MatricPercentage is < 0 or > 100 || application.FscPercentage is < 0 or > 100 || application.EntryTestScore is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(application), "Admission scores must be non-negative and percentages must be between 0 and 100.");
        }

        var matricWeight = await policies.GetAsync("admissions", "merit_weight_matric", 0.30m, ct);
        var fscWeight = await policies.GetAsync("admissions", "merit_weight_fsc", 0.30m, ct);
        var entryWeight = await policies.GetAsync("admissions", "merit_weight_entry_test", 0.40m, ct);
        var maxEntryScore = await policies.GetAsync("admissions", "entry_test_max_score", 100m, ct);

        var matric = (application.MatricPercentage ?? 0m) * matricWeight;
        var fsc = (application.FscPercentage ?? 0m) * fscWeight;
        if (maxEntryScore <= 0)
        {
            throw new InvalidOperationException("Admission entry-test maximum score must be greater than zero.");
        }

        var entry = Math.Clamp((application.EntryTestScore ?? 0) / maxEntryScore * 100m, 0m, 100m) * entryWeight;
        var score = Math.Round(matric + fsc + entry, 2, MidpointRounding.AwayFromZero);

        return new MeritResult(application.Id, score, 0, new MeritExplanation(Math.Round(matric, 2), Math.Round(fsc, 2), Math.Round(entry, 2)));
    }

    public async Task<IReadOnlyList<MeritResult>> GenerateMeritListAsync(Guid programId, Guid semesterId, string quotaType, CancellationToken ct = default)
    {
        var applications = await db.AdmissionApplications
            .Where(a => a.ProgramId == programId && a.QuotaType == quotaType && a.ApplicationStatus == "verified")
            .OrderBy(a => a.SubmittedAt)
            .ToListAsync(ct);
        var submittedAt = applications.ToDictionary(a => a.Id, a => a.SubmittedAt);

        var scored = new List<MeritResult>(applications.Count);
        foreach (var application in applications)
        {
            scored.Add(await CalculateAsync(application, semesterId, ct));
        }

        return scored
            .OrderByDescending(r => r.MeritScore)
            .ThenBy(r => submittedAt[r.ApplicationId])
            .Select((r, index) => r with { MeritRank = index + 1 })
            .ToList();
    }

    public async Task<MeritExplanation> ExplainMeritAsync(Guid applicationId, CancellationToken ct = default)
    {
        var application = await db.AdmissionApplications.FindAsync([applicationId], ct)
            ?? throw new KeyNotFoundException("Application was not found.");

        return (await CalculateAsync(application, Guid.Empty, ct)).Explanation;
    }
}

public sealed class SeatAllocationService(AegisDbContext db) : ISeatAllocationService
{
    public async Task<IReadOnlyList<SeatAllocation>> AllocateAsync(Guid meritListId, CancellationToken ct = default)
    {
        var list = await db.MeritLists.Include(m => m.Entries).FirstOrDefaultAsync(m => m.Id == meritListId, ct)
            ?? throw new KeyNotFoundException("Merit list was not found.");

        var ordered = list.Entries.OrderBy(e => e.MeritRank).ToList();
        var allocations = new List<SeatAllocation>();

        foreach (var entry in ordered)
        {
            var allocated = entry.MeritRank <= list.TotalSeats;
            entry.SeatAllocated = allocated;
            entry.AllocationStatus = allocated ? "allocated" : "waitlist";

            if (allocated && !await db.SeatAllocations.AnyAsync(a => a.MeritListEntryId == entry.Id, ct))
            {
                allocations.Add(new SeatAllocation
                {
                    Id = Guid.NewGuid(),
                    MeritListEntryId = entry.Id,
                    ApplicationId = entry.ApplicationId,
                    SeatType = list.QuotaType == "self_finance" ? "self_finance" : "regular"
                });
            }
        }

        db.SeatAllocations.AddRange(allocations);
        await db.SaveChangesAsync(ct);
        return allocations;
    }
}
