using System.Net.Http.Json;
using System.Text.Json;
using AegisCore.Contracts.Abstractions.Cloud;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Finance;

public sealed class VendorSanctionsService(AegisDbContext db, IHttpClientFactory clients, ISecretService secrets, IConfiguration configuration)
{
    public async Task<string> ScreenAsync(Guid tenantId, Guid vendorId, CancellationToken ct)
    {
        var vendor = await db.FinanceVendors.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == vendorId, ct) ?? throw new KeyNotFoundException("Vendor was not found.");
        var endpointText = configuration["Sanctions:ScreeningUrl"];
        if (!Uri.TryCreate(endpointText, UriKind.Absolute, out var endpoint) || endpoint.Scheme != Uri.UriSchemeHttps) throw new InvalidOperationException("Sanctions screening provider is not configured.");
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = JsonContent.Create(new { name = vendor.LegalName, taxId = vendor.TaxId, iban = vendor.Iban, swift = vendor.Swift, lists = new[] { "OFAC", "UN", "FATF" } }) };
        var keyName = configuration["Sanctions:ApiKeySecretName"];
        if (!string.IsNullOrWhiteSpace(keyName)) request.Headers.Authorization = new("Bearer", await secrets.GetSecretAsync(keyName, ct));
        using var response = await clients.CreateClient("sanctions").SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException("Sanctions provider rejected the screening request.", null, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var matched = document.RootElement.TryGetProperty("matched", out var matchProperty) && matchProperty.ValueKind == JsonValueKind.True;
        var reference = document.RootElement.TryGetProperty("reference", out var referenceProperty) ? referenceProperty.GetString() : null;
        vendor.SanctionsStatus = matched ? "BLOCKED" : "CLEAR";
        vendor.SanctionsReference = string.IsNullOrWhiteSpace(reference) ? "PROVIDER_SCREENING" : reference[..Math.Min(reference.Length, 500)];
        vendor.LastSanctionsScreenedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return vendor.SanctionsStatus;
    }
}

public sealed class VendorSanctionsRescreenWorker(IServiceScopeFactory scopes, ILogger<VendorSanctionsRescreenWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromDays(1));
        do
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AegisDbContext>();
                var screening = scope.ServiceProvider.GetRequiredService<VendorSanctionsService>();
                // Intentional cross-tenant discovery: only due vendor IDs and TenantIds are read, then the worker binds before screening.
                var due = await db.FinanceVendors.IgnoreQueryFilters().AsNoTracking().Where(x => x.Status == "ACTIVE" && (x.LastSanctionsScreenedAt == null || x.LastSanctionsScreenedAt < DateTime.UtcNow.AddDays(-90))).Select(x => new { x.TenantId, x.Id }).Take(100).ToListAsync(stoppingToken);
                var tenantContext = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
                foreach (var vendor in due)
                {
                    tenantContext.HttpContext = new DefaultHttpContext();
                    tenantContext.HttpContext.Items["TenantId"] = vendor.TenantId;
                    await screening.ScreenAsync(vendor.TenantId, vendor.Id, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Scheduled vendor sanctions screening failed."); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
