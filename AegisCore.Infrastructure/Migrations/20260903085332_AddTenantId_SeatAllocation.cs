using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260903085332_AddTenantId_SeatAllocation")]
public partial class AddTenantId_SeatAllocation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from the known tenant before this migration runs in production.
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
