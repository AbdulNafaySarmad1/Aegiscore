using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260903085245_AddTenantId_Batch")]
public partial class AddTenantId_Batch : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from the known tenant before this migration runs in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "Batches",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_Batches_TenantId",
            table: "Batches",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Batches_TenantId",
            table: "Batches");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "Batches");
    }
}
