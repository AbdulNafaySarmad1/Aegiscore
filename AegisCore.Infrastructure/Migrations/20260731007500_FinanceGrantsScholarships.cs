using AegisCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260731007500_FinanceGrantsScholarships")]
public partial class FinanceGrantsScholarships : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.CreateTable("FinanceGrants", t => new
        {
            Id = t.Column<Guid>("uuid", nullable: false), DonorName = t.Column<string>("character varying(200)", maxLength: 200, nullable: false), Currency = t.Column<string>("character varying(3)", maxLength: 3, nullable: false), AwardAmount = t.Column<decimal>("numeric(19,4)", nullable: false), ReceivedAmount = t.Column<decimal>("numeric(19,4)", nullable: false), SpentAmount = t.Column<decimal>("numeric(19,4)", nullable: false), Purpose = t.Column<string>("character varying(500)", maxLength: 500, nullable: false), StartDate = t.Column<DateTime>("timestamp with time zone", nullable: false), EndDate = t.Column<DateTime>("timestamp with time zone", nullable: false), Status = t.Column<string>("character varying(20)", maxLength: 20, nullable: false), TenantId = t.Column<Guid>("uuid", nullable: false)
        }, constraints: t => t.PrimaryKey("PK_FinanceGrants", x => x.Id));
        m.CreateTable("FinanceScholarshipAwards", t => new
        {
            Id = t.Column<Guid>("uuid", nullable: false), StudentId = t.Column<Guid>("uuid", nullable: false), GrantId = t.Column<Guid>("uuid", nullable: true), ScholarshipType = t.Column<string>("character varying(40)", maxLength: 40, nullable: false), CoverageType = t.Column<string>("character varying(20)", maxLength: 20, nullable: false), Amount = t.Column<decimal>("numeric(19,4)", nullable: false), DisbursementMethod = t.Column<string>("character varying(20)", maxLength: 20, nullable: false), Status = t.Column<string>("character varying(20)", maxLength: 20, nullable: false), AwardedAt = t.Column<DateTime>("timestamp with time zone", nullable: false), JournalEntryId = t.Column<Guid>("uuid", nullable: true), TenantId = t.Column<Guid>("uuid", nullable: false)
        }, constraints: t => { t.PrimaryKey("PK_FinanceScholarshipAwards", x => x.Id); t.ForeignKey("FinanceGrants", x => x.GrantId, principalTable: "FinanceGrants", principalColumn: "Id", onDelete: ReferentialAction.Restrict); });
        m.CreateIndex("IX_FinanceGrants_TenantId_DonorName_Purpose", "FinanceGrants", ["TenantId", "DonorName", "Purpose"]);
        m.CreateIndex("IX_FinanceScholarshipAwards_TenantId_StudentId_AwardedAt", "FinanceScholarshipAwards", ["TenantId", "StudentId", "AwardedAt"]);
        m.CreateIndex("IX_FinanceScholarshipAwards_TenantId_GrantId", "FinanceScholarshipAwards", ["TenantId", "GrantId"]);
        m.Sql("ALTER TABLE \"FinanceGrants\" ENABLE ROW LEVEL SECURITY; ALTER TABLE \"FinanceGrants\" FORCE ROW LEVEL SECURITY; CREATE POLICY finance_grant_tenant_isolation ON \"FinanceGrants\" USING (\"TenantId\" = finance_tenant_id()) WITH CHECK (\"TenantId\" = finance_tenant_id()); ALTER TABLE \"FinanceScholarshipAwards\" ENABLE ROW LEVEL SECURITY; ALTER TABLE \"FinanceScholarshipAwards\" FORCE ROW LEVEL SECURITY; CREATE POLICY finance_award_tenant_isolation ON \"FinanceScholarshipAwards\" USING (\"TenantId\" = finance_tenant_id()) WITH CHECK (\"TenantId\" = finance_tenant_id());");
    }

    protected override void Down(MigrationBuilder m)
    {
        m.Sql("DROP POLICY IF EXISTS finance_award_tenant_isolation ON \"FinanceScholarshipAwards\"; DROP POLICY IF EXISTS finance_grant_tenant_isolation ON \"FinanceGrants\";");
        m.DropTable("FinanceScholarshipAwards");
        m.DropTable("FinanceGrants");
    }
}
