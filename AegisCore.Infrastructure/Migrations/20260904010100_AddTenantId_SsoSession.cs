using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904010100_AddTenantId_SsoSession")]
public partial class AddTenantId_SsoSession : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "SsoSessions",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_SsoSessions_TenantId",
            table: "SsoSessions",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_SsoSessions_TenantId",
            table: "SsoSessions");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "SsoSessions");
    }
}
