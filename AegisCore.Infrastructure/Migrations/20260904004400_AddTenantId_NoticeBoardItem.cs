using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904004400_AddTenantId_NoticeBoardItem")]
public partial class AddTenantId_NoticeBoardItem : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "NoticeBoardItems",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_NoticeBoardItems_TenantId",
            table: "NoticeBoardItems",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_NoticeBoardItems_TenantId",
            table: "NoticeBoardItems");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "NoticeBoardItems");
    }
}
