using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

// P11 multi-dialect note: application ITenantScoped global filters enforce portable tenant isolation; this applied PostgreSQL migration retains historical RLS for defense in depth.

namespace AegisCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FinanceProcurementWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinancePurchaseOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancePurchaseOrders", x => x.Id);
                    table.CheckConstraint("CK_FinancePurchaseOrders_PositiveTotal", "\"TotalAmount\" > 0");
                    table.ForeignKey(
                        name: "FK_FinancePurchaseOrders_FinanceVendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "FinanceVendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceThreeWayMatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderTotal = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    ReceivedTotal = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    InvoiceTotal = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Variance = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MatchedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceThreeWayMatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceVendorContracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    StartsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    EndsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    RenewalNoticeDays = table.Column<int>(type: "integer", nullable: false),
                    SlaScore = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceVendorContracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceVendorContracts_FinanceVendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "FinanceVendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceVendorDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    FileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PqcAlgorithm = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    PqcKeyId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PqcSignature = table.Column<byte[]>(type: "bytea", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceVendorDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceVendorDocuments_FinanceVendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "FinanceVendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceGoodsReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ConfirmedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceGoodsReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceGoodsReceipts_FinancePurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "FinancePurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinancePurchaseOrderLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpenseAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancePurchaseOrderLines", x => x.Id);
                    table.CheckConstraint("CK_FinancePurchaseOrderLines_Amounts", "\"Quantity\" > 0 AND \"UnitPrice\" >= 0 AND \"LineTotal\" = \"Quantity\" * \"UnitPrice\"");
                    table.ForeignKey(
                        name: "FK_FinancePurchaseOrderLines_FinancePurchaseOrders_PurchaseOrd~",
                        column: x => x.PurchaseOrderId,
                        principalTable: "FinancePurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceVendorInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaymentVoucherId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceVendorInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceVendorInvoices_FinancePurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "FinancePurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceVendorInvoices_FinanceVendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "FinanceVendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceGoodsReceiptLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GoodsReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuantityAccepted = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    QuantityRejected = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceGoodsReceiptLines", x => x.Id);
                    table.CheckConstraint("CK_FinanceGoodsReceiptLines_Amounts", "\"QuantityAccepted\" >= 0 AND \"QuantityRejected\" >= 0 AND (\"QuantityAccepted\" + \"QuantityRejected\") > 0");
                    table.ForeignKey(
                        name: "FK_FinanceGoodsReceiptLines_FinanceGoodsReceipts_GoodsReceiptId",
                        column: x => x.GoodsReceiptId,
                        principalTable: "FinanceGoodsReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceGoodsReceiptLines_FinancePurchaseOrderLines_Purchase~",
                        column: x => x.PurchaseOrderLineId,
                        principalTable: "FinancePurchaseOrderLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceVendorInvoiceLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceVendorInvoiceLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceVendorInvoiceLines_FinancePurchaseOrderLines_Purchas~",
                        column: x => x.PurchaseOrderLineId,
                        principalTable: "FinancePurchaseOrderLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceVendorInvoiceLines_FinanceVendorInvoices_VendorInvoi~",
                        column: x => x.VendorInvoiceId,
                        principalTable: "FinanceVendorInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceGoodsReceiptLines_GoodsReceiptId",
                table: "FinanceGoodsReceiptLines",
                column: "GoodsReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceGoodsReceiptLines_PurchaseOrderLineId",
                table: "FinanceGoodsReceiptLines",
                column: "PurchaseOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceGoodsReceiptLines_TenantId_PurchaseOrderLineId",
                table: "FinanceGoodsReceiptLines",
                columns: new[] { "TenantId", "PurchaseOrderLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceGoodsReceipts_PurchaseOrderId",
                table: "FinanceGoodsReceipts",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceGoodsReceipts_TenantId_Number",
                table: "FinanceGoodsReceipts",
                columns: new[] { "TenantId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancePurchaseOrderLines_PurchaseOrderId",
                table: "FinancePurchaseOrderLines",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancePurchaseOrderLines_TenantId_PurchaseOrderId",
                table: "FinancePurchaseOrderLines",
                columns: new[] { "TenantId", "PurchaseOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancePurchaseOrders_TenantId_Number",
                table: "FinancePurchaseOrders",
                columns: new[] { "TenantId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancePurchaseOrders_TenantId_VendorId_Status",
                table: "FinancePurchaseOrders",
                columns: new[] { "TenantId", "VendorId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancePurchaseOrders_VendorId",
                table: "FinancePurchaseOrders",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceThreeWayMatches_TenantId_VendorInvoiceId",
                table: "FinanceThreeWayMatches",
                columns: new[] { "TenantId", "VendorInvoiceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVendorContracts_TenantId_ContractNumber",
                table: "FinanceVendorContracts",
                columns: new[] { "TenantId", "ContractNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVendorContracts_TenantId_Status_EndsOn",
                table: "FinanceVendorContracts",
                columns: new[] { "TenantId", "Status", "EndsOn" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVendorContracts_VendorId",
                table: "FinanceVendorContracts",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVendorDocuments_TenantId_VendorId_UploadedAt",
                table: "FinanceVendorDocuments",
                columns: new[] { "TenantId", "VendorId", "UploadedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVendorDocuments_VendorId",
                table: "FinanceVendorDocuments",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVendorInvoiceLines_PurchaseOrderLineId",
                table: "FinanceVendorInvoiceLines",
                column: "PurchaseOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVendorInvoiceLines_TenantId_VendorInvoiceId",
                table: "FinanceVendorInvoiceLines",
                columns: new[] { "TenantId", "VendorInvoiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVendorInvoiceLines_VendorInvoiceId",
                table: "FinanceVendorInvoiceLines",
                column: "VendorInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVendorInvoices_PurchaseOrderId",
                table: "FinanceVendorInvoices",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVendorInvoices_TenantId_VendorId_InvoiceNumber",
                table: "FinanceVendorInvoices",
                columns: new[] { "TenantId", "VendorId", "InvoiceNumber" },
                unique: true);

            migrationBuilder.Sql("""
                DO $$ DECLARE table_name text; BEGIN
                  FOREACH table_name IN ARRAY ARRAY[
                    'FinancePurchaseOrders','FinancePurchaseOrderLines','FinanceGoodsReceipts','FinanceGoodsReceiptLines',
                    'FinanceVendorInvoices','FinanceVendorInvoiceLines','FinanceThreeWayMatches','FinanceVendorContracts','FinanceVendorDocuments'
                  ] LOOP
                    EXECUTE format('ALTER TABLE %I ENABLE ROW LEVEL SECURITY', table_name);
                    EXECUTE format('ALTER TABLE %I FORCE ROW LEVEL SECURITY', table_name);
                    EXECUTE format('CREATE POLICY %I ON %I USING ("TenantId" = finance_tenant_id()) WITH CHECK ("TenantId" = finance_tenant_id())', lower(table_name) || '_tenant_isolation', table_name);
                  END LOOP;
                END $$;
                CREATE TRIGGER finance_grn_append_only BEFORE UPDATE OR DELETE ON "FinanceGoodsReceipts" FOR EACH ROW EXECUTE FUNCTION finance_append_only_guard();
                CREATE TRIGGER finance_grn_line_append_only BEFORE UPDATE OR DELETE ON "FinanceGoodsReceiptLines" FOR EACH ROW EXECUTE FUNCTION finance_append_only_guard();
                CREATE TRIGGER finance_three_way_match_append_only BEFORE UPDATE OR DELETE ON "FinanceThreeWayMatches" FOR EACH ROW EXECUTE FUNCTION finance_append_only_guard();
                CREATE TRIGGER finance_vendor_document_append_only BEFORE UPDATE OR DELETE ON "FinanceVendorDocuments" FOR EACH ROW EXECUTE FUNCTION finance_append_only_guard();
                """);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVendorInvoices_VendorId",
                table: "FinanceVendorInvoices",
                column: "VendorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS finance_vendor_document_append_only ON "FinanceVendorDocuments";
                DROP TRIGGER IF EXISTS finance_three_way_match_append_only ON "FinanceThreeWayMatches";
                DROP TRIGGER IF EXISTS finance_grn_line_append_only ON "FinanceGoodsReceiptLines";
                DROP TRIGGER IF EXISTS finance_grn_append_only ON "FinanceGoodsReceipts";
                """);
            migrationBuilder.DropTable(
                name: "FinanceGoodsReceiptLines");

            migrationBuilder.DropTable(
                name: "FinanceThreeWayMatches");

            migrationBuilder.DropTable(
                name: "FinanceVendorContracts");

            migrationBuilder.DropTable(
                name: "FinanceVendorDocuments");

            migrationBuilder.DropTable(
                name: "FinanceVendorInvoiceLines");

            migrationBuilder.DropTable(
                name: "FinanceGoodsReceipts");

            migrationBuilder.DropTable(
                name: "FinancePurchaseOrderLines");

            migrationBuilder.DropTable(
                name: "FinanceVendorInvoices");

            migrationBuilder.DropTable(
                name: "FinancePurchaseOrders");
        }
    }
}
