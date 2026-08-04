using AegisCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260731008500_FinanceInsurance")]
public partial class FinanceInsurance : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.CreateTable("FinanceInsurancePolicies", t => new
        {
            Id = t.Column<Guid>("uuid", nullable: false), Insurer = t.Column<string>("character varying(200)", maxLength: 200, nullable: false), PolicyNumber = t.Column<string>("character varying(80)", maxLength: 80, nullable: false), PolicyType = t.Column<string>("character varying(40)", maxLength: 40, nullable: false), Currency = t.Column<string>("character varying(3)", maxLength: 3, nullable: false), CoverageAmount = t.Column<decimal>("numeric(19,4)", nullable: false), Premium = t.Column<decimal>("numeric(19,4)", nullable: false), StartDate = t.Column<DateTime>("timestamp with time zone", nullable: false), EndDate = t.Column<DateTime>("timestamp with time zone", nullable: false), RenewalDate = t.Column<DateTime>("timestamp with time zone", nullable: false), Status = t.Column<string>("character varying(20)", maxLength: 20, nullable: false), AmortizedThrough = t.Column<DateTime>("timestamp with time zone", nullable: true), TenantId = t.Column<Guid>("uuid", nullable: false)
        }, constraints: t => t.PrimaryKey("PK_FinanceInsurancePolicies", x => x.Id));
        m.CreateTable("FinanceInsuranceClaims", t => new
        {
            Id = t.Column<Guid>("uuid", nullable: false), FinanceInsurancePolicyId = t.Column<Guid>("uuid", nullable: false), ClaimNumber = t.Column<string>("character varying(80)", maxLength: 80, nullable: false), Description = t.Column<string>("character varying(1000)", maxLength: 1000, nullable: false), ClaimedAmount = t.Column<decimal>("numeric(19,4)", nullable: false), SettledAmount = t.Column<decimal>("numeric(19,4)", nullable: false), Status = t.Column<string>("character varying(20)", maxLength: 20, nullable: false), FiledAt = t.Column<DateTime>("timestamp with time zone", nullable: false), SettledAt = t.Column<DateTime>("timestamp with time zone", nullable: true), JournalEntryId = t.Column<Guid>("uuid", nullable: true), TenantId = t.Column<Guid>("uuid", nullable: false)
        }, constraints: t => { t.PrimaryKey("PK_FinanceInsuranceClaims", x => x.Id); t.ForeignKey("FinanceInsurancePolicies", x => x.FinanceInsurancePolicyId, principalTable: "FinanceInsurancePolicies", principalColumn: "Id", onDelete: ReferentialAction.Restrict); });
        m.CreateIndex("IX_FinanceInsurancePolicies_TenantId_PolicyNumber", "FinanceInsurancePolicies", ["TenantId", "PolicyNumber"], unique: true);
        m.CreateIndex("IX_FinanceInsurancePolicies_TenantId_RenewalDate_Status", "FinanceInsurancePolicies", ["TenantId", "RenewalDate", "Status"]);
        m.CreateIndex("IX_FinanceInsuranceClaims_TenantId_ClaimNumber", "FinanceInsuranceClaims", ["TenantId", "ClaimNumber"], unique: true);
        m.CreateIndex("IX_FinanceInsuranceClaims_TenantId_Status_FiledAt", "FinanceInsuranceClaims", ["TenantId", "Status", "FiledAt"]);
        m.Sql("ALTER TABLE \"FinanceInsurancePolicies\" ENABLE ROW LEVEL SECURITY; ALTER TABLE \"FinanceInsurancePolicies\" FORCE ROW LEVEL SECURITY; CREATE POLICY finance_insurance_policy_tenant_isolation ON \"FinanceInsurancePolicies\" USING (\"TenantId\" = finance_tenant_id()) WITH CHECK (\"TenantId\" = finance_tenant_id()); ALTER TABLE \"FinanceInsuranceClaims\" ENABLE ROW LEVEL SECURITY; ALTER TABLE \"FinanceInsuranceClaims\" FORCE ROW LEVEL SECURITY; CREATE POLICY finance_insurance_claim_tenant_isolation ON \"FinanceInsuranceClaims\" USING (\"TenantId\" = finance_tenant_id()) WITH CHECK (\"TenantId\" = finance_tenant_id());");
    }

    protected override void Down(MigrationBuilder m)
    {
        m.Sql("DROP POLICY IF EXISTS finance_insurance_claim_tenant_isolation ON \"FinanceInsuranceClaims\"; DROP POLICY IF EXISTS finance_insurance_policy_tenant_isolation ON \"FinanceInsurancePolicies\";");
        m.DropTable("FinanceInsuranceClaims");
        m.DropTable("FinanceInsurancePolicies");
    }
}
