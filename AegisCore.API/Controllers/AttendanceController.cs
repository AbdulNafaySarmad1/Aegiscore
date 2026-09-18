using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.API.Controllers;

[ApiController]
[Route("api/attendance")]
[AegisCore.API.Authorization.RequirePermission("Attendance", "Read")]
public sealed class AttendanceController(AegisDbContext db) : ControllerBase
{
    [HttpPost("mark")]
    public async Task<IActionResult> Mark([FromBody] MarkAttendanceRequest request, CancellationToken ct)
    {
        if (request.Entries.Count == 0)
            return BadRequest(new { error = "At least one attendance entry is required." });

        var course = await db.Courses.FindAsync([request.CourseId], ct);
        if (course is null) return NotFound(new { error = "Course not found." });

        // Who marked the roll. The portal used to send an all-zero id, which the validator refused,
        // so marking attendance from the portal never once succeeded. A roll is normally marked by
        // whoever teaches the course, so that is the answer unless the caller names someone else.
        var markedBy = request.MarkedByFacultyId ?? Guid.Empty;
        if (markedBy == Guid.Empty)
        {
            var teaching = await db.CourseAssignments
                .Where(a => a.CourseId == request.CourseId)
                .Select(a => a.FacultyId)
                .Distinct()
                .Take(2)
                .ToListAsync(ct);

            if (teaching.Count == 0)
                return BadRequest(new { error = $"No faculty member is assigned to {course.Code}, so there is nobody to record this roll against. Assign the course first, or name the faculty member marking it." });
            if (teaching.Count > 1)
                return BadRequest(new { error = $"{course.Code} is taught by more than one faculty member. Name the one marking this roll." });

            markedBy = teaching[0];
        }
        else if (!await db.Faculties.AnyAsync(f => f.Id == markedBy, ct))
        {
            return BadRequest(new { error = "That faculty member is not on file." });
        }

        var sessionDate = request.SessionDate.Date;
        int created = 0, updated = 0;

        foreach (var entry in request.Entries)
        {
            var existing = await db.Attendances.FirstOrDefaultAsync(a =>
                a.CourseId == request.CourseId && a.EnrollmentId == entry.EnrollmentId
                && a.Date == sessionDate, ct);

            if (existing is not null)
            {
                updated++;
                continue;
            }

            db.Attendances.Add(new Attendance
            {
                Id = Guid.NewGuid(),
                TenantId = db.CurrentTenantId,
                CourseId = request.CourseId,
                EnrollmentId = entry.EnrollmentId,
                FacultyId = markedBy,
                Date = sessionDate,
                Status = entry.Status,
                CreatedAt = DateTime.UtcNow
            });
            created++;
        }

        await db.SaveChangesAsync(ct);
        var marker = await db.Faculties.Where(f => f.Id == markedBy).Select(f => f.FirstName + " " + f.LastName).FirstOrDefaultAsync(ct);
        return Ok(new { message = "Attendance recorded.", entriesCreated = created, entriesAlreadyMarked = updated, markedBy, markedByName = marker });
    }

    [AegisCore.API.Authorization.RequireFerpaAudit("Attendance", "courseId")]
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

    [AegisCore.API.Authorization.RequireFerpaAudit("Attendance", "studentId")]
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
    /// <summary>Who marked the roll. Left out, it is the faculty member the course is assigned to.</summary>
    Guid? MarkedByFacultyId,
    DateTime SessionDate,
    List<AttendanceEntry> Entries);

public sealed record AttendanceEntry(
    Guid EnrollmentId,
    AttendanceStatus Status);
