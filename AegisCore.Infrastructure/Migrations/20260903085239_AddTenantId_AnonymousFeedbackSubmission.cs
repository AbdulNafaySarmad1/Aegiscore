using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260903085239_AddTenantId_AnonymousFeedbackSubmission")]
public partial class AddTenantId_AnonymousFeedbackSubmission : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from the known tenant before this migration runs in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "AnonymousFeedbackSubmissions",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_AnonymousFeedbackSubmissions_TenantId",
            table: "AnonymousFeedbackSubmissions",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_AnonymousFeedbackSubmissions_TenantId",
            table: "AnonymousFeedbackSubmissions");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "AnonymousFeedbackSubmissions");
    }
}
