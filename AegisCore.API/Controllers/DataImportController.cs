using System.Text.Json;
using AegisCore.Application.DataImport;
using AegisCore.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.API.Controllers;

[ApiController][Route("api/data-import")] public class DataImportController : ControllerBase { private IDataImportService _s; public DataImportController(IDataImportService s) => _s = s;

[HttpPost("connections")] public async Task<IActionResult> AddConnection([FromBody] ExternalDbConnection conn, CancellationToken ct) => Ok(await _s.AddConnectionAsync(conn, ct));

[HttpPost("connections/{id:guid}/test")] public async Task<IActionResult> TestConnection(Guid id, CancellationToken ct) { var ok = await _s.TestConnectionAsync(id, ct); return Ok(new { connectionId = id, success = ok }); }

[HttpGet("connections/{id:guid}/tables")] public async Task<IActionResult> DiscoverTables(Guid id, [FromQuery] string? schema, CancellationToken ct) { var tables = await _s.DiscoverTablesAsync(id, schema, ct); return Ok(new { connectionId = id, schema = schema, total = tables.Count, tables }); }

[HttpGet("connections/{id:guid}/tables/{table}/columns")] public async Task<IActionResult> DiscoverColumns(Guid id, string table, [FromQuery] string? schema, CancellationToken ct) { var cols = await _s.DiscoverColumnsAsync(id, table, schema, ct); return Ok(new { connectionId = id, table, schema, total = cols.Count, columns = cols }); }

[HttpPost("jobs")] public async Task<IActionResult> StartImport([FromBody] StartImportRequest req, CancellationToken ct) { var mappings = JsonSerializer.Deserialize<Dictionary<string, string>>(req.FieldMappingsJson ?? "{}") ?? new(); var job = await _s.StartImportAsync(req.ConnectionId, req.EntityType, req.SourceQuery, mappings, req.TriggeredByUserId, ct); return Ok(job); }

[HttpGet("jobs/{id:guid}")] public async Task<IActionResult> JobStatus(Guid id, CancellationToken ct) => Ok(await _s.GetJobStatusAsync(id, ct));

[HttpGet("jobs")] public async Task<IActionResult> JobHistory(CancellationToken ct) { var jobs = await _s.GetJobHistoryAsync(ct); return Ok(new { total = jobs.Count, jobs }); }

[HttpGet("templates")] public async Task<IActionResult> Templates([FromQuery] string? entityType, CancellationToken ct) { var tmpl = await _s.GetMappingTemplatesAsync(entityType, ct); return Ok(new { total = tmpl.Count, templates = tmpl }); }

[HttpPost("templates")] public async Task<IActionResult> SaveTemplate([FromBody] FieldMappingTemplate tmpl, CancellationToken ct) => Ok(await _s.SaveMappingTemplateAsync(tmpl, ct));

    [HttpGet("formats/{fileName}")] public IActionResult DownloadFormat(string fileName) { var path = Path.Combine(Directory.GetCurrentDirectory(), "formats", fileName); if (!System.IO.File.Exists(path)) return NotFound(new { error = $"Format file '{fileName}' not found. Available: students, faculty, courses, enrollments, grades, fees, timetable, attendance, exams, placements, alumni, transcripts, library, visitors" }); var stream = System.IO.File.OpenRead(path); return File(stream, "text/csv", fileName); }

    [HttpGet("formats")] public IActionResult ListFormats() { var dir = Path.Combine(Directory.GetCurrentDirectory(), "formats"); if (!Directory.Exists(dir)) return Ok(new { formats = Array.Empty<string>() }); var files = Directory.GetFiles(dir, "*.csv").Select(Path.GetFileName).OrderBy(f => f).ToList(); return Ok(new { total = files.Count, formats = files }); }

    public class StartImportRequest { public Guid ConnectionId { get; set; } public string EntityType { get; set; } = ""; public string SourceQuery { get; set; } = ""; public string? FieldMappingsJson { get; set; } public Guid? TriggeredByUserId { get; set; } }
}
