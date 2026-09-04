using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904000300_AddTenantId_AgenticGradeRun")]
public partial class AddTenantId_AgenticGradeRun : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
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
