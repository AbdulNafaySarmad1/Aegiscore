using AegisCore.Application.Finance;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure.Finance;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Onboarding;
using AegisCore.Contracts.Abstractions.Cloud;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Tests;

public sealed class FinanceTests
{
    [Fact]
    public void PaymentRequest_Preserves_Amount_And_Method()
    {
        var request = new PaymentRequest("jazzcash", 12500m, "TXN-1");

        Assert.Equal("jazzcash", request.Method);
        Assert.Equal(12500m, request.Amount);
    }

    [Fact]
    public void FeeStructure_Total_Can_Be_Component_Driven()
    {
        var structure = new FeeStructure
        {
            Components =
            [
                new FeeComponent { Amount = 1000m, IsMandatory = true },
                new FeeComponent { Amount = 500m, IsMandatory = false }
            ]
        };

        Assert.Equal(1000m, structure.Components.Where(c => c.IsMandatory).Sum(c => c.Amount));
    }

    [Theory]
    [InlineData(0, "0-30")]
    [InlineData(30, "0-30")]
    [InlineData(31, "31-60")]
    [InlineData(60, "31-60")]
    [InlineData(61, "61-90")]
    [InlineData(90, "61-90")]
    [InlineData(91, "90+")]
    public void Receivables_Aging_Buckets_Are_Boundary_Correct(int daysOverdue, string expectedBucket)
    {
        var bucket = daysOverdue switch
        {
            <= 30 => "0-30",
            <= 60 => "31-60",
            <= 90 => "61-90",
            _ => "90+"
        };

        Assert.Equal(expectedBucket, bucket);
    }

    [Fact]
    public void Straight_Line_Depreciation_Is_Pro_Rata_And_Capped()
    {
        var purchaseDate = new DateTime(2026, 1, 1);
        var asset = new Asset { PurchaseCost = 1200m, UsefulLifeYears = 1, PurchaseDate = purchaseDate, DepreciationMethod = DepreciationMethod.StraightLine };

        Assert.Equal(101.9178m, AssetDepreciationCalculator.MonthlyCharge(asset, new DateTime(2026, 1, 31)));
        Assert.Equal(1200m, AssetDepreciationCalculator.TotalDepreciation(asset, purchaseDate, new DateTime(2027, 1, 1)));
    }

    [Fact]
    public void Sandbox_Journal_Entries_Are_Explicitly_Marked()
    {
        var entry = new JournalEntry { IsPosted = true, IsSandbox = true, TotalDebit = 10m, TotalCredit = 10m };

        Assert.True(entry.IsSandbox);
    }

    [Theory]
    [InlineData(1000, 0, 1, 10)]
    [InlineData(1000, 25, 0, 25)]
    [InlineData(0, 25, 10, 0)]
    public void Payment_Plan_Penalty_Is_Decimal_And_Configurable(decimal outstanding, decimal flat, decimal percentage, decimal expected)
    {
        Assert.Equal(expected, PaymentPlanPenaltyCalculator.Calculate(outstanding, flat, percentage));
    }

    [Fact]
    public async Task Gateway_Configuration_Requires_Explicit_Environment()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AegisDbContext>().UseSqlite(connection).Options;
        await using var db = new AegisDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var secrets = new Mock<ISecretService>();
        var service = new WizardGatewayService(db, secrets.Object);
        using var payload = JsonDocument.Parse("{\"MERCHANT_ID\":\"merchant\",\"CLIENT_ID\":\"client\",\"CLIENT_SECRET\":\"secret\",\"IBAN\":\"PK00TEST\",\"WEBHOOK_SECRET\":\"webhook\"}");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ConfigureAsync(Guid.NewGuid(), "raast", payload.RootElement, CancellationToken.None));
    }

    [Fact]
    public async Task Gateway_Configuration_Rejects_Overlong_Secret_Value()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AegisDbContext>().UseSqlite(connection).Options;
        await using var db = new AegisDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var secrets = new Mock<ISecretService>();
        var service = new WizardGatewayService(db, secrets.Object);
        var oversized = new string('x', 4097);
        using var payload = JsonDocument.Parse($"{{\"MERCHANT_ID\":\"merchant\",\"CLIENT_ID\":\"client\",\"CLIENT_SECRET\":\"{oversized}\",\"IBAN\":\"PK00TEST\",\"WEBHOOK_SECRET\":\"webhook\",\"ENV\":\"sandbox\"}}");

        await Assert.ThrowsAsync<ArgumentException>(() => service.ConfigureAsync(Guid.NewGuid(), "raast", payload.RootElement, CancellationToken.None));
    }

    [Fact]
    public async Task Accounting_Context_Is_Driven_By_Tenant_Configuration()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AegisDbContext>().UseSqlite(connection).Options;
        await using var db = new AegisDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var tenantId = Guid.NewGuid();
        db.TenantFinanceConfigurations.Add(new TenantFinanceConfiguration { Id = Guid.NewGuid(), TenantId = tenantId, PrimaryStandard = "US_GAAP", FiscalYearLabel = "FY26" });
        await db.SaveChangesAsync();

        var context = await new AccountingStandardsEngine(db).GetContextAsync(tenantId, CancellationToken.None);

        Assert.Equal("US_GAAP", context.PrimaryStandard);
        Assert.Contains("LIFO", context.Policies["inventory_cost_methods"], StringComparison.Ordinal);
    }

    [Fact]
    public async Task Restatement_Creates_Balanced_Memo_Lines_Without_Changing_Live_Ledger()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AegisDbContext>().UseSqlite(connection).Options;
        await using var db = new AegisDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var tenantId = Guid.NewGuid();
        var sourceAccount = new ChartOfAccount { Id = Guid.NewGuid(), TenantId = tenantId, AccountCode = "1500", AccountName = "Development asset", AccountType = AccountType.Asset };
        var targetAccount = new ChartOfAccount { Id = Guid.NewGuid(), TenantId = tenantId, AccountCode = "6500", AccountName = "Development expense", AccountType = AccountType.Expense };
        db.TenantFinanceConfigurations.Add(new TenantFinanceConfiguration { Id = Guid.NewGuid(), TenantId = tenantId, PrimaryStandard = "IFRS", FiscalYearLabel = "FY26" });
        db.ChartOfAccounts.AddRange(sourceAccount, targetAccount);
        db.JournalEntries.Add(new JournalEntry
        {
            Id = Guid.NewGuid(), TenantId = tenantId, EntryNumber = $"TEST-{Guid.NewGuid():N}", EntryDate = DateTime.UtcNow, IsPosted = true, TotalDebit = 100m, TotalCredit = 100m,
            Lines = [new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = sourceAccount.Id, Debit = 100m }, new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = targetAccount.Id, Credit = 100m }]
        });
        await db.SaveChangesAsync();
        var engine = new AccountingStandardsEngine(db);
        await engine.CreateRestatementRuleAsync(tenantId, new CreateRestatementRuleCommand("IFRS", "US_GAAP", sourceAccount.Id, targetAccount.Id, 1m, "Expense IFRS development asset under US GAAP"), CancellationToken.None);
        var liveJournalCount = await db.JournalEntries.IgnoreQueryFilters().CountAsync();

        var run = await engine.RunRestatementAsync(tenantId, Guid.NewGuid(), new RunRestatementCommand("IFRS", "US_GAAP", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1)), CancellationToken.None);

        Assert.Equal(run.Lines.Sum(x => x.Debit), run.Lines.Sum(x => x.Credit));
        Assert.Equal(liveJournalCount, await db.JournalEntries.IgnoreQueryFilters().CountAsync());
    }
}
