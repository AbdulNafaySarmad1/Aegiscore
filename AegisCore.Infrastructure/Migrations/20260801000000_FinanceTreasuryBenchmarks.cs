using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

// P11 multi-dialect note: application ITenantScoped global filters enforce portable tenant isolation; this applied PostgreSQL migration retains historical RLS for defense in depth.

namespace AegisCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FinanceTreasuryBenchmarks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinanceInvestmentBenchmarks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ObservationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AnnualReturnPercent = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    Source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceInvestmentBenchmarks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceInvestmentBenchmarks_TenantId_Name_ObservationDate",
                table: "FinanceInvestmentBenchmarks",
                columns: new[] { "TenantId", "Name", "ObservationDate" },
                unique: true);
            migrationBuilder.Sql("ALTER TABLE \"FinanceInvestmentBenchmarks\" ENABLE ROW LEVEL SECURITY; ALTER TABLE \"FinanceInvestmentBenchmarks\" FORCE ROW LEVEL SECURITY; CREATE POLICY finance_investment_benchmark_tenant_isolation ON \"FinanceInvestmentBenchmarks\" USING (\"TenantId\" = finance_tenant_id()) WITH CHECK (\"TenantId\" = finance_tenant_id());");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP POLICY IF EXISTS finance_investment_benchmark_tenant_isolation ON \"FinanceInvestmentBenchmarks\";");
            migrationBuilder.DropTable(
                name: "FinanceInvestmentBenchmarks");
        }
    }
}
