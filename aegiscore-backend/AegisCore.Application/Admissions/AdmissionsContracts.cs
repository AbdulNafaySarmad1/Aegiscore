using AegisCore.Domain.Entities;

namespace AegisCore.Application.Admissions;

public interface IMeritCalculationService
{
    Task<MeritResult> CalculateAsync(AdmissionApplication application, Guid semesterId, CancellationToken ct = default);
    Task<IReadOnlyList<MeritResult>> GenerateMeritListAsync(Guid programId, Guid semesterId, string quotaType, CancellationToken ct = default);
    Task<MeritExplanation> ExplainMeritAsync(Guid applicationId, CancellationToken ct = default);
}

public interface IIBCCEquivalenceService
{
    decimal ConvertOLevelToMatric(decimal oLevelScore);
    decimal ConvertALevelToFsc(decimal aLevelScore);
    bool IsIBCCApplicable(AdmissionApplication application);
}

public interface ISeatAllocationService
{
    Task<IReadOnlyList<SeatAllocation>> AllocateAsync(Guid meritListId, CancellationToken ct = default);
}

public sealed record MeritResult(Guid ApplicationId, decimal MeritScore, int MeritRank, MeritExplanation Explanation);
public sealed record MeritExplanation(decimal MatricContribution, decimal FscContribution, decimal EntryTestContribution);
