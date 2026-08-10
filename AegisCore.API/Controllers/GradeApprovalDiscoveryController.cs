using System.Security.Claims;
using System.Text.Json;
using AegisCore.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.API.Controllers;

[ApiController]
[Route("api/grade-approvals")]
[Authorize(Roles = "HOD,ExamController,Admin,SuperAdmin")]
public sealed class GradeApprovalDiscoveryController(AegisDbContext db) : ControllerBase
{
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

    [HttpGet]
    public async Task<IActionResult> Queue(CancellationToken ct)
    {
        var allowedCourseIds = await AllowedCourses(ct);
        var pending = await db.MarkModerations.AsNoTracking()
            .Where(m => allowedCourseIds.Contains(m.CourseId)
                && m.Status == "submitted"
                && m.ReviewComments != null
                && m.ReviewComments.StartsWith("GRADE_CHANGE:"))
            .OrderBy(m => m.SubmittedAt)
            .ToListAsync(ct);

        var courseIds = pending.Select(m => m.CourseId).Distinct().ToList();
        var courses = await db.Courses.AsNoTracking().Where(c => courseIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, ct);
        var submitterIds = pending.Select(m => m.SubmittedBy).Distinct().ToList();
        var submitters = await db.Users.AsNoTracking().IgnoreQueryFilters().Where(u => submitterIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, ct);

        var studentIds = new HashSet<Guid>();
        var componentIds = new HashSet<Guid>();
        var parsed = pending.Select(m => (Moderation: m, Detail: Parse(m.ReviewComments))).ToList();
        foreach (var row in parsed)
        {
            if (row.Detail is null) continue;
            studentIds.Add(row.Detail.StudentId);
            componentIds.Add(row.Detail.AssessmentComponentId);
        }
        var students = await db.Students.AsNoTracking().Where(s => studentIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, ct);
        var components = await db.AssessmentComponents.AsNoTracking().Where(c => componentIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, ct);

        return Ok(parsed.Where(x => x.Detail is not null).Select(x =>
        {
            var d = x.Detail!;
            courses.TryGetValue(x.Moderation.CourseId, out var course);
            submitters.TryGetValue(x.Moderation.SubmittedBy, out var submitter);
            students.TryGetValue(d.StudentId, out var student);
            components.TryGetValue(d.AssessmentComponentId, out var component);
            return new
            {
                id = x.Moderation.Id,
                courseId = x.Moderation.CourseId,
                course = new { code = course?.Code ?? "COURSE", title = course?.Title ?? "Unknown Course" },
                student = new { id = d.StudentId, universityId = student?.UniversityId ?? "N/A", name = student is null ? "Unknown Student" : $"{student.FirstName} {student.LastName}".Trim() },
                component = new { id = d.AssessmentComponentId, name = component?.ComponentType ?? "Assessment", totalMarks = component?.TotalMarks ?? 0, passingMarks = component?.PassingMarks },
                previousMark = d.PreviousMark,
                requestedMark = d.RequestedMark,
                delta = d.Delta,
                crossesPassBoundary = d.CrossesPassBoundary,
                passMark = d.PassMark,
                submittedBy = new { id = x.Moderation.SubmittedBy, name = submitter is null ? "Unknown Faculty" : $"{submitter.FirstName} {submitter.LastName}".Trim(), email = submitter?.Email },
                submittedAt = x.Moderation.SubmittedAt,
                reason = d.CrossesPassBoundary ? "Pass/fail boundary change" : "Change of 5 or more marks"
            };
        }));
    }

    private async Task<List<Guid>> AllowedCourses(CancellationToken ct)
    {
        if (User.IsInRole("Admin") || User.IsInRole("SuperAdmin") || User.IsInRole("ExamController"))
            return await db.Courses.AsNoTracking().Select(c => c.Id).ToListAsync(ct);

        var departmentId = await db.Faculties.AsNoTracking().Where(f => f.Id == UserId).Select(f => f.DepartmentId).FirstOrDefaultAsync(ct);
        if (departmentId == Guid.Empty) return [];
        return await db.Courses.AsNoTracking().Where(c => c.DepartmentId == departmentId).Select(c => c.Id).ToListAsync(ct);
    }

    private static Detail? Parse(string? comments)
    {
        if (string.IsNullOrWhiteSpace(comments) || !comments.StartsWith("GRADE_CHANGE:")) return null;
        var json = comments["GRADE_CHANGE:".Length..].Split("\nREVIEW:", 2)[0];
        try { return JsonSerializer.Deserialize<Detail>(json); } catch { return null; }
    }

    private sealed record Detail(Guid StudentId, Guid AssessmentComponentId, int PreviousMark, int RequestedMark, int Delta, int PassMark, bool CrossesPassBoundary);
}
