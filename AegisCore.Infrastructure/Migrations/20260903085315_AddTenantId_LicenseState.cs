using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260903085315_AddTenantId_LicenseState")]
public partial class AddTenantId_LicenseState : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from the known tenant before this migration runs in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "LicenseStates",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_LicenseStates_TenantId",
            table: "LicenseStates",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_LicenseStates_TenantId",
            table: "LicenseStates");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "LicenseStates");
    }
}
