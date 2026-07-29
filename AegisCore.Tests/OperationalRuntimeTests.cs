using AegisCore.API.Controllers;
using AegisCore.API.Middleware;
using AegisCore.Infrastructure.Deployment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;

namespace AegisCore.Tests;

public sealed class OperationalRuntimeTests
{
    [Fact]
    public async Task DemoIdentity_IsAdministrative_AndTenantScoped_OutsideProduction()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Runtime:DemoMode"] = "true",
                ["TENANT_ID"] = "11111111-1111-1111-1111-111111111111"
            })
            .Build();
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(x => x.EnvironmentName).Returns(Environments.Development);
        var context = new DefaultHttpContext();
        var middleware = new DemoIdentityMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context, configuration, environment.Object);

        Assert.True(context.User.Identity?.IsAuthenticated);
        Assert.True(context.User.IsInRole("SuperAdmin"));
        Assert.Equal(
            "11111111-1111-1111-1111-111111111111",
            context.User.FindFirst("tenant_id")?.Value);
    }

    [Fact]
    public async Task DemoIdentity_IsNeverInjectedInProduction()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Runtime:DemoMode"] = "true" })
            .Build();
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(x => x.EnvironmentName).Returns(Environments.Production);
        var context = new DefaultHttpContext();
        var middleware = new DemoIdentityMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context, configuration, environment.Object);

        Assert.NotEqual(true, context.User.Identity?.IsAuthenticated);
    }

    [Theory]
    [InlineData(typeof(SetupController))]
    [InlineData(typeof(EnhancedSetupController))]
    public void SetupControllers_AreReachableBeforeAuthenticationIsConfigured(Type controllerType)
    {
        Assert.NotNull(Attribute.GetCustomAttribute(controllerType, typeof(AllowAnonymousAttribute)));
    }

    [Fact]
    public void Readiness_UsesContainerRedisConfigurationBeforeLocalDefault()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["NOCTURNE_REDIS_CONNECTION"] = "redis:6379,password=runtime-secret",
                ["ConnectionStrings:Redis"] = "localhost:6379"
            })
            .Build();

        Assert.Equal(
            "redis:6379,password=runtime-secret",
            DeploymentReadinessChecker.ResolveRedisConnectionString(configuration));
    }
}
