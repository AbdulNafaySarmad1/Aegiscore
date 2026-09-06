using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

// P11 multi-dialect note: application ITenantScoped global filters enforce portable tenant isolation; this applied PostgreSQL migration retains historical RLS for defense in depth.

[DbContext(typeof(AegisDbContext))]
[Migration("20260731130000_FinancePettyCash")]
public partial class FinancePettyCash : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.CreateTable("FinancePettyCashFunds", t => new
        {
            Id = t.Column<Guid>("uuid", nullable: false),
            DepartmentId = t.Column<Guid>("uuid", nullable: false),
            CustodianUserId = t.Column<Guid>("uuid", nullable: false),
            Currency = t.Column<string>("character varying(3)", maxLength: 3, nullable: false),
            FloatAmount = t.Column<decimal>("numeric(19,4)", nullable: false),
            CurrentBalance = t.Column<decimal>("numeric(19,4)", nullable: false),
            IsActive = t.Column<bool>("boolean", nullable: false),
            CreatedAt = t.Column<DateTime>("timestamp with time zone", nullable: false),
            UpdatedAt = t.Column<DateTime>("timestamp with time zone", nullable: false),
            TenantId = t.Column<Guid>("uuid", nullable: false)
        }, constraints: t =>
        {
            t.PrimaryKey("PK_FinancePettyCashFunds", x => x.Id);
            t.CheckConstraint("CK_FinancePettyCashFunds_PositiveFloat", "\"FloatAmount\" > 0 AND \"CurrentBalance\" >= 0 AND \"CurrentBalance\" <= \"FloatAmount\"");
        });
        m.CreateTable("FinancePettyCashExpenses", t => new
        {
            Id = t.Column<Guid>("uuid", nullable: false),
            FinancePettyCashFundId = t.Column<Guid>("uuid", nullable: false),
            Category = t.Column<string>("character varying(80)", maxLength: 80, nullable: false),
            Description = t.Column<string>("character varying(500)", maxLength: 500, nullable: false),
            VendorName = t.Column<string>("character varying(200)", maxLength: 200, nullable: true),
            Amount = t.Column<decimal>("numeric(19,4)", nullable: false),
            ReceiptHash = t.Column<string>("character varying(200)", maxLength: 200, nullable: true),
            SpentAt = t.Column<DateTime>("timestamp with time zone", nullable: false),
            CreatedAt = t.Column<DateTime>("timestamp with time zone", nullable: false),
            CreatedByUserId = t.Column<Guid>("uuid", nullable: false),
            TenantId = t.Column<Guid>("uuid", nullable: false)
        }, constraints: t =>
        {
            t.PrimaryKey("PK_FinancePettyCashExpenses", x => x.Id);
            t.ForeignKey("FK_FinancePettyCashExpenses_FinancePettyCashFunds_FinancePettyCashFundId", x => x.FinancePettyCashFundId, "FinancePettyCashFunds", "Id", onDelete: ReferentialAction.Restrict);
            t.CheckConstraint("CK_FinancePettyCashExpenses_PositiveAmount", "\"Amount\" > 0");
        });
        m.CreateTable("FinancePettyCashCounts", t => new
        {
            Id = t.Column<Guid>("uuid", nullable: false),
            FinancePettyCashFundId = t.Column<Guid>("uuid", nullable: false),
            SystemBalance = t.Column<decimal>("numeric(19,4)", nullable: false),
            PhysicalBalance = t.Column<decimal>("numeric(19,4)", nullable: false),
            Discrepancy = t.Column<decimal>("numeric(19,4)", nullable: false),
            Status = t.Column<string>("character varying(20)", maxLength: 20, nullable: false),
            Notes = t.Column<string>("character varying(500)", maxLength: 500, nullable: true),
            InitiatedByUserId = t.Column<Guid>("uuid", nullable: false),
            ConfirmedByUserId = t.Column<Guid>("uuid", nullable: false),
            CountedAt = t.Column<DateTime>("timestamp with time zone", nullable: false),
            TenantId = t.Column<Guid>("uuid", nullable: false)
        }, constraints: t =>
        {
            t.PrimaryKey("PK_FinancePettyCashCounts", x => x.Id);
            t.ForeignKey("FK_FinancePettyCashCounts_FinancePettyCashFunds_FinancePettyCashFundId", x => x.FinancePettyCashFundId, "FinancePettyCashFunds", "Id", onDelete: ReferentialAction.Restrict);
            t.CheckConstraint("CK_FinancePettyCashCounts_NonNegativeBalances", "\"SystemBalance\" >= 0 AND \"PhysicalBalance\" >= 0");
        });
        m.CreateIndex("IX_FinancePettyCashFunds_TenantId_DepartmentId", "FinancePettyCashFunds", ["TenantId", "DepartmentId"], unique: true);
        m.CreateIndex("IX_FinancePettyCashFunds_TenantId_IsActive", "FinancePettyCashFunds", ["TenantId", "IsActive"]);
        m.CreateIndex("IX_FinancePettyCashExpenses_TenantId_Fund_SpentAt", "FinancePettyCashExpenses", ["TenantId", "FinancePettyCashFundId", "SpentAt"]);
        m.CreateIndex("IX_FinancePettyCashCounts_TenantId_Fund_CountedAt", "FinancePettyCashCounts", ["TenantId", "FinancePettyCashFundId", "CountedAt"]);
        m.Sql("ALTER TABLE \"FinancePettyCashFunds\" ENABLE ROW LEVEL SECURITY; ALTER TABLE \"FinancePettyCashFunds\" FORCE ROW LEVEL SECURITY; CREATE POLICY finance_petty_cash_fund_tenant_isolation ON \"FinancePettyCashFunds\" USING (\"TenantId\" = finance_tenant_id()) WITH CHECK (\"TenantId\" = finance_tenant_id()); ALTER TABLE \"FinancePettyCashExpenses\" ENABLE ROW LEVEL SECURITY; ALTER TABLE \"FinancePettyCashExpenses\" FORCE ROW LEVEL SECURITY; CREATE POLICY finance_petty_cash_expense_tenant_isolation ON \"FinancePettyCashExpenses\" USING (\"TenantId\" = finance_tenant_id()) WITH CHECK (\"TenantId\" = finance_tenant_id()); ALTER TABLE \"FinancePettyCashCounts\" ENABLE ROW LEVEL SECURITY; ALTER TABLE \"FinancePettyCashCounts\" FORCE ROW LEVEL SECURITY; CREATE POLICY finance_petty_cash_count_tenant_isolation ON \"FinancePettyCashCounts\" USING (\"TenantId\" = finance_tenant_id()) WITH CHECK (\"TenantId\" = finance_tenant_id());");
    }

    protected override void Down(MigrationBuilder m)
    {
        m.Sql("DROP POLICY IF EXISTS finance_petty_cash_count_tenant_isolation ON \"FinancePettyCashCounts\"; DROP POLICY IF EXISTS finance_petty_cash_expense_tenant_isolation ON \"FinancePettyCashExpenses\"; DROP POLICY IF EXISTS finance_petty_cash_fund_tenant_isolation ON \"FinancePettyCashFunds\";");
        m.DropTable("FinancePettyCashCounts");
        m.DropTable("FinancePettyCashExpenses");
        m.DropTable("FinancePettyCashFunds");
    }
}
