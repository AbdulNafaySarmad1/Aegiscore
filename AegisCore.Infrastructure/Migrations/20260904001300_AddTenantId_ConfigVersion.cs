using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904001300_AddTenantId_ConfigVersion")]
public partial class AddTenantId_ConfigVersion : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "ConfigVersions",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_ConfigVersions_TenantId",
            table: "ConfigVersions",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ConfigVersions_TenantId",
            table: "ConfigVersions");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "ConfigVersions");
    }
}
