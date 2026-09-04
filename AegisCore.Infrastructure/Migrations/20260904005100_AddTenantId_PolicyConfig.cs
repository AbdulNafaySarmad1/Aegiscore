using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904005100_AddTenantId_PolicyConfig")]
public partial class AddTenantId_PolicyConfig : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "PolicyConfigs",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_PolicyConfigs_TenantId",
            table: "PolicyConfigs",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_PolicyConfigs_TenantId",
            table: "PolicyConfigs");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "PolicyConfigs");
    }
}
