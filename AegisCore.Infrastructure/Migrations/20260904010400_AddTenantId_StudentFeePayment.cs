using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904010400_AddTenantId_StudentFeePayment")]
public partial class AddTenantId_StudentFeePayment : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "StudentFeePayments",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_StudentFeePayments_TenantId",
            table: "StudentFeePayments",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_StudentFeePayments_TenantId",
            table: "StudentFeePayments");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "StudentFeePayments");
    }
}
