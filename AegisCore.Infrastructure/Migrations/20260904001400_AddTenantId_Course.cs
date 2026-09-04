using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904001400_AddTenantId_Course")]
public partial class AddTenantId_Course : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "Courses",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_Courses_TenantId",
            table: "Courses",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Courses_TenantId",
            table: "Courses");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "Courses");
    }
}
