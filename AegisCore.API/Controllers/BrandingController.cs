using AegisCore.Contracts.Abstractions.Cloud;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.API.Controllers;

[ApiController][Authorize][Route("api/branding")]
public class BrandingController : ControllerBase
{
    private readonly IStorageService _storage;
    private readonly IConfiguration _config;

    public BrandingController(IStorageService storage, IConfiguration config)
    { _storage = storage; _config = config; }

    [AllowAnonymous]
    [HttpGet("client-logo")]
    public async Task<IActionResult> GetClientLogo(CancellationToken ct)
    {
        var logoUrl = _config["Branding:ClientLogoUrl"] ?? _config["AEGISCORE_CLIENT_LOGO_URL"];
        if (string.IsNullOrWhiteSpace(logoUrl))
        {
            var bucket = _config["STORAGE_BUCKET"] ?? "aegiscore";
            var key = "branding/client-logo.png";
            try
            {
                if (await _storage.ExistsAsync(bucket, key, ct))
                    logoUrl = await _storage.GetPresignedUrlAsync(bucket, key, TimeSpan.FromHours(1), ct);
            }
            catch { }
        }

        return Ok(new { logoUrl = logoUrl ?? "" });
    }

    [HttpPost("upload-client-logo"), RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadClientLogo(CancellationToken ct)
    {
        using var ms = new MemoryStream();
        await Request.Body.CopyToAsync(ms, ct);
        ms.Position = 0;

        if (ms.Length < 1024 || ms.Length > 5 * 1024 * 1024)
            return BadRequest(new { error = "Logo must be between 1KB and 5MB." });

        var bucket = _config["STORAGE_BUCKET"] ?? "aegiscore";
        await _storage.CreateBucketIfNotExistsAsync(bucket, ct);
        var url = await _storage.UploadAsync(bucket, "branding/client-logo.png", ms, "image/png", StorageVisibility.Public, ct);

        return Ok(new { logoUrl = url, message = "Client logo uploaded successfully." });
    }

    [HttpGet("aegiscore-logo")]
    public IActionResult GetAegisCoreLogo()
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "formats", "..", "aegiscore-logo.png");
        if (!System.IO.File.Exists(path))
            path = Path.Combine(Directory.GetCurrentDirectory(), "aegiscore-logo.png");
        if (!System.IO.File.Exists(path))
            return NotFound();

        return PhysicalFile(path, "image/png");
    }
}
