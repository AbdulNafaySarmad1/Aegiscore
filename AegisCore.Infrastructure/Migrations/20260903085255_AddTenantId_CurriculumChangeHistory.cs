using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260903085255_AddTenantId_CurriculumChangeHistory")]
public partial class AddTenantId_CurriculumChangeHistory : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from the known tenant before this migration runs in production.
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
