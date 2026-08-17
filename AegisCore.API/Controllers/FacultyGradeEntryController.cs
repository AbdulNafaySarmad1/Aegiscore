using System.IO.Compression;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using AegisCore.Application.AI;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.API.Controllers;

[ApiController]
[Route("api/faculty-grade-entry")]
[Authorize(Roles = "Faculty,HOD,ExamController,Admin,SuperAdmin")]
public sealed class FacultyGradeEntryController(AegisDbContext db, IDocumentOcrService ocr) : ControllerBase
{
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

    [HttpGet("course/{courseId:guid}")]
    public async Task<IActionResult> Course(Guid courseId, CancellationToken ct)
    {
        if (!await CanTeachOrReview(courseId, ct)) return Forbid();

        var scheme = await db.AssessmentSchemes.AsNoTracking()
            .Include(s => s.Components)
            .Where(s => s.CourseId == courseId)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(ct);
        if (scheme is null) return NotFound(new { error = "No assessment scheme configured for this course." });

        var enrollments = await db.Enrollments.AsNoTracking()
            .Where(e => e.CourseId == courseId)
            .Include(e => e.Student)
            .OrderBy(e => e.Student.UniversityId)
            .ToListAsync(ct);

        var studentIds = enrollments.Select(e => e.StudentId).ToList();
        var existing = await db.CourseMarks.AsNoTracking()
            .Where(m => m.CourseId == courseId && studentIds.Contains(m.StudentId))
            .ToListAsync(ct);

        return Ok(new
        {
            scheme = new
            {
                scheme.Id,
                scheme.SemesterId,
                components = scheme.Components.OrderBy(c => c.OrderIndex).Select(c => new
                {
                    c.Id,
                    name = c.ComponentType,
                    c.TotalMarks,
                    c.WeightPercentage,
                    c.PassingMarks,
                    c.IsMandatory
                })
            },
            students = enrollments.Select(e => new
            {
                enrollmentId = e.Id,
                studentId = e.StudentId,
                name = $"{e.Student.FirstName} {e.Student.LastName}".Trim(),
                universityId = e.Student.UniversityId,
                marks = existing.Where(m => m.StudentId == e.StudentId)
                    .ToDictionary(m => m.AssessmentComponentId, m => m.ObtainedMarks)
            })
        });
    }

    [HttpPut("marks")]
    public async Task<IActionResult> PutMark([FromBody] GradeMarkRequest request, CancellationToken ct)
    {
        var result = await ApplyOrRequestApproval(request, ct);
        return result.Kind switch
        {
            "saved" => Ok(new { status = "saved", result.Message }),
            "approval_required" => Accepted(new { status = "approval_required", moderationId = result.ModerationId, result.Message }),
            "forbidden" => Forbid(),
            _ => BadRequest(new { error = result.Message })
        };
    }

    [HttpGet("course/{courseId:guid}/template.csv")]
    public async Task<IActionResult> CsvTemplate(Guid courseId, CancellationToken ct)
    {
        if (!await CanTeachOrReview(courseId, ct)) return Forbid();
        var scheme = await db.AssessmentSchemes.AsNoTracking().Include(s => s.Components)
            .Where(s => s.CourseId == courseId).OrderByDescending(s => s.CreatedAt).FirstOrDefaultAsync(ct);
        if (scheme is null) return NotFound();
        var students = await db.Enrollments.AsNoTracking().Where(e => e.CourseId == courseId).Include(e => e.Student)
            .OrderBy(e => e.Student.UniversityId).ToListAsync(ct);

        var components = scheme.Components.OrderBy(c => c.OrderIndex).ToList();
        var sb = new StringBuilder();
        sb.Append("UniversityId,StudentName");
        foreach (var c in components) sb.Append(',').Append(Csv(c.ComponentType));
        sb.AppendLine();
        foreach (var e in students)
        {
            sb.Append(Csv(e.Student.UniversityId ?? string.Empty)).Append(',')
              .Append(Csv($"{e.Student.FirstName} {e.Student.LastName}".Trim()));
            foreach (var _ in components) sb.Append(',');
            sb.AppendLine();
        }
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"grade-sheet-{courseId:N}.csv");
    }

    [HttpPost("course/{courseId:guid}/import-preview")]
    [RequestSizeLimit(12 * 1024 * 1024)]
    public async Task<IActionResult> ImportPreview(Guid courseId, IFormFile file, CancellationToken ct)
    {
        if (!await CanTeachOrReview(courseId, ct)) return Forbid();
        if (file.Length <= 0 || file.Length > 12 * 1024 * 1024) return BadRequest(new { error = "File must be between 1 byte and 12MB." });

        var scheme = await db.AssessmentSchemes.AsNoTracking().Include(s => s.Components)
            .Where(s => s.CourseId == courseId).OrderByDescending(s => s.CreatedAt).FirstOrDefaultAsync(ct);
        if (scheme is null) return NotFound(new { error = "No assessment scheme configured." });

        List<List<string>> matrix;
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        await using var stream = file.OpenReadStream();
        if (ext == ".csv") matrix = ParseCsv(await new StreamReader(stream).ReadToEndAsync(ct));
        else if (ext == ".xlsx") matrix = ParseXlsx(stream);
        else if (ext == ".pdf")
        {
            var extracted = await ocr.ExtractTextAsync(stream, file.FileName, ct);
            matrix = ParseLooseText(extracted.FullText);
        }
        else return BadRequest(new { error = "Supported grade sheets are .xlsx, .csv and .pdf." });

        var preview = await BuildPreview(courseId, scheme, matrix, ct);
        return Ok(new { source = ext.TrimStart('.'), rows = preview, warnings = preview.Where(x => x.Error is not null).Select(x => x.Error).Distinct() });
    }

    [HttpPost("import-commit")]
    public async Task<IActionResult> ImportCommit([FromBody] GradeImportCommitRequest request, CancellationToken ct)
    {
        var saved = 0;
        var approvalRequired = 0;
        var errors = new List<string>();
        foreach (var entry in request.Entries)
        {
            var result = await ApplyOrRequestApproval(new GradeMarkRequest(request.CourseId, entry.StudentId, entry.AssessmentComponentId, entry.ObtainedMarks), ct);
            if (result.Kind == "saved") saved++;
            else if (result.Kind == "approval_required") approvalRequired++;
            else errors.Add(result.Message);
        }
        return Ok(new { saved, approvalRequired, errors });
    }

    [Authorize(Roles = "HOD,ExamController,Admin,SuperAdmin")]
    [HttpGet("course/{courseId:guid}/pending-approvals")]
    public async Task<IActionResult> Pending(Guid courseId, CancellationToken ct)
    {
        if (!await CanReviewDepartment(courseId, ct)) return Forbid();
        var rows = await db.MarkModerations.AsNoTracking()
            .Where(m => m.CourseId == courseId && m.Status == "submitted" && m.ReviewComments != null && m.ReviewComments.StartsWith("GRADE_CHANGE:"))
            .OrderBy(m => m.SubmittedAt).ToListAsync(ct);
        return Ok(rows.Select(m => new { m.Id, m.CourseId, m.SemesterId, m.SubmittedBy, m.SubmittedAt, detail = ParseDetail(m.ReviewComments) }));
    }

    [Authorize(Roles = "HOD,ExamController,Admin,SuperAdmin")]
    [HttpPost("approvals/{moderationId:guid}/review")]
    public async Task<IActionResult> Review(Guid moderationId, [FromBody] GradeApprovalReview request, CancellationToken ct)
    {
        var moderation = await db.MarkModerations.FirstOrDefaultAsync(m => m.Id == moderationId, ct);
        if (moderation is null) return NotFound();
        if (!await CanReviewDepartment(moderation.CourseId, ct)) return Forbid();
        if (moderation.Status != "submitted") return Conflict(new { error = "This request has already been reviewed." });
        if (request.Decision is not ("approved" or "rejected")) return BadRequest(new { error = "Decision must be approved or rejected." });

        var detail = ParseDetail(moderation.ReviewComments);
        if (detail is null) return Conflict(new { error = "The moderation record is not an individual grade-change request." });

        if (request.Decision == "approved")
        {
            var mark = await db.CourseMarks.FirstOrDefaultAsync(m => m.CourseId == moderation.CourseId && m.StudentId == detail.StudentId && m.AssessmentComponentId == detail.AssessmentComponentId, ct);
            if (mark is null) return NotFound(new { error = "Original mark no longer exists." });
            mark.ObtainedMarks = detail.RequestedMark;
            mark.EnteredBy = moderation.SubmittedBy;
            mark.EntryDate = DateTimeOffset.UtcNow;
        }

        moderation.Status = request.Decision;
        moderation.ReviewedBy = UserId;
        moderation.ReviewedAt = DateTimeOffset.UtcNow;
        moderation.ReviewComments = moderation.ReviewComments + "\nREVIEW:" + JsonSerializer.Serialize(new { request.Decision, request.Comments, reviewedBy = UserId, reviewedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(ct);
        return Ok(new { status = request.Decision, applied = request.Decision == "approved" });
    }

    private async Task<ApplyResult> ApplyOrRequestApproval(GradeMarkRequest request, CancellationToken ct)
    {
        if (!await CanTeachOrReview(request.CourseId, ct)) return new("forbidden", "Not authorized for this course.", null);
        var component = await db.AssessmentComponents.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.AssessmentComponentId, ct);
        if (component is null || request.ObtainedMarks < 0 || request.ObtainedMarks > component.TotalMarks)
            return new("error", "Marks are outside the component bounds.", null);

        var mark = await db.CourseMarks.FirstOrDefaultAsync(m => m.CourseId == request.CourseId && m.StudentId == request.StudentId && m.AssessmentComponentId == request.AssessmentComponentId, ct);
        if (mark is null)
        {
            db.CourseMarks.Add(new CourseMark { Id = Guid.NewGuid(), CourseId = request.CourseId, StudentId = request.StudentId, AssessmentComponentId = request.AssessmentComponentId, ObtainedMarks = request.ObtainedMarks, EnteredBy = UserId, EntryDate = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync(ct);
            return new("saved", "Initial mark saved.", null);
        }

        if (mark.ObtainedMarks == request.ObtainedMarks) return new("saved", "No change required.", null);
        var passMark = component.PassingMarks ?? (int)Math.Ceiling(component.TotalMarks * 0.5m);
        var crossesPassBoundary = (mark.ObtainedMarks < passMark && request.ObtainedMarks >= passMark) || (mark.ObtainedMarks >= passMark && request.ObtainedMarks < passMark);
        var delta = Math.Abs(request.ObtainedMarks - mark.ObtainedMarks);
        var sensitive = delta >= 5 || crossesPassBoundary;

        if (!sensitive || User.IsInRole("HOD") || User.IsInRole("ExamController") || User.IsInRole("Admin") || User.IsInRole("SuperAdmin"))
        {
            mark.ObtainedMarks = request.ObtainedMarks;
            mark.EnteredBy = UserId;
            mark.EntryDate = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            return new("saved", sensitive ? "Sensitive change applied by authorized reviewer." : "Mark updated.", null);
        }

        var scheme = await db.AssessmentSchemes.AsNoTracking().FirstOrDefaultAsync(s => s.Id == component.SchemeId, ct);
        if (scheme is null) return new("error", "Assessment scheme is missing.", null);
        var pendingDuplicate = await db.MarkModerations.AnyAsync(m => m.CourseId == request.CourseId && m.Status == "submitted" && m.ReviewComments != null && m.ReviewComments.Contains(request.StudentId.ToString()), ct);
        if (pendingDuplicate) return new("approval_required", "A sensitive grade-change request for this student is already pending review.", null);

        var detail = new GradeChangeDetail(request.StudentId, request.AssessmentComponentId, mark.ObtainedMarks, request.ObtainedMarks, delta, passMark, crossesPassBoundary);
        var moderation = new MarkModeration
        {
            Id = Guid.NewGuid(), CourseId = request.CourseId, SemesterId = scheme.SemesterId, Status = "submitted",
            SubmittedBy = UserId, SubmittedAt = DateTimeOffset.UtcNow,
            ReviewComments = "GRADE_CHANGE:" + JsonSerializer.Serialize(detail)
        };
        db.MarkModerations.Add(moderation);
        await db.SaveChangesAsync(ct);
        return new("approval_required", crossesPassBoundary ? "Fail/pass boundary change sent to the department head for approval." : "Change of 5 or more marks sent to the department head for approval.", moderation.Id);
    }

    private async Task<bool> CanTeachOrReview(Guid courseId, CancellationToken ct)
    {
        if (User.IsInRole("Admin") || User.IsInRole("SuperAdmin") || User.IsInRole("ExamController")) return true;
        if (User.IsInRole("HOD") && await CanReviewDepartment(courseId, ct)) return true;
        return await db.Timetables.AsNoTracking().AnyAsync(t => t.CourseId == courseId && t.FacultyId == UserId, ct);
    }

    private async Task<bool> CanReviewDepartment(Guid courseId, CancellationToken ct)
    {
        if (User.IsInRole("Admin") || User.IsInRole("SuperAdmin") || User.IsInRole("ExamController")) return true;
        if (!User.IsInRole("HOD")) return false;
        var courseDepartment = await db.Courses.AsNoTracking().Where(c => c.Id == courseId).Select(c => c.DepartmentId).FirstOrDefaultAsync(ct);
        var reviewerDepartment = await db.Faculties.AsNoTracking().Where(f => f.Id == UserId).Select(f => f.DepartmentId).FirstOrDefaultAsync(ct);
        return courseDepartment != Guid.Empty && courseDepartment == reviewerDepartment;
    }

    private async Task<List<PreviewRow>> BuildPreview(Guid courseId, AssessmentScheme scheme, List<List<string>> matrix, CancellationToken ct)
    {
        var output = new List<PreviewRow>();
        if (matrix.Count < 2) return output;
        var headers = matrix[0].Select(x => x.Trim()).ToList();
        var components = scheme.Components.ToList();
        var students = await db.Enrollments.AsNoTracking().Where(e => e.CourseId == courseId).Include(e => e.Student).ToListAsync(ct);
        for (var rowIndex = 1; rowIndex < matrix.Count; rowIndex++)
        {
            var row = matrix[rowIndex];
            if (row.Count == 0 || string.IsNullOrWhiteSpace(row[0])) continue;
            var student = students.FirstOrDefault(e => string.Equals(e.Student.UniversityId, row[0].Trim(), StringComparison.OrdinalIgnoreCase));
            if (student is null) { output.Add(new(Guid.Empty, string.Empty, Guid.Empty, 0, $"Unknown student ID {row[0]}")); continue; }
            for (var col = 2; col < Math.Min(headers.Count, row.Count); col++)
            {
                if (string.IsNullOrWhiteSpace(row[col])) continue;
                var component = components.FirstOrDefault(c => string.Equals(c.ComponentType, headers[col], StringComparison.OrdinalIgnoreCase));
                if (component is null) { output.Add(new(student.StudentId, student.Student.UniversityId ?? "", Guid.Empty, 0, $"Unknown assessment column {headers[col]}")); continue; }
                if (!int.TryParse(row[col].Trim(), out var value) || value < 0 || value > component.TotalMarks)
                { output.Add(new(student.StudentId, student.Student.UniversityId ?? "", component.Id, 0, $"Invalid mark '{row[col]}' for {student.Student.UniversityId}/{component.ComponentType}")); continue; }
                output.Add(new(student.StudentId, student.Student.UniversityId ?? "", component.Id, value, null));
            }
        }
        return output;
    }

    private static List<List<string>> ParseCsv(string text) => text.Replace("\r", "").Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(ParseCsvLine).ToList();
    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>(); var current = new StringBuilder(); var quoted = false;
        for (var i = 0; i < line.Length; i++) { var c = line[i]; if (c == '"') { if (quoted && i + 1 < line.Length && line[i + 1] == '"') { current.Append('"'); i++; } else quoted = !quoted; } else if (c == ',' && !quoted) { result.Add(current.ToString()); current.Clear(); } else current.Append(c); }
        result.Add(current.ToString()); return result;
    }
    private static string Csv(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";

    private static List<List<string>> ParseLooseText(string text)
    {
        var lines = text.Replace("\r", "").Split('\n', StringSplitOptions.RemoveEmptyEntries);
        return lines.Select(line => line.Contains(',') ? ParseCsvLine(line) : line.Split(new[] { '\t', '|' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList()).Where(r => r.Count > 1).ToList();
    }

    private static List<List<string>> ParseXlsx(Stream stream)
    {
        using var copy = new MemoryStream(); stream.CopyTo(copy); copy.Position = 0;
        using var zip = new ZipArchive(copy, ZipArchiveMode.Read, leaveOpen: false);
        var shared = new List<string>();
        var sharedEntry = zip.GetEntry("xl/sharedStrings.xml");
        if (sharedEntry is not null) { using var sr = sharedEntry.Open(); var doc = XDocument.Load(sr); XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"; shared = doc.Descendants(ns + "si").Select(si => string.Concat(si.Descendants(ns + "t").Select(t => t.Value))).ToList(); }
        var sheet = zip.GetEntry("xl/worksheets/sheet1.xml") ?? throw new InvalidDataException("The workbook does not contain sheet1.");
        using var ss = sheet.Open(); var xdoc = XDocument.Load(ss); XNamespace main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var rows = new List<List<string>>();
        foreach (var row in xdoc.Descendants(main + "row"))
        {
            var values = new List<string>(); var expected = 0;
            foreach (var cell in row.Elements(main + "c"))
            {
                var reference = (string?)cell.Attribute("r") ?? "A1"; var col = ColumnIndex(reference);
                while (expected < col) { values.Add(string.Empty); expected++; }
                var raw = cell.Element(main + "v")?.Value ?? cell.Element(main + "is")?.Element(main + "t")?.Value ?? string.Empty;
                if ((string?)cell.Attribute("t") == "s" && int.TryParse(raw, out var idx) && idx >= 0 && idx < shared.Count) raw = shared[idx];
                values.Add(raw); expected++;
            }
            rows.Add(values);
        }
        return rows;
    }
    private static int ColumnIndex(string reference) { var n = 0; foreach (var c in reference.TakeWhile(char.IsLetter)) n = n * 26 + (char.ToUpperInvariant(c) - 'A' + 1); return Math.Max(0, n - 1); }
    private static GradeChangeDetail? ParseDetail(string? comments)
    {
        if (string.IsNullOrWhiteSpace(comments) || !comments.StartsWith("GRADE_CHANGE:")) return null;
        var json = comments["GRADE_CHANGE:".Length..].Split("\nREVIEW:", 2)[0];
        try { return JsonSerializer.Deserialize<GradeChangeDetail>(json); } catch { return null; }
    }
}

public sealed record GradeMarkRequest(Guid CourseId, Guid StudentId, Guid AssessmentComponentId, int ObtainedMarks);
public sealed record GradeImportEntry(Guid StudentId, Guid AssessmentComponentId, int ObtainedMarks);
public sealed record GradeImportCommitRequest(Guid CourseId, IReadOnlyList<GradeImportEntry> Entries);
public sealed record GradeApprovalReview(string Decision, string? Comments);
public sealed record GradeChangeDetail(Guid StudentId, Guid AssessmentComponentId, int PreviousMark, int RequestedMark, int Delta, int PassMark, bool CrossesPassBoundary);
public sealed record PreviewRow(Guid StudentId, string UniversityId, Guid AssessmentComponentId, int ObtainedMarks, string? Error);
internal sealed record ApplyResult(string Kind, string Message, Guid? ModerationId);
