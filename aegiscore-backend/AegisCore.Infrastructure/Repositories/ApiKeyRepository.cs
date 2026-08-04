using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Repositories;

public class ApiKeyRepository : Repository<ApiKey>, IApiKeyRepository
{
    public ApiKeyRepository(AegisDbContext context) : base(context) { }

    public async Task<ApiKey?> GetByKeyHashAsync(string keyHash, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(k => k.KeyHash == keyHash, cancellationToken);
    }

    public async Task<IReadOnlyList<ApiKey>> GetByOwnerAsync(string ownerEmail, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(k => k.OwnerEmail == ownerEmail).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ApiKey>> GetByStatusAsync(ApiKeyStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(k => k.Status == status).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ApiKey>> GetExpiringAsync(DateTime before, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(k => k.ExpiresAt.HasValue && k.ExpiresAt.Value <= before && k.Status == ApiKeyStatus.Active).ToListAsync(cancellationToken);
    }
}

public class WebhookSubscriptionRepository : Repository<WebhookSubscription>, IWebhookSubscriptionRepository
{
    public WebhookSubscriptionRepository(AegisDbContext context) : base(context) { }

    public async Task<IReadOnlyList<WebhookSubscription>> GetByEventTypeAsync(string eventType, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(w => w.EventType == eventType && w.Status == WebhookStatus.Active).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WebhookSubscription>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(w => w.Status == WebhookStatus.Active).ToListAsync(cancellationToken);
    }
}
