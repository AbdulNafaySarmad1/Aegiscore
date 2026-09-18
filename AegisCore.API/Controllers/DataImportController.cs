using System.Text.Json;
using AegisCore.Application.DataImport;
using AegisCore.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.API.Controllers;

[ApiController][Route("api/data-import")] [Authorize] public class DataImportController : ControllerBase
{
    private readonly IDataImportService _s;
    private readonly string _formats;

    public DataImportController(IDataImportService s, Microsoft.AspNetCore.Hosting.IWebHostEnvironment environment)
    {
        _s = s;
        _formats = Path.Combine(environment.ContentRootPath, "formats");
    }


[HttpPost("connections")] public async Task<IActionResult> AddConnection([FromBody] ExternalDbConnection conn, CancellationToken ct) => Ok(await _s.AddConnectionAsync(conn, ct));

[HttpPost("connections/{id:guid}/test")] public async Task<IActionResult> TestConnection(Guid id, CancellationToken ct) { var ok = await _s.TestConnectionAsync(id, ct); return Ok(new { connectionId = id, success = ok }); }

[HttpGet("connections/{id:guid}/tables")] public async Task<IActionResult> DiscoverTables(Guid id, [FromQuery] string? schema, CancellationToken ct) { var tables = await _s.DiscoverTablesAsync(id, schema, ct); return Ok(new { connectionId = id, schema = schema, total = tables.Count, tables }); }

[HttpGet("connections/{id:guid}/tables/{table}/columns")] public async Task<IActionResult> DiscoverColumns(Guid id, string table, [FromQuery] string? schema, CancellationToken ct) { var cols = await _s.DiscoverColumnsAsync(id, table, schema, ct); return Ok(new { connectionId = id, table, schema, total = cols.Count, columns = cols }); }

[HttpPost("jobs")] public async Task<IActionResult> StartImport([FromBody] StartImportRequest req, CancellationToken ct) { if (!IsReadOnlySelect(req.SourceQuery)) return BadRequest(new { error = "SourceQuery contains prohibited syntax. It must be one read-only SELECT or WITH query using approved SQL syntax." }); var mappings = JsonSerializer.Deserialize<Dictionary<string, string>>(req.FieldMappingsJson ?? "{}") ?? new(); // An import rewrites the register; it is recorded against whoever started it, not whoever the caller names.
    var startedBy = Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var actor) ? actor : req.TriggeredByUserId;
    var job = await _s.StartImportAsync(req.ConnectionId, req.EntityType, req.SourceQuery, mappings, startedBy, ct); return Ok(job); }

        public static bool IsReadOnlySelect(string query) => ReadOnlySqlQueryValidator.IsValid(query);

[HttpGet("jobs/{id:guid}")] public async Task<IActionResult> JobStatus(Guid id, CancellationToken ct) => Ok(await _s.GetJobStatusAsync(id, ct));

[HttpGet("jobs")] public async Task<IActionResult> JobHistory(CancellationToken ct) { var jobs = await _s.GetJobHistoryAsync(ct); return Ok(new { total = jobs.Count, jobs }); }

[HttpGet("templates")] public async Task<IActionResult> Templates([FromQuery] string? entityType, CancellationToken ct) { var tmpl = await _s.GetMappingTemplatesAsync(entityType, ct); return Ok(new { total = tmpl.Count, templates = tmpl }); }

[HttpPost("templates")] public async Task<IActionResult> SaveTemplate([FromBody] FieldMappingTemplate tmpl, CancellationToken ct) => Ok(await _s.SaveMappingTemplateAsync(tmpl, ct));

    /// <summary>
    /// One of the CSV templates shipped with the application. The name is matched against the
    /// templates on disk rather than joined onto a path: joined, a name of "../appsettings.json"
    /// read a file that is none of this endpoint's business.
    /// </summary>
    [HttpGet("formats/{fileName}")]
    public IActionResult DownloadFormat(string fileName)
    {
        var available = Available();
        var match = available.FirstOrDefault(name => string.Equals(name, fileName, StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            return NotFound(new { error = available.Count == 0
                ? "No import templates are installed with this build."
                : $"There is no template called '{fileName}'. The templates are: {string.Join(", ", available)}." });
        }

        return File(System.IO.File.OpenRead(Path.Combine(_formats, match)), "text/csv", match);
    }

    [HttpGet("formats")]
    public IActionResult ListFormats()
    {
        var available = Available();
        return Ok(new { total = available.Count, formats = available });
    }

    private List<string> Available()
        => Directory.Exists(_formats)
            ? Directory.GetFiles(_formats, "*.csv").Select(Path.GetFileName).OfType<string>().OrderBy(name => name, StringComparer.Ordinal).ToList()
            : [];

    public class StartImportRequest { public Guid ConnectionId { get; set; } public string EntityType { get; set; } = ""; public string SourceQuery { get; set; } = ""; public string? FieldMappingsJson { get; set; } public Guid? TriggeredByUserId { get; set; } }
}
