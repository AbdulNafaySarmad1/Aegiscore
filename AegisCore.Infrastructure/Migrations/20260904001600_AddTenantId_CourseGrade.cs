using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904001600_AddTenantId_CourseGrade")]
public partial class AddTenantId_CourseGrade : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "CourseGrades",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_CourseGrades_TenantId",
            table: "CourseGrades",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_CourseGrades_TenantId",
            table: "CourseGrades");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "CourseGrades");
    }
}
