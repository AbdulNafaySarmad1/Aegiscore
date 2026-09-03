using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260903085306_AddTenantId_Faculty")]
public partial class AddTenantId_Faculty : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from the known tenant before this migration runs in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "Faculties",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_Faculties_TenantId",
            table: "Faculties",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Faculties_TenantId",
            table: "Faculties");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "Faculties");
    }
}
