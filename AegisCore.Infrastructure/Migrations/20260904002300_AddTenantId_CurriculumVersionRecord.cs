using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904002300_AddTenantId_CurriculumVersionRecord")]
public partial class AddTenantId_CurriculumVersionRecord : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "CurriculumVersions",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_CurriculumVersions_TenantId",
            table: "CurriculumVersions",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_CurriculumVersions_TenantId",
            table: "CurriculumVersions");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "CurriculumVersions");
    }
}
