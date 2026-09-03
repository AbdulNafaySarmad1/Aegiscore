using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260903085236_AddTenantId_AcademicYear")]
public partial class AddTenantId_AcademicYear : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from the known tenant before this migration runs in production.
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
