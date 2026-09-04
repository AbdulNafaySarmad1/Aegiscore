using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904004300_AddTenantId_MeritListEntry")]
public partial class AddTenantId_MeritListEntry : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
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
