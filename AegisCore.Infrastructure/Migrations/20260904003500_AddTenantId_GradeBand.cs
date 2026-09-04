using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904003500_AddTenantId_GradeBand")]
public partial class AddTenantId_GradeBand : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "GradeBands",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_GradeBands_TenantId",
            table: "GradeBands",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_GradeBands_TenantId",
            table: "GradeBands");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "GradeBands");
    }
}
