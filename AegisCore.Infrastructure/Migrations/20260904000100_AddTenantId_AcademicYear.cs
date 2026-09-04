using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904000100_AddTenantId_AcademicYear")]
public partial class AddTenantId_AcademicYear : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "AcademicYears",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_AcademicYears_TenantId",
            table: "AcademicYears",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_AcademicYears_TenantId",
            table: "AcademicYears");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "AcademicYears");
    }
}
