using AegisCore.Application.Finance;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Finance;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Tests;

public sealed class FinanceProcurementSodTests
{
    [Fact]
    public async Task GoodsReceipt_Rejects_PurchaseOrder_Maker_And_Approver()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var tenantId = Guid.NewGuid();
        await using var db = await DatabaseAsync(connection, tenantId);
        var fixture = await SeedAsync(db, tenantId);
        var service = new FinanceProcurementService(db, Mock.Of<IFinanceModuleService>());
        var command = new CreateFinanceGoodsReceiptCommand(fixture.Order.Id, "GRN-1", DateTime.UtcNow,
            [new FinanceGoodsReceiptLineCommand(fixture.Line.Id, 1m, 0m, null)]);

        Assert.Contains("independent", (await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateGoodsReceiptAsync(fixture.TenantId, fixture.MakerId, command, CancellationToken.None))).Message);
        Assert.Contains("independent", (await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateGoodsReceiptAsync(fixture.TenantId, fixture.ApproverId, command, CancellationToken.None))).Message);
        Assert.Empty(db.FinanceGoodsReceipts);
    }

    [Fact]
    public async Task ThreeWayMatch_Rejects_Maker_Approver_And_Receiver()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var tenantId = Guid.NewGuid();
        await using var db = await DatabaseAsync(connection, tenantId);
        var fixture = await SeedAsync(db, tenantId);
        var receiverId = Guid.NewGuid();
        var invoice = await AddReceivedInvoiceAsync(db, fixture, receiverId);
        var service = new FinanceProcurementService(db, Mock.Of<IFinanceModuleService>());

        foreach (var actorId in new[] { fixture.MakerId, fixture.ApproverId, receiverId })
            Assert.Contains("independent", (await Assert.ThrowsAsync<InvalidOperationException>(() => service.MatchInvoiceAsync(fixture.TenantId, invoice.Id, actorId, CancellationToken.None))).Message);

        Assert.Empty(db.FinanceThreeWayMatches);
    }

    [Fact]
    public async Task PaymentVoucher_Rejects_All_Prior_Procurement_Actors()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var tenantId = Guid.NewGuid();
        await using var db = await DatabaseAsync(connection, tenantId);
        var fixture = await SeedAsync(db, tenantId);
        var receiverId = Guid.NewGuid();
        var matcherId = Guid.NewGuid();
        var invoice = await AddReceivedInvoiceAsync(db, fixture, receiverId);
        invoice.Status = "MATCHED";
        db.FinanceThreeWayMatches.Add(new FinanceThreeWayMatch
        {
            Id = Guid.NewGuid(), TenantId = fixture.TenantId, VendorInvoiceId = invoice.Id, Status = "MATCHED", MatchedByUserId = matcherId
        });
        await db.SaveChangesAsync();
        var service = new FinanceProcurementService(db, Mock.Of<IFinanceModuleService>());

        foreach (var actorId in new[] { fixture.MakerId, fixture.ApproverId, receiverId, matcherId })
            Assert.Contains("independent", (await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreatePaymentVoucherAsync(
                fixture.TenantId, invoice.Id, actorId, new CreateProcurementPaymentVoucherCommand(Guid.NewGuid()), CancellationToken.None))).Message);

        Assert.Null(invoice.PaymentVoucherId);
    }

    private static async Task<AegisDbContext> DatabaseAsync(SqliteConnection connection, Guid tenantId)
    {
        var http = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        http.Items["TenantId"] = tenantId;
        var db = new AegisDbContext(new DbContextOptionsBuilder<AegisDbContext>().UseSqlite(connection).Options, new FixedHttpContextAccessor(http));
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private static async Task<Fixture> SeedAsync(AegisDbContext db, Guid tenantId)
    {
        var makerId = Guid.NewGuid();
        var approverId = Guid.NewGuid();
        var vendor = new FinanceVendor { Id = Guid.NewGuid(), TenantId = tenantId, LegalName = "Sod Test Vendor", InvoiceCurrency = "PKR", SanctionsStatus = "CLEAR", Status = "ACTIVE" };
        var account = new ChartOfAccount { Id = Guid.NewGuid(), TenantId = tenantId, AccountCode = "60001", AccountName = "Supplies", AccountType = Domain.AccountType.Expense };
        var line = new FinancePurchaseOrderLine { Id = Guid.NewGuid(), TenantId = tenantId, ExpenseAccountId = account.Id, Description = "Supplies", Quantity = 1m, UnitPrice = 100m, LineTotal = 100m };
        var order = new FinancePurchaseOrder
        {
            Id = Guid.NewGuid(), TenantId = tenantId, VendorId = vendor.Id, Number = "PO-1", Currency = "PKR", TotalAmount = 100m,
            Status = "APPROVED", CreatedByUserId = makerId, ApprovedByUserId = approverId, ApprovedAt = DateTime.UtcNow, Lines = [line]
        };
        db.AddRange(vendor, account, order);
        await db.SaveChangesAsync();
        return new Fixture(tenantId, makerId, approverId, vendor, order, line);
    }

    private static async Task<FinanceVendorInvoice> AddReceivedInvoiceAsync(AegisDbContext db, Fixture fixture, Guid receiverId)
    {
        db.FinanceGoodsReceipts.Add(new FinanceGoodsReceipt
        {
            Id = Guid.NewGuid(), TenantId = fixture.TenantId, PurchaseOrderId = fixture.Order.Id, Number = "GRN-1", ConfirmedByUserId = receiverId,
            ReceivedAt = DateTime.UtcNow, Lines = [new FinanceGoodsReceiptLine { Id = Guid.NewGuid(), TenantId = fixture.TenantId, PurchaseOrderLineId = fixture.Line.Id, QuantityAccepted = 1m }]
        });
        var invoice = new FinanceVendorInvoice
        {
            Id = Guid.NewGuid(), TenantId = fixture.TenantId, VendorId = fixture.Vendor.Id, PurchaseOrderId = fixture.Order.Id, InvoiceNumber = "INV-1",
            Currency = "PKR", TotalAmount = 100m, Status = "RECEIVED", InvoiceDate = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(30),
            Lines = [new FinanceVendorInvoiceLine { Id = Guid.NewGuid(), TenantId = fixture.TenantId, PurchaseOrderLineId = fixture.Line.Id, Quantity = 1m, UnitPrice = 100m, LineTotal = 100m }]
        };
        db.FinanceVendorInvoices.Add(invoice);
        await db.SaveChangesAsync();
        return invoice;
    }

    // HttpContextAccessor stores the context in an AsyncLocal, which does not flow out of an async
    // helper back to the test; a plain property does.
    private sealed class FixedHttpContextAccessor(Microsoft.AspNetCore.Http.HttpContext context) : Microsoft.AspNetCore.Http.IHttpContextAccessor
    {
        public Microsoft.AspNetCore.Http.HttpContext? HttpContext { get; set; } = context;
    }

    private sealed record Fixture(Guid TenantId, Guid MakerId, Guid ApproverId, FinanceVendor Vendor, FinancePurchaseOrder Order, FinancePurchaseOrderLine Line);
}
