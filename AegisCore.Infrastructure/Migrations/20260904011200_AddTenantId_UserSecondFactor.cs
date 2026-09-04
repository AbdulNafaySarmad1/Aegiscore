using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904011200_AddTenantId_UserSecondFactor")]
public partial class AddTenantId_UserSecondFactor : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
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
