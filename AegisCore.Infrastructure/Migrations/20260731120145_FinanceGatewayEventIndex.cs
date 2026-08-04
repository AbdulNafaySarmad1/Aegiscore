using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FinanceGatewayEventIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PaymentGatewayEvents_TenantId_GatewaySlug_CreatedAt",
                table: "PaymentGatewayEvents",
                columns: new[] { "TenantId", "GatewaySlug", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PaymentGatewayEvents_TenantId_GatewaySlug_CreatedAt",
                table: "PaymentGatewayEvents");
        }
    }
}
