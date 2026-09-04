using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904005900_AddTenantId_Semester")]
public partial class AddTenantId_Semester : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
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
