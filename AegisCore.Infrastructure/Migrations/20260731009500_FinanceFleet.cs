using AegisCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

// P11 multi-dialect note: application ITenantScoped global filters enforce portable tenant isolation; this applied PostgreSQL migration retains historical RLS for defense in depth.

[DbContext(typeof(AegisDbContext))]
[Migration("20260731009500_FinanceFleet")]
public partial class FinanceFleet : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.CreateTable("FinanceFleetCostPostings", t => new
        {
            Id = t.Column<Guid>("uuid", nullable: false), TransportVehicleId = t.Column<Guid>("uuid", nullable: false), CostCenterId = t.Column<Guid>("uuid", nullable: false), CostType = t.Column<string>("character varying(20)", maxLength: 20, nullable: false), Amount = t.Column<decimal>("numeric(19,4)", nullable: false), Litres = t.Column<decimal>("numeric(19,4)", nullable: false), Odometer = t.Column<decimal>("numeric(19,4)", nullable: false), Description = t.Column<string>("character varying(500)", maxLength: 500, nullable: false), CostDate = t.Column<DateTime>("timestamp with time zone", nullable: false), JournalEntryId = t.Column<Guid>("uuid", nullable: false), TenantId = t.Column<Guid>("uuid", nullable: false)
        }, constraints: t => t.PrimaryKey("PK_FinanceFleetCostPostings", x => x.Id));
        m.CreateTable("FinanceFleetTrips", t => new
        {
            Id = t.Column<Guid>("uuid", nullable: false), TransportVehicleId = t.Column<Guid>("uuid", nullable: false), CostCenterId = t.Column<Guid>("uuid", nullable: true), DistanceKm = t.Column<decimal>("numeric(19,4)", nullable: false), Purpose = t.Column<string>("character varying(500)", maxLength: 500, nullable: false), FromLocation = t.Column<string>("character varying(160)", maxLength: 160, nullable: true), ToLocation = t.Column<string>("character varying(160)", maxLength: 160, nullable: true), TripDate = t.Column<DateTime>("timestamp with time zone", nullable: false), TenantId = t.Column<Guid>("uuid", nullable: false)
        }, constraints: t => t.PrimaryKey("PK_FinanceFleetTrips", x => x.Id));
        m.CreateIndex("IX_FinanceFleetCostPostings_TenantId_TransportVehicleId_CostDate", "FinanceFleetCostPostings", ["TenantId", "TransportVehicleId", "CostDate"]);
        m.CreateIndex("IX_FinanceFleetCostPostings_JournalEntryId", "FinanceFleetCostPostings", ["JournalEntryId"], unique: true);
        m.CreateIndex("IX_FinanceFleetTrips_TenantId_TransportVehicleId_TripDate", "FinanceFleetTrips", ["TenantId", "TransportVehicleId", "TripDate"]);
        m.Sql("ALTER TABLE \"FinanceFleetCostPostings\" ENABLE ROW LEVEL SECURITY; ALTER TABLE \"FinanceFleetCostPostings\" FORCE ROW LEVEL SECURITY; CREATE POLICY finance_fleet_cost_tenant_isolation ON \"FinanceFleetCostPostings\" USING (\"TenantId\" = finance_tenant_id()) WITH CHECK (\"TenantId\" = finance_tenant_id()); ALTER TABLE \"FinanceFleetTrips\" ENABLE ROW LEVEL SECURITY; ALTER TABLE \"FinanceFleetTrips\" FORCE ROW LEVEL SECURITY; CREATE POLICY finance_fleet_trip_tenant_isolation ON \"FinanceFleetTrips\" USING (\"TenantId\" = finance_tenant_id()) WITH CHECK (\"TenantId\" = finance_tenant_id());");
    }

    protected override void Down(MigrationBuilder m)
    {
        m.Sql("DROP POLICY IF EXISTS finance_fleet_trip_tenant_isolation ON \"FinanceFleetTrips\"; DROP POLICY IF EXISTS finance_fleet_cost_tenant_isolation ON \"FinanceFleetCostPostings\";");
        m.DropTable("FinanceFleetTrips");
        m.DropTable("FinanceFleetCostPostings");
    }
}
