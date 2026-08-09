using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260809120000_AddAnalyticsIntelligencePlatform")]
public partial class AddAnalyticsIntelligencePlatform : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE "AnalyticsDecisionSnapshots" (
                "Id" uuid PRIMARY KEY,
                "TenantId" uuid NOT NULL,
                "ExplanationType" varchar(50) NOT NULL,
                "SubjectType" varchar(50) NOT NULL,
                "SubjectId" uuid NOT NULL,
                "DecisionSource" varchar(20) NOT NULL,
                "PolicyVersion" varchar(128),
                "ExplanationVersion" varchar(30) NOT NULL,
                "InputHash" varchar(64) NOT NULL,
                "PayloadJson" jsonb NOT NULL,
                "DataCutoffAt" timestamptz NOT NULL,
                "CalculatedAt" timestamptz NOT NULL,
                "AuditId" uuid NOT NULL
            );

            CREATE TABLE "AnalyticsStudentTelemetrySnapshots" (
                "Id" uuid PRIMARY KEY,
                "TenantId" uuid NOT NULL,
                "StudentId" uuid NOT NULL,
                "CourseId" uuid,
                "Period" varchar(80) NOT NULL,
                "ComparisonPopulation" varchar(160) NOT NULL,
                "InputHash" varchar(64) NOT NULL,
                "PayloadJson" jsonb NOT NULL,
                "DataCutoffAt" timestamptz NOT NULL,
                "FreshnessAt" timestamptz NOT NULL
            );

            CREATE TABLE "AnalyticsModelPredictionSnapshots" (
                "Id" uuid PRIMARY KEY,
                "TenantId" uuid NOT NULL,
                "SubjectId" uuid NOT NULL,
                "SubjectType" varchar(50) NOT NULL,
                "ModelKind" varchar(40) NOT NULL,
                "ModelName" varchar(160) NOT NULL,
                "ModelVersion" varchar(80) NOT NULL,
                "FeatureSchemaVersion" varchar(80) NOT NULL,
                "PredictionTimestamp" timestamptz NOT NULL,
                "DataCutoffTimestamp" timestamptz NOT NULL,
                "RawPrediction" numeric(20,8) NOT NULL,
                "CalibratedProbability" numeric(20,8),
                "BaselineExpectedValue" numeric(20,8) NOT NULL,
                "FeatureValuesJson" jsonb NOT NULL,
                "ShapValuesJson" jsonb NOT NULL,
                "ExplanationAlgorithm" varchar(80) NOT NULL,
                "ExplanationAlgorithmVersion" varchar(80) NOT NULL,
                "InputHash" varchar(64) NOT NULL,
                "RequiresHumanReview" boolean NOT NULL DEFAULT true,
                "CreatedAt" timestamptz NOT NULL
            );

            CREATE TABLE "AnalyticsModelQualitySnapshots" (
                "Id" uuid PRIMARY KEY,
                "TenantId" uuid NOT NULL,
                "ModelName" varchar(160) NOT NULL,
                "ModelVersion" varchar(80) NOT NULL,
                "PayloadJson" jsonb NOT NULL,
                "EvaluationDataHash" varchar(64) NOT NULL,
                "EvaluationTimestamp" timestamptz NOT NULL
            );

            CREATE TABLE "AnalyticsGlobalExplanationSnapshots" (
                "Id" uuid PRIMARY KEY,
                "TenantId" uuid NOT NULL,
                "ModelName" varchar(160) NOT NULL,
                "ModelVersion" varchar(80) NOT NULL,
                "FeatureSchemaVersion" varchar(80) NOT NULL,
                "AuthorizedPopulation" varchar(200) NOT NULL,
                "SampleSize" integer NOT NULL CHECK ("SampleSize" >= 0),
                "PopulationStart" timestamptz NOT NULL,
                "PopulationEnd" timestamptz NOT NULL,
                "MeanAbsoluteShapValuesJson" jsonb NOT NULL,
                "ExplanationAlgorithm" varchar(80) NOT NULL,
                "ExplanationAlgorithmVersion" varchar(80) NOT NULL,
                "PopulationHash" varchar(64) NOT NULL,
                "CalculatedAt" timestamptz NOT NULL,
                CHECK ("PopulationEnd" >= "PopulationStart")
            );

            CREATE UNIQUE INDEX "UX_AnalyticsDecisionSnapshots_Evidence"
                ON "AnalyticsDecisionSnapshots" ("TenantId", "ExplanationType", "SubjectId", "InputHash");
            CREATE INDEX "IX_AnalyticsDecisionSnapshots_Subject"
                ON "AnalyticsDecisionSnapshots" ("TenantId", "SubjectType", "SubjectId", "CalculatedAt" DESC);
            CREATE INDEX "IX_AnalyticsStudentTelemetrySnapshots_Latest"
                ON "AnalyticsStudentTelemetrySnapshots" ("TenantId", "StudentId", "CourseId", "FreshnessAt" DESC);
            CREATE UNIQUE INDEX "UX_AnalyticsStudentTelemetrySnapshots_CourseEvidence"
                ON "AnalyticsStudentTelemetrySnapshots" ("TenantId", "StudentId", "CourseId", "Period", "InputHash") WHERE "CourseId" IS NOT NULL;
            CREATE UNIQUE INDEX "UX_AnalyticsStudentTelemetrySnapshots_OverallEvidence"
                ON "AnalyticsStudentTelemetrySnapshots" ("TenantId", "StudentId", "Period", "InputHash") WHERE "CourseId" IS NULL;
            CREATE INDEX "IX_AnalyticsModelPredictionSnapshots_Subject"
                ON "AnalyticsModelPredictionSnapshots" ("TenantId", "SubjectType", "SubjectId", "PredictionTimestamp" DESC);
            CREATE INDEX "IX_AnalyticsModelPredictionSnapshots_Model"
                ON "AnalyticsModelPredictionSnapshots" ("TenantId", "ModelName", "ModelVersion", "PredictionTimestamp" DESC);
            CREATE UNIQUE INDEX "UX_AnalyticsModelQualitySnapshots_Evidence"
                ON "AnalyticsModelQualitySnapshots" ("TenantId", "ModelName", "ModelVersion", "EvaluationDataHash");
            CREATE INDEX "IX_AnalyticsModelQualitySnapshots_Latest"
                ON "AnalyticsModelQualitySnapshots" ("TenantId", "ModelName", "EvaluationTimestamp" DESC);
            CREATE UNIQUE INDEX "UX_AnalyticsGlobalExplanationSnapshots_Evidence"
                ON "AnalyticsGlobalExplanationSnapshots" ("TenantId", "ModelName", "ModelVersion", "PopulationHash");
            CREATE INDEX "IX_AnalyticsGlobalExplanationSnapshots_Latest"
                ON "AnalyticsGlobalExplanationSnapshots" ("TenantId", "ModelName", "CalculatedAt" DESC);

            CREATE OR REPLACE FUNCTION analytics_tenant_id() RETURNS uuid
            LANGUAGE sql STABLE
            AS $$ SELECT NULLIF(current_setting('app.tenant_id', true), '')::uuid $$;

            CREATE OR REPLACE FUNCTION reject_analytics_snapshot_mutation() RETURNS trigger
            LANGUAGE plpgsql
            AS $$ BEGIN RAISE EXCEPTION 'analytics snapshots are immutable'; END $$;

            ALTER TABLE "AnalyticsDecisionSnapshots" ENABLE ROW LEVEL SECURITY;
            ALTER TABLE "AnalyticsDecisionSnapshots" FORCE ROW LEVEL SECURITY;
            CREATE POLICY analytics_decision_tenant_isolation ON "AnalyticsDecisionSnapshots"
                USING ("TenantId" = analytics_tenant_id()) WITH CHECK ("TenantId" = analytics_tenant_id());
            CREATE TRIGGER analytics_decision_immutable BEFORE UPDATE OR DELETE ON "AnalyticsDecisionSnapshots"
                FOR EACH ROW EXECUTE FUNCTION reject_analytics_snapshot_mutation();

            ALTER TABLE "AnalyticsStudentTelemetrySnapshots" ENABLE ROW LEVEL SECURITY;
            ALTER TABLE "AnalyticsStudentTelemetrySnapshots" FORCE ROW LEVEL SECURITY;
            CREATE POLICY analytics_telemetry_tenant_isolation ON "AnalyticsStudentTelemetrySnapshots"
                USING ("TenantId" = analytics_tenant_id()) WITH CHECK ("TenantId" = analytics_tenant_id());
            CREATE TRIGGER analytics_telemetry_immutable BEFORE UPDATE OR DELETE ON "AnalyticsStudentTelemetrySnapshots"
                FOR EACH ROW EXECUTE FUNCTION reject_analytics_snapshot_mutation();

            ALTER TABLE "AnalyticsModelPredictionSnapshots" ENABLE ROW LEVEL SECURITY;
            ALTER TABLE "AnalyticsModelPredictionSnapshots" FORCE ROW LEVEL SECURITY;
            CREATE POLICY analytics_prediction_tenant_isolation ON "AnalyticsModelPredictionSnapshots"
                USING ("TenantId" = analytics_tenant_id()) WITH CHECK ("TenantId" = analytics_tenant_id());
            CREATE TRIGGER analytics_prediction_immutable BEFORE UPDATE OR DELETE ON "AnalyticsModelPredictionSnapshots"
                FOR EACH ROW EXECUTE FUNCTION reject_analytics_snapshot_mutation();

            ALTER TABLE "AnalyticsModelQualitySnapshots" ENABLE ROW LEVEL SECURITY;
            ALTER TABLE "AnalyticsModelQualitySnapshots" FORCE ROW LEVEL SECURITY;
            CREATE POLICY analytics_quality_tenant_isolation ON "AnalyticsModelQualitySnapshots"
                USING ("TenantId" = analytics_tenant_id()) WITH CHECK ("TenantId" = analytics_tenant_id());
            CREATE TRIGGER analytics_quality_immutable BEFORE UPDATE OR DELETE ON "AnalyticsModelQualitySnapshots"
                FOR EACH ROW EXECUTE FUNCTION reject_analytics_snapshot_mutation();

            ALTER TABLE "AnalyticsGlobalExplanationSnapshots" ENABLE ROW LEVEL SECURITY;
            ALTER TABLE "AnalyticsGlobalExplanationSnapshots" FORCE ROW LEVEL SECURITY;
            CREATE POLICY analytics_global_explanation_tenant_isolation ON "AnalyticsGlobalExplanationSnapshots"
                USING ("TenantId" = analytics_tenant_id()) WITH CHECK ("TenantId" = analytics_tenant_id());
            CREATE TRIGGER analytics_global_explanation_immutable BEFORE UPDATE OR DELETE ON "AnalyticsGlobalExplanationSnapshots"
                FOR EACH ROW EXECUTE FUNCTION reject_analytics_snapshot_mutation();
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE IF EXISTS "AnalyticsGlobalExplanationSnapshots";
            DROP TABLE IF EXISTS "AnalyticsModelQualitySnapshots";
            DROP TABLE IF EXISTS "AnalyticsModelPredictionSnapshots";
            DROP TABLE IF EXISTS "AnalyticsStudentTelemetrySnapshots";
            DROP TABLE IF EXISTS "AnalyticsDecisionSnapshots";
            DROP FUNCTION IF EXISTS reject_analytics_snapshot_mutation();
            DROP FUNCTION IF EXISTS analytics_tenant_id();
            """);
    }
}
