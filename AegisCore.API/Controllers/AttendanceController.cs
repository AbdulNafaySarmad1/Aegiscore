using System.Security.Claims;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.API.Controllers;

[ApiController]
[Route("api/attendance")]
[Authorize(Roles = "Faculty,HOD,Admin,SuperAdmin")]
public sealed class AttendanceController(AegisDbContext db) : ControllerBase
{
    [HttpGet("roster/{courseId:guid}")]
    public async Task<IActionResult> Roster(Guid courseId, CancellationToken ct)
    {
        var students = await db.Enrollments
            .AsNoTracking()
            .Where(e => e.CourseId == courseId)
            .Include(e => e.Student)
            .OrderBy(e => e.Student.FirstName)
            .ThenBy(e => e.Student.LastName)
            .Select(e => new
            {
                enrollmentId = e.Id,
                studentId = e.StudentId,
                name = e.Student.FirstName + " " + e.Student.LastName,
                universityId = e.Student.UniversityId
            })
            .ToListAsync(ct);

        return Ok(new { courseId, students });
    }

    [HttpPost("mark")]
    public async Task<IActionResult> Mark([FromBody] MarkAttendanceRequest request, CancellationToken ct)
    {
        if (request.Entries.Count == 0)
            return BadRequest(new { error = "At least one attendance entry is required." });

        var course = await db.Courses.FindAsync([request.CourseId], ct);
        if (course is null) return NotFound(new { error = "Course not found." });

        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var facultyId) || facultyId == Guid.Empty)
            return Unauthorized(new { error = "Faculty identity is missing from the authenticated session." });

        var sessionDate = request.SessionDate.Date;
        int created = 0, alreadyMarked = 0;

        foreach (var entry in request.Entries)
        {
            var existing = await db.Attendances.FirstOrDefaultAsync(a =>
                a.CourseId == request.CourseId && a.EnrollmentId == entry.EnrollmentId
                && a.Date == sessionDate, ct);

            if (existing is not null)
            {
                alreadyMarked++;
                continue;
            }

            db.Attendances.Add(new Attendance
            {
                Id = Guid.NewGuid(),
                CourseId = request.CourseId,
                EnrollmentId = entry.EnrollmentId,
                FacultyId = facultyId,
                Date = sessionDate,
                Status = entry.Status,
                CreatedAt = DateTime.UtcNow
            });
            created++;
        }

        await db.SaveChangesAsync(ct);
        return Ok(new { message = "Attendance recorded.", entriesCreated = created, entriesAlreadyMarked = alreadyMarked });
    }

    [HttpGet("report/course/{courseId:guid}")]
    public async Task<IActionResult> CourseReport(Guid courseId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var start = from?.Date ?? DateTime.UtcNow.AddMonths(-3).Date;
        var end = to?.Date ?? DateTime.UtcNow.Date;

        var records = await db.Attendances
            .Where(a => a.CourseId == courseId && a.Date >= start && a.Date <= end)
            .Include(a => a.Enrollment).ThenInclude(e => e.Student)
            .OrderBy(a => a.Date)
            .Take(500)
            .ToListAsync(ct);

        var report = records
            .GroupBy(r => new { r.EnrollmentId, r.Enrollment.Student.FirstName, r.Enrollment.Student.LastName })
            .Select(g => new
            {
                studentId = g.Key.EnrollmentId,
                studentName = $"{g.Key.FirstName} {g.Key.LastName}",
                totalSessions = g.Count(),
                presentSessions = g.Count(r => r.Status == AttendanceStatus.Present),
                percentage = g.Count() > 0
                    ? Math.Round((double)g.Count(r => r.Status == AttendanceStatus.Present) / g.Count() * 100.0, 2)
                    : 0.0
            })
            .OrderBy(r => r.studentName)
            .ToList();

        return Ok(new { courseId, start, end, report });
    }

    [HttpGet("report/student/{studentId:guid}")]
    public async Task<IActionResult> StudentReport(Guid studentId, CancellationToken ct)
    {
        var records = await db.Attendances
            .Where(a => a.Enrollment.StudentId == studentId)
            .Include(a => a.Course)
            .OrderByDescending(a => a.Date)
            .Take(200)
            .ToListAsync(ct);

        var report = records
            .GroupBy(r => new { r.CourseId, r.Course.Title })
            .Select(g => new
            {
                courseId = g.Key.CourseId,
                courseTitle = g.Key.Title,
                totalSessions = g.Count(),
                presentSessions = g.Count(r => r.Status == AttendanceStatus.Present),
                percentage = g.Count() > 0
                    ? Math.Round((double)g.Count(r => r.Status == AttendanceStatus.Present) / g.Count() * 100.0, 2)
                    : 0.0
            })
            .OrderBy(r => r.courseTitle)
            .ToList();

        return Ok(new { studentId, courses = report });
    }
}

public sealed record MarkAttendanceRequest(
    Guid CourseId,
    Guid MarkedByFacultyId,
    DateTime SessionDate,
    List<AttendanceEntry> Entries);

public sealed record AttendanceEntry(
    Guid EnrollmentId,
    AttendanceStatus Status);
