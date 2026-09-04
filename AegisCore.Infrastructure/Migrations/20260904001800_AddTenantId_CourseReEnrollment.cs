using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904001800_AddTenantId_CourseReEnrollment")]
public partial class AddTenantId_CourseReEnrollment : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
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
