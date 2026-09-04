using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904002600_AddTenantId_ExamPaperBatch")]
public partial class AddTenantId_ExamPaperBatch : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "ExamPaperBatches",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_ExamPaperBatches_TenantId",
            table: "ExamPaperBatches",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ExamPaperBatches_TenantId",
            table: "ExamPaperBatches");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "ExamPaperBatches");
    }
}
