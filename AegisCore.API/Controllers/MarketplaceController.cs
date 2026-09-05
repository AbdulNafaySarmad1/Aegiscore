using System.Text.Json;
using AegisCore.Application.Common.Interfaces;
using AegisCore.Application.Marketplace;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace AegisCore.API.Controllers;

[ApiController]
[Route("api/marketplace")]
[Authorize]
public class MarketplaceController : ControllerBase
{
    private readonly IExtensionRepository _extensionRepo;
    private readonly IExtensionInstallationRepository _installationRepo;
    private readonly IExtensionReviewRepository _reviewRepo;
    private readonly IExtensionService _extensionService;
    private readonly AegisDbContext _db;
    private readonly ILogger<MarketplaceController> _logger;

    public MarketplaceController(
        IExtensionRepository extensionRepo,
        IExtensionInstallationRepository installationRepo,
        IExtensionReviewRepository reviewRepo,
        IExtensionService extensionService,
        AegisDbContext db,
        ILogger<MarketplaceController> logger)
    {
        _extensionRepo = extensionRepo;
        _installationRepo = installationRepo;
        _reviewRepo = reviewRepo;
        _extensionService = extensionService;
        _db = db;
        _logger = logger;
    }

    [HttpGet("catalog")]
    public async Task<IActionResult> GetCatalog(
        [FromQuery] string? category,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Extension> extensions;

        if (!string.IsNullOrWhiteSpace(search))
            extensions = await _extensionRepo.SearchAsync(search, cancellationToken);
        else if (!string.IsNullOrWhiteSpace(category) && Enum.TryParse<ExtensionCategory>(category, true, out var cat))
            extensions = await _extensionRepo.GetByCategoryAsync(cat, cancellationToken);
        else
            extensions = await _extensionRepo.GetPublishedAsync(cancellationToken);

        var items = extensions.Select(MapToListItem).ToList();
        return Ok(new { total = items.Count, extensions = items });
    }

    [HttpGet("catalog/{id:guid}")]
    public async Task<IActionResult> GetExtensionDetail(Guid id, CancellationToken cancellationToken)
    {
        var extension = await _extensionRepo.GetByIdAsync(id, cancellationToken);
        if (extension == null)
            return NotFound(new { error = "not_found", message = "Extension not found." });

        var screenshots = JsonSerializer.Deserialize<List<string>>(extension.ScreenshotsJson) ?? new();
        var dependencies = JsonSerializer.Deserialize<List<ExtensionDependency>>(extension.DependenciesJson) ?? new();
        var permissions = JsonSerializer.Deserialize<List<string>>(extension.RequiredPermissionsJson) ?? new();

        return Ok(new ExtensionDetail
        {
            Id = extension.Id,
            Name = extension.Name,
            Description = extension.Description,
            Version = extension.Version,
            Category = extension.Category.ToString(),
            Status = extension.Status.ToString(),
            PricingModel = extension.PricingModel.ToString(),
            Price = extension.Price,
            IconUrl = extension.IconUrl,
            Screenshots = screenshots,
            DeveloperName = extension.DeveloperName,
            DeveloperEmail = extension.DeveloperEmail,
            DeveloperWebsite = extension.DeveloperWebsite,
            DownloadCount = extension.DownloadCount,
            AverageRating = extension.AverageRating,
            ReviewCount = extension.ReviewCount,
            Dependencies = dependencies,
            RequiredPermissions = permissions,
            CreatedAt = extension.CreatedAt,
            UpdatedAt = extension.UpdatedAt,
            PublishedAt = extension.PublishedAt
        });
    }

    [HttpGet("catalog/{id:guid}/reviews")]
    public async Task<IActionResult> GetReviews(Guid id, CancellationToken cancellationToken)
    {
        var reviews = await _reviewRepo.GetByExtensionAsync(id, cancellationToken);
        var response = reviews.Select(r => new ExtensionReviewResponse
        {
            Id = r.Id,
            ReviewedByName = r.ReviewedByName,
            Rating = r.Rating,
            Comment = r.Comment,
            DeveloperResponse = r.DeveloperResponse,
            CreatedAt = r.CreatedAt
        });

        return Ok(new { average = reviews.Any() ? reviews.Average(r => r.Rating) : 0, count = reviews.Count, reviews = response });
    }

    [HttpPost("catalog/{id:guid}/reviews")]
    public async Task<IActionResult> SubmitReview(Guid id, [FromBody] ExtensionReviewRequest request, CancellationToken cancellationToken)
    {
        var review = await _extensionService.SubmitReviewAsync(
            id, Guid.Empty, Guid.Empty, "Anonymous", request.Rating, request.Comment, cancellationToken);

        return Ok(new ExtensionReviewResponse
        {
            Id = review.Id,
            ReviewedByName = review.ReviewedByName,
            Rating = review.Rating,
            Comment = review.Comment,
            CreatedAt = review.CreatedAt
        });
    }

    [AegisCore.API.Authorization.RequirePermission("System", "Write")]
    [HttpPost("publish")]
    public async Task<IActionResult> PublishExtension([FromBody] ExtensionManifest manifest, CancellationToken cancellationToken)
    {
        var extension = new Extension
        {
            Id = Guid.NewGuid(),
            Name = manifest.Name,
            Version = manifest.Version,
            Description = manifest.Description,
            Category = Enum.TryParse<ExtensionCategory>(manifest.Category, true, out var cat) ? cat : ExtensionCategory.Finance,
            PricingModel = Enum.TryParse<ExtensionPricingModel>(manifest.PricingModel, true, out var pm) ? pm : ExtensionPricingModel.Free,
            Price = manifest.Price,
            DeveloperName = manifest.DeveloperName,
            DeveloperEmail = manifest.DeveloperEmail,
            DeveloperWebsite = manifest.DeveloperWebsite,
            IconUrl = manifest.IconUrl,
            ManifestJson = JsonSerializer.Serialize(manifest),
            ScreenshotsJson = JsonSerializer.Serialize(manifest.Screenshots),
            DependenciesJson = JsonSerializer.Serialize(manifest.Dependencies),
            RequiredPermissionsJson = JsonSerializer.Serialize(manifest.RequiredPermissions),
            PackageChecksum = manifest.PackageChecksum,
            PackageUrl = manifest.PackageUrl
        };

        var published = await _extensionService.PublishExtensionAsync(extension, cancellationToken);

        return Ok(new { id = published.Id, name = published.Name, version = published.Version, status = published.Status.ToString() });
    }

    [AegisCore.API.Authorization.RequirePermission("System", "Write")]
    [HttpPost("install")]
    public async Task<IActionResult> InstallExtension([FromBody] InstallExtensionRequest request, CancellationToken cancellationToken)
    {
        var installation = await _extensionService.InstallExtensionAsync(
            request.ExtensionId, Guid.Empty, Guid.Empty, cancellationToken);

        return Ok(new InstallationDto
        {
            Id = installation.Id,
            ExtensionId = installation.ExtensionId,
            ExtensionName = installation.Extension?.Name ?? "Unknown",
            InstalledVersion = installation.InstalledVersion,
            Status = installation.Status.ToString(),
            InstalledAt = installation.InstalledAt
        });
    }

    [AegisCore.API.Authorization.RequirePermission("System", "Delete")]
    [HttpPost("install/{installationId:guid}/uninstall")]
    public async Task<IActionResult> UninstallExtension(Guid installationId, CancellationToken cancellationToken)
    {
        await _extensionService.UninstallExtensionAsync(installationId, cancellationToken);
        return Ok(new { message = "Extension uninstalled successfully." });
    }

    [AegisCore.API.Authorization.RequirePermission("System", "Write")]
    [HttpPost("install/{installationId:guid}/upgrade")]
    public async Task<IActionResult> UpgradeExtension(Guid installationId, CancellationToken cancellationToken)
    {
        var upgraded = await _extensionService.UpgradeExtensionAsync(installationId, cancellationToken);
        return Ok(new InstallationDto
        {
            Id = upgraded.Id,
            ExtensionId = upgraded.ExtensionId,
            InstalledVersion = upgraded.InstalledVersion,
            Status = upgraded.Status.ToString(),
            InstalledAt = upgraded.InstalledAt
        });
    }

    [HttpGet("installations")]
    public async Task<IActionResult> GetInstallations(CancellationToken cancellationToken)
    {
        var installations = await _installationRepo.GetByTenantAsync(Guid.Empty, cancellationToken);
        var response = installations.Select(i => new InstallationDto
        {
            Id = i.Id,
            ExtensionId = i.ExtensionId,
            ExtensionName = i.Extension?.Name ?? "Unknown",
            InstalledVersion = i.InstalledVersion,
            Status = i.Status.ToString(),
            InstalledAt = i.InstalledAt,
            UninstalledAt = i.UninstalledAt
        });

        return Ok(new { total = response.Count(), installations = response });
    }

    [HttpGet("categories")]
    public IActionResult GetCategories()
    {
        var categories = Enum.GetValues<ExtensionCategory>()
            .Select(c => new { id = (int)c, name = c.ToString() })
            .ToList();

        return Ok(new { categories });
    }

    [AegisCore.API.Authorization.RequirePermission("System", "Write")]
    [HttpPost("revenue")]
    public async Task<IActionResult> RecordRevenue([FromBody] RecordPluginRevenueRequest request, CancellationToken cancellationToken)
    {
        var extension = await _extensionRepo.GetByIdAsync(request.ExtensionId, cancellationToken);
        if (extension == null)
            return NotFound(new { error = "not_found", message = "Extension not found." });

        var split = MarketplaceRevenueCalculator.Split(request.GrossAmount);
        var row = new PluginRevenueShare
        {
            Id = Guid.NewGuid(),
            ExtensionId = request.ExtensionId,
            TenantId = request.TenantId,
            DeveloperEmail = extension.DeveloperEmail ?? "",
            GrossAmount = split.GrossAmount,
            AegisCoreAmount = split.AegisCoreAmount,
            DeveloperAmount = split.DeveloperAmount
        };

        _db.PluginRevenueShares.Add(row);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(row);
    }

    [HttpGet("developers/{email}/earnings")]
    public async Task<IActionResult> GetDeveloperEarnings(string email, CancellationToken cancellationToken)
    {
        var rows = await _db.PluginRevenueShares
            .Where(r => r.DeveloperEmail == email)
            .OrderByDescending(r => r.RecordedAt)
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            developerEmail = email,
            gross = rows.Sum(r => r.GrossAmount),
            aegiscore = rows.Sum(r => r.AegisCoreAmount),
            developer = rows.Sum(r => r.DeveloperAmount),
            rows
        });
    }

    private static ExtensionListItem MapToListItem(Extension e)
    {
        return new ExtensionListItem
        {
            Id = e.Id,
            Name = e.Name,
            Description = e.Description.Length > 200 ? e.Description[..200] + "..." : e.Description,
            Version = e.Version,
            Category = e.Category.ToString(),
            Status = e.Status.ToString(),
            PricingModel = e.PricingModel.ToString(),
            Price = e.Price,
            IconUrl = e.IconUrl,
            DeveloperName = e.DeveloperName,
            DownloadCount = e.DownloadCount,
            AverageRating = e.AverageRating,
            ReviewCount = e.ReviewCount,
            PublishedAt = e.PublishedAt
        };
    }
}
