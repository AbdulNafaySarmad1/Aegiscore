using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904000500_AddTenantId_ApplicationDocument")]
public partial class AddTenantId_ApplicationDocument : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "ApplicationDocuments",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_ApplicationDocuments_TenantId",
            table: "ApplicationDocuments",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ApplicationDocuments_TenantId",
            table: "ApplicationDocuments");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "ApplicationDocuments");
    }
}
