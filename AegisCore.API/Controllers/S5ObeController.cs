using AegisCore.Application.Integrations;
using AegisCore.Application.Obe;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.API.Controllers;

[ApiController]
[Route("api/obe/v2")]
[AegisCore.API.Authorization.RequirePermission("Grades", "Read")]
public sealed class S5ObeController(
    AegisDbContext db,
    ICisExtractionService cis,
    IOBEAttainmentService attainment,
    IHemisExportService hemis,
    IIndirectAssessmentReportService indirect) : ControllerBase
{
    [HttpPost("cis/{courseId:guid}/upload")]
    public async Task<ActionResult<Guid>> UploadCis(Guid courseId, IFormFile file, [FromQuery] Guid facultyId, CancellationToken ct)
    {
        if (file.Length is <= 0 or > 10 * 1024 * 1024) return BadRequest("CIS file must be between 1 byte and 10MB.");
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not ".pdf" and not ".png" and not ".jpg" and not ".jpeg") return BadRequest("Only PDF and image CIS files are allowed.");
        if (!await db.Courses.AnyAsync(c => c.Id == courseId, ct)) return NotFound("Course was not found.");
        await using var stream = file.OpenReadStream();
        return Ok(await cis.UploadAsync(courseId, facultyId, stream, file.FileName, file.ContentType, ct));
    }

    [HttpPost("cis/{id:guid}/process")]
    public async Task<IActionResult> ProcessCis(Guid id, CancellationToken ct)
    {
        await cis.ProcessAsync(id, ct);
        return NoContent();
    }

    [HttpPost("cis/{id:guid}/approve")]
    public async Task<IActionResult> ApproveCis(Guid id, [FromQuery] Guid reviewerId, CancellationToken ct)
    {
        await cis.ApproveAsync(id, reviewerId, ct);
        return NoContent();
    }

    [HttpPost("mappings")]
    public async Task<ActionResult<CLOPOMapping>> MapCloToPo([FromBody] CloPoMappingRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<MappingStrength>(request.Strength, true, out var strength)) return BadRequest("Mapping strength is invalid.");
        var clo = await db.CourseLearningOutcomes.FindAsync([request.CloId], ct);
        var po = await db.ProgramOutcomes.FindAsync([request.PoId], ct);
        if (clo is null || po is null) return NotFound();
        if (await db.CLOPOMappings.AnyAsync(m => m.CourseLearningOutcomeId == request.CloId && m.ProgramOutcomeId == request.PoId, ct)) return Conflict("Mapping already exists.");
        var mapping = new CLOPOMapping { Id = Guid.NewGuid(), CourseLearningOutcomeId = clo.Id, ProgramOutcomeId = po.Id, Strength = strength };
        db.CLOPOMappings.Add(mapping);
        await db.SaveChangesAsync(ct);
        return Ok(mapping);
    }

    [HttpGet("attainment/clo/{courseId:guid}/{cloId:guid}/{semesterId:guid}")]
    public Task<CLOAttainmentResult> CloAttainment(Guid courseId, Guid cloId, Guid semesterId, CancellationToken ct)
        => attainment.CalculateCLOAttainmentAsync(courseId, cloId, semesterId, ct);

    [HttpGet("attainment/po/{programId:guid}/{poId:guid}/{semesterId:guid}")]
    public Task<POAttainmentResult> PoAttainment(Guid programId, Guid poId, Guid semesterId, CancellationToken ct)
        => attainment.CalculatePOAttainmentAsync(programId, poId, semesterId, ct);

    [HttpGet("attainment/program/{programId:guid}/{semesterId:guid}")]
    public Task<ProgramOBEReport> ProgramAttainment(Guid programId, Guid semesterId, CancellationToken ct)
        => attainment.GenerateProgramReportAsync(programId, semesterId, ct);

    /// <summary>
    /// Indirect assessment: survey answers about courses, CLOs, teaching and (through the CLO→PLO
    /// mapping) programme outcomes, whether they came from a native form or an external one.
    /// </summary>
    [HttpGet("indirect")]
    public Task<IndirectAssessmentReport> Indirect([FromQuery] Guid? courseId, [FromQuery] string? semester, [FromQuery] Guid? surveyId, CancellationToken ct)
        => indirect.GetReportAsync(courseId, semester, ct, surveyId);

    /// <summary>The outcome structure: each course's CLOs, each programme's outcomes, and the CLO→PLO links.</summary>
    [HttpGet("outcomes")]
    public async Task<IActionResult> Outcomes(CancellationToken ct)
    {
        var courses = await db.Courses.AsNoTracking().OrderBy(c => c.Code).Select(c => new { c.Id, c.Code, c.Title }).ToListAsync(ct);
        var clos = await db.CourseLearningOutcomes.AsNoTracking().OrderBy(c => c.SortOrder).ThenBy(c => c.Code)
            .Select(c => new { c.Id, c.CourseId, c.Code, c.Description }).ToListAsync(ct);
        var programs = await db.DegreePrograms.AsNoTracking().OrderBy(p => p.Name).Select(p => new { p.Id, p.Name }).ToListAsync(ct);
        var pos = await db.ProgramOutcomes.AsNoTracking().OrderBy(p => p.SortOrder).ThenBy(p => p.Code)
            .Select(p => new { p.Id, p.ProgramId, p.Code, p.Description }).ToListAsync(ct);
        var links = await db.CLOPOMappings.AsNoTracking()
            .Select(m => new { m.Id, m.CourseLearningOutcomeId, m.ProgramOutcomeId, Strength = m.Strength.ToString() }).ToListAsync(ct);
        return Ok(new
        {
            courses = courses.Select(c => new { c.Id, c.Code, c.Title, clos = clos.Where(o => o.CourseId == c.Id) }),
            programs = programs.Select(p => new { p.Id, p.Name, outcomes = pos.Where(o => o.ProgramId == p.Id) }),
            mappings = links,
        });
    }

    [HttpPost("courses/{courseId:guid}/clos")]
    [AegisCore.API.Authorization.RequirePermission("Grades", "Write")]
    public async Task<IActionResult> AddClo(Guid courseId, [FromBody] OutcomeDefinitionRequest request, CancellationToken ct)
    {
        var code = request.Code?.Trim().ToUpperInvariant() ?? "";
        if (code.Length is 0 or > 50 || string.IsNullOrWhiteSpace(request.Description) || request.Description.Length > 1000)
            return BadRequest("A CLO needs a code of at most 50 characters and a description of at most 1000.");
        if (!await db.Courses.AnyAsync(c => c.Id == courseId, ct)) return NotFound("Course was not found.");
        if (await db.CourseLearningOutcomes.AnyAsync(c => c.CourseId == courseId && c.Code == code, ct)) return Conflict($"{code} already exists for this course.");
        var order = await db.CourseLearningOutcomes.CountAsync(c => c.CourseId == courseId, ct) + 1;
        var clo = new CourseLearningOutcome
        {
            Id = Guid.NewGuid(), CourseId = courseId, Code = code, Description = request.Description.Trim(),
            BloomsLevel = Enum.TryParse<BloomsLevel>(request.BloomsLevel, true, out var level) ? level : BloomsLevel.Understand, SortOrder = order,
        };
        db.CourseLearningOutcomes.Add(clo);
        await db.SaveChangesAsync(ct);
        return Ok(new { clo.Id, clo.CourseId, clo.Code, clo.Description });
    }

    [HttpPost("programs/{programId:guid}/outcomes")]
    [AegisCore.API.Authorization.RequirePermission("Grades", "Write")]
    public async Task<IActionResult> AddProgramOutcome(Guid programId, [FromBody] OutcomeDefinitionRequest request, CancellationToken ct)
    {
        var code = request.Code?.Trim().ToUpperInvariant() ?? "";
        if (code.Length is 0 or > 50 || string.IsNullOrWhiteSpace(request.Description) || request.Description.Length > 1000)
            return BadRequest("A programme outcome needs a code of at most 50 characters and a description of at most 1000.");
        if (!await db.DegreePrograms.AnyAsync(p => p.Id == programId, ct)) return NotFound("Programme was not found.");
        if (await db.ProgramOutcomes.AnyAsync(p => p.ProgramId == programId && p.Code == code, ct)) return Conflict($"{code} already exists for this programme.");
        var order = await db.ProgramOutcomes.CountAsync(p => p.ProgramId == programId, ct) + 1;
        var po = new ProgramOutcome { Id = Guid.NewGuid(), ProgramId = programId, Code = code, Description = request.Description.Trim(), SortOrder = order };
        db.ProgramOutcomes.Add(po);
        await db.SaveChangesAsync(ct);
        return Ok(new { po.Id, po.ProgramId, po.Code, po.Description });
    }

    [HttpGet("hemis/{programId:guid}/{semesterId:guid}")]
    public async Task<IActionResult> Hemis(Guid programId, Guid semesterId, CancellationToken ct)
        => Content(await hemis.ExportProgramAsync(programId, semesterId, ct), "application/xml");
}

public sealed record CloPoMappingRequest(Guid CloId, Guid PoId, string Strength);
public sealed record OutcomeDefinitionRequest(string? Code, string Description, string? BloomsLevel);
