using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260903085238_AddTenantId_AgenticGradeRun")]
public partial class AddTenantId_AgenticGradeRun : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from the known tenant before this migration runs in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "AgenticGradeRuns",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_AgenticGradeRuns_TenantId",
            table: "AgenticGradeRuns",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_AgenticGradeRuns_TenantId",
            table: "AgenticGradeRuns");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "AgenticGradeRuns");
    }
}
