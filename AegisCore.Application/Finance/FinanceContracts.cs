using AegisCore.Domain.Entities;

namespace AegisCore.Application.Finance;

public interface IFeeService
{
    /// <summary>Raises the fee demand for a student's semester from the fee structure. The demand is the fee ledger.</summary>
    Task<FeeDemand> CalculateFeeAsync(Guid studentId, Guid semesterId, Guid feeStructureId, CancellationToken ct = default);
    Task RecordPaymentAsync(Guid feeDueId, PaymentRequest payment, CancellationToken ct = default);
    Task<decimal> CalculateLateFeeAsync(Guid feeDueId, CancellationToken ct = default);
    Task ApplyScholarshipAsync(Guid feeDueId, decimal amount, Guid scholarshipId, CancellationToken ct = default);
}

public interface IAccountingService
{
    Task PostFeePaymentAsync(FinancePayment payment, Guid postedBy, CancellationToken ct = default);
    Task<string> ExportPifraAsync(DateOnly from, DateOnly to, CancellationToken ct = default);
}

public interface IBudgetService
{
    Task<BudgetVarianceReport> GetVarianceAsync(Guid costCenterId, CancellationToken ct = default);
}

public sealed record PaymentRequest(string Method, decimal Amount, string? ReferenceNumber);
public sealed record BudgetVarianceReport(Guid CostCenterId, decimal BudgetedAmount, decimal ActualExpense, decimal Variance, decimal VariancePercentage, string Status);
