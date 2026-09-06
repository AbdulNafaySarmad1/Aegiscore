using AegisCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

// P11 multi-dialect note: application ITenantScoped global filters enforce portable tenant isolation; this applied PostgreSQL migration retains historical RLS for defense in depth.

[DbContext(typeof(AegisDbContext))]
[Migration("20260731010000_FinanceInvestments")]
public partial class FinanceInvestments : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.CreateTable("FinanceInvestmentFunds", t => new
        {
            Id = t.Column<Guid>("uuid", nullable: false), Name = t.Column<string>("character varying(160)", maxLength: 160, nullable: false), FundType = t.Column<string>("character varying(30)", maxLength: 30, nullable: false), Currency = t.Column<string>("character varying(3)", maxLength: 3, nullable: false), Principal = t.Column<decimal>("numeric(19,4)", nullable: false), CurrentValue = t.Column<decimal>("numeric(19,4)", nullable: false), SpendingRatePercent = t.Column<decimal>("numeric(9,4)", nullable: false), ReserveTarget = t.Column<decimal>("numeric(19,4)", nullable: false), Status = t.Column<string>("character varying(20)", maxLength: 20, nullable: false), TenantId = t.Column<Guid>("uuid", nullable: false)
        }, constraints: t => t.PrimaryKey("PK_FinanceInvestmentFunds", x => x.Id));
        m.CreateTable("FinanceInvestmentHoldings", t => new
        {
            Id = t.Column<Guid>("uuid", nullable: false), FinanceInvestmentFundId = t.Column<Guid>("uuid", nullable: false), InstrumentType = t.Column<string>("character varying(100)", maxLength: 100, nullable: false), Issuer = t.Column<string>("character varying(200)", maxLength: 200, nullable: false), AssetClass = t.Column<string>("character varying(30)", maxLength: 30, nullable: false), Measurement = t.Column<string>("character varying(20)", maxLength: 20, nullable: false), Currency = t.Column<string>("character varying(3)", maxLength: 3, nullable: false), CostBasis = t.Column<decimal>("numeric(19,4)", nullable: false), MarketValue = t.Column<decimal>("numeric(19,4)", nullable: false), AnnualYieldPercent = t.Column<decimal>("numeric(9,4)", nullable: false), PurchaseDate = t.Column<DateTime>("timestamp with time zone", nullable: false), MaturityDate = t.Column<DateTime>("timestamp with time zone", nullable: true), Status = t.Column<string>("character varying(20)", maxLength: 20, nullable: false), TenantId = t.Column<Guid>("uuid", nullable: false)
        }, constraints: t => { t.PrimaryKey("PK_FinanceInvestmentHoldings", x => x.Id); t.ForeignKey("FinanceInvestmentFunds", x => x.FinanceInvestmentFundId, principalTable: "FinanceInvestmentFunds", principalColumn: "Id", onDelete: ReferentialAction.Restrict); });
        m.CreateTable("FinanceInvestmentTransactions", t => new
        {
            Id = t.Column<Guid>("uuid", nullable: false), FinanceInvestmentHoldingId = t.Column<Guid>("uuid", nullable: false), TransactionType = t.Column<string>("character varying(20)", maxLength: 20, nullable: false), Amount = t.Column<decimal>("numeric(19,4)", nullable: false), GainOrLoss = t.Column<decimal>("numeric(19,4)", nullable: false), Description = t.Column<string>("character varying(500)", maxLength: 500, nullable: false), TransactionDate = t.Column<DateTime>("timestamp with time zone", nullable: false), JournalEntryId = t.Column<Guid>("uuid", nullable: true), TenantId = t.Column<Guid>("uuid", nullable: false)
        }, constraints: t => { t.PrimaryKey("PK_FinanceInvestmentTransactions", x => x.Id); t.ForeignKey("FinanceInvestmentHoldings", x => x.FinanceInvestmentHoldingId, principalTable: "FinanceInvestmentHoldings", principalColumn: "Id", onDelete: ReferentialAction.Restrict); });
        m.CreateIndex("IX_FinanceInvestmentFunds_TenantId_Name", "FinanceInvestmentFunds", ["TenantId", "Name"], unique: true);
        m.CreateIndex("IX_FinanceInvestmentHoldings_TenantId_Issuer_InstrumentType", "FinanceInvestmentHoldings", ["TenantId", "Issuer", "InstrumentType"]);
        m.CreateIndex("IX_FinanceInvestmentHoldings_TenantId_MaturityDate_Status", "FinanceInvestmentHoldings", ["TenantId", "MaturityDate", "Status"]);
        m.CreateIndex("IX_FinanceInvestmentTransactions_TenantId_FinanceInvestmentHoldingId_TransactionDate", "FinanceInvestmentTransactions", ["TenantId", "FinanceInvestmentHoldingId", "TransactionDate"]);
        m.Sql("ALTER TABLE \"FinanceInvestmentFunds\" ENABLE ROW LEVEL SECURITY; ALTER TABLE \"FinanceInvestmentFunds\" FORCE ROW LEVEL SECURITY; CREATE POLICY finance_investment_fund_tenant_isolation ON \"FinanceInvestmentFunds\" USING (\"TenantId\" = finance_tenant_id()) WITH CHECK (\"TenantId\" = finance_tenant_id()); ALTER TABLE \"FinanceInvestmentHoldings\" ENABLE ROW LEVEL SECURITY; ALTER TABLE \"FinanceInvestmentHoldings\" FORCE ROW LEVEL SECURITY; CREATE POLICY finance_investment_holding_tenant_isolation ON \"FinanceInvestmentHoldings\" USING (\"TenantId\" = finance_tenant_id()) WITH CHECK (\"TenantId\" = finance_tenant_id()); ALTER TABLE \"FinanceInvestmentTransactions\" ENABLE ROW LEVEL SECURITY; ALTER TABLE \"FinanceInvestmentTransactions\" FORCE ROW LEVEL SECURITY; CREATE POLICY finance_investment_transaction_tenant_isolation ON \"FinanceInvestmentTransactions\" USING (\"TenantId\" = finance_tenant_id()) WITH CHECK (\"TenantId\" = finance_tenant_id());");
    }

    protected override void Down(MigrationBuilder m)
    {
        m.Sql("DROP POLICY IF EXISTS finance_investment_transaction_tenant_isolation ON \"FinanceInvestmentTransactions\"; DROP POLICY IF EXISTS finance_investment_holding_tenant_isolation ON \"FinanceInvestmentHoldings\"; DROP POLICY IF EXISTS finance_investment_fund_tenant_isolation ON \"FinanceInvestmentFunds\";");
        m.DropTable("FinanceInvestmentTransactions");
        m.DropTable("FinanceInvestmentHoldings");
        m.DropTable("FinanceInvestmentFunds");
    }
}
