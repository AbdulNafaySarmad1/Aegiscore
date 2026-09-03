using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260903085349_AddTenantId_WorkflowStep")]
public partial class AddTenantId_WorkflowStep : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from the known tenant before this migration runs in production.
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
