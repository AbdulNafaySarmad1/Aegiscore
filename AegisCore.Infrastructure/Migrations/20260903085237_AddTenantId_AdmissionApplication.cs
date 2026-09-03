using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260903085237_AddTenantId_AdmissionApplication")]
public partial class AddTenantId_AdmissionApplication : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from the known tenant before this migration runs in production.
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
