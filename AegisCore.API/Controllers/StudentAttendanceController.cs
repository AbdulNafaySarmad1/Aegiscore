using System.Security.Claims;
using System.Text.Json;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.API.Controllers;

[ApiController]
[Route("api/student-attendance")]
[Authorize(Roles = "Student")]
public sealed class StudentAttendanceController(
    AegisDbContext db,
    IConfiguration configuration,
    ILogger<StudentAttendanceController> logger) : ControllerBase
{
    private Guid StudentId => Guid.TryParse(
        User.FindFirstValue("student_id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier),
        out var id) ? id : Guid.Empty;

    private Guid TenantId => Guid.TryParse(User.FindFirstValue("tenant_id"), out var id) ? id : Guid.Empty;

    [HttpGet("history")]
    public async Task<IActionResult> History(CancellationToken ct)
    {
        var studentId = StudentId;
        if (studentId == Guid.Empty) return Forbid();

        var enrollments = await db.Enrollments.AsNoTracking()
            .Where(e => e.StudentId == studentId && e.EnrollmentStatus == EnrollmentStatus.Active)
            .Include(e => e.Course)
            .OrderBy(e => e.Course.Title)
            .ToListAsync(ct);

        if (enrollments.Count == 0)
            return Ok(new { studentId, courses = Array.Empty<object>() });

        var enrollmentIds = enrollments.Select(e => e.Id).ToArray();
        var attendance = await db.Attendances.AsNoTracking()
            .Where(a => enrollmentIds.Contains(a.EnrollmentId))
            .OrderByDescending(a => a.Date)
            .Take(1000)
            .ToListAsync(ct);

        var byEnrollment = attendance.GroupBy(a => a.EnrollmentId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var courses = enrollments.Select(enrollment =>
        {
            var records = byEnrollment.TryGetValue(enrollment.Id, out var found) ? found : [];
            var total = records.Count;
            var present = records.Count(r => r.Status == AttendanceStatus.Present);
            var absent = records.Count(r => r.Status == AttendanceStatus.Absent);
            return new
            {
                courseId = enrollment.CourseId,
                courseTitle = enrollment.Course.Title,
                courseCode = enrollment.Course.Code,
                totalSessions = total,
                presentSessions = present,
                absentSessions = absent,
                percentage = total > 0 ? Math.Round((double)present / total * 100.0, 2) : 0.0,
                recent = records.OrderByDescending(r => r.Date).Take(10).Select(r => new
                {
                    date = r.Date.ToString("yyyy-MM-dd"),
                    status = r.Status.ToString(),
                }).ToArray(),
            };
        }).ToArray();

        return Ok(new { studentId, courses });
    }

    [HttpPost("check-in")]
    public async Task<IActionResult> CheckIn([FromBody] CheckInRequest request, CancellationToken ct)
    {
        var studentId = StudentId;
        var tenantId = TenantId;
        if (studentId == Guid.Empty || tenantId == Guid.Empty) return Forbid();

        if (!IsValidCoordinate(request.Latitude, request.Longitude))
            return BadRequest(new { error = "Valid device geolocation is required to mark attendance." });

        if (!TryGetGeofence(out var campusLatitude, out var campusLongitude, out var radiusMeters))
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Attendance geofence is not configured." });

        var distanceMeters = DistanceMeters(request.Latitude, request.Longitude, campusLatitude, campusLongitude);
        if (distanceMeters > radiusMeters)
        {
            logger.LogWarning(
                "Rejected attendance check-in outside geofence: student {StudentId}, course {CourseId}, distance {DistanceMeters:F1}m",
                studentId, request.CourseId, distanceMeters);
            return BadRequest(new
            {
                error = $"You are outside the attendance zone ({Math.Round(distanceMeters)} m from campus; allowed radius {Math.Round(radiusMeters)} m).",
                distanceMeters = Math.Round(distanceMeters, 1),
                allowedRadiusMeters = radiusMeters,
            });
        }

        var localNow = GetCampusLocalTime();
        var attendanceDate = localNow.Date;
        var dayOfWeek = (AegisCore.Domain.DayOfWeek)localNow.DayOfWeek;

        var enrollment = await db.Enrollments.AsNoTracking()
            .SingleOrDefaultAsync(e => e.StudentId == studentId
                && e.CourseId == request.CourseId
                && e.EnrollmentStatus == EnrollmentStatus.Active, ct);
        if (enrollment is null) return NotFound(new { error = "No active enrollment found for this course." });

        var student = await db.Students.AsNoTracking().SingleOrDefaultAsync(s => s.Id == studentId, ct);
        if (student is null) return NotFound(new { error = "Student record not found." });

        var timetable = await db.Timetables.AsNoTracking()
            .Where(t => t.BatchId == student.BatchId && t.CourseId == request.CourseId && t.DayOfWeek == dayOfWeek)
            .OrderBy(t => t.StartTime)
            .FirstOrDefaultAsync(ct);
        if (timetable is null) return BadRequest(new { error = "No class scheduled for this course today." });

        var now = localNow.TimeOfDay;
        var windowStart = timetable.StartTime.Subtract(TimeSpan.FromMinutes(15));
        var windowEnd = timetable.EndTime.Add(TimeSpan.FromMinutes(15));
        if (now < windowStart || now > windowEnd)
        {
            return BadRequest(new
            {
                error = "Check-in is only allowed 15 minutes before class until 15 minutes after it ends.",
                classStart = timetable.StartTime.ToString(@"hh\:mm"),
                classEnd = timetable.EndTime.ToString(@"hh\:mm"),
                campusTime = localNow.ToString("HH:mm"),
            });
        }

        var existing = await db.Attendances.AsNoTracking().FirstOrDefaultAsync(a =>
            a.EnrollmentId == enrollment.Id && a.Date == attendanceDate, ct);
        if (existing is not null)
            return Ok(new { success = true, message = "Already checked in today.", status = existing.Status.ToString(), distanceMeters = Math.Round(distanceMeters, 1) });

        var attendance = new Attendance
        {
            Id = Guid.NewGuid(),
            EnrollmentId = enrollment.Id,
            CourseId = request.CourseId,
            FacultyId = timetable.FacultyId,
            Date = attendanceDate,
            Status = AttendanceStatus.Present,
        };
        db.Attendances.Add(attendance);
        await db.SaveChangesAsync(ct);

        await PersistGeolocationEvidenceAsync(new AttendanceLocationEvidence(
            AttendanceId: attendance.Id,
            TenantId: tenantId,
            StudentId: studentId,
            CourseId: request.CourseId,
            CapturedAtUtc: DateTimeOffset.UtcNow,
            Latitude: request.Latitude,
            Longitude: request.Longitude,
            DistanceMeters: Math.Round(distanceMeters, 2),
            AllowedRadiusMeters: radiusMeters), ct);

        logger.LogInformation(
            "Attendance check-in accepted: attendance {AttendanceId}, student {StudentId}, course {CourseId}, distance {DistanceMeters:F1}m",
            attendance.Id, studentId, request.CourseId, distanceMeters);

        return Ok(new
        {
            success = true,
            message = "Attendance marked successfully.",
            id = attendance.Id,
            distanceMeters = Math.Round(distanceMeters, 1),
        });
    }

    private bool TryGetGeofence(out double latitude, out double longitude, out double radiusMeters)
    {
        latitude = configuration.GetValue<double?>("Attendance:CampusLatitude") ?? double.NaN;
        longitude = configuration.GetValue<double?>("Attendance:CampusLongitude") ?? double.NaN;
        radiusMeters = configuration.GetValue<double?>("Attendance:RadiusMeters") ?? 0;
        return IsValidCoordinate(latitude, longitude) && radiusMeters is >= 25 and <= 10_000;
    }

    private DateTime GetCampusLocalTime()
    {
        var zoneId = configuration["Attendance:TimeZone"] ?? "Asia/Karachi";
        try
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(zoneId));
        }
        catch (TimeZoneNotFoundException)
        {
            logger.LogError("Configured attendance time zone {TimeZone} is unavailable", zoneId);
            throw new InvalidOperationException("Attendance time zone is not configured correctly.");
        }
    }

    private async Task PersistGeolocationEvidenceAsync(AttendanceLocationEvidence evidence, CancellationToken ct)
    {
        var basePath = configuration["AEGISCORE_STORAGE_LOCAL_PATH"]
            ?? configuration["Storage:LocalPath"]
            ?? Path.Combine(AppContext.BaseDirectory, "storage");
        var directory = Path.Combine(basePath, "attendance-geolocation", evidence.TenantId.ToString("D"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"{evidence.AttendanceId:D}.json");
        var temp = path + ".tmp";
        await System.IO.File.WriteAllTextAsync(temp, JsonSerializer.Serialize(evidence), ct);
        System.IO.File.Move(temp, path, true);
    }

    private static bool IsValidCoordinate(double latitude, double longitude)
        => double.IsFinite(latitude) && double.IsFinite(longitude)
            && latitude is >= -90 and <= 90 && longitude is >= -180 and <= 180;

    private static double DistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusMeters = 6_371_000;
        static double Radians(double degrees) => degrees * Math.PI / 180d;
        var phi1 = Radians(lat1);
        var phi2 = Radians(lat2);
        var dPhi = Radians(lat2 - lat1);
        var dLambda = Radians(lon2 - lon1);
        var a = Math.Sin(dPhi / 2) * Math.Sin(dPhi / 2)
            + Math.Cos(phi1) * Math.Cos(phi2) * Math.Sin(dLambda / 2) * Math.Sin(dLambda / 2);
        return earthRadiusMeters * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    public sealed record CheckInRequest(Guid CourseId, double Latitude, double Longitude);

    private sealed record AttendanceLocationEvidence(
        Guid AttendanceId,
        Guid TenantId,
        Guid StudentId,
        Guid CourseId,
        DateTimeOffset CapturedAtUtc,
        double Latitude,
        double Longitude,
        double DistanceMeters,
        double AllowedRadiusMeters);
}
