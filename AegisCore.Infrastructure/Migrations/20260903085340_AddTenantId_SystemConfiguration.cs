using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260903085340_AddTenantId_SystemConfiguration")]
public partial class AddTenantId_SystemConfiguration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from the known tenant before this migration runs in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "SystemConfigurations",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_SystemConfigurations_TenantId",
            table: "SystemConfigurations",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_SystemConfigurations_TenantId",
            table: "SystemConfigurations");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "SystemConfigurations");
    }
}
