using System.Net;
using System.Security.Claims;
using AegisCore.API.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace AegisCore.Tests;

/// <summary>Compliance control AC-33: the metrics feed is not open to the internet.</summary>
public sealed class MetricsAccessTests
{
    private static async Task<int> RunAsync(string path, string? remoteIp, string? token = null, string? configuredToken = null,
        bool authenticated = false, bool allowPrivate = true)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        if (remoteIp is not null) context.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);
        if (token is not null) context.Request.Headers[MetricsAccessMiddleware.TokenHeader] = token;
        if (authenticated) context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "ops")], "test"));
        var settings = new Dictionary<string, string?> { ["Metrics:AllowPrivateNetwork"] = allowPrivate ? "true" : "false" };
        if (configuredToken is not null) settings["Metrics:ScrapeToken"] = configuredToken;
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        var reached = false;
        await new MetricsAccessMiddleware(_ => { reached = true; return Task.CompletedTask; }).InvokeAsync(context, configuration);
        return reached ? 200 : context.Response.StatusCode;
    }

    [Fact]
    public async Task Only_a_scraper_with_the_token_a_signed_in_user_or_a_private_address_reads_the_metrics()
    {
        // A public address with nothing to show for itself.
        Assert.Equal(404, await RunAsync("/metrics", "203.0.113.5"));
        Assert.Equal(404, await RunAsync("/metrics", "203.0.113.5", token: "wrong", configuredToken: "s3cret-scrape-token"));

        Assert.Equal(200, await RunAsync("/metrics", "203.0.113.5", token: "s3cret-scrape-token", configuredToken: "s3cret-scrape-token"));
        Assert.Equal(200, await RunAsync("/metrics", "203.0.113.5", authenticated: true));
        Assert.Equal(200, await RunAsync("/metrics", "10.4.2.9"));
        Assert.Equal(200, await RunAsync("/metrics", "127.0.0.1"));
        Assert.Equal(200, await RunAsync("/metrics", "172.18.0.3"));   // docker bridge
        Assert.Equal(404, await RunAsync("/metrics", "172.32.0.3"));   // outside 172.16/12
        Assert.Equal(404, await RunAsync("/metrics", "10.4.2.9", allowPrivate: false));

        // Everything else is untouched.
        Assert.Equal(200, await RunAsync("/health", "203.0.113.5"));
        Assert.Equal(200, await RunAsync("/api/session", "203.0.113.5"));
    }
}
