using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using AegisCore.Infrastructure;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

// P11 multi-dialect note: application ITenantScoped global filters enforce portable tenant isolation; this applied PostgreSQL migration retains historical RLS for defense in depth.

[DbContext(typeof(AegisDbContext))]
[Migration("20260731002500_FinanceSecurityDeposits")]
public partial class FinanceSecurityDeposits : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "FinanceSecurityDeposits",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                DepositType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                OriginalAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                RefundedAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                ForfeitedAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                RefundSlaDays = table.Column<int>(type: "integer", nullable: false),
                CollectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                RefundRequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                AssessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ResolutionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false)
            }, constraints: table => table.PrimaryKey("PK_FinanceSecurityDeposits", x => x.Id));

        migrationBuilder.CreateTable(
            name: "FinanceSecurityDepositDeductions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SecurityDepositId = table.Column<Guid>(type: "uuid", nullable: false),
                Category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                Amount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false)
            }, constraints: table =>
            {
                table.PrimaryKey("PK_FinanceSecurityDepositDeductions", x => x.Id);
                table.ForeignKey("FK_FinanceSecurityDepositDeductions_FinanceSecurityDeposits_SecurityDepositId", x => x.SecurityDepositId, "FinanceSecurityDeposits", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex("IX_FinanceSecurityDeposits_TenantId_StudentId_Status", "FinanceSecurityDeposits", ["TenantId", "StudentId", "Status"]);
        migrationBuilder.CreateIndex("IX_FinanceSecurityDepositDeductions_TenantId_SecurityDepositId_CreatedAt", "FinanceSecurityDepositDeductions", ["TenantId", "SecurityDepositId", "CreatedAt"]);
        migrationBuilder.CreateIndex("IX_FinanceSecurityDepositDeductions_SecurityDepositId", "FinanceSecurityDepositDeductions", "SecurityDepositId");
        migrationBuilder.Sql("""
            ALTER TABLE "FinanceSecurityDeposits" ENABLE ROW LEVEL SECURITY;
            ALTER TABLE "FinanceSecurityDeposits" FORCE ROW LEVEL SECURITY;
            ALTER TABLE "FinanceSecurityDepositDeductions" ENABLE ROW LEVEL SECURITY;
            ALTER TABLE "FinanceSecurityDepositDeductions" FORCE ROW LEVEL SECURITY;
            CREATE POLICY finance_security_deposit_tenant_isolation ON "FinanceSecurityDeposits"
              USING ("TenantId" = finance_tenant_id()) WITH CHECK ("TenantId" = finance_tenant_id());
            CREATE POLICY finance_security_deposit_deduction_tenant_isolation ON "FinanceSecurityDepositDeductions"
              USING ("TenantId" = finance_tenant_id()) WITH CHECK ("TenantId" = finance_tenant_id());
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP POLICY IF EXISTS finance_security_deposit_deduction_tenant_isolation ON "FinanceSecurityDepositDeductions";
            DROP POLICY IF EXISTS finance_security_deposit_tenant_isolation ON "FinanceSecurityDeposits";
            ALTER TABLE "FinanceSecurityDepositDeductions" DISABLE ROW LEVEL SECURITY;
            ALTER TABLE "FinanceSecurityDeposits" DISABLE ROW LEVEL SECURITY;
            """);
        migrationBuilder.DropTable("FinanceSecurityDepositDeductions");
        migrationBuilder.DropTable("FinanceSecurityDeposits");
    }
}
