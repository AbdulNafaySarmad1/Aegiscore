using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AegisCore.API.Finance;
using AegisCore.API.Middleware;
using AegisCore.Contracts.Abstractions.Cloud;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Cloud.R2;
using AegisCore.Infrastructure.Finance;
using AegisCore.Infrastructure.Finance.Gateways;
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

public sealed class FinancePaymentEndpointTests
{
    [Fact]
    public async Task RequestToPay_Rejects_A_Different_Student()
    {
        await using var host = await PaymentHost.CreateAsync();

        using var response = await host.PostAsync(host.StudentB, host.TenantA, host.DemandA, "PKR");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Null(host.Gateway.LastRequest);
    }

    [Fact]
    public async Task RequestToPay_Rejects_NonFinance_User_Without_Student_Identity()
    {
        await using var host = await PaymentHost.CreateAsync();

        using var response = await host.PostAsync(null, host.TenantA, host.DemandA, "PKR", "Faculty");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Null(host.Gateway.LastRequest);
    }

    [Fact]
    public async Task RequestToPay_Uses_Registered_Alias_And_Persists_Sanitized_Event()
    {
        await using var host = await PaymentHost.CreateAsync();

        using var response = await host.PostAsync(host.StudentA, host.TenantA, host.DemandA, "PKR", payerReference: "attacker-controlled");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("+923001234567", host.Gateway.LastRequest?.PayerReference);
        await using var scope = host.App.Services.CreateAsyncScope();
        var events = await scope.ServiceProvider.GetRequiredService<AegisDbContext>().PaymentGatewayEvents.IgnoreQueryFilters().ToListAsync();
        var paymentEvent = Assert.Single(events);
        Assert.Equal(host.TenantA, paymentEvent.TenantId);
        Assert.Equal("RTP_SENT", paymentEvent.EventType);
        Assert.DoesNotContain("+923001234567", paymentEvent.SanitizedPayloadJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RequestToPay_Does_Not_Reveal_CrossTenant_Demand()
    {
        await using var host = await PaymentHost.CreateAsync();

        using var response = await host.PostAsync(host.StudentA, host.TenantB, host.DemandA, "PKR");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Null(host.Gateway.LastRequest);
    }

    private sealed class PaymentHost(WebApplication app, SqliteConnection connection, CapturingGateway gateway) : IAsyncDisposable
    {
        public WebApplication App { get; } = app;
        public CapturingGateway Gateway { get; } = gateway;
        public Guid TenantA { get; } = Guid.NewGuid();
        public Guid TenantB { get; } = Guid.NewGuid();
        public Guid StudentA { get; } = Guid.NewGuid();
        public Guid StudentB { get; } = Guid.NewGuid();
        public Guid DemandA { get; } = Guid.NewGuid();

        public static async Task<PaymentHost> CreateAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();
            var gateway = new CapturingGateway();
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
            builder.WebHost.UseTestServer();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddDbContext<AegisDbContext>((services, options) => options.UseSqlite(connection));
            builder.Services.AddAuthentication("test").AddScheme<AuthenticationSchemeOptions, HeaderAuthenticationHandler>("test", _ => { });
            builder.Services.AddAuthorization();
            builder.Services.AddFinanceModule();
            builder.Services.RemoveAll<IHostedService>();
            builder.Services.RemoveAll<IPaymentGatewayAdapter>();
            builder.Services.RemoveAll<SandboxPaymentGatewayAdapter>();
            builder.Services.RemoveAll<PaymentGatewayRouter>();
            builder.Services.AddSingleton(Mock.Of<ISecretService>());
            builder.Services.AddSingleton(Mock.Of<IPqcSignatureService>());
            builder.Services.AddSingleton(Mock.Of<IR2StorageService>());
            builder.Services.AddSingleton<IPaymentGatewayAdapter>(gateway);
            builder.Services.AddSingleton<SandboxPaymentGatewayAdapter>();
            builder.Services.AddScoped<PaymentGatewayRouter>();

            var app = builder.Build();
            app.UseAuthentication();
            app.UseMiddleware<TenantContextMiddleware>();
            app.UseAuthorization();
            app.MapFinanceModuleEndpoints();
            await app.StartAsync();

            var host = new PaymentHost(app, connection, gateway);
            await host.SeedAsync();
            return host;
        }

        public async Task<HttpResponseMessage> PostAsync(Guid? studentId, Guid tenantId, Guid demandId, string currency, string role = "Student", string payerReference = "registered")
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/finance/v2/payments/raast/request-to-pay")
            {
                Content = JsonContent.Create(new { feeDemandId = demandId, feeInstallmentId = (Guid?)null, currency, payerReference })
            };
            request.Headers.Add("X-Test-Tenant", tenantId.ToString("D"));
            request.Headers.Add("X-Test-Role", role);
            if (studentId.HasValue) request.Headers.Add("X-Test-Student", studentId.Value.ToString("D"));
            return await App.GetTestClient().SendAsync(request);
        }

        private async Task SeedAsync()
        {
            await using var scope = App.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AegisDbContext>();
            await db.Database.EnsureCreatedAsync();
            db.Licenses.Add(new License { Id = Guid.NewGuid(), TenantId = TenantA, CustomerName = "Test", Edition = LicenseEdition.Professional, Status = LicenseStatus.Active, IssuedAt = DateTime.UtcNow });
            db.PaymentGatewayConfigurations.Add(new PaymentGatewayConfiguration { Id = Guid.NewGuid(), TenantId = TenantA, GatewaySlug = "raast", Environment = "production", SecretNamespace = "/test", IsActive = true, HealthStatus = "ok", ConfiguredByUserId = Guid.NewGuid() });
            db.StudentProfiles.Add(new StudentProfile { Id = Guid.NewGuid(), TenantId = TenantA, StudentId = StudentA, ProfileJson = "{\"registeredMobile\":\"+923001234567\"}" });
            db.FeeDemands.Add(new FeeDemand { Id = DemandA, TenantId = TenantA, StudentId = StudentA, DemandNumber = "FD-1", Currency = "PKR", TotalAmount = 1000m, OutstandingAmount = 1000m, DueDate = DateTime.UtcNow.AddDays(7) });
            await db.SaveChangesAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await App.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    private sealed class CapturingGateway : IPaymentGatewayAdapter
    {
        public string Slug => "raast";
        public GatewayRtpRequest? LastRequest { get; private set; }

        public Task<GatewayRtpResult> SendRtpAsync(Guid tenantId, string secretNamespace, GatewayRtpRequest request, CancellationToken ct)
        {
            LastRequest = request;
            return Task.FromResult(new GatewayRtpResult("TEST-RTP", DateTime.UtcNow.AddMinutes(30), null, false, "{\"status\":\"pending\"}"));
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
            var student = Request.Headers["X-Test-Student"].ToString();
            if (!string.IsNullOrWhiteSpace(student)) claims.Add(new Claim("student_id", student));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name)), Scheme.Name)));
        }
    }
}
