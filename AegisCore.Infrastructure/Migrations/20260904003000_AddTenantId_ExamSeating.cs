using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904003000_AddTenantId_ExamSeating")]
public partial class AddTenantId_ExamSeating : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "ExamSeatings",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_ExamSeatings_TenantId",
            table: "ExamSeatings",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ExamSeatings_TenantId",
            table: "ExamSeatings");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "ExamSeatings");
    }
}
