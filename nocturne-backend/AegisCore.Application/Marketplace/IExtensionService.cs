using AegisCore.Domain.Entities;

namespace AegisCore.Application.Marketplace;

public interface IExtensionService
{
    Task<Extension> PublishExtensionAsync(Extension extension, CancellationToken cancellationToken = default);
    Task<ExtensionInstallation> InstallExtensionAsync(Guid extensionId, Guid tenantId, Guid installedByUserId, CancellationToken cancellationToken = default);
    Task UninstallExtensionAsync(Guid installationId, CancellationToken cancellationToken = default);
    Task<ExtensionInstallation> UpgradeExtensionAsync(Guid installationId, CancellationToken cancellationToken = default);
    Task<ExtensionReview> SubmitReviewAsync(Guid extensionId, Guid tenantId, Guid userId, string userName, int rating, string? comment, CancellationToken cancellationToken = default);
    Task<bool> ValidateDependenciesAsync(Guid extensionId, Guid tenantId, CancellationToken cancellationToken = default);
}
