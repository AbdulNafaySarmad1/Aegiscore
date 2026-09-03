using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260903085329_AddTenantId_PurchaseOrder")]
public partial class AddTenantId_PurchaseOrder : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from the known tenant before this migration runs in production.
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
