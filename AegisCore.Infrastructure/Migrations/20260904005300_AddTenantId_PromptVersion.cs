using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904005300_AddTenantId_PromptVersion")]
public partial class AddTenantId_PromptVersion : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "PromptVersions",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_PromptVersions_TenantId",
            table: "PromptVersions",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_PromptVersions_TenantId",
            table: "PromptVersions");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "PromptVersions");
    }
}
