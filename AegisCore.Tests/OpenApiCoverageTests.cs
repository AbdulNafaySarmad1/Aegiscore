using AegisCore.API.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Swashbuckle.AspNetCore.Swagger;

namespace AegisCore.Tests;

/// <summary>
/// The OpenAPI document is what every route sweep and every API client reads. Minimal-API endpoint
/// groups (finance v2, board, achievements, ...) are described only when the endpoint API explorer
/// is registered; without it 244 endpoints were invisible to the document and to the sweeps.
/// </summary>
public sealed class OpenApiCoverageTests
{
    [Fact]
    public void Minimal_Api_Endpoints_Appear_In_The_OpenApi_Document()
    {
        using var host = new HostBuilder()
            .ConfigureAppConfiguration(c => c.AddInMemoryCollection(new Dictionary<string, string?> { ["ASPNETCORE_ENVIRONMENT"] = "Development" }))
            .ConfigureWebHost(web => web.UseTestServer()
                .ConfigureServices((context, services) => services.AddCoreServices(context.Configuration))
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/api/probe/minimal", () => Results.Ok()).RequireAuthorization();
                        var group = endpoints.MapGroup("/api/probe/v2").WithTags("Probe");
                        group.MapGet("/dashboard", (HttpContext http) => Results.Ok());
                    });
                }))
            .Build();
        host.Start();
        var document = host.Services.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");
        Assert.Contains("/api/probe/minimal", document.Paths.Keys);
        Assert.Contains("/api/probe/v2/dashboard", document.Paths.Keys);
    }
}
