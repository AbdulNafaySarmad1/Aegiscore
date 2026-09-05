using AegisCore.Application.CampusOperations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.API.Controllers;

[ApiController]
[Route("api/campus-operations")]
[Authorize]
public class CampusOperationsController : ControllerBase
{
    private readonly ICampusOperationsService _service;
    public CampusOperationsController(ICampusOperationsService service) => _service = service;

    [HttpGet("library/books")]
    public async Task<IActionResult> SearchBooks([FromQuery] string q = "", CancellationToken ct = default)
        => Ok(new { books = await _service.SearchBooksAsync(q, ct) });

    [HttpGet("library/fine")]
    public IActionResult Fine([FromQuery] DateTime dueDate, [FromQuery] DateTime? returnedDate)
        => Ok(new { fine = _service.CalculateLibraryFine(dueDate, returnedDate) });

    [HttpGet("transport/routes/{routeId:guid}/capacity")]
    public async Task<IActionResult> Capacity(Guid routeId, CancellationToken ct)
        => Ok(await _service.GetTransportCapacityAsync(routeId, ct));

    [HttpPost("visitors/{visitorId:guid}/passes")]
    [AegisCore.API.Authorization.RequirePermission("Settings", "Write")]
    public async Task<IActionResult> VisitorPass(Guid visitorId, [FromQuery] DateTime validFrom, [FromQuery] DateTime validTo, CancellationToken ct)
        => Ok(await _service.GenerateVisitorPassAsync(visitorId, validFrom, validTo, ct));
}
