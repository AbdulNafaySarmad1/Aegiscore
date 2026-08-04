using System.Security.Cryptography;
using System.Text;
using AegisCore.API.Middleware;
using AegisCore.Infrastructure.Security.Pqc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace AegisCore.Tests;

public sealed class HybridPqcTransportMiddlewareTests
{
    [Fact]
    public async Task Valid_Edge_Attestation_Allows_Finance_Request()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var context = Context("/api/finance/v2/dashboard", key, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        var called = false;
        var middleware = Middleware(_ => { called = true; return Task.CompletedTask; }, key);

        await middleware.InvokeAsync(context);

        Assert.True(called);
    }

    [Theory]
    [InlineData(-61)]
    [InlineData(61)]
    public async Task Stale_Edge_Attestation_Is_Rejected(int offsetSeconds)
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var context = Context("/api/finance/v2/dashboard", key, DateTimeOffset.UtcNow.AddSeconds(offsetSeconds).ToUnixTimeSeconds());
        var middleware = Middleware(_ => Task.CompletedTask, key);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status426UpgradeRequired, context.Response.StatusCode);
    }

    [Fact]
    public async Task Forged_Edge_Attestation_Is_Rejected()
    {
        var context = Context("/api/finance/v2/dashboard", RandomNumberGenerator.GetBytes(32), DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        var middleware = Middleware(_ => Task.CompletedTask, RandomNumberGenerator.GetBytes(32));

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status426UpgradeRequired, context.Response.StatusCode);
    }

    [Fact]
    public async Task Extreme_Timestamp_Is_Rejected_Without_Overflow()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var context = Context("/api/finance/v2/dashboard", key, long.MinValue);
        var middleware = Middleware(_ => Task.CompletedTask, key);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status426UpgradeRequired, context.Response.StatusCode);
    }

    [Fact]
    public async Task Non_Finance_Request_Does_Not_Require_Attestation()
    {
        var called = false;
        var context = new DefaultHttpContext();
        context.Request.Path = "/health/live";
        var middleware = Middleware(_ => { called = true; return Task.CompletedTask; }, RandomNumberGenerator.GetBytes(32));

        await middleware.InvokeAsync(context);

        Assert.True(called);
    }

    [Fact]
    public async Task Replayed_Attestation_Is_Rejected()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var replayStore = new MemoryReplayStore();
        var middleware = new HybridPqcTransportMiddleware(_ => Task.CompletedTask, Configuration(key), replayStore);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var first = Context("/api/finance/v2/dashboard", key, timestamp);
        var replay = Context("/api/finance/v2/dashboard", key, timestamp);

        await middleware.InvokeAsync(first);
        await middleware.InvokeAsync(replay);

        Assert.Equal(StatusCodes.Status426UpgradeRequired, replay.Response.StatusCode);
    }

    [Fact]
    public async Task Query_Or_Body_Tampering_Is_Rejected()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var query = Context("/api/finance/v2/dashboard", key, timestamp);
        query.Request.QueryString = new QueryString("?tenant=other");
        var body = Context("/api/finance/v2/vouchers", key, timestamp, "original", HttpMethods.Post);
        body.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("changed"));

        await Middleware(_ => Task.CompletedTask, key).InvokeAsync(query);
        await Middleware(_ => Task.CompletedTask, key).InvokeAsync(body);

        Assert.Equal(StatusCodes.Status426UpgradeRequired, query.Response.StatusCode);
        Assert.Equal(StatusCodes.Status426UpgradeRequired, body.Response.StatusCode);
    }

    private static DefaultHttpContext Context(string path, byte[] key, long timestamp, string body = "", string method = "GET")
    {
        const string mode = "ml-kem-1024+x25519";
        const string nonce = "test-nonce-0000000000000001";
        var contentHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(body)));
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        context.Request.Headers["X-AegisCore-PQC-Transport"] = mode;
        context.Request.Headers["X-AegisCore-PQC-Timestamp"] = timestamp.ToString(System.Globalization.CultureInfo.InvariantCulture);
        context.Request.Headers["X-AegisCore-PQC-Nonce"] = nonce;
        context.Request.Headers["X-AegisCore-Content-SHA256"] = contentHash;
        context.Request.Headers["X-AegisCore-PQC-Proof"] = Convert.ToBase64String(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes($"{method}|{path}||{timestamp}|{nonce}|{mode}|{contentHash}")));
        return context;
    }

    private static HybridPqcTransportMiddleware Middleware(RequestDelegate next, byte[] key)
        => new(next, Configuration(key), new MemoryReplayStore());

    private static IConfiguration Configuration(byte[] key) => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["HybridPqcTransport:Enforce"] = "true",
        ["HybridPqcTransport:EdgeProofKeyBase64"] = Convert.ToBase64String(key)
    }).Build();

    private sealed class MemoryReplayStore : IHybridPqcReplayStore
    {
        private readonly HashSet<string> _used = new(StringComparer.Ordinal);
        public Task<bool> TryUseAsync(string nonce, TimeSpan lifetime, CancellationToken ct)
        {
            lock (_used) return Task.FromResult(_used.Add(nonce));
        }
    }
}
