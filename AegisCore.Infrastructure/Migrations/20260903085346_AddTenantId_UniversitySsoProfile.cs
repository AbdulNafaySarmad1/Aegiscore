using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260903085346_AddTenantId_UniversitySsoProfile")]
public partial class AddTenantId_UniversitySsoProfile : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from the known tenant before this migration runs in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "UniversitySsoProfiles",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_UniversitySsoProfiles_TenantId",
            table: "UniversitySsoProfiles",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_UniversitySsoProfiles_TenantId",
            table: "UniversitySsoProfiles");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "UniversitySsoProfiles");
    }
}
