using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using AegisCore.Infrastructure;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

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
