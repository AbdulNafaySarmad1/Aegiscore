using AegisCore.Domain.Entities;

namespace AegisCore.Infrastructure.DegreeAudit;

public static class AcademicRecordMath
{
    public static bool IsPassing(string? grade) => grade?.Trim().ToUpperInvariant()
        is "A+" or "A" or "A-" or "B+" or "B" or "B-" or "C+" or "C" or "C-" or "D+" or "D" or "P";

    // All graded attempts count, including failures. Transfer credits do not carry GPA.
    public static decimal Cgpa(IEnumerable<Enrollment> enrollments, IEnumerable<Course> courses)
    {
        var credits = courses.ToDictionary(c => c.Id, c => c.CreditHours);
        decimal points = 0;
        var attempted = 0;
        foreach (var enrollment in enrollments)
        {
            var grade = enrollment.Grade?.Trim().ToUpperInvariant();
            if (grade == "P" || (!IsPassing(grade) && grade != "F")) continue;
            if (!credits.TryGetValue(enrollment.CourseId, out var hours) || hours <= 0) continue;
            if (grade != "F" && !enrollment.GpaPoints.HasValue) continue;
            points += (grade == "F" ? 0 : enrollment.GpaPoints!.Value) * hours;
            attempted += hours;
        }
        return attempted == 0 ? 0 : points / attempted;
    }
}
