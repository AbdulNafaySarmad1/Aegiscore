using System.Security.Claims;
using AegisCore.API.Middleware;
using Microsoft.AspNetCore.Http;

namespace AegisCore.Tests;

public sealed class TenantContextMiddlewareTests
{
    [Fact]
    public async Task Authenticated_Request_Binds_Validated_Tenant()
    {
        var tenantId = Guid.NewGuid();
        var called = false;
        var middleware = new TenantContextMiddleware(_ => { called = true; return Task.CompletedTask; });
        var context = Context(new Claim("tenant_id", tenantId.ToString()));

        await middleware.InvokeAsync(context);

        Assert.True(called);
        Assert.Equal(tenantId, context.Items["TenantId"]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task Authenticated_Request_Rejects_Invalid_Tenant(string tenantClaim)
    {
        var called = false;
        var middleware = new TenantContextMiddleware(_ => { called = true; return Task.CompletedTask; });
        var context = Context(new Claim("tenant_id", tenantClaim));

        await middleware.InvokeAsync(context);

        Assert.False(called);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
    }

    private static DefaultHttpContext Context(Claim tenantClaim)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.User = new ClaimsPrincipal(new ClaimsIdentity([tenantClaim], "test"));
        return context;
    }
}
