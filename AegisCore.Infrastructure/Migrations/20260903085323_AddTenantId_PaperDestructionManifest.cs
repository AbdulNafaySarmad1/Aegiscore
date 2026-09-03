using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260903085323_AddTenantId_PaperDestructionManifest")]
public partial class AddTenantId_PaperDestructionManifest : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from the known tenant before this migration runs in production.
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
