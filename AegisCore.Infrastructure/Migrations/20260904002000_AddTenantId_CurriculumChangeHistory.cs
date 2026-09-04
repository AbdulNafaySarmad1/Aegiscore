using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904002000_AddTenantId_CurriculumChangeHistory")]
public partial class AddTenantId_CurriculumChangeHistory : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "CurriculumChangeHistory",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_CurriculumChangeHistory_TenantId",
            table: "CurriculumChangeHistory",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_CurriculumChangeHistory_TenantId",
            table: "CurriculumChangeHistory");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "CurriculumChangeHistory");
    }
}
