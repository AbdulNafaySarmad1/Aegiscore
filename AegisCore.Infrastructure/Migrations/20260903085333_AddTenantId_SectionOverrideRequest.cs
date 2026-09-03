using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260903085333_AddTenantId_SectionOverrideRequest")]
public partial class AddTenantId_SectionOverrideRequest : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from the known tenant before this migration runs in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "SectionOverrideRequests",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_SectionOverrideRequests_TenantId",
            table: "SectionOverrideRequests",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_SectionOverrideRequests_TenantId",
            table: "SectionOverrideRequests");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "SectionOverrideRequests");
    }
}
