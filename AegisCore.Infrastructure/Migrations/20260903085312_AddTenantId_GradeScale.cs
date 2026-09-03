using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260903085312_AddTenantId_GradeScale")]
public partial class AddTenantId_GradeScale : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from the known tenant before this migration runs in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "GradeScales",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_GradeScales_TenantId",
            table: "GradeScales",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_GradeScales_TenantId",
            table: "GradeScales");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "GradeScales");
    }
}
