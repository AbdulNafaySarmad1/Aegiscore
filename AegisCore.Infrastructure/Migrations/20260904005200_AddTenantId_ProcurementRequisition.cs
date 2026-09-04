using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904005200_AddTenantId_ProcurementRequisition")]
public partial class AddTenantId_ProcurementRequisition : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "ProcurementRequisitions",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_ProcurementRequisitions_TenantId",
            table: "ProcurementRequisitions",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ProcurementRequisitions_TenantId",
            table: "ProcurementRequisitions");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "ProcurementRequisitions");
    }
}
