using AegisCore.Domain.Entities;
using AegisCore.Infrastructure.Curriculum;
using Microsoft.Extensions.Logging.Abstractions;

namespace AegisCore.Tests;

public sealed class CurriculumTests
{
    [Fact]
    public void TotalCredits_Adds_Theory_And_Lab()
    {
        var service = NewService();

        Assert.Equal(4, service.TotalCredits(3, 1));
    }

    [Fact]
    public void Workload_Blocks_When_Hours_Exceeded()
    {
        var service = NewService();
        var existing = new[] { new FacultyWorkload { CourseId = Guid.NewGuid(), HoursPerWeek = 10 } };

        Assert.False(service.CanAssignWorkload(existing, 3));
    }

    [Fact]
    public void OfflineAttendance_Dedupes_And_Drops_Expired()
    {
        var service = NewService();
        var now = DateTimeOffset.UtcNow;
        var events = new[]
        {
            new OfflineAttendanceEvent { ClientEventId = "a", WasOfflineAt = now.AddMinutes(-10), ReceivedAt = now.AddSeconds(-1) },
            new OfflineAttendanceEvent { ClientEventId = "a", WasOfflineAt = now.AddMinutes(-10), ReceivedAt = now },
            new OfflineAttendanceEvent { ClientEventId = "b", WasOfflineAt = now.AddMinutes(-31), ReceivedAt = now }
        };

        var syncable = service.FilterSyncableAttendance(events, now);

        Assert.Single(syncable);
        Assert.Equal("a", syncable[0].ClientEventId);
        Assert.Equal(now, syncable[0].ReceivedAt);
    }

    private static CurriculumService NewService()
        => new(null!, NullLogger<CurriculumService>.Instance);
}
