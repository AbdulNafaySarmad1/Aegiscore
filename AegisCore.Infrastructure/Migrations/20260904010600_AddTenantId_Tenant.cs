using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904010600_AddTenantId_Tenant")]
public partial class AddTenantId_Tenant : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "Tenants",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_Tenants_TenantId",
            table: "Tenants",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Tenants_TenantId",
            table: "Tenants");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "Tenants");
    }
}
