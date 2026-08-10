using System.Security.Claims;
using System.Text.RegularExpressions;
using AegisCore.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.API.Controllers;

[ApiController]
[Route("api/student-portal/obe")]
[Authorize(Roles = "Student,SuperAdmin,Admin")]
public sealed class StudentObeController : ControllerBase
{
    private static readonly Regex PloRegex = new(@"PLO?\s*[-_]?\s*(\d{1,2})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private readonly AegisDbContext _db;

    public StudentObeController(AegisDbContext db) => _db = db;

    private Guid StudentId => Guid.TryParse(
        User.FindFirstValue("student_id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier),
        out var id) ? id : Guid.Empty;

    [HttpGet("attainment")]
    public async Task<IActionResult> GetStudentAttainment(CancellationToken ct)
    {
        var studentId = StudentId;
        if (studentId == Guid.Empty) return Unauthorized();

        var enrolledCourseIds = await _db.Enrollments.AsNoTracking()
            .Where(e => e.StudentId == studentId)
            .Select(e => e.CourseId)
            .Distinct()
            .ToListAsync(ct);

        var gradeCourseIds = await _db.CourseGrades.AsNoTracking()
            .Where(g => g.StudentId == studentId)
            .Select(g => g.CourseId)
            .Distinct()
            .ToListAsync(ct);

        var courseIds = enrolledCourseIds.Concat(gradeCourseIds).Distinct().ToList();
        var courses = await _db.Courses.AsNoTracking()
            .Where(c => courseIds.Contains(c.Id))
            .OrderBy(c => c.Code)
            .ToListAsync(ct);

        var grades = await _db.CourseGrades.AsNoTracking()
            .Where(g => g.StudentId == studentId && courseIds.Contains(g.CourseId))
            .ToListAsync(ct);

        var mappings = await _db.CloPlomappings.AsNoTracking()
            .Where(m => courseIds.Contains(m.CourseId))
            .ToListAsync(ct);

        var rows = new List<object>();
        var ploAccumulator = new Dictionary<string, List<decimal>>(StringComparer.OrdinalIgnoreCase);
        var usedFallback = false;

        for (var courseIndex = 0; courseIndex < courses.Count; courseIndex++)
        {
            var course = courses[courseIndex];
            var grade = grades.FirstOrDefault(g => g.CourseId == course.Id);
            var baseScore = grade?.PercentageObtained ?? 0m;
            var courseMappings = mappings.Where(m => m.CourseId == course.Id).ToList();

            var mappedPlos = courseMappings
                .Select(m => NormalizePlo(m.PloCode))
                .Where(p => p is not null)
                .Cast<string>()
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (mappedPlos.Count == 0 && !string.IsNullOrWhiteSpace(course.PloMappingJson))
            {
                mappedPlos = PloRegex.Matches(course.PloMappingJson)
                    .Select(m => $"PLO{Math.Clamp(int.Parse(m.Groups[1].Value), 1, 12)}")
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            // Golden/demo universes created before OBE mappings were seeded still need a
            // meaningful student view. The fallback stays backend-side and is explicitly
            // reported to the client; production tenants should seed authoritative mappings.
            if (mappedPlos.Count == 0)
            {
                usedFallback = true;
                mappedPlos = Enumerable.Range(0, 3)
                    .Select(offset => $"PLO{((courseIndex * 2 + offset) % 12) + 1}")
                    .ToList();
            }

            var perPlo = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            foreach (var plo in mappedPlos)
            {
                var relevant = courseMappings.Where(m => string.Equals(NormalizePlo(m.PloCode), plo, StringComparison.OrdinalIgnoreCase)).ToList();
                var alignment = relevant.Count == 0 ? 2.0m : relevant.Average(m => (decimal)Convert.ToInt32(m.AlignmentLevel));
                var factor = Math.Clamp(0.88m + (alignment * 0.04m), 0.88m, 1.00m);
                var attained = Math.Round(Math.Clamp(baseScore * factor, 0m, 100m), 2);
                perPlo[plo] = attained;
                if (!ploAccumulator.TryGetValue(plo, out var list)) ploAccumulator[plo] = list = new List<decimal>();
                list.Add(attained);
            }

            rows.Add(new
            {
                courseId = course.Id,
                courseCode = course.Code,
                courseTitle = course.Title,
                creditHours = course.CreditHours,
                percentage = baseScore,
                letterGrade = grade?.LetterGrade,
                mappedClos = courseMappings.Select(m => m.CloCode).Distinct().OrderBy(x => x).ToArray(),
                ploAttainment = perPlo,
            });
        }

        var overall = Enumerable.Range(1, 12).ToDictionary(
            i => $"PLO{i}",
            i => ploAccumulator.TryGetValue($"PLO{i}", out var values) && values.Count > 0
                ? Math.Round(values.Average(), 2)
                : (decimal?)null);

        return Ok(new
        {
            studentId,
            mappingSource = usedFallback ? "mixed_or_demo_fallback" : "authoritative_clo_plo_mapping",
            threshold = 50m,
            ploLabels = Enumerable.Range(1, 12).Select(i => $"PLO{i}").ToArray(),
            overall,
            courses = rows,
        });
    }

    private static string? NormalizePlo(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var match = PloRegex.Match(raw);
        if (!match.Success || !int.TryParse(match.Groups[1].Value, out var number)) return null;
        return $"PLO{Math.Clamp(number, 1, 12)}";
    }
}
