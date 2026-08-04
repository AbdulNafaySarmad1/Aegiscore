using System.Reflection;
using System.Text.Json;
using AegisCore.Application.Finance;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Finance;

public sealed class AccountingStandardsEngine(AegisDbContext db) : IAccountingStandardsEngine
{
    public async Task<AccountingStandardContext> GetContextAsync(Guid tenantId, CancellationToken ct)
    {
        RequireTenant(tenantId);
        await EnsureCatalogAsync(tenantId, ct);
        var config = await db.TenantFinanceConfigurations.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.TenantId == tenantId, ct)
            ?? throw new InvalidOperationException("Tenant finance configuration is required.");
        var policy = await PolicyAsync(tenantId, config.PrimaryStandard, ct);
        return new AccountingStandardContext(Normalize(config.PrimaryStandard), ParsePolicy(policy.ValueJson));
    }

    public async Task<FinanceReportingEntity> CreateReportingEntityAsync(Guid tenantId, CreateReportingEntityCommand command, CancellationToken ct)
    {
        RequireTenant(tenantId);
        var code = command.Code.Trim().ToUpperInvariant();
        var currency = command.FunctionalCurrency.Trim().ToUpperInvariant();
        if (code.Length is < 2 or > 30 || string.IsNullOrWhiteSpace(command.Name) || currency.Length != 3)
            throw new ArgumentException("Reporting entity fields are invalid.");
        if (command.ParentEntityId.HasValue && !await db.FinanceReportingEntities.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.Id == command.ParentEntityId, ct))
            throw new ArgumentException("Parent reporting entity does not exist.");
        if (await db.FinanceReportingEntities.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.Code == code, ct))
            throw new InvalidOperationException("Reporting entity code already exists.");
        var entity = new FinanceReportingEntity { Id = Guid.NewGuid(), TenantId = tenantId, Code = code, Name = command.Name.Trim(), FunctionalCurrency = currency, ParentEntityId = command.ParentEntityId };
        db.FinanceReportingEntities.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<FinanceRestatementRule> CreateRestatementRuleAsync(Guid tenantId, CreateRestatementRuleCommand command, CancellationToken ct)
    {
        RequireTenant(tenantId);
        await EnsureCatalogAsync(tenantId, ct);
        var source = Normalize(command.SourceStandard);
        var target = Normalize(command.TargetStandard);
        await PolicyAsync(tenantId, source, ct);
        await PolicyAsync(tenantId, target, ct);
        if (source == target || command.SourceAccountId == command.TargetAccountId || command.Factor == 0m || string.IsNullOrWhiteSpace(command.Rationale))
            throw new ArgumentException("Restatement rule is invalid.");
        var accountCount = await db.ChartOfAccounts.IgnoreQueryFilters().CountAsync(x => x.TenantId == tenantId && (x.Id == command.SourceAccountId || x.Id == command.TargetAccountId), ct);
        if (accountCount != 2) throw new ArgumentException("Restatement accounts must belong to the tenant.");
        var rule = new FinanceRestatementRule { Id = Guid.NewGuid(), TenantId = tenantId, SourceStandard = source, TargetStandard = target, SourceAccountId = command.SourceAccountId, TargetAccountId = command.TargetAccountId, Factor = command.Factor, Rationale = command.Rationale.Trim() };
        db.FinanceRestatementRules.Add(rule);
        await db.SaveChangesAsync(ct);
        return rule;
    }

    public async Task<FinanceRestatementRun> RunRestatementAsync(Guid tenantId, Guid actorId, RunRestatementCommand command, CancellationToken ct)
    {
        RequireTenant(tenantId);
        if (actorId == Guid.Empty || command.PeriodFrom == default || command.PeriodTo < command.PeriodFrom) throw new ArgumentException("Restatement period and actor are required.");
        await EnsureCatalogAsync(tenantId, ct);
        var source = Normalize(command.SourceStandard);
        var target = Normalize(command.TargetStandard);
        await PolicyAsync(tenantId, source, ct);
        await PolicyAsync(tenantId, target, ct);
        var rules = await db.FinanceRestatementRules.IgnoreQueryFilters().Where(x => x.TenantId == tenantId && x.SourceStandard == source && x.TargetStandard == target && x.IsActive).ToListAsync(ct);
        var sourceIds = rules.Select(x => x.SourceAccountId).Distinct().ToArray();
        var balances = await (from line in db.JournalEntryLines.IgnoreQueryFilters()
                              join entry in db.JournalEntries.IgnoreQueryFilters() on line.JournalEntryId equals entry.Id
                              where line.TenantId == tenantId && entry.TenantId == tenantId && entry.IsPosted && !entry.IsSandbox && entry.EntryDate >= command.PeriodFrom.ToUniversalTime() && entry.EntryDate <= command.PeriodTo.ToUniversalTime() && sourceIds.Contains(line.ChartOfAccountId)
                              group line by line.ChartOfAccountId into grouped
                              select new { AccountId = grouped.Key, Balance = grouped.Sum(x => x.Debit - x.Credit) }).ToDictionaryAsync(x => x.AccountId, x => x.Balance, ct);
        var run = new FinanceRestatementRun { Id = Guid.NewGuid(), TenantId = tenantId, SourceStandard = source, TargetStandard = target, PeriodFrom = command.PeriodFrom.ToUniversalTime(), PeriodTo = command.PeriodTo.ToUniversalTime(), CreatedByUserId = actorId };
        foreach (var rule in rules)
        {
            var adjustment = balances.GetValueOrDefault(rule.SourceAccountId) * rule.Factor;
            if (adjustment == 0m) continue;
            var amount = Math.Abs(decimal.Round(adjustment, 4, MidpointRounding.ToEven));
            run.Lines.Add(new FinanceRestatementMemoLine { Id = Guid.NewGuid(), TenantId = tenantId, RestatementRuleId = rule.Id, ChartOfAccountId = rule.SourceAccountId, Debit = adjustment < 0m ? amount : 0m, Credit = adjustment > 0m ? amount : 0m, Description = rule.Rationale });
            run.Lines.Add(new FinanceRestatementMemoLine { Id = Guid.NewGuid(), TenantId = tenantId, RestatementRuleId = rule.Id, ChartOfAccountId = rule.TargetAccountId, Debit = adjustment > 0m ? amount : 0m, Credit = adjustment < 0m ? amount : 0m, Description = rule.Rationale });
        }
        db.FinanceRestatementRuns.Add(run);
        await db.SaveChangesAsync(ct);
        return run;
    }

    public async Task<FinanceRestatementRun> SwitchStandardAsync(Guid tenantId, Guid actorId, SwitchAccountingStandardCommand command, CancellationToken ct)
    {
        RequireTenant(tenantId);
        await EnsureCatalogAsync(tenantId, ct);
        var config = await db.TenantFinanceConfigurations.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.TenantId == tenantId, ct)
            ?? throw new InvalidOperationException("Tenant finance configuration is required.");
        var source = Normalize(config.PrimaryStandard);
        var target = Normalize(command.TargetStandard);
        if (source == target) throw new InvalidOperationException("Target standard is already active.");
        await PolicyAsync(tenantId, target, ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var run = await RunRestatementAsync(tenantId, actorId, new RunRestatementCommand(source, target, command.ComparativeFrom, command.ComparativeTo), ct);
        config.PrimaryStandard = target;
        config.UpdatedAt = command.EffectiveAt.ToUniversalTime();
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return run;
    }

    public async Task<FinanceConsolidationGroup> CreateConsolidationGroupAsync(Guid tenantId, CreateConsolidationGroupCommand command, CancellationToken ct)
    {
        RequireTenant(tenantId);
        var memberIds = command.ReportingEntityIds.Distinct().ToArray();
        var currency = command.PresentationCurrency.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(command.Name) || currency.Length != 3 || memberIds.Length == 0) throw new ArgumentException("Consolidation group fields are invalid.");
        if (await db.FinanceReportingEntities.IgnoreQueryFilters().CountAsync(x => x.TenantId == tenantId && x.IsActive && memberIds.Contains(x.Id), ct) != memberIds.Length)
            throw new ArgumentException("Every consolidation member must be an active tenant reporting entity.");
        var group = new FinanceConsolidationGroup { Id = Guid.NewGuid(), TenantId = tenantId, Name = command.Name.Trim(), PresentationCurrency = currency, Members = memberIds.Select(id => new FinanceConsolidationMember { Id = Guid.NewGuid(), TenantId = tenantId, ReportingEntityId = id, EffectiveFrom = DateTime.UtcNow }).ToList() };
        db.FinanceConsolidationGroups.Add(group);
        await db.SaveChangesAsync(ct);
        return group;
    }

    public async Task<FinanceEliminationRule> CreateEliminationRuleAsync(Guid tenantId, CreateEliminationRuleCommand command, CancellationToken ct)
    {
        RequireTenant(tenantId);
        if (string.IsNullOrWhiteSpace(command.Description) || !await db.FinanceConsolidationGroups.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId && x.Id == command.ConsolidationGroupId, ct))
            throw new ArgumentException("Elimination rule is invalid.");
        var accountCount = await db.ChartOfAccounts.IgnoreQueryFilters().CountAsync(x => x.TenantId == tenantId && (x.Id == command.SourceAccountId || x.Id == command.CounterpartAccountId), ct);
        if (accountCount != (command.SourceAccountId == command.CounterpartAccountId ? 1 : 2)) throw new ArgumentException("Elimination accounts must belong to the tenant.");
        var rule = new FinanceEliminationRule { Id = Guid.NewGuid(), TenantId = tenantId, ConsolidationGroupId = command.ConsolidationGroupId, SourceAccountId = command.SourceAccountId, CounterpartAccountId = command.CounterpartAccountId, Description = command.Description.Trim() };
        db.FinanceEliminationRules.Add(rule);
        await db.SaveChangesAsync(ct);
        return rule;
    }

    public async Task<FinanceConsolidationRun> RunConsolidationAsync(Guid tenantId, Guid actorId, RunConsolidationCommand command, CancellationToken ct)
    {
        RequireTenant(tenantId);
        if (actorId == Guid.Empty || command.PeriodFrom == default || command.PeriodTo < command.PeriodFrom) throw new ArgumentException("Consolidation period and actor are required.");
        var members = await db.FinanceConsolidationMembers.IgnoreQueryFilters().Where(x => x.TenantId == tenantId && x.ConsolidationGroupId == command.ConsolidationGroupId && x.EffectiveFrom <= command.PeriodTo && (!x.EffectiveTo.HasValue || x.EffectiveTo >= command.PeriodFrom)).Select(x => x.ReportingEntityId).ToListAsync(ct);
        if (members.Count == 0) throw new InvalidOperationException("Consolidation group has no effective members.");
        var rules = await db.FinanceEliminationRules.IgnoreQueryFilters().Where(x => x.TenantId == tenantId && x.ConsolidationGroupId == command.ConsolidationGroupId && x.IsActive).ToListAsync(ct);
        var eliminationAccounts = rules.SelectMany(x => new[] { x.SourceAccountId, x.CounterpartAccountId }).ToHashSet();
        var lines = await (from line in db.JournalEntryLines.IgnoreQueryFilters()
                           join entry in db.JournalEntries.IgnoreQueryFilters() on line.JournalEntryId equals entry.Id
                           where line.TenantId == tenantId && entry.TenantId == tenantId && entry.IsPosted && !entry.IsSandbox && entry.EntryDate >= command.PeriodFrom.ToUniversalTime() && entry.EntryDate <= command.PeriodTo.ToUniversalTime() && line.ReportingEntityId.HasValue && members.Contains(line.ReportingEntityId.Value)
                           select new { line.ChartOfAccountId, line.ReportingEntityId, line.CounterpartyReportingEntityId, line.Debit, line.Credit }).ToListAsync(ct);
        var run = new FinanceConsolidationRun { Id = Guid.NewGuid(), TenantId = tenantId, ConsolidationGroupId = command.ConsolidationGroupId, PeriodFrom = command.PeriodFrom.ToUniversalTime(), PeriodTo = command.PeriodTo.ToUniversalTime(), CreatedByUserId = actorId };
        foreach (var accountGroup in lines.GroupBy(x => x.ChartOfAccountId))
        {
            var ledger = accountGroup.Sum(x => x.Debit - x.Credit);
            var interEntity = eliminationAccounts.Contains(accountGroup.Key)
                ? accountGroup.Where(x => x.CounterpartyReportingEntityId.HasValue && members.Contains(x.CounterpartyReportingEntityId.Value) && x.CounterpartyReportingEntityId != x.ReportingEntityId).Sum(x => x.Debit - x.Credit)
                : 0m;
            run.Lines.Add(new FinanceConsolidationMemoLine { Id = Guid.NewGuid(), TenantId = tenantId, ChartOfAccountId = accountGroup.Key, LedgerBalance = ledger, EliminationAmount = -interEntity, ConsolidatedBalance = ledger - interEntity });
        }
        db.FinanceConsolidationRuns.Add(run);
        await db.SaveChangesAsync(ct);
        return run;
    }

    private async Task EnsureCatalogAsync(Guid tenantId, CancellationToken ct)
    {
        if (await db.FinanceAccountingStandardPolicies.IgnoreQueryFilters().AnyAsync(x => x.TenantId == tenantId, ct)) return;
        var assembly = typeof(AccountingStandardsEngine).Assembly;
        var resource = assembly.GetManifestResourceNames().Single(x => x.EndsWith("accounting-standards.json", StringComparison.Ordinal));
        await using var stream = assembly.GetManifestResourceStream(resource) ?? throw new InvalidOperationException("Accounting standards catalog is unavailable.");
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        foreach (var standard in document.RootElement.EnumerateObject())
            db.FinanceAccountingStandardPolicies.Add(new FinanceAccountingStandardPolicy { Id = Guid.NewGuid(), TenantId = tenantId, StandardCode = Normalize(standard.Name), PolicyCode = "profile", ValueJson = standard.Value.GetRawText(), EffectiveFrom = DateTime.UnixEpoch });
        await db.SaveChangesAsync(ct);
    }

    private async Task<FinanceAccountingStandardPolicy> PolicyAsync(Guid tenantId, string standard, CancellationToken ct)
        => await db.FinanceAccountingStandardPolicies.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.TenantId == tenantId && x.StandardCode == Normalize(standard) && x.PolicyCode == "profile" && !x.EffectiveTo.HasValue, ct)
           ?? throw new ArgumentException("Accounting standard is not configured for this tenant.");

    private static IReadOnlyDictionary<string, string> ParsePolicy(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.EnumerateObject().ToDictionary(x => x.Name, x => x.Value.GetRawText(), StringComparer.Ordinal);
    }

    private static string Normalize(string value) => value.Trim().ToUpperInvariant().Replace(' ', '_');
    private static void RequireTenant(Guid tenantId) { if (tenantId == Guid.Empty) throw new ArgumentException("Tenant is required."); }
}
