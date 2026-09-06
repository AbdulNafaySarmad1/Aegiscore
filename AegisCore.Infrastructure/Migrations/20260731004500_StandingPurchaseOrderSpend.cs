using AegisCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

// P11 multi-dialect note: application ITenantScoped global filters enforce portable tenant isolation; this applied PostgreSQL migration retains historical RLS for defense in depth.

[DbContext(typeof(AegisDbContext))]
[Migration("20260731004500_StandingPurchaseOrderSpend")]
public partial class StandingPurchaseOrderSpend : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.CreateTable("FinanceStandingPurchaseOrderSpend", t => new
        {
            Id = t.Column<Guid>("uuid", nullable: false), StandingPurchaseOrderId = t.Column<Guid>("uuid", nullable: false), Amount = t.Column<decimal>("numeric(19,4)", nullable: false),
            Description = t.Column<string>("character varying(500)", maxLength: 500, nullable: false), SpentAt = t.Column<DateTime>("timestamp with time zone", nullable: false), TenantId = t.Column<Guid>("uuid", nullable: false)
        }, constraints: t => { t.PrimaryKey("PK_FinanceStandingPurchaseOrderSpend", x => x.Id); t.ForeignKey("FK_FinanceStandingPurchaseOrderSpend_FinanceStandingPurchaseOrders_StandingPurchaseOrderId", x => x.StandingPurchaseOrderId, "FinanceStandingPurchaseOrders", "Id", onDelete: ReferentialAction.Restrict); });
        m.CreateIndex("IX_FinanceStandingPurchaseOrderSpend_TenantId_StandingPurchaseOrderId_SpentAt", "FinanceStandingPurchaseOrderSpend", ["TenantId", "StandingPurchaseOrderId", "SpentAt"]);
        m.Sql("""
            ALTER TABLE "FinanceStandingPurchaseOrderSpend" ENABLE ROW LEVEL SECURITY; ALTER TABLE "FinanceStandingPurchaseOrderSpend" FORCE ROW LEVEL SECURITY;
            CREATE POLICY finance_standing_po_spend_tenant_isolation ON "FinanceStandingPurchaseOrderSpend" USING ("TenantId" = finance_tenant_id()) WITH CHECK ("TenantId" = finance_tenant_id());
            """);
    }

    protected override void Down(MigrationBuilder m)
    {
        m.Sql("DROP POLICY IF EXISTS finance_standing_po_spend_tenant_isolation ON \"FinanceStandingPurchaseOrderSpend\";");
        m.DropTable("FinanceStandingPurchaseOrderSpend");
    }
}
