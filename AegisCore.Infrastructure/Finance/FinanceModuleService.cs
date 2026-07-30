using System.Data;
using System.Security.Cryptography;
using System.Text;
using AegisCore.Application.Finance;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Finance;

public sealed class FinanceModuleService(AegisDbContext db) : IFinanceModuleService
{
    public async Task<FinanceVoucher> CreateVoucherAsync(Guid tenantId, Guid makerId, CreateVoucherCommand command, CancellationToken ct)
    {
        RequireTenant(tenantId);
        if (makerId == Guid.Empty || command.Lines.Count == 0) throw new ArgumentException("Maker and voucher lines are required.");
        if (string.IsNullOrWhiteSpace(command.VoucherType) || command.VoucherType.Length > 20) throw new ArgumentException("Voucher type is invalid.");
        if (command.Lines.Any(l => l.Debit < 0m || l.Credit < 0m || (l.Debit > 0m && l.Credit > 0m) || (l.Debit == 0m && l.Credit == 0m)))
            throw new ArgumentException("Each voucher line must contain either a positive debit or a positive credit.");

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var sequence = await db.Set<FinanceVoucherSequence>().IgnoreQueryFilters()
            .SingleOrDefaultAsync(s => s.TenantId == tenantId && s.VoucherType == command.VoucherType && s.FiscalYear == DateTime.UtcNow.Year, ct);
        if (sequence is null)
        {
            sequence = new FinanceVoucherSequence { Id = Guid.NewGuid(), TenantId = tenantId, VoucherType = command.VoucherType, FiscalYear = DateTime.UtcNow.Year };
            db.Set<FinanceVoucherSequence>().Add(sequence);
        }

        var number = sequence.NextNumber++;
        var voucher = new FinanceVoucher
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            VoucherNumber = $"{command.VoucherType.ToUpperInvariant()}-FY{DateTime.UtcNow:yy}-{number:000000}",
            VoucherType = command.VoucherType.ToUpperInvariant(),
            Currency = NormalizeCurrency(command.Currency),
            Description = command.Description,
            CreatedByUserId = makerId,
            TotalAmount = command.Lines.Sum(l => l.Debit),
            Lines = command.Lines.Select(l => new FinanceVoucherLine
            {
                Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = l.ChartOfAccountId,
                CostCenterId = l.CostCenterId, Debit = l.Debit, Credit = l.Credit, Description = l.Description
            }).ToList()
        };
        db.Set<FinanceVoucher>().Add(voucher);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return voucher;
    }

    public async Task<FinanceVoucher> SubmitVoucherAsync(Guid tenantId, Guid voucherId, Guid actorId, CancellationToken ct)
    {
        var voucher = await VoucherAsync(tenantId, voucherId, ct);
        EnsureState(voucher, FinanceStates.Draft);
        if (voucher.Lines.Sum(l => l.Debit) != voucher.Lines.Sum(l => l.Credit)) throw new InvalidOperationException("Voucher is not balanced.");
        voucher.Status = FinanceStates.PendingApproval;
        await db.SaveChangesAsync(ct);
        return voucher;
    }

    public async Task<FinanceVoucher> DecideVoucherAsync(Guid tenantId, Guid voucherId, Guid actorId, VoucherDecisionCommand command, CancellationToken ct)
    {
        var voucher = await VoucherAsync(tenantId, voucherId, ct);
        EnsureState(voucher, FinanceStates.PendingApproval);
        if (actorId == voucher.CreatedByUserId) throw new InvalidOperationException("Maker-checker separation is required.");
        if (actorId == Guid.Empty) throw new InvalidOperationException("Approver identity is required.");

        var approval = new FinanceVoucherApproval
        {
            Id = Guid.NewGuid(), FinanceVoucherId = voucher.Id, TenantId = tenantId, ApproverUserId = actorId,
            Decision = command.Approve ? "APPROVED" : "REJECTED", Note = command.Note
        };
        db.Set<FinanceVoucherApproval>().Add(approval);
        voucher.Status = command.Approve ? FinanceStates.Approved : FinanceStates.Rejected;
        voucher.RejectionNote = command.Approve ? null : command.Note;
        voucher.ApprovedByUserId = command.Approve ? actorId : null;
        voucher.ApprovedAt = command.Approve ? DateTime.UtcNow : null;
        await db.SaveChangesAsync(ct);
        return voucher;
    }

    public async Task<FinanceVoucher> PostVoucherAsync(Guid tenantId, Guid voucherId, Guid actorId, CancellationToken ct)
    {
        var voucher = await VoucherAsync(tenantId, voucherId, ct);
        EnsureState(voucher, FinanceStates.Approved);
        if (voucher.Lines.Sum(l => l.Debit) != voucher.Lines.Sum(l => l.Credit)) throw new InvalidOperationException("Voucher is not balanced.");

        var journal = new JournalEntry
        {
            Id = Guid.NewGuid(), TenantId = tenantId, EntryNumber = voucher.VoucherNumber, EntryType = JournalEntryType.Manual,
            Description = voucher.Description ?? voucher.VoucherNumber, EntryDate = DateTime.UtcNow,
            TotalDebit = voucher.Lines.Sum(l => l.Debit), TotalCredit = voucher.Lines.Sum(l => l.Credit),
            IsPosted = true, PostedByUserId = actorId, PostedAt = DateTime.UtcNow,
            Lines = voucher.Lines.Select(l => new JournalEntryLine
            {
                Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = l.ChartOfAccountId,
                Debit = l.Debit, Credit = l.Credit, Description = l.Description
            }).ToList()
        };
        db.JournalEntries.Add(journal);
        voucher.Status = FinanceStates.Posted;
        voucher.PostedByUserId = actorId;
        voucher.PostedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return voucher;
    }

    public async Task<FeeDemand> CreateFeeDemandAsync(Guid tenantId, CreateFeeDemandCommand command, CancellationToken ct)
    {
        RequireTenant(tenantId);
        if (command.StudentId == Guid.Empty || command.TotalAmount <= 0m || command.DueDate == default) throw new ArgumentException("Student, amount, and due date are required.");
        var installments = command.Installments is { Count: > 0 }
            ? command.Installments.ToList()
            : [new FeeInstallmentCommand(command.TotalAmount, command.DueDate)];
        if (installments.Any(i => i.Amount <= 0m || i.DueDate == default) || installments.Sum(i => i.Amount) != command.TotalAmount)
            throw new ArgumentException("Installments must be positive and sum exactly to the demand amount.");

        var demand = new FeeDemand
        {
            Id = Guid.NewGuid(), TenantId = tenantId, StudentId = command.StudentId,
            DemandNumber = $"FD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..21], Currency = NormalizeCurrency(command.Currency),
            TotalAmount = command.TotalAmount, OutstandingAmount = command.TotalAmount, DueDate = command.DueDate,
            Installments = installments.Select((i, index) => new FeeInstallment
            {
                Id = Guid.NewGuid(), TenantId = tenantId, Sequence = index + 1, Amount = i.Amount, DueDate = i.DueDate
            }).ToList()
        };
        db.Set<FeeDemand>().Add(demand);
        await db.SaveChangesAsync(ct);
        return demand;
    }

    public async Task<FinancePayment> RecordPaymentAsync(Guid tenantId, RecordFinancePaymentCommand command, CancellationToken ct)
    {
        RequireTenant(tenantId);
        if (command.FeeDemandId == Guid.Empty || command.Amount <= 0m || string.IsNullOrWhiteSpace(command.IdempotencyKey)) throw new ArgumentException("Payment fields are invalid.");
        if (!string.IsNullOrWhiteSpace(command.GatewaySlug)) throw new InvalidOperationException("Gateway payments must be confirmed by the gateway webhook.");
        if (!string.Equals(command.Method, "MANUAL", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Only manually verified payments may use this endpoint.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var existing = await db.Set<FinancePayment>().IgnoreQueryFilters().SingleOrDefaultAsync(p => p.TenantId == tenantId && p.IdempotencyKey == command.IdempotencyKey, ct);
        if (existing is not null) return existing;

        var demand = await db.Set<FeeDemand>().IgnoreQueryFilters().Include(d => d.Installments)
            .SingleOrDefaultAsync(d => d.Id == command.FeeDemandId && d.TenantId == tenantId, ct)
            ?? throw new InvalidOperationException("Fee demand was not found.");
        if (command.Amount > demand.OutstandingAmount) throw new InvalidOperationException("Payment exceeds the outstanding demand.");
        var installment = command.FeeInstallmentId is null ? null : demand.Installments.SingleOrDefault(i => i.Id == command.FeeInstallmentId);
        if (command.FeeInstallmentId is not null && installment is null) throw new InvalidOperationException("Fee installment was not found.");

        var payment = new FinancePayment
        {
            Id = Guid.NewGuid(), TenantId = tenantId, FeeDemandId = demand.Id, FeeInstallmentId = installment?.Id,
            IdempotencyKey = command.IdempotencyKey, Method = command.Method.Trim().ToUpperInvariant(),
            GatewaySlug = command.GatewaySlug, ExternalReference = command.ExternalReference, Amount = command.Amount,
            Status = "SETTLED", SettledAt = DateTime.UtcNow
        };
        demand.PaidAmount += command.Amount;
        demand.OutstandingAmount -= command.Amount;
        demand.Status = demand.OutstandingAmount == 0m ? "PAID" : "PARTIAL";
        if (installment is not null)
        {
            installment.PaidAmount += command.Amount;
            installment.Status = installment.PaidAmount >= installment.Amount ? "PAID" : "PARTIAL";
        }
        db.Set<FinancePayment>().Add(payment);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return payment;
    }

    private async Task<ChartOfAccount> AccountAsync(Guid tenantId, string code, string name, AccountType type, CancellationToken ct)
    {
        var account = await db.ChartOfAccounts.IgnoreQueryFilters().SingleOrDefaultAsync(a => a.TenantId == tenantId && a.AccountCode == code, ct);
        if (account is not null) return account;
        account = new ChartOfAccount { Id = Guid.NewGuid(), TenantId = tenantId, AccountCode = code, AccountName = name, AccountType = type };
        db.ChartOfAccounts.Add(account);
        return account;
    }

    public async Task<FinancePayment> RecordGatewayPaymentAsync(Guid tenantId, string gatewaySlug, RecordGatewayPaymentCommand command, CancellationToken ct)
    {
        RequireTenant(tenantId);
        var gateway = gatewaySlug.Trim().ToLowerInvariant();
        if (gateway.Length is < 2 or > 40 || gateway.Any(c => !(char.IsAsciiLetterOrDigit(c) || c == '-'))) throw new ArgumentException("Gateway is invalid.");
        if (command.FeeDemandId == Guid.Empty || command.Amount <= 0m || command.WebhookId.Length is < 1 or > 100 || command.ExternalReference.Length is < 1 or > 200)
            throw new ArgumentException("Gateway payment fields are invalid.");
        if (command.SanitizedPayloadJson.Length > 20000) throw new ArgumentException("Gateway payload is too large.");
        var currency = NormalizeCurrency(command.Currency);

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var existing = await db.Set<FinancePayment>().IgnoreQueryFilters()
            .SingleOrDefaultAsync(p => p.TenantId == tenantId && p.WebhookId == command.WebhookId, ct);
        if (existing is not null) return existing;

        var demand = await db.Set<FeeDemand>().IgnoreQueryFilters().Include(d => d.Installments)
            .SingleOrDefaultAsync(d => d.Id == command.FeeDemandId && d.TenantId == tenantId, ct)
            ?? throw new InvalidOperationException("Fee demand was not found.");
        if (!string.Equals(demand.Currency, currency, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Payment currency does not match the fee demand.");
        if (command.Amount > demand.OutstandingAmount) throw new InvalidOperationException("Payment exceeds the outstanding demand.");
        var installment = command.FeeInstallmentId is null ? null : demand.Installments.SingleOrDefault(i => i.Id == command.FeeInstallmentId);
        if (command.FeeInstallmentId is not null && installment is null) throw new InvalidOperationException("Fee installment was not found.");

        var payment = new FinancePayment
        {
            Id = Guid.NewGuid(), TenantId = tenantId, FeeDemandId = demand.Id, FeeInstallmentId = installment?.Id,
            IdempotencyKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{gateway}:{command.WebhookId}"))).ToLowerInvariant(),
            Method = "WEBHOOK", GatewaySlug = gateway, ExternalReference = command.ExternalReference,
            Amount = command.Amount, Status = "SETTLED", WebhookId = command.WebhookId, SettledAt = DateTime.UtcNow
        };
        demand.PaidAmount += command.Amount;
        demand.OutstandingAmount -= command.Amount;
        demand.Status = demand.OutstandingAmount == 0m ? "PAID" : "PARTIAL";
        if (installment is not null)
        {
            installment.PaidAmount += command.Amount;
            installment.Status = installment.PaidAmount >= installment.Amount ? "PAID" : "PARTIAL";
        }
        var bankAccount = await AccountAsync(tenantId, "1010", $"{gateway.ToUpperInvariant()} settlement bank", AccountType.Asset, ct);
        var receivableAccount = await AccountAsync(tenantId, "1100", "Student fee receivable", AccountType.Asset, ct);
        db.JournalEntries.Add(new JournalEntry
        {
            Id = Guid.NewGuid(), TenantId = tenantId, EntryNumber = $"RV-{payment.Id:N}"[..20],
            EntryType = JournalEntryType.AutoGenerated, Description = $"Gateway payment {payment.ExternalReference}",
            EntryDate = DateTime.UtcNow, TotalDebit = command.Amount, TotalCredit = command.Amount,
            IsPosted = true, PostedAt = DateTime.UtcNow,
            Lines =
            [
                new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = bankAccount.Id, Debit = command.Amount },
                new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = receivableAccount.Id, Credit = command.Amount }
            ]
        });
        db.Set<FinancePayment>().Add(payment);
        db.Set<PaymentGatewayEvent>().Add(new PaymentGatewayEvent
        {
            Id = Guid.NewGuid(), TenantId = tenantId, GatewaySlug = gateway, EventType = "PAYMENT_CONFIRMED",
            ReferenceId = command.ExternalReference, FinancePaymentId = payment.Id, Amount = command.Amount,
            Currency = currency, Status = "PROCESSED", IsSandbox = command.IsSandbox,
            SanitizedPayloadJson = command.SanitizedPayloadJson
        });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return payment;
    }

    public async Task<PaymentGatewayConfiguration> ConfigureGatewayAsync(Guid tenantId, Guid actorId, ConfigureGatewayCommand command, CancellationToken ct)
    {
        RequireTenant(tenantId);
        if (actorId == Guid.Empty || string.IsNullOrWhiteSpace(command.GatewaySlug) || string.IsNullOrWhiteSpace(command.SecretNamespace)) throw new ArgumentException("Gateway configuration is incomplete.");
        var configuration = await db.Set<PaymentGatewayConfiguration>().IgnoreQueryFilters()
            .SingleOrDefaultAsync(g => g.TenantId == tenantId && g.GatewaySlug == command.GatewaySlug, ct);
        if (configuration is null)
        {
            configuration = new PaymentGatewayConfiguration { Id = Guid.NewGuid(), TenantId = tenantId, GatewaySlug = command.GatewaySlug.Trim().ToLowerInvariant() };
            db.Set<PaymentGatewayConfiguration>().Add(configuration);
        }
        configuration.Environment = command.Environment.Trim().ToLowerInvariant() switch { "sandbox" or "production" => command.Environment.Trim().ToLowerInvariant(), _ => throw new ArgumentException("Gateway environment is invalid.") };
        configuration.SecretNamespace = command.SecretNamespace.Trim();
        configuration.IsActive = command.IsActive;
        configuration.ConfiguredByUserId = actorId;
        configuration.ConfiguredAt = DateTime.UtcNow;
        configuration.HealthStatus = "unknown";
        configuration.HealthError = null;
        await db.SaveChangesAsync(ct);
        return configuration;
    }

    public async Task<FinanceDashboardSummary> GetDashboardAsync(Guid tenantId, CancellationToken ct)
    {
        RequireTenant(tenantId);
        var demands = db.Set<FeeDemand>().IgnoreQueryFilters().Where(d => d.TenantId == tenantId);
        var vouchers = db.Set<FinanceVoucher>().IgnoreQueryFilters().Where(v => v.TenantId == tenantId);
        return new FinanceDashboardSummary(
            await demands.Where(d => d.Status != "PAID").SumAsync(d => d.OutstandingAmount, ct),
            await vouchers.Where(v => v.Status == FinanceStates.Posted && v.PostedAt >= DateTime.UtcNow.AddDays(-30)).SumAsync(v => v.TotalAmount, ct),
            await vouchers.Where(v => v.Status == FinanceStates.PendingApproval).SumAsync(v => v.TotalAmount, ct),
            await demands.CountAsync(d => d.Status != "PAID", ct),
            await vouchers.CountAsync(v => v.Status == FinanceStates.PendingApproval, ct));
    }

    public async Task<IReadOnlyList<TrialBalanceLine>> GetTrialBalanceAsync(Guid tenantId, DateTime periodFrom, DateTime periodTo, CancellationToken ct)
    {
        RequireTenant(tenantId);
        if (periodTo < periodFrom) throw new ArgumentException("Report date range is invalid.");
        return await (
            from line in db.JournalEntryLines.AsNoTracking()
            join entry in db.JournalEntries.AsNoTracking() on line.JournalEntryId equals entry.Id
            join account in db.ChartOfAccounts.AsNoTracking() on line.ChartOfAccountId equals account.Id
            where line.TenantId == tenantId && entry.TenantId == tenantId && account.TenantId == tenantId && entry.IsPosted && entry.EntryDate >= periodFrom && entry.EntryDate <= periodTo
            group line by new { account.AccountCode, account.AccountName } into grouped
            let debit = grouped.Sum(line => line.Debit)
            let credit = grouped.Sum(line => line.Credit)
            orderby grouped.Key.AccountCode
            select new TrialBalanceLine(grouped.Key.AccountCode, grouped.Key.AccountName, debit, credit, debit - credit)
        ).ToListAsync(ct);
    }

    private async Task<FinanceVoucher> VoucherAsync(Guid tenantId, Guid id, CancellationToken ct)
        => await db.Set<FinanceVoucher>().IgnoreQueryFilters().Include(v => v.Lines).SingleOrDefaultAsync(v => v.Id == id && v.TenantId == tenantId, ct)
            ?? throw new InvalidOperationException("Voucher was not found.");

    private static void EnsureState(FinanceVoucher voucher, string expected)
    {
        if (!string.Equals(voucher.Status, expected, StringComparison.Ordinal)) throw new InvalidOperationException($"Voucher must be {expected}.");
    }

    private static string NormalizeCurrency(string currency)
    {
        var normalized = currency.Trim().ToUpperInvariant();
        if (normalized.Length != 3 || normalized.Any(c => c is < 'A' or > 'Z')) throw new ArgumentException("Currency must be an ISO 4217 code.");
        return normalized;
    }

    private static void RequireTenant(Guid tenantId)
    {
        if (tenantId == Guid.Empty) throw new InvalidOperationException("A tenant context is required.");
    }
}
