using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904000600_AddTenantId_ApprovalRecord")]
public partial class AddTenantId_ApprovalRecord : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
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
