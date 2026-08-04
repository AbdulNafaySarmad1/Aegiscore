namespace AegisCore.Application.StudentLifecycle;

public interface IGamificationService
{
    Task<IReadOnlyList<AchievementDetectedDto>> DetectAchievementsAsync(Guid tenantId, CancellationToken ct = default);
    Task<BadgeAwardDto> IssueBadgeAsync(Guid badgeDefinitionId, Guid studentId, CancellationToken ct = default);
    Task<IReadOnlyList<DeansListEntryDto>> ProcessDeansListAsync(Guid semesterId, CancellationToken ct = default);
    Task<LeaderboardDto> GenerateLeaderboardAsync(string scope, Guid tenantId, CancellationToken ct = default);
    Task<AchievementRoadmapDto> GetStudentRoadmapAsync(Guid studentId, CancellationToken ct = default);
    Task<GamificationAnalyticsDto> GetAnalyticsAsync(Guid tenantId, CancellationToken ct = default);
}

public sealed record AchievementDetectedDto(
    string AchievementType,
    Guid StudentId,
    string AchievementDetail,
    bool BadgeMatchFound,
    string? MatchingBadgeName);

public sealed record BadgeAwardDto(
    Guid Id,
    Guid BadgeDefinitionId,
    string BadgeName,
    Guid StudentId,
    string? AchievementDetail,
    Guid? SemesterId,
    DateTimeOffset AwardedAt,
    bool ParentNotified);

public sealed record DeansListEntryDto(
    Guid StudentId,
    Guid SemesterId,
    decimal Cgpa,
    string Status,
    bool ParentNotified,
    string? FormalLetterUrl);

public sealed record LeaderboardDto(
    string Scope,
    string Metric,
    Guid SemesterId,
    List<LeaderboardRowDto> Entries,
    int TotalEntries);

public sealed record LeaderboardRowDto(
    int Rank,
    Guid StudentId,
    string? StudentName,
    decimal Score,
    bool IsAnonymized);

public sealed record AchievementRoadmapDto(
    Guid StudentId,
    List<NextBadgeDto> NextBadges,
    List<InProgressDto> InProgress);

public sealed record NextBadgeDto(
    string BadgeName,
    decimal ProgressPercent,
    string? RequirementDetail);

public sealed record InProgressDto(
    string Milestone,
    decimal ProgressPercent);

public sealed record GamificationAnalyticsDto(
    int TotalBadgesIssued,
    int DistinctStudentsWithBadges,
    int StudentsWithZeroBadges,
    int DeansListCountThisSemester,
    int DeansListCountLastSemester,
    Dictionary<string, int> MostPopularBadges);
