using AegisCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

// P11 multi-dialect note: application ITenantScoped global filters enforce portable tenant isolation; this applied PostgreSQL migration retains historical RLS for defense in depth.

[DbContext(typeof(AegisDbContext))]
[Migration("20260731005500_FinanceReportProfiles")]
public partial class FinanceReportProfiles : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.CreateTable("FinanceReportProfiles", t => new
        {
            Id = t.Column<Guid>("uuid", nullable: false), Name = t.Column<string>("character varying(120)", maxLength: 120, nullable: false), TargetStandard = t.Column<string>("character varying(20)", maxLength: 20, nullable: false), FunderName = t.Column<string>("character varying(160)", maxLength: 160, nullable: true),
            FundFilter = t.Column<string>("character varying(20)", maxLength: 20, nullable: false), PeriodType = t.Column<string>("character varying(20)", maxLength: 20, nullable: false), ReportCurrency = t.Column<string>("character varying(3)", maxLength: 3, nullable: false), RateMethod = t.Column<string>("character varying(20)", maxLength: 20, nullable: false), Schedule = t.Column<string>("character varying(20)", maxLength: 20, nullable: false), Delivery = t.Column<string>("character varying(20)", maxLength: 20, nullable: false), Format = t.Column<string>("character varying(10)", maxLength: 10, nullable: false),
            ComparativeYears = t.Column<int>("integer", nullable: false), BudgetVsActual = t.Column<bool>("boolean", nullable: false), RestrictedFundUtilisation = t.Column<bool>("boolean", nullable: false), GrantDrawdown = t.Column<bool>("boolean", nullable: false), PqcEnabled = t.Column<bool>("boolean", nullable: false), IsActive = t.Column<bool>("boolean", nullable: false), CreatedAt = t.Column<DateTime>("timestamp with time zone", nullable: false), CreatedByUserId = t.Column<Guid>("uuid", nullable: false), TenantId = t.Column<Guid>("uuid", nullable: false)
        }, constraints: t => t.PrimaryKey("PK_FinanceReportProfiles", x => x.Id));
        m.CreateIndex("IX_FinanceReportProfiles_TenantId_IsActive_Schedule", "FinanceReportProfiles", ["TenantId", "IsActive", "Schedule"]);
        m.Sql("""
            ALTER TABLE "FinanceReportProfiles" ENABLE ROW LEVEL SECURITY; ALTER TABLE "FinanceReportProfiles" FORCE ROW LEVEL SECURITY;
            CREATE POLICY finance_report_profile_tenant_isolation ON "FinanceReportProfiles" USING ("TenantId" = finance_tenant_id()) WITH CHECK ("TenantId" = finance_tenant_id());
            """);
    }

    protected override void Down(MigrationBuilder m)
    {
        m.Sql("DROP POLICY IF EXISTS finance_report_profile_tenant_isolation ON \"FinanceReportProfiles\";");
        m.DropTable("FinanceReportProfiles");
    }
}
