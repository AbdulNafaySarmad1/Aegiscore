using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904001200_AddTenantId_CloPlomapping")]
public partial class AddTenantId_CloPlomapping : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "CloPlomappings",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_CloPlomappings_TenantId",
            table: "CloPlomappings",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_CloPlomappings_TenantId",
            table: "CloPlomappings");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "CloPlomappings");
    }
}
