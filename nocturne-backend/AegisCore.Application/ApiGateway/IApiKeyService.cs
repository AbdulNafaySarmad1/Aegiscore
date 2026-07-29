using AegisCore.Domain.Entities;

namespace AegisCore.Application.ApiGateway;

public interface IApiKeyService
{
    Task<ApiKey> CreateApiKeyAsync(string name, ApiKeyTier tier, string ownerName, string? ownerEmail, CancellationToken cancellationToken = default);
    Task<ApiKey> RotateApiKeyAsync(Guid apiKeyId, CancellationToken cancellationToken = default);
    Task RevokeApiKeyAsync(Guid apiKeyId, string reason, CancellationToken cancellationToken = default);
    Task<ApiKey?> ValidateApiKeyAsync(string apiKey, CancellationToken cancellationToken = default);
    Task<bool> IsRateLimitedAsync(Guid apiKeyId, CancellationToken cancellationToken = default);
}
