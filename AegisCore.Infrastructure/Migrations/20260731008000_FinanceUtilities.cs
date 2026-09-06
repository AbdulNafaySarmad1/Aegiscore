using AegisCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

// P11 multi-dialect note: application ITenantScoped global filters enforce portable tenant isolation; this applied PostgreSQL migration retains historical RLS for defense in depth.

[DbContext(typeof(AegisDbContext))]
[Migration("20260731008000_FinanceUtilities")]
public partial class FinanceUtilities : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.CreateTable("FinanceUtilityBills", t => new
        {
            Id = t.Column<Guid>("uuid", nullable: false), UtilityType = t.Column<string>("character varying(30)", maxLength: 30, nullable: false), Provider = t.Column<string>("character varying(200)", maxLength: 200, nullable: false), AccountNumber = t.Column<string>("character varying(80)", maxLength: 80, nullable: false), Currency = t.Column<string>("character varying(3)", maxLength: 3, nullable: false), AmountDue = t.Column<decimal>("numeric(19,4)", nullable: false), UnitsConsumed = t.Column<decimal>("numeric(19,4)", nullable: false), BillingPeriodStart = t.Column<DateTime>("timestamp with time zone", nullable: false), BillingPeriodEnd = t.Column<DateTime>("timestamp with time zone", nullable: false), DueDate = t.Column<DateTime>("timestamp with time zone", nullable: false), Status = t.Column<string>("character varying(20)", maxLength: 20, nullable: false), JournalEntryId = t.Column<Guid>("uuid", nullable: true), ApprovedAt = t.Column<DateTime>("timestamp with time zone", nullable: true), ApprovedByUserId = t.Column<Guid>("uuid", nullable: true), TenantId = t.Column<Guid>("uuid", nullable: false)
        }, constraints: t => t.PrimaryKey("PK_FinanceUtilityBills", x => x.Id));
        m.CreateTable("FinanceUtilityAllocations", t => new
        {
            Id = t.Column<Guid>("uuid", nullable: false), FinanceUtilityBillId = t.Column<Guid>("uuid", nullable: false), CostCenterId = t.Column<Guid>("uuid", nullable: false), Amount = t.Column<decimal>("numeric(19,4)", nullable: false), TenantId = t.Column<Guid>("uuid", nullable: false)
        }, constraints: t => { t.PrimaryKey("PK_FinanceUtilityAllocations", x => x.Id); t.ForeignKey("FinanceUtilityBills", x => x.FinanceUtilityBillId, principalTable: "FinanceUtilityBills", principalColumn: "Id", onDelete: ReferentialAction.Restrict); });
        m.CreateIndex("IX_FinanceUtilityBills_TenantId_AccountNumber_BillingPeriodStart_BillingPeriodEnd", "FinanceUtilityBills", ["TenantId", "AccountNumber", "BillingPeriodStart", "BillingPeriodEnd"], unique: true);
        m.CreateIndex("IX_FinanceUtilityBills_TenantId_Status_DueDate", "FinanceUtilityBills", ["TenantId", "Status", "DueDate"]);
        m.CreateIndex("IX_FinanceUtilityAllocations_TenantId_FinanceUtilityBillId_CostCenterId", "FinanceUtilityAllocations", ["TenantId", "FinanceUtilityBillId", "CostCenterId"], unique: true);
        m.Sql("ALTER TABLE \"FinanceUtilityBills\" ENABLE ROW LEVEL SECURITY; ALTER TABLE \"FinanceUtilityBills\" FORCE ROW LEVEL SECURITY; CREATE POLICY finance_utility_bill_tenant_isolation ON \"FinanceUtilityBills\" USING (\"TenantId\" = finance_tenant_id()) WITH CHECK (\"TenantId\" = finance_tenant_id()); ALTER TABLE \"FinanceUtilityAllocations\" ENABLE ROW LEVEL SECURITY; ALTER TABLE \"FinanceUtilityAllocations\" FORCE ROW LEVEL SECURITY; CREATE POLICY finance_utility_allocation_tenant_isolation ON \"FinanceUtilityAllocations\" USING (\"TenantId\" = finance_tenant_id()) WITH CHECK (\"TenantId\" = finance_tenant_id());");
    }

    protected override void Down(MigrationBuilder m)
    {
        m.Sql("DROP POLICY IF EXISTS finance_utility_allocation_tenant_isolation ON \"FinanceUtilityAllocations\"; DROP POLICY IF EXISTS finance_utility_bill_tenant_isolation ON \"FinanceUtilityBills\";");
        m.DropTable("FinanceUtilityAllocations");
        m.DropTable("FinanceUtilityBills");
    }
}
