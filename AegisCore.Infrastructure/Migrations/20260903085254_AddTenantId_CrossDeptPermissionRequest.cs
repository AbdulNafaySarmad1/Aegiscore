using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260903085254_AddTenantId_CrossDeptPermissionRequest")]
public partial class AddTenantId_CrossDeptPermissionRequest : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from the known tenant before this migration runs in production.
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "CrossDeptPermissionRequests",
            type: "uuid",
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_CrossDeptPermissionRequests_TenantId",
            table: "CrossDeptPermissionRequests",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_CrossDeptPermissionRequests_TenantId",
            table: "CrossDeptPermissionRequests");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "CrossDeptPermissionRequests");
    }
}
