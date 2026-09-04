using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904000200_AddTenantId_AdmissionApplication")]
public partial class AddTenantId_AdmissionApplication : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "AdmissionApplications",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_AdmissionApplications_TenantId",
            table: "AdmissionApplications",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_AdmissionApplications_TenantId",
            table: "AdmissionApplications");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "AdmissionApplications");
    }
}
