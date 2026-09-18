using System.Xml.Linq;
using AegisCore.Application.Finance;
using AegisCore.Application.Foundation;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Finance;

/// <summary>
/// The lifecycle fee operations (raise a semester's demand from the fee structure, record a verified
/// payment, apply a scholarship) on the one fee ledger, FeeDemands. StudentFeeDues, the earlier
/// ledger, was folded into it; there is no second place a student's balance can live.
/// </summary>
public sealed class FeeService(AegisDbContext db, IPolicyService policies, IAccountingService accounting) : IFeeService
{
    public static readonly string[] PaymentMethods = ["bank_transfer", "jazzcash", "easypaisa", "cheque", "cash", "manual"];

    public async Task<FeeDemand> CalculateFeeAsync(Guid studentId, Guid semesterId, Guid feeStructureId, CancellationToken ct = default)
    {
        if (studentId == Guid.Empty || semesterId == Guid.Empty || feeStructureId == Guid.Empty) throw new ArgumentException("Student, semester, and fee structure are required.");
        var structure = await db.FeeStructures.Include(f => f.Components).FirstOrDefaultAsync(f => f.Id == feeStructureId, ct)
            ?? throw new KeyNotFoundException("Fee structure was not found.");
        var total = structure.Components.Count == 0 ? structure.TotalFee : structure.Components.Where(c => c.IsMandatory).Sum(c => c.Amount);
        if (total < 0) throw new InvalidOperationException("Fee structure total cannot be negative.");
        if (await db.FeeDemands.AnyAsync(d => d.StudentId == studentId && d.SemesterId == semesterId && d.FeeStructureId == feeStructureId, ct)) throw new InvalidOperationException("A fee demand already exists for this student, semester and fee structure.");
        var semester = await db.Semesters.AsNoTracking().FirstOrDefaultAsync(s => s.Id == semesterId, ct) ?? throw new KeyNotFoundException("Semester was not found.");
        var dueDays = await policies.GetAsync("finance", "fee_due_days", 30, ct);
        var now = DateTime.UtcNow;
        var demand = new FeeDemand
        {
            Id = Guid.NewGuid(), StudentId = studentId, SemesterId = semesterId, FeeStructureId = feeStructureId,
            DemandNumber = $"FD-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..21], Currency = "PKR",
            TotalAmount = total, OutstandingAmount = total, Status = total == 0m ? "PAID" : "OPEN", IssuedAt = now,
            DueDate = (semester.StartDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) is var start && start > now ? start : now).AddDays(dueDays),
        };
        demand.Installments.Add(new FeeInstallment { Id = Guid.NewGuid(), Sequence = 1, Amount = total, DueDate = demand.DueDate });
        db.FeeDemands.Add(demand);
        await db.SaveChangesAsync(ct);
        return demand;
    }

    public async Task RecordPaymentAsync(Guid feeDemandId, PaymentRequest payment, CancellationToken ct = default)
    {
        var demand = await db.FeeDemands.Include(d => d.Installments).FirstOrDefaultAsync(d => d.Id == feeDemandId, ct) ?? throw new KeyNotFoundException("Fee demand was not found.");
        if (payment.Amount <= 0 || payment.Amount > demand.OutstandingAmount || string.IsNullOrWhiteSpace(payment.Method) || !PaymentMethods.Contains(payment.Method.Trim().ToLowerInvariant()))
            throw new InvalidOperationException("Payment amount is outside the outstanding balance, or the payment method is not one of: " + string.Join(", ", PaymentMethods) + ".");

        var row = new FinancePayment
        {
            Id = Guid.NewGuid(), FeeDemandId = demand.Id, IdempotencyKey = string.IsNullOrWhiteSpace(payment.ReferenceNumber) ? Guid.NewGuid().ToString("N") : $"ref:{payment.ReferenceNumber.Trim()}"[..Math.Min(40, 4 + payment.ReferenceNumber.Trim().Length)],
            Method = payment.Method.Trim().ToUpperInvariant(), ExternalReference = payment.ReferenceNumber, Amount = payment.Amount, Status = "SETTLED", SettledAt = DateTime.UtcNow
        };
        if (await db.FinancePayments.AnyAsync(p => p.IdempotencyKey == row.IdempotencyKey, ct)) throw new InvalidOperationException($"A payment with reference '{payment.ReferenceNumber}' was already recorded.");
        Apply(demand, paid: payment.Amount);
        var installment = demand.Installments.OrderBy(i => i.Sequence).FirstOrDefault(i => i.PaidAmount < i.Amount);
        if (installment is not null) { row.FeeInstallmentId = installment.Id; installment.PaidAmount += payment.Amount; installment.Status = installment.PaidAmount >= installment.Amount ? "PAID" : "PARTIAL"; }
        db.FinancePayments.Add(row);
        await db.SaveChangesAsync(ct);
        await accounting.PostFeePaymentAsync(row, Guid.Empty, ct);
    }

    public async Task<decimal> CalculateLateFeeAsync(Guid feeDemandId, CancellationToken ct = default)
    {
        var demand = await db.FeeDemands.FindAsync([feeDemandId], ct) ?? throw new KeyNotFoundException("Fee demand was not found.");
        var graceDays = await policies.GetAsync("finance", "late_fee_grace_days", 3, ct);
        return demand.Status == "PAID" || demand.DueDate.AddDays(graceDays) > DateTime.UtcNow ? 0m : Math.Round(demand.OutstandingAmount * 0.01m * graceDays, 2);
    }

    public async Task ApplyScholarshipAsync(Guid feeDemandId, decimal amount, Guid scholarshipId, CancellationToken ct = default)
    {
        var demand = await db.FeeDemands.FindAsync([feeDemandId], ct) ?? throw new KeyNotFoundException("Fee demand was not found.");
        if (scholarshipId == Guid.Empty || amount <= 0 || amount > demand.OutstandingAmount) throw new InvalidOperationException("Scholarship amount is outside the outstanding balance.");
        demand.ScholarshipDeduction += amount;
        Apply(demand, paid: 0m);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Outstanding = total − paid − scholarship − waived; the status follows from it.</summary>
    public static void Apply(FeeDemand demand, decimal paid)
    {
        demand.PaidAmount += paid;
        demand.OutstandingAmount = Math.Max(0m, demand.TotalAmount - demand.PaidAmount - demand.ScholarshipDeduction - demand.WaivedAmount);
        demand.Status = demand.OutstandingAmount == 0m ? (demand.WaivedAmount > 0m && demand.PaidAmount == 0m ? "WAIVED" : "PAID") : demand.PaidAmount > 0m || demand.ScholarshipDeduction > 0m ? "PARTIAL" : "OPEN";
    }
}

public sealed class AccountingService(AegisDbContext db) : IAccountingService
{
    public async Task PostFeePaymentAsync(FinancePayment payment, Guid postedBy, CancellationToken ct = default)
    {
        if (payment.Amount <= 0) throw new InvalidOperationException("Journal amount must be positive.");
        if (await db.JournalEntries.AnyAsync(j => j.Description == $"Fee payment {payment.Id:N}", ct)) return;
        var bank = await AccountAsync("1010", "Bank", AccountType.Asset, ct);
        var revenue = await AccountAsync("4010", "Student Fee Revenue", AccountType.Revenue, ct);
        var journal = new JournalEntry
        {
            Id = Guid.NewGuid(),
            EntryNumber = $"FEE-{payment.Id:N}"[..20],
            EntryDate = payment.SettledAt ?? payment.CreatedAt,
            EntryType = JournalEntryType.AutoGenerated,
            Description = $"Fee payment {payment.Id:N}",
            TotalDebit = payment.Amount,
            TotalCredit = payment.Amount,
            IsPosted = true,
            PostedByUserId = postedBy,
            PostedAt = DateTime.UtcNow,
            Lines =
            [
                new JournalEntryLine { Id = Guid.NewGuid(), ChartOfAccountId = bank.Id, Debit = payment.Amount, Credit = 0m },
                new JournalEntryLine { Id = Guid.NewGuid(), ChartOfAccountId = revenue.Id, Debit = 0m, Credit = payment.Amount }
            ]
        };

        db.JournalEntries.Add(journal);
        await db.SaveChangesAsync(ct);
    }

    public async Task<string> ExportPifraAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var fromDate = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toDate = to.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
        var entries = await db.JournalEntries.Where(j => j.EntryDate >= fromDate && j.EntryDate <= toDate).ToListAsync(ct);
        return new XDocument(new XElement("pifra",
            new XAttribute("from", from),
            new XAttribute("to", to),
            entries.Select(e => new XElement("journal",
                new XAttribute("entryNumber", e.EntryNumber),
                new XAttribute("date", e.EntryDate.ToString("yyyy-MM-dd")),
                new XAttribute("debit", e.TotalDebit),
                new XAttribute("credit", e.TotalCredit))))).ToString(SaveOptions.DisableFormatting);
    }

    private async Task<ChartOfAccount> AccountAsync(string code, string name, AccountType type, CancellationToken ct)
    {
        var account = await db.ChartOfAccounts.FirstOrDefaultAsync(a => a.AccountCode == code, ct);
        if (account is not null) return account;

        account = new ChartOfAccount { Id = Guid.NewGuid(), AccountCode = code, AccountName = name, AccountType = type };
        db.ChartOfAccounts.Add(account);
        return account;
    }
}

public sealed class BudgetService(AegisDbContext db) : IBudgetService
{
    public async Task<BudgetVarianceReport> GetVarianceAsync(Guid costCenterId, CancellationToken ct = default)
    {
        var costCenter = await db.CostCenters.FindAsync([costCenterId], ct) ?? throw new KeyNotFoundException("Cost center was not found.");
        var actual = await db.JournalEntryLines.Where(l => l.Debit > 0).SumAsync(l => l.Debit, ct);
        var variance = costCenter.BudgetAllocation - actual;
        var percentage = costCenter.BudgetAllocation == 0 ? 0m : Math.Round(variance / costCenter.BudgetAllocation * 100m, 2);
        return new BudgetVarianceReport(costCenterId, costCenter.BudgetAllocation, actual, variance, percentage, variance < 0 ? "over_budget" : "on_budget");
    }
}
