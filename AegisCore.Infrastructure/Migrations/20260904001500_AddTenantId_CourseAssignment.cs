using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904001500_AddTenantId_CourseAssignment")]
public partial class AddTenantId_CourseAssignment : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "CourseAssignments",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_CourseAssignments_TenantId",
            table: "CourseAssignments",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_CourseAssignments_TenantId",
            table: "CourseAssignments");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "CourseAssignments");
    }
}
