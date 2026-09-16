using StackExchange.Redis;
using AegisCore.Application.Common.Interfaces;
using AegisCore.Contracts.Common.Interfaces;

namespace AegisCore.Infrastructure.Security;

/// <summary>
/// Service for brute force protection using Redis.
/// </summary>
public class BruteForceProtectionService : IBruteForceProtectionService
{
    private readonly IConnectionMultiplexer _redis;
    private const int MaxFailedAttempts = 5;
    private const int LockoutDurationSeconds = 900; // 15 minutes

    /// <summary>
    /// Initializes a new instance of the <see cref="BruteForceProtectionService"/> class.
    /// </summary>
    /// <param name="redis">The Redis connection multiplexer.</param>
    public BruteForceProtectionService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    /// <inheritdoc />
    public async Task<bool> IsLockedOutAsync(string email)
    {
        var key = $"login_lockout:{email}";
        var lockoutCount = await _redis.GetDatabase().StringGetAsync(key);
        return lockoutCount.HasValue && int.Parse(lockoutCount.ToString()) >= MaxFailedAttempts;
    }

    /// <inheritdoc />
    public async Task RecordFailedAttemptAsync(string email)
    {
        var key = $"login_lockout:{email}";
        var db = _redis.GetDatabase();
        var result = await db.StringIncrementAsync(key);
        if (result == 1)
        {
            await db.KeyExpireAsync(key, TimeSpan.FromSeconds(LockoutDurationSeconds));
        }
    }

    /// <inheritdoc />
    public async Task ClearFailedAttemptsAsync(string email)
    {
        var key = $"login_lockout:{email}";
        await _redis.GetDatabase().KeyDeleteAsync(key);
    }
}
