using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904005400_AddTenantId_PurchaseOrder")]
public partial class AddTenantId_PurchaseOrder : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "PurchaseOrders",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_PurchaseOrders_TenantId",
            table: "PurchaseOrders",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_PurchaseOrders_TenantId",
            table: "PurchaseOrders");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "PurchaseOrders");
    }
}
