using AegisCore.Domain.Entities;

namespace AegisCore.Application.Finance;

public static class FinanceStates
{
    public const string Draft = "DRAFT";
    public const string PendingApproval = "PENDING_APPROVAL";
    public const string Approved = "APPROVED";
    public const string Posted = "POSTED";
    public const string Rejected = "REJECTED";
}

public sealed record VoucherLineCommand(Guid ChartOfAccountId, Guid? CostCenterId, decimal Debit, decimal Credit, string? Description);
public sealed record CreateVoucherCommand(string VoucherType, string Currency, string? Description, IReadOnlyList<VoucherLineCommand> Lines);
public sealed record VoucherDecisionCommand(bool Approve, string? Note);
public sealed record CreateFeeDemandCommand(Guid StudentId, string Currency, decimal TotalAmount, DateTime DueDate, IReadOnlyList<FeeInstallmentCommand>? Installments);
public sealed record FeeInstallmentCommand(decimal Amount, DateTime DueDate);
public sealed record RecordFinancePaymentCommand(Guid FeeDemandId, Guid? FeeInstallmentId, string Method, decimal Amount, string IdempotencyKey, string? GatewaySlug, string? ExternalReference);
public sealed record RecordGatewayPaymentCommand(Guid FeeDemandId, Guid? FeeInstallmentId, decimal Amount, string Currency, string WebhookId, string ExternalReference, string SanitizedPayloadJson, bool IsSandbox);
public sealed record ConfigurePaymentPlanCommand(string PlanType, bool Enabled, decimal SurchargePercent, decimal MinimumDownPaymentPercent, int MaximumDuration, decimal LatePenaltyAmount, decimal LatePenaltyPercent, int MaximumMissedInstallments, bool RequiresApproval);
public sealed record CreatePaymentPlanCommand(Guid FeeDemandId, string PlanType, DateTime StartDate, int InstallmentCount);
public sealed record ConfigureGatewayCommand(string GatewaySlug, string Environment, string SecretNamespace, bool IsActive);
public sealed record FinanceDashboardSummary(decimal OpenReceivables, decimal PostedThisPeriod, decimal PendingApprovalAmount, int OpenDemandCount, int PendingVoucherCount);
public sealed record TrialBalanceLine(string AccountCode, string AccountName, decimal Debit, decimal Credit, decimal Balance);

public interface IFinanceModuleService
{
    Task<FinanceVoucher> CreateVoucherAsync(Guid tenantId, Guid makerId, CreateVoucherCommand command, CancellationToken ct);
    Task<FinanceVoucher> SubmitVoucherAsync(Guid tenantId, Guid voucherId, Guid actorId, CancellationToken ct);
    Task<FinanceVoucher> DecideVoucherAsync(Guid tenantId, Guid voucherId, Guid actorId, VoucherDecisionCommand command, CancellationToken ct);
    Task<FinanceVoucher> PostVoucherAsync(Guid tenantId, Guid voucherId, Guid actorId, CancellationToken ct);
    Task<FeeDemand> CreateFeeDemandAsync(Guid tenantId, CreateFeeDemandCommand command, CancellationToken ct);
    Task<FinancePayment> RecordPaymentAsync(Guid tenantId, RecordFinancePaymentCommand command, CancellationToken ct);
    Task<FinancePayment> RecordGatewayPaymentAsync(Guid tenantId, string gatewaySlug, RecordGatewayPaymentCommand command, CancellationToken ct);
    Task<FinancePaymentPlanConfiguration> ConfigurePaymentPlanAsync(Guid tenantId, ConfigurePaymentPlanCommand command, CancellationToken ct);
    Task<FinancePaymentPlan> CreatePaymentPlanAsync(Guid tenantId, CreatePaymentPlanCommand command, CancellationToken ct);
    Task<FinancePaymentPlan> ApprovePaymentPlanAsync(Guid tenantId, Guid planId, Guid approverId, CancellationToken ct);
    Task<PaymentGatewayConfiguration> ConfigureGatewayAsync(Guid tenantId, Guid actorId, ConfigureGatewayCommand command, CancellationToken ct);
    Task<FinanceDashboardSummary> GetDashboardAsync(Guid tenantId, CancellationToken ct);
    Task<IReadOnlyList<TrialBalanceLine>> GetTrialBalanceAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct);
}
