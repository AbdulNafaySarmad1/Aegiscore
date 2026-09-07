using System.Net;
using System.Text;
using AegisCore.API.Background;
using AegisCore.API.Middleware;
using AegisCore.Application.Licensing;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Licensing;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace AegisCore.Tests;

public sealed class ClientLicenseTests
{
    [Theory]
    [InlineData("invalid")]
    [InlineData("expired")]
    [InlineData("revoked")]
    public async Task Authority_denial_locks_immediately_and_valid_response_restores_tier(string denied)
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AegisDbContext>().UseSqlite(connection).Options;
        await using var db = new AegisDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var tenant = Guid.NewGuid();
        db.LicenseStates.Add(new LicenseState { Id = Guid.NewGuid(), TenantId = tenant, UniversityId = tenant, UniversityName = "Client", LicenseKey = "signed-key" });
        db.LicenseStates.Add(new LicenseState { Id = Guid.NewGuid(), TenantId = Guid.NewGuid(), Status = "active", UniversityName = "Unrelated", CreatedAt = DateTimeOffset.UtcNow.AddDays(1) });
        await db.SaveChangesAsync();
        var body = $$"""{"status":"{{denied}}","message":"denied"}""";
        using var client = new HttpClient(new Handler(() => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") }));
        var factory = new Mock<IHttpClientFactory>(); factory.Setup(f => f.CreateClient("license")).Returns(client);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["TENANT_ID"] = tenant.ToString(), ["LICENSE_SERVER_URL"] = "https://authority.example/heartbeat" }).Build();
        var service = new LicenseService(db, configuration, factory.Object, null!, NullLogger<LicenseService>.Instance);
        Assert.False((await service.SendHeartbeatAsync()).Success);
        Assert.True((await service.GetStatusAsync()).IsLocked);
        body = """{"status":"valid","message":"renewed","tier":"Premium"}""";
        Assert.True((await service.SendHeartbeatAsync()).Success);
        var status = await service.GetStatusAsync();
        Assert.False(status.IsLocked); Assert.Equal("active", status.Status); Assert.Equal(LicenseTier.Premium, status.Tier);
        body = """{"status":"valid","message":"bad tier","tier":"Unlimited"}""";
        Assert.False((await service.SendHeartbeatAsync()).Success);
        Assert.True((await service.GetStatusAsync()).IsLocked);
    }

    [Theory]
    [InlineData("GET", "/api/students", true, LicenseTier.Basic)]
    [InlineData("POST", "/api/students", false, LicenseTier.Basic)]
    [InlineData("PUT", "/api/examination/marks", false, LicenseTier.Standard)]
    [InlineData("GET", "/api/proctoring/sessions", false, LicenseTier.Standard)]
    [InlineData("GET", "/api/proctoring/sessions", true, LicenseTier.Premium)]
    [InlineData("POST", "/api/auth/login", true, LicenseTier.Basic)]
    [InlineData("POST", "/api/wizard/step/4/save", false, LicenseTier.Basic)]
    public async Task Lockdown_keeps_reads_and_recovery_but_blocks_writes_and_premium_modules(string method, string path, bool allowed, LicenseTier tier)
    {
        var called = false;
        var middleware = new LicenseLockoutMiddleware(_ => { called = true; return Task.CompletedTask; });
        var licenses = new Mock<ILicenseService>();
        licenses.Setup(s => s.GetStatusAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new LicenseRuntimeStatus("expired", true, "expired", null, null, 0, null, tier));
        var context = new DefaultHttpContext(); context.Request.Method = method; context.Request.Path = path; context.Response.Body = new MemoryStream();
        await middleware.InvokeAsync(context, licenses.Object, new ConfigurationBuilder().Build(), Mock.Of<IHostEnvironment>());
        Assert.Equal(allowed, called);
        if (!allowed) Assert.Equal(403, context.Response.StatusCode);
    }

    [Fact]
    public async Task Heartbeat_runs_on_service_start()
    {
        var invoked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var licenses = new Mock<ILicenseService>();
        licenses.Setup(s => s.SendHeartbeatAsync(It.IsAny<CancellationToken>())).Returns(() => { invoked.TrySetResult(); return Task.FromResult(new HeartbeatResult(true, "valid", "OK")); });
        await using var provider = new ServiceCollection().AddScoped(_ => licenses.Object).BuildServiceProvider();
        using var worker = new LicenseHeartbeatHostedService(provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<LicenseHeartbeatHostedService>.Instance);
        await worker.StartAsync(CancellationToken.None);
        await invoked.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await worker.StopAsync(CancellationToken.None);
        licenses.Verify(s => s.SendHeartbeatAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Persistence_guard_blocks_background_writes_without_deleting_data()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:"); await connection.OpenAsync();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["AEGISCORE_INSTALLATION_COMPLETED"] = "true" }).Build();
        var guard = new LicenseWriteInterceptor(configuration, Mock.Of<IHostEnvironment>(e => e.ContentRootPath == "/tmp"), new HttpContextAccessor());
        var options = new DbContextOptionsBuilder<AegisDbContext>().UseSqlite(connection).AddInterceptors(guard).Options;
        await using var db = new AegisDbContext(options); await db.Database.EnsureCreatedAsync();
        db.LicenseStates.Add(new LicenseState { Status = "expired", UniversityName = "Client" }); await db.SaveChangesAsync();
        db.Students.Add(new Student { FirstName = "Test", LastName = "Learner" });
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        Assert.Throws<InvalidOperationException>(() => db.SaveChanges());
        Assert.Equal(0, await db.Students.CountAsync());
        Assert.Equal(1, await db.LicenseStates.CountAsync());
    }

    private sealed class Handler(Func<HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(response());
    }
}
