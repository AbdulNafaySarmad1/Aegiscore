using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904003700_AddTenantId_GradeScale")]
public partial class AddTenantId_GradeScale : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
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
