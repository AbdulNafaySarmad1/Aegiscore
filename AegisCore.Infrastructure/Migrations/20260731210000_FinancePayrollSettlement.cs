using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FinancePayrollSettlement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinanceGratuityAssumptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DiscountRatePercent = table.Column<decimal>(type: "numeric(9,6)", nullable: false),
                    SalaryGrowthRatePercent = table.Column<decimal>(type: "numeric(9,6)", nullable: false),
                    TurnoverRatePercent = table.Column<decimal>(type: "numeric(9,6)", nullable: false),
                    BenefitDivisor = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    RetirementHorizonYears = table.Column<int>(type: "integer", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceGratuityAssumptions", x => x.Id);
                    table.CheckConstraint("CK_FinanceGratuityAssumptions_Rates", "\"DiscountRatePercent\" >= 0 AND \"SalaryGrowthRatePercent\" >= 0 AND \"TurnoverRatePercent\" >= 0 AND \"BenefitDivisor\" > 0 AND \"RetirementHorizonYears\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "FinanceGratuityValuations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssumptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ValuationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DefinedBenefitObligation = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    CurrentServiceCost = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    InterestCost = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    JournalEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceGratuityValuations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinancePayrollBankInstructions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    EncryptedAccountReference = table.Column<byte[]>(type: "bytea", nullable: false),
                    RoutingCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    AccountName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancePayrollBankInstructions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinancePayrollPaymentFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PayrollPostingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Format = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PqcAlgorithm = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    PqcKeyId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PqcSignature = table.Column<byte[]>(type: "bytea", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    RecordCount = table.Column<int>(type: "integer", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GeneratedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancePayrollPaymentFiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceGratuityAssumptions_TenantId_EffectiveFrom",
                table: "FinanceGratuityAssumptions",
                columns: new[] { "TenantId", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceGratuityValuations_TenantId_ValuationDate",
                table: "FinanceGratuityValuations",
                columns: new[] { "TenantId", "ValuationDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancePayrollBankInstructions_TenantId_EmployeeId",
                table: "FinancePayrollBankInstructions",
                columns: new[] { "TenantId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancePayrollPaymentFiles_TenantId_PayrollPostingId_Format",
                table: "FinancePayrollPaymentFiles",
                columns: new[] { "TenantId", "PayrollPostingId", "Format" },
                unique: true);

            migrationBuilder.AddForeignKey("FK_FinancePayrollBankInstructions_Employees_EmployeeId", "FinancePayrollBankInstructions", "EmployeeId", "Employees", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey("FK_FinancePayrollPaymentFiles_FinancePayrollPostings_PayrollPostingId", "FinancePayrollPaymentFiles", "PayrollPostingId", "FinancePayrollPostings", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey("FK_FinanceGratuityValuations_FinanceGratuityAssumptions_AssumptionId", "FinanceGratuityValuations", "AssumptionId", "FinanceGratuityAssumptions", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey("FK_FinanceGratuityValuations_JournalEntries_JournalEntryId", "FinanceGratuityValuations", "JournalEntryId", "JournalEntries", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.Sql("""
                DO $$ DECLARE table_name text; BEGIN
                  FOREACH table_name IN ARRAY ARRAY['FinancePayrollBankInstructions','FinancePayrollPaymentFiles','FinanceGratuityAssumptions','FinanceGratuityValuations'] LOOP
                    EXECUTE format('ALTER TABLE %I ENABLE ROW LEVEL SECURITY', table_name);
                    EXECUTE format('ALTER TABLE %I FORCE ROW LEVEL SECURITY', table_name);
                    EXECUTE format('CREATE POLICY %I ON %I USING ("TenantId" = finance_tenant_id()) WITH CHECK ("TenantId" = finance_tenant_id())', lower(table_name) || '_tenant_isolation', table_name);
                  END LOOP;
                END $$;
                CREATE TRIGGER finance_payroll_payment_file_append_only BEFORE UPDATE OR DELETE ON "FinancePayrollPaymentFiles" FOR EACH ROW EXECUTE FUNCTION finance_append_only_guard();
                CREATE TRIGGER finance_gratuity_valuation_append_only BEFORE UPDATE OR DELETE ON "FinanceGratuityValuations" FOR EACH ROW EXECUTE FUNCTION finance_append_only_guard();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS finance_gratuity_valuation_append_only ON \"FinanceGratuityValuations\"; DROP TRIGGER IF EXISTS finance_payroll_payment_file_append_only ON \"FinancePayrollPaymentFiles\";");
            migrationBuilder.DropTable(
                name: "FinanceGratuityAssumptions");

            migrationBuilder.DropTable(
                name: "FinanceGratuityValuations");

            migrationBuilder.DropTable(
                name: "FinancePayrollBankInstructions");

            migrationBuilder.DropTable(
                name: "FinancePayrollPaymentFiles");
        }
    }
}
