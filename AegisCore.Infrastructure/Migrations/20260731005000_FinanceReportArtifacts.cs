using AegisCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

// P11 multi-dialect note: application ITenantScoped global filters enforce portable tenant isolation; this applied PostgreSQL migration retains historical RLS for defense in depth.

[DbContext(typeof(AegisDbContext))]
[Migration("20260731005000_FinanceReportArtifacts")]
public partial class FinanceReportArtifacts : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.CreateTable("FinanceReportArtifacts", t => new
        {
            Id = t.Column<Guid>("uuid", nullable: false), ReportType = t.Column<string>("character varying(50)", maxLength: 50, nullable: false), Format = t.Column<string>("character varying(10)", maxLength: 10, nullable: false), PeriodLabel = t.Column<string>("character varying(120)", maxLength: 120, nullable: false),
            StorageKey = t.Column<string>("character varying(300)", maxLength: 300, nullable: false), ContentHash = t.Column<string>("character varying(64)", maxLength: 64, nullable: false), PqcAlgorithm = t.Column<string>("character varying(80)", maxLength: 80, nullable: false), PqcKeyId = t.Column<string>("character varying(200)", maxLength: 200, nullable: false),
            PqcSignature = t.Column<byte[]>("bytea", nullable: false), PqcPublicKey = t.Column<byte[]>("bytea", nullable: false), GeneratedAt = t.Column<DateTime>("timestamp with time zone", nullable: false), GeneratedByUserId = t.Column<Guid>("uuid", nullable: false), TenantId = t.Column<Guid>("uuid", nullable: false)
        }, constraints: t => t.PrimaryKey("PK_FinanceReportArtifacts", x => x.Id));
        m.CreateIndex("IX_FinanceReportArtifacts_TenantId_ReportType_GeneratedAt", "FinanceReportArtifacts", ["TenantId", "ReportType", "GeneratedAt"]);
        m.Sql("""
            ALTER TABLE "FinanceReportArtifacts" ENABLE ROW LEVEL SECURITY; ALTER TABLE "FinanceReportArtifacts" FORCE ROW LEVEL SECURITY;
            CREATE POLICY finance_report_artifact_tenant_isolation ON "FinanceReportArtifacts" USING ("TenantId" = finance_tenant_id()) WITH CHECK ("TenantId" = finance_tenant_id());
            CREATE OR REPLACE FUNCTION finance_reject_report_artifact_mutation() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'Finance report artifacts are append-only'; END; $$;
            CREATE TRIGGER finance_report_artifact_no_update BEFORE UPDATE ON "FinanceReportArtifacts" FOR EACH ROW EXECUTE FUNCTION finance_reject_report_artifact_mutation();
            CREATE TRIGGER finance_report_artifact_no_delete BEFORE DELETE ON "FinanceReportArtifacts" FOR EACH ROW EXECUTE FUNCTION finance_reject_report_artifact_mutation();
            """);
    }

    protected override void Down(MigrationBuilder m)
    {
        m.Sql("DROP TRIGGER IF EXISTS finance_report_artifact_no_update ON \"FinanceReportArtifacts\"; DROP TRIGGER IF EXISTS finance_report_artifact_no_delete ON \"FinanceReportArtifacts\"; DROP FUNCTION IF EXISTS finance_reject_report_artifact_mutation(); DROP POLICY IF EXISTS finance_report_artifact_tenant_isolation ON \"FinanceReportArtifacts\";");
        m.DropTable("FinanceReportArtifacts");
    }
}
