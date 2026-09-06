using AegisCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

// P11 multi-dialect note: application ITenantScoped global filters enforce portable tenant isolation; this applied PostgreSQL migration retains historical RLS for defense in depth.

[DbContext(typeof(AegisDbContext))]
[Migration("20260731004000_FinanceVendors")]
public partial class FinanceVendors : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.CreateTable("FinanceVendors", t => new
        {
            Id = t.Column<Guid>("uuid", nullable: false), LegalName = t.Column<string>("character varying(200)", maxLength: 200, nullable: false),
            TradeName = t.Column<string>("character varying(200)", maxLength: 200, nullable: true), Tier = t.Column<string>("character varying(12)", maxLength: 12, nullable: false),
            TaxId = t.Column<string>("character varying(40)", maxLength: 40, nullable: true), InvoiceCurrency = t.Column<string>("character varying(3)", maxLength: 3, nullable: false),
            PaymentTerms = t.Column<string>("character varying(20)", maxLength: 20, nullable: false), Iban = t.Column<string>("character varying(60)", maxLength: 60, nullable: true),
            BankName = t.Column<string>("character varying(120)", maxLength: 120, nullable: true), Swift = t.Column<string>("character varying(40)", maxLength: 40, nullable: true),
            RaastId = t.Column<string>("character varying(40)", maxLength: 40, nullable: true), Category = t.Column<string>("character varying(30)", maxLength: 30, nullable: false),
            Status = t.Column<string>("character varying(20)", maxLength: 20, nullable: false), SanctionsStatus = t.Column<string>("character varying(20)", maxLength: 20, nullable: false),
            SanctionsReference = t.Column<string>("character varying(500)", maxLength: 500, nullable: true), RelationshipNotes = t.Column<string>("character varying(1000)", maxLength: 1000, nullable: true),
            PerformanceScore = t.Column<decimal>("numeric(5,2)", nullable: false), LastSanctionsScreenedAt = t.Column<DateTime>("timestamp with time zone", nullable: true), TenantId = t.Column<Guid>("uuid", nullable: false)
        }, constraints: t => t.PrimaryKey("PK_FinanceVendors", x => x.Id));
        m.CreateTable("FinanceVendorContacts", t => new
        {
            Id = t.Column<Guid>("uuid", nullable: false), VendorId = t.Column<Guid>("uuid", nullable: false), Name = t.Column<string>("character varying(160)", maxLength: 160, nullable: false),
            Designation = t.Column<string>("character varying(100)", maxLength: 100, nullable: true), Mobile = t.Column<string>("character varying(20)", maxLength: 20, nullable: true), Email = t.Column<string>("character varying(200)", maxLength: 200, nullable: true), TenantId = t.Column<Guid>("uuid", nullable: false)
        }, constraints: t => { t.PrimaryKey("PK_FinanceVendorContacts", x => x.Id); t.ForeignKey("FK_FinanceVendorContacts_FinanceVendors_VendorId", x => x.VendorId, "FinanceVendors", "Id", onDelete: ReferentialAction.Restrict); });
        m.CreateTable("FinanceStandingPurchaseOrders", t => new
        {
            Id = t.Column<Guid>("uuid", nullable: false), VendorId = t.Column<Guid>("uuid", nullable: false), Number = t.Column<string>("character varying(40)", maxLength: 40, nullable: false), Period = t.Column<string>("character varying(20)", maxLength: 20, nullable: false),
            LimitAmount = t.Column<decimal>("numeric(19,4)", nullable: false), UsedAmount = t.Column<decimal>("numeric(19,4)", nullable: false), StartsOn = t.Column<DateOnly>("date", nullable: false), EndsOn = t.Column<DateOnly>("date", nullable: false), Status = t.Column<string>("character varying(20)", maxLength: 20, nullable: false), TenantId = t.Column<Guid>("uuid", nullable: false)
        }, constraints: t => { t.PrimaryKey("PK_FinanceStandingPurchaseOrders", x => x.Id); t.ForeignKey("FK_FinanceStandingPurchaseOrders_FinanceVendors_VendorId", x => x.VendorId, "FinanceVendors", "Id", onDelete: ReferentialAction.Restrict); });
        m.CreateIndex("IX_FinanceVendors_TenantId_LegalName", "FinanceVendors", ["TenantId", "LegalName"]);
        m.CreateIndex("IX_FinanceVendors_TenantId_Status_Category", "FinanceVendors", ["TenantId", "Status", "Category"]);
        m.CreateIndex("IX_FinanceVendorContacts_TenantId_VendorId", "FinanceVendorContacts", ["TenantId", "VendorId"]);
        m.CreateIndex("IX_FinanceStandingPurchaseOrders_TenantId_Number", "FinanceStandingPurchaseOrders", ["TenantId", "Number"], unique: true);
        m.CreateIndex("IX_FinanceStandingPurchaseOrders_TenantId_Status_EndsOn", "FinanceStandingPurchaseOrders", ["TenantId", "Status", "EndsOn"]);
        m.Sql("""
            ALTER TABLE "FinanceVendors" ENABLE ROW LEVEL SECURITY; ALTER TABLE "FinanceVendors" FORCE ROW LEVEL SECURITY;
            ALTER TABLE "FinanceVendorContacts" ENABLE ROW LEVEL SECURITY; ALTER TABLE "FinanceVendorContacts" FORCE ROW LEVEL SECURITY;
            ALTER TABLE "FinanceStandingPurchaseOrders" ENABLE ROW LEVEL SECURITY; ALTER TABLE "FinanceStandingPurchaseOrders" FORCE ROW LEVEL SECURITY;
            CREATE POLICY finance_vendor_tenant_isolation ON "FinanceVendors" USING ("TenantId" = finance_tenant_id()) WITH CHECK ("TenantId" = finance_tenant_id());
            CREATE POLICY finance_vendor_contact_tenant_isolation ON "FinanceVendorContacts" USING ("TenantId" = finance_tenant_id()) WITH CHECK ("TenantId" = finance_tenant_id());
            CREATE POLICY finance_standing_po_tenant_isolation ON "FinanceStandingPurchaseOrders" USING ("TenantId" = finance_tenant_id()) WITH CHECK ("TenantId" = finance_tenant_id());
            """);
    }

    protected override void Down(MigrationBuilder m)
    {
        m.Sql("DROP POLICY IF EXISTS finance_standing_po_tenant_isolation ON \"FinanceStandingPurchaseOrders\"; DROP POLICY IF EXISTS finance_vendor_contact_tenant_isolation ON \"FinanceVendorContacts\"; DROP POLICY IF EXISTS finance_vendor_tenant_isolation ON \"FinanceVendors\";");
        m.DropTable("FinanceStandingPurchaseOrders"); m.DropTable("FinanceVendorContacts"); m.DropTable("FinanceVendors");
    }
}
