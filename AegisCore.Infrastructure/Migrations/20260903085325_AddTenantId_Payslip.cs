using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260903085325_AddTenantId_Payslip")]
public partial class AddTenantId_Payslip : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from the known tenant before this migration runs in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "Payslips",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_Payslips_TenantId",
            table: "Payslips",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Payslips_TenantId",
            table: "Payslips");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "Payslips");
    }
}
