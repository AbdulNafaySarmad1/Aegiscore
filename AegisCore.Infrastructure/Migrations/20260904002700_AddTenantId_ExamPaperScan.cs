using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904002700_AddTenantId_ExamPaperScan")]
public partial class AddTenantId_ExamPaperScan : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "ExamPaperScans",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_ExamPaperScans_TenantId",
            table: "ExamPaperScans",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ExamPaperScans_TenantId",
            table: "ExamPaperScans");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "ExamPaperScans");
    }
}
