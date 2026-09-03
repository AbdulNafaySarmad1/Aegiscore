using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260903085313_AddTenantId_GrievanceResponse")]
public partial class AddTenantId_GrievanceResponse : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from the known tenant before this migration runs in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "GrievanceResponses",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_GrievanceResponses_TenantId",
            table: "GrievanceResponses",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_GrievanceResponses_TenantId",
            table: "GrievanceResponses");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "GrievanceResponses");
    }
}
