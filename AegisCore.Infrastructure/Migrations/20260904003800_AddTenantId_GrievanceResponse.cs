using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904003800_AddTenantId_GrievanceResponse")]
public partial class AddTenantId_GrievanceResponse : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
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
