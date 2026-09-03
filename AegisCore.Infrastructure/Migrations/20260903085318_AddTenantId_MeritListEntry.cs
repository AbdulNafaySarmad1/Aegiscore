using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260903085318_AddTenantId_MeritListEntry")]
public partial class AddTenantId_MeritListEntry : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from the known tenant before this migration runs in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "MeritListEntries",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_MeritListEntries_TenantId",
            table: "MeritListEntries",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_MeritListEntries_TenantId",
            table: "MeritListEntries");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "MeritListEntries");
    }
}
