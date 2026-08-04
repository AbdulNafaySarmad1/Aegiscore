using AegisCore.Application.Curriculum;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Curriculum;

public sealed class CurriculumService(AegisDbContext db, ILogger<CurriculumService> logger) : ICurriculumService
{
    private static readonly TimeSpan OfflineWindow = TimeSpan.FromMinutes(30);

    public int TotalCredits(int theoryCredits, int labCredits)
    {
        if (theoryCredits < 0 || labCredits < 0) throw new ArgumentOutOfRangeException(nameof(theoryCredits));
        return theoryCredits + labCredits;
    }

    public bool CanAssignWorkload(IEnumerable<FacultyWorkload> existing, int newHours, int maxHours = 12, int maxSections = 4)
    {
        if (newHours < 0 || maxHours < 0 || maxSections < 0) throw new ArgumentOutOfRangeException(nameof(newHours));
        var active = existing.Where(w => w.EffectiveTo is null || w.EffectiveTo > DateTime.UtcNow).ToList();
        return active.Sum(w => w.HoursPerWeek) + newHours <= maxHours && active.Select(w => w.CourseId).Distinct().Count() + 1 <= maxSections;
    }

    public IReadOnlyList<OfflineAttendanceEvent> FilterSyncableAttendance(IEnumerable<OfflineAttendanceEvent> events, DateTimeOffset now)
        => events
            .Where(e => !string.IsNullOrWhiteSpace(e.ClientEventId) && e.WasOfflineAt <= now && now - e.WasOfflineAt <= OfflineWindow)
            .GroupBy(e => e.ClientEventId)
            .Select(g => g.OrderByDescending(e => e.ReceivedAt).First())
            .ToList();

    public async Task<CurriculumGraphDto> GetGraphAsync(Guid curriculumVersionId, CancellationToken ct = default)
    {
        var courses = await db.CurriculumCourses
            .Where(c => c.CurriculumVersionId == curriculumVersionId)
            .Join(db.Courses, cc => cc.CourseId, c => c.Id, (cc, c) => new CurriculumCourseNode(
                cc.Id,
                cc.CourseId,
                c.Code ?? string.Empty,
                c.Title,
                cc.Semester,
                cc.TheoryCreditHours,
                cc.LabCreditHours,
                cc.MappedClosCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)))
            .ToListAsync(ct);

        var edges = await db.CurriculumPrerequisites
            .Where(p => p.CurriculumVersionId == curriculumVersionId)
            .Select(p => new CurriculumEdge(p.CourseId, p.PrerequisiteCourseId, p.Relationship))
            .ToListAsync(ct);

        return new CurriculumGraphDto(courses, edges);
    }

    public async Task<int> SyncOfflineAttendanceAsync(IEnumerable<OfflineAttendanceEvent> events, CancellationToken ct = default)
    {
        var syncable = FilterSyncableAttendance(events, DateTimeOffset.UtcNow);
        if (syncable.Count == 0) return 0;

        var clientIds = syncable.Select(e => e.ClientEventId).ToArray();
        var existing = await db.OfflineAttendanceEvents
            .Where(e => clientIds.Contains(e.ClientEventId))
            .Select(e => e.ClientEventId)
            .ToListAsync(ct);

        var existingSet = existing.ToHashSet(StringComparer.Ordinal);
        var rows = syncable.Where(e => !existingSet.Contains(e.ClientEventId)).Select(e =>
        {
            e.Id = e.Id == default ? Guid.NewGuid() : e.Id;
            e.SyncedAt = DateTimeOffset.UtcNow;
            return e;
        }).ToList();

        db.OfflineAttendanceEvents.AddRange(rows);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Synced {Count} offline attendance events", rows.Count);
        return rows.Count;
    }
}
