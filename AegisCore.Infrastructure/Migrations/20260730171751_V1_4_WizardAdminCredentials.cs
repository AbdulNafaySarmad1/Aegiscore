using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

// P11 multi-dialect note: application ITenantScoped global filters enforce portable tenant isolation; this applied PostgreSQL migration retains historical RLS for defense in depth.

namespace AegisCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V1_4_WizardAdminCredentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WizardAdminCredentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    RoleName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WizardAdminCredentials", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WizardAdminCredentials_TenantId_SessionId_Email",
                table: "WizardAdminCredentials",
                columns: new[] { "TenantId", "SessionId", "Email" },
                unique: true);

            migrationBuilder.Sql("ALTER TABLE \"WizardAdminCredentials\" ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("CREATE POLICY wizard_admin_credential_tenant_isolation ON \"WizardAdminCredentials\" USING (\"TenantId\" = finance_tenant_id()) WITH CHECK (\"TenantId\" = finance_tenant_id());");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WizardAdminCredentials");
        }
    }
}
