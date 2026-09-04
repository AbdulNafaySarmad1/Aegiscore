using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904004500_AddTenantId_NotificationRecord")]
public partial class AddTenantId_NotificationRecord : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "NotificationRecords",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_NotificationRecords_TenantId",
            table: "NotificationRecords",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_NotificationRecords_TenantId",
            table: "NotificationRecords");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "NotificationRecords");
    }
}
