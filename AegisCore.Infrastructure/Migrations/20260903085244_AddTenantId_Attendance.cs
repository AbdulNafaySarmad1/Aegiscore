using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260903085244_AddTenantId_Attendance")]
public partial class AddTenantId_Attendance : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from the known tenant before this migration runs in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "Attendances",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_Attendances_TenantId",
            table: "Attendances",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Attendances_TenantId",
            table: "Attendances");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "Attendances");
    }
}
