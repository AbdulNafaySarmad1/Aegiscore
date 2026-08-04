using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FinanceComplianceTenantIsolation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX "UX_ComplianceFrameworks_TenantId_Id" ON "ComplianceFrameworks" ("TenantId", "Id");
                CREATE UNIQUE INDEX "UX_ComplianceControls_TenantId_Id" ON "ComplianceControls" ("TenantId", "Id");
                CREATE UNIQUE INDEX "UX_AuditFindings_TenantId_Id" ON "AuditFindings" ("TenantId", "Id");

                ALTER TABLE "ComplianceControls" ADD CONSTRAINT "FK_ComplianceControls_TenantFramework" FOREIGN KEY ("TenantId", "FrameworkId") REFERENCES "ComplianceFrameworks" ("TenantId", "Id") ON DELETE RESTRICT;
                ALTER TABLE "ControlEvidences" ADD CONSTRAINT "FK_ControlEvidences_TenantControl" FOREIGN KEY ("TenantId", "ControlId") REFERENCES "ComplianceControls" ("TenantId", "Id") ON DELETE RESTRICT;
                ALTER TABLE "AuditFindings" ADD CONSTRAINT "FK_AuditFindings_TenantControl" FOREIGN KEY ("TenantId", "ControlId") REFERENCES "ComplianceControls" ("TenantId", "Id") ON DELETE RESTRICT;
                ALTER TABLE "CorrectiveActionPlans" ADD CONSTRAINT "FK_CorrectiveActionPlans_TenantFinding" FOREIGN KEY ("TenantId", "FindingId") REFERENCES "AuditFindings" ("TenantId", "Id") ON DELETE RESTRICT;

                ALTER TABLE "CorrectiveActionPlans" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "CorrectiveActionPlans" FORCE ROW LEVEL SECURITY;
                CREATE POLICY corrective_action_plan_tenant_isolation ON "CorrectiveActionPlans" USING ("TenantId" = finance_tenant_id()) WITH CHECK ("TenantId" = finance_tenant_id());
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP POLICY IF EXISTS corrective_action_plan_tenant_isolation ON "CorrectiveActionPlans";
                ALTER TABLE "CorrectiveActionPlans" DISABLE ROW LEVEL SECURITY;

                ALTER TABLE "CorrectiveActionPlans" DROP CONSTRAINT IF EXISTS "FK_CorrectiveActionPlans_TenantFinding";
                ALTER TABLE "AuditFindings" DROP CONSTRAINT IF EXISTS "FK_AuditFindings_TenantControl";
                ALTER TABLE "ControlEvidences" DROP CONSTRAINT IF EXISTS "FK_ControlEvidences_TenantControl";
                ALTER TABLE "ComplianceControls" DROP CONSTRAINT IF EXISTS "FK_ComplianceControls_TenantFramework";

                DROP INDEX IF EXISTS "UX_AuditFindings_TenantId_Id";
                DROP INDEX IF EXISTS "UX_ComplianceControls_TenantId_Id";
                DROP INDEX IF EXISTS "UX_ComplianceFrameworks_TenantId_Id";
                """);
        }
    }
}
