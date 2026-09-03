using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260903085256_AddTenantId_CurriculumCourse")]
public partial class AddTenantId_CurriculumCourse : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from the known tenant before this migration runs in production.
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
