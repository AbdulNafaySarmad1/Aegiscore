using System.Globalization;
using System.Text;
using AegisCore.Application.Geolocation;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Security.Pqc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.API.Controllers;

[ApiController]
[Route("api/attendance")]
public sealed class AttendanceGeolocationController(AegisDbContext db, IPqcCryptographyService encryption) : ControllerBase
{
    [AegisCore.API.Authorization.RequireFerpaAudit("Attendance", "courseId")]
    [HttpGet("roster/{courseId:guid}")]
    [AegisCore.API.Authorization.RequirePermission("Attendance", "Read")]
    public async Task<IActionResult> Roster(Guid courseId, CancellationToken ct)
    {
        var students = await db.Enrollments.AsNoTracking()
            .Where(item => item.CourseId == courseId)
            .Select(item => new
            {
                item.Id,
                item.StudentId,
                Name = item.Student.FirstName + " " + item.Student.LastName,
                item.Student.UniversityId
            })
            .OrderBy(item => item.Name)
            .ToListAsync(ct);

        var enrollmentIds = students.Select(item => item.Id).ToArray();
        var latest = await db.Attendances.AsNoTracking()
            .Where(item => item.CourseId == courseId && enrollmentIds.Contains(item.EnrollmentId))
            .GroupBy(item => item.EnrollmentId)
            .Select(group => group.OrderByDescending(item => item.Date).ThenByDescending(item => item.CreatedAt).First())
            .ToDictionaryAsync(item => item.EnrollmentId, ct);

        return Ok(new
        {
            students = students.Select(student =>
            {
                latest.TryGetValue(student.Id, out var attendance);
                return new
                {
                    enrollmentId = student.Id,
                    studentId = student.StudentId,
                    student.Name,
                    universityId = student.UniversityId,
                    attendanceId = attendance?.Id,
                    locationStatus = attendance is null ? null : StatusName(attendance.LocationStatus),
                    isManualOverride = attendance?.IsManualOverride ?? false
                };
            })
        });
    }

    [HttpGet("student-courses/{studentId}")]
    [AegisCore.API.Authorization.RequirePermission("Attendance", "Read")]
    public async Task<IActionResult> StudentCourses(string studentId, CancellationToken ct)
    {
        var hasGuid = Guid.TryParse(studentId, out var parsedStudentId);
        var courses = await db.Enrollments.AsNoTracking()
            .Where(item => (hasGuid && item.StudentId == parsedStudentId) || item.Student.UniversityId == studentId)
            .Select(item => new { id = item.CourseId, item.Course.Code, item.Course.Title })
            .OrderBy(item => item.Code)
            .ToListAsync(ct);
        return Ok(courses);
    }

    [HttpPost("checkin")]
    [AegisCore.API.Authorization.RequirePermission("Attendance", "Write")]
    public async Task<IActionResult> CheckIn([FromBody] AttendanceCheckInRequest request, CancellationToken ct)
    {
        if ((request.Latitude is null) != (request.Longitude is null))
            return BadRequest(new { error = "Latitude and longitude must both be supplied or both be null." });
        if (request.Latitude is < -90 or > 90 || request.Longitude is < -180 or > 180)
            return BadRequest(new { error = "Coordinates are outside valid latitude/longitude bounds." });

        var hasStudentGuid = Guid.TryParse(request.StudentId, out var studentGuid);
        var enrollment = await db.Enrollments.AsNoTracking().SingleOrDefaultAsync(item =>
            item.CourseId == request.CourseId && ((hasStudentGuid && item.StudentId == studentGuid) || item.Student.UniversityId == request.StudentId), ct);
        if (enrollment is null) return NotFound(new { error = "The student is not enrolled in this course." });

        var facultyId = await db.CourseAssignments.AsNoTracking()
            .Where(item => item.CourseId == request.CourseId)
            .Select(item => (Guid?)item.FacultyId)
            .FirstOrDefaultAsync(ct);
        if (facultyId is null) return BadRequest(new { error = "This course has no assigned faculty member." });

        var today = DateTime.UtcNow.Date;
        var alreadyRecorded = await db.Attendances.AnyAsync(item =>
            item.EnrollmentId == enrollment.Id && item.CourseId == request.CourseId && item.Date == today, ct);
        if (alreadyRecorded) return Conflict(new { error = "Attendance has already been recorded for this course today." });

        var polygons = await db.CampusPolygons.AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.Name)
            .ToListAsync(ct);

        var status = LocationStatus.Unverifiable;
        CampusPolygon? matchedPolygon = null;
        if (request.Latitude is not null && request.Longitude is not null && polygons.Count > 0)
        {
            matchedPolygon = polygons.FirstOrDefault(polygon =>
                GeofenceGeometry.IsPointInPolygon(request.Latitude.Value, request.Longitude.Value, polygon.GetVertices()));
            status = matchedPolygon is null ? LocationStatus.OffCampus : LocationStatus.OnCampus;
        }

        var attendance = new Attendance
        {
            Id = Guid.NewGuid(),
            TenantId = db.CurrentTenantId,
            EnrollmentId = enrollment.Id,
            CourseId = request.CourseId,
            FacultyId = facultyId.Value,
            Date = today,
            Status = AttendanceStatus.Present,
            GeofenceId = matchedPolygon?.Id ?? polygons.FirstOrDefault()?.Id,
            LocationStatus = status,
            CreatedAt = DateTime.UtcNow
        };

        if (request.Latitude is not null && request.Longitude is not null)
        {
            var latitude = decimal.Round(request.Latitude.Value, 6);
            var longitude = decimal.Round(request.Longitude.Value, 6);
            attendance.SetEncryptedLocation(
                latitude,
                longitude,
                EncryptCoordinate(latitude, attendance, "latitude"),
                EncryptCoordinate(longitude, attendance, "longitude"));
        }
        else
        {
            attendance.SetEncryptedLocation(null, null, null, null);
        }

        db.Attendances.Add(attendance);
        await db.SaveChangesAsync(ct);

        return Ok(new
        {
            status = StatusName(status),
            recordedAt = attendance.CreatedAt,
            message = StatusMessage(status)
        });
    }

    [HttpPost("{id:guid}/override")]
    [AegisCore.API.Authorization.RequireFerpaAudit("Attendance", "id")]
    [AegisCore.API.Authorization.RequirePermission("Attendance", "Approve")]
    public async Task<IActionResult> Override(Guid id, [FromBody] AttendanceOverrideRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 500)
            return BadRequest(new { error = "An override reason between 1 and 500 characters is required." });

        var attendance = await db.Attendances.SingleOrDefaultAsync(item => item.Id == id, ct);
        if (attendance is null) return NotFound();
        attendance.ApplyManualOverride(request.Reason);
        await db.SaveChangesAsync(ct);

        return Ok(new
        {
            attendance.Id,
            status = "MANUAL_OVERRIDE",
            attendance.IsManualOverride,
            attendance.OverrideReason
        });
    }

    private byte[] EncryptCoordinate(decimal value, Attendance attendance, string axis)
    {
        var plaintext = Encoding.UTF8.GetBytes(value.ToString("0.000000", CultureInfo.InvariantCulture));
        var associatedData = Encoding.UTF8.GetBytes($"{attendance.TenantId:N}:{attendance.Id:N}:{axis}");
        return encryption.Encrypt(plaintext, associatedData);
    }

    private static string StatusName(LocationStatus status) => status switch
    {
        LocationStatus.OnCampus => "ON_CAMPUS",
        LocationStatus.OffCampus => "OFF_CAMPUS",
        _ => "UNVERIFIABLE"
    };

    private static string StatusMessage(LocationStatus status) => status switch
    {
        LocationStatus.OnCampus => "Attendance recorded — you're on campus.",
        LocationStatus.OffCampus => "Attendance recorded — location shows off-campus. Contact your teacher if this is incorrect.",
        _ => "Attendance recorded — location unavailable. Marked for teacher review."
    };
}

public sealed record AttendanceCheckInRequest(string StudentId, Guid CourseId, decimal? Latitude, decimal? Longitude);
public sealed record AttendanceOverrideRequest(string Reason);
