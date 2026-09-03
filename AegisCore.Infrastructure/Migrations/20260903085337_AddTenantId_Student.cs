using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260903085337_AddTenantId_Student")]
public partial class AddTenantId_Student : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from the known tenant before this migration runs in production.
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
