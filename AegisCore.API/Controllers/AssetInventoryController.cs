using AegisCore.Application.AssetInventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.API.Controllers;

[ApiController]
[Route("api/asset-inventory")]
[Authorize]
public class AssetInventoryController : ControllerBase
{
    private readonly IAssetInventoryService _service;
    public AssetInventoryController(IAssetInventoryService service) => _service = service;

    [HttpGet("assets/{assetId:guid}/current-value")]
    public async Task<IActionResult> CurrentValue(Guid assetId, [FromQuery] DateTime? asAt, CancellationToken ct)
        => Ok(new { currentValue = await _service.CalculateCurrentValueAsync(assetId, asAt ?? DateTime.UtcNow, ct) });

    [HttpGet("inventory/low-stock")]
    public async Task<IActionResult> LowStock(CancellationToken ct)
        => Ok(new { items = await _service.GetLowStockAsync(ct) });

    [HttpGet("hemis-export")]
    [AegisCore.API.Authorization.RequirePermission("Reports", "Export")]
    public async Task<IActionResult> Hemis([FromQuery] int academicYear, CancellationToken ct)
        => Content(await _service.BuildHemisXmlAsync(academicYear, ct), "application/xml");
}
