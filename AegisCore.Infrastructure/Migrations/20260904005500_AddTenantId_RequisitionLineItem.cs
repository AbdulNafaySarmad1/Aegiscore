using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904005500_AddTenantId_RequisitionLineItem")]
public partial class AddTenantId_RequisitionLineItem : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "RequisitionLineItems",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_RequisitionLineItems_TenantId",
            table: "RequisitionLineItems",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_RequisitionLineItems_TenantId",
            table: "RequisitionLineItems");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "RequisitionLineItems");
    }
}
