using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

// P11 multi-dialect note: application ITenantScoped global filters enforce portable tenant isolation; this applied PostgreSQL migration retains historical RLS for defense in depth.

namespace AegisCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FinanceAncillaryLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "LibraryCapitalizationThreshold",
                table: "TenantFinanceConfigurations",
                type: "numeric(19,4)",
                nullable: false,
                defaultValue: 5000m);

            migrationBuilder.CreateTable(
                name: "FinanceFleetProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransportVehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    CostCenterId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustodianUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Make = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ModelYear = table.Column<int>(type: "integer", nullable: false),
                    FuelType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ServiceIntervalDays = table.Column<int>(type: "integer", nullable: false),
                    ServiceIntervalKm = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    CurrentOdometer = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    LastServiceAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextServiceAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceFleetProfiles", x => x.Id);
                    table.CheckConstraint("CK_FinanceFleetProfiles_Service", "\"ModelYear\" >= 1900 AND \"ServiceIntervalDays\" > 0 AND \"ServiceIntervalKm\" > 0 AND \"CurrentOdometer\" >= 0");
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_TenantFinanceConfigurations_LibraryThreshold",
                table: "TenantFinanceConfigurations",
                sql: "\"LibraryCapitalizationThreshold\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceFleetProfiles_TenantId_NextServiceAt_Status",
                table: "FinanceFleetProfiles",
                columns: new[] { "TenantId", "NextServiceAt", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceFleetProfiles_TenantId_TransportVehicleId",
                table: "FinanceFleetProfiles",
                columns: new[] { "TenantId", "TransportVehicleId" },
                unique: true);
            migrationBuilder.Sql("""
                ALTER TABLE "FinanceFleetProfiles" ADD CONSTRAINT "FK_FinanceFleetProfiles_TransportVehicles" FOREIGN KEY ("TransportVehicleId") REFERENCES "TransportVehicles" ("Id") ON DELETE RESTRICT;
                ALTER TABLE "FinanceFleetProfiles" ADD CONSTRAINT "FK_FinanceFleetProfiles_Assets" FOREIGN KEY ("AssetId") REFERENCES "Assets" ("Id") ON DELETE RESTRICT;
                ALTER TABLE "FinanceFleetProfiles" ADD CONSTRAINT "FK_FinanceFleetProfiles_CostCenters" FOREIGN KEY ("CostCenterId") REFERENCES "CostCenters" ("Id") ON DELETE RESTRICT;
                ALTER TABLE "FinanceFleetProfiles" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "FinanceFleetProfiles" FORCE ROW LEVEL SECURITY;
                CREATE POLICY finance_fleet_profile_tenant_isolation ON "FinanceFleetProfiles" USING ("TenantId" = finance_tenant_id()) WITH CHECK ("TenantId" = finance_tenant_id());
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP POLICY IF EXISTS finance_fleet_profile_tenant_isolation ON \"FinanceFleetProfiles\";");
            migrationBuilder.DropTable(
                name: "FinanceFleetProfiles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TenantFinanceConfigurations_LibraryThreshold",
                table: "TenantFinanceConfigurations");

            migrationBuilder.DropColumn(
                name: "LibraryCapitalizationThreshold",
                table: "TenantFinanceConfigurations");
        }
    }
}
