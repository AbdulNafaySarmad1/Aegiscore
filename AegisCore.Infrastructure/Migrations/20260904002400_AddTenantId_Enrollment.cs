using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904002400_AddTenantId_Enrollment")]
public partial class AddTenantId_Enrollment : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "Enrollments",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_Enrollments_TenantId",
            table: "Enrollments",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Enrollments_TenantId",
            table: "Enrollments");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "Enrollments");
    }
}
