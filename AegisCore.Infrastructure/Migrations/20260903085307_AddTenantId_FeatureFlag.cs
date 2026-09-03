using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260903085307_AddTenantId_FeatureFlag")]
public partial class AddTenantId_FeatureFlag : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from the known tenant before this migration runs in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "FeatureFlags",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_FeatureFlags_TenantId",
            table: "FeatureFlags",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_FeatureFlags_TenantId",
            table: "FeatureFlags");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "FeatureFlags");
    }
}
