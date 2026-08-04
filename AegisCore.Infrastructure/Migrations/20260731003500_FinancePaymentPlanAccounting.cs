using AegisCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260731003500_FinancePaymentPlanAccounting")]
public partial class FinancePaymentPlanAccounting : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>("PenaltyAmount", "FeeInstallments", type: "numeric(19,4)", nullable: false, defaultValue: 0m);
        migrationBuilder.AddColumn<decimal>("RecognizedAmount", "FeeInstallments", type: "numeric(19,4)", nullable: false, defaultValue: 0m);
        migrationBuilder.AddColumn<DateTime>("LastPenaltyAppliedOn", "FeeInstallments", type: "timestamp with time zone", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("LastPenaltyAppliedOn", "FeeInstallments");
        migrationBuilder.DropColumn("RecognizedAmount", "FeeInstallments");
        migrationBuilder.DropColumn("PenaltyAmount", "FeeInstallments");
    }
}
