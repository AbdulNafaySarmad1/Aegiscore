using AegisCore.Domain.Entities;

namespace AegisCore.Application.Hr;

public interface IPayrollService
{
    PayslipData CalculatePayslip(Employee employee, DateOnly month, bool providentFundEnabled = true);
    Task<PayrollRun> RunPayrollAsync(DateOnly month, Guid actorId, CancellationToken ct = default);
    Task<string> ExportBankTransferCsvAsync(Guid payrollRunId, CancellationToken ct = default);
    decimal CalculateGratuity(Employee employee, DateOnly resignationDate);
}

public sealed record PayslipData(
    decimal BasicSalary,
    decimal GrossSalary,
    decimal EobiDeduction,
    decimal IncomeTaxDeduction,
    decimal ProvidentFundDeduction,
    decimal TotalDeductions,
    decimal NetSalary);
