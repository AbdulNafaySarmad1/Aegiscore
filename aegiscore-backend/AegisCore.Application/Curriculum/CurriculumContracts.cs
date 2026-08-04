using AegisCore.Domain.Entities;

namespace AegisCore.Application.Curriculum;

public interface ICurriculumService
{
    int TotalCredits(int theoryCredits, int labCredits);
    bool CanAssignWorkload(IEnumerable<FacultyWorkload> existing, int newHours, int maxHours = 12, int maxSections = 4);
    IReadOnlyList<OfflineAttendanceEvent> FilterSyncableAttendance(IEnumerable<OfflineAttendanceEvent> events, DateTimeOffset now);
    Task<CurriculumGraphDto> GetGraphAsync(Guid curriculumVersionId, CancellationToken ct = default);
    Task<int> SyncOfflineAttendanceAsync(IEnumerable<OfflineAttendanceEvent> events, CancellationToken ct = default);
}

public sealed record CurriculumGraphDto(IReadOnlyList<CurriculumCourseNode> Courses, IReadOnlyList<CurriculumEdge> Prerequisites);
public sealed record CurriculumCourseNode(Guid Id, Guid CourseId, string Code, string Name, int Semester, int TheoryCredits, int LabCredits, string[] Clos);
public sealed record CurriculumEdge(Guid CourseId, Guid PrerequisiteCourseId, string Relationship);
