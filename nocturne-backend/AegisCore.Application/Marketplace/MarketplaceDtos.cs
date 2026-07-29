namespace AegisCore.Application.Marketplace;

public class ExtensionManifest
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "Finance";
    public string PricingModel { get; set; } = "Free";
    public decimal Price { get; set; }
    public string DeveloperName { get; set; } = string.Empty;
    public string? DeveloperEmail { get; set; }
    public string? DeveloperWebsite { get; set; }
    public string? IconUrl { get; set; }
    public List<string> Screenshots { get; set; } = new();
    public List<ExtensionDependency> Dependencies { get; set; } = new();
    public List<string> RequiredPermissions { get; set; } = new();
    public string? PackageChecksum { get; set; }
    public string? PackageUrl { get; set; }
    public string MinAegisCoreVersion { get; set; } = "1.0.0";
}

public class ExtensionDependency
{
    public string Name { get; set; } = string.Empty;
    public string VersionConstraint { get; set; } = ">=1.0.0";
}

public class ExtensionListItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PricingModel { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? IconUrl { get; set; }
    public string DeveloperName { get; set; } = string.Empty;
    public long DownloadCount { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public DateTime? PublishedAt { get; set; }
}

public class ExtensionDetail
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PricingModel { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? IconUrl { get; set; }
    public List<string> Screenshots { get; set; } = new();
    public string DeveloperName { get; set; } = string.Empty;
    public string? DeveloperEmail { get; set; }
    public string? DeveloperWebsite { get; set; }
    public long DownloadCount { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public List<ExtensionDependency> Dependencies { get; set; } = new();
    public List<string> RequiredPermissions { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
}

public class InstallExtensionRequest
{
    public Guid ExtensionId { get; set; }
}

public class ExtensionReviewRequest
{
    public int Rating { get; set; }
    public string? Comment { get; set; }
}

public class ExtensionReviewResponse
{
    public Guid Id { get; set; }
    public string ReviewedByName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public string? DeveloperResponse { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class InstallationDto
{
    public Guid Id { get; set; }
    public Guid ExtensionId { get; set; }
    public string ExtensionName { get; set; } = string.Empty;
    public string InstalledVersion { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime InstalledAt { get; set; }
    public DateTime? UninstalledAt { get; set; }
}
