using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FinanceVoucherOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApproverRole",
                table: "FinanceVoucherApprovals",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "FinanceVoucherApprovalRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    MinimumAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    MaximumAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: true),
                    RequiredRolesJson = table.Column<string>(type: "jsonb", nullable: false),
                    MinimumApprovals = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceVoucherApprovalRules", x => x.Id);
                    table.CheckConstraint("CK_FinanceVoucherApprovalRules_Range", "\"MinimumAmount\" >= 0 AND (\"MaximumAmount\" IS NULL OR \"MaximumAmount\" > \"MinimumAmount\") AND \"MinimumApprovals\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "FinanceVoucherAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FinanceVoucherId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceVoucherAttachments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceVoucherReversals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalVoucherId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReversalVoucherId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceVoucherReversals", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVoucherApprovals_TenantId_FinanceVoucherId_ApproverR~",
                table: "FinanceVoucherApprovals",
                columns: new[] { "TenantId", "FinanceVoucherId", "ApproverRole" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVoucherApprovalRules_TenantId_Currency_MinimumAmount",
                table: "FinanceVoucherApprovalRules",
                columns: new[] { "TenantId", "Currency", "MinimumAmount" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVoucherAttachments_TenantId_FinanceVoucherId",
                table: "FinanceVoucherAttachments",
                columns: new[] { "TenantId", "FinanceVoucherId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVoucherAttachments_TenantId_Sha256",
                table: "FinanceVoucherAttachments",
                columns: new[] { "TenantId", "Sha256" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVoucherReversals_TenantId_OriginalVoucherId",
                table: "FinanceVoucherReversals",
                columns: new[] { "TenantId", "OriginalVoucherId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVoucherReversals_TenantId_ReversalVoucherId",
                table: "FinanceVoucherReversals",
                columns: new[] { "TenantId", "ReversalVoucherId" },
                unique: true);

            migrationBuilder.AddForeignKey("FK_FinanceVoucherAttachments_FinanceVouchers_FinanceVoucherId", "FinanceVoucherAttachments", "FinanceVoucherId", "FinanceVouchers", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey("FK_FinanceVoucherReversals_FinanceVouchers_OriginalVoucherId", "FinanceVoucherReversals", "OriginalVoucherId", "FinanceVouchers", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey("FK_FinanceVoucherReversals_FinanceVouchers_ReversalVoucherId", "FinanceVoucherReversals", "ReversalVoucherId", "FinanceVouchers", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.Sql("""
                DO $$ DECLARE table_name text; BEGIN
                  FOREACH table_name IN ARRAY ARRAY['FinanceVoucherApprovalRules','FinanceVoucherAttachments','FinanceVoucherReversals'] LOOP
                    EXECUTE format('ALTER TABLE %I ENABLE ROW LEVEL SECURITY', table_name);
                    EXECUTE format('ALTER TABLE %I FORCE ROW LEVEL SECURITY', table_name);
                    EXECUTE format('CREATE POLICY %I ON %I USING ("TenantId" = finance_tenant_id()) WITH CHECK ("TenantId" = finance_tenant_id())', lower(table_name) || '_tenant_isolation', table_name);
                  END LOOP;
                END $$;
                CREATE TRIGGER finance_voucher_attachment_append_only BEFORE UPDATE OR DELETE ON "FinanceVoucherAttachments" FOR EACH ROW EXECUTE FUNCTION finance_append_only_guard();
                CREATE TRIGGER finance_voucher_reversal_append_only BEFORE UPDATE OR DELETE ON "FinanceVoucherReversals" FOR EACH ROW EXECUTE FUNCTION finance_append_only_guard();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS finance_voucher_reversal_append_only ON "FinanceVoucherReversals";
                DROP TRIGGER IF EXISTS finance_voucher_attachment_append_only ON "FinanceVoucherAttachments";
                """);
            migrationBuilder.DropTable(
                name: "FinanceVoucherApprovalRules");

            migrationBuilder.DropTable(
                name: "FinanceVoucherAttachments");

            migrationBuilder.DropTable(
                name: "FinanceVoucherReversals");

            migrationBuilder.DropIndex(
                name: "IX_FinanceVoucherApprovals_TenantId_FinanceVoucherId_ApproverR~",
                table: "FinanceVoucherApprovals");

            migrationBuilder.DropColumn(
                name: "ApproverRole",
                table: "FinanceVoucherApprovals");
        }
    }
}
