using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904000000_AddTenantId_AcademicHoliday")]
public partial class AddTenantId_AcademicHoliday : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
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
