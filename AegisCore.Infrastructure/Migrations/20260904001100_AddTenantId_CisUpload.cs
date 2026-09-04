using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904001100_AddTenantId_CisUpload")]
public partial class AddTenantId_CisUpload : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "CisUploads",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_CisUploads_TenantId",
            table: "CisUploads",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_CisUploads_TenantId",
            table: "CisUploads");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "CisUploads");
    }
}
