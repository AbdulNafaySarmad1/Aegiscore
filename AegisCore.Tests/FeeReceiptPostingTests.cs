using AegisCore.Application.Finance;
using AegisCore.Contracts.Abstractions.Cloud;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Cloud.R2;
using AegisCore.Infrastructure.Finance;
using AegisCore.Infrastructure.Security.Pqc;
using AegisCore.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace AegisCore.Tests;

/// <summary>
/// Money the university collects has to reach its books. It did not: a settled fee payment updated
/// the student's demand and was written to FinancePayments, and the general ledger never heard of
/// it — so revenue, cash, and every statement drawn from them, omitted every rupee taken in fees.
/// On the pilot database that was Rs 115,000 collected against a reported revenue of zero.
/// </summary>
public class FeeReceiptPostingTests
{
    private static readonly Guid Tenant = Guid.Parse("8f61d3b7-4c9e-4a3d-9f2a-2b7c5d9e1a44");

    [Theory, DatabaseProviders]
    public async Task A_Settled_Fee_Payment_Reaches_The_General_Ledger(bool postgres)
    {
        await using var database = await TestDatabase.CreateAsync(postgres);
        await database.EnsureSchemaAsync(Tenant);
        await using var provider = BuildFinance(database);
        await using var scope = provider.CreateAsyncScope();
        var finance = scope.ServiceProvider.GetRequiredService<IFinanceModuleService>();
        var db = scope.ServiceProvider.GetRequiredService<AegisDbContext>();

        var demand = await finance.CreateFeeDemandAsync(Tenant, new CreateFeeDemandCommand(
            Guid.NewGuid(), "PKR", 60_000m, DateTime.UtcNow.AddMonths(2), null), CancellationToken.None);

        var payment = await finance.RecordPaymentAsync(Tenant, new RecordFinancePaymentCommand(
            demand.Id, null, "MANUAL", 60_000m, "receipt-1", null, "RCPT-1"), CancellationToken.None);

        Assert.Equal("SETTLED", payment.Status);

        var lines = await (from line in db.JournalEntryLines.AsNoTracking()
                           join account in db.ChartOfAccounts.AsNoTracking() on line.ChartOfAccountId equals account.Id
                           select new { account.AccountCode, account.AccountType, line.Debit, line.Credit }).ToListAsync();

        var bank = Assert.Single(lines.Where(l => l.AccountCode == "1010"));
        var revenue = Assert.Single(lines.Where(l => l.AccountCode == "4000"));
        Assert.Equal(60_000m, bank.Debit);
        Assert.Equal(60_000m, revenue.Credit);
        Assert.Equal(AegisCore.Domain.AccountType.Asset, bank.AccountType);
        Assert.Equal(AegisCore.Domain.AccountType.Revenue, revenue.AccountType);
        Assert.Equal(lines.Sum(l => l.Debit), lines.Sum(l => l.Credit));
    }

    [Theory, DatabaseProviders]
    public async Task The_Same_Payment_Is_Not_Posted_Twice(bool postgres)
    {
        await using var database = await TestDatabase.CreateAsync(postgres);
        await database.EnsureSchemaAsync(Tenant);
        await using var provider = BuildFinance(database);
        await using var scope = provider.CreateAsyncScope();
        var finance = scope.ServiceProvider.GetRequiredService<IFinanceModuleService>();
        var db = scope.ServiceProvider.GetRequiredService<AegisDbContext>();

        var demand = await finance.CreateFeeDemandAsync(Tenant, new CreateFeeDemandCommand(
            Guid.NewGuid(), "PKR", 10_000m, DateTime.UtcNow.AddMonths(1), null), CancellationToken.None);

        var command = new RecordFinancePaymentCommand(demand.Id, null, "MANUAL", 10_000m, "one-key", null, "RCPT");
        var first = await finance.RecordPaymentAsync(Tenant, command, CancellationToken.None);
        var retry = await finance.RecordPaymentAsync(Tenant, command, CancellationToken.None);

        Assert.Equal(first.Id, retry.Id);
        Assert.Equal(10_000m, await db.JournalEntryLines.AsNoTracking().SumAsync(l => l.Debit));
    }

    [Theory, DatabaseProviders]
    public async Task Paying_More_Than_Is_Owed_Is_Refused_As_A_Rule_Not_A_Fault(bool postgres)
    {
        await using var database = await TestDatabase.CreateAsync(postgres);
        await database.EnsureSchemaAsync(Tenant);
        await using var provider = BuildFinance(database);
        await using var scope = provider.CreateAsyncScope();
        var finance = scope.ServiceProvider.GetRequiredService<IFinanceModuleService>();
        var db = scope.ServiceProvider.GetRequiredService<AegisDbContext>();

        var demand = await finance.CreateFeeDemandAsync(Tenant, new CreateFeeDemandCommand(
            Guid.NewGuid(), "PKR", 5_000m, DateTime.UtcNow.AddMonths(1), null), CancellationToken.None);

        // A clerk paying twice by mistake is told the rule, not "something went wrong on the server".
        var refused = await Assert.ThrowsAsync<AegisCore.Infrastructure.Deployment.BusinessRuleException>(
            () => finance.RecordPaymentAsync(Tenant, new RecordFinancePaymentCommand(
                demand.Id, null, "MANUAL", 7_500m, "too-much", null, "RCPT"), CancellationToken.None));

        Assert.Contains("outstanding", refused.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await db.JournalEntryLines.AsNoTracking().ToListAsync());
    }

    /// <summary>The finance module wired the way the API wires it, minus the web host.</summary>
    private static ServiceProvider BuildFinance(TestDatabase database)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var http = new DefaultHttpContext();
        http.Items["TenantId"] = Tenant;
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
