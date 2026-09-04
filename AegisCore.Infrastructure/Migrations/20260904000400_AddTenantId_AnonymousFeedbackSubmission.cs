using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904000400_AddTenantId_AnonymousFeedbackSubmission")]
public partial class AddTenantId_AnonymousFeedbackSubmission : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
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
