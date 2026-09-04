using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904011400_AddTenantId_WorkflowStep")]
public partial class AddTenantId_WorkflowStep : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "WorkflowSteps",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_WorkflowSteps_TenantId",
            table: "WorkflowSteps",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_WorkflowSteps_TenantId",
            table: "WorkflowSteps");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "WorkflowSteps");
    }
}
