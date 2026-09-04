using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904010900_AddTenantId_TranslationKey")]
public partial class AddTenantId_TranslationKey : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "TranslationKeys",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_TranslationKeys_TenantId",
            table: "TranslationKeys",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_TranslationKeys_TenantId",
            table: "TranslationKeys");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "TranslationKeys");
    }
}
