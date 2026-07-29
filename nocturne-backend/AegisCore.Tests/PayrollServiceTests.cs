using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure.Hr;

namespace AegisCore.Tests;

public sealed class PayrollServiceTests
{
    [Fact]
    public void CalculatePayslip_AppliesPakistanDeductions()
    {
        var service = new PayrollService(null!);
        var employee = new Employee { Salary = 100000m, Status = EmployeeStatus.Active };

        var slip = service.CalculatePayslip(employee, new DateOnly(2026, 7, 1));

        Assert.Equal(128000m, slip.GrossSalary);
        Assert.Equal(5000m, slip.EobiDeduction);
        Assert.Equal(5000m, slip.ProvidentFundDeduction);
        Assert.True(slip.NetSalary < slip.GrossSalary);
    }

    [Fact]
    public void CalculateGratuity_RequiresPermanentEmployeeWithOneYear()
    {
        var service = new PayrollService(null!);
        var employee = new Employee
        {
            Salary = 90000m,
            EmployeeType = EmployeeType.Permanent,
            DateOfJoining = new DateTime(2024, 7, 1)
        };

        var gratuity = service.CalculateGratuity(employee, new DateOnly(2026, 7, 1));

        Assert.True(gratuity > 0m);
    }
}
