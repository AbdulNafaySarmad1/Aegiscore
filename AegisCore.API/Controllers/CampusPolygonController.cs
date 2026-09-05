using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.API.Controllers;

[ApiController]
[Route("api/campuses/polygon")]
public sealed class CampusPolygonController(AegisDbContext db) : ControllerBase
{
    [HttpGet]
    [AegisCore.API.Authorization.RequirePermission("CampusPolygon", "Read")]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var polygons = await db.CampusPolygons.AsNoTracking().OrderBy(item => item.Name).ToListAsync(ct);
        return Ok(polygons.Select(ToResponse));
    }

    [HttpPost]
    [AegisCore.API.Authorization.RequirePermission("CampusPolygon", "Write")]
    public async Task<IActionResult> Create([FromBody] CampusPolygonRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 160)
            return BadRequest(new { error = "A campus name between 1 and 160 characters is required." });

        var polygon = new CampusPolygon
        {
            Id = Guid.NewGuid(),
            TenantId = db.CurrentTenantId,
            Name = request.Name.Trim(),
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };
        try { polygon.SetVertices(request.Vertices); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }

        db.CampusPolygons.Add(polygon);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), new { id = polygon.Id }, ToResponse(polygon));
    }

    [HttpPut("{id:guid}")]
    [AegisCore.API.Authorization.RequirePermission("CampusPolygon", "Write")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CampusPolygonRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 160)
            return BadRequest(new { error = "A campus name between 1 and 160 characters is required." });

        var polygon = await db.CampusPolygons.SingleOrDefaultAsync(item => item.Id == id, ct);
        if (polygon is null) return NotFound();

        try { polygon.SetVertices(request.Vertices); }
        catch (ArgumentException exception) { return BadRequest(new { error = exception.Message }); }
        polygon.Name = request.Name.Trim();
        polygon.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
        return Ok(ToResponse(polygon));
    }

    private static object ToResponse(CampusPolygon polygon) => new
    {
        polygon.Id,
        polygon.Name,
        vertices = polygon.GetVertices(),
        polygon.CreatedAt,
        polygon.IsActive
    };
}

public sealed record CampusPolygonRequest(string Name, IReadOnlyList<CampusVertex> Vertices, bool IsActive = true);
