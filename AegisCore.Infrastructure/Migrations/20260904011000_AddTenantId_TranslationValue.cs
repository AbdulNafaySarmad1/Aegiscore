using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904011000_AddTenantId_TranslationValue")]
public partial class AddTenantId_TranslationValue : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "TranslationValues",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_TranslationValues_TenantId",
            table: "TranslationValues",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_TranslationValues_TenantId",
            table: "TranslationValues");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "TranslationValues");
    }
}
