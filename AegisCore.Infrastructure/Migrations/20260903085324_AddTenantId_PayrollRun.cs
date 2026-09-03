using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260903085324_AddTenantId_PayrollRun")]
public partial class AddTenantId_PayrollRun : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from the known tenant before this migration runs in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "PayrollRuns",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_PayrollRuns_TenantId",
            table: "PayrollRuns",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_PayrollRuns_TenantId",
            table: "PayrollRuns");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "PayrollRuns");
    }
}
