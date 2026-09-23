using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using AegisCore.API.Controllers;
using AegisCore.API.Middleware;
using AegisCore.Application.Finance;
using AegisCore.Contracts.Abstractions.Cloud;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Cloud.R2;
using AegisCore.Infrastructure.Finance;
using AegisCore.Infrastructure.Finance.Gateways.PayFast;
using AegisCore.Infrastructure.Security.Pqc;
using AegisCore.Tests.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace AegisCore.Tests;

/// <summary>
/// PayFast hosted checkout, driven through the real HTTP routes (the anonymous IPN included). Only
/// PayFast's own HTTP calls are faked — there are no sandbox credentials in this environment — so
/// these prove AegisCore's side of the contract, not PayFast's. See docs/integrations/payfast.md.
/// </summary>
public sealed class PayFastCheckoutTests
{
    private const string MerchantId = "102";
    private const string SecuredKey = "sk-test-DO-NOT-LOG-7f3a";
    private const string AccessToken = "tok-test-DO-NOT-LOG-91c2";

    [Theory, DatabaseProviders]
    public async Task Begin_Marks_Demand_Pending_And_Returns_A_Hosted_Form_With_No_Card_Fields(bool postgres)
    {
        await using var host = await Host.CreateAsync(postgres);

        var start = await host.BeginAsync(host.StudentA, host.DemandA);

        Assert.Equal("https://ipguat.apps.net.pk/Ecommerce/api/Transaction/PostTransaction", start.GetProperty("actionUrl").GetString());
        var fields = start.GetProperty("fields").EnumerateObject().ToDictionary(x => x.Name, x => x.Value.GetString());
        var basket = start.GetProperty("basketId").GetString()!;
        Assert.Equal(basket, fields["BASKET_ID"]);
        Assert.StartsWith("FD-1-", basket);
        Assert.Equal("1000.00", fields["TXNAMT"]);
        Assert.Equal("PKR", fields["CURRENCY_CODE"]);
        Assert.Equal(AccessToken, fields["TOKEN"]);
        Assert.Equal($"https://portal.test/api/payments/payfast/{host.TenantA:D}/ipn", fields["CHECKOUT_URL"]);
        Assert.DoesNotContain("SECURED_KEY", fields.Keys);
        Assert.DoesNotContain(fields.Keys, k => k.Contains("CARD", StringComparison.OrdinalIgnoreCase) || k.Contains("CVV", StringComparison.OrdinalIgnoreCase) || k.Contains("PAN", StringComparison.OrdinalIgnoreCase) || k.Contains("EXPIR", StringComparison.OrdinalIgnoreCase));
        Assert.Equal((basket, 1000m), host.PayFast.LastTokenRequest);

        var (demand, session) = await host.StateAsync(basket);
        Assert.Equal("PAYMENT_PENDING", demand.Status);
        Assert.Equal("PENDING", session.Status);
        Assert.Equal("OVERDUE", session.PreviousDemandStatus);
        Assert.Equal(0m, demand.PaidAmount);
    }

    [Theory, DatabaseProviders]
    public async Task Begin_Refuses_Another_Students_Demand(bool postgres)
    {
        await using var host = await Host.CreateAsync(postgres);

        using var response = await host.SendAsync(HttpMethod.Post, "/api/payments/payfast/checkout", host.StudentB, new { feeDemandId = host.DemandA });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Null(host.PayFast.LastTokenRequest);
    }

    [Theory, DatabaseProviders]
    public async Task Verified_Ipn_Marks_The_Demand_Paid_With_PayFasts_Transaction_Reference(bool postgres)
    {
        await using var host = await Host.CreateAsync(postgres);
        var basket = (await host.BeginAsync(host.StudentA, host.DemandA)).GetProperty("basketId").GetString()!;
        host.PayFast.Record("TXN-5001", basket, "00", 1000m);

        using var response = await host.IpnAsync(basket, "000", "TXN-5001");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var (demand, session) = await host.StateAsync(basket);
        Assert.Equal("PAID", demand.Status);
        Assert.Equal(1000m, demand.PaidAmount);
        Assert.Equal(0m, demand.OutstandingAmount);
        Assert.Equal("PAID", session.Status);
        Assert.Equal("TXN-5001", session.GatewayTransactionId);
        var payment = Assert.Single(await host.PaymentsAsync());
        Assert.Equal("TXN-5001", payment.ExternalReference);
        Assert.Equal("payfast", payment.GatewaySlug);
        Assert.Equal("SETTLED", payment.Status);
        Assert.Equal(payment.Id, session.FinancePaymentId);
    }

    [Theory, DatabaseProviders]
    public async Task Replayed_Ipn_And_The_Students_Return_Record_The_Payment_Once(bool postgres)
    {
        await using var host = await Host.CreateAsync(postgres);
        var basket = (await host.BeginAsync(host.StudentA, host.DemandA)).GetProperty("basketId").GetString()!;
        host.PayFast.Record("TXN-5002", basket, "00", 1000m);

        (await host.IpnAsync(basket, "000", "TXN-5002")).Dispose();
        (await host.IpnAsync(basket, "000", "TXN-5002")).Dispose();
        using var returned = await host.SendAsync(HttpMethod.Post, $"/api/payments/payfast/checkout/{basket}/confirm", host.StudentA, host.PayFastParameters(basket, "000", "TXN-5002"));

        Assert.Equal(HttpStatusCode.OK, returned.StatusCode);
        var body = await returned.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("AlreadyFinal", body.GetProperty("verification").GetString());
        Assert.Equal("PAID", body.GetProperty("checkout").GetProperty("status").GetString());
        Assert.Single(await host.PaymentsAsync());
        var (demand, _) = await host.StateAsync(basket);
        Assert.Equal(1000m, demand.PaidAmount);
    }

    [Theory, DatabaseProviders]
    public async Task Return_Alone_Settles_The_Payment_When_The_Ipn_Never_Arrives(bool postgres)
    {
        await using var host = await Host.CreateAsync(postgres);
        var basket = (await host.BeginAsync(host.StudentA, host.DemandA)).GetProperty("basketId").GetString()!;
        host.PayFast.Record("TXN-5003", basket, "00", 1000m);

        using var returned = await host.SendAsync(HttpMethod.Post, $"/api/payments/payfast/checkout/{basket}/confirm", host.StudentA, host.PayFastParameters(basket, "00", "TXN-5003"));

        var body = await returned.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Paid", body.GetProperty("verification").GetString());
        Assert.Equal("PAID", body.GetProperty("checkout").GetProperty("demandStatus").GetString());
    }

    [Theory, DatabaseProviders]
    public async Task Forged_Ipn_With_A_Wrong_Hash_Changes_Nothing(bool postgres)
    {
        await using var host = await Host.CreateAsync(postgres);
        var basket = (await host.BeginAsync(host.StudentA, host.DemandA)).GetProperty("basketId").GetString()!;
        host.PayFast.Record("TXN-5004", basket, "00", 1000m);
        var forged = host.PayFastParameters(basket, "000", "TXN-5004");
        forged["validation_hash"] = PayFastCheckoutService.ValidationHash(new PayFastCredentials(MerchantId, "a-guessed-key", "x", true), basket, "000");

        using var response = await host.IpnAsync(forged);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, host.PayFast.LookupCount);
        Assert.Empty(await host.PaymentsAsync());
        var (demand, session) = await host.StateAsync(basket);
        Assert.Equal("PAYMENT_PENDING", demand.Status);
        Assert.Equal("PENDING", session.Status);
    }

    [Theory, DatabaseProviders]
    public async Task Ipn_Naming_A_Transaction_From_Another_Basket_Is_Rejected(bool postgres)
    {
        await using var host = await Host.CreateAsync(postgres);
        var basket = (await host.BeginAsync(host.StudentA, host.DemandA)).GetProperty("basketId").GetString()!;
        // A genuine, cheap transaction the attacker paid for on some other basket.
        host.PayFast.Record("TXN-OTHER", "FD-OTHER-000000", "00", 1m);

        using var response = await host.IpnAsync(basket, "000", "TXN-OTHER");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await host.PaymentsAsync());
        Assert.Equal("PENDING", (await host.StateAsync(basket)).Session.Status);
    }

    [Theory, DatabaseProviders]
    public async Task Success_For_The_Wrong_Amount_Goes_To_Finance_Review_Not_Paid(bool postgres)
    {
        await using var host = await Host.CreateAsync(postgres);
        var basket = (await host.BeginAsync(host.StudentA, host.DemandA)).GetProperty("basketId").GetString()!;
        host.PayFast.Record("TXN-5005", basket, "00", 10m);

        (await host.IpnAsync(basket, "000", "TXN-5005")).Dispose();

        Assert.Empty(await host.PaymentsAsync());
        var (demand, session) = await host.StateAsync(basket);
        Assert.Equal("NEEDS_REVIEW", session.Status);
        Assert.Equal("OVERDUE", demand.Status);
        Assert.Equal(1000m, demand.OutstandingAmount);
    }

    [Theory, DatabaseProviders]
    public async Task A_Verified_Failure_Puts_The_Demand_Back_Exactly_As_It_Was(bool postgres)
    {
        await using var host = await Host.CreateAsync(postgres);
        var basket = (await host.BeginAsync(host.StudentA, host.DemandA)).GetProperty("basketId").GetString()!;

        var parameters = host.PayFastParameters(basket, "97", null);
        parameters["err_msg"] = "Transaction declined by issuer";
        using var response = await host.IpnAsync(parameters);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var (demand, session) = await host.StateAsync(basket);
        Assert.Equal("OVERDUE", demand.Status);
        Assert.Equal("FAILED", session.Status);
        Assert.Equal("97", session.GatewayErrorCode);
        Assert.Equal("Transaction declined by issuer", session.GatewayMessage);
        Assert.Empty(await host.PaymentsAsync());

        // The student can simply try again.
        var retry = await host.BeginAsync(host.StudentA, host.DemandA);
        Assert.NotEqual(basket, retry.GetProperty("basketId").GetString());
    }

    [Theory, DatabaseProviders]
    public async Task Cancelling_Restores_The_Demand_But_A_Late_Verified_Payment_Is_Still_Honoured(bool postgres)
    {
        await using var host = await Host.CreateAsync(postgres);
        var basket = (await host.BeginAsync(host.StudentA, host.DemandA)).GetProperty("basketId").GetString()!;

        using var cancelled = await host.SendAsync(HttpMethod.Post, $"/api/payments/payfast/checkout/{basket}/cancel", host.StudentA, null);
        Assert.Equal(HttpStatusCode.OK, cancelled.StatusCode);
        Assert.Equal("OVERDUE", (await host.StateAsync(basket)).Demand.Status);

        host.PayFast.Record("TXN-5006", basket, "00", 1000m);
        (await host.IpnAsync(basket, "000", "TXN-5006")).Dispose();

        var (demand, session) = await host.StateAsync(basket);
        Assert.Equal("PAID", session.Status);
        Assert.Equal("PAID", demand.Status);
        Assert.Single(await host.PaymentsAsync());
    }

    [Theory, DatabaseProviders]
    public async Task Paying_Twice_From_Two_Tabs_Applies_One_Payment_And_Flags_The_Other_For_Review(bool postgres)
    {
        await using var host = await Host.CreateAsync(postgres);
        var first = (await host.BeginAsync(host.StudentA, host.DemandA)).GetProperty("basketId").GetString()!;
        var second = (await host.BeginAsync(host.StudentA, host.DemandA)).GetProperty("basketId").GetString()!;
        Assert.Equal("SUPERSEDED", (await host.StateAsync(first)).Session.Status);
        Assert.Equal("OVERDUE", (await host.StateAsync(second)).Session.PreviousDemandStatus);
        host.PayFast.Record("TXN-6001", second, "00", 1000m);
        host.PayFast.Record("TXN-6002", first, "00", 1000m);

        (await host.IpnAsync(second, "000", "TXN-6001")).Dispose();
        (await host.IpnAsync(first, "000", "TXN-6002")).Dispose();

        var payment = Assert.Single(await host.PaymentsAsync());
        Assert.Equal("TXN-6001", payment.ExternalReference);
        var (demand, late) = await host.StateAsync(first);
        Assert.Equal("NEEDS_REVIEW", late.Status);
        Assert.Equal("TXN-6002", late.GatewayTransactionId);
        Assert.Equal("PAID", demand.Status);
        Assert.Equal(1000m, demand.PaidAmount);
    }

    [Theory, DatabaseProviders]
    public async Task An_Abandoned_Attempt_Expires_And_Releases_The_Demand(bool postgres)
    {
        await using var host = await Host.CreateAsync(postgres);
        var basket = (await host.BeginAsync(host.StudentA, host.DemandA)).GetProperty("basketId").GetString()!;

        var closed = await host.RunAsTenantAsync(checkout => checkout.ExpireStaleAsync(host.TenantA, DateTime.UtcNow.AddHours(1), CancellationToken.None));

        Assert.Equal(1, closed);
        var (demand, session) = await host.StateAsync(basket);
        Assert.Equal("EXPIRED", session.Status);
        Assert.Equal("OVERDUE", demand.Status);
    }

    [Theory, DatabaseProviders]
    public async Task When_PayFasts_Status_Api_Is_Down_The_Ipn_Asks_For_A_Retry_And_The_Sweep_Settles_It_Later(bool postgres)
    {
        await using var host = await Host.CreateAsync(postgres);
        var basket = (await host.BeginAsync(host.StudentA, host.DemandA)).GetProperty("basketId").GetString()!;

        using var response = await host.IpnAsync(basket, "000", "TXN-5007");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var (_, pending) = await host.StateAsync(basket);
        Assert.Equal("PENDING", pending.Status);
        Assert.Equal("TXN-5007", pending.GatewayTransactionId);

        host.PayFast.Record("TXN-5007", basket, "00", 1000m);
        await host.RunAsTenantAsync(checkout => checkout.ExpireStaleAsync(host.TenantA, DateTime.UtcNow.AddHours(1), CancellationToken.None));

        var (demand, session) = await host.StateAsync(basket);
        Assert.Equal("PAID", session.Status);
        Assert.Equal("PAID", demand.Status);
    }

    [Theory, DatabaseProviders]
    public async Task A_Student_Cannot_See_Or_Settle_Another_Students_Attempt(bool postgres)
    {
        await using var host = await Host.CreateAsync(postgres);
        var basket = (await host.BeginAsync(host.StudentA, host.DemandA)).GetProperty("basketId").GetString()!;
        host.PayFast.Record("TXN-5008", basket, "00", 1000m);

        using var peek = await host.SendAsync(HttpMethod.Get, $"/api/payments/payfast/checkout/{basket}", host.StudentB, null);
        using var cancel = await host.SendAsync(HttpMethod.Post, $"/api/payments/payfast/checkout/{basket}/cancel", host.StudentB, null);

        Assert.Equal(HttpStatusCode.NotFound, peek.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, cancel.StatusCode);
        Assert.Equal("PENDING", (await host.StateAsync(basket)).Session.Status);
    }

    [Theory, DatabaseProviders]
    public async Task No_Secret_Or_Token_Reaches_The_Logs_Across_A_Whole_Payment(bool postgres)
    {
        await using var host = await Host.CreateAsync(postgres);
        var basket = (await host.BeginAsync(host.StudentA, host.DemandA)).GetProperty("basketId").GetString()!;
        host.PayFast.Record("TXN-5009", basket, "00", 1000m);
        var parameters = host.PayFastParameters(basket, "000", "TXN-5009");
        (await host.IpnAsync(parameters)).Dispose();
        var forged = new Dictionary<string, string>(parameters) { ["validation_hash"] = new string('0', 64) };
        (await host.IpnAsync(forged)).Dispose();

        var lines = host.Logs.ToArray();
        Assert.NotEmpty(lines);
        Assert.Contains(lines, l => l.Contains(basket, StringComparison.Ordinal));
        foreach (var line in lines)
        {
            Assert.DoesNotContain(SecuredKey, line, StringComparison.Ordinal);
            Assert.DoesNotContain(AccessToken, line, StringComparison.Ordinal);
            Assert.DoesNotContain(parameters["validation_hash"], line, StringComparison.Ordinal);
            Assert.DoesNotContain("+923001234567", line, StringComparison.Ordinal);
        }
        await using var scope = host.App.Services.CreateAsyncScope();
        var events = await scope.ServiceProvider.GetRequiredService<AegisDbContext>().PaymentGatewayEvents.IgnoreQueryFilters().ToListAsync();
        Assert.All(events, e => Assert.DoesNotContain(SecuredKey, e.SanitizedPayloadJson, StringComparison.Ordinal));
        Assert.All(events, e => Assert.DoesNotContain(AccessToken, e.SanitizedPayloadJson, StringComparison.Ordinal));
    }

    private sealed class Host(WebApplication app, TestDatabase database, FakePayFast payFast, ConcurrentQueue<string> logs) : IAsyncDisposable
    {
        public WebApplication App { get; } = app;
        public FakePayFast PayFast { get; } = payFast;
        public ConcurrentQueue<string> Logs { get; } = logs;
        public Guid TenantA { get; } = Guid.NewGuid();
        public Guid StudentA { get; } = Guid.NewGuid();
        public Guid StudentB { get; } = Guid.NewGuid();
        public Guid DemandA { get; } = Guid.NewGuid();

        public static async Task<Host> CreateAsync(bool postgres)
        {
            var database = await TestDatabase.CreateAsync(postgres);
            var payFast = new FakePayFast();
            var logs = new ConcurrentQueue<string>();
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
            builder.WebHost.UseTestServer();
            builder.Configuration["APP_URL"] = "https://portal.test";
            builder.Logging.ClearProviders();
            builder.Logging.AddProvider(new CapturingLoggerProvider(logs));
            builder.Logging.SetMinimumLevel(LogLevel.Information);
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddDbContext<AegisDbContext>((services, options) => database.Configure(options));
            builder.Services.AddAuthentication("test").AddScheme<AuthenticationSchemeOptions, HeaderAuthenticationHandler>("test", _ => { });
            builder.Services.AddAuthorization();
            builder.Services.AddControllers().AddApplicationPart(typeof(PayFastController).Assembly);
            builder.Services.AddFinanceModule();
            builder.Services.RemoveAll<IHostedService>();
            builder.Services.RemoveAll<IPayFastApi>();
            builder.Services.AddSingleton<IPayFastApi>(payFast);
            var secrets = new TestSecretService();
            builder.Services.AddSingleton<ISecretService>(secrets);
            builder.Services.AddSingleton(Mock.Of<IPqcSignatureService>());
            builder.Services.AddSingleton(Mock.Of<IR2StorageService>());
            builder.Services.AddSingleton(Mock.Of<IFinanceRealtimePublisher>());

            var app = builder.Build();
            app.UseAuthentication();
            app.UseMiddleware<TenantContextMiddleware>();
            app.UseAuthorization();
            app.MapControllers();
            await app.StartAsync();

            var host = new Host(app, database, payFast, logs);
            var ns = $"/tenants/{host.TenantA:D}/gateways/payfast";
            await secrets.SetSecretAsync($"{ns}/MERCHANT_ID", MerchantId);
            await secrets.SetSecretAsync($"{ns}/SECURED_KEY", SecuredKey);
            await secrets.SetSecretAsync($"{ns}/MERCHANT_NAME", "Test University");
            await host.SeedAsync(ns);
            return host;
        }

        public async Task<JsonElement> BeginAsync(Guid student, Guid demand)
        {
            using var response = await SendAsync(HttpMethod.Post, "/api/payments/payfast/checkout", student, new { feeDemandId = demand });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }

        public Dictionary<string, string> PayFastParameters(string basket, string errorCode, string? transactionId)
        {
            var values = new Dictionary<string, string>
            {
                ["basket_id"] = basket,
                ["err_code"] = errorCode,
                ["err_msg"] = errorCode.StartsWith("0") ? "Success" : "Failed",
                ["validation_hash"] = PayFastCheckoutService.ValidationHash(new PayFastCredentials(MerchantId, SecuredKey, "Test University", true), basket, errorCode),
                ["order_date"] = "2026-09-23 10:00:00"
            };
            if (transactionId is not null) values["transaction_id"] = transactionId;
            return values;
        }

        public Task<HttpResponseMessage> IpnAsync(string basket, string errorCode, string? transactionId) => IpnAsync(PayFastParameters(basket, errorCode, transactionId));

        // PayFast sends the IPN detail in the query string.
        public Task<HttpResponseMessage> IpnAsync(IReadOnlyDictionary<string, string> parameters)
            => App.GetTestClient().GetAsync($"/api/payments/payfast/{TenantA:D}/ipn?{string.Join("&", parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"))}");

        public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, Guid student, object? body)
        {
            using var request = new HttpRequestMessage(method, path);
            if (body is not null) request.Content = JsonContent.Create(body);
            request.Headers.Add("X-Test-Tenant", TenantA.ToString("D"));
            request.Headers.Add("X-Test-Role", "Student");
            request.Headers.Add("X-Test-Student", student.ToString("D"));
            return await App.GetTestClient().SendAsync(request);
        }

        public async Task<(FeeDemand Demand, FeeCheckoutSession Session)> StateAsync(string basket)
        {
            await using var scope = App.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AegisDbContext>();
            var session = await db.FeeCheckoutSessions.IgnoreQueryFilters().AsNoTracking().SingleAsync(s => s.BasketId == basket);
            var demand = await db.FeeDemands.IgnoreQueryFilters().AsNoTracking().SingleAsync(d => d.Id == session.FeeDemandId);
            return (demand, session);
        }

        public async Task<List<FinancePayment>> PaymentsAsync()
        {
            await using var scope = App.Services.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<AegisDbContext>().FinancePayments.IgnoreQueryFilters().AsNoTracking().ToListAsync();
        }

        public async Task<T> RunAsTenantAsync<T>(Func<PayFastCheckoutService, Task<T>> action)
        {
            await using var scope = App.Services.CreateAsyncScope();
            var accessor = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
            accessor.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
            accessor.HttpContext.Items["TenantId"] = TenantA;
            try { return await action(scope.ServiceProvider.GetRequiredService<PayFastCheckoutService>()); }
            finally { accessor.HttpContext = null; }
        }

        private async Task SeedAsync(string ns)
        {
            await using var scope = App.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AegisDbContext>();
            await db.Database.EnsureCreatedAsync();
            db.Tenants.Add(new Tenant { Id = TenantA, TenantId = TenantA, Name = "Test University", ShortName = "TU", IsActive = true });
            db.Licenses.Add(new License { Id = Guid.NewGuid(), TenantId = TenantA, CustomerName = "Test", Edition = LicenseEdition.Professional, Status = LicenseStatus.Active, IssuedAt = DateTime.UtcNow });
            db.PaymentGatewayConfigurations.Add(new PaymentGatewayConfiguration { Id = Guid.NewGuid(), TenantId = TenantA, GatewaySlug = "payfast", Environment = "sandbox", SecretNamespace = ns, IsActive = true, HealthStatus = "ok", ConfiguredByUserId = Guid.NewGuid() });
            db.StudentProfiles.Add(new StudentProfile { Id = Guid.NewGuid(), TenantId = TenantA, StudentId = StudentA, ProfileJson = "{\"registeredMobile\":\"+923001234567\"}" });
            // OVERDUE, not OPEN, so the tests prove a failure restores the exact prior status.
            db.FeeDemands.Add(new FeeDemand { Id = DemandA, TenantId = TenantA, StudentId = StudentA, DemandNumber = "FD-1", Currency = "PKR", TotalAmount = 1000m, OutstandingAmount = 1000m, Status = "OVERDUE", DueDate = DateTime.UtcNow.AddDays(-3) });
            await db.SaveChangesAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await App.DisposeAsync();
            await database.DisposeAsync();
        }
    }

    /// <summary>Stands in for PayFast's three HTTP calls. Unknown transactions look like an outage (null).</summary>
    private sealed class FakePayFast : IPayFastApi
    {
        private readonly ConcurrentDictionary<string, PayFastTransactionRecord> _transactions = new();
        public (string Basket, decimal Amount)? LastTokenRequest { get; private set; }
        public int LookupCount;

        public void Record(string transactionId, string basket, string status, decimal amount) => _transactions[transactionId] = new PayFastTransactionRecord(basket, status, amount, transactionId);

        public string PostTransactionUrl(bool sandbox) => sandbox ? "https://ipguat.apps.net.pk/Ecommerce/api/Transaction/PostTransaction" : "https://ipg1.apps.net.pk/Ecommerce/api/Transaction/PostTransaction";

        public Task<string?> GetAccessTokenAsync(PayFastCredentials credentials, string basketId, decimal amount, string currency, CancellationToken ct)
        {
            Assert.Equal(SecuredKey, credentials.SecuredKey);
            LastTokenRequest = (basketId, amount);
            return Task.FromResult<string?>(AccessToken);
        }

        public Task<PayFastTransactionRecord?> GetTransactionAsync(PayFastCredentials credentials, string transactionId, CancellationToken ct)
        {
            Interlocked.Increment(ref LookupCount);
            return Task.FromResult(_transactions.TryGetValue(transactionId, out var record) ? record : null);
        }
    }

    private sealed class CapturingLoggerProvider(ConcurrentQueue<string> lines) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new Logger(lines, categoryName);
        public void Dispose() { }

        private sealed class Logger(ConcurrentQueue<string> lines, string category) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
                => lines.Enqueue($"{logLevel} {category}: {formatter(state, exception)} {exception}");
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
