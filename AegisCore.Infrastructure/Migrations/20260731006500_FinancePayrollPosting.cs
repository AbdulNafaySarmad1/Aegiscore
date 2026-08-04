using AegisCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260731006500_FinancePayrollPosting")]
public partial class FinancePayrollPosting : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.CreateTable("FinancePayrollPostings", t => new
        {
            Id = t.Column<Guid>("uuid", nullable: false), Period = t.Column<string>("character varying(7)", maxLength: 7, nullable: false), Status = t.Column<string>("character varying(20)", maxLength: 20, nullable: false),
            GrossSalary = t.Column<decimal>("numeric(19,4)", nullable: false), NetSalary = t.Column<decimal>("numeric(19,4)", nullable: false), IncomeTax = t.Column<decimal>("numeric(19,4)", nullable: false), Eobi = t.Column<decimal>("numeric(19,4)", nullable: false), SocialSecurity = t.Column<decimal>("numeric(19,4)", nullable: false), Gratuity = t.Column<decimal>("numeric(19,4)", nullable: false), JournalEntryId = t.Column<Guid>("uuid", nullable: true), ImportedByUserId = t.Column<Guid>("uuid", nullable: false), ImportedAt = t.Column<DateTime>("timestamp with time zone", nullable: false), TenantId = t.Column<Guid>("uuid", nullable: false)
        }, constraints: t => t.PrimaryKey("PK_FinancePayrollPostings", x => x.Id));
        m.CreateTable("FinancePayrollPostingLines", t => new
        {
            Id = t.Column<Guid>("uuid", nullable: false), FinancePayrollPostingId = t.Column<Guid>("uuid", nullable: false), EmployeeId = t.Column<Guid>("uuid", nullable: false), CostCenterId = t.Column<Guid>("uuid", nullable: false),
            GrossSalary = t.Column<decimal>("numeric(19,4)", nullable: false), IncomeTax = t.Column<decimal>("numeric(19,4)", nullable: false), Eobi = t.Column<decimal>("numeric(19,4)", nullable: false), SocialSecurity = t.Column<decimal>("numeric(19,4)", nullable: false), Gratuity = t.Column<decimal>("numeric(19,4)", nullable: false), NetSalary = t.Column<decimal>("numeric(19,4)", nullable: false), TenantId = t.Column<Guid>("uuid", nullable: false)
        }, constraints: t => { t.PrimaryKey("PK_FinancePayrollPostingLines", x => x.Id); t.ForeignKey("FinancePayrollPostings", x => x.FinancePayrollPostingId, principalTable: "FinancePayrollPostings", principalColumn: "Id", onDelete: ReferentialAction.Restrict); });
        m.CreateIndex("IX_FinancePayrollPostings_TenantId_Period", "FinancePayrollPostings", ["TenantId", "Period"], unique: true);
        m.CreateIndex("IX_FinancePayrollPostingLines_TenantId_FinancePayrollPostingId_EmployeeId", "FinancePayrollPostingLines", ["TenantId", "FinancePayrollPostingId", "EmployeeId"], unique: true);
        m.CreateIndex("IX_FinancePayrollPostingLines_TenantId_CostCenterId", "FinancePayrollPostingLines", ["TenantId", "CostCenterId"]);
        m.Sql("ALTER TABLE \"FinancePayrollPostings\" ENABLE ROW LEVEL SECURITY; ALTER TABLE \"FinancePayrollPostings\" FORCE ROW LEVEL SECURITY; CREATE POLICY finance_payroll_posting_tenant_isolation ON \"FinancePayrollPostings\" USING (\"TenantId\" = finance_tenant_id()) WITH CHECK (\"TenantId\" = finance_tenant_id()); ALTER TABLE \"FinancePayrollPostingLines\" ENABLE ROW LEVEL SECURITY; ALTER TABLE \"FinancePayrollPostingLines\" FORCE ROW LEVEL SECURITY; CREATE POLICY finance_payroll_posting_line_tenant_isolation ON \"FinancePayrollPostingLines\" USING (\"TenantId\" = finance_tenant_id()) WITH CHECK (\"TenantId\" = finance_tenant_id());");
    }

    protected override void Down(MigrationBuilder m)
    {
        m.Sql("DROP POLICY IF EXISTS finance_payroll_posting_line_tenant_isolation ON \"FinancePayrollPostingLines\"; DROP POLICY IF EXISTS finance_payroll_posting_tenant_isolation ON \"FinancePayrollPostings\";");
        m.DropTable("FinancePayrollPostingLines");
        m.DropTable("FinancePayrollPostings");
    }
}
