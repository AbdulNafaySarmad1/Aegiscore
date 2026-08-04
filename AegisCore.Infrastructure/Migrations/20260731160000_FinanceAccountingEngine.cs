using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FinanceAccountingEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CounterpartyReportingEntityId",
                table: "JournalEntryLines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReportingEntityId",
                table: "JournalEntryLines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CounterpartyReportingEntityId",
                table: "FinanceVoucherLines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReportingEntityId",
                table: "FinanceVoucherLines",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FinanceAccountingStandardPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StandardCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PolicyCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ValueJson = table.Column<string>(type: "jsonb", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceAccountingStandardPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceConsolidationGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    PresentationCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceConsolidationGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceConsolidationRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsolidationGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceConsolidationRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceEliminationRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsolidationGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    CounterpartAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceEliminationRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceReportingEntities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    FunctionalCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    ParentEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceReportingEntities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceRestatementRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceStandard = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TargetStandard = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SourceAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Factor = table.Column<decimal>(type: "numeric(19,8)", nullable: false),
                    Rationale = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceRestatementRules", x => x.Id);
                    table.CheckConstraint("CK_FinanceRestatementRules_Factor", "\"Factor\" <> 0 AND \"SourceAccountId\" <> \"TargetAccountId\"");
                });

            migrationBuilder.CreateTable(
                name: "FinanceRestatementRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceStandard = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TargetStandard = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PeriodFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceRestatementRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceConsolidationMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsolidationGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportingEntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceConsolidationMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceConsolidationMembers_FinanceConsolidationGroups_Cons~",
                        column: x => x.ConsolidationGroupId,
                        principalTable: "FinanceConsolidationGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceConsolidationMemoLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsolidationRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChartOfAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    LedgerBalance = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    EliminationAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    ConsolidatedBalance = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceConsolidationMemoLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceConsolidationMemoLines_FinanceConsolidationRuns_Cons~",
                        column: x => x.ConsolidationRunId,
                        principalTable: "FinanceConsolidationRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceRestatementMemoLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RestatementRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    RestatementRuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChartOfAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Debit = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Credit = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceRestatementMemoLines", x => x.Id);
                    table.CheckConstraint("CK_FinanceRestatementMemoLines_OneSide", "(\"Debit\" > 0 AND \"Credit\" = 0) OR (\"Credit\" > 0 AND \"Debit\" = 0)");
                    table.ForeignKey(
                        name: "FK_FinanceRestatementMemoLines_FinanceRestatementRuns_Restatem~",
                        column: x => x.RestatementRunId,
                        principalTable: "FinanceRestatementRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceAccountingStandardPolicies_TenantId_StandardCode_Pol~",
                table: "FinanceAccountingStandardPolicies",
                columns: new[] { "TenantId", "StandardCode", "PolicyCode", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceConsolidationGroups_TenantId_Name",
                table: "FinanceConsolidationGroups",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceConsolidationMembers_ConsolidationGroupId",
                table: "FinanceConsolidationMembers",
                column: "ConsolidationGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceConsolidationMembers_TenantId_ConsolidationGroupId_R~",
                table: "FinanceConsolidationMembers",
                columns: new[] { "TenantId", "ConsolidationGroupId", "ReportingEntityId", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceConsolidationMemoLines_ConsolidationRunId",
                table: "FinanceConsolidationMemoLines",
                column: "ConsolidationRunId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceConsolidationMemoLines_TenantId_ConsolidationRunId_C~",
                table: "FinanceConsolidationMemoLines",
                columns: new[] { "TenantId", "ConsolidationRunId", "ChartOfAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceConsolidationRuns_TenantId_ConsolidationGroupId_Crea~",
                table: "FinanceConsolidationRuns",
                columns: new[] { "TenantId", "ConsolidationGroupId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceEliminationRules_TenantId_ConsolidationGroupId_IsAct~",
                table: "FinanceEliminationRules",
                columns: new[] { "TenantId", "ConsolidationGroupId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceReportingEntities_TenantId_Code",
                table: "FinanceReportingEntities",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceReportingEntities_TenantId_ParentEntityId_IsActive",
                table: "FinanceReportingEntities",
                columns: new[] { "TenantId", "ParentEntityId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceRestatementMemoLines_RestatementRunId",
                table: "FinanceRestatementMemoLines",
                column: "RestatementRunId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceRestatementMemoLines_TenantId_RestatementRunId_Chart~",
                table: "FinanceRestatementMemoLines",
                columns: new[] { "TenantId", "RestatementRunId", "ChartOfAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceRestatementRules_TenantId_SourceStandard_TargetStand~",
                table: "FinanceRestatementRules",
                columns: new[] { "TenantId", "SourceStandard", "TargetStandard", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceRestatementRuns_TenantId_SourceStandard_TargetStanda~",
                table: "FinanceRestatementRuns",
                columns: new[] { "TenantId", "SourceStandard", "TargetStandard", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntryLines_TenantId_ReportingEntityId_ChartOfAccountId",
                table: "JournalEntryLines",
                columns: new[] { "TenantId", "ReportingEntityId", "ChartOfAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntryLines_TenantId_CounterpartyReportingEntityId",
                table: "JournalEntryLines",
                columns: new[] { "TenantId", "CounterpartyReportingEntityId" });

            migrationBuilder.Sql("""
                DO $$
                DECLARE table_name text;
                BEGIN
                  FOREACH table_name IN ARRAY ARRAY[
                    'FinanceAccountingStandardPolicies', 'FinanceReportingEntities',
                    'FinanceRestatementRules', 'FinanceRestatementRuns', 'FinanceRestatementMemoLines',
                    'FinanceConsolidationGroups', 'FinanceConsolidationMembers', 'FinanceEliminationRules',
                    'FinanceConsolidationRuns', 'FinanceConsolidationMemoLines'
                  ] LOOP
                    EXECUTE format('ALTER TABLE %I ENABLE ROW LEVEL SECURITY', table_name);
                    EXECUTE format('ALTER TABLE %I FORCE ROW LEVEL SECURITY', table_name);
                    EXECUTE format('CREATE POLICY %I ON %I USING ("TenantId" = finance_tenant_id()) WITH CHECK ("TenantId" = finance_tenant_id())', lower(table_name) || '_tenant_isolation', table_name);
                  END LOOP;
                END $$;

                CREATE TRIGGER finance_restatement_run_append_only BEFORE UPDATE OR DELETE ON "FinanceRestatementRuns" FOR EACH ROW EXECUTE FUNCTION finance_append_only_guard();
                CREATE TRIGGER finance_restatement_line_append_only BEFORE UPDATE OR DELETE ON "FinanceRestatementMemoLines" FOR EACH ROW EXECUTE FUNCTION finance_append_only_guard();
                CREATE TRIGGER finance_consolidation_run_append_only BEFORE UPDATE OR DELETE ON "FinanceConsolidationRuns" FOR EACH ROW EXECUTE FUNCTION finance_append_only_guard();
                CREATE TRIGGER finance_consolidation_line_append_only BEFORE UPDATE OR DELETE ON "FinanceConsolidationMemoLines" FOR EACH ROW EXECUTE FUNCTION finance_append_only_guard();

                CREATE OR REPLACE FUNCTION finance_accounting_policy_guard()
                RETURNS trigger LANGUAGE plpgsql AS $$
                DECLARE interest_allowed boolean;
                DECLARE account_name text;
                BEGIN
                  SELECT COALESCE((p."ValueJson"->>'interest_accounts_allowed')::boolean, true)
                    INTO interest_allowed
                    FROM "TenantFinanceConfigurations" c
                    JOIN "FinanceAccountingStandardPolicies" p
                      ON p."TenantId" = c."TenantId" AND p."StandardCode" = c."PrimaryStandard" AND p."PolicyCode" = 'profile' AND p."EffectiveTo" IS NULL
                   WHERE c."TenantId" = NEW."TenantId";
                  IF COALESCE(interest_allowed, true) = false THEN
                    SELECT "AccountName" INTO account_name FROM "ChartOfAccounts" WHERE "Id" = NEW."ChartOfAccountId" AND "TenantId" = NEW."TenantId";
                    IF account_name ILIKE '%interest%' THEN
                      RAISE EXCEPTION 'Tenant accounting policy prohibits interest accounts';
                    END IF;
                  END IF;
                  RETURN NEW;
                END;
                $$;
                CREATE TRIGGER finance_journal_line_accounting_policy BEFORE INSERT ON "JournalEntryLines" FOR EACH ROW EXECUTE FUNCTION finance_accounting_policy_guard();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS finance_consolidation_line_append_only ON "FinanceConsolidationMemoLines";
                DROP TRIGGER IF EXISTS finance_consolidation_run_append_only ON "FinanceConsolidationRuns";
                DROP TRIGGER IF EXISTS finance_restatement_line_append_only ON "FinanceRestatementMemoLines";
                DROP TRIGGER IF EXISTS finance_restatement_run_append_only ON "FinanceRestatementRuns";
                DROP TRIGGER IF EXISTS finance_journal_line_accounting_policy ON "JournalEntryLines";
                DROP FUNCTION IF EXISTS finance_accounting_policy_guard();
                """);
            migrationBuilder.DropTable(
                name: "FinanceAccountingStandardPolicies");

            migrationBuilder.DropTable(
                name: "FinanceConsolidationMembers");

            migrationBuilder.DropTable(
                name: "FinanceConsolidationMemoLines");

            migrationBuilder.DropTable(
                name: "FinanceEliminationRules");

            migrationBuilder.DropTable(
                name: "FinanceReportingEntities");

            migrationBuilder.DropTable(
                name: "FinanceRestatementMemoLines");

            migrationBuilder.DropTable(
                name: "FinanceRestatementRules");

            migrationBuilder.DropTable(
                name: "FinanceConsolidationGroups");

            migrationBuilder.DropTable(
                name: "FinanceConsolidationRuns");

            migrationBuilder.DropTable(
                name: "FinanceRestatementRuns");

            migrationBuilder.DropColumn(
                name: "CounterpartyReportingEntityId",
                table: "JournalEntryLines");

            migrationBuilder.DropColumn(
                name: "ReportingEntityId",
                table: "JournalEntryLines");

            migrationBuilder.DropColumn(
                name: "CounterpartyReportingEntityId",
                table: "FinanceVoucherLines");

            migrationBuilder.DropColumn(
                name: "ReportingEntityId",
                table: "FinanceVoucherLines");
        }
    }
}
