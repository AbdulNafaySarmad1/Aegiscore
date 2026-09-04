using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904011300_AddTenantId_Vendor")]
public partial class AddTenantId_Vendor : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "Vendors",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_Vendors_TenantId",
            table: "Vendors",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Vendors_TenantId",
            table: "Vendors");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "Vendors");
    }
}
