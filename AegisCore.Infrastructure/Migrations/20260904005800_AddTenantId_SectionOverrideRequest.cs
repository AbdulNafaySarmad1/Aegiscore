using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904005800_AddTenantId_SectionOverrideRequest")]
public partial class AddTenantId_SectionOverrideRequest : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
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
