using System.Data;
using AegisCore.Application.Finance;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Finance;

public sealed class FinanceAssetLifecycleService(AegisDbContext db)
{
    public async Task<AssetTransfer> TransferAsync(Guid tenantId, Guid actorId, Guid assetId, TransferFinanceAssetCommand command, CancellationToken ct)
    {
        var asset = await AssetAsync(tenantId, assetId, ct);
        if (actorId == Guid.Empty || command.ToLocationId == Guid.Empty || asset.Status is AssetStatus.Disposed or AssetStatus.WrittenOff) throw new ArgumentException("Asset transfer fields are invalid.");
        var from = asset.LocationRoomId ?? asset.LocationBuildingId ?? throw new InvalidOperationException("Asset has no current location.");
        if (from == command.ToLocationId) throw new InvalidOperationException("Asset is already at the destination.");
        var transfer = new AssetTransfer { Id = Guid.NewGuid(), TenantId = tenantId, AssetId = asset.Id, FromLocationId = from, ToLocationId = command.ToLocationId, TransferredByUserId = actorId };
        asset.LocationRoomId = command.ToLocationId;
        asset.Status = AssetStatus.Transferred;
        db.AssetTransfers.Add(transfer);
        await db.SaveChangesAsync(ct);
        asset.Status = AssetStatus.Active;
        await db.SaveChangesAsync(ct);
        return transfer;
    }

    public async Task<FinanceAssetAccountingEvent> RevalueAsync(Guid tenantId, Guid actorId, Guid assetId, RevalueFinanceAssetCommand command, CancellationToken ct)
    {
        var asset = await AssetAsync(tenantId, assetId, ct);
        if (actorId == Guid.Empty || command.NewCarryingValue <= 0m || command.NewCarryingValue == asset.CurrentValue) throw new ArgumentException("Revaluation fields are invalid.");
        var delta = Math.Abs(command.NewCarryingValue - asset.CurrentValue);
        var upward = command.NewCarryingValue > asset.CurrentValue;
        var debit = await AccountAsync(tenantId, upward ? "1500" : "6111", upward ? "Property plant and equipment" : "Revaluation loss", upward ? AccountType.Asset : AccountType.Expense, ct);
        var credit = await AccountAsync(tenantId, upward ? "3200" : "1591", upward ? "Revaluation surplus" : "Accumulated revaluation adjustment", upward ? AccountType.Equity : AccountType.Asset, ct);
        return await PostEventAsync(tenantId, actorId, asset, "REVALUATION", command.NewCarryingValue, 0m, command.Reason, delta, debit.Id, credit.Id, ct);
    }

    public async Task<FinanceAssetAccountingEvent> ImpairAsync(Guid tenantId, Guid actorId, Guid assetId, ImpairFinanceAssetCommand command, CancellationToken ct)
    {
        var asset = await AssetAsync(tenantId, assetId, ct);
        if (actorId == Guid.Empty || command.ImpairmentAmount <= 0m || command.ImpairmentAmount >= asset.CurrentValue) throw new ArgumentException("Impairment must be positive and below carrying value.");
        var expense = await AccountAsync(tenantId, "6110", "Asset impairment expense", AccountType.Expense, ct);
        var accumulated = await AccountAsync(tenantId, "1592", "Accumulated impairment", AccountType.Asset, ct);
        return await PostEventAsync(tenantId, actorId, asset, "IMPAIRMENT", asset.CurrentValue - command.ImpairmentAmount, 0m, command.Reason, command.ImpairmentAmount, expense.Id, accumulated.Id, ct);
    }

    public async Task<FinanceAssetAccountingEvent> DisposeAsync(Guid tenantId, Guid actorId, Guid assetId, DisposeFinanceAssetCommand command, CancellationToken ct)
    {
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            var asset = await AssetAsync(tenantId, assetId, ct);
            if (actorId == Guid.Empty || command.Proceeds < 0m || !Enum.TryParse<DisposalMethod>(command.Method, true, out var method)) throw new ArgumentException("Disposal fields are invalid.");
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var bank = await AccountAsync(tenantId, "1010", "Bank", AccountType.Asset, ct);
            var assetAccount = await AccountAsync(tenantId, "1500", "Property plant and equipment", AccountType.Asset, ct);
            var accumulatedAccount = await AccountAsync(tenantId, "1590", "Accumulated depreciation", AccountType.Asset, ct);
            var gainAccount = await AccountAsync(tenantId, "4210", "Gain on asset disposal", AccountType.Revenue, ct);
            var lossAccount = await AccountAsync(tenantId, "6112", "Loss on asset disposal", AccountType.Expense, ct);
            var carrying = asset.CurrentValue;
            var accumulated = Math.Max(0m, asset.PurchaseCost - carrying);
            var difference = command.Proceeds - carrying;
            var lines = new List<JournalEntryLine>();
            if (command.Proceeds > 0m) lines.Add(Line(tenantId, bank.Id, command.Proceeds, 0m));
            if (accumulated > 0m) lines.Add(Line(tenantId, accumulatedAccount.Id, accumulated, 0m));
            if (difference < 0m) lines.Add(Line(tenantId, lossAccount.Id, -difference, 0m));
            lines.Add(Line(tenantId, assetAccount.Id, 0m, asset.PurchaseCost));
            if (difference > 0m) lines.Add(Line(tenantId, gainAccount.Id, 0m, difference));
            var total = lines.Sum(x => x.Debit);
            var journal = Journal(tenantId, actorId, $"FA-DIS-{asset.Id:N}", $"Asset disposal: {asset.Name}", total, lines);
            db.JournalEntries.Add(journal);
            db.AssetDisposals.Add(new AssetDisposal { Id = Guid.NewGuid(), TenantId = tenantId, AssetId = asset.Id, Method = method, Status = DisposalStatus.Completed, SaleValue = command.Proceeds, DisposalDate = DateTime.UtcNow });
            asset.CurrentValue = 0m;
            asset.Status = method == DisposalMethod.WriteOff ? AssetStatus.WrittenOff : AssetStatus.Disposed;
            var accountingEvent = Event(tenantId, actorId, asset.Id, "DISPOSAL", carrying, 0m, command.Proceeds, command.Reason, journal.Id);
            db.FinanceAssetAccountingEvents.Add(accountingEvent);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return accountingEvent;
        });
    }

    public async Task<VerificationCycle> CreateVerificationCycleAsync(Guid tenantId, CreateAssetVerificationCycleCommand command, CancellationToken ct)
    {
        if (tenantId == Guid.Empty || string.IsNullOrWhiteSpace(command.Name) || command.Name.Length > 200 || command.EndDate <= command.StartDate) throw new ArgumentException("Verification cycle fields are invalid.");
        var assetIds = await db.Assets.Where(x => x.TenantId == tenantId && x.Status != AssetStatus.Disposed && x.Status != AssetStatus.WrittenOff).Select(x => x.Id).ToArrayAsync(ct);
        var cycle = new VerificationCycle
        {
            Id = Guid.NewGuid(), TenantId = tenantId, Name = command.Name.Trim(), StartDate = command.StartDate, EndDate = command.EndDate,
            Status = VerificationCycleStatus.InProgress,
            Verifications = assetIds.Select(id => new AssetVerification { Id = Guid.NewGuid(), TenantId = tenantId, AssetId = id, Status = VerificationStatus.Pending }).ToList()
        };
        db.VerificationCycles.Add(cycle);
        await db.SaveChangesAsync(ct);
        return cycle;
    }

    public async Task<AssetVerification> VerifyAsync(Guid tenantId, Guid actorId, Guid cycleId, Guid assetId, VerifyFinanceAssetCommand command, CancellationToken ct)
    {
        if (actorId == Guid.Empty || !Enum.TryParse<VerificationStatus>(command.Status, true, out var status) || status == VerificationStatus.Pending) throw new ArgumentException("Verification status or actor is invalid.");
        var verification = await db.AssetVerifications.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.VerificationCycleId == cycleId && x.AssetId == assetId, ct)
            ?? throw new KeyNotFoundException("Asset verification assignment was not found.");
        if (verification.Status != VerificationStatus.Pending) throw new InvalidOperationException("Asset has already been verified in this cycle.");
        verification.Status = status;
        verification.VerifiedByUserId = actorId;
        verification.VerifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return verification;
    }

    private async Task<FinanceAssetAccountingEvent> PostEventAsync(Guid tenantId, Guid actorId, Asset asset, string type, decimal newValue, decimal proceeds, string reason, decimal amount, Guid debitId, Guid creditId, CancellationToken ct)
    {
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var previous = asset.CurrentValue;
            var journal = Journal(tenantId, actorId, $"FA-{type[..3]}-{asset.Id:N}-{DateTime.UtcNow:yyyyMMddHHmmss}", $"{type}: {asset.Name}", amount, [Line(tenantId, debitId, amount, 0m), Line(tenantId, creditId, 0m, amount)]);
            db.JournalEntries.Add(journal);
            asset.CurrentValue = newValue;
            var accountingEvent = Event(tenantId, actorId, asset.Id, type, previous, newValue, proceeds, reason, journal.Id);
            db.FinanceAssetAccountingEvents.Add(accountingEvent);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return accountingEvent;
        });
    }

    private async Task<Asset> AssetAsync(Guid tenantId, Guid id, CancellationToken ct)
        => await db.Assets.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id && x.Status != AssetStatus.Disposed && x.Status != AssetStatus.WrittenOff, ct)
           ?? throw new KeyNotFoundException("Active asset was not found.");

    private async Task<ChartOfAccount> AccountAsync(Guid tenantId, string code, string name, AccountType type, CancellationToken ct)
    {
        var account = await db.ChartOfAccounts.SingleOrDefaultAsync(x => x.TenantId == tenantId && x.AccountCode == code, ct);
        if (account is not null) return account;
        account = new ChartOfAccount { Id = Guid.NewGuid(), TenantId = tenantId, AccountCode = code, AccountName = name, AccountType = type };
        db.ChartOfAccounts.Add(account);
        return account;
    }

    private static JournalEntryLine Line(Guid tenantId, Guid accountId, decimal debit, decimal credit)
        => new() { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = accountId, Debit = debit, Credit = credit };

    private static JournalEntry Journal(Guid tenantId, Guid actorId, string number, string description, decimal total, List<JournalEntryLine> lines)
        => new() { Id = Guid.NewGuid(), TenantId = tenantId, EntryNumber = number[..Math.Min(50, number.Length)], EntryType = JournalEntryType.AutoGenerated, Description = description, EntryDate = DateTime.UtcNow, TotalDebit = total, TotalCredit = total, IsPosted = true, PostedByUserId = actorId, PostedAt = DateTime.UtcNow, Lines = lines };

    private static FinanceAssetAccountingEvent Event(Guid tenantId, Guid actorId, Guid assetId, string type, decimal previous, decimal current, decimal proceeds, string reason, Guid journalId)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 1000) throw new ArgumentException("An event reason is required and must not exceed 1000 characters.");
        return new FinanceAssetAccountingEvent { Id = Guid.NewGuid(), TenantId = tenantId, AssetId = assetId, EventType = type, PreviousCarryingValue = previous, NewCarryingValue = current, Proceeds = proceeds, Reason = reason.Trim(), ActorUserId = actorId, JournalEntryId = journalId };
    }
}
