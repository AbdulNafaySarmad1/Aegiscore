using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904002200_AddTenantId_CurriculumPrerequisite")]
public partial class AddTenantId_CurriculumPrerequisite : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
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
