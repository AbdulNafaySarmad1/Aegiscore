using AegisCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

// P11 multi-dialect note: application ITenantScoped global filters enforce portable tenant isolation; this applied PostgreSQL migration retains historical RLS for defense in depth.

[DbContext(typeof(AegisDbContext))]
[Migration("20260731007000_FinanceTreasuryFeeds")]
public partial class FinanceTreasuryFeeds : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.CreateTable("FinanceBankAccounts", t => new
        {
            Id = t.Column<Guid>("uuid", nullable: false), Name = t.Column<string>("character varying(160)", maxLength: 160, nullable: false), AccountReference = t.Column<string>("character varying(34)", maxLength: 34, nullable: false), Currency = t.Column<string>("character varying(3)", maxLength: 3, nullable: false), ChartOfAccountId = t.Column<Guid>("uuid", nullable: false), IsActive = t.Column<bool>("boolean", nullable: false), TenantId = t.Column<Guid>("uuid", nullable: false)
        }, constraints: t => t.PrimaryKey("PK_FinanceBankAccounts", x => x.Id));
        m.CreateTable("FinanceBankFeedTransactions", t => new
        {
            Id = t.Column<Guid>("uuid", nullable: false), FinanceBankAccountId = t.Column<Guid>("uuid", nullable: false), ExternalId = t.Column<string>("character varying(160)", maxLength: 160, nullable: false), BookingDate = t.Column<DateTime>("timestamp with time zone", nullable: false), Amount = t.Column<decimal>("numeric(19,4)", nullable: false), Narration = t.Column<string>("character varying(1000)", maxLength: 1000, nullable: false), Status = t.Column<string>("character varying(20)", maxLength: 20, nullable: false), MatchedJournalEntryId = t.Column<Guid>("uuid", nullable: true), ImportedAt = t.Column<DateTime>("timestamp with time zone", nullable: false), TenantId = t.Column<Guid>("uuid", nullable: false)
        }, constraints: t => { t.PrimaryKey("PK_FinanceBankFeedTransactions", x => x.Id); t.ForeignKey("FinanceBankAccounts", x => x.FinanceBankAccountId, principalTable: "FinanceBankAccounts", principalColumn: "Id", onDelete: ReferentialAction.Restrict); });
        m.CreateIndex("IX_FinanceBankAccounts_TenantId_AccountReference", "FinanceBankAccounts", ["TenantId", "AccountReference"], unique: true);
        m.CreateIndex("IX_FinanceBankFeedTransactions_TenantId_FinanceBankAccountId_ExternalId", "FinanceBankFeedTransactions", ["TenantId", "FinanceBankAccountId", "ExternalId"], unique: true);
        m.CreateIndex("IX_FinanceBankFeedTransactions_TenantId_Status_BookingDate", "FinanceBankFeedTransactions", ["TenantId", "Status", "BookingDate"]);
        m.Sql("ALTER TABLE \"FinanceBankAccounts\" ENABLE ROW LEVEL SECURITY; ALTER TABLE \"FinanceBankAccounts\" FORCE ROW LEVEL SECURITY; CREATE POLICY finance_bank_account_tenant_isolation ON \"FinanceBankAccounts\" USING (\"TenantId\" = finance_tenant_id()) WITH CHECK (\"TenantId\" = finance_tenant_id()); ALTER TABLE \"FinanceBankFeedTransactions\" ENABLE ROW LEVEL SECURITY; ALTER TABLE \"FinanceBankFeedTransactions\" FORCE ROW LEVEL SECURITY; CREATE POLICY finance_bank_feed_tenant_isolation ON \"FinanceBankFeedTransactions\" USING (\"TenantId\" = finance_tenant_id()) WITH CHECK (\"TenantId\" = finance_tenant_id());");
    }

    protected override void Down(MigrationBuilder m)
    {
        m.Sql("DROP POLICY IF EXISTS finance_bank_feed_tenant_isolation ON \"FinanceBankFeedTransactions\"; DROP POLICY IF EXISTS finance_bank_account_tenant_isolation ON \"FinanceBankAccounts\";");
        m.DropTable("FinanceBankFeedTransactions");
        m.DropTable("FinanceBankAccounts");
    }
}
