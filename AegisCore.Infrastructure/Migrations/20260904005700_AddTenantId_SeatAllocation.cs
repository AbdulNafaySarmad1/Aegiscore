using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904005700_AddTenantId_SeatAllocation")]
public partial class AddTenantId_SeatAllocation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "SeatAllocations",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_SeatAllocations_TenantId",
            table: "SeatAllocations",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_SeatAllocations_TenantId",
            table: "SeatAllocations");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "SeatAllocations");
    }
}
