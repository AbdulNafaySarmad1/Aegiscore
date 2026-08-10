using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
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
[Route("api/student-documents")]
public sealed class StudentDocumentsController(
    AegisDbContext db,
    IPqcSignatureService pqc,
    IConfiguration configuration,
    ILogger<StudentDocumentsController> logger) : ControllerBase
{
    private Guid StudentId => Guid.TryParse(
        User.FindFirstValue("student_id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier),
        out var id) ? id : Guid.Empty;

    [Authorize(Roles = "Student,SuperAdmin,Admin")]
    [HttpPost("transcript/official")]
    public async Task<IActionResult> GenerateOfficialTranscript(CancellationToken ct)
    {
        var studentId = StudentId;
        if (studentId == Guid.Empty) return Forbid();

        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == studentId, ct);
        var student = await db.Students.AsNoTracking().SingleOrDefaultAsync(x => x.Id == studentId, ct);
        if (user is null || student is null) return NotFound(new { error = "Student record not found." });

        var profile = await db.StudentProfiles.AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == user.TenantId && x.StudentId == studentId, ct);

        var grades = await db.CourseGrades.AsNoTracking()
            .Where(x => x.StudentId == studentId && x.ResultStatus == "published")
            .OrderBy(x => x.PublishedAt)
            .ToListAsync(ct);
        if (grades.Count == 0) return Conflict(new { error = "No published grades are available for an official transcript." });

        var courseIds = grades.Select(x => x.CourseId).Distinct().ToArray();
        var courses = await db.Courses.AsNoTracking()
            .Where(x => courseIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, ct);

        var profileJson = ParseProfile(profile?.ProfileJson);
        var program = StringProperty(profileJson, "program") ?? "N/A";
        var issuedAt = DateTime.UtcNow;
        var documentId = Guid.NewGuid().ToString("N");
        var verificationUrl = BuildPublicUrl($"/api/student-documents/transcript/{documentId}/verify");

        var rows = grades.Select(g =>
        {
            courses.TryGetValue(g.CourseId, out var course);
            return new TranscriptRow(
                g.SemesterId,
                course?.Code ?? "—",
                course?.Title ?? "Unknown Course",
                Math.Max(course?.CreditHours ?? 0, 0),
                g.LetterGrade ?? "N/A",
                g.GradePoints,
                g.PercentageObtained,
                g.PublishedAt);
        }).ToList();

        var pdf = BuildTranscriptPdf(
            studentName: $"{user.FirstName} {user.LastName}".Trim(),
            universityId: student.UniversityId ?? "N/A",
            program: program,
            issuedAt: issuedAt,
            documentId: documentId,
            verificationUrl: verificationUrl,
            rows: rows);

        var signature = await pqc.SignAsync(user.TenantId, "transcript", pdf, archival: true, ct);
        var sha256 = Convert.ToHexString(SHA256.HashData(pdf)).ToLowerInvariant();
        var metadata = new TranscriptMetadata(
            DocumentId: documentId,
            TenantId: user.TenantId,
            StudentId: studentId,
            IssuedAt: issuedAt,
            Sha256: sha256,
            Algorithm: signature.Algorithm,
            KeyId: signature.KeyId,
            SignatureBase64: Convert.ToBase64String(signature.Signature),
            PublicKeyBase64: Convert.ToBase64String(signature.PublicKey));

        await PersistAsync(metadata, pdf, ct);

        return Ok(new
        {
            hash = documentId,
            documentId,
            downloadUrl = $"/api/student-documents/transcript/{documentId}/download",
            verificationUrl,
            algorithm = signature.Algorithm,
            keyId = signature.KeyId,
            signature = metadata.SignatureBase64,
            publicKey = metadata.PublicKeyBase64,
            sha256,
            issuedAt,
        });
    }

    [Authorize(Roles = "Student,SuperAdmin,Admin")]
    [HttpGet("transcript/{documentId}/download")]
    public async Task<IActionResult> Download(string documentId, CancellationToken ct)
    {
        if (!IsDocumentId(documentId)) return BadRequest(new { error = "Invalid document identifier." });
        var stored = await LoadAsync(documentId, ct);
        if (stored is null) return NotFound(new { error = "Transcript not found." });

        if (!User.IsInRole("SuperAdmin") && !User.IsInRole("Admin") && stored.Value.Metadata.StudentId != StudentId)
            return Forbid();

        var valid = Verify(stored.Value.Metadata, stored.Value.Pdf);
        if (!valid)
        {
            logger.LogError("Stored transcript {DocumentId} failed PQC verification before download", documentId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Transcript integrity verification failed." });
        }

        return File(stored.Value.Pdf, "application/pdf", $"official-transcript-{documentId[..12]}.pdf");
    }

    [AllowAnonymous]
    [HttpGet("transcript/{documentId}/verify")]
    public async Task<IActionResult> VerifyDocument(string documentId, CancellationToken ct)
    {
        if (!IsDocumentId(documentId)) return BadRequest(new { verified = false, error = "Invalid document identifier." });
        var stored = await LoadAsync(documentId, ct);
        if (stored is null) return NotFound(new { verified = false, error = "Transcript not found." });

        var actualHash = Convert.ToHexString(SHA256.HashData(stored.Value.Pdf)).ToLowerInvariant();
        var verified = CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(actualHash),
                Convert.FromHexString(stored.Value.Metadata.Sha256))
            && Verify(stored.Value.Metadata, stored.Value.Pdf);

        return Ok(new
        {
            verified,
            documentId = stored.Value.Metadata.DocumentId,
            issuedAt = stored.Value.Metadata.IssuedAt,
            algorithm = stored.Value.Metadata.Algorithm,
            keyId = stored.Value.Metadata.KeyId,
            sha256 = actualHash,
            message = verified
                ? "This AegisCore transcript is authentic and its stored PDF matches the PQC signature."
                : "Transcript verification failed. Do not trust this document."
        });
    }

    private bool Verify(TranscriptMetadata metadata, byte[] pdf)
    {
        try
        {
            return pqc.Verify(
                metadata.Algorithm,
                pdf,
                Convert.FromBase64String(metadata.SignatureBase64),
                Convert.FromBase64String(metadata.PublicKeyBase64));
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private async Task PersistAsync(TranscriptMetadata metadata, byte[] pdf, CancellationToken ct)
    {
        var directory = StorageDirectory(metadata.TenantId);
        Directory.CreateDirectory(directory);
        var pdfPath = Path.Combine(directory, $"{metadata.DocumentId}.pdf");
        var metadataPath = Path.Combine(directory, $"{metadata.DocumentId}.json");

        var pdfTemp = pdfPath + ".tmp";
        var metadataTemp = metadataPath + ".tmp";
        await System.IO.File.WriteAllBytesAsync(pdfTemp, pdf, ct);
        await System.IO.File.WriteAllTextAsync(metadataTemp, JsonSerializer.Serialize(metadata), ct);
        System.IO.File.Move(pdfTemp, pdfPath, true);
        System.IO.File.Move(metadataTemp, metadataPath, true);
    }

    private async Task<(TranscriptMetadata Metadata, byte[] Pdf)?> LoadAsync(string documentId, CancellationToken ct)
    {
        var root = StorageRoot();
        if (!Directory.Exists(root)) return null;

        // Document IDs are random and tenant metadata is authoritative. Search tenant directories
        // rather than trusting a caller-supplied tenant path.
        foreach (var metadataPath in Directory.EnumerateFiles(root, $"{documentId}.json", SearchOption.AllDirectories))
        {
            var metadata = JsonSerializer.Deserialize<TranscriptMetadata>(await System.IO.File.ReadAllTextAsync(metadataPath, ct));
            if (metadata is null || metadata.DocumentId != documentId) continue;
            var pdfPath = Path.ChangeExtension(metadataPath, ".pdf");
            if (!System.IO.File.Exists(pdfPath)) return null;
            return (metadata, await System.IO.File.ReadAllBytesAsync(pdfPath, ct));
        }
        return null;
    }

    private string StorageDirectory(Guid tenantId) => Path.Combine(StorageRoot(), tenantId.ToString("D"));

    private string StorageRoot()
    {
        var basePath = configuration["AEGISCORE_STORAGE_LOCAL_PATH"]
            ?? configuration["Storage:LocalPath"]
            ?? Path.Combine(AppContext.BaseDirectory, "storage");
        return Path.Combine(basePath, "official-transcripts");
    }

    private string BuildPublicUrl(string path)
    {
        var configured = configuration["AEGISCORE_FRONTEND_URL"];
        if (Uri.TryCreate(configured, UriKind.Absolute, out var baseUri))
            return new Uri(baseUri, path).ToString();
        return $"{Request.Scheme}://{Request.Host}{path}";
    }

    private static bool IsDocumentId(string value) => value.Length == 32 && value.All(Uri.IsHexDigit);

    private static JsonElement ParseProfile(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return default;
        try { return JsonSerializer.Deserialize<JsonElement>(json); }
        catch (JsonException) { return default; }
    }

    private static string? StringProperty(JsonElement json, string name)
        => json.ValueKind == JsonValueKind.Object && json.TryGetProperty(name, out var value) ? value.GetString() : null;

    private static byte[] BuildTranscriptPdf(
        string studentName,
        string universityId,
        string program,
        DateTime issuedAt,
        string documentId,
        string verificationUrl,
        IReadOnlyList<TranscriptRow> rows)
    {
        using var document = new PdfDocument();
        var semesterGroups = rows.GroupBy(x => x.SemesterId).ToList();
        var totalCredits = rows.Sum(x => x.CreditHours);
        var weightedPoints = rows.Sum(x => x.GradePoints * x.CreditHours);
        var cgpa = totalCredits > 0 ? Math.Round(weightedPoints / totalCredits, 2) : Math.Round(rows.Average(x => x.GradePoints), 2);

        var title = new XFont("Helvetica-Bold", 17, XFontStyle.Bold);
        var subtitle = new XFont("Helvetica-Bold", 12, XFontStyle.Bold);
        var header = new XFont("Helvetica-Bold", 8.5, XFontStyle.Bold);
        var body = new XFont("Helvetica", 8.5, XFontStyle.Regular);
        var small = new XFont("Helvetica", 7, XFontStyle.Regular);
        var boldSmall = new XFont("Helvetica-Bold", 7, XFontStyle.Bold);

        PdfPage? page = null;
        XGraphics? gfx = null;
        double y = 0;

        void NewPage(bool continuation)
        {
            gfx?.Dispose();
            page = document.AddPage();
            page.Size = PdfSharpCore.PageSize.A4;
            gfx = XGraphics.FromPdfPage(page);
            y = 34;
            gfx.DrawString("INSTITUTE OF BUSINESS MANAGEMENT", title, XBrushes.Black, new XRect(36, y, page.Width - 72, 22), XStringFormats.TopCenter);
            y += 23;
            gfx.DrawString(continuation ? "OFFICIAL ACADEMIC TRANSCRIPT — CONTINUED" : "OFFICIAL ACADEMIC TRANSCRIPT", subtitle, XBrushes.Black, new XRect(36, y, page.Width - 72, 18), XStringFormats.TopCenter);
            y += 22;
            gfx.DrawLine(new XPen(XColors.Black, 0.8), 36, y, page.Width - 36, y);
            y += 12;
        }

        void Ensure(double required)
        {
            if (page is null || y + required > page.Height - 120) NewPage(page is not null);
        }

        NewPage(false);
        gfx!.DrawString($"Student Name: {studentName}", body, XBrushes.Black, 36, y);
        gfx.DrawString($"Student ID: {universityId}", body, XBrushes.Black, 310, y);
        y += 14;
        gfx.DrawString($"Program: {program}", body, XBrushes.Black, 36, y);
        gfx.DrawString($"Issued: {issuedAt:dd-MMM-yyyy}", body, XBrushes.Black, 310, y);
        y += 14;
        gfx.DrawString($"Document ID: {documentId}", small, XBrushes.Black, 36, y);
        y += 16;

        for (var semesterIndex = 0; semesterIndex < semesterGroups.Count; semesterIndex++)
        {
            var group = semesterGroups[semesterIndex].ToList();
            Ensure(42 + group.Count * 13);
            gfx!.DrawString($"SEMESTER {semesterIndex + 1}", header, XBrushes.Black, 36, y);
            y += 12;
            gfx.DrawRectangle(new XPen(XColors.Black, 0.5), 36, y, 523, 18);
            gfx.DrawString("Code", header, XBrushes.Black, 40, y + 5);
            gfx.DrawString("Course Title", header, XBrushes.Black, 92, y + 5);
            gfx.DrawString("CH", header, XBrushes.Black, 390, y + 5);
            gfx.DrawString("Grade", header, XBrushes.Black, 430, y + 5);
            gfx.DrawString("GP", header, XBrushes.Black, 500, y + 5);
            y += 18;

            foreach (var row in group)
            {
                Ensure(14);
                gfx.DrawLine(new XPen(XColors.Gray, 0.25), 36, y + 12, 559, y + 12);
                gfx.DrawString(row.CourseCode, body, XBrushes.Black, 40, y + 2);
                gfx.DrawString(Trim(row.CourseTitle, 52), body, XBrushes.Black, 92, y + 2);
                gfx.DrawString(row.CreditHours.ToString(), body, XBrushes.Black, 395, y + 2);
                gfx.DrawString(row.LetterGrade, body, XBrushes.Black, 434, y + 2);
                gfx.DrawString($"{row.GradePoints:F2}", body, XBrushes.Black, 500, y + 2);
                y += 13;
            }

            var semCredits = group.Sum(x => x.CreditHours);
            var semWeighted = group.Sum(x => x.GradePoints * x.CreditHours);
            var sgpa = semCredits > 0 ? semWeighted / semCredits : group.Average(x => x.GradePoints);
            y += 4;
            gfx.DrawString($"Semester GPA: {sgpa:F2}", boldSmall, XBrushes.Black, 430, y);
            y += 17;
        }

        Ensure(135);
        gfx!.DrawLine(new XPen(XColors.Black, 0.8), 36, y, 559, y);
        y += 10;
        gfx.DrawString($"Total Credit Hours: {totalCredits}", header, XBrushes.Black, 36, y);
        gfx.DrawString($"Cumulative GPA: {cgpa:F2}", header, XBrushes.Black, 190, y);
        y += 18;

        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(verificationUrl, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrData);
        using var qrImage = XImage.FromStream(() => new MemoryStream(qrCode.GetGraphic(5)));
        gfx.DrawImage(qrImage, 36, y, 72, 72);
        gfx.DrawString("SCAN TO VERIFY", boldSmall, XBrushes.Black, 118, y + 8);
        gfx.DrawString("This official transcript is digitally signed with a post-quantum", small, XBrushes.Black, 118, y + 22);
        gfx.DrawString("SLH-DSA archival signature. The verification service checks both", small, XBrushes.Black, 118, y + 32);
        gfx.DrawString("the stored PDF hash and its detached PQC signature.", small, XBrushes.Black, 118, y + 42);
        gfx.DrawString($"Verification: {Trim(verificationUrl, 74)}", small, XBrushes.Black, 118, y + 57);
        y += 82;

        gfx.DrawLine(new XPen(XColors.Black, 0.5), 36, y, 180, y);
        gfx.DrawLine(new XPen(XColors.Black, 0.5), 415, y, 559, y);
        gfx.DrawString("Prepared / Checked", small, XBrushes.Black, 36, y + 4);
        gfx.DrawString("Controller of Examinations", small, XBrushes.Black, 415, y + 4);
        y += 23;
        gfx.DrawString("This electronically generated transcript is valid only when its QR verification reports VERIFIED.", small, XBrushes.Black, 36, y);

        gfx.Dispose();
        using var stream = new MemoryStream();
        document.Save(stream, false);
        return stream.ToArray();
    }

    private static string Trim(string value, int max) => value.Length <= max ? value : value[..(max - 1)] + "…";

    private sealed record TranscriptRow(
        Guid SemesterId,
        string CourseCode,
        string CourseTitle,
        int CreditHours,
        string LetterGrade,
        decimal GradePoints,
        decimal Percentage,
        DateTime? PublishedAt);

    private sealed record TranscriptMetadata(
        string DocumentId,
        Guid TenantId,
        Guid StudentId,
        DateTime IssuedAt,
        string Sha256,
        string Algorithm,
        string KeyId,
        string SignatureBase64,
        string PublicKeyBase64);
}
