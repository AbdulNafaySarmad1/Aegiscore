using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904004200_AddTenantId_MeritList")]
public partial class AddTenantId_MeritList : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "MeritLists",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_MeritLists_TenantId",
            table: "MeritLists",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_MeritLists_TenantId",
            table: "MeritLists");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "MeritLists");
    }
}
