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
[Authorize(Roles = "Faculty,OBECoordinator,Dean,Registrar,Admin,SuperAdmin")]
public sealed class S5ObeController(
    AegisDbContext db,
    ICisExtractionService cis,
    IOBEAttainmentService attainment,
    IHemisExportService hemis) : ControllerBase
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

    [HttpGet("hemis/{programId:guid}/{semesterId:guid}")]
    public async Task<IActionResult> Hemis(Guid programId, Guid semesterId, CancellationToken ct)
        => Content(await hemis.ExportProgramAsync(programId, semesterId, ct), "application/xml");
}

public sealed record CloPoMappingRequest(Guid CloId, Guid PoId, string Strength);
