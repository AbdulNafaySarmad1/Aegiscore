using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904001000_AddTenantId_Batch")]
public partial class AddTenantId_Batch : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
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
