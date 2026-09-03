using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260903085257_AddTenantId_CurriculumPrerequisite")]
public partial class AddTenantId_CurriculumPrerequisite : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from the known tenant before this migration runs in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "CurriculumPrerequisites",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_CurriculumPrerequisites_TenantId",
            table: "CurriculumPrerequisites",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_CurriculumPrerequisites_TenantId",
            table: "CurriculumPrerequisites");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "CurriculumPrerequisites");
    }
}
