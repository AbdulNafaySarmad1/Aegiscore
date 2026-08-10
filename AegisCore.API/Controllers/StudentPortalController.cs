using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Security.Pqc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using QRCoder;

namespace AegisCore.API.Controllers;

[ApiController]
[Route("api/student-portal")]
[Authorize(Roles = "Student,SuperAdmin,Admin")]
public sealed class StudentPortalController : ControllerBase
{

    private sealed record GeneratedTranscript(
        byte[] Pdf,
        DateTime ExpiresAt,
        Guid StudentId
    );

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, GeneratedTranscript>
        GeneratedTranscripts = new();

    private readonly AegisDbContext _db;
    private readonly IPqcSignatureService _pqc;

    public StudentPortalController(AegisDbContext db, IPqcSignatureService pqc)
    {
        _db = db;
        _pqc = pqc;
    }

    private Guid StudentId => Guid.TryParse(User.FindFirstValue("student_id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

    [HttpGet("me")]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var studentId = StudentId;
        var user = await _db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == studentId, ct);
        var profile = await _db.StudentProfiles.AsNoTracking()
            .FirstOrDefaultAsync(sp => sp.TenantId == user!.TenantId && sp.StudentId == studentId, ct);

        JsonElement profileJson = default;
        if (profile?.ProfileJson is not null)
        {
            try { profileJson = JsonSerializer.Deserialize<JsonElement>(profile.ProfileJson); } catch { }
        }

        return Ok(new
        {
            studentId,
            name = user?.FirstName + " " + user?.LastName,
            email = user?.Email,
            firstName = user?.FirstName,
            lastName = user?.LastName,
            program = profileJson.TryGetProperty("program", out var pr) ? pr.GetString() : null,
            currentSemester = profileJson.TryGetProperty("currentSemester", out var cs) ? cs.GetString() : null,
            cgpa = profileJson.TryGetProperty("cgpa", out var cg) ? cg.GetString() : null,
            creditsCompleted = profileJson.TryGetProperty("creditsCompleted", out var cc) ? cc.GetString() : null,
            totalCredits = profileJson.TryGetProperty("totalCredits", out var tc) ? tc.GetString() : null,
            enrollmentStatus = profileJson.TryGetProperty("enrollmentStatus", out var es) ? es.GetString() : null,
            universityId = profileJson.TryGetProperty("universityId", out var uid) ? uid.GetString() : null,
            phone = profileJson.TryGetProperty("phone", out var ph) ? ph.GetString() : null,
            address = profileJson.TryGetProperty("address", out var ad) ? ad.GetString() : null,
            guardianName = profileJson.TryGetProperty("guardianName", out var gn) ? gn.GetString() : null,
            guardianPhone = profileJson.TryGetProperty("guardianPhone", out var gp) ? gp.GetString() : null,
            paymentAlias = profileJson.TryGetProperty("paymentAlias", out var pa) ? pa.GetString() : null,
        });
    }

    [HttpPut("me/profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var studentId = StudentId;
        var user = await _db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == studentId, ct);
        if (user is null) return NotFound();

        var profile = await _db.StudentProfiles
            .FirstOrDefaultAsync(sp => sp.TenantId == user.TenantId && sp.StudentId == studentId, ct);
        if (profile is null) return NotFound();

        var json = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(profile.ProfileJson) ?? new();

        if (request.Phone is not null) json["phone"] = JsonSerializer.SerializeToElement(request.Phone);
        if (request.Address is not null) json["address"] = JsonSerializer.SerializeToElement(request.Address);
        if (request.PaymentAlias is not null) json["paymentAlias"] = JsonSerializer.SerializeToElement(request.PaymentAlias);
        if (request.PreferredName is not null) json["preferredName"] = JsonSerializer.SerializeToElement(request.PreferredName);

        profile.ProfileJson = JsonSerializer.Serialize(json);
        profile.UpdatedAt = DateTime.UtcNow;

        if (request.PreferredName is not null)
        {
            var parts = request.PreferredName.Split(' ', 2);
            user = await _db.Users.SingleOrDefaultAsync(u => u.Id == studentId, ct);
            if (user is not null)
            {
                user.FirstName = parts[0];
                if (parts.Length > 1) user.LastName = parts[1];
            }
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new { success = true });
    }

    [HttpGet("my-timetable")]
    public async Task<IActionResult> GetTimetable(CancellationToken ct)
    {
        var studentId = StudentId;
        var student = await _db.Students.AsNoTracking().SingleOrDefaultAsync(s => s.Id == studentId, ct);
        if (student is null) return NotFound();

        var today = DateTime.UtcNow.Date;
        var todayDow = (AegisCore.Domain.DayOfWeek)today.DayOfWeek;

        var timetable = await _db.Timetables.AsNoTracking()
            .Where(t => t.BatchId == student.BatchId)
            .Include(t => t.Course)
            .OrderBy(t => t.DayOfWeek).ThenBy(t => t.StartTime)
            .ToListAsync(ct);

        var result = timetable.Select(t => new
        {
            t.Id,
            t.DayOfWeek,
            dayName = t.DayOfWeek.ToString(),
            startTime = t.StartTime.ToString(@"hh\:mm"),
            endTime = t.EndTime.ToString(@"hh\:mm"),
            t.Room,
            courseId = t.CourseId,
            courseTitle = t.Course.Title,
            courseCode = t.Course.Code,
            isToday = t.DayOfWeek == todayDow,
        }).ToList();

        return Ok(new { today = todayDow.ToString(), entries = result });
    }

    [HttpPost("attendance/check-in")]
    public async Task<IActionResult> CheckIn([FromBody] AttendanceCheckInRequest request, CancellationToken ct)
    {
        var studentId = StudentId;
        var today = DateTime.UtcNow.Date;
        var courseId = request.CourseId;

        var enrollment = await _db.Enrollments.AsNoTracking()
            .SingleOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == courseId && e.EnrollmentStatus == EnrollmentStatus.Active, ct);
        if (enrollment is null) return NotFound(new { error = "No active enrollment found for this course." });

        var timetable = await _db.Timetables.AsNoTracking()
            .FirstOrDefaultAsync(t => t.CourseId == courseId && t.DayOfWeek == (AegisCore.Domain.DayOfWeek)today.DayOfWeek, ct);
        if (timetable is null) return BadRequest(new { error = "No class scheduled for this course today." });

        var now = TimeSpan.FromTicks(DateTime.UtcNow.TimeOfDay.Ticks);
        var windowStart = timetable.StartTime.Subtract(TimeSpan.FromMinutes(15));
        var windowEnd = timetable.EndTime.Add(TimeSpan.FromMinutes(15));
        if (now < windowStart || now > windowEnd)
            return BadRequest(new { error = "Check-in is only allowed 15 minutes before to 15 minutes after the scheduled class." });

        if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            if (request.Latitude < -90 || request.Latitude > 90 || request.Longitude < -180 || request.Longitude > 180)
                return BadRequest(new { error = "Invalid geolocation coordinates." });
        }

        var existing = await _db.Attendances.FirstOrDefaultAsync(a =>
            a.EnrollmentId == enrollment.Id && a.CourseId == courseId && a.Date == today, ct);
        if (existing is not null)
            return Ok(new { success = true, message = "Already checked in today.", status = existing.Status.ToString() });

        var attendance = new Attendance
        {
            Id = Guid.NewGuid(),
            EnrollmentId = enrollment.Id,
            CourseId = courseId,
            FacultyId = timetable.FacultyId,
            Date = today,
            Status = AttendanceStatus.Present,
        };
        _db.Attendances.Add(attendance);
        await _db.SaveChangesAsync(ct);

        return Ok(new { success = true, message = "Check-in recorded.", id = attendance.Id });
    }

    [HttpGet("attendance/history")]
    public async Task<IActionResult> AttendanceHistory(CancellationToken ct)
    {
        var studentId = StudentId;
        var records = await _db.Attendances.AsNoTracking()
            .Where(a => a.Enrollment.StudentId == studentId)
            .Include(a => a.Course)
            .OrderByDescending(a => a.Date)
            .Take(200)
            .ToListAsync(ct);

        var byCourse = records
            .GroupBy(r => new { r.CourseId, r.Course.Title })
            .Select(g => new
            {
                courseId = g.Key.CourseId,
                courseTitle = g.Key.Title,
                totalSessions = g.Count(),
                presentSessions = g.Count(r => r.Status == AttendanceStatus.Present),
                absentSessions = g.Count(r => r.Status == AttendanceStatus.Absent),
                percentage = g.Count() > 0 ? Math.Round((double)g.Count(r => r.Status == AttendanceStatus.Present) / g.Count() * 100.0, 2) : 0.0,
                recent = g.OrderByDescending(r => r.Date).Take(10).Select(r => new
                {
                    date = r.Date.ToString("yyyy-MM-dd"),
                    status = r.Status.ToString(),
                }),
            })
            .OrderBy(r => r.courseTitle)
            .ToList();

        return Ok(new { studentId, courses = byCourse });
    }

    [HttpGet("examinations")]
    public async Task<IActionResult> Examinations(CancellationToken ct)
    {
        var studentId = StudentId;
        var enrolledCourseIds = await _db.Enrollments.AsNoTracking()
            .Where(e => e.StudentId == studentId && e.EnrollmentStatus == EnrollmentStatus.Active)
            .Select(e => e.CourseId).ToListAsync(ct);

        var upcoming = await _db.ExamSchedules.AsNoTracking()
            .Where(e => enrolledCourseIds.Contains(e.CourseId) && e.ScheduledDateTime >= DateTime.UtcNow)
            .Include(e => e.Course)
            .OrderBy(e => e.ScheduledDateTime)
            .Select(e => new
            {
                e.Id,
                courseId = e.CourseId,
                courseTitle = e.Course.Title,
                courseCode = e.Course.Code,
                examType = e.ExamType.ToString(),
                scheduledDateTime = e.ScheduledDateTime,
                e.Venue,
            })
            .ToListAsync(ct);

        var grades = await _db.CourseGrades.AsNoTracking()
            .Where(g => g.StudentId == studentId)
            .OrderByDescending(g => g.PublishedAt)
            .Select(g => new
            {
                g.Id,
                g.CourseId,
                g.LetterGrade,
                g.GradePoints,
                g.PercentageObtained,
                g.IsPassing,
                g.ResultStatus,
                g.PublishedAt,
            })
            .ToListAsync(ct);

        return Ok(new { upcomingExams = upcoming, publishedGrades = grades });
    }

    [HttpGet("transcript")]
    public async Task<IActionResult> Transcript(CancellationToken ct)
    {
        var studentId = StudentId;
        var user = await _db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == studentId, ct);
        var profile = await _db.StudentProfiles.AsNoTracking()
            .FirstOrDefaultAsync(sp => sp.TenantId == user!.TenantId && sp.StudentId == studentId, ct);

        var grades = await _db.CourseGrades.AsNoTracking()
            .Where(g => g.StudentId == studentId && g.ResultStatus == "published")
            .OrderBy(g => g.PublishedAt)
            .ToListAsync(ct);

        var student = await _db.Students.AsNoTracking()
            .SingleOrDefaultAsync(s => s.Id == studentId, ct);

        var courseIds = grades
            .Select(g => g.CourseId)
            .Distinct()
            .ToList();

        var courseTitles = await _db.Courses.AsNoTracking()
            .Where(c => courseIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Title, ct);

        var semesterOrder = grades
            .GroupBy(g => g.SemesterId)
            .Select((g, idx) => new
            {
                semesterId = g.Key,
                index = idx,
                grades = g.ToList()
            })
            .Select(g => new
            {
                semesterIndex = g.index + 1,
                courses = g.grades.Select(cg => new
                {
                    courseId = cg.CourseId,
                    courseTitle = courseTitles.TryGetValue(cg.CourseId, out var title)
                        ? title
                        : "Unknown Course",
                    letterGrade = cg.LetterGrade,
                    gradePoints = cg.GradePoints,
                    percentageObtained = cg.PercentageObtained,
                }).ToList(),
                sgpa = g.grades.Any()
                    ? Math.Round(g.grades.Average(cg => cg.GradePoints), 2)
                    : 0m,
            })
            .ToList();

        var cgpa = grades.Any() ? Math.Round(grades.Average(g => g.GradePoints), 2) : 0m;

        return Ok(new
        {
            studentName = user?.FirstName + " " + user?.LastName,
            universityId = student?.UniversityId,
            semesters = semesterOrder,
            cgpa,
            totalCredits = grades.Count * 3,
        });
    }

    [HttpPost("transcript/official")]
    public async Task<IActionResult> GenerateOfficialTranscript(CancellationToken ct)
    {
        var studentId = StudentId;
        var user = await _db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == studentId, ct);
        if (user is null) return NotFound();

        var student = await _db.Students.AsNoTracking().SingleOrDefaultAsync(s => s.Id == studentId, ct);
        var profile = await _db.StudentProfiles.AsNoTracking()
            .FirstOrDefaultAsync(sp => sp.TenantId == user.TenantId && sp.StudentId == studentId, ct);

        string program = "N/A";
        JsonElement profileJson = default;
        if (profile?.ProfileJson is not null)
        {
            try { profileJson = JsonSerializer.Deserialize<JsonElement>(profile.ProfileJson); } catch { }
            profileJson.TryGetProperty("program", out var pr);
            program = pr.GetString() ?? "N/A";
        }

        var grades = await _db.CourseGrades.AsNoTracking()
            .Where(g => g.StudentId == studentId && g.ResultStatus == "published")
            .OrderBy(g => g.PublishedAt)
            .ToListAsync(ct);

        var cgpa = grades.Any() ? Math.Round(grades.Average(g => g.GradePoints), 2) : 0m;
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{studentId}:{DateTime.UtcNow:O}:{cgpa}"))).ToLowerInvariant()[..16];
        var verificationUrl = $"/api/student-portal/transcript/{hash}/verify";
        var downloadPath = $"/api/student-portal/transcript/{hash}/download";

        var courseRows = grades.Select(g => (dynamic)new
        {
            courseTitle = _db.Courses.AsNoTracking().FirstOrDefault(c => c.Id == g.CourseId)?.Title ?? "Unknown",
            letterGrade = g.LetterGrade ?? "N/A",
            gradePoints = g.GradePoints,
        }).ToList();

        var pdfBytes = GenerateTranscriptPdf(
            studentName: user.FirstName + " " + user.LastName,
            universityId: student?.UniversityId ?? "N/A",
            program: program,
            cgpa: cgpa,
            courses: courseRows,
            verificationUrl: verificationUrl,
            hash: hash
        );

        var signature = await _pqc.SignAsync(user.TenantId, "transcript", pdfBytes, archival: true, ct);

                GeneratedTranscripts[hash] = new GeneratedTranscript(
            pdfBytes,
            DateTime.UtcNow.AddMinutes(15),
            studentId
        );

return Ok(new
        {
            hash,
            downloadUrl = downloadPath,
            verificationUrl,
            algorithm = signature.Algorithm,
            keyId = signature.KeyId,
            signature = Convert.ToBase64String(signature.Signature),
            publicKey = Convert.ToBase64String(signature.PublicKey),
        });
    }

    [HttpGet("transcript/{hash}/download")]
    public IActionResult DownloadTranscript(string hash)
    {
        if (!GeneratedTranscripts.TryGetValue(hash, out var transcript))
        {
            return NotFound(new
            {
                error = "Transcript not found or download link expired."
            });
        }

        if (transcript.ExpiresAt <= DateTime.UtcNow)
        {
            GeneratedTranscripts.TryRemove(hash, out _);

            return NotFound(new
            {
                error = "Transcript download link expired. Generate a new official copy."
            });
        }

        if (transcript.StudentId != StudentId)
            return Forbid();

        return File(
            transcript.Pdf,
            "application/pdf",
            $"AegisCore-Official-Transcript-{hash[..Math.Min(hash.Length, 12)]}.pdf"
        );
    }

    [HttpGet("transcript/{hash}/verify")]
    [AllowAnonymous]
    public IActionResult VerifyTranscript(string hash)
    {
        return Ok(new { verified = false, message = "Verification requires the original signed document. Submit the PDF for verification at /api/student-portal/transcript/verify-upload." });
    }

    [HttpPost("finance/challan/{demandId:guid}")]
    public async Task<IActionResult> GenerateChallan(Guid demandId, CancellationToken ct)
    {
        var studentId = StudentId;
        var user = await _db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == studentId, ct);
        if (user is null) return NotFound();

        var demand = await _db.FeeDemands.AsNoTracking()
            .SingleOrDefaultAsync(f => f.TenantId == user.TenantId && f.StudentId == studentId && f.Id == demandId, ct);
        if (demand is null) return NotFound(new { error = "Fee demand not found." });

        var installments = await _db.FeeInstallments.AsNoTracking()
            .Where(i => i.TenantId == user.TenantId && i.FeeDemandId == demandId)
            .OrderBy(i => i.Sequence).ToListAsync(ct);

        var student = await _db.Students.AsNoTracking().SingleOrDefaultAsync(s => s.Id == studentId, ct);

        var pdfBytes = GenerateChallanPdf(
            studentName: user.FirstName + " " + user.LastName,
            universityId: student?.UniversityId ?? "N/A",
            demandNumber: demand.DemandNumber,
            totalAmount: demand.TotalAmount,
            paidAmount: demand.PaidAmount,
            outstandingAmount: demand.OutstandingAmount,
            dueDate: demand.DueDate,
            currency: demand.Currency,
            installments: installments.Select(i => (dynamic)new { i.Sequence, i.Amount, i.PaidAmount, i.DueDate, i.Status }).ToList()
        );

        return File(pdfBytes, "application/pdf", $"challan-{demand.DemandNumber}.pdf");
    }

    private static byte[] GenerateTranscriptPdf(string studentName, string universityId, string program, decimal cgpa,
        List<dynamic> courses, string verificationUrl, string hash)
    {
        using var doc = new PdfDocument();
        var page = doc.AddPage();
        using var gfx = XGraphics.FromPdfPage(page);
        var fontTitle = new XFont("Helvetica-Bold", 18, XFontStyle.Bold);
        var fontHeader = new XFont("Helvetica-Bold", 12, XFontStyle.Bold);
        var fontBody = new XFont("Helvetica", 10, XFontStyle.Regular);
        var fontSmall = new XFont("Helvetica", 8, XFontStyle.Regular);

        double y = 40;
        gfx.DrawString("Institute of Business Management", fontTitle, XBrushes.Black, new XRect(40, y, page.Width - 80, 30), XStringFormats.TopCenter);
        y += 30;
        gfx.DrawString("Official Academic Transcript", new XFont("Helvetica-Bold", 14, XFontStyle.Bold), XBrushes.Black, new XRect(40, y, page.Width - 80, 20), XStringFormats.TopCenter);
        y += 25;
        gfx.DrawLine(new XPen(XColors.Black, 1), 40, y, page.Width - 40, y);
        y += 15;

        gfx.DrawString($"Student: {studentName}", fontBody, XBrushes.Black, 40, y);
        gfx.DrawString($"University ID: {universityId}", fontBody, XBrushes.Black, 250, y);
        y += 16;
        gfx.DrawString($"Program: {program}", fontBody, XBrushes.Black, 40, y);
        gfx.DrawString($"CGPA: {cgpa:F2}", fontBody, XBrushes.Black, 250, y);
        y += 16;
        gfx.DrawString($"Date: {DateTime.UtcNow:yyyy-MM-dd}", fontBody, XBrushes.Black, 40, y);
        gfx.DrawString($"Verification: {hash}", fontSmall, XBrushes.Black, 250, y);
        y += 20;

        gfx.DrawLine(new XPen(XColors.Black, 0.5), 40, y, page.Width - 40, y);
        y += 10;
        gfx.DrawString("Course", fontHeader, XBrushes.Black, 40, y);
        gfx.DrawString("Grade", fontHeader, XBrushes.Black, 300, y);
        gfx.DrawString("Points", fontHeader, XBrushes.Black, 380, y);
        y += 5;
        gfx.DrawLine(new XPen(XColors.Black, 0.5), 40, y, page.Width - 40, y);
        y += 10;

        foreach (var course in courses)
        {
            gfx.DrawString((string)course.courseTitle, fontBody, XBrushes.Black, 40, y);
            gfx.DrawString((string)course.letterGrade, fontBody, XBrushes.Black, 300, y);
            gfx.DrawString($"{course.gradePoints:F2}", fontBody, XBrushes.Black, 380, y);
            y += 14;
        }

        y += 10;
        gfx.DrawLine(new XPen(XColors.Black, 0.5), 40, y, page.Width - 40, y);
        y += 15;

        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(verificationUrl, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrData);
        var qrBytes = qrCode.GetGraphic(5);
        using var qrImage = XImage.FromStream(() => new MemoryStream(qrBytes));
        gfx.DrawImage(qrImage, 40, y, 80, 80);
        gfx.DrawString("Scan to verify this transcript", fontSmall, XBrushes.Black, 130, y + 35);
        gfx.DrawString("This document is cryptographically signed using PQC (SLH-DSA-SHA2-128s).", fontSmall, XBrushes.Black, 130, y + 50);
        gfx.DrawString("Verify at: " + verificationUrl, fontSmall, XBrushes.Black, 130, y + 65);

        using var ms = new MemoryStream();
        doc.Save(ms, false);
        return ms.ToArray();
    }

    private static byte[] GenerateChallanPdf(string studentName, string universityId, string demandNumber,
        decimal totalAmount, decimal paidAmount, decimal outstandingAmount, DateTime dueDate, string currency,
        List<dynamic> installments)
    {
        using var doc = new PdfDocument();
        var page = doc.AddPage();
        using var gfx = XGraphics.FromPdfPage(page);
        var fontTitle = new XFont("Helvetica-Bold", 16, XFontStyle.Bold);
        var fontHeader = new XFont("Helvetica-Bold", 11, XFontStyle.Bold);
        var fontBody = new XFont("Helvetica", 10, XFontStyle.Regular);

        double y = 40;
        gfx.DrawString("AegisCore — Fee Challan", fontTitle, XBrushes.Black, new XRect(40, y, page.Width - 80, 25), XStringFormats.TopCenter);
        y += 30;
        gfx.DrawLine(new XPen(XColors.Black, 1), 40, y, page.Width - 40, y);
        y += 12;

        gfx.DrawString($"Student: {studentName}", fontBody, XBrushes.Black, 40, y);
        gfx.DrawString($"ID: {universityId}", fontBody, XBrushes.Black, 250, y);
        y += 14;
        gfx.DrawString($"Challan #: {demandNumber}", fontBody, XBrushes.Black, 40, y);
        gfx.DrawString($"Due: {dueDate:yyyy-MM-dd}", fontBody, XBrushes.Black, 250, y);
        y += 14;
        gfx.DrawString($"Total: {currency} {totalAmount:N2}", fontBody, XBrushes.Black, 40, y);
        gfx.DrawString($"Paid: {currency} {paidAmount:N2}", fontBody, XBrushes.Black, 200, y);
        gfx.DrawString($"Outstanding: {currency} {outstandingAmount:N2}", fontHeader, XBrushes.Black, 350, y);
        y += 20;
        gfx.DrawLine(new XPen(XColors.Black, 0.5), 40, y, page.Width - 40, y);
        y += 10;

        gfx.DrawString("Seq", fontHeader, XBrushes.Black, 40, y);
        gfx.DrawString("Amount", fontHeader, XBrushes.Black, 100, y);
        gfx.DrawString("Paid", fontHeader, XBrushes.Black, 200, y);
        gfx.DrawString("Due Date", fontHeader, XBrushes.Black, 300, y);
        gfx.DrawString("Status", fontHeader, XBrushes.Black, 400, y);
        y += 5;
        gfx.DrawLine(new XPen(XColors.Black, 0.5), 40, y, page.Width - 40, y);
        y += 10;

        foreach (var inst in installments)
        {
            gfx.DrawString($"{inst.Sequence}", fontBody, XBrushes.Black, 40, y);
            gfx.DrawString($"{currency} {inst.Amount:N2}", fontBody, XBrushes.Black, 100, y);
            gfx.DrawString($"{currency} {inst.PaidAmount:N2}", fontBody, XBrushes.Black, 200, y);
            gfx.DrawString($"{((DateTime)inst.DueDate):yyyy-MM-dd}", fontBody, XBrushes.Black, 300, y);
            gfx.DrawString((string)inst.Status, fontBody, XBrushes.Black, 400, y);
            y += 14;
        }

        y += 15;
        gfx.DrawLine(new XPen(XColors.Black, 0.5), 40, y, page.Width - 40, y);
        y += 15;
        gfx.DrawString("Pay online via Raast using the AegisCore student portal.", fontBody, XBrushes.Black, 40, y);

        using var ms = new MemoryStream();
        doc.Save(ms, false);
        return ms.ToArray();
    }
}

public sealed record UpdateProfileRequest(string? Phone, string? Address, string? PaymentAlias, string? PreferredName);
public sealed record AttendanceCheckInRequest(Guid CourseId, double? Latitude, double? Longitude);
