using System.Net;
using System.Text.Json;
using AegisCore.Infrastructure.Cloud.Infisical;
using Microsoft.Extensions.Configuration;
using Moq;
using StackExchange.Redis;

namespace AegisCore.Tests;

public sealed class InfisicalSecretServiceTests
{
    [Fact]
    public async Task V4Contract_UsesTenantPathAndCorrectCreateUpdateDeleteMethods()
    {
        var requests = new List<CapturedRequest>();
        var responses = new Queue<HttpResponseMessage>(
        [
            Json(HttpStatusCode.OK, "{\"secret\":{\"secretValue\":\"first\"}}"),
            new(HttpStatusCode.NotFound), Json(HttpStatusCode.OK, "{}"),
            Json(HttpStatusCode.OK, "{\"secret\":{\"secretValue\":\"first\"}}"), Json(HttpStatusCode.OK, "{}"),
            Json(HttpStatusCode.OK, "{}")
        ]);
        using var client = new HttpClient(new RecordingHandler(requests, responses)) { BaseAddress = new Uri("https://vault.example") };
        var database = new Mock<IDatabase>();
        database.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(RedisValue.Null);
        database.Setup(x => x.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<When>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);
        database.Setup(x => x.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);
        var redis = new Mock<IConnectionMultiplexer>();
        redis.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(database.Object);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["INFISICAL_TOKEN"] = "test-token-from-runtime",
            ["INFISICAL_WORKSPACE_ID"] = "project-1",
            ["INFISICAL_ENVIRONMENT"] = "production"
        }).Build();
        var service = new InfisicalSecretService(new FixedClientFactory(client), configuration, redis.Object);

        Assert.Equal("first", await service.GetSecretAsync("/tenants/tenant-a/pqc/signing/PRIVATE_KEY"));
        await service.SetSecretAsync("/tenants/tenant-a/pqc/signing/PRIVATE_KEY", "created");
        await service.SetSecretAsync("/tenants/tenant-a/pqc/signing/PRIVATE_KEY", "updated");
        await service.DeleteSecretAsync("/tenants/tenant-a/pqc/signing/PRIVATE_KEY");

        Assert.Equal([HttpMethod.Get, HttpMethod.Get, HttpMethod.Post, HttpMethod.Get, HttpMethod.Patch, HttpMethod.Delete], requests.Select(x => x.Method));
        Assert.All(requests, request => Assert.Equal("Bearer test-token-from-runtime", request.Authorization));
        Assert.Contains("/api/v4/secrets/PRIVATE_KEY?", requests[0].Uri);
        Assert.Contains("secretPath=%2Ftenants%2Ftenant-a%2Fpqc%2Fsigning", requests[0].Uri);
        Assert.Equal("created", JsonDocument.Parse(requests[2].Body!).RootElement.GetProperty("secretValue").GetString());
        Assert.Equal("updated", JsonDocument.Parse(requests[4].Body!).RootElement.GetProperty("secretValue").GetString());
        Assert.Equal("/tenants/tenant-a/pqc/signing", JsonDocument.Parse(requests[5].Body!).RootElement.GetProperty("secretPath").GetString());
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string content)
        => new(status) { Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json") };

    private sealed record CapturedRequest(HttpMethod Method, string Uri, string? Authorization, string? Body);

    private sealed class RecordingHandler(List<CapturedRequest> requests, Queue<HttpResponseMessage> responses) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            requests.Add(new(request.Method, request.RequestUri!.PathAndQuery, request.Headers.Authorization?.ToString(), request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken)));
            return responses.Dequeue();
        }
    }

    private sealed class FixedClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }
}
