using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260903085311_AddTenantId_GradeOverrideRequest")]
public partial class AddTenantId_GradeOverrideRequest : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from the known tenant before this migration runs in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "GradeOverrideRequests",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_GradeOverrideRequests_TenantId",
            table: "GradeOverrideRequests",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_GradeOverrideRequests_TenantId",
            table: "GradeOverrideRequests");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "GradeOverrideRequests");
    }
}
