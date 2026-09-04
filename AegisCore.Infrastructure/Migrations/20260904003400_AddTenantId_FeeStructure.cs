using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904003400_AddTenantId_FeeStructure")]
public partial class AddTenantId_FeeStructure : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "FeeStructures",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_FeeStructures_TenantId",
            table: "FeeStructures",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_FeeStructures_TenantId",
            table: "FeeStructures");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "FeeStructures");
    }
}
