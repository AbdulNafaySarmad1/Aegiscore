using AegisCore.Application.StudentLifecycle;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.StudentLifecycle;

public sealed class GamificationService(AegisDbContext db, ILogger<GamificationService> logger) : IGamificationService
{
    public async Task<IReadOnlyList<AchievementDetectedDto>> DetectAchievementsAsync(Guid tenantId, CancellationToken ct = default)
    {
        var badges = await db.Set<BadgeDefinition>().Where(b => b.TenantId == tenantId && b.IsActive).ToListAsync(ct);
        var results = new List<AchievementDetectedDto>();
        foreach (var badge in badges)
        {
            results.Add(new AchievementDetectedDto(badge.Category, Guid.Empty, badge.Description ?? badge.BadgeName, true, badge.BadgeName));
        }
        return results;
    }

    public async Task<BadgeAwardDto> IssueBadgeAsync(Guid badgeDefinitionId, Guid studentId, CancellationToken ct = default)
    {
        var badge = await db.Set<BadgeDefinition>().FindAsync([badgeDefinitionId], ct) ?? throw new InvalidOperationException("Badge not found.");
        var award = new BadgeAward
        {
            Id = Guid.NewGuid(), BadgeDefinitionId = badgeDefinitionId, StudentId = studentId,
            AchievementDetail = $"Earned {badge.BadgeName}", VerificationHash = Guid.NewGuid().ToString("N"),
            AwardedAt = DateTimeOffset.UtcNow, TenantId = badge.TenantId
        };
        db.Set<BadgeAward>().Add(award);
        await db.SaveChangesAsync(ct);
        return new BadgeAwardDto(award.Id, badgeDefinitionId, badge.BadgeName, studentId, award.AchievementDetail, null, award.AwardedAt, false);
    }

    public async Task<IReadOnlyList<DeansListEntryDto>> ProcessDeansListAsync(Guid semesterId, CancellationToken ct = default)
    {
        var entries = await db.Set<DeansListEntry>().Where(e => e.Status == "pending_verification").ToListAsync(ct);
        foreach (var entry in entries)
        {
            entry.Status = "confirmed";
            entry.FormalLetterUrl = $"/letters/deans-list-{entry.StudentId}-{semesterId}.pdf";
        }
        await db.SaveChangesAsync(ct);
        return entries.Select(e => new DeansListEntryDto(e.StudentId, e.SemesterId, e.Cgpa, e.Status, e.ParentNotified, e.FormalLetterUrl)).ToList();
    }

    public async Task<LeaderboardDto> GenerateLeaderboardAsync(string scope, Guid tenantId, CancellationToken ct = default)
    {
        var entries = await db.Set<LeaderboardEntry>().Where(l => l.TenantId == tenantId && l.Scope == scope).OrderBy(l => l.Rank).Take(100).ToListAsync(ct);
        var rows = entries.Select(e => new LeaderboardRowDto(e.Rank, e.StudentId, e.IsAnonymized ? null : $"Student-{e.StudentId.ToString()[..8]}", e.Score, e.IsAnonymized)).ToList();
        return new LeaderboardDto(scope, "cgpa", Guid.Empty, rows, rows.Count);
    }

    public async Task<AchievementRoadmapDto> GetStudentRoadmapAsync(Guid studentId, CancellationToken ct = default)
    {
        var roadmaps = await db.Set<AchievementRoadmap>().Where(r => r.StudentId == studentId).ToListAsync(ct);
        var nextBadges = roadmaps.Select(r => new NextBadgeDto(r.NextMilestone ?? "Next Badge", r.ProgressPercent, "Complete required actions to earn")).ToList();
        return new AchievementRoadmapDto(studentId, nextBadges, []);
    }

    public async Task<GamificationAnalyticsDto> GetAnalyticsAsync(Guid tenantId, CancellationToken ct = default)
    {
        var awards = await db.Set<BadgeAward>().Where(a => a.TenantId == tenantId).ToListAsync(ct);
        var deansList = await db.Set<DeansListEntry>().Where(e => e.TenantId == tenantId).ToListAsync(ct);
        return new GamificationAnalyticsDto(awards.Count, awards.Select(a => a.StudentId).Distinct().Count(), 0, deansList.Count(e => e.Status == "confirmed"), 0, []);
    }
}
