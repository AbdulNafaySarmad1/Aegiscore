using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904010200_AddTenantId_Student")]
public partial class AddTenantId_Student : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "Students",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_Students_TenantId",
            table: "Students",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Students_TenantId",
            table: "Students");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "Students");
    }
}
