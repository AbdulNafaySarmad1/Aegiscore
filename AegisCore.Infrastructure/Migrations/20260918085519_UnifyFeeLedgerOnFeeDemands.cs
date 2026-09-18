using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UnifyFeeLedgerOnFeeDemands : Migration
    {
        /// <inheritdoc />
        /// <summary>
        /// The v1 ledger (StudentFeeDues / StudentFeePayments) is folded into the finance ledger
        /// (FeeDemands / FeeInstallments / FinancePayments), row for row, keeping the v1 ids so any
        /// reference to a due keeps resolving to the same demand. The columns the v1 ledger carried
        /// (semester, fee structure, scholarship, waiver) are added to FeeDemands first, then the
        /// data is copied, then the v1 tables go. Rows already present in FeeDemands are left alone.
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FeeStructureId",
                table: "FeeDemands",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ScholarshipDeduction",
                table: "FeeDemands",
                type: "numeric(19,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "SemesterId",
                table: "FeeDemands",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WaivedAmount",
                table: "FeeDemands",
                type: "numeric(19,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "WaivedBy",
                table: "FeeDemands",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WaivedReason",
                table: "FeeDemands",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.Sql("""
                INSERT INTO "FeeDemands" ("Id", "StudentId", "DemandNumber", "Currency", "TotalAmount", "PaidAmount", "OutstandingAmount", "Status", "IssuedAt", "DueDate", "TenantId",
                                          "SemesterId", "FeeStructureId", "ScholarshipDeduction", "WaivedAmount", "WaivedBy", "WaivedReason")
                SELECT d."Id", d."StudentId", 'FD-V1-' || left(replace(d."Id"::text, '-', ''), 12), 'PKR', d."TotalDue", d."PaidAmount",
                       GREATEST(0, d."TotalDue" - d."PaidAmount" - d."ScholarshipDeduction"),
                       CASE lower(d."Status") WHEN 'paid' THEN 'PAID' WHEN 'partial' THEN 'PARTIAL' WHEN 'waived' THEN 'WAIVED' ELSE 'OPEN' END,
                       (d."CreatedAt" AT TIME ZONE 'UTC'), (d."CreatedAt" AT TIME ZONE 'UTC') + interval '30 days', d."TenantId",
                       d."SemesterId", d."FeeStructureId", d."ScholarshipDeduction", 0, d."WaivedBy", left(d."WaivedReason", 500)
                FROM "StudentFeeDues" d
                WHERE NOT EXISTS (SELECT 1 FROM "FeeDemands" f WHERE f."Id" = d."Id");
                """);
            migrationBuilder.Sql("""
                INSERT INTO "FeeInstallments" ("Id", "FeeDemandId", "Sequence", "Amount", "PaidAmount", "PenaltyAmount", "RecognizedAmount", "DueDate", "Status", "TenantId")
                SELECT md5(d."Id"::text || ':installment')::uuid, d."Id", 1, d."TotalDue", d."PaidAmount", 0, 0, (d."CreatedAt" AT TIME ZONE 'UTC') + interval '30 days',
                       CASE WHEN d."PaidAmount" >= d."TotalDue" THEN 'PAID' WHEN d."PaidAmount" > 0 THEN 'PARTIAL' ELSE 'PENDING' END, d."TenantId"
                FROM "StudentFeeDues" d
                WHERE NOT EXISTS (SELECT 1 FROM "FeeInstallments" i WHERE i."FeeDemandId" = d."Id");
                """);
            migrationBuilder.Sql("""
                INSERT INTO "FinancePayments" ("Id", "FeeDemandId", "FeeInstallmentId", "IdempotencyKey", "Method", "GatewaySlug", "ExternalReference", "Amount", "Status", "WebhookId", "CreatedAt", "SettledAt", "TenantId")
                SELECT p."Id", p."FeeDueId", md5(p."FeeDueId"::text || ':installment')::uuid, 'v1:' || left(replace(p."Id"::text, '-', ''), 36), upper(left(p."PaymentMethod", 30)), NULL, left(p."ReferenceNumber", 200), p."Amount",
                       CASE WHEN p."VerifiedBy" IS NOT NULL THEN 'SETTLED' ELSE 'PENDING' END, NULL, (p."PaymentDate" AT TIME ZONE 'UTC'),
                       CASE WHEN p."VerifiedBy" IS NOT NULL THEN COALESCE(p."VerificationDate", p."PaymentDate") AT TIME ZONE 'UTC' END, p."TenantId"
                FROM "StudentFeePayments" p
                WHERE EXISTS (SELECT 1 FROM "FeeDemands" f WHERE f."Id" = p."FeeDueId")
                  AND NOT EXISTS (SELECT 1 FROM "FinancePayments" x WHERE x."Id" = p."Id");
                """);

            migrationBuilder.DropTable(
                name: "StudentFeePayments");

            migrationBuilder.DropTable(
                name: "StudentFeeDues");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FeeStructureId",
                table: "FeeDemands");

            migrationBuilder.DropColumn(
                name: "ScholarshipDeduction",
                table: "FeeDemands");

            migrationBuilder.DropColumn(
                name: "SemesterId",
                table: "FeeDemands");

            migrationBuilder.DropColumn(
                name: "WaivedAmount",
                table: "FeeDemands");

            migrationBuilder.DropColumn(
                name: "WaivedBy",
                table: "FeeDemands");

            migrationBuilder.DropColumn(
                name: "WaivedReason",
                table: "FeeDemands");

            migrationBuilder.CreateTable(
                name: "StudentFeeDues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FeeStructureId = table.Column<Guid>(type: "uuid", nullable: false),
                    OutstandingAmount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    ScholarshipDeduction = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    SemesterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalDue = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    WaivedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    WaivedReason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentFeeDues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudentFeePayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FeeDueId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PaymentMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReferenceNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    VerificationDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    VerifiedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentFeePayments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentFeeDues_Status",
                table: "StudentFeeDues",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_StudentFeeDues_StudentId_SemesterId",
                table: "StudentFeeDues",
                columns: new[] { "StudentId", "SemesterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentFeePayments_FeeDueId",
                table: "StudentFeePayments",
                column: "FeeDueId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentFeePayments_ReferenceNumber",
                table: "StudentFeePayments",
                column: "ReferenceNumber");
        }
    }
}
