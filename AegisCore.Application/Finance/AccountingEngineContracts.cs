using AegisCore.Domain.Entities;

namespace AegisCore.Application.Finance;

public sealed record SwitchAccountingStandardCommand(string TargetStandard, DateTime EffectiveAt, DateTime ComparativeFrom, DateTime ComparativeTo);
public sealed record CreateReportingEntityCommand(string Code, string Name, string FunctionalCurrency, Guid? ParentEntityId);
public sealed record CreateRestatementRuleCommand(string SourceStandard, string TargetStandard, Guid SourceAccountId, Guid TargetAccountId, decimal Factor, string Rationale);
public sealed record RunRestatementCommand(string SourceStandard, string TargetStandard, DateTime PeriodFrom, DateTime PeriodTo);
public sealed record CreateConsolidationGroupCommand(string Name, string PresentationCurrency, IReadOnlyList<Guid> ReportingEntityIds);
public sealed record CreateEliminationRuleCommand(Guid ConsolidationGroupId, Guid SourceAccountId, Guid CounterpartAccountId, string Description);
public sealed record RunConsolidationCommand(Guid ConsolidationGroupId, DateTime PeriodFrom, DateTime PeriodTo);
public sealed record AccountingStandardContext(string PrimaryStandard, IReadOnlyDictionary<string, string> Policies);

public interface IAccountingStandardsEngine
{
    Task<AccountingStandardContext> GetContextAsync(Guid tenantId, CancellationToken ct);
    Task<FinanceReportingEntity> CreateReportingEntityAsync(Guid tenantId, CreateReportingEntityCommand command, CancellationToken ct);
    Task<FinanceRestatementRule> CreateRestatementRuleAsync(Guid tenantId, CreateRestatementRuleCommand command, CancellationToken ct);
    Task<FinanceRestatementRun> RunRestatementAsync(Guid tenantId, Guid actorId, RunRestatementCommand command, CancellationToken ct);
    Task<FinanceRestatementRun> SwitchStandardAsync(Guid tenantId, Guid actorId, SwitchAccountingStandardCommand command, CancellationToken ct);
    Task<FinanceConsolidationGroup> CreateConsolidationGroupAsync(Guid tenantId, CreateConsolidationGroupCommand command, CancellationToken ct);
    Task<FinanceEliminationRule> CreateEliminationRuleAsync(Guid tenantId, CreateEliminationRuleCommand command, CancellationToken ct);
    Task<FinanceConsolidationRun> RunConsolidationAsync(Guid tenantId, Guid actorId, RunConsolidationCommand command, CancellationToken ct);
}
