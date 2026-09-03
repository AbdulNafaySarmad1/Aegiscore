using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260903085259_AddTenantId_Enrollment")]
public partial class AddTenantId_Enrollment : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from the known tenant before this migration runs in production.
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
