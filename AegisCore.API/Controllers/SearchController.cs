using AegisCore.Application.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.API.Controllers;

[ApiController]
[Route("api/search")]
[Authorize]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(ISearchService searchService, ILogger<SearchController> logger)
    {
        _searchService = searchService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string q,
        [FromQuery] string? domains,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { error = "missing_query", message = "Query parameter 'q' is required." });

        var request = new UnifiedSearchRequest
        {
            Query = q,
            Domains = domains?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(d => d.Trim()).ToList(),
            Page = page,
            PageSize = pageSize,
            UserId = Guid.Empty
        };

        var response = await _searchService.SearchAsync(q, request, cancellationToken);
        return Ok(response);
    }

    [AegisCore.API.Authorization.RequirePermission("Settings", "Write")]
    [HttpPost("index/rebuild")]
    public async Task<IActionResult> RebuildIndices(CancellationToken cancellationToken)
    {
        await _searchService.RebuildAllIndicesAsync(cancellationToken);
        return Ok(new { message = "All search indices rebuilt successfully." });
    }

    [AegisCore.API.Authorization.RequirePermission("Settings", "Write")]
    [HttpPost("index/{entityType}/{entityId}")]
    public async Task<IActionResult> IndexEntity(string entityType, string entityId, CancellationToken cancellationToken)
    {
        await _searchService.IndexEntityAsync(entityType, entityId, cancellationToken);
        return Ok(new { message = $"Entity {entityType}/{entityId} indexed." });
    }

    [AegisCore.API.Authorization.RequirePermission("Settings", "Delete")]
    [HttpDelete("index/{entityType}/{entityId}")]
    public async Task<IActionResult> DeleteFromIndex(string entityType, string entityId, CancellationToken cancellationToken)
    {
        await _searchService.DeleteEntityAsync(entityType, entityId, cancellationToken);
        return Ok(new { message = $"Entity {entityType}/{entityId} removed from index." });
    }
}
