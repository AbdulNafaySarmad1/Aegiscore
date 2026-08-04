namespace AegisCore.Application.Hr;

public interface IVisitingFacultyService
{
    Task<IReadOnlyList<SlotMatchDto>> MatchFacultyToSlotsAsync(Guid tenantId, CancellationToken ct = default);
    Task<FacultyProfileDto> VetFacultyProfileAsync(Guid profileId, CancellationToken ct = default);
    Task<FacultyEngagementDto> CreateEngagementAsync(Guid slotId, Guid profileId, CancellationToken ct = default);
    Task<FacultyPerformanceDto> RatePerformanceAsync(Guid engagementId, CancellationToken ct = default);
    Task<FacultyCompensationDto> ProcessCompensationAsync(Guid engagementId, CancellationToken ct = default);
    Task<PoolAnalyticsDto> GetPoolAnalyticsAsync(Guid tenantId, CancellationToken ct = default);
}

public sealed record SlotMatchDto(
    Guid TeachingSlotId,
    Guid FacultyProfileId,
    string FacultyName,
    decimal MatchScore,
    int Rank,
    string? MatchRationale,
    int SpecializationScore,
    int QualificationScore,
    int ExperienceScore,
    int PriorPerformanceScore,
    int CompensationScore,
    bool IsAvailabilityConfirmed);

public sealed record FacultyProfileDto(
    Guid Id,
    string FullName,
    string HighestQualification,
    string HecEligibilityStatus,
    string ProfileStatus,
    int YearsOfTeachingExperience,
    int? YearsOfIndustryExperience,
    string? SpecializationsCsv,
    bool DocumentsVerified,
    bool ReferencesChecked,
    string? VerdictForRehire);

public sealed record FacultyEngagementDto(
    Guid Id,
    Guid TeachingSlotId,
    Guid FacultyProfileId,
    string ContractNumber,
    decimal TotalCompensation,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    string Status,
    string? ContractStatus);

public sealed record FacultyPerformanceDto(
    Guid EngagementId,
    decimal OverallRating,
    int AttendanceScore,
    int StudentFeedbackScore,
    int GradeComplianceScore,
    string RehireRecommendation,
    string? DepartmentEndorsement);

public sealed record FacultyCompensationDto(
    Guid EngagementId,
    decimal GrossAmount,
    decimal TaxDeduction,
    decimal NetAmount,
    string PaymentStatus,
    string? VoucherReference);

public sealed record PoolAnalyticsDto(
    int TotalFaculty,
    int ReadyToDeploy,
    int PendingDocuments,
    Dictionary<string, int> SpecializationCounts,
    int OpenSlots,
    int FilledSlots,
    decimal AverageTimeToFillDays,
    decimal AverageCostPerCreditHour,
    decimal RehireRate,
    List<string> ShortageSpecializations);
