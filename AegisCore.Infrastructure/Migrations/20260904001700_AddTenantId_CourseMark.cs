using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904001700_AddTenantId_CourseMark")]
public partial class AddTenantId_CourseMark : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "CourseMarks",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_CourseMarks_TenantId",
            table: "CourseMarks",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_CourseMarks_TenantId",
            table: "CourseMarks");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "CourseMarks");
    }
}
