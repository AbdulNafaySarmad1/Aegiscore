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
[Route("api/student-portal/registration")]
[Authorize(Roles = "Student,SuperAdmin,Admin")]
public sealed class StudentRegistrationController(AegisDbContext db) : ControllerBase
{
    private sealed record FacultyOption(Guid Id, string Name, string Section, string Schedule, string Room, int SeatsRemaining);
    private sealed record CourseOffering(Guid Id, string Code, string Title, int CreditHours, FacultyOption[] FacultyOptions);

    private static readonly CourseOffering[] DemoOfferings =
    [
        new(
            Guid.Parse("ca110000-0000-0000-0000-000000000001"), "CS304", "Data Structures & Algorithms", 3,
            [
                new(Guid.Parse("fa110000-0000-0000-0000-000000000001"), "Dr. Imran Siddiqui", "A", "Mon/Wed 09:00–10:30", "CS-201", 18),
                new(Guid.Parse("fa110000-0000-0000-0000-000000000002"), "Dr. Ayesha Rahman", "B", "Mon/Wed 11:00–12:30", "CS-202", 11),
                new(Guid.Parse("fa110000-0000-0000-0000-000000000003"), "Engr. Hamza Khan", "C", "Tue/Thu 09:00–10:30", "CS-203", 22),
                new(Guid.Parse("fa110000-0000-0000-0000-000000000004"), "Ms. Sara Ahmed", "D", "Tue/Thu 14:00–15:30", "CS-204", 9),
            ]),
        new(
            Guid.Parse("ca110000-0000-0000-0000-000000000002"), "CS305", "Computer Organization & Assembly", 3,
            [
                new(Guid.Parse("fa120000-0000-0000-0000-000000000001"), "Dr. Imran Siddiqui", "A", "Tue/Thu 10:30–12:00", "LAB-03", 14),
                new(Guid.Parse("fa120000-0000-0000-0000-000000000002"), "Dr. Ayesha Rahman", "B", "Mon/Wed 14:00–15:30", "LAB-04", 16),
                new(Guid.Parse("fa120000-0000-0000-0000-000000000003"), "Engr. Hamza Khan", "C", "Fri 09:00–12:00", "LAB-05", 7),
                new(Guid.Parse("fa120000-0000-0000-0000-000000000004"), "Ms. Sara Ahmed", "D", "Fri 13:00–16:00", "LAB-06", 20),
            ]),
        new(
            Guid.Parse("ca110000-0000-0000-0000-000000000003"), "CS306", "Discrete Structures", 3,
            [
                new(Guid.Parse("fa130000-0000-0000-0000-000000000001"), "Dr. Imran Siddiqui", "A", "Mon/Wed 08:00–09:30", "CS-205", 12),
                new(Guid.Parse("fa130000-0000-0000-0000-000000000002"), "Dr. Ayesha Rahman", "B", "Tue/Thu 12:30–14:00", "CS-206", 19),
                new(Guid.Parse("fa130000-0000-0000-0000-000000000003"), "Engr. Hamza Khan", "C", "Mon/Wed 16:00–17:30", "CS-207", 24),
                new(Guid.Parse("fa130000-0000-0000-0000-000000000004"), "Ms. Sara Ahmed", "D", "Tue/Thu 16:00–17:30", "CS-208", 8),
            ]),
        new(
            Guid.Parse("ca110000-0000-0000-0000-000000000004"), "MT201", "Differential Equations", 3,
            [
                new(Guid.Parse("fa140000-0000-0000-0000-000000000001"), "Dr. Imran Siddiqui", "A", "Mon/Wed 12:30–14:00", "MTH-01", 13),
                new(Guid.Parse("fa140000-0000-0000-0000-000000000002"), "Dr. Ayesha Rahman", "B", "Tue/Thu 08:00–09:30", "MTH-02", 17),
                new(Guid.Parse("fa140000-0000-0000-0000-000000000003"), "Engr. Hamza Khan", "C", "Mon/Wed 14:00–15:30", "MTH-03", 21),
                new(Guid.Parse("fa140000-0000-0000-0000-000000000004"), "Ms. Sara Ahmed", "D", "Tue/Thu 11:00–12:30", "MTH-04", 10),
            ]),
    ];

    private Guid StudentId => Guid.TryParse(
        User.FindFirstValue("student_id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
        ? id : Guid.Empty;

    [HttpGet]
    public async Task<IActionResult> GetRegistration(CancellationToken ct)
    {
        var studentId = StudentId;
        var student = await db.Students.AsNoTracking().SingleOrDefaultAsync(s => s.Id == studentId, ct);
        if (student is null) return NotFound(new { error = "Student record not found." });

        var activeEnrollments = await db.Enrollments.AsNoTracking()
            .Where(e => e.StudentId == studentId && e.EnrollmentStatus == EnrollmentStatus.Active)
            .Include(e => e.Course)
            .OrderBy(e => e.Course.Code)
            .Select(e => new
            {
                enrollmentId = e.Id,
                courseId = e.CourseId,
                code = e.Course.Code,
                title = e.Course.Title,
                creditHours = e.Course.CreditHours,
                grade = e.Grade,
            })
            .ToListAsync(ct);

        var selections = await ReadSelectionsAsync(studentId, ct);

        var offerings = DemoOfferings.Select(o => new
        {
            courseId = o.Id,
            o.Code,
            o.Title,
            o.CreditHours,
            registered = activeEnrollments.Any(e => e.courseId == o.Id),
            selectedFacultyOptionId = selections.TryGetValue(o.Id.ToString(), out var saved)
                && saved.TryGetProperty("facultyOptionId", out var facultyId) ? facultyId.GetString() : null,
            facultyOptions = o.FacultyOptions.Take(4).Select(f => new
            {
                id = f.Id,
                f.Name,
                f.Section,
                f.Schedule,
                f.Room,
                f.SeatsRemaining,
            })
        });

        return Ok(new
        {
            registrationOpen = true,
            term = "Next Academic Term",
            maxFacultyOptionsPerCourse = 4,
            enrolledCourses = activeEnrollments,
            offerings,
        });
    }

    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterCourseRequest request, CancellationToken ct)
    {
        var studentId = StudentId;
        var student = await db.Students.SingleOrDefaultAsync(s => s.Id == studentId, ct);
        if (student is null) return NotFound(new { error = "Student record not found." });

        var offering = DemoOfferings.SingleOrDefault(o => o.Id == request.CourseId);
        if (offering is null) return BadRequest(new { error = "This course is not currently available for registration." });

        var faculty = offering.FacultyOptions.Take(4).SingleOrDefault(f => f.Id == request.FacultyOptionId);
        if (faculty is null) return BadRequest(new { error = "Select a valid faculty/section option for this course." });
        if (faculty.SeatsRemaining <= 0) return Conflict(new { error = "The selected section is full." });

        var course = await db.Courses.SingleOrDefaultAsync(c => c.Id == offering.Id, ct);
        if (course is null)
        {
            var batch = await db.Batches.AsNoTracking().SingleOrDefaultAsync(b => b.Id == student.BatchId, ct);
            if (batch is null) return BadRequest(new { error = "Student batch is not configured." });

            course = new Course
            {
                Id = offering.Id,
                Code = offering.Code,
                Title = offering.Title,
                CreditHours = offering.CreditHours,
                DepartmentId = batch.DepartmentId,
            };
            db.Courses.Add(course);
        }

        var enrollment = await db.Enrollments.SingleOrDefaultAsync(e =>
            e.StudentId == studentId && e.CourseId == offering.Id, ct);

        if (enrollment is null)
        {
            enrollment = new Enrollment
            {
                Id = Guid.NewGuid(),
                StudentId = studentId,
                CourseId = offering.Id,
                EnrollmentStatus = EnrollmentStatus.Active,
            };
            db.Enrollments.Add(enrollment);
        }
        else if (enrollment.EnrollmentStatus != EnrollmentStatus.Active)
        {
            // EnrollmentStatus is immutable after construction in the domain model.
            // Never mutate it here: an inactive historical enrollment must go through
            // the university's formal re-enrollment/repeat-course workflow instead.
            return Conflict(new
            {
                error = "A previous enrollment exists for this course. Use the re-enrollment/repeat-course workflow."
            });
        }

        var profile = await db.StudentProfiles.SingleOrDefaultAsync(p => p.StudentId == studentId, ct);
        if (profile is null) return BadRequest(new { error = "Student profile is not configured." });

        Dictionary<string, JsonElement> root;
        try { root = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(profile.ProfileJson ?? "{}") ?? new(); }
        catch { root = new(); }

        var selections = new Dictionary<string, JsonElement>();
        if (root.TryGetValue("courseRegistrationSelections", out var existing) && existing.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in existing.EnumerateObject()) selections[property.Name] = property.Value.Clone();
        }

        selections[offering.Id.ToString()] = JsonSerializer.SerializeToElement(new
        {
            facultyOptionId = faculty.Id,
            facultyName = faculty.Name,
            faculty.Section,
            faculty.Schedule,
            faculty.Room,
            registeredAt = DateTime.UtcNow,
        });
        root["courseRegistrationSelections"] = JsonSerializer.SerializeToElement(selections);
        profile.ProfileJson = JsonSerializer.Serialize(root);
        profile.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return Ok(new
        {
            success = true,
            enrollmentId = enrollment.Id,
            course = new { offering.Id, offering.Code, offering.Title, offering.CreditHours },
            faculty = new { faculty.Id, faculty.Name, faculty.Section, faculty.Schedule, faculty.Room },
            message = $"{offering.Code} registered successfully with {faculty.Name}, Section {faculty.Section}."
        });
    }

    private async Task<Dictionary<string, JsonElement>> ReadSelectionsAsync(Guid studentId, CancellationToken ct)
    {
        var json = await db.StudentProfiles.AsNoTracking()
            .Where(p => p.StudentId == studentId)
            .Select(p => p.ProfileJson)
            .SingleOrDefaultAsync(ct);
        if (string.IsNullOrWhiteSpace(json)) return new();

        try
        {
            var root = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json) ?? new();
            if (!root.TryGetValue("courseRegistrationSelections", out var selections) || selections.ValueKind != JsonValueKind.Object)
                return new();
            return selections.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone());
        }
        catch { return new(); }
    }

    public sealed record RegisterCourseRequest(Guid CourseId, Guid FacultyOptionId);
}
