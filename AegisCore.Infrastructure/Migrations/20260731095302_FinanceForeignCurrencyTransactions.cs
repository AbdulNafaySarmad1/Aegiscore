using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FinanceForeignCurrencyTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinanceForeignCurrencyTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ForeignAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    ForeignCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    FunctionalAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    FunctionalCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    RateUsed = table.Column<decimal>(type: "numeric(24,12)", nullable: false),
                    RateSource = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RateDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsSettled = table.Column<bool>(type: "boolean", nullable: false),
                    SettledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RealizedFxGainLoss = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceForeignCurrencyTransactions", x => x.Id);
                    table.CheckConstraint("CK_FinanceForeignCurrencyTransactions_CurrenciesDiffer", "\"ForeignCurrency\" <> \"FunctionalCurrency\"");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceForeignCurrencyTransactions_TenantId_ForeignCurrency~",
                table: "FinanceForeignCurrencyTransactions",
                columns: new[] { "TenantId", "ForeignCurrency", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceForeignCurrencyTransactions_TenantId_SourceType_Sour~",
                table: "FinanceForeignCurrencyTransactions",
                columns: new[] { "TenantId", "SourceType", "SourceId" });

            migrationBuilder.Sql("""
                ALTER TABLE "FinanceForeignCurrencyTransactions" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "FinanceForeignCurrencyTransactions" FORCE ROW LEVEL SECURITY;
                CREATE POLICY finance_fcy_transaction_tenant_isolation ON "FinanceForeignCurrencyTransactions"
                  USING ("TenantId" = finance_tenant_id()) WITH CHECK ("TenantId" = finance_tenant_id());
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP POLICY IF EXISTS finance_fcy_transaction_tenant_isolation ON \"FinanceForeignCurrencyTransactions\";");
            migrationBuilder.DropTable(
                name: "FinanceForeignCurrencyTransactions");
        }
    }
}
