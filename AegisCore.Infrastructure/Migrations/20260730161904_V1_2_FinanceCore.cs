using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V1_2_FinanceCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExchangeRateSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BaseCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    QuoteCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Rate = table.Column<decimal>(type: "numeric(24,12)", nullable: false),
                    Source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ValidForDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PulledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeRateSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FeeDemands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DemandNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    OutstandingAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeeDemands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceVouchers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VoucherNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    VoucherType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TotalAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    PostedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PostedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectionNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PqcAlgorithm = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    PqcKeyId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PqcSignature = table.Column<byte[]>(type: "bytea", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceVouchers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceVoucherSequences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VoucherType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FiscalYear = table.Column<int>(type: "integer", nullable: false),
                    NextNumber = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceVoucherSequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentGatewayConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GatewaySlug = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Environment = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    SecretNamespace = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    HealthStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LastHealthCheckAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    HealthError = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ConfiguredByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfiguredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentGatewayConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentGatewayEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GatewaySlug = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    EventType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ReferenceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FinancePaymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(19,4)", nullable: true),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsSandbox = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SanitizedPayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentGatewayEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FeeInstallments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FeeDemandId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeeInstallments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeeInstallments_FeeDemands_FeeDemandId",
                        column: x => x.FeeDemandId,
                        principalTable: "FeeDemands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinancePayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FeeDemandId = table.Column<Guid>(type: "uuid", nullable: false),
                    FeeInstallmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Method = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    GatewaySlug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExternalReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    WebhookId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SettledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancePayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancePayments_FeeDemands_FeeDemandId",
                        column: x => x.FeeDemandId,
                        principalTable: "FeeDemands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FinanceVoucherApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FinanceVoucherId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApproverUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Decision = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PqcAlgorithm = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    PqcKeyId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PqcSignature = table.Column<byte[]>(type: "bytea", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceVoucherApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceVoucherApprovals_FinanceVouchers_FinanceVoucherId",
                        column: x => x.FinanceVoucherId,
                        principalTable: "FinanceVouchers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceVoucherLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FinanceVoucherId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChartOfAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    CostCenterId = table.Column<Guid>(type: "uuid", nullable: true),
                    Debit = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Credit = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceVoucherLines", x => x.Id);
                    table.CheckConstraint("CK_FinanceVoucherLines_OneSided", "(\"Debit\" > 0 AND \"Credit\" = 0) OR (\"Credit\" > 0 AND \"Debit\" = 0)");
                    table.ForeignKey(
                        name: "FK_FinanceVoucherLines_FinanceVouchers_FinanceVoucherId",
                        column: x => x.FinanceVoucherId,
                        principalTable: "FinanceVouchers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRateSnapshots_TenantId_BaseCurrency_QuoteCurrency_V~",
                table: "ExchangeRateSnapshots",
                columns: new[] { "TenantId", "BaseCurrency", "QuoteCurrency", "ValidForDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeeDemands_TenantId_DemandNumber",
                table: "FeeDemands",
                columns: new[] { "TenantId", "DemandNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeeDemands_TenantId_StudentId_Status",
                table: "FeeDemands",
                columns: new[] { "TenantId", "StudentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FeeInstallments_FeeDemandId",
                table: "FeeInstallments",
                column: "FeeDemandId");

            migrationBuilder.CreateIndex(
                name: "IX_FeeInstallments_TenantId_FeeDemandId_Sequence",
                table: "FeeInstallments",
                columns: new[] { "TenantId", "FeeDemandId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancePayments_FeeDemandId",
                table: "FinancePayments",
                column: "FeeDemandId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancePayments_TenantId_FeeDemandId_Status",
                table: "FinancePayments",
                columns: new[] { "TenantId", "FeeDemandId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancePayments_TenantId_IdempotencyKey",
                table: "FinancePayments",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVoucherApprovals_FinanceVoucherId",
                table: "FinanceVoucherApprovals",
                column: "FinanceVoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVoucherApprovals_TenantId_FinanceVoucherId_ApproverU~",
                table: "FinanceVoucherApprovals",
                columns: new[] { "TenantId", "FinanceVoucherId", "ApproverUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVoucherLines_FinanceVoucherId",
                table: "FinanceVoucherLines",
                column: "FinanceVoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVoucherLines_TenantId_FinanceVoucherId",
                table: "FinanceVoucherLines",
                columns: new[] { "TenantId", "FinanceVoucherId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVouchers_TenantId_Status_CreatedAt",
                table: "FinanceVouchers",
                columns: new[] { "TenantId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVouchers_TenantId_VoucherNumber",
                table: "FinanceVouchers",
                columns: new[] { "TenantId", "VoucherNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVoucherSequences_TenantId_VoucherType_FiscalYear",
                table: "FinanceVoucherSequences",
                columns: new[] { "TenantId", "VoucherType", "FiscalYear" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentGatewayConfigurations_TenantId_GatewaySlug",
                table: "PaymentGatewayConfigurations",
                columns: new[] { "TenantId", "GatewaySlug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentGatewayEvents_TenantId_CreatedAt",
                table: "PaymentGatewayEvents",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentGatewayEvents_TenantId_GatewaySlug_ReferenceId",
                table: "PaymentGatewayEvents",
                columns: new[] { "TenantId", "GatewaySlug", "ReferenceId" });

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION finance_tenant_id() RETURNS uuid
                LANGUAGE sql STABLE AS $$ SELECT NULLIF(current_setting('app.tenant_id', true), '')::uuid $$;

                CREATE OR REPLACE FUNCTION finance_assert_posted_voucher_balanced() RETURNS trigger
                LANGUAGE plpgsql AS $$
                DECLARE voucher_id uuid; total_debit numeric; total_credit numeric;
                BEGIN
                    IF TG_TABLE_NAME = 'FinanceVouchers' THEN
                        voucher_id := COALESCE(NEW."Id", OLD."Id");
                    ELSE
                        voucher_id := COALESCE(NEW."FinanceVoucherId", OLD."FinanceVoucherId");
                    END IF;
                    IF EXISTS (SELECT 1 FROM "FinanceVouchers" WHERE "Id" = voucher_id AND "Status" = 'POSTED') THEN
                        SELECT COALESCE(SUM("Debit"), 0), COALESCE(SUM("Credit"), 0)
                          INTO total_debit, total_credit
                          FROM "FinanceVoucherLines" WHERE "FinanceVoucherId" = voucher_id;
                        IF total_debit <= 0 OR total_debit <> total_credit THEN
                            RAISE EXCEPTION 'posted finance voucher % is not balanced', voucher_id;
                        END IF;
                    END IF;
                    RETURN COALESCE(NEW, OLD);
                END $$;

                CREATE CONSTRAINT TRIGGER finance_voucher_lines_balanced
                AFTER INSERT OR UPDATE OR DELETE ON "FinanceVoucherLines"
                DEFERRABLE INITIALLY DEFERRED FOR EACH ROW
                EXECUTE FUNCTION finance_assert_posted_voucher_balanced();

                CREATE CONSTRAINT TRIGGER finance_voucher_status_balanced
                AFTER UPDATE OF "Status" ON "FinanceVouchers"
                DEFERRABLE INITIALLY DEFERRED FOR EACH ROW
                EXECUTE FUNCTION finance_assert_posted_voucher_balanced();

                CREATE OR REPLACE FUNCTION finance_block_posted_voucher_change() RETURNS trigger
                LANGUAGE plpgsql AS $$
                BEGIN
                    IF OLD."Status" = 'POSTED' THEN
                        RAISE EXCEPTION 'posted finance vouchers are immutable';
                    END IF;
                    RETURN NEW;
                END $$;
                CREATE TRIGGER finance_voucher_immutable
                BEFORE UPDATE OR DELETE ON "FinanceVouchers"
                FOR EACH ROW EXECUTE FUNCTION finance_block_posted_voucher_change();

                CREATE OR REPLACE FUNCTION finance_block_posted_line_change() RETURNS trigger
                LANGUAGE plpgsql AS $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM "FinanceVouchers" WHERE "Id" = COALESCE(OLD."FinanceVoucherId", NEW."FinanceVoucherId") AND "Status" = 'POSTED') THEN
                        RAISE EXCEPTION 'posted finance voucher lines are immutable';
                    END IF;
                    RETURN COALESCE(NEW, OLD);
                END $$;
                CREATE TRIGGER finance_voucher_line_immutable
                BEFORE UPDATE OR DELETE ON "FinanceVoucherLines"
                FOR EACH ROW EXECUTE FUNCTION finance_block_posted_line_change();

                CREATE OR REPLACE FUNCTION finance_block_gateway_event_change() RETURNS trigger
                LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'payment gateway events are append-only'; END $$;
                CREATE TRIGGER finance_gateway_event_immutable
                BEFORE UPDATE OR DELETE ON "PaymentGatewayEvents"
                FOR EACH ROW EXECUTE FUNCTION finance_block_gateway_event_change();
                """);

            foreach (var table in new[] { "FinanceVouchers", "FinanceVoucherLines", "FinanceVoucherApprovals", "FinanceVoucherSequences", "FeeDemands", "FeeInstallments", "FinancePayments", "PaymentGatewayConfigurations", "PaymentGatewayEvents", "ExchangeRateSnapshots" })
            {
                migrationBuilder.Sql($"ALTER TABLE \"{table}\" ENABLE ROW LEVEL SECURITY;");
                migrationBuilder.Sql($"CREATE POLICY finance_tenant_isolation_{table} ON \"{table}\" USING (\"TenantId\" = finance_tenant_id()) WITH CHECK (\"TenantId\" = finance_tenant_id());");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExchangeRateSnapshots");

            migrationBuilder.DropTable(
                name: "FeeInstallments");

            migrationBuilder.DropTable(
                name: "FinancePayments");

            migrationBuilder.DropTable(
                name: "FinanceVoucherApprovals");

            migrationBuilder.DropTable(
                name: "FinanceVoucherLines");

            migrationBuilder.DropTable(
                name: "FinanceVoucherSequences");

            migrationBuilder.DropTable(
                name: "PaymentGatewayConfigurations");

            migrationBuilder.DropTable(
                name: "PaymentGatewayEvents");

            migrationBuilder.DropTable(
                name: "FeeDemands");

            migrationBuilder.DropTable(
                name: "FinanceVouchers");
        }
    }
}
