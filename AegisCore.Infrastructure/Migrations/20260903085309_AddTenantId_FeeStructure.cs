using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260903085309_AddTenantId_FeeStructure")]
public partial class AddTenantId_FeeStructure : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from the known tenant before this migration runs in production.
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
