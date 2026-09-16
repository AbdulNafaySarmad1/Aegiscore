using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AegisCore.API.Finance;
using AegisCore.API.Middleware;
using AegisCore.Contracts.Abstractions.Cloud;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Cloud.R2;
using AegisCore.Infrastructure.Finance;
using AegisCore.Infrastructure.Security.Pqc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AegisCore.Tests;

public sealed class FinanceDashboardEndpointTests
{
    [Fact]
    public async Task Department_Manager_Cannot_Read_Another_Department()
    {
        await using var host = await DashboardHost.CreateAsync();
        using var response = await host.GetDepartmentAsync(host.TenantA, host.DepartmentB, "HOD", host.DepartmentA);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Finance_User_Cannot_Enumerate_CrossTenant_Department()
    {
        await using var host = await DashboardHost.CreateAsync();
        using var response = await host.GetDepartmentAsync(host.TenantA, host.DepartmentB, "Finance Controller", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Department_Manager_Can_Read_Assigned_Department()
    {
        await using var host = await DashboardHost.CreateAsync();
        using var response = await host.GetDepartmentAsync(host.TenantA, host.DepartmentA, "HOD", host.DepartmentA);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Computer Science", body, StringComparison.Ordinal);
        Assert.DoesNotContain("Business", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Vice_Chancellor_Can_Read_Executive_Dashboard()
    {
        await using var host = await DashboardHost.CreateAsync();
        using var response = await host.SendAsync(HttpMethod.Get, "/api/finance/v2/dashboard/executive", host.TenantA, "VC");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("feeFunnel", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Vice_Chancellor_Read_Access_Does_Not_Extend_To_Finance_Actions()
    {
        await using var host = await DashboardHost.CreateAsync();
        // The executive view is read-only for the VC; the operations queue and every mutation stay behind CanManageFinance.
        using var operations = await host.SendAsync(HttpMethod.Get, "/api/finance/v2/dashboard/operations", host.TenantA, "VC");
        Assert.Equal(HttpStatusCode.Forbidden, operations.StatusCode);
        using var mutation = await host.SendAsync(HttpMethod.Post, "/api/finance/v2/accounting/reporting-entities", host.TenantA, "VC", "{}");
        Assert.Equal(HttpStatusCode.Forbidden, mutation.StatusCode);
    }

    [Fact]
    public async Task Department_Manager_Cannot_Read_Executive_Dashboard()
    {
        await using var host = await DashboardHost.CreateAsync();
        using var response = await host.SendAsync(HttpMethod.Get, "/api/finance/v2/dashboard/executive", host.TenantA, "HOD");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private sealed class DashboardHost(WebApplication app, SqliteConnection connection) : IAsyncDisposable
    {
        public Guid TenantA { get; } = Guid.NewGuid();
        public Guid TenantB { get; } = Guid.NewGuid();
        public Guid DepartmentA { get; } = Guid.NewGuid();
        public Guid DepartmentB { get; } = Guid.NewGuid();

        public static async Task<DashboardHost> CreateAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
            builder.WebHost.UseTestServer();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddDbContext<AegisDbContext>(options => options.UseSqlite(connection));
            builder.Services.AddAuthentication("test").AddScheme<AuthenticationSchemeOptions, HeaderAuthenticationHandler>("test", _ => { });
            builder.Services.AddAuthorization();
            builder.Services.AddFinanceModule();
            builder.Services.RemoveAll<IHostedService>();
            builder.Services.AddSingleton(Mock.Of<ISecretService>());
            builder.Services.AddSingleton(Mock.Of<IPqcSignatureService>());
            builder.Services.AddSingleton(Mock.Of<IR2StorageService>());
            builder.Services.AddSingleton(Mock.Of<AegisCore.Application.Finance.IFinanceRealtimePublisher>());
            var app = builder.Build();
            app.UseAuthentication();
            app.UseMiddleware<TenantContextMiddleware>();
            app.UseAuthorization();
            app.MapFinanceModuleEndpoints();
            await app.StartAsync();
            var host = new DashboardHost(app, connection);
            await host.SeedAsync();
            return host;
        }

        public async Task<HttpResponseMessage> GetDepartmentAsync(Guid tenantId, Guid departmentId, string role, Guid? claimedDepartment)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/finance/v2/dashboard/department/{departmentId:D}");
            request.Headers.Add("X-Test-Tenant", tenantId.ToString("D"));
            request.Headers.Add("X-Test-Role", role);
            if (claimedDepartment.HasValue) request.Headers.Add("X-Test-Department", claimedDepartment.Value.ToString("D"));
            return await app.GetTestClient().SendAsync(request);
        }

        public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, Guid tenantId, string role, string? json = null)
        {
            using var request = new HttpRequestMessage(method, path);
            request.Headers.Add("X-Test-Tenant", tenantId.ToString("D"));
            request.Headers.Add("X-Test-Role", role);
            if (json is not null) request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            return await app.GetTestClient().SendAsync(request);
        }

        private async Task SeedAsync()
        {
            await using var scope = app.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AegisDbContext>();
            await db.Database.EnsureCreatedAsync();
            db.Departments.AddRange(
                new Department { Id = DepartmentA, TenantId = TenantA, Name = "Computer Science", Code = "CS" },
                new Department { Id = DepartmentB, TenantId = TenantB, Name = "Business", Code = "BUS" });
            await db.SaveChangesAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await app.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    private sealed class HeaderAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var tenant = Request.Headers["X-Test-Tenant"].ToString();
            if (string.IsNullOrWhiteSpace(tenant)) return Task.FromResult(AuthenticateResult.NoResult());
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString("D")),
                new("tenant_id", tenant),
                new(ClaimTypes.Role, Request.Headers["X-Test-Role"].ToString())
            };
            var department = Request.Headers["X-Test-Department"].ToString();
            if (!string.IsNullOrWhiteSpace(department)) claims.Add(new Claim("department_id", department));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name)), Scheme.Name)));
        }
    }
}
