using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904005600_AddTenantId_RuleVersion")]
public partial class AddTenantId_RuleVersion : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "RuleVersions",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_RuleVersions_TenantId",
            table: "RuleVersions",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_RuleVersions_TenantId",
            table: "RuleVersions");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "RuleVersions");
    }
}
