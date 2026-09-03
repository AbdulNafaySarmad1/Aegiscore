using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260903085253_AddTenantId_CourseReEnrollment")]
public partial class AddTenantId_CourseReEnrollment : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from the known tenant before this migration runs in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "CourseReEnrollments",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_CourseReEnrollments_TenantId",
            table: "CourseReEnrollments",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_CourseReEnrollments_TenantId",
            table: "CourseReEnrollments");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "CourseReEnrollments");
    }
}
