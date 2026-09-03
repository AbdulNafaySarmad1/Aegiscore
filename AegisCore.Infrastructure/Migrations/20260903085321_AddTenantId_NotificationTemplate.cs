using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260903085321_AddTenantId_NotificationTemplate")]
public partial class AddTenantId_NotificationTemplate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from the known tenant before this migration runs in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "NotificationTemplates",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_NotificationTemplates_TenantId",
            table: "NotificationTemplates",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_NotificationTemplates_TenantId",
            table: "NotificationTemplates");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "NotificationTemplates");
    }
}
