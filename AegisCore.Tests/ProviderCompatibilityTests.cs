using AegisCore.Domain.Entities;
using AegisCore.Application.Finance;
using AegisCore.Contracts.Abstractions.Cloud;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Cloud.R2;
using AegisCore.Infrastructure.Finance;
using AegisCore.Infrastructure.Security.Pqc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AegisCore.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Tests;

/// <summary>
/// Queries that SQLite accepts and PostgreSQL rejects. Each case here was found by running the API
/// against the pilot PostgreSQL database; the SQLite half of every theory documents that the old
/// suite could never have caught it.
/// </summary>
public sealed class ProviderCompatibilityTests
{
    private static readonly Guid Tenant = Guid.NewGuid();

    [Theory, DatabaseProviders]
    public async Task Unspecified_Kind_DateTimes_Are_Accepted_In_Filters_And_Writes(bool postgres)
    {
        await using var database = await TestDatabase.CreateAsync(postgres);
        await database.EnsureSchemaAsync(Tenant);
        await using var db = database.CreateContext(Tenant);

        // A bare query-string date binds with Kind=Unspecified; DateOnly.ToDateTime yields the same.
        var unspecified = new DateTime(2026, 9, 1);
        Assert.Equal(DateTimeKind.Unspecified, unspecified.Kind);
        db.JournalEntries.Add(new JournalEntry { Id = Guid.NewGuid(), TenantId = Tenant, EntryDate = unspecified, Description = "kind probe", EntryNumber = "K-1", IsPosted = true });
        await db.SaveChangesAsync();

        var local = DateTime.SpecifyKind(new DateTime(2026, 8, 31, 23, 0, 0), DateTimeKind.Local);
        var found = await db.JournalEntries.AsNoTracking().Where(j => j.EntryDate >= unspecified && j.EntryDate <= unspecified.AddDays(1) || j.EntryDate >= local).ToListAsync();
        Assert.Single(found);
        Assert.Equal(DateTimeKind.Utc, found[0].EntryDate.Kind);
        Assert.Equal(unspecified, found[0].EntryDate);
    }

    [Theory, DatabaseProviders]
    public async Task Trial_Balance_And_Cash_Position_Group_Aggregates_Translate(bool postgres)
    {
        await using var database = await TestDatabase.CreateAsync(postgres);
        await database.EnsureSchemaAsync(Tenant);
        await using var db = database.CreateContext(Tenant);
        var cash = new ChartOfAccount { Id = Guid.NewGuid(), TenantId = Tenant, AccountCode = "1000", AccountName = "Cash", AccountType = AegisCore.Domain.AccountType.Asset, IsActive = true };
        var fees = new ChartOfAccount { Id = Guid.NewGuid(), TenantId = Tenant, AccountCode = "4000", AccountName = "Tuition", AccountType = AegisCore.Domain.AccountType.Revenue, IsActive = true };
        var entry = new JournalEntry { Id = Guid.NewGuid(), TenantId = Tenant, EntryDate = DateTime.UtcNow, Description = "fee receipt", EntryNumber = "J-1", IsPosted = true };
        db.ChartOfAccounts.AddRange(cash, fees);
        db.JournalEntries.Add(entry);
        db.JournalEntryLines.AddRange(
            new JournalEntryLine { Id = Guid.NewGuid(), TenantId = Tenant, JournalEntryId = entry.Id, ChartOfAccountId = cash.Id, Debit = 500m, Credit = 0m },
            new JournalEntryLine { Id = Guid.NewGuid(), TenantId = Tenant, JournalEntryId = entry.Id, ChartOfAccountId = fees.Id, Debit = 0m, Credit = 500m });
        await db.SaveChangesAsync();

        await using var provider = BuildFinanceServices(database);
        await using var scope = provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IFinanceModuleService>();
        var trial = await service.GetTrialBalanceAsync(Tenant, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), CancellationToken.None);
        Assert.Equal(2, trial.Count);
        Assert.Equal(500m, trial.Single(l => l.AccountCode == "1000").Debit);
        Assert.Equal(-500m, trial.Single(l => l.AccountCode == "4000").Balance);
    }

    /// <summary>The finance module wired the way the API wires it, minus the web host.</summary>
    private static ServiceProvider BuildFinanceServices(TestDatabase database)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var http = new DefaultHttpContext(); http.Items["TenantId"] = Tenant;
        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = http });
        services.AddDbContext<AegisDbContext>(options => database.Configure(options));
        services.AddFinanceModule();
        services.RemoveAll<IHostedService>();
        services.AddSingleton(Mock.Of<ISecretService>());
        services.AddSingleton(Mock.Of<IPqcSignatureService>());
        services.AddSingleton(Mock.Of<IR2StorageService>());
        services.AddSingleton(Mock.Of<IFinanceRealtimePublisher>());
        return services.BuildServiceProvider();
    }
}
