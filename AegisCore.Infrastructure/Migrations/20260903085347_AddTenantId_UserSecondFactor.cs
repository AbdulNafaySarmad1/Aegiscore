using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260903085347_AddTenantId_UserSecondFactor")]
public partial class AddTenantId_UserSecondFactor : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from the known tenant before this migration runs in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "UserSecondFactors",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_UserSecondFactors_TenantId",
            table: "UserSecondFactors",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_UserSecondFactors_TenantId",
            table: "UserSecondFactors");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "UserSecondFactors");
    }
}
