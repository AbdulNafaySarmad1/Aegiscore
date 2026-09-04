using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904010500_AddTenantId_SystemConfiguration")]
public partial class AddTenantId_SystemConfiguration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
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
