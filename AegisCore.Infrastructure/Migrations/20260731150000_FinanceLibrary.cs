using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260731150000_FinanceLibrary")]
public partial class FinanceLibrary : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.CreateTable("FinanceLibraryAcquisitions", t => new
        {
            Id = t.Column<Guid>("uuid", nullable: false), LibraryBookId = t.Column<Guid>("uuid", nullable: true), ResourceType = t.Column<string>("character varying(20)", maxLength: 20, nullable: false), Title = t.Column<string>("character varying(300)", maxLength: 300, nullable: false), Currency = t.Column<string>("character varying(3)", maxLength: 3, nullable: false), Cost = t.Column<decimal>("numeric(19,4)", nullable: false), FairValue = t.Column<decimal>("numeric(19,4)", nullable: false), AccumulatedAmortization = t.Column<decimal>("numeric(19,4)", nullable: false), UsefulLifeMonths = t.Column<int>("integer", nullable: false), AcquiredAt = t.Column<DateTime>("timestamp with time zone", nullable: false), SubscriptionEndsAt = t.Column<DateTime>("timestamp with time zone", nullable: true), AccountingTreatment = t.Column<string>("character varying(20)", maxLength: 20, nullable: false), Status = t.Column<string>("character varying(20)", maxLength: 20, nullable: false), JournalEntryId = t.Column<Guid>("uuid", nullable: true), TenantId = t.Column<Guid>("uuid", nullable: false)
        }, constraints: t => { t.PrimaryKey("PK_FinanceLibraryAcquisitions", x => x.Id); t.CheckConstraint("CK_FinanceLibraryAcquisitions_NonNegativeAmounts", "\"Cost\" >= 0 AND \"FairValue\" >= 0 AND \"AccumulatedAmortization\" >= 0"); });
        m.CreateTable("FinanceLibraryFines", t => new
        {
            Id = t.Column<Guid>("uuid", nullable: false), StudentId = t.Column<Guid>("uuid", nullable: false), LibraryBookId = t.Column<Guid>("uuid", nullable: true), FineType = t.Column<string>("character varying(30)", maxLength: 30, nullable: false), Amount = t.Column<decimal>("numeric(19,4)", nullable: false), Currency = t.Column<string>("character varying(3)", maxLength: 3, nullable: false), Status = t.Column<string>("character varying(20)", maxLength: 20, nullable: false), Description = t.Column<string>("character varying(500)", maxLength: 500, nullable: false), AssessedAt = t.Column<DateTime>("timestamp with time zone", nullable: false), PaidAt = t.Column<DateTime>("timestamp with time zone", nullable: true), JournalEntryId = t.Column<Guid>("uuid", nullable: true), TenantId = t.Column<Guid>("uuid", nullable: false)
        }, constraints: t => { t.PrimaryKey("PK_FinanceLibraryFines", x => x.Id); t.CheckConstraint("CK_FinanceLibraryFines_PositiveAmount", "\"Amount\" > 0"); });
        m.CreateIndex("IX_FinanceLibraryAcquisitions_TenantId_Status_AcquiredAt", "FinanceLibraryAcquisitions", ["TenantId", "Status", "AcquiredAt"]);
        m.CreateIndex("IX_FinanceLibraryFines_TenantId_StudentId_Status", "FinanceLibraryFines", ["TenantId", "StudentId", "Status"]);
        m.Sql("ALTER TABLE \"FinanceLibraryAcquisitions\" ENABLE ROW LEVEL SECURITY; ALTER TABLE \"FinanceLibraryAcquisitions\" FORCE ROW LEVEL SECURITY; CREATE POLICY finance_library_acquisition_tenant_isolation ON \"FinanceLibraryAcquisitions\" USING (\"TenantId\" = finance_tenant_id()) WITH CHECK (\"TenantId\" = finance_tenant_id()); ALTER TABLE \"FinanceLibraryFines\" ENABLE ROW LEVEL SECURITY; ALTER TABLE \"FinanceLibraryFines\" FORCE ROW LEVEL SECURITY; CREATE POLICY finance_library_fine_tenant_isolation ON \"FinanceLibraryFines\" USING (\"TenantId\" = finance_tenant_id()) WITH CHECK (\"TenantId\" = finance_tenant_id());");
    }
    protected override void Down(MigrationBuilder m)
    {
        m.Sql("DROP POLICY IF EXISTS finance_library_fine_tenant_isolation ON \"FinanceLibraryFines\"; DROP POLICY IF EXISTS finance_library_acquisition_tenant_isolation ON \"FinanceLibraryAcquisitions\";");
        m.DropTable("FinanceLibraryFines"); m.DropTable("FinanceLibraryAcquisitions");
    }
}
