using System.Globalization;
using System.Text;
using AegisCore.Application.Hr;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Hr;

public sealed class PayrollService(AegisDbContext db) : IPayrollService
{
    public PayslipData CalculatePayslip(Employee employee, DateOnly month, bool providentFundEnabled = true)
    {
        var basic = employee.Salary ?? 0m;
        var hra = basic * 0.20m;
        var gross = basic + hra + 5000m + 3000m;
        var eobi = basic * 0.05m;
        var provident = providentFundEnabled ? basic * 0.05m : 0m;
        var tax = CalculateFederalIncomeTax(gross - eobi);
        var deductions = eobi + provident + tax;

        return new PayslipData(
            basic,
            Math.Round(gross, 2),
            Math.Round(eobi, 2),
            Math.Round(tax, 2),
            Math.Round(provident, 2),
            Math.Round(deductions, 2),
            Math.Round(gross - deductions, 2));
    }

    public async Task<PayrollRun> RunPayrollAsync(DateOnly month, Guid actorId, CancellationToken ct = default)
    {
        var employees = await db.Employees.Where(e => e.Status == EmployeeStatus.Active).ToListAsync(ct);
        var run = new PayrollRun
        {
            Id = Guid.NewGuid(),
            Month = new DateOnly(month.Year, month.Month, 1),
            RunBy = actorId,
            TotalEmployees = employees.Count
        };

        foreach (var employee in employees)
        {
            var data = CalculatePayslip(employee, run.Month);
            run.Payslips.Add(new Payslip
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                Month = run.Month,
                BasicSalary = data.BasicSalary,
                GrossSalary = data.GrossSalary,
                EobiDeduction = data.EobiDeduction,
                IncomeTaxDeduction = data.IncomeTaxDeduction,
                ProvidentFundDeduction = data.ProvidentFundDeduction,
                TotalDeductions = data.TotalDeductions,
                NetSalary = data.NetSalary
            });
        }

        run.TotalSalary = run.Payslips.Sum(p => p.NetSalary);
        db.PayrollRuns.Add(run);
        await db.SaveChangesAsync(ct);
        return run;
    }

    public async Task<string> ExportBankTransferCsvAsync(Guid payrollRunId, CancellationToken ct = default)
    {
        var slips = await db.Payslips.Where(p => p.PayrollRunId == payrollRunId).OrderBy(p => p.EmployeeId).ToListAsync(ct);
        var csv = new StringBuilder("employee_id,amount,reference\n");
        foreach (var slip in slips)
        {
            csv.Append(slip.EmployeeId)
                .Append(',')
                .Append(slip.NetSalary.ToString("0.00", CultureInfo.InvariantCulture))
                .Append(",PAY-")
                .Append(slip.Month.ToString("yyyyMM", CultureInfo.InvariantCulture))
                .Append('\n');
        }

        return csv.ToString();
    }

    public decimal CalculateGratuity(Employee employee, DateOnly resignationDate)
    {
        if (employee.DateOfJoining is null || employee.EmployeeType != EmployeeType.Permanent)
        {
            return 0m;
        }

        var joining = DateOnly.FromDateTime(employee.DateOfJoining.Value);
        var years = (resignationDate.DayNumber - joining.DayNumber) / 365.25m;
        if (years < 1m)
        {
            return 0m;
        }

        var finalSalary = (employee.Salary ?? 0m) * 1.20m;
        return Math.Round(finalSalary / 30m * years, 2, MidpointRounding.AwayFromZero);
    }

    private static decimal CalculateFederalIncomeTax(decimal taxableMonthlyIncome)
    {
        var annual = taxableMonthlyIncome * 12m;
        if (annual <= 600000m) return 0m;
        if (annual <= 1200000m) return ((annual - 600000m) * 0.05m) / 12m;
        if (annual <= 2400000m) return (30000m + (annual - 1200000m) * 0.15m) / 12m;
        return (210000m + (annual - 2400000m) * 0.25m) / 12m;
    }
}
