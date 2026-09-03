using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260903085246_AddTenantId_CisUpload")]
public partial class AddTenantId_CisUpload : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from the known tenant before this migration runs in production.
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
