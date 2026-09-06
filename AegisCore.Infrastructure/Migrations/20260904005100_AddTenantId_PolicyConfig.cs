using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904005100_AddTenantId_PolicyConfig")]
public partial class AddTenantId_PolicyConfig : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Baseline policy rows are system templates and intentionally use Guid.Empty.
        // Tenant onboarding creates tenant-specific policy copies after this migration.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "PolicyConfigs",
            type: "uuid",
            nullable: true);

        migrationBuilder.Sql(
            """
            UPDATE "PolicyConfigs"
            SET "TenantId" = '00000000-0000-0000-0000-000000000000'
            WHERE "TenantId" IS NULL;
            """);

        migrationBuilder.AlterColumn<Guid>(
            name: "TenantId",
            table: "PolicyConfigs",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_PolicyConfigs_TenantId",
            table: "PolicyConfigs",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_PolicyConfigs_TenantId",
            table: "PolicyConfigs");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "PolicyConfigs");
    }
}
