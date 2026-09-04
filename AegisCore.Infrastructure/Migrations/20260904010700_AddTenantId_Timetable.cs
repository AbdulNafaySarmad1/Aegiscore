using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904010700_AddTenantId_Timetable")]
public partial class AddTenantId_Timetable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
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
