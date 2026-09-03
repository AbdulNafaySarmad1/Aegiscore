using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260903085300_AddTenantId_EventSchemaVersion")]
public partial class AddTenantId_EventSchemaVersion : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from the known tenant before this migration runs in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "EventSchemaVersions",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_EventSchemaVersions_TenantId",
            table: "EventSchemaVersions",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_EventSchemaVersions_TenantId",
            table: "EventSchemaVersions");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "EventSchemaVersions");
    }
}
