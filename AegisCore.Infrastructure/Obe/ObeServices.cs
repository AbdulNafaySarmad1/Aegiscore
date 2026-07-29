using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using AegisCore.Application.AI;
using AegisCore.Application.Foundation;
using AegisCore.Application.Obe;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Obe;

public sealed class CisExtractionService(AegisDbContext db, IStorageProvider storage, IDocumentOcrService ocr) : ICisExtractionService
{
    public async Task<Guid> UploadAsync(Guid courseId, Guid facultyId, Stream file, string fileName, string contentType, CancellationToken ct = default)
    {
        var id = Guid.NewGuid();
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var path = await storage.UploadAsync($"cis/{courseId}/{id}{extension}", file, contentType, ct);
        db.CisUploads.Add(new CisUpload { Id = id, CourseId = courseId, FacultyId = facultyId, CisFilePath = path });
        await db.SaveChangesAsync(ct);
        return id;
    }

    public async Task ProcessAsync(Guid cisUploadId, CancellationToken ct = default)
    {
        var upload = await db.CisUploads.FindAsync([cisUploadId], ct) ?? throw new InvalidOperationException("CIS upload was not found.");
        upload.ProcessingStatus = "processing";
        try
        {
            await using var stream = await storage.DownloadAsync(upload.CisFilePath, ct);
            var ocrResult = await ocr.ExtractTextAsync(stream, Path.GetFileName(upload.CisFilePath), ct);
            var clos = ocrResult.FullText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(line => line.StartsWith("CLO", StringComparison.OrdinalIgnoreCase))
                .Take(12)
                .Select((line, index) => new { code = $"CLO{index + 1}", description = line })
                .ToList();

            upload.ExtractedData = JsonSerializer.Serialize(new { clos, confidence = ocrResult.Pages.DefaultIfEmpty().Average(p => p?.Confidence ?? 0) });
            upload.ProcessingStatus = "completed";
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            upload.ProcessingStatus = "failed";
            await db.SaveChangesAsync(ct);
            throw;
        }
    }

    public async Task ApproveAsync(Guid cisUploadId, Guid reviewerId, CancellationToken ct = default)
    {
        var upload = await db.CisUploads.FindAsync([cisUploadId], ct) ?? throw new InvalidOperationException("CIS upload was not found.");
        upload.HumanReviewStatus = "approved";
        upload.ReviewedBy = reviewerId;
        upload.ReviewedAt = DateTimeOffset.UtcNow;

        var extracted = JsonDocument.Parse(upload.ExtractedData ?? "{}");
        if (extracted.RootElement.TryGetProperty("clos", out var clos))
        {
            var index = 1;
            foreach (var clo in clos.EnumerateArray())
            {
                var code = clo.GetProperty("code").GetString() ?? $"CLO{index}";
                if (!await db.CourseLearningOutcomes.AnyAsync(c => c.CourseId == upload.CourseId && c.Code == code, ct))
                {
                    db.CourseLearningOutcomes.Add(new CourseLearningOutcome
                    {
                        Id = Guid.NewGuid(),
                        CourseId = upload.CourseId,
                        Code = code,
                        Description = clo.GetProperty("description").GetString() ?? code,
                        SortOrder = index
                    });
                }
                index++;
            }
        }

        await db.SaveChangesAsync(ct);
    }
}

public sealed class OBEAttainmentService(AegisDbContext db) : IOBEAttainmentService
{
    public async Task<CLOAttainmentResult> CalculateCLOAttainmentAsync(Guid courseId, Guid cloId, Guid semesterId, CancellationToken ct = default)
    {
        var clo = await db.CourseLearningOutcomes.FirstOrDefaultAsync(c => c.Id == cloId && c.CourseId == courseId, ct)
            ?? throw new InvalidOperationException("CLO was not found for the course.");
        var percentages = await db.CourseGrades
            .Where(g => g.CourseId == courseId && g.SemesterId == semesterId)
            .Select(g => g.PercentageObtained)
            .ToListAsync(ct);
        var attainment = percentages.Count == 0 ? 0m : Math.Round(percentages.Average(), 2, MidpointRounding.AwayFromZero);
        return new CLOAttainmentResult(clo.Id, clo.Code, attainment, percentages.Count, percentages.Count(p => p >= 70m));
    }

    public async Task<POAttainmentResult> CalculatePOAttainmentAsync(Guid programId, Guid poId, Guid semesterId, CancellationToken ct = default)
    {
        var po = await db.ProgramOutcomes.FirstOrDefaultAsync(p => p.Id == poId && p.ProgramId == programId, ct)
            ?? throw new InvalidOperationException("PO was not found for the program.");
        var mappings = await db.CLOPOMappings.Where(m => m.ProgramOutcomeId == poId).ToListAsync(ct);
        var clos = new List<CLOAttainmentResult>();
        var weighted = 0m;
        var weights = 0m;
        foreach (var mapping in mappings)
        {
            var clo = await CalculateCLOAttainmentAsync(
                (await db.CourseLearningOutcomes.FindAsync([mapping.CourseLearningOutcomeId], ct))?.CourseId
                    ?? throw new InvalidOperationException("Mapped CLO was not found."),
                mapping.CourseLearningOutcomeId, semesterId, ct);
            var weight = mapping.Strength switch { MappingStrength.Low => 0.33m, MappingStrength.Medium => 0.66m, _ => 1m };
            clos.Add(clo);
            weighted += clo.AttainmentPercentage * weight;
            weights += weight;
        }

        return new POAttainmentResult(po.Id, po.Code, weights == 0 ? 0m : Math.Round(weighted / weights, 2, MidpointRounding.AwayFromZero), clos);
    }

    public async Task<ProgramOBEReport> GenerateProgramReportAsync(Guid programId, Guid semesterId, CancellationToken ct = default)
    {
        var outcomes = await db.ProgramOutcomes.Where(p => p.ProgramId == programId).OrderBy(p => p.Code).ToListAsync(ct);
        var results = new List<POAttainmentResult>();
        foreach (var outcome in outcomes) results.Add(await CalculatePOAttainmentAsync(programId, outcome.Id, semesterId, ct));
        return new ProgramOBEReport(programId, semesterId, results.Count == 0 ? 0m : Math.Round(results.Average(r => r.AttainmentPercentage), 2), results);
    }
}

public sealed class HemisExportService(AegisDbContext db) : IHemisExportService
{
    public async Task<string> ExportProgramAsync(Guid programId, Guid semesterId, CancellationToken ct = default)
    {
        var pos = await db.ProgramOutcomes.Where(p => p.ProgramId == programId).OrderBy(p => p.Code).ToListAsync(ct);
        var clos = await db.CourseLearningOutcomes.OrderBy(c => c.CourseId).ThenBy(c => c.Code).ToListAsync(ct);
        var xml = new XElement("hemis",
            new XAttribute("programId", programId),
            new XAttribute("semesterId", semesterId),
            new XElement("programOutcomes", pos.Select(po => new XElement("po",
                new XAttribute("code", po.Code),
                new XCData(po.Description)))),
            new XElement("courseLearningOutcomes", clos.Select(clo => new XElement("clo",
                new XAttribute("courseId", clo.CourseId),
                new XAttribute("code", clo.Code),
                new XCData(clo.Description)))));

        return new XDocument(xml).ToString(SaveOptions.DisableFormatting);
    }
}
