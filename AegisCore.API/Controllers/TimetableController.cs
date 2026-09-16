using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DayOfWeek = AegisCore.Domain.DayOfWeek;

namespace AegisCore.API.Controllers;

[ApiController]
[Route("api/timetable")]
[AegisCore.API.Authorization.RequirePermission("Timetable", "Read")]
public sealed class TimetableController(AegisDbContext db) : ControllerBase
{
    [HttpPost("entries")]
    public async Task<IActionResult> Create([FromBody] CreateTimetableEntryRequest request, CancellationToken ct)
    {
        var conflict = await db.Timetables.FirstOrDefaultAsync(t =>
            t.FacultyId == request.FacultyId
            && t.DayOfWeek == request.DayOfWeek
            && t.StartTime < request.EndTime
            && t.EndTime > request.StartTime, ct);
        if (conflict is not null)
            return Conflict(new { error = "Faculty scheduling conflict detected.", conflictCourseId = conflict.CourseId, conflictRoom = conflict.Room });

        var roomConflict = await db.Timetables.FirstOrDefaultAsync(t =>
            t.Room == request.Room
            && t.DayOfWeek == request.DayOfWeek
            && t.StartTime < request.EndTime
            && t.EndTime > request.StartTime, ct);
        if (roomConflict is not null)
            return Conflict(new { error = "Room scheduling conflict detected.", conflictCourseId = roomConflict.CourseId, conflictFacultyId = roomConflict.FacultyId });

        var entry = new Timetable
        {
            Id = Guid.NewGuid(),
            BatchId = request.BatchId,
            CourseId = request.CourseId,
            FacultyId = request.FacultyId,
            DayOfWeek = request.DayOfWeek,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Room = request.Room,
            CreatedAt = DateTime.UtcNow
        };

        db.Timetables.Add(entry);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = entry.Id }, entry);
    }

    [HttpGet("entries/{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var entry = await db.Timetables.Where(t => t.Id == id).Select(Shape).FirstOrDefaultAsync(ct);
        return entry is not null ? Ok(entry) : NotFound(new { error = "Timetable entry was not found." });
    }

    [HttpGet("batch/{batchId:guid}")]
    public async Task<IActionResult> GetByBatch(Guid batchId, CancellationToken ct)
    {
        var entries = await db.Timetables
            .Where(t => t.BatchId == batchId)
            .OrderBy(t => t.DayOfWeek)
            .ThenBy(t => t.StartTime)
            .Take(200)
            .Select(Shape)
            .ToListAsync(ct);

        return Ok(new { batchId, entries });
    }

    [HttpGet("faculty/{facultyId:guid}")]
    public async Task<IActionResult> GetByFaculty(Guid facultyId, CancellationToken ct)
    {
        var entries = await db.Timetables
            .Where(t => t.FacultyId == facultyId)
            .OrderBy(t => t.DayOfWeek)
            .ThenBy(t => t.StartTime)
            .Take(200)
            .Select(Shape)
            .ToListAsync(ct);

        return Ok(new { facultyId, entries });
    }

    // Entities carry navigation graphs that cycle (course → timetables → course); serialising them
    // 500s once a course has more than one entry. Answer with the flat shape the portal reads.
    private static readonly System.Linq.Expressions.Expression<Func<Timetable, object>> Shape = t => new
    {
        t.Id, t.DayOfWeek, t.StartTime, t.EndTime, t.Room, t.CreatedAt,
        course = new { id = t.CourseId, t.Course.Code, t.Course.Title },
        faculty = new { id = t.FacultyId, t.Faculty.FirstName, t.Faculty.LastName, t.Faculty.EmployeeCode },
        batch = new { id = t.BatchId, t.Batch.Name, t.Batch.StartYear, t.Batch.EndYear }
    };

    [HttpDelete("entries/{id:guid}")]
    [AegisCore.API.Authorization.RequirePermission("Timetable", "Delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var entry = await db.Timetables.FindAsync([id], ct);
        if (entry is null) return NotFound();
        db.Timetables.Remove(entry);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("conflicts/check")]
    public async Task<IActionResult> CheckConflicts(
        [FromQuery] Guid? facultyId,
        [FromQuery] string? room,
        [FromQuery] DayOfWeek? day,
        [FromQuery] string? startTime,
        [FromQuery] string? endTime,
        CancellationToken ct)
    {
        if (facultyId is null || day is null || !TimeSpan.TryParse(startTime, out var start) || !TimeSpan.TryParse(endTime, out var end))
            return BadRequest(new { error = "FacultyId, day, startTime, and endTime are required." });

        var conflicts = await db.Timetables
            .Where(t => t.FacultyId == facultyId
                && t.DayOfWeek == day
                && t.StartTime < end
                && t.EndTime > start)
            .Include(t => t.Course)
            .Take(50)
            .ToListAsync(ct);

        return Ok(new { hasConflicts = conflicts.Count > 0, conflicts });
    }
}

public sealed record CreateTimetableEntryRequest(
    Guid BatchId,
    Guid CourseId,
    Guid FacultyId,
    DayOfWeek DayOfWeek,
    TimeSpan StartTime,
    TimeSpan EndTime,
    string? Room);
