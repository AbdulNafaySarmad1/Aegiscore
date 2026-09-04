using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904004900_AddTenantId_PayrollRun")]
public partial class AddTenantId_PayrollRun : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
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
