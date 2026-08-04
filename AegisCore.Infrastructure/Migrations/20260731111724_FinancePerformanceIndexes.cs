using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FinancePerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_TenantId_IsPosted_IsSandbox_EntryDate",
                table: "JournalEntries",
                columns: new[] { "TenantId", "IsPosted", "IsSandbox", "EntryDate" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVoucherLines_TenantId_ChartOfAccountId",
                table: "FinanceVoucherLines",
                columns: new[] { "TenantId", "ChartOfAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancePayments_TenantId_WebhookId",
                table: "FinancePayments",
                columns: new[] { "TenantId", "WebhookId" },
                unique: true,
                filter: "\"WebhookId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FeeDemands_TenantId_Status_DueDate",
                table: "FeeDemands",
                columns: new[] { "TenantId", "Status", "DueDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_JournalEntries_TenantId_IsPosted_IsSandbox_EntryDate",
                table: "JournalEntries");

            migrationBuilder.DropIndex(
                name: "IX_FinanceVoucherLines_TenantId_ChartOfAccountId",
                table: "FinanceVoucherLines");

            migrationBuilder.DropIndex(
                name: "IX_FinancePayments_TenantId_WebhookId",
                table: "FinancePayments");

            migrationBuilder.DropIndex(
                name: "IX_FeeDemands_TenantId_Status_DueDate",
                table: "FeeDemands");
        }
    }
}
