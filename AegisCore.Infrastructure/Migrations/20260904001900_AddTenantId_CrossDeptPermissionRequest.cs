using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260904001900_AddTenantId_CrossDeptPermissionRequest")]
public partial class AddTenantId_CrossDeptPermissionRequest : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DATA MIGRATION REQUIRED — populate TenantId from known tenant before running this migration in production.
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
