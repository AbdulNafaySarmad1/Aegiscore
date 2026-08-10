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
    public async Task Student_Department_Claim_Cannot_Read_Department_Finance()
    {
        await using var host = await DashboardHost.CreateAsync();
        using var response = await host.GetDepartmentAsync(host.TenantA, host.DepartmentA, "Student", host.DepartmentA, studentDerived: true);
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

        public async Task<HttpResponseMessage> GetDepartmentAsync(
            Guid tenantId,
            Guid departmentId,
            string role,
            Guid? claimedDepartment,
            bool studentDerived = false)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/finance/v2/dashboard/department/{departmentId:D}");
            request.Headers.Add("X-Test-Tenant", tenantId.ToString("D"));
            request.Headers.Add("X-Test-Role", role);
            if (claimedDepartment.HasValue)
                request.Headers.Add(studentDerived ? "X-Test-Student-Department" : "X-Test-Department", claimedDepartment.Value.ToString("D"));
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
            if (!string.IsNullOrWhiteSpace(department)) claims.Add(new Claim("staff_department_id", department));
            var studentDepartment = Request.Headers["X-Test-Student-Department"].ToString();
            if (!string.IsNullOrWhiteSpace(studentDepartment)) claims.Add(new Claim("department_id", studentDepartment));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name)), Scheme.Name)));
        }
    }
}
