using AegisCore.API.Controllers;
using AegisCore.API.Middleware;
using AegisCore.Infrastructure.Deployment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using System.Reflection;

namespace AegisCore.Tests;

public sealed class OperationalRuntimeTests
{
    [Fact]
    public async Task DemoIdentityInjection_IsDisabledEvenInDevelopment()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Runtime:DemoMode"] = "true",
                ["Runtime:InjectDemoIdentity"] = "true",
                ["TENANT_ID"] = "11111111-1111-1111-1111-111111111111"
            })
            .Build();
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(x => x.EnvironmentName).Returns(Environments.Development);
        var context = new DefaultHttpContext();
        var middleware = new DemoIdentityMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context, configuration, environment.Object, Mock.Of<ILogger<DemoIdentityMiddleware>>());

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
        Assert.NotEqual(true, context.User.Identity?.IsAuthenticated);
    }

    [Fact]
    public async Task DemoIdentity_IsNotInjectedUnlessExplicitlyEnabled()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Runtime:DemoMode"] = "true" })
            .Build();
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(x => x.EnvironmentName).Returns(Environments.Development);
        var context = new DefaultHttpContext();
        var middleware = new DemoIdentityMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context, configuration, environment.Object, Mock.Of<ILogger<DemoIdentityMiddleware>>());

        Assert.NotEqual(true, context.User.Identity?.IsAuthenticated);
    }

    [Fact]
    public async Task DemoIdentity_IsNotInjectedInProduction()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Runtime:DemoMode"] = "true" })
            .Build();
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(x => x.EnvironmentName).Returns(Environments.Production);
        var context = new DefaultHttpContext();
        var middleware = new DemoIdentityMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context, configuration, environment.Object, Mock.Of<ILogger<DemoIdentityMiddleware>>());

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
    }

    [Fact]
    public async Task DemoIdentity_IsRejectedOutsideDevelopment()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Runtime:DemoMode"] = "true" })
            .Build();
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(x => x.EnvironmentName).Returns(Environments.Staging);
        var context = new DefaultHttpContext();
        var middleware = new DemoIdentityMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context, configuration, environment.Object, Mock.Of<ILogger<DemoIdentityMiddleware>>());

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
    }

    [Fact]
    public async Task DemoIdentityInjection_IsDisabledForRemoteRequests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Runtime:DemoMode"] = "true",
                ["Runtime:InjectDemoIdentity"] = "true"
            })
            .Build();
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(x => x.EnvironmentName).Returns(Environments.Development);
        var context = new DefaultHttpContext();
        var middleware = new DemoIdentityMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context, configuration, environment.Object, Mock.Of<ILogger<DemoIdentityMiddleware>>());

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
        Assert.NotEqual(true, context.User.Identity?.IsAuthenticated);
    }

    [Fact]
    public async Task DemoIdentity_IsNotInjectedWhenDemoModeDisabled()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Runtime:DemoMode"] = "false" })
            .Build();
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(x => x.EnvironmentName).Returns(Environments.Development);
        var context = new DefaultHttpContext();
        var middleware = new DemoIdentityMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context, configuration, environment.Object, Mock.Of<ILogger<DemoIdentityMiddleware>>());

        Assert.NotEqual(true, context.User.Identity?.IsAuthenticated);
    }

    [Fact]
    public void ObeEvidence_ExcludesUnscopedFacultyRole()
    {
        var method = typeof(S5ObeController).GetMethod(nameof(S5ObeController.Evidence));
        var roles = method!.GetCustomAttributes<AuthorizeAttribute>().Select(attribute => attribute.Roles).ToArray();

        Assert.Contains(roles, value => value?.Contains("OBECoordinator", StringComparison.Ordinal) == true);
        Assert.DoesNotContain(roles, value => value?.Split(',').Contains("Faculty", StringComparer.Ordinal) == true);
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
                ["AEGISCORE_REDIS_CONNECTION"] = "redis:6379,password=runtime-secret",
                ["ConnectionStrings:Redis"] = "localhost:6379"
            })
            .Build();

        Assert.Equal(
            "redis:6379,password=runtime-secret",
            DeploymentReadinessChecker.ResolveRedisConnectionString(configuration));
    }
}
