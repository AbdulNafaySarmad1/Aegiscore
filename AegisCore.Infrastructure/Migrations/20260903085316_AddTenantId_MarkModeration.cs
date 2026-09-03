using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260903085316_AddTenantId_MarkModeration")]
public partial class AddTenantId_MarkModeration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from the known tenant before this migration runs in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "MarkModerations",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_MarkModerations_TenantId",
            table: "MarkModerations",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_MarkModerations_TenantId",
            table: "MarkModerations");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "MarkModerations");
    }
}
