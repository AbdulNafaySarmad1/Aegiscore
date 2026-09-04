using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904010800_AddTenantId_Transcript")]
public partial class AddTenantId_Transcript : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "Transcripts",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_Transcripts_TenantId",
            table: "Transcripts",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Transcripts_TenantId",
            table: "Transcripts");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "Transcripts");
    }
}
