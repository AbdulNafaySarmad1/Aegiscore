using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AegisCore.Application.Finance;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure.Security.Pqc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Finance;

public sealed class FinanceModuleService(AegisDbContext db, IPqcSignatureService pqc, IAccountingStandardsEngine accounting, IFinanceRealtimePublisher realtime, FinanceContinuousControlService controls, Microsoft.Extensions.Logging.ILogger<FinanceModuleService> logger) : IFinanceModuleService
{
    public async Task<FinanceVoucher> CreateVoucherAsync(Guid tenantId, Guid makerId, CreateVoucherCommand command, CancellationToken ct)
    {
        RequireTenant(tenantId);
        if (makerId == Guid.Empty || command.Lines.Count == 0) throw new ArgumentException("Maker and voucher lines are required.");
        if (string.IsNullOrWhiteSpace(command.VoucherType) || command.VoucherType.Length > 20) throw new ArgumentException("Voucher type is invalid.");
        if (command.Lines.Any(l => l.Debit < 0m || l.Credit < 0m || (l.Debit > 0m && l.Credit > 0m) || (l.Debit == 0m && l.Credit == 0m)))
            throw new ArgumentException("Each voucher line must contain either a positive debit or a positive credit.");
        if (command.Lines.Any(l => l.ReportingEntityId.HasValue && l.ReportingEntityId == l.CounterpartyReportingEntityId))
            throw new ArgumentException("Reporting entity and counterparty must differ.");
        var reportingEntityIds = command.Lines.SelectMany(l => new[] { l.ReportingEntityId, l.CounterpartyReportingEntityId }).Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToArray();
        if (reportingEntityIds.Length > 0 && await db.FinanceReportingEntities.CountAsync(x => x.TenantId == tenantId && x.IsActive && reportingEntityIds.Contains(x.Id), ct) != reportingEntityIds.Length)
            throw new ArgumentException("Voucher reporting entities must be active and belong to the tenant.");
        var context = await accounting.GetContextAsync(tenantId, ct);
        if (!JsonSerializer.Deserialize<bool>(context.Policies["interest_accounts_allowed"]))
        {
            var accountIds = command.Lines.Select(x => x.ChartOfAccountId).Distinct().ToArray();
            if (await db.ChartOfAccounts.AnyAsync(x => x.TenantId == tenantId && accountIds.Contains(x.Id) && EF.Functions.ILike(x.AccountName, "%interest%"), ct))
                throw new AegisCore.Infrastructure.Deployment.BusinessRuleException("The tenant accounting policy prohibits interest accounts.");
        }

        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var sequence = await db.Set<FinanceVoucherSequence>()
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
                    CostCenterId = l.CostCenterId, ReportingEntityId = l.ReportingEntityId, CounterpartyReportingEntityId = l.CounterpartyReportingEntityId,
                    Debit = l.Debit, Credit = l.Credit, Description = l.Description
                }).ToList()
            };
            db.Set<FinanceVoucher>().Add(voucher);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return voucher;
        });
    }

    public async Task<FinanceVoucher> SubmitVoucherAsync(Guid tenantId, Guid voucherId, Guid actorId, CancellationToken ct)
    {
        var voucher = await VoucherAsync(tenantId, voucherId, ct);
        EnsureState(voucher, FinanceStates.Draft);
        if (voucher.Lines.Sum(l => l.Debit) != voucher.Lines.Sum(l => l.Credit)) throw new AegisCore.Infrastructure.Deployment.BusinessRuleException("Voucher is not balanced.");
        voucher.Status = FinanceStates.PendingApproval;
        await db.SaveChangesAsync(ct);
        await PublishAsync(tenantId, "voucher-updated", new { voucher.Id, voucher.VoucherNumber, voucher.VoucherType, voucher.TotalAmount, voucher.Currency, voucher.Status }, ct);
        return voucher;
    }

    public async Task<FinanceVoucher> DecideVoucherAsync(Guid tenantId, Guid voucherId, Guid actorId, VoucherDecisionCommand command, CancellationToken ct)
    {
        var voucher = await VoucherAsync(tenantId, voucherId, ct);
        EnsureState(voucher, FinanceStates.PendingApproval);
        if (actorId == voucher.CreatedByUserId) throw new InvalidOperationException("Maker-checker separation is required.");
        if (actorId == Guid.Empty) throw new InvalidOperationException("Approver identity is required.");
        if (!command.Approve && string.IsNullOrWhiteSpace(command.Note)) throw new ArgumentException("A rejection note is required.");

        var requirement = await ApprovalRequirementAsync(tenantId, voucher.Currency, voucher.TotalAmount, ct);
        var actorRoles = await db.UserRoles.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.UserId == actorId && (x.ExpiresAt == null || x.ExpiresAt > DateTime.UtcNow))
            .Select(x => x.Role.Name).ToListAsync(ct);
        var existingApprovals = await db.FinanceVoucherApprovals.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.FinanceVoucherId == voucher.Id && x.Decision == "APPROVED").ToListAsync(ct);
        var approvalRole = requirement.Roles.FirstOrDefault(role => actorRoles.Contains(role, StringComparer.OrdinalIgnoreCase)
            && (!existingApprovals.Any(x => x.ApproverRole.Equals(role, StringComparison.OrdinalIgnoreCase)) || requirement.MinimumApprovals > requirement.Roles.Count));
        if (command.Approve && approvalRole is null) throw new InvalidOperationException("The approver does not hold a required approval role or that role is already satisfied.");

        var approval = new FinanceVoucherApproval
        {
            Id = Guid.NewGuid(), FinanceVoucherId = voucher.Id, TenantId = tenantId, ApproverUserId = actorId,
            ApproverRole = approvalRole ?? actorRoles.FirstOrDefault() ?? "REJECTOR",
            Decision = command.Approve ? "APPROVED" : "REJECTED", Note = command.Note
        };
        if (command.Approve)
        {
            var signingPayload = FinanceVoucherSigningPayload.Create(tenantId, voucher.Id, voucher.VoucherNumber, voucher.TotalAmount, actorId, approval.CreatedAt);
            var signature = await pqc.SignAsync(tenantId, "voucher-approval", signingPayload, archival: false, ct);
            approval.PqcAlgorithm = signature.Algorithm;
            approval.PqcKeyId = signature.KeyId;
            approval.PqcSignature = signature.Signature;
            voucher.PqcAlgorithm = signature.Algorithm;
            voucher.PqcKeyId = signature.KeyId;
            voucher.PqcSignature = signature.Signature;
            voucher.PqcPublicKey = signature.PublicKey;
        }
        db.Set<FinanceVoucherApproval>().Add(approval);
        var approvalsAfter = command.Approve ? existingApprovals.Append(approval).ToList() : existingApprovals;
        var quorumMet = command.Approve
            && requirement.Roles.All(role => approvalsAfter.Any(x => x.ApproverRole.Equals(role, StringComparison.OrdinalIgnoreCase)))
            && approvalsAfter.Select(x => x.ApproverUserId).Distinct().Count() >= requirement.MinimumApprovals;
        voucher.Status = !command.Approve ? FinanceStates.Rejected : quorumMet ? FinanceStates.Approved : FinanceStates.PendingApproval;
        voucher.RejectionNote = command.Approve ? null : command.Note;
        voucher.ApprovedByUserId = quorumMet ? actorId : null;
        voucher.ApprovedAt = quorumMet ? DateTime.UtcNow : null;
        await db.SaveChangesAsync(ct);
        return voucher;
    }

    public async Task<FinanceVoucher> ReverseVoucherAsync(Guid tenantId, Guid voucherId, Guid actorId, ReverseVoucherCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Reason) || command.Reason.Length > 1000) throw new ArgumentException("A reversal reason is required.");
        var original = await VoucherAsync(tenantId, voucherId, ct);
        EnsureState(original, FinanceStates.Posted);
        if (await db.FinanceVoucherReversals.AnyAsync(x => x.TenantId == tenantId && x.OriginalVoucherId == voucherId, ct))
            throw new InvalidOperationException("The voucher already has a reversal.");
        var reversal = await CreateVoucherAsync(tenantId, actorId, new CreateVoucherCommand(
            original.VoucherType, original.Currency, $"Reversal of {original.VoucherNumber}: {command.Reason}",
            original.Lines.Select(x => new VoucherLineCommand(x.ChartOfAccountId, x.CostCenterId, x.Credit, x.Debit, x.Description, x.ReportingEntityId, x.CounterpartyReportingEntityId)).ToList()), ct);
        db.FinanceVoucherReversals.Add(new FinanceVoucherReversal
        {
            Id = Guid.NewGuid(), TenantId = tenantId, OriginalVoucherId = original.Id, ReversalVoucherId = reversal.Id,
            Reason = command.Reason.Trim(), RequestedByUserId = actorId, CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
        return reversal;
    }

    public async Task<FinanceVoucher> PostVoucherAsync(Guid tenantId, Guid voucherId, Guid actorId, CancellationToken ct)
    {
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var voucher = await VoucherAsync(tenantId, voucherId, ct);
            EnsureState(voucher, FinanceStates.Approved);
            if (voucher.Lines.Sum(l => l.Debit) != voucher.Lines.Sum(l => l.Credit)) throw new AegisCore.Infrastructure.Deployment.BusinessRuleException("Voucher is not balanced.");

            var journal = new JournalEntry
            {
                Id = Guid.NewGuid(), TenantId = tenantId, EntryNumber = voucher.VoucherNumber, EntryType = JournalEntryType.Manual,
                Description = voucher.Description ?? voucher.VoucherNumber, EntryDate = DateTime.UtcNow,
                TotalDebit = voucher.Lines.Sum(l => l.Debit), TotalCredit = voucher.Lines.Sum(l => l.Credit),
                IsPosted = true, PostedByUserId = actorId, PostedAt = DateTime.UtcNow,
                Lines = voucher.Lines.Select(l => new JournalEntryLine
                {
                    Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = l.ChartOfAccountId,
                    CostCenterId = l.CostCenterId,
                    ReportingEntityId = l.ReportingEntityId, CounterpartyReportingEntityId = l.CounterpartyReportingEntityId,
                    Debit = l.Debit, Credit = l.Credit, Description = l.Description
                }).ToList()
            };
            db.JournalEntries.Add(journal);
            voucher.Status = FinanceStates.Posted;
            voucher.PostedByUserId = actorId;
            voucher.PostedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            await controls.EvaluatePostingAsync(tenantId, voucher.Id, ct);
            await transaction.CommitAsync(ct);
            await PublishAsync(tenantId, "voucher-posted", new { voucher.Id, voucher.VoucherNumber, voucher.VoucherType, voucher.TotalAmount, voucher.Currency, voucher.Status, voucher.PostedAt }, ct);
            return voucher;
        });
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
        if (!string.IsNullOrWhiteSpace(command.GatewaySlug)) throw new AegisCore.Infrastructure.Deployment.BusinessRuleException("Gateway payments must be confirmed by the gateway webhook.");
        if (!string.Equals(command.Method, "MANUAL", StringComparison.OrdinalIgnoreCase)) throw new AegisCore.Infrastructure.Deployment.BusinessRuleException("Only manually verified payments may use this endpoint.");
        var receipt = await PrepareFeeReceiptAsync(tenantId, ct);
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var existing = await db.Set<FinancePayment>().SingleOrDefaultAsync(p => p.TenantId == tenantId && p.IdempotencyKey == command.IdempotencyKey, ct);
            if (existing is not null) return existing;

            var demand = await db.Set<FeeDemand>().Include(d => d.Installments)
                .SingleOrDefaultAsync(d => d.Id == command.FeeDemandId && d.TenantId == tenantId, ct)
                ?? throw new KeyNotFoundException("Fee demand was not found.");
            if (command.Amount > demand.OutstandingAmount) throw new AegisCore.Infrastructure.Deployment.BusinessRuleException("Payment exceeds the outstanding demand.");
            var installment = command.FeeInstallmentId is null ? null : demand.Installments.SingleOrDefault(i => i.Id == command.FeeInstallmentId);
            if (command.FeeInstallmentId is not null && installment is null) throw new KeyNotFoundException("Fee installment was not found.");

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
            await PostFeeReceiptAsync(tenantId, payment, demand.Currency, $"Fee receipt for demand {demand.DemandNumber} ({payment.Method})", isSandbox: false, receipt, ct);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            await PublishAsync(tenantId, "payment-settled", new { payment.Id, payment.FeeDemandId, payment.Amount, demand.Currency, payment.Status, payment.SettledAt }, ct);
            return payment;
        });
    }

    /// <summary>
    /// Puts a settled fee payment into the books: debit bank, credit student fee revenue.
    ///
    /// Nothing did this before. A payment updated the student's demand and was written to
    /// FinancePayments, and the general ledger never heard of it — so the trial balance, the
    /// profit and loss, the statement of financial position and the cash position all omitted every
    /// rupee the university had collected in fees. On the pilot database that was Rs 115,000
    /// collected against a reported revenue and cash position of zero.
    ///
    /// Income is recognised when the money is received. An institution that recognises fee income
    /// when the demand is raised instead needs a posting at demand time as well; this one does not
    /// make that choice silently.
    ///
    /// The voucher is written already posted, with its journal entry, inside the caller's
    /// transaction: the payment and its bookkeeping are one act or neither.
    /// </summary>
    private sealed record FeeReceiptAccounts(Guid BankId, Guid RevenueId, string VoucherNumber);

    /// <summary>
    /// Everything the receipt needs that has to be read or created first. It runs before the payment
    /// transaction opens, because a query inside a user-initiated transaction is refused outright by
    /// the retrying execution strategy this context uses.
    /// </summary>
    private async Task<FeeReceiptAccounts> PrepareFeeReceiptAsync(Guid tenantId, CancellationToken ct)
    {
        var bank = await AccountAsync(tenantId, "1010", "Bank", AccountType.Asset, ct);
        var feeRevenue = await AccountAsync(tenantId, "4000", "Student fee revenue", AccountType.Revenue, ct);

        var year = DateTime.UtcNow.Year;
        var sequence = await db.Set<FinanceVoucherSequence>()
            .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.VoucherType == "RV" && x.FiscalYear == year, ct);
        if (sequence is null)
        {
            sequence = new FinanceVoucherSequence { Id = Guid.NewGuid(), TenantId = tenantId, VoucherType = "RV", FiscalYear = year };
            db.Set<FinanceVoucherSequence>().Add(sequence);
        }
        var number = sequence.NextNumber++;
        await db.SaveChangesAsync(ct);
        return new FeeReceiptAccounts(bank.Id, feeRevenue.Id, $"RV-FY{DateTime.UtcNow:yy}-{number:000000}");
    }

    private async Task PostFeeReceiptAsync(Guid tenantId, FinancePayment payment, string currency, string description, bool isSandbox, FeeReceiptAccounts accounts, CancellationToken ct)
    {
        var bank = accounts.BankId;
        var feeRevenue = accounts.RevenueId;
        var voucherNumber = accounts.VoucherNumber;

        var voucher = new FinanceVoucher
        {
            Id = Guid.NewGuid(), TenantId = tenantId, VoucherNumber = voucherNumber, VoucherType = "RV",
            Currency = NormalizeCurrency(currency), Description = description,
            CreatedByUserId = payment.Id,              // the payment is what made this entry; no person keyed it
            TotalAmount = payment.Amount,
            Lines =
            [
                new FinanceVoucherLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = bank, Debit = payment.Amount, Credit = 0m, Description = description },
                new FinanceVoucherLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = feeRevenue, Debit = 0m, Credit = payment.Amount, Description = description },
            ],
        };
        db.Set<FinanceVoucher>().Add(voucher);

        db.JournalEntries.Add(new JournalEntry
        {
            Id = Guid.NewGuid(), TenantId = tenantId, EntryNumber = voucherNumber, EntryType = JournalEntryType.AutoGenerated,
            Description = description, EntryDate = payment.SettledAt ?? DateTime.UtcNow,
            TotalDebit = payment.Amount, TotalCredit = payment.Amount, IsPosted = true, IsSandbox = isSandbox, PostedAt = DateTime.UtcNow,
            Lines =
            [
                new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = bank, Debit = payment.Amount, Credit = 0m, Description = description },
                new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = feeRevenue, Debit = 0m, Credit = payment.Amount, Description = description },
            ],
        });

        // The lines must exist and balance before the database will accept the voucher as posted.
        await db.SaveChangesAsync(ct);
        voucher.Status = FinanceStates.Posted;
        voucher.PostedAt = DateTime.UtcNow;
    }

    private async Task<ChartOfAccount> AccountAsync(Guid tenantId, string code, string name, AccountType type, CancellationToken ct)
    {
        var account = await db.ChartOfAccounts.SingleOrDefaultAsync(a => a.TenantId == tenantId && a.AccountCode == code, ct);
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
        var receipt = await PrepareFeeReceiptAsync(tenantId, ct);

        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var existing = await db.Set<FinancePayment>()
                .SingleOrDefaultAsync(p => p.TenantId == tenantId && p.WebhookId == command.WebhookId, ct);
            if (existing is not null) return existing;

            var demand = await db.Set<FeeDemand>().Include(d => d.Installments)
                .SingleOrDefaultAsync(d => d.Id == command.FeeDemandId && d.TenantId == tenantId, ct)
                ?? throw new KeyNotFoundException("Fee demand was not found.");
            if (!string.Equals(demand.Currency, currency, StringComparison.OrdinalIgnoreCase)) throw new AegisCore.Infrastructure.Deployment.BusinessRuleException("Payment currency does not match the fee demand.");
            if (command.Amount > demand.OutstandingAmount) throw new AegisCore.Infrastructure.Deployment.BusinessRuleException("Payment exceeds the outstanding demand.");
            var installment = command.FeeInstallmentId is null ? null : demand.Installments.SingleOrDefault(i => i.Id == command.FeeInstallmentId);
            if (command.FeeInstallmentId is not null && installment is null) throw new KeyNotFoundException("Fee installment was not found.");

            var payment = new FinancePayment
            {
                Id = Guid.NewGuid(), TenantId = tenantId, FeeDemandId = demand.Id, FeeInstallmentId = installment?.Id,
                // 40 hex characters (160 bits) because the column is varchar(40); the full 64-character digest
                // overflowed it on PostgreSQL, so no gateway payment could ever be recorded there.
                IdempotencyKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{gateway}:{command.WebhookId}")))[..40].ToLowerInvariant(),
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
            db.Set<FinancePayment>().Add(payment);
            await PostFeeReceiptAsync(tenantId, payment, currency, $"Fee receipt for demand {demand.DemandNumber} ({gateway} {command.ExternalReference})", command.IsSandbox, receipt, ct);
            db.Set<PaymentGatewayEvent>().Add(new PaymentGatewayEvent
            {
                Id = Guid.NewGuid(), TenantId = tenantId, GatewaySlug = gateway, EventType = "PAYMENT_CONFIRMED",
                ReferenceId = command.ExternalReference, FinancePaymentId = payment.Id, Amount = command.Amount,
                Currency = currency, Status = "PROCESSED", IsSandbox = command.IsSandbox,
                SanitizedPayloadJson = command.SanitizedPayloadJson
            });
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            await PublishAsync(tenantId, "payment-settled", new { payment.Id, payment.FeeDemandId, payment.Amount, currency, payment.Status, payment.SettledAt, payment.GatewaySlug }, ct);
            return payment;
        });
    }

    public async Task<FinancePaymentPlanConfiguration> ConfigurePaymentPlanAsync(Guid tenantId, ConfigurePaymentPlanCommand command, CancellationToken ct)
    {
        RequireTenant(tenantId);
        var planType = NormalizePlanType(command.PlanType);
        if (command.SurchargePercent is < 0m or > 100m || command.MinimumDownPaymentPercent is < 0m or > 100m || command.LatePenaltyPercent is < 0m or > 100m || command.LatePenaltyAmount < 0m || command.MaximumDuration < 0 || command.MaximumMissedInstallments is < 1 or > 12)
            throw new ArgumentException("Payment plan configuration is invalid.");
        var configuration = await db.Set<FinancePaymentPlanConfiguration>()
            .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.PlanType == planType, ct);
        if (configuration is null)
        {
            configuration = new FinancePaymentPlanConfiguration { Id = Guid.NewGuid(), TenantId = tenantId, PlanType = planType };
            db.Set<FinancePaymentPlanConfiguration>().Add(configuration);
        }
        configuration.Enabled = command.Enabled;
        configuration.SurchargePercent = command.SurchargePercent;
        configuration.MinimumDownPaymentPercent = command.MinimumDownPaymentPercent;
        configuration.MaximumDuration = command.MaximumDuration;
        configuration.LatePenaltyAmount = command.LatePenaltyAmount;
        configuration.LatePenaltyPercent = command.LatePenaltyPercent;
        configuration.MaximumMissedInstallments = command.MaximumMissedInstallments;
        configuration.RequiresApproval = command.RequiresApproval;
        configuration.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return configuration;
    }

    public async Task<FinancePaymentPlan> CreatePaymentPlanAsync(Guid tenantId, CreatePaymentPlanCommand command, CancellationToken ct)
    {
        RequireTenant(tenantId);
        if (command.FeeDemandId == Guid.Empty || command.StartDate == default) throw new ArgumentException("Fee demand and start date are required.");
        var planType = NormalizePlanType(command.PlanType);
        var demand = await db.Set<FeeDemand>().Include(x => x.Installments)
            .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == command.FeeDemandId, ct)
            ?? throw new KeyNotFoundException("Fee demand was not found.");
        if (demand.PaidAmount > 0m || await db.Set<FinancePaymentPlan>().AnyAsync(x => x.TenantId == tenantId && x.FeeDemandId == demand.Id, ct)) throw new InvalidOperationException("A paid or planned fee demand cannot be rescheduled.");
        var configuration = await db.Set<FinancePaymentPlanConfiguration>()
            .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.PlanType == planType && x.Enabled, ct);
        if (planType != "full" && configuration is null) throw new InvalidOperationException("This payment plan is not enabled for the tenant.");
        var count = command.InstallmentCount > 0 ? command.InstallmentCount : planType switch { "full" => 1, "semester-split" => 2, "monthly" => 6, "weekly" => 16, "annual" => 1, _ => 0 };
        if (count < 1 || (configuration?.MaximumDuration > 0 && count > configuration.MaximumDuration)) throw new ArgumentException("Installment count exceeds the configured plan duration.");

        var subtotal = demand.TotalAmount;
        var surcharge = Math.Round(subtotal * (configuration?.SurchargePercent ?? 0m) / 100m, 4, MidpointRounding.ToEven);
        var total = subtotal + surcharge;
        var plan = new FinancePaymentPlan
        {
            Id = Guid.NewGuid(), TenantId = tenantId, StudentId = demand.StudentId, FeeDemandId = demand.Id,
            PlanType = planType, Status = configuration?.RequiresApproval == true ? "PENDING_APPROVAL" : "ACTIVE",
            Subtotal = subtotal, SurchargeAmount = surcharge, TotalAmount = total, InstallmentCount = count, StartDate = command.StartDate,
            ApprovedAt = configuration?.RequiresApproval == true ? null : DateTime.UtcNow
        };
        db.Set<FeeInstallment>().RemoveRange(demand.Installments);
        demand.TotalAmount = total;
        demand.OutstandingAmount = total;
        demand.Installments = FinancePaymentPlanSchedule.Build(plan, count);
        var minimumDownPayment = total * (configuration?.MinimumDownPaymentPercent ?? 0m) / 100m;
        if (demand.Installments.First().Amount < minimumDownPayment) throw new InvalidOperationException("The generated schedule does not meet the configured minimum down payment.");
        var receivable = await AccountAsync(tenantId, "1100", "Student fee receivable", AccountType.Asset, ct);
        var deferredRevenue = await AccountAsync(tenantId, "2200", "Deferred fee revenue", AccountType.Liability, ct);
        db.JournalEntries.Add(new JournalEntry
        {
            Id = Guid.NewGuid(), TenantId = tenantId, EntryNumber = $"PP-CREATE-{plan.Id:N}"[..20],
            EntryType = JournalEntryType.AutoGenerated, Description = $"Payment plan deferred revenue {plan.Id:N}", EntryDate = DateTime.UtcNow,
            TotalDebit = total, TotalCredit = total, IsPosted = true, PostedAt = DateTime.UtcNow,
            Lines =
            [
                new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = receivable.Id, Debit = total },
                new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = deferredRevenue.Id, Credit = total }
            ]
        });
        db.Set<FinancePaymentPlan>().Add(plan);
        await db.SaveChangesAsync(ct);
        return plan;
    }

    public async Task<FinancePaymentPlan> ApprovePaymentPlanAsync(Guid tenantId, Guid planId, Guid approverId, CancellationToken ct)
    {
        RequireTenant(tenantId);
        if (approverId == Guid.Empty) throw new ArgumentException("Approver identity is required.");
        var plan = await db.Set<FinancePaymentPlan>().SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == planId, ct)
            ?? throw new KeyNotFoundException("Payment plan was not found.");
        if (!string.Equals(plan.Status, "PENDING_APPROVAL", StringComparison.Ordinal)) throw new InvalidOperationException("Payment plan is not awaiting approval.");
        plan.Status = "ACTIVE";
        plan.ApprovedByUserId = approverId;
        plan.ApprovedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return plan;
    }

    public async Task<PaymentGatewayConfiguration> ConfigureGatewayAsync(Guid tenantId, Guid actorId, ConfigureGatewayCommand command, CancellationToken ct)
    {
        RequireTenant(tenantId);
        if (actorId == Guid.Empty || string.IsNullOrWhiteSpace(command.GatewaySlug) || string.IsNullOrWhiteSpace(command.SecretNamespace)) throw new ArgumentException("Gateway configuration is incomplete.");
        var configuration = await db.Set<PaymentGatewayConfiguration>()
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
        var demands = db.Set<FeeDemand>().Where(d => d.TenantId == tenantId);
        var vouchers = db.Set<FinanceVoucher>().Where(v => v.TenantId == tenantId);
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
        return (await (
            from line in db.JournalEntryLines.AsNoTracking()
            join entry in db.JournalEntries.AsNoTracking() on line.JournalEntryId equals entry.Id
            join account in db.ChartOfAccounts.AsNoTracking() on line.ChartOfAccountId equals account.Id
            where line.TenantId == tenantId && entry.TenantId == tenantId && account.TenantId == tenantId && entry.IsPosted && !entry.IsSandbox && entry.EntryDate >= periodFrom && entry.EntryDate <= periodTo
            group line by new { account.AccountCode, account.AccountName } into grouped
            orderby grouped.Key.AccountCode
            // Aggregates stay inside the projection: a `let` over the group is not translatable on PostgreSQL.
            select new { grouped.Key.AccountCode, grouped.Key.AccountName, Debit = grouped.Sum(line => line.Debit), Credit = grouped.Sum(line => line.Credit) }
        ).ToListAsync(ct)).Select(x => new TrialBalanceLine(x.AccountCode, x.AccountName, x.Debit, x.Credit, x.Debit - x.Credit)).ToList();
    }

    public async Task<ReceivablesAgingReport> GetReceivablesAgingAsync(Guid tenantId, DateTime asOf, CancellationToken ct)
    {
        RequireTenant(tenantId);
        if (asOf == default) throw new ArgumentException("Report date is required.");

        var reportDate = asOf.ToUniversalTime();
        var demands = await db.Set<FeeDemand>().AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.OutstandingAmount > 0m)
            .Select(x => new
            {
                x.Id,
                x.StudentId,
                x.DemandNumber,
                x.Currency,
                x.DueDate,
                x.OutstandingAmount
            })
            .ToListAsync(ct);

        var lines = demands.Select(demand =>
        {
            var daysOverdue = Math.Max(0, (reportDate.Date - demand.DueDate.ToUniversalTime().Date).Days);
            return new ReceivablesAgingLine(
                demand.Id,
                demand.StudentId,
                demand.DemandNumber,
                demand.Currency,
                demand.DueDate,
                daysOverdue,
                daysOverdue switch
                {
                    <= 30 => "0-30",
                    <= 60 => "31-60",
                    <= 90 => "61-90",
                    _ => "90+"
                },
                demand.OutstandingAmount);
        }).OrderByDescending(x => x.DaysOverdue).ThenByDescending(x => x.OutstandingAmount).ToList();

        var totals = new Dictionary<string, decimal>(StringComparer.Ordinal)
        {
            ["0-30"] = 0m,
            ["31-60"] = 0m,
            ["61-90"] = 0m,
            ["90+"] = 0m
        };
        foreach (var line in lines) totals[line.Bucket] += line.OutstandingAmount;
        return new ReceivablesAgingReport(reportDate, lines, totals);
    }

    public async Task<CashPositionReport> GetCashPositionAsync(Guid tenantId, DateTime asOf, CancellationToken ct)
    {
        RequireTenant(tenantId);
        if (asOf == default) throw new ArgumentException("Cash-position date is required.");
        var reportDate = asOf.ToUniversalTime();
        var lines = await (
            from line in db.JournalEntryLines.AsNoTracking()
            join entry in db.JournalEntries.AsNoTracking() on line.JournalEntryId equals entry.Id
            join account in db.ChartOfAccounts.AsNoTracking() on line.ChartOfAccountId equals account.Id
            where line.TenantId == tenantId && entry.TenantId == tenantId && account.TenantId == tenantId &&
                  entry.IsPosted && !entry.IsSandbox && entry.EntryDate <= reportDate && account.AccountCode.StartsWith("10")
            group line by new { account.AccountCode, account.AccountName } into grouped
            orderby grouped.Key.AccountCode
            // Aggregate inside the projection: a `let` over the group is not translatable to SQL.
            select new { grouped.Key.AccountCode, grouped.Key.AccountName, Balance = grouped.Sum(x => x.Debit - x.Credit) }
        ).ToListAsync(ct);
        var cashLines = lines.Select(x => new CashPositionLine(x.AccountCode, x.AccountName, x.Balance)).ToList();
        return new CashPositionReport(reportDate, cashLines, cashLines.Sum(x => x.Balance));
    }

    public async Task<FinancialPositionReport> GetFinancialPositionAsync(Guid tenantId, DateTime asOf, CancellationToken ct)
    {
        RequireTenant(tenantId);
        if (asOf == default) throw new ArgumentException("Statement date is required.");
        var balances = await AccountBalancesAsync(tenantId, DateTime.UnixEpoch, asOf.ToUniversalTime(), ct);
        var assets = balances.Where(x => x.Type == AccountType.Asset).Select(ToStatementLine).ToList();
        var liabilities = balances.Where(x => x.Type == AccountType.Liability).Select(ToStatementLine).ToList();
        var equity = balances.Where(x => x.Type == AccountType.Equity).Select(ToStatementLine).ToList();
        var context = await accounting.GetContextAsync(tenantId, ct);
        return new FinancialPositionReport(context.PrimaryStandard, asOf.ToUniversalTime(), assets, liabilities, equity, assets.Sum(x => x.Balance), liabilities.Sum(x => x.Balance), equity.Sum(x => x.Balance));
    }

    public async Task<ProfitLossReport> GetProfitLossAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct)
    {
        RequireTenant(tenantId);
        if (from == default || to == default || to < from) throw new ArgumentException("Profit and loss date range is invalid.");
        var balances = await AccountBalancesAsync(tenantId, from.ToUniversalTime(), to.ToUniversalTime(), ct);
        var revenue = balances.Where(x => x.Type == AccountType.Revenue).Select(ToStatementLine).ToList();
        var expenses = balances.Where(x => x.Type == AccountType.Expense).Select(ToStatementLine).ToList();
        var totalRevenue = revenue.Sum(x => x.Balance);
        var totalExpenses = expenses.Sum(x => x.Balance);
        var context = await accounting.GetContextAsync(tenantId, ct);
        return new ProfitLossReport(context.PrimaryStandard, from.ToUniversalTime(), to.ToUniversalTime(), revenue, expenses, totalRevenue, totalExpenses, totalRevenue - totalExpenses);
    }

    public async Task<PaymentPlanSweepResult> RunPaymentPlanSweepAsync(Guid tenantId, DateTime asOf, CancellationToken ct)
    {
        RequireTenant(tenantId);
        if (asOf == default) throw new ArgumentException("Payment-plan sweep date is required.");
        var now = asOf.ToUniversalTime();
        var plans = await db.Set<FinancePaymentPlan>()
            .Include(x => x.Demand).ThenInclude(x => x.Installments)
            .Where(x => x.TenantId == tenantId && x.Status == "ACTIVE")
            .ToListAsync(ct);
        var configurations = await db.Set<FinancePaymentPlanConfiguration>()
            .Where(x => x.TenantId == tenantId && x.Enabled)
            .ToDictionaryAsync(x => x.PlanType, StringComparer.OrdinalIgnoreCase, ct);
        var deferred = await AccountAsync(tenantId, "2200", "Deferred fee revenue", AccountType.Liability, ct);
        var feeRevenue = await AccountAsync(tenantId, "4000", "Student fee revenue", AccountType.Revenue, ct);
        var receivable = await AccountAsync(tenantId, "1100", "Student fee receivable", AccountType.Asset, ct);
        var penaltyRevenue = await AccountAsync(tenantId, "4300", "Late penalty income", AccountType.Revenue, ct);
        var processed = 0;
        var penalties = 0m;
        var recognized = 0m;
        var cancelled = 0;
        foreach (var plan in plans)
        {
            configurations.TryGetValue(plan.PlanType, out var configuration);
            foreach (var installment in plan.Demand.Installments.Where(x => x.DueDate.ToUniversalTime() <= now))
            {
                var installmentChanged = false;
                var principal = Math.Max(0m, installment.Amount - installment.PenaltyAmount);
                var toRecognize = Math.Round(Math.Max(0m, principal - installment.RecognizedAmount), 4, MidpointRounding.ToEven);
                if (toRecognize > 0m)
                {
                    var entryNumber = $"PP-REV-{installment.Id:N}"[..20];
                    if (!await db.JournalEntries.AnyAsync(x => x.TenantId == tenantId && x.EntryNumber == entryNumber, ct))
                    {
                        db.JournalEntries.Add(new JournalEntry
                        {
                            Id = Guid.NewGuid(), TenantId = tenantId, EntryNumber = entryNumber, EntryType = JournalEntryType.AutoGenerated,
                            Description = $"Payment plan revenue recognition {installment.Id:N}", EntryDate = installment.DueDate,
                            TotalDebit = toRecognize, TotalCredit = toRecognize, IsPosted = true, PostedAt = now,
                            Lines =
                            [
                                new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = deferred.Id, Debit = toRecognize },
                                new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = feeRevenue.Id, Credit = toRecognize }
                            ]
                        });
                        installment.RecognizedAmount += toRecognize;
                        recognized += toRecognize;
                        installmentChanged = true;
                    }
                }
                if (installment.PaidAmount < installment.Amount && configuration is not null && (installment.LastPenaltyAppliedOn is null || installment.LastPenaltyAppliedOn.Value.Date < now.Date))
                {
                    var firstLateDay = installment.LastPenaltyAppliedOn is null;
                    var penalty = PaymentPlanPenaltyCalculator.Calculate(installment.Amount - installment.PaidAmount, configuration.LatePenaltyAmount, configuration.LatePenaltyPercent);
                    installment.LastPenaltyAppliedOn = now.Date;
                    if (penalty > 0m)
                    {
                        var entryNumber = $"PP-PEN-{installment.Id:N}-{now:yyyyMMdd}";
                        if (!await db.JournalEntries.AnyAsync(x => x.TenantId == tenantId && x.EntryNumber == entryNumber, ct))
                        {
                            installment.Amount += penalty;
                            installment.PenaltyAmount += penalty;
                            plan.Demand.TotalAmount += penalty;
                            plan.Demand.OutstandingAmount += penalty;
                            plan.TotalAmount += penalty;
                            db.JournalEntries.Add(new JournalEntry
                            {
                                Id = Guid.NewGuid(), TenantId = tenantId, EntryNumber = entryNumber, EntryType = JournalEntryType.AutoGenerated,
                                Description = $"Payment plan late penalty {installment.Id:N}", EntryDate = now,
                                TotalDebit = penalty, TotalCredit = penalty, IsPosted = true, PostedAt = now,
                                Lines =
                                [
                                    new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = receivable.Id, Debit = penalty },
                                    new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = penaltyRevenue.Id, Credit = penalty }
                                ]
                            });
                            penalties += penalty;
                            installmentChanged = true;
                        }
                    }
                    if (firstLateDay)
                    {
                        plan.MissedInstallments++;
                        if (plan.MissedInstallments >= configuration.MaximumMissedInstallments)
                        {
                            plan.Status = "CANCELLED";
                            plan.CancelledAt = now;
                            plan.Demand.Status = "OVERDUE";
                            cancelled++;
                        }
                    }
                }
                if (installmentChanged) processed++;
            }
        }
        await db.SaveChangesAsync(ct);
        return new PaymentPlanSweepResult(plans.Count, processed, penalties, recognized, cancelled);
    }

    private async Task<List<FinanceAccountBalance>> AccountBalancesAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct)
        => await db.JournalEntryLines.AsNoTracking()
            .Join(db.JournalEntries.AsNoTracking(), line => line.JournalEntryId, entry => entry.Id, (line, entry) => new { line, entry })
            .Join(db.ChartOfAccounts.AsNoTracking(), item => item.line.ChartOfAccountId, account => account.Id, (item, account) => new { item.line, item.entry, account })
            .Where(item => item.line.TenantId == tenantId && item.entry.TenantId == tenantId && item.account.TenantId == tenantId && item.entry.IsPosted && item.entry.IsSandbox == false && item.entry.EntryDate >= from && item.entry.EntryDate <= to)
            .GroupBy(item => new { item.account.AccountCode, item.account.AccountName, item.account.AccountType })
            .Select(grouped => new FinanceAccountBalance(
                grouped.Key.AccountCode,
                grouped.Key.AccountName,
                grouped.Key.AccountType,
                grouped.Key.AccountType == AccountType.Asset || grouped.Key.AccountType == AccountType.Expense
                    ? grouped.Sum(item => item.line.Debit - item.line.Credit)
                    : grouped.Sum(item => item.line.Credit - item.line.Debit)))
            .ToListAsync(ct);

    private static FinancialStatementLine ToStatementLine(FinanceAccountBalance balance) => new(balance.Code, balance.Name, balance.Balance);

    private sealed record FinanceAccountBalance(string Code, string Name, AccountType Type, decimal Balance);

    public async Task<FinancePayrollPosting> ImportAndPostPayrollAsync(Guid tenantId, Guid actorId, ImportFinancePayrollCommand command, CancellationToken ct)
    {
        RequireTenant(tenantId);
        if (actorId == Guid.Empty || !System.Text.RegularExpressions.Regex.IsMatch(command.Period, "^\\d{4}-(0[1-9]|1[0-2])$"))
            throw new ArgumentException("Payroll period must be yyyy-MM.");
        if (command.Lines.Count == 0 || command.Lines.Any(x => x.EmployeeId == Guid.Empty || x.CostCenterId == Guid.Empty || x.GrossSalary < 0m || x.IncomeTax < 0m || x.Eobi < 0m || x.SocialSecurity < 0m || x.Gratuity < 0m || x.NetSalary < 0m))
            throw new ArgumentException("Payroll lines are invalid.");
        if (command.Lines.Any(x => Math.Round(x.GrossSalary - x.IncomeTax - x.Eobi - x.SocialSecurity - x.NetSalary, 4) != 0m))
            throw new ArgumentException("Each payroll line must reconcile gross salary to deductions and net salary.");

        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var existing = await db.FinancePayrollPostings.Include(x => x.Lines)
                .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Period == command.Period, ct);
            if (existing is not null) return existing;

            var salary = await AccountAsync(tenantId, "6000", "Salary expense", AccountType.Expense, ct);
            var tax = await AccountAsync(tenantId, "2110", "Income tax payable", AccountType.Liability, ct);
            var eobi = await AccountAsync(tenantId, "2111", "EOBI payable", AccountType.Liability, ct);
            var social = await AccountAsync(tenantId, "2112", "Social security payable", AccountType.Liability, ct);
            var gratuityExpense = await AccountAsync(tenantId, "6001", "Gratuity expense", AccountType.Expense, ct);
            var gratuity = await AccountAsync(tenantId, "2113", "Gratuity payable", AccountType.Liability, ct);
            var net = await AccountAsync(tenantId, "2114", "Net salaries payable", AccountType.Liability, ct);
            var lines = command.Lines.Select(x => new FinancePayrollPostingLine
            {
                Id = Guid.NewGuid(), TenantId = tenantId, EmployeeId = x.EmployeeId, CostCenterId = x.CostCenterId,
                GrossSalary = x.GrossSalary, IncomeTax = x.IncomeTax, Eobi = x.Eobi, SocialSecurity = x.SocialSecurity,
                Gratuity = x.Gratuity, NetSalary = x.NetSalary
            }).ToList();
            var posting = new FinancePayrollPosting
            {
                Id = Guid.NewGuid(), TenantId = tenantId, Period = command.Period, Status = "POSTED", ImportedByUserId = actorId,
                GrossSalary = lines.Sum(x => x.GrossSalary), NetSalary = lines.Sum(x => x.NetSalary), IncomeTax = lines.Sum(x => x.IncomeTax),
                Eobi = lines.Sum(x => x.Eobi), SocialSecurity = lines.Sum(x => x.SocialSecurity), Gratuity = lines.Sum(x => x.Gratuity), Lines = lines
            };
            var total = posting.GrossSalary + posting.Gratuity;
            var journal = new JournalEntry
            {
                Id = Guid.NewGuid(), TenantId = tenantId, EntryNumber = $"PAY-{command.Period}-{Guid.NewGuid():N}"[..20], EntryType = JournalEntryType.AutoGenerated,
                Description = $"Payroll posting {command.Period}", EntryDate = DateTime.UtcNow, TotalDebit = total, TotalCredit = total, IsPosted = true, PostedByUserId = actorId, PostedAt = DateTime.UtcNow,
                Lines = [
                    new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = salary.Id, Debit = posting.GrossSalary },
                    new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = gratuityExpense.Id, Debit = posting.Gratuity },
                    new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = tax.Id, Credit = posting.IncomeTax },
                    new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = eobi.Id, Credit = posting.Eobi },
                    new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = social.Id, Credit = posting.SocialSecurity },
                    new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = gratuity.Id, Credit = posting.Gratuity },
                    new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = net.Id, Credit = posting.NetSalary }
                ]
            };
            posting.JournalEntryId = journal.Id;
            db.FinancePayrollPostings.Add(posting);
            db.JournalEntries.Add(journal);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return posting;
        });
    }

    public async Task<IReadOnlyList<FinanceBankFeedTransaction>> ImportBankFeedAsync(Guid tenantId, ImportFinanceBankFeedCommand command, CancellationToken ct)
    {
        RequireTenant(tenantId);
        if (command.BankAccountId == Guid.Empty || command.Rows.Count == 0) throw new ArgumentException("Bank account and feed rows are required.");
        if (command.Rows.Any(x => string.IsNullOrWhiteSpace(x.ExternalId) || x.ExternalId.Length > 160 || x.BookingDate == default || x.Amount == 0m || string.IsNullOrWhiteSpace(x.Narration) || x.Narration.Length > 1000))
            throw new ArgumentException("Bank feed rows are invalid.");
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var account = await db.FinanceBankAccounts.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == command.BankAccountId && x.IsActive, ct)
                ?? throw new KeyNotFoundException("Bank account was not found.");
            var rows = new List<FinanceBankFeedTransaction>();
            var matchedJournalIds = new HashSet<Guid>();
            foreach (var row in command.Rows)
            {
                if (await db.FinanceBankFeedTransactions.AnyAsync(x => x.TenantId == tenantId && x.FinanceBankAccountId == account.Id && x.ExternalId == row.ExternalId, ct)) continue;
                var item = new FinanceBankFeedTransaction { Id = Guid.NewGuid(), TenantId = tenantId, FinanceBankAccountId = account.Id, ExternalId = row.ExternalId.Trim(), BookingDate = row.BookingDate, Amount = row.Amount, Narration = row.Narration.Trim() };
                var candidates = await (from line in db.JournalEntryLines.AsNoTracking()
                                        join entry in db.JournalEntries.AsNoTracking() on line.JournalEntryId equals entry.Id
                                        where line.TenantId == tenantId && entry.TenantId == tenantId && entry.IsPosted && line.ChartOfAccountId == account.ChartOfAccountId
                                            && entry.EntryDate >= row.BookingDate.AddDays(-2) && entry.EntryDate <= row.BookingDate.AddDays(2)
                                        select new { entry, movement = line.Debit - line.Credit }).ToListAsync(ct);
                var candidate = candidates.Where(x => x.movement == row.Amount && !matchedJournalIds.Contains(x.entry.Id))
                    .OrderByDescending(x => Similarity(x.entry.Description, row.Narration)).ThenBy(x => Math.Abs((x.entry.EntryDate - row.BookingDate).TotalDays)).FirstOrDefault();
                if (candidate is not null)
                {
                    item.Status = "MATCHED";
                    item.MatchedJournalEntryId = candidate.entry.Id;
                    matchedJournalIds.Add(candidate.entry.Id);
                }
                db.FinanceBankFeedTransactions.Add(item);
                rows.Add(item);
            }
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return rows;
        });
    }

    public async Task<FinanceGrant> CreateGrantAsync(Guid tenantId, CreateFinanceGrantCommand command, CancellationToken ct)
    {
        RequireTenant(tenantId);
        var currency = NormalizeCurrency(command.Currency);
        if (string.IsNullOrWhiteSpace(command.DonorName) || command.AwardAmount <= 0m || string.IsNullOrWhiteSpace(command.Purpose) || command.StartDate == default || command.EndDate <= command.StartDate)
            throw new ArgumentException("Grant fields are invalid.");
        var grant = new FinanceGrant { Id = Guid.NewGuid(), TenantId = tenantId, DonorName = command.DonorName.Trim(), Currency = currency, AwardAmount = command.AwardAmount, Purpose = command.Purpose.Trim(), StartDate = command.StartDate, EndDate = command.EndDate };
        db.FinanceGrants.Add(grant);
        await db.SaveChangesAsync(ct);
        return grant;
    }

    public async Task<FinanceGrant> RecordGrantReceiptAsync(Guid tenantId, Guid actorId, RecordFinanceGrantReceiptCommand command, CancellationToken ct)
    {
        RequireTenant(tenantId);
        if (actorId == Guid.Empty || command.GrantId == Guid.Empty || command.Amount <= 0m || string.IsNullOrWhiteSpace(command.ExternalReference) || command.ExternalReference.Length > 200)
            throw new ArgumentException("Grant receipt fields are invalid.");
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var grant = await db.FinanceGrants.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == command.GrantId, ct)
                ?? throw new KeyNotFoundException("Grant was not found.");
            if (grant.ReceivedAmount + command.Amount > grant.AwardAmount) throw new InvalidOperationException("Receipt exceeds the grant award.");
            var bank = await AccountAsync(tenantId, "1010", "Bank", AccountType.Asset, ct);
            var deferred = await AccountAsync(tenantId, "2201", "Deferred scholarship income", AccountType.Liability, ct);
            db.JournalEntries.Add(new JournalEntry
            {
                Id = Guid.NewGuid(), TenantId = tenantId, EntryNumber = $"GRANT-{tenantId:N}-{command.ExternalReference}"[..Math.Min(50, $"GRANT-{tenantId:N}-{command.ExternalReference}".Length)], EntryType = JournalEntryType.AutoGenerated,
                Description = $"Grant receipt {command.ExternalReference}", EntryDate = DateTime.UtcNow, TotalDebit = command.Amount, TotalCredit = command.Amount, IsPosted = true, PostedByUserId = actorId, PostedAt = DateTime.UtcNow,
                Lines = [new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = bank.Id, Debit = command.Amount }, new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = deferred.Id, Credit = command.Amount }]
            });
            grant.ReceivedAmount += command.Amount;
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return grant;
        });
    }

    public async Task<FinanceScholarshipAward> AwardScholarshipAsync(Guid tenantId, Guid actorId, AwardFinanceScholarshipCommand command, CancellationToken ct)
    {
        RequireTenant(tenantId);
        var method = command.DisbursementMethod.Trim().ToUpperInvariant();
        if (actorId == Guid.Empty || command.StudentId == Guid.Empty || command.Amount <= 0m || method is not ("FEE_CREDIT" or "CASH_STIPEND")) throw new ArgumentException("Scholarship award fields are invalid.");
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            FinanceGrant? grant = null;
            if (command.GrantId is not null)
            {
                grant = await db.FinanceGrants.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == command.GrantId, ct) ?? throw new KeyNotFoundException("Grant was not found.");
                if (grant.ReceivedAmount - grant.SpentAmount < command.Amount) throw new InvalidOperationException("Scholarship exceeds the available grant balance.");
            }
            var awardExpense = await AccountAsync(tenantId, method == "FEE_CREDIT" ? "6200" : "6201", method == "FEE_CREDIT" ? "Scholarship expense" : "Stipend expense", AccountType.Expense, ct);
            var credit = await AccountAsync(tenantId, method == "FEE_CREDIT" ? "1100" : "1010", method == "FEE_CREDIT" ? "Student fee receivable" : "Bank", method == "FEE_CREDIT" ? AccountType.Asset : AccountType.Asset, ct);
            var award = new FinanceScholarshipAward { Id = Guid.NewGuid(), TenantId = tenantId, StudentId = command.StudentId, GrantId = command.GrantId, ScholarshipType = command.ScholarshipType.Trim(), CoverageType = command.CoverageType.Trim().ToUpperInvariant(), Amount = command.Amount, DisbursementMethod = method, Grant = grant };
            var lines = new List<JournalEntryLine>
            {
                new() { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = awardExpense.Id, Debit = command.Amount },
                new() { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = credit.Id, Credit = command.Amount }
            };
            if (grant is not null)
            {
                var deferred = await AccountAsync(tenantId, "2201", "Deferred scholarship income", AccountType.Liability, ct);
                var grantIncome = await AccountAsync(tenantId, "4200", "Released scholarship grant income", AccountType.Revenue, ct);
                lines.Add(new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = deferred.Id, Debit = command.Amount });
                lines.Add(new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = grantIncome.Id, Credit = command.Amount });
            }
            var journalTotal = command.Amount * (grant is null ? 1m : 2m);
            var journal = new JournalEntry
            {
                Id = Guid.NewGuid(), TenantId = tenantId, EntryNumber = $"AID-{award.Id:N}"[..20], EntryType = JournalEntryType.AutoGenerated, Description = $"Scholarship award {award.Id:N}", EntryDate = DateTime.UtcNow, TotalDebit = journalTotal, TotalCredit = journalTotal, IsPosted = true, PostedByUserId = actorId, PostedAt = DateTime.UtcNow,
                Lines = lines
            };
            award.JournalEntryId = journal.Id;
            if (grant is not null) grant.SpentAmount += command.Amount;
            db.FinanceScholarshipAwards.Add(award);
            db.JournalEntries.Add(journal);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return award;
        });
    }

    public async Task<FinanceUtilityBill> CreateUtilityBillAsync(Guid tenantId, CreateFinanceUtilityBillCommand command, CancellationToken ct)
    {
        RequireTenant(tenantId);
        var currency = NormalizeCurrency(command.Currency);
        if (string.IsNullOrWhiteSpace(command.UtilityType) || string.IsNullOrWhiteSpace(command.Provider) || string.IsNullOrWhiteSpace(command.AccountNumber) || command.AmountDue <= 0m || command.BillingPeriodStart == default || command.BillingPeriodEnd < command.BillingPeriodStart || command.DueDate == default || command.Allocations.Count == 0 || command.Allocations.Any(x => x.CostCenterId == Guid.Empty || x.Amount <= 0m) || command.Allocations.Sum(x => x.Amount) != command.AmountDue)
            throw new ArgumentException("Utility bill fields and allocations are invalid.");
        var bill = new FinanceUtilityBill
        {
            Id = Guid.NewGuid(), TenantId = tenantId, UtilityType = command.UtilityType.Trim().ToUpperInvariant(), Provider = command.Provider.Trim(), AccountNumber = command.AccountNumber.Trim(), Currency = currency, AmountDue = command.AmountDue, UnitsConsumed = command.UnitsConsumed, BillingPeriodStart = command.BillingPeriodStart, BillingPeriodEnd = command.BillingPeriodEnd, DueDate = command.DueDate,
            Allocations = command.Allocations.Select(x => new FinanceUtilityAllocation { Id = Guid.NewGuid(), TenantId = tenantId, CostCenterId = x.CostCenterId, Amount = x.Amount }).ToList()
        };
        db.FinanceUtilityBills.Add(bill);
        await db.SaveChangesAsync(ct);
        return bill;
    }

    public async Task<FinanceUtilityBill> ApproveUtilityBillAsync(Guid tenantId, Guid actorId, Guid billId, CancellationToken ct)
    {
        RequireTenant(tenantId);
        if (actorId == Guid.Empty) throw new ArgumentException("Approver identity is required.");
        var bill = await db.FinanceUtilityBills.Include(x => x.Allocations).SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == billId, ct) ?? throw new KeyNotFoundException("Utility bill was not found.");
        if (bill.Status != "BILL_RECEIVED") throw new InvalidOperationException("Only received utility bills may be approved.");
        var expense = await AccountAsync(tenantId, "6300", "Utility expense", AccountType.Expense, ct);
        var payable = await AccountAsync(tenantId, "2105", "Utility payable", AccountType.Liability, ct);
        var journal = new JournalEntry
        {
            Id = Guid.NewGuid(), TenantId = tenantId, EntryNumber = $"UTIL-{bill.Id:N}"[..20], EntryType = JournalEntryType.AutoGenerated, Description = $"Utility bill {bill.Provider} {bill.BillingPeriodStart:yyyy-MM}", EntryDate = DateTime.UtcNow, TotalDebit = bill.AmountDue, TotalCredit = bill.AmountDue, IsPosted = true, PostedByUserId = actorId, PostedAt = DateTime.UtcNow,
            Lines = bill.Allocations.Select(x => new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = expense.Id, Debit = x.Amount, Description = $"{bill.UtilityType} cost centre {x.CostCenterId:D}" }).Append(new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = payable.Id, Credit = bill.AmountDue }).ToList()
        };
        bill.JournalEntryId = journal.Id;
        bill.Status = "APPROVED";
        bill.ApprovedAt = DateTime.UtcNow;
        bill.ApprovedByUserId = actorId;
        db.JournalEntries.Add(journal);
        await db.SaveChangesAsync(ct);
        return bill;
    }

    public async Task<FinanceInsurancePolicy> CreateInsurancePolicyAsync(Guid tenantId, Guid actorId, CreateFinanceInsurancePolicyCommand command, CancellationToken ct)
    {
        RequireTenant(tenantId);
        var currency = NormalizeCurrency(command.Currency);
        if (actorId == Guid.Empty || string.IsNullOrWhiteSpace(command.Insurer) || string.IsNullOrWhiteSpace(command.PolicyNumber) || command.CoverageAmount <= 0m || command.Premium <= 0m || command.StartDate == default || command.EndDate <= command.StartDate || command.RenewalDate < command.StartDate)
            throw new ArgumentException("Insurance policy fields are invalid.");
        var policy = new FinanceInsurancePolicy { Id = Guid.NewGuid(), TenantId = tenantId, Insurer = command.Insurer.Trim(), PolicyNumber = command.PolicyNumber.Trim(), PolicyType = command.PolicyType.Trim().ToUpperInvariant(), Currency = currency, CoverageAmount = command.CoverageAmount, Premium = command.Premium, StartDate = command.StartDate, EndDate = command.EndDate, RenewalDate = command.RenewalDate };
        var prepaid = await AccountAsync(tenantId, "1400", "Prepaid insurance", AccountType.Asset, ct);
        var bank = await AccountAsync(tenantId, "1010", "Bank", AccountType.Asset, ct);
        db.JournalEntries.Add(new JournalEntry
        {
            Id = Guid.NewGuid(), TenantId = tenantId, EntryNumber = $"INS-{policy.Id:N}"[..20], EntryType = JournalEntryType.AutoGenerated, Description = $"Insurance premium {policy.PolicyNumber}", EntryDate = DateTime.UtcNow, TotalDebit = policy.Premium, TotalCredit = policy.Premium, IsPosted = true, PostedByUserId = actorId, PostedAt = DateTime.UtcNow,
            Lines = [new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = prepaid.Id, Debit = policy.Premium }, new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = bank.Id, Credit = policy.Premium }]
        });
        db.FinanceInsurancePolicies.Add(policy);
        await db.SaveChangesAsync(ct);
        return policy;
    }

    public async Task<FinanceInsuranceClaim> CreateInsuranceClaimAsync(Guid tenantId, CreateFinanceInsuranceClaimCommand command, CancellationToken ct)
    {
        RequireTenant(tenantId);
        if (command.PolicyId == Guid.Empty || string.IsNullOrWhiteSpace(command.ClaimNumber) || string.IsNullOrWhiteSpace(command.Description) || command.ClaimedAmount <= 0m)
            throw new ArgumentException("Insurance claim fields are invalid.");
        if (!await db.FinanceInsurancePolicies.AnyAsync(x => x.TenantId == tenantId && x.Id == command.PolicyId, ct)) throw new KeyNotFoundException("Insurance policy was not found.");
        var claim = new FinanceInsuranceClaim { Id = Guid.NewGuid(), TenantId = tenantId, FinanceInsurancePolicyId = command.PolicyId, ClaimNumber = command.ClaimNumber.Trim(), Description = command.Description.Trim(), ClaimedAmount = command.ClaimedAmount };
        db.FinanceInsuranceClaims.Add(claim);
        await db.SaveChangesAsync(ct);
        return claim;
    }

    public async Task<FinanceInsuranceClaim> SettleInsuranceClaimAsync(Guid tenantId, Guid actorId, SettleFinanceInsuranceClaimCommand command, CancellationToken ct)
    {
        RequireTenant(tenantId);
        if (actorId == Guid.Empty || command.ClaimId == Guid.Empty || command.SettledAmount < 0m) throw new ArgumentException("Claim settlement fields are invalid.");
        var claim = await db.FinanceInsuranceClaims.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == command.ClaimId, ct) ?? throw new KeyNotFoundException("Insurance claim was not found.");
        if (claim.Status == "SETTLED") throw new InvalidOperationException("Insurance claim is already settled.");
        if (command.SettledAmount > claim.ClaimedAmount) throw new InvalidOperationException("Settlement exceeds the claim amount.");
        var bank = await AccountAsync(tenantId, "1010", "Bank", AccountType.Asset, ct);
        var receivable = await AccountAsync(tenantId, "1300", "Insurance claim receivable", AccountType.Asset, ct);
        var loss = await AccountAsync(tenantId, "6500", "Insurance claim loss", AccountType.Expense, ct);
        var lines = new List<JournalEntryLine>();
        if (command.SettledAmount > 0m) lines.Add(new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = bank.Id, Debit = command.SettledAmount });
        if (claim.ClaimedAmount > command.SettledAmount) lines.Add(new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = loss.Id, Debit = claim.ClaimedAmount - command.SettledAmount });
        lines.Add(new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = receivable.Id, Credit = claim.ClaimedAmount });
        var journal = new JournalEntry { Id = Guid.NewGuid(), TenantId = tenantId, EntryNumber = $"CLM-{claim.Id:N}"[..20], EntryType = JournalEntryType.AutoGenerated, Description = $"Insurance claim settlement {claim.ClaimNumber}", EntryDate = DateTime.UtcNow, TotalDebit = claim.ClaimedAmount, TotalCredit = claim.ClaimedAmount, IsPosted = true, PostedByUserId = actorId, PostedAt = DateTime.UtcNow, Lines = lines };
        claim.SettledAmount = command.SettledAmount;
        claim.Status = "SETTLED";
        claim.SettledAt = DateTime.UtcNow;
        claim.JournalEntryId = journal.Id;
        db.JournalEntries.Add(journal);
        await db.SaveChangesAsync(ct);
        return claim;
    }

    public async Task<int> RunInsuranceAmortizationAsync(Guid tenantId, Guid actorId, RunInsuranceAmortizationCommand command, CancellationToken ct)
    {
        RequireTenant(tenantId);
        if (actorId == Guid.Empty || command.ThroughDate == default) throw new ArgumentException("Amortization date is required.");
        var policies = await db.FinanceInsurancePolicies.Where(x => x.TenantId == tenantId && x.Status == "ACTIVE" && x.StartDate <= command.ThroughDate && x.EndDate >= x.StartDate).ToListAsync(ct);
        var prepaid = await AccountAsync(tenantId, "1400", "Prepaid insurance", AccountType.Asset, ct);
        var expense = await AccountAsync(tenantId, "6400", "Insurance expense", AccountType.Expense, ct);
        var posted = 0;
        foreach (var policy in policies)
        {
            var next = new DateTime(policy.AmortizedThrough?.Year ?? policy.StartDate.Year, policy.AmortizedThrough?.Month ?? policy.StartDate.Month, 1).AddMonths(1);
            if (policy.AmortizedThrough is null) next = new DateTime(policy.StartDate.Year, policy.StartDate.Month, 1);
            var months = ((command.ThroughDate.Year - next.Year) * 12) + command.ThroughDate.Month - next.Month + 1;
            var totalMonths = ((policy.EndDate.Year - policy.StartDate.Year) * 12) + policy.EndDate.Month - policy.StartDate.Month + 1;
            var count = Math.Clamp(months, 0, Math.Max(0, totalMonths));
            if (count == 0) continue;
            var amount = Math.Round(policy.Premium / Math.Max(1, totalMonths), 4, MidpointRounding.AwayFromZero);
            for (var index = 0; index < count; index++)
            {
                var month = next.AddMonths(index);
                var charge = count == totalMonths && index == count - 1 ? policy.Premium - amount * Math.Max(0, totalMonths - 1) : amount;
                if (charge <= 0m) continue;
                db.JournalEntries.Add(new JournalEntry { Id = Guid.NewGuid(), TenantId = tenantId, EntryNumber = $"INSA-{policy.Id:N}-{month:yyyyMM}"[..Math.Min(50, $"INSA-{policy.Id:N}-{month:yyyyMM}".Length)], EntryType = JournalEntryType.AutoGenerated, Description = $"Insurance amortization {policy.PolicyNumber} {month:yyyy-MM}", EntryDate = month, TotalDebit = charge, TotalCredit = charge, IsPosted = true, PostedByUserId = actorId, PostedAt = DateTime.UtcNow, Lines = [new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = expense.Id, Debit = charge }, new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = prepaid.Id, Credit = charge }] });
                posted++;
            }
            policy.AmortizedThrough = next.AddMonths(count - 1);
        }
        await db.SaveChangesAsync(ct);
        return posted;
    }

    public async Task<FinanceInventoryItem> CreateInventoryItemAsync(Guid tenantId, CreateFinanceInventoryItemCommand command, CancellationToken ct)
    {
        RequireTenant(tenantId);
        var method = command.CostMethod.Trim().ToUpperInvariant();
        var context = await accounting.GetContextAsync(tenantId, ct);
        var permittedMethods = JsonSerializer.Deserialize<string[]>(context.Policies["inventory_cost_methods"]) ?? [];
        if (!permittedMethods.Contains(method, StringComparer.Ordinal)) throw new InvalidOperationException("Inventory cost method is prohibited by the tenant accounting standard.");
        if (string.IsNullOrWhiteSpace(command.ItemCode) || string.IsNullOrWhiteSpace(command.ItemName) || string.IsNullOrWhiteSpace(command.Unit) || command.ReorderLevel < 0m || command.ReorderQuantity < 0m)
            throw new ArgumentException("Inventory item fields are invalid.");
        var item = new FinanceInventoryItem { Id = Guid.NewGuid(), TenantId = tenantId, ItemCode = command.ItemCode.Trim(), ItemName = command.ItemName.Trim(), Unit = command.Unit.Trim(), CostMethod = method, ReorderLevel = command.ReorderLevel, ReorderQuantity = command.ReorderQuantity };
        db.FinanceInventoryItems.Add(item);
        await db.SaveChangesAsync(ct);
        return item;
    }

    public async Task<FinanceInventoryMovement> ReceiveInventoryAsync(Guid tenantId, Guid actorId, ReceiveFinanceInventoryCommand command, CancellationToken ct)
    {
        RequireTenant(tenantId);
        if (actorId == Guid.Empty || command.ItemId == Guid.Empty || command.Quantity <= 0m || command.UnitCost <= 0m || string.IsNullOrWhiteSpace(command.Reference)) throw new ArgumentException("Inventory receipt fields are invalid.");
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var item = await db.FinanceInventoryItems.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == command.ItemId && x.Status == "ACTIVE", ct) ?? throw new KeyNotFoundException("Inventory item was not found.");
            var total = command.Quantity * command.UnitCost;
            var oldValue = item.OnHandQuantity * item.WeightedAverageCost;
            item.OnHandQuantity += command.Quantity;
            item.WeightedAverageCost = Math.Round((oldValue + total) / item.OnHandQuantity, 4, MidpointRounding.AwayFromZero);
            var inventory = await AccountAsync(tenantId, "1500", "Inventory", AccountType.Asset, ct);
            var payable = await AccountAsync(tenantId, "2105", "Inventory payable", AccountType.Liability, ct);
            var movement = new FinanceInventoryMovement { Id = Guid.NewGuid(), TenantId = tenantId, FinanceInventoryItemId = item.Id, MovementType = "RECEIPT", Quantity = command.Quantity, UnitCost = command.UnitCost, TotalCost = total, Reference = command.Reference.Trim() };
            var journal = InventoryJournal(tenantId, actorId, $"INV-R-{movement.Id:N}"[..20], $"Inventory receipt {item.ItemCode}", total, inventory.Id, payable.Id);
            movement.JournalEntryId = journal.Id;
            db.FinanceInventoryMovements.Add(movement);
            db.JournalEntries.Add(journal);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return movement;
        });
    }

    public async Task<FinanceInventoryMovement> IssueInventoryAsync(Guid tenantId, Guid actorId, IssueFinanceInventoryCommand command, CancellationToken ct)
    {
        RequireTenant(tenantId);
        if (actorId == Guid.Empty || command.ItemId == Guid.Empty || command.Quantity <= 0m || command.CostCenterId == Guid.Empty || string.IsNullOrWhiteSpace(command.Reference)) throw new ArgumentException("Inventory issue fields are invalid.");
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var item = await db.FinanceInventoryItems.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == command.ItemId && x.Status == "ACTIVE", ct) ?? throw new KeyNotFoundException("Inventory item was not found.");
            if (command.Quantity > item.OnHandQuantity) throw new InvalidOperationException("Inventory quantity is insufficient.");
            var unitCost = item.CostMethod == "FIFO" ? await FifoIssueUnitCostAsync(tenantId, item.Id, command.Quantity, ct) : item.WeightedAverageCost;
            var total = Math.Round(command.Quantity * unitCost, 4, MidpointRounding.AwayFromZero);
            item.OnHandQuantity -= command.Quantity;
            var inventory = await AccountAsync(tenantId, "1500", "Inventory", AccountType.Asset, ct);
            var expense = await AccountAsync(tenantId, "6301", "Stationery and consumables expense", AccountType.Expense, ct);
            var movement = new FinanceInventoryMovement { Id = Guid.NewGuid(), TenantId = tenantId, FinanceInventoryItemId = item.Id, MovementType = "ISSUE", Quantity = -command.Quantity, UnitCost = unitCost, TotalCost = total, CostCenterId = command.CostCenterId, Reference = command.Reference.Trim() };
            var journal = InventoryJournal(tenantId, actorId, $"INV-I-{movement.Id:N}"[..20], $"Inventory issue {item.ItemCode}", total, expense.Id, inventory.Id);
            movement.JournalEntryId = journal.Id;
            db.FinanceInventoryMovements.Add(movement);
            db.JournalEntries.Add(journal);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return movement;
        });
    }

    public async Task<FinanceInventoryMovement> CountInventoryAsync(Guid tenantId, Guid actorId, CountFinanceInventoryCommand command, CancellationToken ct)
    {
        RequireTenant(tenantId);
        if (actorId == Guid.Empty || command.ItemId == Guid.Empty || command.PhysicalQuantity < 0m || string.IsNullOrWhiteSpace(command.Reference)) throw new ArgumentException("Inventory count fields are invalid.");
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var item = await db.FinanceInventoryItems.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == command.ItemId && x.Status == "ACTIVE", ct) ?? throw new KeyNotFoundException("Inventory item was not found.");
            var difference = command.PhysicalQuantity - item.OnHandQuantity;
            var total = Math.Round(Math.Abs(difference) * item.WeightedAverageCost, 4, MidpointRounding.AwayFromZero);
            item.OnHandQuantity = command.PhysicalQuantity;
            var inventory = await AccountAsync(tenantId, "1500", "Inventory", AccountType.Asset, ct);
            var variance = await AccountAsync(tenantId, difference < 0m ? "6501" : "4501", difference < 0m ? "Inventory shrinkage" : "Inventory count gain", difference < 0m ? AccountType.Expense : AccountType.Revenue, ct);
            var movement = new FinanceInventoryMovement { Id = Guid.NewGuid(), TenantId = tenantId, FinanceInventoryItemId = item.Id, MovementType = "COUNT_ADJUSTMENT", Quantity = difference, UnitCost = item.WeightedAverageCost, TotalCost = total, Reference = command.Reference.Trim() };
            if (total > 0m)
            {
                var journal = InventoryJournal(tenantId, actorId, $"INV-C-{movement.Id:N}"[..20], $"Inventory count {item.ItemCode}", total, difference > 0m ? inventory.Id : variance.Id, difference > 0m ? variance.Id : inventory.Id);
                movement.JournalEntryId = journal.Id;
                db.JournalEntries.Add(journal);
            }
            db.FinanceInventoryMovements.Add(movement);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return movement;
        });
    }

    public async Task<FinanceFleetCostPosting> PostFleetCostAsync(Guid tenantId, Guid actorId, PostFinanceFleetCostCommand command, CancellationToken ct)
    {
        RequireTenant(tenantId);
        var type = command.CostType.Trim().ToUpperInvariant();
        if (actorId == Guid.Empty || command.VehicleId == Guid.Empty || command.CostCenterId == Guid.Empty || type is not ("FUEL" or "MAINTENANCE") || command.Amount <= 0m || command.Litres < 0m || command.Odometer < 0m || string.IsNullOrWhiteSpace(command.Description) || command.CostDate == default)
            throw new ArgumentException("Fleet cost fields are invalid.");
        if (!await db.TransportVehicles.AnyAsync(x => x.TenantId == tenantId && x.Id == command.VehicleId, ct)) throw new KeyNotFoundException("Vehicle was not found.");
        var expense = await AccountAsync(tenantId, type == "FUEL" ? "6310" : "6311", type == "FUEL" ? "Vehicle fuel expense" : "Vehicle maintenance expense", AccountType.Expense, ct);
        var payable = await AccountAsync(tenantId, "2105", "Fleet payable", AccountType.Liability, ct);
        var posting = new FinanceFleetCostPosting { Id = Guid.NewGuid(), TenantId = tenantId, TransportVehicleId = command.VehicleId, CostCenterId = command.CostCenterId, CostType = type, Amount = command.Amount, Litres = command.Litres, Odometer = command.Odometer, Description = command.Description.Trim(), CostDate = command.CostDate };
        var journal = new JournalEntry { Id = Guid.NewGuid(), TenantId = tenantId, EntryNumber = $"FLEET-{posting.Id:N}"[..20], EntryType = JournalEntryType.AutoGenerated, Description = posting.Description, EntryDate = command.CostDate, TotalDebit = command.Amount, TotalCredit = command.Amount, IsPosted = true, PostedByUserId = actorId, PostedAt = DateTime.UtcNow, Lines = [new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = expense.Id, Debit = command.Amount, Description = $"Vehicle {command.VehicleId:D}" }, new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = payable.Id, Credit = command.Amount }] };
        posting.JournalEntryId = journal.Id;
        db.FinanceFleetCostPostings.Add(posting);
        db.JournalEntries.Add(journal);
        await db.SaveChangesAsync(ct);
        return posting;
    }

    public async Task<FinanceFleetTrip> RecordFleetTripAsync(Guid tenantId, RecordFinanceFleetTripCommand command, CancellationToken ct)
    {
        RequireTenant(tenantId);
        if (command.VehicleId == Guid.Empty || command.DistanceKm <= 0m || string.IsNullOrWhiteSpace(command.Purpose) || command.TripDate == default) throw new ArgumentException("Fleet trip fields are invalid.");
        if (!await db.TransportVehicles.AnyAsync(x => x.TenantId == tenantId && x.Id == command.VehicleId, ct)) throw new KeyNotFoundException("Vehicle was not found.");
        var trip = new FinanceFleetTrip { Id = Guid.NewGuid(), TenantId = tenantId, TransportVehicleId = command.VehicleId, CostCenterId = command.CostCenterId, DistanceKm = command.DistanceKm, Purpose = command.Purpose.Trim(), FromLocation = command.FromLocation?.Trim(), ToLocation = command.ToLocation?.Trim(), TripDate = command.TripDate };
        db.FinanceFleetTrips.Add(trip);
        await db.SaveChangesAsync(ct);
        return trip;
    }

    public async Task<FinanceInvestmentFund> CreateInvestmentFundAsync(Guid tenantId, CreateFinanceInvestmentFundCommand command, CancellationToken ct)
    {
        RequireTenant(tenantId);
        var currency = NormalizeCurrency(command.Currency);
        var type = command.FundType.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(command.Name) || type is not ("ENDOWMENT" or "QUASI_ENDOWMENT" or "TERM_ENDOWMENT" or "OPERATING_RESERVE" or "SINKING_FUND" or "RESEARCH_FUND" or "PRIZE_FUND") || command.Principal < 0m || command.SpendingRatePercent < 0m || command.SpendingRatePercent > 100m || command.ReserveTarget < 0m)
            throw new ArgumentException("Investment fund fields are invalid.");
        var fund = new FinanceInvestmentFund { Id = Guid.NewGuid(), TenantId = tenantId, Name = command.Name.Trim(), FundType = type, Currency = currency, Principal = command.Principal, CurrentValue = command.Principal, SpendingRatePercent = command.SpendingRatePercent, ReserveTarget = command.ReserveTarget };
        db.FinanceInvestmentFunds.Add(fund);
        await db.SaveChangesAsync(ct);
        return fund;
    }

    public async Task<FinanceInvestmentHolding> CreateInvestmentHoldingAsync(Guid tenantId, Guid actorId, CreateFinanceInvestmentHoldingCommand command, CancellationToken ct)
    {
        RequireTenant(tenantId);
        var currency = NormalizeCurrency(command.Currency);
        var measurement = command.Measurement.Trim().ToUpperInvariant();
        if (actorId == Guid.Empty || command.FundId == Guid.Empty || string.IsNullOrWhiteSpace(command.InstrumentType) || string.IsNullOrWhiteSpace(command.Issuer) || command.CostBasis <= 0m || command.AnnualYieldPercent < 0m || command.PurchaseDate == default || command.MaturityDate < command.PurchaseDate || measurement is not ("FVTPL" or "FVOCI" or "AMORTIZED_COST"))
            throw new ArgumentException("Investment holding fields are invalid.");
        var fund = await db.FinanceInvestmentFunds.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == command.FundId && x.Status == "ACTIVE", ct) ?? throw new KeyNotFoundException("Investment fund was not found.");
        var investment = await AccountAsync(tenantId, "1600", "Investment assets", AccountType.Asset, ct);
        var bank = await AccountAsync(tenantId, "1010", "Bank", AccountType.Asset, ct);
        var holding = new FinanceInvestmentHolding { Id = Guid.NewGuid(), TenantId = tenantId, FinanceInvestmentFundId = fund.Id, InstrumentType = command.InstrumentType.Trim().ToUpperInvariant(), Issuer = command.Issuer.Trim(), AssetClass = command.AssetClass.Trim().ToUpperInvariant(), Measurement = measurement, Currency = currency, CostBasis = command.CostBasis, MarketValue = command.CostBasis, AnnualYieldPercent = command.AnnualYieldPercent, PurchaseDate = command.PurchaseDate, MaturityDate = command.MaturityDate };
        var transaction = new FinanceInvestmentTransaction { Id = Guid.NewGuid(), TenantId = tenantId, FinanceInvestmentHoldingId = holding.Id, TransactionType = "PURCHASE", Amount = command.CostBasis, Description = $"Investment purchase {holding.InstrumentType}", TransactionDate = command.PurchaseDate };
        var journal = InvestmentJournal(tenantId, actorId, $"INV-P-{holding.Id:N}"[..20], transaction.Description, command.CostBasis, investment.Id, bank.Id);
        transaction.JournalEntryId = journal.Id;
        fund.CurrentValue += command.CostBasis;
        holding.Transactions.Add(transaction);
        db.FinanceInvestmentHoldings.Add(holding);
        db.FinanceInvestmentTransactions.Add(transaction);
        db.JournalEntries.Add(journal);
        await db.SaveChangesAsync(ct);
        return holding;
    }

    public async Task<FinanceInvestmentTransaction> RecordInvestmentValuationAsync(Guid tenantId, Guid actorId, RecordInvestmentValuationCommand command, CancellationToken ct)
    {
        RequireTenant(tenantId);
        if (actorId == Guid.Empty || command.HoldingId == Guid.Empty || command.MarketValue < 0m || command.ValuationDate == default) throw new ArgumentException("Investment valuation fields are invalid.");
        var holding = await db.FinanceInvestmentHoldings.Include(x => x.Fund).SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == command.HoldingId && x.Status == "HELD", ct) ?? throw new KeyNotFoundException("Investment holding was not found.");
        var delta = command.MarketValue - holding.MarketValue;
        if (delta == 0m) throw new InvalidOperationException("Valuation does not change market value.");
        var asset = await AccountAsync(tenantId, "1600", "Investment assets", AccountType.Asset, ct);
        var gain = await AccountAsync(tenantId, delta > 0m ? "4400" : "6600", delta > 0m ? "Unrealized investment gain" : "Unrealized investment loss", delta > 0m ? AccountType.Revenue : AccountType.Expense, ct);
        var transaction = new FinanceInvestmentTransaction { Id = Guid.NewGuid(), TenantId = tenantId, FinanceInvestmentHoldingId = holding.Id, TransactionType = "VALUATION", Amount = Math.Abs(delta), GainOrLoss = delta, Description = $"Investment valuation {holding.Issuer}", TransactionDate = command.ValuationDate };
        var journal = delta > 0m ? InvestmentJournal(tenantId, actorId, $"INV-V-{transaction.Id:N}"[..20], transaction.Description, delta, asset.Id, gain.Id) : InvestmentJournal(tenantId, actorId, $"INV-V-{transaction.Id:N}"[..20], transaction.Description, -delta, gain.Id, asset.Id);
        transaction.JournalEntryId = journal.Id;
        holding.MarketValue = command.MarketValue;
        holding.Fund.CurrentValue += delta;
        db.FinanceInvestmentTransactions.Add(transaction);
        db.JournalEntries.Add(journal);
        await db.SaveChangesAsync(ct);
        return transaction;
    }

    public async Task<FinanceInvestmentTransaction> RecordInvestmentInterestAsync(Guid tenantId, Guid actorId, RecordInvestmentInterestCommand command, CancellationToken ct)
    {
        RequireTenant(tenantId);
        var context = await accounting.GetContextAsync(tenantId, ct);
        if (!JsonSerializer.Deserialize<bool>(context.Policies["interest_accounts_allowed"])) throw new InvalidOperationException("The tenant accounting policy prohibits interest income.");
        if (actorId == Guid.Empty || command.HoldingId == Guid.Empty || command.Amount <= 0m || command.InterestDate == default) throw new ArgumentException("Investment interest fields are invalid.");
        var holding = await db.FinanceInvestmentHoldings.Include(x => x.Fund).SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == command.HoldingId && x.Status == "HELD", ct) ?? throw new KeyNotFoundException("Investment holding was not found.");
        var receivable = await AccountAsync(tenantId, "1610", "Accrued investment interest", AccountType.Asset, ct);
        var income = await AccountAsync(tenantId, "4400", "Investment income", AccountType.Revenue, ct);
        var transaction = new FinanceInvestmentTransaction { Id = Guid.NewGuid(), TenantId = tenantId, FinanceInvestmentHoldingId = holding.Id, TransactionType = "INTEREST", Amount = command.Amount, Description = $"Investment interest {holding.Issuer}", TransactionDate = command.InterestDate };
        var journal = InvestmentJournal(tenantId, actorId, $"INV-I-{transaction.Id:N}"[..20], transaction.Description, command.Amount, receivable.Id, income.Id);
        transaction.JournalEntryId = journal.Id;
        holding.Fund.CurrentValue += command.Amount;
        db.FinanceInvestmentTransactions.Add(transaction);
        db.JournalEntries.Add(journal);
        await db.SaveChangesAsync(ct);
        return transaction;
    }

    public async Task<FinanceTaxRule> CreateTaxRuleAsync(Guid tenantId, CreateFinanceTaxRuleCommand command, CancellationToken ct)
    {
        RequireTenant(tenantId);
        if (string.IsNullOrWhiteSpace(command.TaxType) || string.IsNullOrWhiteSpace(command.Jurisdiction) || string.IsNullOrWhiteSpace(command.VendorCategory) || command.FilerRatePercent < 0m || command.FilerRatePercent > 100m || command.NonFilerMultiplier < 1m || command.EffectiveFrom == default || command.EffectiveTo <= command.EffectiveFrom)
            throw new ArgumentException("Tax rule fields are invalid.");
        var rule = new FinanceTaxRule { Id = Guid.NewGuid(), TenantId = tenantId, TaxType = command.TaxType.Trim().ToUpperInvariant(), Jurisdiction = command.Jurisdiction.Trim().ToUpperInvariant(), VendorCategory = command.VendorCategory.Trim().ToUpperInvariant(), FilerRatePercent = command.FilerRatePercent, NonFilerMultiplier = command.NonFilerMultiplier, EffectiveFrom = command.EffectiveFrom, EffectiveTo = command.EffectiveTo };
        db.FinanceTaxRules.Add(rule);
        await db.SaveChangesAsync(ct);
        return rule;
    }

    public async Task<FinanceWithholdingRecord> RecordWithholdingAsync(Guid tenantId, Guid actorId, RecordFinanceWithholdingCommand command, CancellationToken ct)
    {
        RequireTenant(tenantId);
        var filer = command.FilerStatus.Trim().ToUpperInvariant();
        if (actorId == Guid.Empty || string.IsNullOrWhiteSpace(command.TaxType) || !System.Text.RegularExpressions.Regex.IsMatch(command.FilingPeriod, "^\\d{4}-(0[1-9]|1[0-2])$") || string.IsNullOrWhiteSpace(command.PaymentReference) || command.PaymentReference.Length > 160 || command.GrossAmount <= 0m || filer is not ("FILER" or "NON_FILER") || command.WithheldAt == default)
            throw new ArgumentException("Withholding fields are invalid.");
        var taxType = command.TaxType.Trim().ToUpperInvariant();
        var rule = command.TaxRuleId is not null
            ? await db.FinanceTaxRules.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == command.TaxRuleId && x.IsActive, ct)
            : await db.FinanceTaxRules.Where(x => x.TenantId == tenantId && x.IsActive && x.TaxType == taxType && x.EffectiveFrom <= command.WithheldAt && (x.EffectiveTo == null || x.EffectiveTo > command.WithheldAt)).OrderByDescending(x => x.EffectiveFrom).FirstOrDefaultAsync(ct);
        if (rule is null) throw new InvalidOperationException("No active tax rule applies to this withholding.");
        var rate = filer == "NON_FILER" ? rule.FilerRatePercent * rule.NonFilerMultiplier : rule.FilerRatePercent;
        var withheld = Math.Round(command.GrossAmount * rate / 100m, 4, MidpointRounding.AwayFromZero);
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            if (await db.FinanceWithholdingRecords.AnyAsync(x => x.TenantId == tenantId && x.PaymentReference == command.PaymentReference.Trim(), ct)) throw new InvalidOperationException("Payment reference already has a withholding record.");
            var expense = await AccountAsync(tenantId, "6100", "Vendor expense", AccountType.Expense, ct);
            var taxPayable = await AccountAsync(tenantId, "2120", "Withholding tax payable", AccountType.Liability, ct);
            var vendorPayable = await AccountAsync(tenantId, "2100", "Vendor payable", AccountType.Liability, ct);
            var record = new FinanceWithholdingRecord { Id = Guid.NewGuid(), TenantId = tenantId, VendorId = command.VendorId, TaxRuleId = rule.Id, TaxType = taxType, FilingPeriod = command.FilingPeriod, PaymentReference = command.PaymentReference.Trim(), GrossAmount = command.GrossAmount, AppliedRatePercent = rate, WithheldAmount = withheld, FilerStatus = filer, WithheldAt = command.WithheldAt };
            var journal = new JournalEntry { Id = Guid.NewGuid(), TenantId = tenantId, EntryNumber = $"WHT-{record.Id:N}"[..20], EntryType = JournalEntryType.AutoGenerated, Description = $"Withholding {record.PaymentReference}", EntryDate = command.WithheldAt, TotalDebit = command.GrossAmount, TotalCredit = command.GrossAmount, IsPosted = true, PostedByUserId = actorId, PostedAt = DateTime.UtcNow, Lines = [new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = expense.Id, Debit = command.GrossAmount }, new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = taxPayable.Id, Credit = withheld }, new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = vendorPayable.Id, Credit = command.GrossAmount - withheld }] };
            record.JournalEntryId = journal.Id;
            db.FinanceWithholdingRecords.Add(record);
            db.JournalEntries.Add(journal);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return record;
        });
    }

    private static JournalEntry InvestmentJournal(Guid tenantId, Guid actorId, string entryNumber, string description, decimal amount, Guid debitAccount, Guid creditAccount)
        => new() { Id = Guid.NewGuid(), TenantId = tenantId, EntryNumber = entryNumber, EntryType = JournalEntryType.AutoGenerated, Description = description, EntryDate = DateTime.UtcNow, TotalDebit = amount, TotalCredit = amount, IsPosted = true, PostedByUserId = actorId, PostedAt = DateTime.UtcNow, Lines = [new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = debitAccount, Debit = amount }, new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = creditAccount, Credit = amount }] };

    private async Task<decimal> FifoIssueUnitCostAsync(Guid tenantId, Guid itemId, decimal quantity, CancellationToken ct)
    {
        var movements = await db.FinanceInventoryMovements.AsNoTracking().Where(x => x.TenantId == tenantId && x.FinanceInventoryItemId == itemId && (x.MovementType == "RECEIPT" || x.MovementType == "ISSUE" || x.MovementType == "COUNT_ADJUSTMENT")).OrderBy(x => x.MovementDate).ThenBy(x => x.Id).ToListAsync(ct);
        var lots = new Queue<(decimal Quantity, decimal UnitCost)>();
        foreach (var movement in movements)
        {
            if (movement.MovementType == "RECEIPT" || (movement.MovementType == "COUNT_ADJUSTMENT" && movement.Quantity > 0m)) lots.Enqueue((movement.Quantity, movement.UnitCost));
            else
            {
                var remainingIssue = Math.Abs(movement.Quantity);
                while (remainingIssue > 0m && lots.Count > 0)
                {
                    var lot = lots.Dequeue();
                    if (lot.Quantity > remainingIssue)
                    {
                        lots.Enqueue((lot.Quantity - remainingIssue, lot.UnitCost));
                        remainingIssue = 0m;
                    }
                    else remainingIssue -= lot.Quantity;
                }
            }
        }
        var remaining = quantity;
        var total = 0m;
        while (remaining > 0m && lots.Count > 0)
        {
            var lot = lots.Dequeue();
            var used = Math.Min(remaining, lot.Quantity);
            total += used * lot.UnitCost;
            remaining -= used;
            if (lot.Quantity > used) lots.Enqueue((lot.Quantity - used, lot.UnitCost));
        }
        if (remaining > 0m) throw new InvalidOperationException("Inventory quantity is insufficient.");
        return Math.Round(total / quantity, 4, MidpointRounding.AwayFromZero);
    }

    private static JournalEntry InventoryJournal(Guid tenantId, Guid actorId, string entryNumber, string description, decimal amount, Guid debitAccount, Guid creditAccount)
        => new() { Id = Guid.NewGuid(), TenantId = tenantId, EntryNumber = entryNumber, EntryType = JournalEntryType.AutoGenerated, Description = description, EntryDate = DateTime.UtcNow, TotalDebit = amount, TotalCredit = amount, IsPosted = true, PostedByUserId = actorId, PostedAt = DateTime.UtcNow, Lines = [new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = debitAccount, Debit = amount }, new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = creditAccount, Credit = amount }] };

    private static int Similarity(string? left, string right)
    {
        if (string.IsNullOrWhiteSpace(left)) return 0;
        var tokens = right.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return tokens.Count(token => left.Contains(token, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<FinanceSecurityDeposit> CreateSecurityDepositAsync(Guid tenantId, CreateSecurityDepositCommand command, CancellationToken ct)
    {
        RequireTenant(tenantId);
        if (command.StudentId == Guid.Empty || command.Amount <= 0m || command.RefundSlaDays is < 1 or > 3650 || string.IsNullOrWhiteSpace(command.DepositType))
            throw new ArgumentException("Security deposit details are invalid.");
        var currency = NormalizeCurrency(command.Currency);
        var deposit = new FinanceSecurityDeposit
        {
            Id = Guid.NewGuid(), TenantId = tenantId, StudentId = command.StudentId,
            DepositType = command.DepositType.Trim(), Currency = currency, OriginalAmount = command.Amount,
            RefundSlaDays = command.RefundSlaDays, Status = "HELD", CollectedAt = DateTime.UtcNow
        };
        var bank = await AccountAsync(tenantId, "1010", "Bank", AccountType.Asset, ct);
        var liability = await AccountAsync(tenantId, "2100", "Security deposits payable", AccountType.Liability, ct);
        AddDepositJournal(tenantId, $"SD-{deposit.Id:N}"[..20], $"Security deposit collected {deposit.Id:N}", command.Amount, bank.Id, liability.Id);
        db.Set<FinanceSecurityDeposit>().Add(deposit);
        await db.SaveChangesAsync(ct);
        return deposit;
    }

    public async Task<FinanceSecurityDeposit> RequestSecurityDepositRefundAsync(Guid tenantId, Guid depositId, CancellationToken ct)
    {
        var deposit = await DepositAsync(tenantId, depositId, ct);
        if (deposit.Status != "HELD") throw new InvalidOperationException("Only held deposits can be requested for refund.");
        deposit.Status = "REFUND_REQUESTED";
        deposit.RefundRequestedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return deposit;
    }

    public async Task<FinanceSecurityDeposit> AssessSecurityDepositAsync(Guid tenantId, Guid depositId, AssessSecurityDepositCommand command, CancellationToken ct)
    {
        var deposit = await DepositAsync(tenantId, depositId, ct);
        if (deposit.Status != "REFUND_REQUESTED") throw new InvalidOperationException("Deposit is not awaiting assessment.");
        if (command.Deductions.Any(x => x.Amount <= 0m || string.IsNullOrWhiteSpace(x.Category))) throw new ArgumentException("Deposit deductions are invalid.");
        var total = command.Deductions.Sum(x => x.Amount);
        if (total > deposit.OriginalAmount) throw new ArgumentException("Deductions exceed the deposit amount.");
        foreach (var deduction in command.Deductions)
            deposit.Deductions.Add(new FinanceSecurityDepositDeduction
            {
                Id = Guid.NewGuid(), TenantId = tenantId, SecurityDepositId = deposit.Id,
                Category = deduction.Category.Trim(), Amount = deduction.Amount, Note = deduction.Note?.Trim()
            });
        deposit.Status = "ASSESSED";
        deposit.AssessedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return deposit;
    }

    public async Task<FinanceSecurityDeposit> ResolveSecurityDepositAsync(Guid tenantId, Guid depositId, Guid actorId, ResolveSecurityDepositCommand command, CancellationToken ct)
    {
        if (actorId == Guid.Empty || string.IsNullOrWhiteSpace(command.Reason)) throw new ArgumentException("Resolver and reason are required.");
        var deposit = await DepositAsync(tenantId, depositId, ct);
        if (deposit.Status != "ASSESSED") throw new InvalidOperationException("Only assessed deposits can be resolved.");
        var deductions = deposit.Deductions.Sum(x => x.Amount);
        var remaining = deposit.OriginalAmount - deposit.RefundedAmount - deposit.ForfeitedAmount;
        if (command.Forfeit)
        {
            var forfeited = remaining;
            var liability = await AccountAsync(tenantId, "2100", "Security deposits payable", AccountType.Liability, ct);
            var income = await AccountAsync(tenantId, "4200", "Forfeited deposit income", AccountType.Revenue, ct);
            AddDepositJournal(tenantId, $"SD-F-{deposit.Id:N}"[..20], $"Security deposit forfeited {deposit.Id:N}", forfeited, liability.Id, income.Id);
            deposit.ForfeitedAmount += forfeited;
            deposit.Status = "FORFEITED";
        }
        else
        {
            var netRefund = remaining - deductions;
            if (netRefund < 0m) throw new InvalidOperationException("Deposit deductions exceed the remaining liability.");
            var liability = await AccountAsync(tenantId, "2100", "Security deposits payable", AccountType.Liability, ct);
            var bank = await AccountAsync(tenantId, "1010", "Bank", AccountType.Asset, ct);
            if (netRefund > 0m) AddDepositJournal(tenantId, $"SD-R-{deposit.Id:N}"[..20], $"Security deposit refunded {deposit.Id:N}", netRefund, liability.Id, bank.Id);
            if (deductions > 0m)
            {
                var recovery = await AccountAsync(tenantId, "4201", "Deposit damage recovery", AccountType.Revenue, ct);
                AddDepositJournal(tenantId, $"SD-D-{deposit.Id:N}"[..20], $"Security deposit deductions {deposit.Id:N}", deductions, liability.Id, recovery.Id);
            }
            deposit.RefundedAmount += netRefund;
            deposit.ForfeitedAmount += deductions;
            deposit.Status = deductions > 0m ? "PARTIALLY_REFUNDED" : "REFUNDED";
        }
        deposit.ResolvedAt = DateTime.UtcNow;
        deposit.ResolutionReason = command.Reason.Trim();
        await db.SaveChangesAsync(ct);
        return deposit;
    }

    public async Task<IReadOnlyList<AssetDepreciationLine>> RunAssetDepreciationAsync(Guid tenantId, Guid actorId, RunAssetDepreciationCommand command, CancellationToken ct)
    {
        RequireTenant(tenantId);
        if (actorId == Guid.Empty || command.PeriodEnd == default) throw new ArgumentException("Depreciation period and actor are required.");
        var periodEnd = command.PeriodEnd.ToUniversalTime().Date;
        var period = $"{periodEnd:yyyyMM}";
        var assets = await db.Assets.Where(x => x.TenantId == tenantId && x.Status == AssetStatus.Active && (x.PurchaseDate ?? x.CreatedAt) <= periodEnd).ToListAsync(ct);
        var expense = await AccountAsync(tenantId, "6100", "Depreciation expense", AccountType.Expense, ct);
        var accumulated = await AccountAsync(tenantId, "1590", "Accumulated depreciation", AccountType.Asset, ct);
        var results = new List<AssetDepreciationLine>();
        foreach (var asset in assets)
        {
            var amount = AssetDepreciationCalculator.MonthlyCharge(asset, periodEnd);
            if (amount <= 0m) continue;
            var entryNumber = $"FA-DEP-{asset.Id:N}-{period}";
            if (await db.JournalEntries.AnyAsync(x => x.TenantId == tenantId && x.EntryNumber == entryNumber, ct)) continue;
            db.JournalEntries.Add(new JournalEntry
            {
                Id = Guid.NewGuid(), TenantId = tenantId, EntryNumber = entryNumber, EntryType = JournalEntryType.AutoGenerated,
                Description = $"Monthly depreciation {asset.Name}", EntryDate = periodEnd, TotalDebit = amount, TotalCredit = amount,
                IsPosted = true, PostedByUserId = actorId, PostedAt = DateTime.UtcNow,
                Lines =
                [
                    new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = expense.Id, Debit = amount, Description = asset.Name },
                    new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = accumulated.Id, Credit = amount, Description = asset.Name }
                ]
            });
            var carryingValue = Math.Max(0m, asset.PurchaseCost - AssetDepreciationCalculator.TotalDepreciation(asset, (asset.PurchaseDate ?? asset.CreatedAt).Date, periodEnd));
            asset.CurrentValue = Math.Round(carryingValue, 2, MidpointRounding.ToEven);
            results.Add(new AssetDepreciationLine(asset.Id, amount, asset.CurrentValue, entryNumber));
        }
        await db.SaveChangesAsync(ct);
        return results;
    }

    private async Task<FinanceSecurityDeposit> DepositAsync(Guid tenantId, Guid depositId, CancellationToken ct)
        => await db.Set<FinanceSecurityDeposit>().Include(x => x.Deductions)
            .SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == depositId, ct)
            ?? throw new KeyNotFoundException("Security deposit was not found.");

    private void AddDepositJournal(Guid tenantId, string entryNumber, string description, decimal amount, Guid debitAccountId, Guid creditAccountId)
    {
        db.JournalEntries.Add(new JournalEntry
        {
            Id = Guid.NewGuid(), TenantId = tenantId, EntryNumber = entryNumber, Description = description,
            EntryType = JournalEntryType.AutoGenerated, EntryDate = DateTime.UtcNow, TotalDebit = amount, TotalCredit = amount,
            IsPosted = true, PostedAt = DateTime.UtcNow,
            Lines =
            [
                new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = debitAccountId, Debit = amount },
                new JournalEntryLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = creditAccountId, Credit = amount }
            ]
        });
    }

    private async Task<FinanceVoucher> VoucherAsync(Guid tenantId, Guid id, CancellationToken ct)
        => await db.Set<FinanceVoucher>().Include(v => v.Lines).SingleOrDefaultAsync(v => v.Id == id && v.TenantId == tenantId, ct)
            ?? throw new KeyNotFoundException("Voucher was not found.");

    private static void EnsureState(FinanceVoucher voucher, string expected)
    {
        if (!string.Equals(voucher.Status, expected, StringComparison.Ordinal)) throw new InvalidOperationException($"Voucher must be {expected}.");
    }

    private async Task<ApprovalRequirement> ApprovalRequirementAsync(Guid tenantId, string currency, decimal amount, CancellationToken ct)
    {
        var rule = await db.FinanceVoucherApprovalRules.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Currency == currency && x.IsActive && amount >= x.MinimumAmount && (x.MaximumAmount == null || amount < x.MaximumAmount))
            .OrderByDescending(x => x.MinimumAmount).FirstOrDefaultAsync(ct);
        if (rule is not null)
        {
            var roles = JsonSerializer.Deserialize<string[]>(rule.RequiredRolesJson) ?? [];
            if (roles.Length == 0) throw new InvalidOperationException("Voucher approval rule has no roles.");
            return new ApprovalRequirement(roles, rule.MinimumApprovals);
        }
        var local = currency == "PKR";
        return amount < (local ? 50_000m : 500m) ? new(["Department Head"], 1)
            : amount < (local ? 500_000m : 5_000m) ? new(["HOD", "Finance Controller"], 2)
            : amount < (local ? 5_000_000m : 50_000m) ? new(["HOD", "Finance Controller", "CFO"], 3)
            : new(["Board Member"], 3);
    }

    private sealed record ApprovalRequirement(IReadOnlyList<string> Roles, int MinimumApprovals);

    private static string NormalizeCurrency(string currency)
    {
        var normalized = currency.Trim().ToUpperInvariant();
        if (normalized.Length != 3 || normalized.Any(c => c is < 'A' or > 'Z')) throw new ArgumentException("Currency must be an ISO 4217 code.");
        return normalized;
    }

    private static string NormalizePlanType(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Replace('_', '-');
        if (normalized is not ("full" or "semester-split" or "monthly" or "weekly" or "annual" or "custom")) throw new ArgumentException("Payment plan type is invalid.");
        return normalized;
    }

    private static void RequireTenant(Guid tenantId)
    {
        if (tenantId == Guid.Empty) throw new InvalidOperationException("A tenant context is required.");
    }

    private async Task PublishAsync(Guid tenantId, string eventName, object payload, CancellationToken ct)
    {
        try { await realtime.PublishAsync(tenantId, eventName, payload, ct); }
        catch (Exception exception) { logger.LogWarning(exception, "Finance realtime event {EventName} was not delivered for tenant {TenantId}", eventName, tenantId); }
    }
}

public static class FinanceVoucherSigningPayload
{
    public static byte[] Create(Guid tenantId, Guid voucherId, string voucherNumber, decimal amount, Guid approverId, DateTime approvedAt)
        => Encoding.UTF8.GetBytes(FormattableString.Invariant($"{tenantId:D}|{voucherId:D}|{voucherNumber}|{amount:0.0000}|{approverId:D}|{approvedAt:O}"));
}
