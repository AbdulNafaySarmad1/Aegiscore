using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904004800_AddTenantId_PaperDestructionManifest")]
public partial class AddTenantId_PaperDestructionManifest : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "PaperDestructionManifests",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_PaperDestructionManifests_TenantId",
            table: "PaperDestructionManifests",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_PaperDestructionManifests_TenantId",
            table: "PaperDestructionManifests");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "PaperDestructionManifests");
    }
}
