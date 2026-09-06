using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using AegisCore.Infrastructure;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

// P11 multi-dialect note: application ITenantScoped global filters enforce portable tenant isolation; this applied PostgreSQL migration retains historical RLS for defense in depth.

[DbContext(typeof(AegisDbContext))]
[Migration("20260731000000_FinanceWebhookIdempotency")]
public partial class FinanceWebhookIdempotency : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS \"UX_FinancePayments_TenantId_WebhookId\" ON \"FinancePayments\" (\"TenantId\", \"WebhookId\") WHERE \"WebhookId\" IS NOT NULL;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX IF EXISTS \"UX_FinancePayments_TenantId_WebhookId\";");
    }
}
