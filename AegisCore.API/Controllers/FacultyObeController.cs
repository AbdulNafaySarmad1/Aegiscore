using System.Collections.Concurrent;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
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
[Route("api/obe/faculty")]
[Authorize(Roles = "Faculty,HOD,Dean,OBECoordinator,Registrar,Admin,SuperAdmin")]
public sealed class FacultyObeController(AegisDbContext db, IPqcSignatureService pqc) : ControllerBase
{
    private sealed record SignedObeDocument(
        byte[] Pdf,
        DateTime ExpiresAt,
        Guid OwnerId,
        Guid TenantId,
        byte[] ManifestBytes,
        string Algorithm,
        string KeyId,
        byte[] Signature,
        byte[] PublicKey,
        string Digest,
        string CourseCode,
        string CourseTitle,
        string IssuerName,
        DateTime GeneratedAtUtc,
        string GeoId,
        string EdgeRay,
        string ClientIp);

    private static readonly ConcurrentDictionary<Guid, SignedObeDocument> Documents = new();

    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

    [HttpGet("overview")]
    public async Task<IActionResult> Overview(CancellationToken ct)
    {
        var userId = UserId;
        if (userId == Guid.Empty) return Unauthorized(new { error = "Authenticated faculty identity is missing." });

        var user = await db.Users.AsNoTracking().IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return Unauthorized(new { error = "Authenticated user does not exist." });

        var elevated = User.IsInRole("HOD") || User.IsInRole("Dean") || User.IsInRole("OBECoordinator") || User.IsInRole("Registrar") || User.IsInRole("Admin") || User.IsInRole("SuperAdmin");

        var courseIds = elevated
            ? await db.Courses.AsNoTracking().Select(c => c.Id).ToListAsync(ct)
            : await db.Timetables.AsNoTracking().Where(t => t.FacultyId == userId).Select(t => t.CourseId).Distinct().ToListAsync(ct);

        var courses = await db.Courses.AsNoTracking()
            .Where(c => courseIds.Contains(c.Id))
            .OrderBy(c => c.Code)
            .ToListAsync(ct);

        var result = new List<object>();
        foreach (var course in courses)
        {
            var mappings = await db.CloPlomappings.AsNoTracking()
                .Where(m => m.CourseId == course.Id)
                .OrderBy(m => m.CloCode).ThenBy(m => m.PloCode)
                .Select(m => new { clo = m.CloCode, plo = m.PloCode, alignment = m.AlignmentLevel.ToString() })
                .ToListAsync(ct);

            var enrollmentCount = await db.Enrollments.AsNoTracking().CountAsync(e => e.CourseId == course.Id, ct);
            var results = await db.Results.AsNoTracking().Where(r => r.CourseId == course.Id).ToListAsync(ct);
            var attendance = await db.Attendances.AsNoTracking().Where(a => a.CourseId == course.Id).ToListAsync(ct);

            var avgAssessment = results.Count == 0
                ? (decimal?)null
                : Math.Round(results.Where(r => r.TotalMarks > 0).Select(r => r.TotalMarks > 0 ? r.ObtainedMarks / r.TotalMarks * 100m : 0m).DefaultIfEmpty(0m).Average(), 2);

            result.Add(new
            {
                id = course.Id,
                code = course.Code ?? "COURSE",
                title = course.Title,
                creditHours = course.CreditHours,
                enrollmentCount,
                assessmentRecords = results.Count,
                averageAssessmentPercentage = avgAssessment,
                attendanceRecords = attendance.Count,
                mappedClos = mappings.Select(m => m.clo).Distinct().Count(),
                mappedPlos = mappings.Select(m => m.plo).Distinct().Count(),
                mappings
            });
        }

        return Ok(new
        {
            faculty = new { id = user.Id, name = $"{user.FirstName} {user.LastName}".Trim(), email = user.Email },
            tenantId = user.TenantId,
            courses = result,
            reportSecurity = new
            {
                pqc = "SLH-DSA-SHA2-128s",
                qrVerification = true,
                auditMetadata = new[] { "document UUID", "issuer ID", "tenant ID", "UTC timestamp", "client IP", "Geo-ID/CF country", "Cloudflare Ray ID" }
            }
        });
    }

    [HttpPost("course/{courseId:guid}/signed-report")]
    public async Task<IActionResult> GenerateSignedReport(Guid courseId, CancellationToken ct)
    {
        var userId = UserId;
        if (userId == Guid.Empty) return Unauthorized(new { error = "Authenticated faculty identity is missing." });

        var user = await db.Users.AsNoTracking().IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return Unauthorized();

        var course = await db.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == courseId, ct);
        if (course is null) return NotFound(new { error = "Course not found." });

        var elevated = User.IsInRole("HOD") || User.IsInRole("Dean") || User.IsInRole("OBECoordinator") || User.IsInRole("Registrar") || User.IsInRole("Admin") || User.IsInRole("SuperAdmin");
        if (!elevated && !await db.Timetables.AsNoTracking().AnyAsync(t => t.FacultyId == userId && t.CourseId == courseId, ct))
            return Forbid();

        var mappings = await db.CloPlomappings.AsNoTracking()
            .Where(m => m.CourseId == courseId)
            .OrderBy(m => m.CloCode).ThenBy(m => m.PloCode)
            .Select(m => new { clo = m.CloCode, plo = m.PloCode, alignment = m.AlignmentLevel.ToString() })
            .ToListAsync(ct);
        var enrollments = await db.Enrollments.AsNoTracking().CountAsync(e => e.CourseId == courseId, ct);
        var results = await db.Results.AsNoTracking().Where(r => r.CourseId == courseId).ToListAsync(ct);
        var attendance = await db.Attendances.AsNoTracking().Where(a => a.CourseId == courseId).ToListAsync(ct);

        var averageAssessment = results.Count == 0
            ? (decimal?)null
            : Math.Round(results.Where(r => r.TotalMarks > 0).Select(r => r.TotalMarks > 0 ? r.ObtainedMarks / r.TotalMarks * 100m : 0m).DefaultIfEmpty(0m).Average(), 2);

        var documentId = Guid.NewGuid();
        var generatedAt = DateTime.UtcNow;
        var geoId = Request.Headers["X-Aegis-Geo-ID"].FirstOrDefault()
            ?? Request.Headers["CF-IPCountry"].FirstOrDefault()
            ?? "not-supplied";
        var edgeRay = Request.Headers["CF-Ray"].FirstOrDefault() ?? "local/no-edge";
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var issuerName = $"{user.FirstName} {user.LastName}".Trim();

        var reportBody = JsonSerializer.Serialize(new
        {
            course = new { course.Id, code = course.Code, course.Title, course.CreditHours },
            issuer = new { user.Id, name = issuerName, user.Email, user.TenantId },
            metrics = new { enrollments, assessmentRecords = results.Count, averageAssessmentPercentage = averageAssessment, attendanceRecords = attendance.Count },
            mappings
        });
        var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(reportBody))).ToLowerInvariant();

        var manifestObject = new
        {
            schema = "aegiscore.signed-academic-report.v1",
            documentId,
            documentType = "Faculty OBE Course Report",
            reportDigestSha256 = digest,
            tenantId = user.TenantId,
            issuerUserId = user.Id,
            issuerName,
            courseId = course.Id,
            courseCode = course.Code,
            generatedAtUtc = generatedAt,
            geoId,
            edgeRay,
            clientIp
        };
        var manifestBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(manifestObject));
        var signature = await pqc.SignAsync(user.TenantId, "obe-report", manifestBytes, archival: true, ct);

        var verifyUrl = $"{Request.Scheme}://{Request.Host}/api/obe/faculty/documents/{documentId:D}/verify";
        var pdf = GeneratePdf(
            course.Code ?? "COURSE",
            course.Title,
            course.CreditHours,
            issuerName,
            user.Email ?? "N/A",
            user.Id,
            user.TenantId,
            documentId,
            generatedAt,
            geoId,
            edgeRay,
            clientIp,
            enrollments,
            results.Count,
            averageAssessment,
            attendance.Count,
            mappings.Select(m => (m.clo, m.plo, m.alignment)).ToList(),
            digest,
            verifyUrl,
            signature.Algorithm,
            signature.KeyId,
            Convert.ToBase64String(signature.Signature),
            Convert.ToBase64String(signature.PublicKey),
            Encoding.UTF8.GetString(manifestBytes));

        Documents[documentId] = new SignedObeDocument(
            pdf, DateTime.UtcNow.AddHours(12), user.Id, user.TenantId, manifestBytes,
            signature.Algorithm, signature.KeyId, signature.Signature, signature.PublicKey,
            digest, course.Code ?? "COURSE", course.Title, issuerName, generatedAt, geoId, edgeRay, clientIp);

        return File(pdf, "application/pdf", $"AegisCore-OBE-{SanitizeFileName(course.Code ?? "Course")}-{documentId.ToString("N")[..8]}.pdf");
    }

    [HttpGet("documents/{documentId:guid}/verify")]
    [AllowAnonymous]
    public IActionResult Verify(Guid documentId)
    {
        if (!Documents.TryGetValue(documentId, out var document))
            return NotFound(new { verified = false, error = "Verification record is unavailable or expired." });

        if (document.ExpiresAt <= DateTime.UtcNow)
        {
            Documents.TryRemove(documentId, out _);
            return NotFound(new { verified = false, error = "Verification record expired." });
        }

        var verified = pqc.Verify(document.Algorithm, document.ManifestBytes, document.Signature, document.PublicKey);
        return Ok(new
        {
            verified,
            documentId,
            documentType = "Faculty OBE Course Report",
            algorithm = document.Algorithm,
            keyId = document.KeyId,
            reportDigestSha256 = document.Digest,
            course = new { code = document.CourseCode, title = document.CourseTitle },
            issuer = document.IssuerName,
            generatedAtUtc = document.GeneratedAtUtc,
            tenantId = document.TenantId,
            geoId = document.GeoId,
            edgeRay = document.EdgeRay,
            clientIp = document.ClientIp
        });
    }

    private static byte[] GeneratePdf(
        string courseCode,
        string courseTitle,
        int creditHours,
        string issuerName,
        string issuerEmail,
        Guid issuerId,
        Guid tenantId,
        Guid documentId,
        DateTime generatedAt,
        string geoId,
        string edgeRay,
        string clientIp,
        int enrollmentCount,
        int assessmentRecords,
        decimal? averageAssessment,
        int attendanceRecords,
        List<(string Clo, string Plo, string Alignment)> mappings,
        string digest,
        string verifyUrl,
        string algorithm,
        string keyId,
        string signatureBase64,
        string publicKeyBase64,
        string canonicalManifest)
    {
        using var doc = new PdfDocument();
        doc.Info.Title = $"OBE Report — {courseCode}";
        doc.Info.Author = issuerName;
        doc.Info.Subject = $"PQC-verified OBE course report {documentId:D}";

        var page = doc.AddPage();
        using (var gfx = XGraphics.FromPdfPage(page))
        {
            var title = new XFont("Helvetica-Bold", 18, XFontStyle.Bold);
            var h = new XFont("Helvetica-Bold", 11, XFontStyle.Bold);
            var body = new XFont("Helvetica", 9, XFontStyle.Regular);
            var small = new XFont("Helvetica", 7.5, XFontStyle.Regular);
            var stamp = new XFont("Helvetica-Bold", 10, XFontStyle.Bold);
            double y = 36;

            gfx.DrawString("AegisCore — Faculty OBE Course Report", title, XBrushes.Black, new XRect(35, y, page.Width - 70, 28), XStringFormats.TopCenter);
            y += 32;
            gfx.DrawString("PQC VERIFIED", stamp, XBrushes.DarkGreen, new XRect(35, y, page.Width - 70, 18), XStringFormats.TopCenter);
            y += 24;
            gfx.DrawLine(new XPen(XColors.Black, 1), 35, y, page.Width - 35, y);
            y += 16;

            gfx.DrawString($"Course: {courseCode} — {courseTitle}", h, XBrushes.Black, 35, y); y += 16;
            gfx.DrawString($"Credit Hours: {creditHours}", body, XBrushes.Black, 35, y);
            gfx.DrawString($"Enrolled Students: {enrollmentCount}", body, XBrushes.Black, 220, y); y += 14;
            gfx.DrawString($"Assessment Records: {assessmentRecords}", body, XBrushes.Black, 35, y);
            gfx.DrawString($"Average Assessment: {(averageAssessment.HasValue ? averageAssessment.Value.ToString("F2") + "%" : "N/A")}", body, XBrushes.Black, 220, y); y += 14;
            gfx.DrawString($"Attendance Records: {attendanceRecords}", body, XBrushes.Black, 35, y); y += 20;

            gfx.DrawString("CLO → PLO Mapping", h, XBrushes.Black, 35, y); y += 14;
            if (mappings.Count == 0)
            {
                gfx.DrawString("No CLO/PLO mappings configured for this course yet.", body, XBrushes.Gray, 35, y); y += 14;
            }
            else
            {
                foreach (var map in mappings.Take(22))
                {
                    gfx.DrawString($"{map.Clo}  →  {map.Plo}   [{map.Alignment}]", body, XBrushes.Black, 45, y);
                    y += 12;
                }
            }

            y += 8;
            gfx.DrawLine(new XPen(XColors.LightGray, 0.8), 35, y, page.Width - 35, y); y += 14;
            gfx.DrawString("Audit / Issuance Metadata", h, XBrushes.Black, 35, y); y += 14;
            gfx.DrawString($"Document UUID: {documentId:D}", small, XBrushes.Black, 35, y); y += 11;
            gfx.DrawString($"Printed/Generated By: {issuerName} ({issuerEmail})", small, XBrushes.Black, 35, y); y += 11;
            gfx.DrawString($"Issuer User ID: {issuerId:D}", small, XBrushes.Black, 35, y); y += 11;
            gfx.DrawString($"Tenant ID: {tenantId:D}", small, XBrushes.Black, 35, y); y += 11;
            gfx.DrawString($"UTC Timestamp: {generatedAt:O}", small, XBrushes.Black, 35, y); y += 11;
            gfx.DrawString($"Geo-ID: {geoId} | Client IP: {clientIp} | Edge Ray: {edgeRay}", small, XBrushes.Black, 35, y); y += 11;
            gfx.DrawString($"SHA-256 Report Digest: {digest}", small, XBrushes.Black, 35, y); y += 18;

            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(verifyUrl, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrData);
            var qrBytes = qrCode.GetGraphic(5);
            using var qrImage = XImage.FromStream(() => new MemoryStream(qrBytes));
            gfx.DrawImage(qrImage, 35, y, 82, 82);
            gfx.DrawString("Scan to verify PQC signature", body, XBrushes.Black, 128, y + 22);
            gfx.DrawString($"Algorithm: {algorithm}", small, XBrushes.Black, 128, y + 38);
            gfx.DrawString($"Key ID: {keyId}", small, XBrushes.Black, 128, y + 50);
            gfx.DrawString(verifyUrl, small, XBrushes.Black, new XRect(128, y + 60, page.Width - 170, 24), XStringFormats.TopLeft);
        }

        var cryptoPage = doc.AddPage();
        using (var gfx = XGraphics.FromPdfPage(cryptoPage))
        {
            var title = new XFont("Helvetica-Bold", 14, XFontStyle.Bold);
            var body = new XFont("Courier", 6.2, XFontStyle.Regular);
            var h = new XFont("Helvetica-Bold", 9, XFontStyle.Bold);
            double y = 35;
            gfx.DrawString("Cryptographic Verification Manifest", title, XBrushes.Black, 35, y); y += 26;
            gfx.DrawString("Canonical manifest signed with post-quantum cryptography:", h, XBrushes.Black, 35, y); y += 14;
            y = DrawWrapped(gfx, canonicalManifest, body, 35, y, cryptoPage.Width - 70, 8.2);
            y += 12;
            gfx.DrawString("Detached PQC Signature (Base64)", h, XBrushes.Black, 35, y); y += 13;
            y = DrawWrapped(gfx, signatureBase64, body, 35, y, cryptoPage.Width - 70, 8.2);
            if (y > cryptoPage.Height - 120)
            {
                y = 35;
            }
            y += 12;
            gfx.DrawString("Public Key (Base64)", h, XBrushes.Black, 35, y); y += 13;
            DrawWrapped(gfx, publicKeyBase64, body, 35, y, cryptoPage.Width - 70, 8.2);
        }

        using var ms = new MemoryStream();
        doc.Save(ms, false);
        return ms.ToArray();
    }

    private static double DrawWrapped(XGraphics gfx, string value, XFont font, double x, double y, double width, double lineHeight)
    {
        var charsPerLine = Math.Max(24, (int)(width / Math.Max(3.4, font.Size * 0.57)));
        for (var i = 0; i < value.Length; i += charsPerLine)
        {
            var line = value.Substring(i, Math.Min(charsPerLine, value.Length - i));
            gfx.DrawString(line, font, XBrushes.Black, x, y);
            y += lineHeight;
            if (y > 790) break;
        }
        return y;
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(value.Select(c => invalid.Contains(c) ? '-' : c));
    }
}
