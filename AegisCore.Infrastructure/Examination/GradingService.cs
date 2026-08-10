using System.Security.Cryptography;
using System.Text;
using AegisCore.Application.Examination;
using AegisCore.Application.Foundation;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Examination;

public sealed class GradingService(AegisDbContext db, IPolicyService policies, IAuditService audit) : IGradingService
{
    public async Task CalculateCourseGradeAsync(Guid courseId, Guid studentId, CancellationToken ct = default)
    {
        var scheme = await db.AssessmentSchemes
            .Include(s => s.Components)
            .FirstOrDefaultAsync(s => s.CourseId == courseId, ct)
            ?? throw new InvalidOperationException("Assessment scheme was not found for this course.");

        if (scheme.Components.Count == 0 || scheme.Components.Sum(c => c.WeightPercentage) != 100
            || scheme.Components.Any(c => c.TotalMarks <= 0 || c.WeightPercentage < 0))
        {
            throw new InvalidOperationException("Assessment scheme must contain positive marks and weights totaling 100%.");
        }

        var marks = await db.CourseMarks
            .Include(m => m.AssessmentComponent)
            .Where(m => m.CourseId == courseId && m.StudentId == studentId)
            .ToListAsync(ct);

        if (marks.Count == 0)
        {
            throw new InvalidOperationException("No marks exist for this student/course.");
        }

        if (scheme.Components.Any(component => marks.All(mark => mark.AssessmentComponentId != component.Id)))
        {
            throw new InvalidOperationException("All assessment components must have marks before grading.");
        }

        foreach (var mark in marks)
        {
            if (mark.ObtainedMarks < 0 || mark.ObtainedMarks > mark.AssessmentComponent.TotalMarks)
            {
                throw new InvalidOperationException("Course marks are outside component bounds.");
            }
        }

        var weighted = marks.Sum(m => (decimal)m.ObtainedMarks / m.AssessmentComponent.TotalMarks * m.AssessmentComponent.WeightPercentage);
        var band = await db.GradeBands
            .OrderByDescending(g => g.PercentageFrom)
            .FirstOrDefaultAsync(g => weighted >= g.PercentageFrom && weighted < g.PercentageTo + 1m, ct);

        var grade = await db.CourseGrades.FirstOrDefaultAsync(g => g.CourseId == courseId && g.StudentId == studentId, ct)
            ?? new CourseGrade { Id = Guid.NewGuid(), CourseId = courseId, SemesterId = scheme.SemesterId, StudentId = studentId };

        grade.SemesterId = scheme.SemesterId;
        grade.TotalMarksObtained = marks.Sum(m => m.ObtainedMarks);
        grade.PercentageObtained = Math.Round(weighted, 2, MidpointRounding.AwayFromZero);
        grade.LetterGrade = band?.LetterGrade ?? "F";
        grade.GradePoints = band?.GpaPoints ?? 0m;
        grade.IsPassing = grade.GradePoints > 0m;
        grade.ResultStatus = "calculated";
        grade.CalculatedAt = DateTimeOffset.UtcNow;

        if (db.Entry(grade).State == EntityState.Detached)
        {
            db.CourseGrades.Add(grade);
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<SemesterGpa> CalculateSGPAAsync(Guid studentId, Guid semesterId, CancellationToken ct = default)
    {
        var grades = await db.CourseGrades
            .Where(g => g.StudentId == studentId && (semesterId == Guid.Empty || g.SemesterId == semesterId))
            .ToListAsync(ct);
        var courseIds = grades.Select(g => g.CourseId).ToHashSet();
        var courses = await db.Courses.Where(c => courseIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, ct);
        var totalCredits = grades.Sum(g => courses.TryGetValue(g.CourseId, out var c) ? c.CreditHours : 0);
        var weighted = grades.Sum(g => g.GradePoints * (courses.TryGetValue(g.CourseId, out var c) ? c.CreditHours : 0));
        var gpa = totalCredits == 0 ? 0m : Math.Round(weighted / totalCredits, 2, MidpointRounding.AwayFromZero);

        return new SemesterGpa(gpa, totalCredits, grades.Where(g => g.IsPassing).Sum(g => courses.TryGetValue(g.CourseId, out var c) ? c.CreditHours : 0), grades);
    }

    public async Task<decimal> CalculateCGPAAsync(Guid studentId, CancellationToken ct = default)
        => (await CalculateSGPAAsync(studentId, Guid.Empty, ct)).Gpa;

    public async Task ApplyGracemarksAsync(Guid courseId, IEnumerable<GracemarkEntry> entries, Guid actorId, CancellationToken ct = default)
    {
        var max = await policies.GetAsync("examination", "grace_marks_max", 2, ct);
        foreach (var entry in entries)
        {
            if (entry.Marks < 0 || entry.Marks > max)
            {
                throw new InvalidOperationException("Grace marks exceed policy limit.");
            }

            var mark = await db.CourseMarks
                .Include(m => m.AssessmentComponent)
                .Where(m => m.CourseId == courseId && m.StudentId == entry.StudentId)
                .OrderByDescending(m => m.AssessmentComponent.WeightPercentage)
                .FirstOrDefaultAsync(ct);

            if (mark is not null)
            {
                if (mark.ObtainedMarks + entry.Marks > mark.AssessmentComponent.TotalMarks)
                {
                    throw new InvalidOperationException("Grace marks would exceed the component total.");
                }

                mark.ObtainedMarks += entry.Marks;
                await CalculateCourseGradeAsync(courseId, entry.StudentId, ct);
                await audit.LogAsync(new AuditEntry(actorId, null, "examination.grace_marks_applied", "examination", "CourseMark", mark.Id.ToString(), null, entry.Marks.ToString(), null, null, null), ct);
            }
        }
    }
}

public sealed class TranscriptService(AegisDbContext db, IStorageProvider storage) : ITranscriptService
{
    public async Task<Stream> GenerateTranscriptAsync(Guid studentId, CancellationToken ct = default)
    {
        var grades = await db.CourseGrades.Where(g => g.StudentId == studentId).OrderBy(g => g.CourseId).ToListAsync(ct);
        var text = string.Join(Environment.NewLine, grades.Select(g => $"{g.CourseId},{g.LetterGrade},{g.GradePoints}"));
        return new MemoryStream(Encoding.UTF8.GetBytes(text));
    }

    public async Task<string> RequestTranscriptAsync(Guid studentId, Guid requestedBy, string reason, CancellationToken ct = default)
    {
        await using var stream = await GenerateTranscriptAsync(studentId, ct);
        using var sha = SHA256.Create();
        var hash = Convert.ToHexString(await sha.ComputeHashAsync(stream, ct)).ToLowerInvariant();
        stream.Position = 0;
        var path = await storage.UploadAsync($"transcripts/{studentId}/{hash}.txt", stream, "text/plain", ct);
        db.Transcripts.Add(new Transcript { Id = Guid.NewGuid(), StudentId = studentId, RequestedBy = requestedBy, PdfFilePath = path, VerificationHash = hash });
        await db.SaveChangesAsync(ct);
        return hash;
    }

    public Task<bool> VerifyTranscriptAsync(string transcriptHash, CancellationToken ct = default)
        => db.Transcripts.AnyAsync(t => t.VerificationHash == transcriptHash, ct);
}
