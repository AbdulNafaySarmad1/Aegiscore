using System.Data;
using AegisCore.Application.Finance;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Finance;

public sealed class FinanceProcurementService(AegisDbContext db, IFinanceModuleService finance)
{
    public async Task<FinancePurchaseOrder> CreatePurchaseOrderAsync(Guid tenantId, Guid actorId, CreateFinancePurchaseOrderCommand command, CancellationToken ct)
    {
        if (actorId == Guid.Empty || command.Lines.Count == 0 || command.Lines.Any(x => x.ExpenseAccountId == Guid.Empty || string.IsNullOrWhiteSpace(x.Description) || x.Quantity <= 0m || x.UnitPrice < 0m)) throw new ArgumentException("Purchase order lines are invalid.");
        var vendor = await db.FinanceVendors.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == command.VendorId, ct) ?? throw new InvalidOperationException("Vendor was not found.");
        if (vendor.Status != "ACTIVE" || vendor.SanctionsStatus != "CLEAR") throw new InvalidOperationException("Vendor must be active and sanctions-cleared.");
        var currency = command.Currency.Trim().ToUpperInvariant();
        if (currency.Length != 3 || vendor.InvoiceCurrency != currency || command.Number.Trim().Length is < 1 or > 50) throw new ArgumentException("Purchase order header is invalid.");
        var accountIds = command.Lines.Select(x => x.ExpenseAccountId).Distinct().ToArray();
        if (await db.ChartOfAccounts.CountAsync(x => x.TenantId == tenantId && x.IsActive && accountIds.Contains(x.Id), ct) != accountIds.Length) throw new ArgumentException("Purchase order expense accounts are invalid.");
        var order = new FinancePurchaseOrder
        {
            Id = Guid.NewGuid(), TenantId = tenantId, VendorId = vendor.Id, Number = command.Number.Trim(), Currency = currency, CreatedByUserId = actorId,
            Lines = command.Lines.Select(x => new FinancePurchaseOrderLine { Id = Guid.NewGuid(), TenantId = tenantId, ExpenseAccountId = x.ExpenseAccountId, Description = x.Description.Trim(), Quantity = x.Quantity, UnitPrice = x.UnitPrice, LineTotal = x.Quantity * x.UnitPrice }).ToList()
        };
        order.TotalAmount = order.Lines.Sum(x => x.LineTotal);
        db.FinancePurchaseOrders.Add(order);
        await db.SaveChangesAsync(ct);
        return order;
    }

    public async Task<FinancePurchaseOrder> ApprovePurchaseOrderAsync(Guid tenantId, Guid orderId, Guid actorId, CancellationToken ct)
    {
        var order = await db.FinancePurchaseOrders.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == orderId, ct) ?? throw new InvalidOperationException("Purchase order was not found.");
        if (order.Status != "DRAFT" || order.CreatedByUserId == actorId) throw new InvalidOperationException("Purchase order requires an independent approver.");
        order.Status = "APPROVED"; order.ApprovedByUserId = actorId; order.ApprovedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return order;
    }

    public async Task<FinanceGoodsReceipt> CreateGoodsReceiptAsync(Guid tenantId, Guid actorId, CreateFinanceGoodsReceiptCommand command, CancellationToken ct)
    {
        var order = await db.FinancePurchaseOrders.Include(x => x.Lines).SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == command.PurchaseOrderId, ct) ?? throw new InvalidOperationException("Purchase order was not found.");
        if (order.Status is not ("APPROVED" or "PARTIALLY_RECEIVED") || command.Lines.Count == 0) throw new InvalidOperationException("Purchase order cannot receive goods.");
        if (actorId == Guid.Empty || actorId == order.CreatedByUserId || actorId == order.ApprovedByUserId) throw new InvalidOperationException("Goods receipt requires a user independent of the purchase order maker and approver.");
        var prior = await db.FinanceGoodsReceiptLines.Where(x => x.TenantId == tenantId && order.Lines.Select(l => l.Id).Contains(x.PurchaseOrderLineId)).GroupBy(x => x.PurchaseOrderLineId).ToDictionaryAsync(x => x.Key, x => x.Sum(y => y.QuantityAccepted), ct);
        foreach (var line in command.Lines)
        {
            var ordered = order.Lines.SingleOrDefault(x => x.Id == line.PurchaseOrderLineId) ?? throw new ArgumentException("Receipt line is not on the purchase order.");
            if (line.QuantityAccepted < 0m || line.QuantityRejected < 0m || line.QuantityAccepted + line.QuantityRejected <= 0m || prior.GetValueOrDefault(line.PurchaseOrderLineId) + line.QuantityAccepted > ordered.Quantity) throw new ArgumentException("Receipt quantity exceeds the purchase order.");
        }
        var receipt = new FinanceGoodsReceipt { Id = Guid.NewGuid(), TenantId = tenantId, PurchaseOrderId = order.Id, Number = command.Number.Trim(), ConfirmedByUserId = actorId, ReceivedAt = command.ReceivedAt.ToUniversalTime(), Lines = command.Lines.Select(x => new FinanceGoodsReceiptLine { Id = Guid.NewGuid(), TenantId = tenantId, PurchaseOrderLineId = x.PurchaseOrderLineId, QuantityAccepted = x.QuantityAccepted, QuantityRejected = x.QuantityRejected, Note = x.Note?.Trim() }).ToList() };
        var acceptedAfter = receipt.Lines.Sum(x => x.QuantityAccepted) + prior.Values.Sum();
        order.Status = acceptedAfter >= order.Lines.Sum(x => x.Quantity) ? "RECEIVED" : "PARTIALLY_RECEIVED";
        db.FinanceGoodsReceipts.Add(receipt);
        await db.SaveChangesAsync(ct);
        return receipt;
    }

    public async Task<FinanceVendorInvoice> CreateInvoiceAsync(Guid tenantId, CreateFinanceVendorInvoiceCommand command, CancellationToken ct)
    {
        var order = await db.FinancePurchaseOrders.Include(x => x.Lines).SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == command.PurchaseOrderId, ct) ?? throw new InvalidOperationException("Purchase order was not found.");
        if (order.VendorId != command.VendorId || order.Currency != command.Currency.Trim().ToUpperInvariant() || order.Status is not ("RECEIVED" or "PARTIALLY_RECEIVED")) throw new InvalidOperationException("Invoice does not match an receivable purchase order.");
        if (command.Lines.Count == 0 || command.DueDate < command.InvoiceDate) throw new ArgumentException("Invoice dates or lines are invalid.");
        var invoice = new FinanceVendorInvoice { Id = Guid.NewGuid(), TenantId = tenantId, VendorId = command.VendorId, PurchaseOrderId = order.Id, InvoiceNumber = command.InvoiceNumber.Trim(), Currency = order.Currency, InvoiceDate = command.InvoiceDate.ToUniversalTime(), DueDate = command.DueDate.ToUniversalTime(), Lines = command.Lines.Select(x => new FinanceVendorInvoiceLine { Id = Guid.NewGuid(), TenantId = tenantId, PurchaseOrderLineId = x.PurchaseOrderLineId, Quantity = x.Quantity, UnitPrice = x.UnitPrice, LineTotal = x.Quantity * x.UnitPrice }).ToList() };
        if (invoice.Lines.Any(x => x.Quantity <= 0m || x.UnitPrice < 0m || !order.Lines.Any(p => p.Id == x.PurchaseOrderLineId))) throw new ArgumentException("Invoice line is invalid.");
        invoice.TotalAmount = invoice.Lines.Sum(x => x.LineTotal);
        db.FinanceVendorInvoices.Add(invoice);
        await db.SaveChangesAsync(ct);
        return invoice;
    }

    public async Task<FinanceThreeWayMatch> MatchInvoiceAsync(Guid tenantId, Guid invoiceId, Guid actorId, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var invoice = await db.FinanceVendorInvoices.Include(x => x.Lines).SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == invoiceId, ct) ?? throw new InvalidOperationException("Invoice was not found.");
        if (invoice.Status != "RECEIVED") throw new InvalidOperationException("Invoice has already been matched.");
        var order = await db.FinancePurchaseOrders.Include(x => x.Lines).SingleAsync(x => x.TenantId == tenantId && x.Id == invoice.PurchaseOrderId, ct);
        var receiptActors = await db.FinanceGoodsReceipts.Where(x => x.TenantId == tenantId && x.PurchaseOrderId == order.Id).Select(x => x.ConfirmedByUserId).Distinct().ToListAsync(ct);
        if (actorId == Guid.Empty || actorId == order.CreatedByUserId || actorId == order.ApprovedByUserId || receiptActors.Contains(actorId)) throw new InvalidOperationException("Invoice matching requires a user independent of purchasing, approval, and receipt.");
        var received = await db.FinanceGoodsReceiptLines.Where(x => x.TenantId == tenantId && order.Lines.Select(p => p.Id).Contains(x.PurchaseOrderLineId)).GroupBy(x => x.PurchaseOrderLineId).ToDictionaryAsync(x => x.Key, x => x.Sum(y => y.QuantityAccepted), ct);
        var invoiced = await db.FinanceVendorInvoiceLines
            .Where(x => x.TenantId == tenantId && x.VendorInvoiceId != invoice.Id && order.Lines.Select(p => p.Id).Contains(x.PurchaseOrderLineId)
                && db.FinanceVendorInvoices.Any(i => i.Id == x.VendorInvoiceId && i.Status != "FAILED"))
            .GroupBy(x => x.PurchaseOrderLineId).ToDictionaryAsync(x => x.Key, x => x.Sum(y => y.Quantity), ct);
        var valid = invoice.Lines.All(line => order.Lines.Any(po => po.Id == line.PurchaseOrderLineId && po.UnitPrice == line.UnitPrice)
            && line.Quantity + invoiced.GetValueOrDefault(line.PurchaseOrderLineId) <= received.GetValueOrDefault(line.PurchaseOrderLineId));
        var receivedTotal = order.Lines.Sum(x => received.GetValueOrDefault(x.Id) * x.UnitPrice);
        var match = new FinanceThreeWayMatch { Id = Guid.NewGuid(), TenantId = tenantId, VendorInvoiceId = invoice.Id, PurchaseOrderTotal = order.TotalAmount, ReceivedTotal = receivedTotal, InvoiceTotal = invoice.TotalAmount, Variance = invoice.TotalAmount - receivedTotal, Status = valid ? "MATCHED" : "FAILED", MatchedByUserId = actorId, MatchedAt = DateTime.UtcNow };
        invoice.Status = match.Status;
        db.FinanceThreeWayMatches.Add(match);
        if (valid)
        {
            var payable = await AccountAsync(tenantId, "2000", "Accounts payable", AccountType.Liability, ct);
            db.JournalEntries.Add(new JournalEntry { Id = Guid.NewGuid(), TenantId = tenantId, EntryNumber = $"AP-{invoice.Id:N}"[..20], EntryType = JournalEntryType.AutoGenerated, Description = $"Vendor invoice {invoice.InvoiceNumber}", EntryDate = invoice.InvoiceDate, TotalDebit = invoice.TotalAmount, TotalCredit = invoice.TotalAmount, IsPosted = true, PostedByUserId = actorId, PostedAt = DateTime.UtcNow, Lines = invoice.Lines.Select(line => new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = order.Lines.Single(po => po.Id == line.PurchaseOrderLineId).ExpenseAccountId, Debit = line.LineTotal }).Append(new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = payable.Id, Credit = invoice.TotalAmount }).ToList() });
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return match;
    }

    public async Task<FinanceVoucher> CreatePaymentVoucherAsync(Guid tenantId, Guid invoiceId, Guid actorId, CreateProcurementPaymentVoucherCommand command, CancellationToken ct)
    {
        var invoice = await db.FinanceVendorInvoices.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == invoiceId, ct) ?? throw new InvalidOperationException("Invoice was not found.");
        if (invoice.Status != "MATCHED" || invoice.PaymentVoucherId is not null) throw new InvalidOperationException("Only a matched, unpaid invoice can be paid.");
        var order = await db.FinancePurchaseOrders.SingleAsync(x => x.TenantId == tenantId && x.Id == invoice.PurchaseOrderId, ct);
        var excludedActors = await db.FinanceGoodsReceipts.Where(x => x.TenantId == tenantId && x.PurchaseOrderId == order.Id).Select(x => x.ConfirmedByUserId)
            .Concat(db.FinanceThreeWayMatches.Where(x => x.TenantId == tenantId && x.VendorInvoiceId == invoice.Id).Select(x => x.MatchedByUserId)).Distinct().ToListAsync(ct);
        if (actorId == Guid.Empty || actorId == order.CreatedByUserId || actorId == order.ApprovedByUserId || excludedActors.Contains(actorId)) throw new InvalidOperationException("Payment creation requires a user independent of purchasing, approval, receipt, and matching.");
        var vendor = await db.FinanceVendors.SingleAsync(x => x.TenantId == tenantId && x.Id == invoice.VendorId, ct);
        if (vendor.SanctionsStatus != "CLEAR") throw new InvalidOperationException("Vendor payment is blocked by sanctions status.");
        var bank = await db.FinanceBankAccounts.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == command.BankAccountId && x.IsActive && x.Currency == invoice.Currency, ct) ?? throw new InvalidOperationException("Settlement bank account is invalid.");
        var payable = await AccountAsync(tenantId, "2000", "Accounts payable", AccountType.Liability, ct);
        var voucher = await finance.CreateVoucherAsync(tenantId, actorId, new CreateVoucherCommand("PV", invoice.Currency, $"Vendor invoice {invoice.InvoiceNumber}", [new VoucherLineCommand(payable.Id, null, invoice.TotalAmount, 0m, vendor.LegalName), new VoucherLineCommand(bank.ChartOfAccountId, null, 0m, invoice.TotalAmount, vendor.LegalName)]), ct);
        invoice.PaymentVoucherId = voucher.Id; invoice.Status = "PAYMENT_PENDING";
        await db.SaveChangesAsync(ct);
        return voucher;
    }

    private async Task<ChartOfAccount> AccountAsync(Guid tenantId, string code, string name, AccountType type, CancellationToken ct)
    {
        var account = await db.ChartOfAccounts.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.AccountCode == code, ct);
        if (account is not null) return account;
        account = new ChartOfAccount { Id = Guid.NewGuid(), TenantId = tenantId, AccountCode = code, AccountName = name, AccountType = type };
        db.ChartOfAccounts.Add(account);
        return account;
    }
}
