using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260903085235_AddTenantId_AcademicHoliday")]
public partial class AddTenantId_AcademicHoliday : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from the known tenant before this migration runs in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "AcademicHolidays",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_AcademicHolidays_TenantId",
            table: "AcademicHolidays",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_AcademicHolidays_TenantId",
            table: "AcademicHolidays");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "AcademicHolidays");
    }
}
