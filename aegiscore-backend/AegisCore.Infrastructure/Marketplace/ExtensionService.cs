using System.Text.Json;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Application.Marketplace;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Marketplace;

public class ExtensionService : IExtensionService
{
    private readonly IExtensionRepository _extensionRepo;
    private readonly IExtensionInstallationRepository _installationRepo;
    private readonly IExtensionReviewRepository _reviewRepo;
    private readonly ILogger<ExtensionService> _logger;

    public ExtensionService(
        IExtensionRepository extensionRepo,
        IExtensionInstallationRepository installationRepo,
        IExtensionReviewRepository reviewRepo,
        ILogger<ExtensionService> logger)
    {
        _extensionRepo = extensionRepo;
        _installationRepo = installationRepo;
        _reviewRepo = reviewRepo;
        _logger = logger;
    }

    public async Task<Extension> PublishExtensionAsync(Extension extension, CancellationToken cancellationToken = default)
    {
        var existing = await _extensionRepo.GetByNameAndVersionAsync(extension.Name, extension.Version, cancellationToken);
        if (existing != null)
            throw new InvalidOperationException($"Extension '{extension.Name}' v{extension.Version} already exists.");

        extension.Status = ExtensionStatus.Draft;
        extension.CreatedAt = DateTime.UtcNow;
        extension.UpdatedAt = DateTime.UtcNow;

        await _extensionRepo.AddAsync(extension, cancellationToken);

        _logger.LogInformation("Extension submitted: {Name} v{Version} by {Developer}",
            extension.Name, extension.Version, extension.DeveloperName);

        return extension;
    }

    public async Task<ExtensionInstallation> InstallExtensionAsync(Guid extensionId, Guid tenantId, Guid installedByUserId, CancellationToken cancellationToken = default)
    {
        var extension = await _extensionRepo.GetByIdAsync(extensionId, cancellationToken)
            ?? throw new InvalidOperationException("Extension not found.");

        if (extension.Status != ExtensionStatus.Published)
            throw new InvalidOperationException("Cannot install an extension that is not published.");

        var existing = await _installationRepo.GetByTenantAndExtensionAsync(tenantId, extensionId, cancellationToken);
        if (existing != null)
            throw new InvalidOperationException($"Extension '{extension.Name}' is already installed.");

        var depsValid = await ValidateDependenciesAsync(extensionId, tenantId, cancellationToken);
        if (!depsValid)
            throw new InvalidOperationException("Dependency check failed. Ensure all dependent extensions are installed.");

        var installation = new ExtensionInstallation
        {
            Id = Guid.NewGuid(),
            ExtensionId = extensionId,
            InstalledVersion = extension.Version,
            Status = InstallationStatus.Installed,
            InstalledAt = DateTime.UtcNow,
            InstalledByUserId = installedByUserId,
            TenantId = tenantId,
            InstallLog = $"Installed {extension.Name} v{extension.Version} successfully."
        };

        await _installationRepo.AddAsync(installation, cancellationToken);

        extension.DownloadCount++;
        await _extensionRepo.UpdateAsync(extension, cancellationToken);

        _logger.LogInformation("Extension installed: {Name} v{Version} for tenant {TenantId}",
            extension.Name, extension.Version, tenantId);

        return installation;
    }

    public async Task UninstallExtensionAsync(Guid installationId, CancellationToken cancellationToken = default)
    {
        var installation = await _installationRepo.GetByIdAsync(installationId, cancellationToken)
            ?? throw new InvalidOperationException("Installation not found.");

        if (installation.Status == InstallationStatus.Uninstalled)
            throw new InvalidOperationException("Extension is already uninstalled.");

        installation.Status = InstallationStatus.Uninstalled;
        installation.UninstalledAt = DateTime.UtcNow;
        installation.InstallLog = $"Uninstalled at {DateTime.UtcNow:yyyy-MM-dd HH:mm}.";

        await _installationRepo.UpdateAsync(installation, cancellationToken);

        _logger.LogInformation("Extension uninstalled: installation {InstallationId}", installationId);
    }

    public async Task<ExtensionInstallation> UpgradeExtensionAsync(Guid installationId, CancellationToken cancellationToken = default)
    {
        var installation = await _installationRepo.GetByIdAsync(installationId, cancellationToken)
            ?? throw new InvalidOperationException("Installation not found.");

        var extension = await _extensionRepo.GetByIdAsync(installation.ExtensionId, cancellationToken)
            ?? throw new InvalidOperationException("Extension not found.");

        if (installation.InstalledVersion == extension.Version)
            throw new InvalidOperationException("Already on the latest version.");

        installation.InstalledVersion = extension.Version;
        installation.InstallLog = $"Upgraded from {installation.InstalledVersion} to {extension.Version} at {DateTime.UtcNow:yyyy-MM-dd HH:mm}.";

        await _installationRepo.UpdateAsync(installation, cancellationToken);

        _logger.LogInformation("Extension upgraded: installation {InstallationId} to v{Version}", installationId, extension.Version);

        return installation;
    }

    public async Task<ExtensionReview> SubmitReviewAsync(Guid extensionId, Guid tenantId, Guid userId, string userName, int rating, string? comment, CancellationToken cancellationToken = default)
    {
        if (rating < 1 || rating > 5)
            throw new ArgumentException("Rating must be between 1 and 5.");

        var extension = await _extensionRepo.GetByIdAsync(extensionId, cancellationToken)
            ?? throw new InvalidOperationException("Extension not found.");

        var review = new ExtensionReview
        {
            Id = Guid.NewGuid(),
            ExtensionId = extensionId,
            Rating = rating,
            Comment = comment,
            ReviewedByName = userName,
            ReviewedByUserId = userId,
            TenantId = tenantId,
            CreatedAt = DateTime.UtcNow
        };

        await _reviewRepo.AddAsync(review, cancellationToken);

        extension.AverageRating = await _reviewRepo.GetAverageRatingAsync(extensionId, cancellationToken);
        extension.ReviewCount++;
        await _extensionRepo.UpdateAsync(extension, cancellationToken);

        _logger.LogInformation("Review submitted for {ExtensionName}: {Rating}/5", extension.Name, rating);

        return review;
    }

    public async Task<bool> ValidateDependenciesAsync(Guid extensionId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var extension = await _extensionRepo.GetByIdAsync(extensionId, cancellationToken);
        if (extension == null) return false;

        var dependencies = JsonSerializer.Deserialize<List<ExtensionDependency>>(
            extension.DependenciesJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? new List<ExtensionDependency>();

        if (dependencies.Count == 0) return true;

        var installations = await _installationRepo.GetByTenantAsync(tenantId, cancellationToken);
        var installedNames = installations.Select(i => i.Extension?.Name ?? "").ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var dep in dependencies)
        {
            if (!installedNames.Contains(dep.Name))
            {
                _logger.LogWarning("Dependency missing for {Extension}: requires {Dependency} {Constraint}",
                    extension.Name, dep.Name, dep.VersionConstraint);
                return false;
            }
        }

        return true;
    }
}
