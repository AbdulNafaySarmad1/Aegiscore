using System.Security.Claims;
using AegisCore.API.Authorization;
using AegisCore.Domain;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.DegreeAudit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.API.Controllers;

[ApiController, Authorize, Route("api/student/overview")]
[RequirePermission("Students", "Read")]
public sealed class StudentOverviewController(AegisDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var studentId)) return Forbid();
        var student = await db.Students.AsNoTracking().SingleOrDefaultAsync(s => s.Id == studentId, ct);
        if (student is null) return NotFound(new { error = "The signed-in account is not linked to a student record." });
        var user = await db.Users.AsNoTracking().SingleAsync(u => u.Id == studentId, ct);
        var applicant = student.AdmissionApplicantProfileId is { } applicantId
            ? await db.ApplicantProfiles.AsNoTracking().SingleOrDefaultAsync(a => a.Id == applicantId && a.ConvertedStudentId == studentId, ct) : null;
        var program = applicant is null ? null : await db.DegreePrograms.AsNoTracking().SingleOrDefaultAsync(p => p.Id == applicant.ProgramId, ct);
        var enrollments = await db.Enrollments.AsNoTracking().Where(e => e.StudentId == studentId).Include(e => e.Course).OrderBy(e => e.Course.Code).ToListAsync(ct);
        var courses = enrollments.Select(e => e.Course).DistinctBy(c => c.Id).ToList();
        var passing = enrollments.Where(e => AcademicRecordMath.IsPassing(e.Grade)).ToList();
        var credits = passing.Sum(e => e.Course.CreditHours);
        var cgpa = AcademicRecordMath.Cgpa(enrollments, courses);
        var attendance = await db.Attendances.AsNoTracking().Where(a => a.Enrollment.StudentId == studentId).Include(a => a.Course).ToListAsync(ct);
        var dues = await db.FeeDemands.AsNoTracking().Where(d => d.StudentId == studentId).ToListAsync(ct);
        var test = applicant is null ? null : await db.ApplicantTests.AsNoTracking().SingleOrDefaultAsync(t => t.ApplicantId == applicant.Id, ct);
        var documents = applicant is null ? 0 : await db.ApplicantDocuments.AsNoTracking().CountAsync(d => d.ApplicantId == applicant.Id && d.ValidationStatus == AdmissionDocumentValidationStatus.Approved, ct);
        var timetable = await db.Timetables.AsNoTracking().Where(t => t.BatchId == student.BatchId).Include(t => t.Course)
            .OrderBy(t => t.DayOfWeek).ThenBy(t => t.StartTime).Take(10).Select(t => new { day = t.DayOfWeek.ToString(), t.StartTime, t.EndTime, t.Room, course = t.Course.Title }).ToListAsync(ct);
        return Ok(new
        {
            student = new { student.Id, name = $"{student.FirstName} {student.LastName}", student.UniversityId, student.RollNumber, institutionalEmail = user.Email, status = student.EnrollmentStatus.ToString(), student.CreatedAt },
            admission = applicant is null ? null : new { applicationNumber = applicant.ApplicantId, applicant.Email, emailVerified = applicant.EmailVerifiedAt != null,
                status = applicant.Status.ToString(), applicant.SubmittedAt, approvedDocuments = documents, testScore = test?.Score, testPassed = test?.Passed },
            academics = new { program = program?.Name, cgpa = Math.Round(cgpa, 2), earnedCredits = credits, requiredCredits = program?.MinCreditHours ?? 0,
                completedCourses = passing.Count, totalCourses = enrollments.Count,
                courses = enrollments.Select(e => new { e.Course.Code, title = e.Course.Title, credits = e.Course.CreditHours, grade = e.Grade, status = e.EnrollmentStatus.ToString() }) },
            attendance = attendance.GroupBy(a => new { a.CourseId, a.Course.Title }).Select(g => new { course = g.Key.Title, total = g.Count(), present = g.Count(a => a.Status == AttendanceStatus.Present), percentage = Math.Round(g.Count(a => a.Status == AttendanceStatus.Present) * 100m / g.Count(), 1) }),
            finance = new { demanded = dues.Sum(d => d.TotalAmount), paid = dues.Sum(d => d.PaidAmount), scholarships = dues.Sum(d => d.ScholarshipDeduction), waived = dues.Sum(d => d.WaivedAmount), outstanding = dues.Sum(d => d.OutstandingAmount), status = dues.Count == 0 ? "No demands" : dues.Sum(d => d.OutstandingAmount) == 0 ? "Paid" : "Outstanding" },
            timetable
        });
    }
}
