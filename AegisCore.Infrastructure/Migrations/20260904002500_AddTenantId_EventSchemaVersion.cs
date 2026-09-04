using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904002500_AddTenantId_EventSchemaVersion")]
public partial class AddTenantId_EventSchemaVersion : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
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
