using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904004700_AddTenantId_OfflineAttendanceEvent")]
public partial class AddTenantId_OfflineAttendanceEvent : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "OfflineAttendanceEvents",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_OfflineAttendanceEvents_TenantId",
            table: "OfflineAttendanceEvents",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_OfflineAttendanceEvents_TenantId",
            table: "OfflineAttendanceEvents");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "OfflineAttendanceEvents");
    }
}
