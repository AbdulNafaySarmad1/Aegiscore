using System.Security.Cryptography;
using System.Text;
using StackExchange.Redis;

namespace AegisCore.Infrastructure.Security.Pqc;

public interface IHybridPqcReplayStore
{
    Task<bool> TryUseAsync(string nonce, TimeSpan lifetime, CancellationToken ct);
}

public sealed class RedisHybridPqcReplayStore(IConnectionMultiplexer redis) : IHybridPqcReplayStore
{
    public async Task<bool> TryUseAsync(string nonce, TimeSpan lifetime, CancellationToken ct)
    {
        var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(nonce)));
        return await redis.GetDatabase()
            .StringSetAsync($"pqc-transport:nonce:{digest}", "1", lifetime, When.NotExists)
            .WaitAsync(ct);
    }
}
