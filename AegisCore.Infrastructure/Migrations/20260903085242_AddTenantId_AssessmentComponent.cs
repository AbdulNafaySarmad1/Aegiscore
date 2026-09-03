using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260903085242_AddTenantId_AssessmentComponent")]
public partial class AddTenantId_AssessmentComponent : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from the known tenant before this migration runs in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "AssessmentComponents",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_AssessmentComponents_TenantId",
            table: "AssessmentComponents",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_AssessmentComponents_TenantId",
            table: "AssessmentComponents");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "AssessmentComponents");
    }
}
