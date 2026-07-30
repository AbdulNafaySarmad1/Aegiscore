using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

public partial class FinanceAuditSeals : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "FinanceAuditSeals",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                PeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                PeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                EventCount = table.Column<int>(type: "integer", nullable: false),
                Algorithm = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                KeyId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                MerkleRoot = table.Column<byte[]>(type: "bytea", nullable: false),
                Signature = table.Column<byte[]>(type: "bytea", nullable: false),
                PublicKey = table.Column<byte[]>(type: "bytea", nullable: false),
                SealedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_FinanceAuditSeals", x => x.Id));

        migrationBuilder.CreateIndex("IX_FinanceAuditSeals_TenantId_SealedAt", "FinanceAuditSeals", ["TenantId", "SealedAt"]);
        migrationBuilder.Sql("""
            ALTER TABLE "FinanceAuditSeals" ENABLE ROW LEVEL SECURITY;
            ALTER TABLE "FinanceAuditSeals" FORCE ROW LEVEL SECURITY;
            CREATE POLICY finance_audit_seal_tenant_isolation ON "FinanceAuditSeals"
              USING ("TenantId" = finance_tenant_id()) WITH CHECK ("TenantId" = finance_tenant_id());
            CREATE OR REPLACE FUNCTION finance_audit_seal_is_immutable() RETURNS trigger AS $$
            BEGIN RAISE EXCEPTION 'finance audit seals are append-only'; END; $$ LANGUAGE plpgsql;
            CREATE TRIGGER finance_audit_seal_immutable BEFORE UPDATE OR DELETE ON "FinanceAuditSeals"
              FOR EACH ROW EXECUTE FUNCTION finance_audit_seal_is_immutable();
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TRIGGER IF EXISTS finance_audit_seal_immutable ON "FinanceAuditSeals";
            DROP FUNCTION IF EXISTS finance_audit_seal_is_immutable();
            DROP POLICY IF EXISTS finance_audit_seal_tenant_isolation ON "FinanceAuditSeals";
            ALTER TABLE "FinanceAuditSeals" DISABLE ROW LEVEL SECURITY;
            """);
        migrationBuilder.DropTable("FinanceAuditSeals");
    }
}
