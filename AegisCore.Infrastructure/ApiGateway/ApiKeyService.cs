using System.Security.Cryptography;
using AegisCore.Application.ApiGateway;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;

namespace AegisCore.Infrastructure.ApiGateway;

public class ApiKeyService : IApiKeyService
{
    private readonly IApiKeyRepository _apiKeyRepo;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiKeyService> _logger;

    private static readonly Dictionary<ApiKeyTier, (int PerMin, int PerDay)> TierDefaults = new()
    {
        [ApiKeyTier.Free] = (30, 5000),
        [ApiKeyTier.Basic] = (120, 20000),
        [ApiKeyTier.Pro] = (600, 100000),
        [ApiKeyTier.Enterprise] = (3000, 500000)
    };

    public ApiKeyService(
        IApiKeyRepository apiKeyRepo,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<ApiKeyService> logger)
    {
        _apiKeyRepo = apiKeyRepo;
        _cache = cache;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ApiKey> CreateApiKeyAsync(string name, ApiKeyTier tier, string ownerName, string? ownerEmail, CancellationToken cancellationToken = default)
    {
        var rawKey = GenerateRawApiKey();
        var keyHash = HashApiKey(rawKey);

        var limits = TierDefaults.GetValueOrDefault(tier, (60, 10000));

        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            Name = name,
            KeyHash = keyHash,
            KeyPrefix = rawKey[..8],
            Tier = tier,
            OwnerName = ownerName,
            OwnerEmail = ownerEmail,
            RateLimitPerMinute = limits.Item1,
            RateLimitPerDay = limits.Item2,
            Status = ApiKeyStatus.Active,
            Permission = tier >= ApiKeyTier.Pro ? ApiPermission.ReadWrite : ApiPermission.ReadOnly
        };

        await _apiKeyRepo.AddAsync(apiKey, cancellationToken);

        _logger.LogInformation("API key created: {Name} ({Prefix}) for {Owner}", name, apiKey.KeyPrefix[..Math.Min(apiKey.KeyPrefix.Length, 8)], ownerName);

        // Return raw key only in the response DTO, never persisted
        apiKey.KeyHash = rawKey; // Temporary — caller must read and discard immediately
        return apiKey;
    }

    public async Task<ApiKey> RotateApiKeyAsync(Guid apiKeyId, CancellationToken cancellationToken = default)
    {
        var apiKey = await _apiKeyRepo.GetByIdAsync(apiKeyId, cancellationToken)
            ?? throw new KeyNotFoundException("API key not found.");

        var rawKey = GenerateRawApiKey();
        apiKey.KeyHash = HashApiKey(rawKey);
        apiKey.KeyPrefix = rawKey[..8];

        await _apiKeyRepo.UpdateAsync(apiKey, cancellationToken);

        apiKey.KeyHash = rawKey;
        _logger.LogInformation("API key rotated: {Name} ({Prefix})", apiKey.Name, apiKey.KeyPrefix);

        return apiKey;
    }

    public async Task RevokeApiKeyAsync(Guid apiKeyId, string reason, CancellationToken cancellationToken = default)
    {
        var apiKey = await _apiKeyRepo.GetByIdAsync(apiKeyId, cancellationToken)
            ?? throw new KeyNotFoundException("API key not found.");

        apiKey.Status = ApiKeyStatus.Revoked;
        apiKey.RevokedAt = DateTime.UtcNow;
        apiKey.RevocationReason = reason;

        await _apiKeyRepo.UpdateAsync(apiKey, cancellationToken);
        _logger.LogInformation("API key revoked: {Name} - {Reason}", apiKey.Name, reason);
    }

    public async Task<ApiKey?> ValidateApiKeyAsync(string apiKeyHeader, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(apiKeyHeader))
            return null;

        var cacheKey = $"apikey:{apiKeyHeader}";
        if (_cache.TryGetValue<ApiKey>(cacheKey, out var cached))
            return cached;

        var keyHash = HashApiKey(apiKeyHeader);
        var apiKey = await _apiKeyRepo.GetByKeyHashAsync(keyHash, cancellationToken);

        if (apiKey == null || apiKey.Status != ApiKeyStatus.Active)
            return null;

        if (apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt.Value < DateTime.UtcNow)
        {
            apiKey.Status = ApiKeyStatus.Expired;
            await _apiKeyRepo.UpdateAsync(apiKey, cancellationToken);
            return null;
        }

        apiKey.LastUsedAt = DateTime.UtcNow;
        apiKey.TotalRequests++;
        await _apiKeyRepo.UpdateAsync(apiKey, cancellationToken);

        _cache.Set(cacheKey, apiKey, TimeSpan.FromMinutes(5));

        return apiKey;
    }

    public async Task<bool> IsRateLimitedAsync(Guid apiKeyId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"ratelimit:{apiKeyId}";
        var windowKey = $"ratelimit_window:{apiKeyId}";

        if (_cache.TryGetValue<int>(cacheKey, out var count))
        {
            var apiKey = await _apiKeyRepo.GetByIdAsync(apiKeyId, cancellationToken);
            if (apiKey != null && count >= apiKey.RateLimitPerMinute)
                return true;

            _cache.Set(cacheKey, count + 1, TimeSpan.FromMinutes(1));
            return false;
        }

        _cache.Set(cacheKey, 1, TimeSpan.FromMinutes(1));
        return false;
    }

    private static string GenerateRawApiKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return $"aegis_{Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "")[..40]}";
    }

    private static string HashApiKey(string rawKey)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawKey));
        return Convert.ToHexString(bytes);
    }
}
