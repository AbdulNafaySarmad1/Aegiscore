using AegisCore.Domain.Entities;

namespace AegisCore.Application.Common.Interfaces;

public interface IApiKeyRepository : IRepository<ApiKey>
{
    Task<ApiKey?> GetByKeyHashAsync(string keyHash, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApiKey>> GetByOwnerAsync(string ownerEmail, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApiKey>> GetByStatusAsync(ApiKeyStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApiKey>> GetExpiringAsync(DateTime before, CancellationToken cancellationToken = default);
}

public interface IWebhookSubscriptionRepository : IRepository<WebhookSubscription>
{
    Task<IReadOnlyList<WebhookSubscription>> GetByEventTypeAsync(string eventType, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WebhookSubscription>> GetActiveAsync(CancellationToken cancellationToken = default);
}
