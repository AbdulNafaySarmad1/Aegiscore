using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904010000_AddTenantId_ServiceDependency")]
public partial class AddTenantId_ServiceDependency : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "ServiceDependencies",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_ServiceDependencies_TenantId",
            table: "ServiceDependencies",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ServiceDependencies_TenantId",
            table: "ServiceDependencies");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "ServiceDependencies");
    }
}
