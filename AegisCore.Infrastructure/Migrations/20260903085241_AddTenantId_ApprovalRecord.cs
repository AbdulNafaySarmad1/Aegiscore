using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260903085241_AddTenantId_ApprovalRecord")]
public partial class AddTenantId_ApprovalRecord : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from the known tenant before this migration runs in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "ApprovalRecords",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_ApprovalRecords_TenantId",
            table: "ApprovalRecords",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ApprovalRecords_TenantId",
            table: "ApprovalRecords");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "ApprovalRecords");
    }
}
