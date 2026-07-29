using AegisCore.Domain.Entities;

namespace AegisCore.Application.Examination;

public interface IGradingService
{
    Task CalculateCourseGradeAsync(Guid courseId, Guid studentId, CancellationToken ct = default);
    Task<SemesterGpa> CalculateSGPAAsync(Guid studentId, Guid semesterId, CancellationToken ct = default);
    Task<decimal> CalculateCGPAAsync(Guid studentId, CancellationToken ct = default);
    Task ApplyGracemarksAsync(Guid courseId, IEnumerable<GracemarkEntry> entries, Guid actorId, CancellationToken ct = default);
}

public interface ITranscriptService
{
    Task<Stream> GenerateTranscriptAsync(Guid studentId, CancellationToken ct = default);
    Task<string> RequestTranscriptAsync(Guid studentId, Guid requestedBy, string reason, CancellationToken ct = default);
    Task<bool> VerifyTranscriptAsync(string transcriptHash, CancellationToken ct = default);
}

public sealed record SemesterGpa(decimal Gpa, int TotalCredits, int CompletedCredits, IReadOnlyList<CourseGrade> Grades);
public sealed record GracemarkEntry(Guid StudentId, int Marks);
