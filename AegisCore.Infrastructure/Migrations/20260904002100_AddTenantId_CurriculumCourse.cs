using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904002100_AddTenantId_CurriculumCourse")]
public partial class AddTenantId_CurriculumCourse : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "CurriculumCourses",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_CurriculumCourses_TenantId",
            table: "CurriculumCourses",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_CurriculumCourses_TenantId",
            table: "CurriculumCourses");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "CurriculumCourses");
    }
}
