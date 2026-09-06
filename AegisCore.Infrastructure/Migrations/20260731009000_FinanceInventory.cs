using AegisCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

// P11 multi-dialect note: application ITenantScoped global filters enforce portable tenant isolation; this applied PostgreSQL migration retains historical RLS for defense in depth.

[DbContext(typeof(AegisDbContext))]
[Migration("20260731009000_FinanceInventory")]
public partial class FinanceInventory : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.CreateTable("FinanceInventoryItems", t => new
        {
            Id = t.Column<Guid>("uuid", nullable: false), ItemCode = t.Column<string>("character varying(60)", maxLength: 60, nullable: false), ItemName = t.Column<string>("character varying(200)", maxLength: 200, nullable: false), Unit = t.Column<string>("character varying(30)", maxLength: 30, nullable: false), CostMethod = t.Column<string>("character varying(20)", maxLength: 20, nullable: false), OnHandQuantity = t.Column<decimal>("numeric(19,4)", nullable: false), WeightedAverageCost = t.Column<decimal>("numeric(19,4)", nullable: false), ReorderLevel = t.Column<decimal>("numeric(19,4)", nullable: false), ReorderQuantity = t.Column<decimal>("numeric(19,4)", nullable: false), Status = t.Column<string>("character varying(20)", maxLength: 20, nullable: false), TenantId = t.Column<Guid>("uuid", nullable: false)
        }, constraints: t => t.PrimaryKey("PK_FinanceInventoryItems", x => x.Id));
        m.CreateTable("FinanceInventoryMovements", t => new
        {
            Id = t.Column<Guid>("uuid", nullable: false), FinanceInventoryItemId = t.Column<Guid>("uuid", nullable: false), MovementType = t.Column<string>("character varying(30)", maxLength: 30, nullable: false), Quantity = t.Column<decimal>("numeric(19,4)", nullable: false), UnitCost = t.Column<decimal>("numeric(19,4)", nullable: false), TotalCost = t.Column<decimal>("numeric(19,4)", nullable: false), CostCenterId = t.Column<Guid>("uuid", nullable: true), Reference = t.Column<string>("character varying(160)", maxLength: 160, nullable: true), MovementDate = t.Column<DateTime>("timestamp with time zone", nullable: false), JournalEntryId = t.Column<Guid>("uuid", nullable: true), TenantId = t.Column<Guid>("uuid", nullable: false)
        }, constraints: t => { t.PrimaryKey("PK_FinanceInventoryMovements", x => x.Id); t.ForeignKey("FinanceInventoryItems", x => x.FinanceInventoryItemId, principalTable: "FinanceInventoryItems", principalColumn: "Id", onDelete: ReferentialAction.Restrict); });
        m.CreateIndex("IX_FinanceInventoryItems_TenantId_ItemCode", "FinanceInventoryItems", ["TenantId", "ItemCode"], unique: true);
        m.CreateIndex("IX_FinanceInventoryItems_TenantId_Status_OnHandQuantity", "FinanceInventoryItems", ["TenantId", "Status", "OnHandQuantity"]);
        m.CreateIndex("IX_FinanceInventoryMovements_TenantId_FinanceInventoryItemId_MovementDate", "FinanceInventoryMovements", ["TenantId", "FinanceInventoryItemId", "MovementDate"]);
        m.Sql("ALTER TABLE \"FinanceInventoryItems\" ENABLE ROW LEVEL SECURITY; ALTER TABLE \"FinanceInventoryItems\" FORCE ROW LEVEL SECURITY; CREATE POLICY finance_inventory_item_tenant_isolation ON \"FinanceInventoryItems\" USING (\"TenantId\" = finance_tenant_id()) WITH CHECK (\"TenantId\" = finance_tenant_id()); ALTER TABLE \"FinanceInventoryMovements\" ENABLE ROW LEVEL SECURITY; ALTER TABLE \"FinanceInventoryMovements\" FORCE ROW LEVEL SECURITY; CREATE POLICY finance_inventory_movement_tenant_isolation ON \"FinanceInventoryMovements\" USING (\"TenantId\" = finance_tenant_id()) WITH CHECK (\"TenantId\" = finance_tenant_id());");
    }

    protected override void Down(MigrationBuilder m)
    {
        m.Sql("DROP POLICY IF EXISTS finance_inventory_movement_tenant_isolation ON \"FinanceInventoryMovements\"; DROP POLICY IF EXISTS finance_inventory_item_tenant_isolation ON \"FinanceInventoryItems\";");
        m.DropTable("FinanceInventoryMovements");
        m.DropTable("FinanceInventoryItems");
    }
}
