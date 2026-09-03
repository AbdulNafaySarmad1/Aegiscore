using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260903085327_AddTenantId_ProcurementRequisition")]
public partial class AddTenantId_ProcurementRequisition : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from the known tenant before this migration runs in production.
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
