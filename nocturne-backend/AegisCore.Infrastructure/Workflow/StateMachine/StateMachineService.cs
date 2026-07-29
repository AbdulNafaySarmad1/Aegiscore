using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace AegisCore.Infrastructure.Workflow.StateMachine;

public interface IStateMachineService
{
    Task SetStateAsync(string key, string state, CancellationToken cancellationToken = default);
    Task<string?> GetStateAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> TransitionAsync(string key, string expected, string next, CancellationToken cancellationToken = default);
}

public class RedisStateMachineService : IStateMachineService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisStateMachineService> _logger;

    public RedisStateMachineService(IConnectionMultiplexer redis, ILogger<RedisStateMachineService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task SetStateAsync(string key, string state, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        await db.StringSetAsync($"sm:{key}", state);
    }

    public async Task<string?> GetStateAsync(string key, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync($"sm:{key}");
        return value.HasValue ? value.ToString() : null;
    }

    public async Task<bool> TransitionAsync(string key, string expected, string next, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var current = await db.StringGetAsync($"sm:{key}");
        if (current != expected)
        {
            _logger.LogWarning("State transition for {Key} rejected: expected {Expected}, found {Current}.", key, expected, current);
            return false;
        }

        await db.StringSetAsync($"sm:{key}", next);
        return true;
    }
}

public static class StateMachineServiceExtensions
{
    public static IServiceCollection AddStateMachine(this IServiceCollection services)
    {
        services.AddScoped<IStateMachineService, RedisStateMachineService>();
        return services;
    }
}
