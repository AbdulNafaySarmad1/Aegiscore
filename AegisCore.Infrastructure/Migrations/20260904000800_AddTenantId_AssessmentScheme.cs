using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904000800_AddTenantId_AssessmentScheme")]
public partial class AddTenantId_AssessmentScheme : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "AssessmentSchemes",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_AssessmentSchemes_TenantId",
            table: "AssessmentSchemes",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_AssessmentSchemes_TenantId",
            table: "AssessmentSchemes");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "AssessmentSchemes");
    }
}
