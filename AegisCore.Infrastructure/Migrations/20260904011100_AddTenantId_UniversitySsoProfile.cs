using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904011100_AddTenantId_UniversitySsoProfile")]
public partial class AddTenantId_UniversitySsoProfile : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
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
