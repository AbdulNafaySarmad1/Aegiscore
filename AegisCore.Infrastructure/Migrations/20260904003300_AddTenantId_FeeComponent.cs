using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904003300_AddTenantId_FeeComponent")]
public partial class AddTenantId_FeeComponent : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "FeeComponents",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_FeeComponents_TenantId",
            table: "FeeComponents",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_FeeComponents_TenantId",
            table: "FeeComponents");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "FeeComponents");
    }
}
