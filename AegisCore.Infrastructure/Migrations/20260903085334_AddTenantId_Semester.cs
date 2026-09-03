using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260903085334_AddTenantId_Semester")]
public partial class AddTenantId_Semester : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from the known tenant before this migration runs in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "Semesters",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_Semesters_TenantId",
            table: "Semesters",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Semesters_TenantId",
            table: "Semesters");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "Semesters");
    }
}
