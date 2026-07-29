using AegisCore.Domain;
using AegisCore.Domain.Entities;

namespace AegisCore.Application.Common.Interfaces;

public interface IExtensionRepository : IRepository<Extension>
{
    Task<IReadOnlyList<Extension>> GetByCategoryAsync(ExtensionCategory category, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Extension>> GetPublishedAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Extension>> GetByDeveloperAsync(string developerEmail, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Extension>> SearchAsync(string query, CancellationToken cancellationToken = default);
    Task<Extension?> GetByNameAndVersionAsync(string name, string version, CancellationToken cancellationToken = default);
}

public interface IExtensionInstallationRepository : IRepository<ExtensionInstallation>
{
    Task<IReadOnlyList<ExtensionInstallation>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<ExtensionInstallation?> GetByTenantAndExtensionAsync(Guid tenantId, Guid extensionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExtensionInstallation>> GetByExtensionAsync(Guid extensionId, CancellationToken cancellationToken = default);
}

public interface IExtensionReviewRepository : IRepository<ExtensionReview>
{
    Task<IReadOnlyList<ExtensionReview>> GetByExtensionAsync(Guid extensionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExtensionReview>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<double> GetAverageRatingAsync(Guid extensionId, CancellationToken cancellationToken = default);
}
