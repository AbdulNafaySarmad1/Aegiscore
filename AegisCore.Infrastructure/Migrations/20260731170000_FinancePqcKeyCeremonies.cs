using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

// P11 multi-dialect note: application ITenantScoped global filters enforce portable tenant isolation; this applied PostgreSQL migration retains historical RLS for defense in depth.

namespace AegisCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FinancePqcKeyCeremonies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinancePqcKeyCeremonies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Purpose = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Algorithm = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    RequiredApprovals = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ActivatedKeyId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancePqcKeyCeremonies", x => x.Id);
                    table.CheckConstraint("CK_FinancePqcKeyCeremonies_Threshold", "\"RequiredApprovals\" BETWEEN 2 AND 20");
                });

            migrationBuilder.CreateTable(
                name: "FinancePqcKeyCeremonyApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CeremonyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ApprovalDigest = table.Column<byte[]>(type: "bytea", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancePqcKeyCeremonyApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancePqcKeyCeremonyApprovals_FinancePqcKeyCeremonies_Cere~",
                        column: x => x.CeremonyId,
                        principalTable: "FinancePqcKeyCeremonies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinancePqcKeyCeremonies_TenantId_Purpose_Status",
                table: "FinancePqcKeyCeremonies",
                columns: new[] { "TenantId", "Purpose", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancePqcKeyCeremonyApprovals_CeremonyId",
                table: "FinancePqcKeyCeremonyApprovals",
                column: "CeremonyId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancePqcKeyCeremonyApprovals_TenantId_CeremonyId_Approved~",
                table: "FinancePqcKeyCeremonyApprovals",
                columns: new[] { "TenantId", "CeremonyId", "ApprovedByUserId" },
                unique: true);

            migrationBuilder.Sql("""
                ALTER TABLE "FinancePqcKeyCeremonies" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "FinancePqcKeyCeremonies" FORCE ROW LEVEL SECURITY;
                CREATE POLICY finance_pqc_ceremony_tenant_isolation ON "FinancePqcKeyCeremonies"
                    USING ("TenantId" = finance_tenant_id()) WITH CHECK ("TenantId" = finance_tenant_id());
                ALTER TABLE "FinancePqcKeyCeremonyApprovals" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "FinancePqcKeyCeremonyApprovals" FORCE ROW LEVEL SECURITY;
                CREATE POLICY finance_pqc_ceremony_approval_tenant_isolation ON "FinancePqcKeyCeremonyApprovals"
                    USING ("TenantId" = finance_tenant_id()) WITH CHECK ("TenantId" = finance_tenant_id());
                CREATE TRIGGER finance_pqc_ceremony_approval_immutable
                    BEFORE UPDATE OR DELETE ON "FinancePqcKeyCeremonyApprovals"
                    FOR EACH ROW EXECUTE FUNCTION finance_append_only_guard();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS finance_pqc_ceremony_approval_immutable ON "FinancePqcKeyCeremonyApprovals";
                DROP POLICY IF EXISTS finance_pqc_ceremony_approval_tenant_isolation ON "FinancePqcKeyCeremonyApprovals";
                DROP POLICY IF EXISTS finance_pqc_ceremony_tenant_isolation ON "FinancePqcKeyCeremonies";
                """);
            migrationBuilder.DropTable(
                name: "FinancePqcKeyCeremonyApprovals");

            migrationBuilder.DropTable(
                name: "FinancePqcKeyCeremonies");
        }
    }
}
