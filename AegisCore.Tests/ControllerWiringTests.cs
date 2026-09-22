using System.Reflection;
using AegisCore.API.Extensions;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Installation;
using AegisCore.Infrastructure.Security.Pqc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AegisCore.Tests;

/// <summary>
/// A controller whose constructor asks for a service nobody registered compiles, passes every unit
/// test, and returns 500 on its first real request. This composes the container exactly as the
/// installed runtime does (Program.cs production branch), takes the controllers MVC will actually
/// map, and resolves every constructor and [FromServices] dependency, so a missing registration
/// fails here instead of in front of a client.
/// </summary>
public sealed class ControllerWiringTests
{
    public static IHost BuildInstalledRuntimeHost()
    {
        var settings = new Dictionary<string, string?>
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Development",
            ["AEGISCORE_DB_CONNECTION"] = "Host=127.0.0.1;Port=1;Database=wiring;Username=wiring;Password=wiring",
            ["AEGISCORE_REDIS_CONNECTION"] = "127.0.0.1:1,abortConnect=false",
            ["AEGISCORE_JWT_SECRET"] = Convert.ToBase64String(new byte[32]),
            ["AEGISCORE_LOCAL_SECRET_PATH"] = Path.Combine(Path.GetTempPath(), "aegiscore-wiring", "vault.bin"),
            ["DataProtection:KeyPath"] = Path.Combine(Path.GetTempPath(), "aegiscore-wiring", "keys"),
        };
        return new HostBuilder()
            .UseEnvironment("Development")
            .ConfigureAppConfiguration(c => c.AddInMemoryCollection(settings))
            .UseDefaultServiceProvider(o => { o.ValidateScopes = true; o.ValidateOnBuild = true; })
            .ConfigureWebHost(web => web.UseTestServer().ConfigureServices((context, services) =>
            {
                var configuration = context.Configuration;
                services.AddInstallationState();
                services.AddCoreServices(configuration);
                services.AddInfrastructure(configuration);
                services.AddProductionServices(configuration);
                services.AddAuthenticationServices(configuration);
                services.AddBackgroundWorkers(configuration);
                services.AddControllers().AddApplicationPart(typeof(Program).Assembly); // the test host is not the API entry assembly
                services.Configure<PqcCryptographyOptions>(o => o.MasterKeyBase64 = Convert.ToBase64String(new byte[32]));
            }).Configure(_ => { }))
            .Build();
    }

    /// <summary>The controllers MVC maps in the installed runtime, as its own feature providers decide.</summary>
    public static IReadOnlyList<Type> MappedControllers(IServiceProvider services)
    {
        var feature = new ControllerFeature();
        services.GetRequiredService<ApplicationPartManager>().PopulateFeature(feature);
        return feature.Controllers.Select(c => c.AsType()).OrderBy(t => t.FullName).ToList();
    }

    /// <summary>
    /// Building the runtime scans every referenced assembly (MediatR's open-generic registration does
    /// this). A machine with an OS application-control policy (Windows WDAC / Smart App Control) can
    /// refuse to load one of the cloud SDKs, and the failure then arrives as a
    /// ReflectionTypeLoadException whose message says nothing about wiring — which is how a developer
    /// learns to ignore a red suite. xunit v2 cannot skip at runtime, so the failure is kept but
    /// re-thrown with a message naming the cause and the remedy. Internal audit finding A-01, 2026-09-22.
    /// </summary>
    private static IHost BuildInstalledRuntimeHostOrExplain()
    {
        try { return BuildInstalledRuntimeHost(); }
        catch (Exception exception) when (Blocked(exception) is { Length: > 0 } blocked)
        {
            throw new InvalidOperationException(
                "This test could not run on this machine: the operating system's application-control policy "
                + "(WDAC / Smart App Control) refused to load a referenced assembly, so the runtime container cannot be built. "
                + "This is NOT a dependency-injection failure and says nothing about the product. Run the suite on a machine "
                + "without that policy, or rely on CI, which runs this test on every push. Blocked: "
                + string.Join(" | ", blocked), exception);
        }
    }

    private static string[] Blocked(Exception exception)
    {
        var load = exception as ReflectionTypeLoadException
            ?? exception.InnerException as ReflectionTypeLoadException;
        return load?.LoaderExceptions
            .Select(inner => inner?.Message)
            .Where(message => message?.Contains("Application Control policy", StringComparison.OrdinalIgnoreCase) == true)
            .Select(message => message!)
            .Distinct()
            .ToArray() ?? [];
    }

    [Fact]
    public void Every_Mapped_Controller_Resolves_From_The_Installed_Runtime_Container()
    {
        using var host = BuildInstalledRuntimeHostOrExplain();
        using var scope = host.Services.CreateScope();
        var controllers = MappedControllers(host.Services);
        var failures = new List<string>();
        foreach (var controller in controllers)
        {
            var constructor = controller.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .OrderByDescending(c => c.GetParameters().Length).First();
            foreach (var parameter in constructor.GetParameters())
                Check(scope.ServiceProvider, failures, $"{controller.Name}.ctor", parameter);
            foreach (var action in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                foreach (var parameter in action.GetParameters().Where(p => p.GetCustomAttribute<FromServicesAttribute>() is not null))
                    Check(scope.ServiceProvider, failures, $"{controller.Name}.{action.Name}", parameter);
        }
        Assert.True(controllers.Count > 50, $"Only {controllers.Count} controllers were discovered.");
        Assert.True(failures.Count == 0, $"{failures.Count} controller dependencies cannot be resolved:\n  " + string.Join("\n  ", failures));
    }

    [Fact]
    public void The_Installation_Wizard_Is_Not_Mapped_Once_Installed()
    {
        using var host = BuildInstalledRuntimeHostOrExplain();
        var controllers = MappedControllers(host.Services);
        Assert.DoesNotContain(controllers, c => c.Name == "WizardController");
        Assert.Contains(controllers, c => c.Name == "AuthController");
    }

    private static void Check(IServiceProvider services, List<string> failures, string site, ParameterInfo parameter)
    {
        if (parameter.HasDefaultValue) return;
        object? resolved;
        try { resolved = services.GetService(parameter.ParameterType); }
        catch (Exception ex) { failures.Add($"{site}({parameter.ParameterType.Name} {parameter.Name}): {ex.GetBaseException().Message}"); return; }
        if (resolved is null) failures.Add($"{site}({parameter.ParameterType.FullName} {parameter.Name}) is not registered");
    }
}
