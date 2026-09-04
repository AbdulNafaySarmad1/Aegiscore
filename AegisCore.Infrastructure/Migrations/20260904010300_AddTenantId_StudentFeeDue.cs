using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904010300_AddTenantId_StudentFeeDue")]
public partial class AddTenantId_StudentFeeDue : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "StudentFeeDues",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_StudentFeeDues_TenantId",
            table: "StudentFeeDues",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_StudentFeeDues_TenantId",
            table: "StudentFeeDues");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "StudentFeeDues");
    }
}
