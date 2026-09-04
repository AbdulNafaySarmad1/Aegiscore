using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904002900_AddTenantId_ExamSchedule")]
public partial class AddTenantId_ExamSchedule : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "ExamSchedules",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_ExamSchedules_TenantId",
            table: "ExamSchedules",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ExamSchedules_TenantId",
            table: "ExamSchedules");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "ExamSchedules");
    }
}
