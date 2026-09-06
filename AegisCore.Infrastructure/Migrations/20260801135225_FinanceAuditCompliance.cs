using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

// P11 multi-dialect note: application ITenantScoped global filters enforce portable tenant isolation; this applied PostgreSQL migration retains historical RLS for defense in depth.

namespace AegisCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FinanceAuditCompliance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinanceAuditEvidencePackages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuditorEngagementId = table.Column<Guid>(type: "uuid", nullable: true),
                    Framework = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    PeriodFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PqcAlgorithm = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    PqcKeyId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PqcSignature = table.Column<byte[]>(type: "bytea", nullable: false),
                    PqcPublicKey = table.Column<byte[]>(type: "bytea", nullable: false),
                    GeneratedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceAuditEvidencePackages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceAuditorEngagements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuditorName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AuditorEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ScopesJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceAuditorEngagements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceAuditSamples",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuditorEngagementId = table.Column<Guid>(type: "uuid", nullable: false),
                    PopulationType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    PeriodFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PopulationCount = table.Column<int>(type: "integer", nullable: false),
                    SampleSize = table.Column<int>(type: "integer", nullable: false),
                    SeedHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SelectedReferencesJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceAuditSamples", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceControlEvaluationBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Trigger = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ReferenceType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ControlCount = table.Column<int>(type: "integer", nullable: false),
                    PassedCount = table.Column<int>(type: "integer", nullable: false),
                    FlaggedCount = table.Column<int>(type: "integer", nullable: false),
                    ResultsJson = table.Column<string>(type: "jsonb", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EvaluatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceControlEvaluationBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceSodRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IncompatibleActionsJson = table.Column<string>(type: "jsonb", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceSodRules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceAuditEvidencePackages_TenantId_GeneratedAt",
                table: "FinanceAuditEvidencePackages",
                columns: new[] { "TenantId", "GeneratedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceAuditorEngagements_TenantId_ExpiresAt",
                table: "FinanceAuditorEngagements",
                columns: new[] { "TenantId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceAuditorEngagements_TokenHash",
                table: "FinanceAuditorEngagements",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceAuditSamples_TenantId_AuditorEngagementId_CreatedAt",
                table: "FinanceAuditSamples",
                columns: new[] { "TenantId", "AuditorEngagementId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceControlEvaluationBatches_TenantId_EvaluatedAt",
                table: "FinanceControlEvaluationBatches",
                columns: new[] { "TenantId", "EvaluatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceControlEvaluationBatches_TenantId_ReferenceType_Refe~",
                table: "FinanceControlEvaluationBatches",
                columns: new[] { "TenantId", "ReferenceType", "ReferenceId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceSodRules_TenantId_Code",
                table: "FinanceSodRules",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.Sql("""
                ALTER TABLE "FinanceControlEvaluationBatches" ADD CONSTRAINT "CK_FinanceControlEvaluationBatches_Counts" CHECK ("ControlCount" >= 50 AND "PassedCount" >= 0 AND "FlaggedCount" >= 0 AND "PassedCount" + "FlaggedCount" = "ControlCount");
                ALTER TABLE "FinanceAuditSamples" ADD CONSTRAINT "CK_FinanceAuditSamples_Counts" CHECK ("PopulationCount" >= 0 AND "SampleSize" >= 0 AND "SampleSize" <= "PopulationCount");
                ALTER TABLE "FinanceAuditorEngagements" ADD CONSTRAINT "CK_FinanceAuditorEngagements_Expiry" CHECK ("ExpiresAt" > "CreatedAt");
                CREATE UNIQUE INDEX "UX_FinanceAuditorEngagements_TenantId_Id" ON "FinanceAuditorEngagements" ("TenantId", "Id");
                ALTER TABLE "FinanceAuditSamples" ADD CONSTRAINT "FK_FinanceAuditSamples_TenantEngagement" FOREIGN KEY ("TenantId", "AuditorEngagementId") REFERENCES "FinanceAuditorEngagements" ("TenantId", "Id") ON DELETE RESTRICT;
                ALTER TABLE "FinanceAuditEvidencePackages" ADD CONSTRAINT "FK_FinanceAuditEvidencePackages_TenantEngagement" FOREIGN KEY ("TenantId", "AuditorEngagementId") REFERENCES "FinanceAuditorEngagements" ("TenantId", "Id") ON DELETE RESTRICT;
                ALTER TABLE "FinanceControlEvaluationBatches" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "FinanceControlEvaluationBatches" FORCE ROW LEVEL SECURITY;
                CREATE POLICY finance_control_evaluation_tenant_isolation ON "FinanceControlEvaluationBatches" USING ("TenantId" = finance_tenant_id()) WITH CHECK ("TenantId" = finance_tenant_id());
                ALTER TABLE "FinanceSodRules" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "FinanceSodRules" FORCE ROW LEVEL SECURITY;
                CREATE POLICY finance_sod_rule_tenant_isolation ON "FinanceSodRules" USING ("TenantId" = finance_tenant_id()) WITH CHECK ("TenantId" = finance_tenant_id());
                ALTER TABLE "FinanceAuditorEngagements" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "FinanceAuditorEngagements" FORCE ROW LEVEL SECURITY;
                CREATE POLICY finance_auditor_engagement_tenant_isolation ON "FinanceAuditorEngagements" USING ("TenantId" = finance_tenant_id()) WITH CHECK ("TenantId" = finance_tenant_id());
                ALTER TABLE "FinanceAuditSamples" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "FinanceAuditSamples" FORCE ROW LEVEL SECURITY;
                CREATE POLICY finance_audit_sample_tenant_isolation ON "FinanceAuditSamples" USING ("TenantId" = finance_tenant_id()) WITH CHECK ("TenantId" = finance_tenant_id());
                ALTER TABLE "FinanceAuditEvidencePackages" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "FinanceAuditEvidencePackages" FORCE ROW LEVEL SECURITY;
                CREATE POLICY finance_audit_evidence_tenant_isolation ON "FinanceAuditEvidencePackages" USING ("TenantId" = finance_tenant_id()) WITH CHECK ("TenantId" = finance_tenant_id());

                CREATE TRIGGER finance_control_evaluation_append_only BEFORE UPDATE OR DELETE ON "FinanceControlEvaluationBatches" FOR EACH ROW EXECUTE FUNCTION finance_append_only_guard();
                CREATE TRIGGER finance_audit_sample_append_only BEFORE UPDATE OR DELETE ON "FinanceAuditSamples" FOR EACH ROW EXECUTE FUNCTION finance_append_only_guard();
                CREATE TRIGGER finance_audit_evidence_append_only BEFORE UPDATE OR DELETE ON "FinanceAuditEvidencePackages" FOR EACH ROW EXECUTE FUNCTION finance_append_only_guard();

                ALTER TABLE "ComplianceFrameworks" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "ComplianceFrameworks" FORCE ROW LEVEL SECURITY;
                CREATE POLICY compliance_framework_tenant_isolation ON "ComplianceFrameworks" USING ("TenantId" = finance_tenant_id()) WITH CHECK ("TenantId" = finance_tenant_id());
                ALTER TABLE "ComplianceControls" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "ComplianceControls" FORCE ROW LEVEL SECURITY;
                CREATE POLICY compliance_control_tenant_isolation ON "ComplianceControls" USING ("TenantId" = finance_tenant_id()) WITH CHECK ("TenantId" = finance_tenant_id());
                ALTER TABLE "AuditFindings" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "AuditFindings" FORCE ROW LEVEL SECURITY;
                CREATE POLICY audit_finding_tenant_isolation ON "AuditFindings" USING ("TenantId" = finance_tenant_id()) WITH CHECK ("TenantId" = finance_tenant_id());
                ALTER TABLE "ControlEvidences" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "ControlEvidences" FORCE ROW LEVEL SECURITY;
                CREATE POLICY control_evidence_tenant_isolation ON "ControlEvidences" USING ("TenantId" = finance_tenant_id()) WITH CHECK ("TenantId" = finance_tenant_id());
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP POLICY IF EXISTS compliance_framework_tenant_isolation ON "ComplianceFrameworks";
                DROP POLICY IF EXISTS compliance_control_tenant_isolation ON "ComplianceControls";
                DROP POLICY IF EXISTS audit_finding_tenant_isolation ON "AuditFindings";
                DROP POLICY IF EXISTS control_evidence_tenant_isolation ON "ControlEvidences";
                ALTER TABLE "ComplianceFrameworks" DISABLE ROW LEVEL SECURITY;
                ALTER TABLE "ComplianceControls" DISABLE ROW LEVEL SECURITY;
                ALTER TABLE "AuditFindings" DISABLE ROW LEVEL SECURITY;
                ALTER TABLE "ControlEvidences" DISABLE ROW LEVEL SECURITY;
                """);
            migrationBuilder.DropTable(
                name: "FinanceAuditEvidencePackages");

            migrationBuilder.DropTable(
                name: "FinanceAuditorEngagements");

            migrationBuilder.DropTable(
                name: "FinanceAuditSamples");

            migrationBuilder.DropTable(
                name: "FinanceControlEvaluationBatches");

            migrationBuilder.DropTable(
                name: "FinanceSodRules");
        }
    }
}
