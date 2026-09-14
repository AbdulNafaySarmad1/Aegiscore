using System.Net;
using System.Text.Json;
using AegisCore.API.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace AegisCore.Tests;

public sealed class SetupSecurityMiddlewareTests
{
    private const string Token = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    [Fact]
    public async Task BootstrapToken_RejectsInvalidTokenWithRequiredMessage()
    {
        var invoked = false;
        var middleware = new SetupBootstrapTokenMiddleware(_ => { invoked = true; return Task.CompletedTask; }, Configuration());
        var context = Context("/api/wizard/session");
        context.Request.Headers[SetupBootstrapToken.HeaderName] = "wrong";

        await middleware.InvokeAsync(context);

        Assert.False(invoked);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        using var body = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal("Invalid setup token. Check AEGISCORE_SETUP_TOKEN.", body.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task BootstrapToken_AllowsExactTokenAndIgnoresNonSetupRoutes()
    {
        var calls = 0;
        var middleware = new SetupBootstrapTokenMiddleware(_ => { calls++; return Task.CompletedTask; }, Configuration());
        var valid = Context("/api/wizard/session");
        valid.Request.Headers[SetupBootstrapToken.HeaderName] = Token;
        await middleware.InvokeAsync(valid);
        await middleware.InvokeAsync(Context("/health"));
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task SetupRateLimit_AllowsTenRequestsThenReturns429()
    {
        var calls = 0;
        var middleware = new SetupRateLimitMiddleware(_ => { calls++; return Task.CompletedTask; }, Configuration());
        DefaultHttpContext? last = null;
        var ip = IPAddress.Parse($"198.51.100.{Random.Shared.Next(1, 250)}");
        for (var request = 0; request < 11; request++)
        {
            last = Context("/api/wizard/session", ip);
            await middleware.InvokeAsync(last);
        }

        Assert.Equal(10, calls);
        Assert.Equal(StatusCodes.Status429TooManyRequests, last!.Response.StatusCode);
        last.Response.Body.Position = 0;
        using var body = await JsonDocument.ParseAsync(last.Response.Body);
        Assert.Equal("Too many setup attempts. Wait 60 seconds.", body.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task SetupRateLimit_AllowsCompleteAuthenticatedWizardBurst()
    {
        var calls = 0;
        var middleware = new SetupRateLimitMiddleware(_ => { calls++; return Task.CompletedTask; }, Configuration());
        var ip = IPAddress.Parse($"198.51.100.{Random.Shared.Next(1, 250)}");
        DefaultHttpContext? last = null;
        for (var request = 0; request < 61; request++)
        {
            last = Context("/api/wizard/session", ip);
            last.Request.Headers[SetupBootstrapToken.HeaderName] = Token;
            await middleware.InvokeAsync(last);
        }
        Assert.Equal(60, calls);
        Assert.Equal(StatusCodes.Status429TooManyRequests, last!.Response.StatusCode);
    }

    [Fact]
    public void SetupToken_MustContainAtLeast32RandomBytes()
    {
        Assert.True(SetupBootstrapToken.IsStrong(Token));
        Assert.True(SetupBootstrapToken.IsStrong(Convert.ToBase64String(new byte[32])));
        Assert.False(SetupBootstrapToken.IsStrong("short-token"));
    }

    [Fact]
    public void SetupMode_RefusesToStartWithoutStrongBootstrapToken()
    {
        var missing = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var weak = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["AEGISCORE_SETUP_TOKEN"] = "predictable" })
            .Build();
        Assert.Throws<ApplicationException>(() => SetupBootstrapToken.RequireValid(missing));
        Assert.Throws<ApplicationException>(() => SetupBootstrapToken.RequireValid(weak));
    }

    [Fact]
    public void InstallModePipeline_ContainsExceptionCorsTokenAndRateLimitMiddleware()
    {
        var source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../../../AegisCore.API/Program.cs"));
        var installBranch = source[source.IndexOf("if (!isInstalled)", source.IndexOf("var app = builder.Build()", StringComparison.Ordinal), StringComparison.Ordinal)..];
        Assert.Contains("UseMiddleware<GlobalExceptionMiddleware>()", installBranch);
        Assert.Contains("UseCors(\"CloudflarePages\")", installBranch);
        Assert.Contains("UseMiddleware<SetupRateLimitMiddleware>()", installBranch);
        Assert.Contains("UseMiddleware<SetupBootstrapTokenMiddleware>()", installBranch);
    }

    private static IConfiguration Configuration() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["AEGISCORE_SETUP_TOKEN"] = Token })
        .Build();

    private static DefaultHttpContext Context(string path, IPAddress? ip = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Connection.RemoteIpAddress = ip ?? IPAddress.Parse("203.0.113.10");
        context.Response.Body = new MemoryStream();
        return context;
    }
}
