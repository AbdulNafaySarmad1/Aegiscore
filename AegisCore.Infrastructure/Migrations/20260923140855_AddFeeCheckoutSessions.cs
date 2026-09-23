using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFeeCheckoutSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FeeCheckoutSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FeeDemandId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    GatewaySlug = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    BasketId = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PreviousDemandStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsSandbox = table.Column<bool>(type: "boolean", nullable: false),
                    GatewayTransactionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    GatewayErrorCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    GatewayMessage = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FinancePaymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeeCheckoutSessions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FeeCheckoutSessions_Status_ExpiresAt",
                table: "FeeCheckoutSessions",
                columns: new[] { "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FeeCheckoutSessions_TenantId_FeeDemandId_Status",
                table: "FeeCheckoutSessions",
                columns: new[] { "TenantId", "FeeDemandId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FeeCheckoutSessions_TenantId_GatewaySlug_BasketId",
                table: "FeeCheckoutSessions",
                columns: new[] { "TenantId", "GatewaySlug", "BasketId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FeeCheckoutSessions");
        }
    }
}
