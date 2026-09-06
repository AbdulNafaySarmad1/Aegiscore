using AegisCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

// P11 multi-dialect note: application ITenantScoped global filters enforce portable tenant isolation; this applied PostgreSQL migration retains historical RLS for defense in depth.

[DbContext(typeof(AegisDbContext))]
[Migration("20260731010500_FinanceTax")]
public partial class FinanceTax : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.CreateTable("FinanceTaxRules", t => new
        {
            Id = t.Column<Guid>("uuid", nullable: false), TaxType = t.Column<string>("character varying(30)", maxLength: 30, nullable: false), Jurisdiction = t.Column<string>("character varying(80)", maxLength: 80, nullable: false), VendorCategory = t.Column<string>("character varying(30)", maxLength: 30, nullable: false), FilerRatePercent = t.Column<decimal>("numeric(9,4)", nullable: false), NonFilerMultiplier = t.Column<decimal>("numeric(9,4)", nullable: false), EffectiveFrom = t.Column<DateTime>("timestamp with time zone", nullable: false), EffectiveTo = t.Column<DateTime>("timestamp with time zone", nullable: true), IsActive = t.Column<bool>("boolean", nullable: false), TenantId = t.Column<Guid>("uuid", nullable: false)
        }, constraints: t => t.PrimaryKey("PK_FinanceTaxRules", x => x.Id));
        m.CreateTable("FinanceWithholdingRecords", t => new
        {
            Id = t.Column<Guid>("uuid", nullable: false), VendorId = t.Column<Guid>("uuid", nullable: true), TaxRuleId = t.Column<Guid>("uuid", nullable: true), TaxType = t.Column<string>("character varying(30)", maxLength: 30, nullable: false), FilingPeriod = t.Column<string>("character varying(20)", maxLength: 20, nullable: false), PaymentReference = t.Column<string>("character varying(160)", maxLength: 160, nullable: false), GrossAmount = t.Column<decimal>("numeric(19,4)", nullable: false), AppliedRatePercent = t.Column<decimal>("numeric(9,4)", nullable: false), WithheldAmount = t.Column<decimal>("numeric(19,4)", nullable: false), FilerStatus = t.Column<string>("character varying(20)", maxLength: 20, nullable: false), Status = t.Column<string>("character varying(20)", maxLength: 20, nullable: false), JournalEntryId = t.Column<Guid>("uuid", nullable: false), WithheldAt = t.Column<DateTime>("timestamp with time zone", nullable: false), TenantId = t.Column<Guid>("uuid", nullable: false)
        }, constraints: t => t.PrimaryKey("PK_FinanceWithholdingRecords", x => x.Id));
        m.CreateIndex("IX_FinanceTaxRules_TenantId_TaxType_Jurisdiction_VendorCategory_EffectiveFrom", "FinanceTaxRules", ["TenantId", "TaxType", "Jurisdiction", "VendorCategory", "EffectiveFrom"]);
        m.CreateIndex("IX_FinanceWithholdingRecords_TenantId_PaymentReference", "FinanceWithholdingRecords", ["TenantId", "PaymentReference"], unique: true);
        m.CreateIndex("IX_FinanceWithholdingRecords_TenantId_FilingPeriod_TaxType", "FinanceWithholdingRecords", ["TenantId", "FilingPeriod", "TaxType"]);
        m.Sql("ALTER TABLE \"FinanceTaxRules\" ENABLE ROW LEVEL SECURITY; ALTER TABLE \"FinanceTaxRules\" FORCE ROW LEVEL SECURITY; CREATE POLICY finance_tax_rule_tenant_isolation ON \"FinanceTaxRules\" USING (\"TenantId\" = finance_tenant_id()) WITH CHECK (\"TenantId\" = finance_tenant_id()); ALTER TABLE \"FinanceWithholdingRecords\" ENABLE ROW LEVEL SECURITY; ALTER TABLE \"FinanceWithholdingRecords\" FORCE ROW LEVEL SECURITY; CREATE POLICY finance_withholding_tenant_isolation ON \"FinanceWithholdingRecords\" USING (\"TenantId\" = finance_tenant_id()) WITH CHECK (\"TenantId\" = finance_tenant_id());");
    }

    protected override void Down(MigrationBuilder m)
    {
        m.Sql("DROP POLICY IF EXISTS finance_withholding_tenant_isolation ON \"FinanceWithholdingRecords\"; DROP POLICY IF EXISTS finance_tax_rule_tenant_isolation ON \"FinanceTaxRules\";");
        m.DropTable("FinanceWithholdingRecords");
        m.DropTable("FinanceTaxRules");
    }
}
