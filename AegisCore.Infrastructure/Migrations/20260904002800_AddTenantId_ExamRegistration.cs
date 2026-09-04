using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904002800_AddTenantId_ExamRegistration")]
public partial class AddTenantId_ExamRegistration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "ExamRegistrations",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_ExamRegistrations_TenantId",
            table: "ExamRegistrations",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ExamRegistrations_TenantId",
            table: "ExamRegistrations");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "ExamRegistrations");
    }
}
