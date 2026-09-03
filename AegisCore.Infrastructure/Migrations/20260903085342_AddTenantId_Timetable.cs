using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260903085342_AddTenantId_Timetable")]
public partial class AddTenantId_Timetable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from the known tenant before this migration runs in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "Timetables",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_Timetables_TenantId",
            table: "Timetables",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Timetables_TenantId",
            table: "Timetables");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "Timetables");
    }
}
