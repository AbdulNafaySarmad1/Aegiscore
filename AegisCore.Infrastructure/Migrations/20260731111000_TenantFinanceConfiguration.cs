using AegisCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

// P11 multi-dialect note: application ITenantScoped global filters enforce portable tenant isolation; this applied PostgreSQL migration retains historical RLS for defense in depth.

[DbContext(typeof(AegisDbContext))]
[Migration("20260731111000_TenantFinanceConfiguration")]
public partial class TenantFinanceConfiguration : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.CreateTable("TenantFinanceConfigurations", t => new
        {
            Id = t.Column<Guid>("uuid", nullable: false),
            PrimaryStandard = t.Column<string>("character varying(20)", maxLength: 20, nullable: false),
            FiscalYearStartMonth = t.Column<int>("integer", nullable: false),
            FiscalYearLabel = t.Column<string>("character varying(30)", maxLength: 30, nullable: false),
            BaseCurrency = t.Column<string>("character varying(3)", maxLength: 3, nullable: false),
            AdditionalCurrenciesJson = t.Column<string>("jsonb", nullable: false),
            RateSource = t.Column<string>("character varying(30)", maxLength: 30, nullable: false),
            RateApiKeySecretName = t.Column<string>("character varying(300)", maxLength: 300, nullable: true),
            TaxSettingsJson = t.Column<string>("jsonb", nullable: false),
            UpdatedAt = t.Column<DateTime>("timestamp with time zone", nullable: false),
            TenantId = t.Column<Guid>("uuid", nullable: false)
        }, constraints: t => t.PrimaryKey("PK_TenantFinanceConfigurations", x => x.Id));
        m.CreateIndex("IX_TenantFinanceConfigurations_TenantId", "TenantFinanceConfigurations", "TenantId", unique: true);
        m.Sql("""
            ALTER TABLE "TenantFinanceConfigurations" ENABLE ROW LEVEL SECURITY;
            ALTER TABLE "TenantFinanceConfigurations" FORCE ROW LEVEL SECURITY;
            CREATE POLICY tenant_finance_configuration_tenant_isolation ON "TenantFinanceConfigurations"
                USING ("TenantId" = finance_tenant_id()) WITH CHECK ("TenantId" = finance_tenant_id());
            """);
    }

    protected override void Down(MigrationBuilder m)
    {
        m.Sql("DROP POLICY IF EXISTS tenant_finance_configuration_tenant_isolation ON \"TenantFinanceConfigurations\";");
        m.DropTable("TenantFinanceConfigurations");
    }
}
