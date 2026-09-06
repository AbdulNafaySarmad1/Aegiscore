using AegisCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

// P11 multi-dialect note: application ITenantScoped global filters enforce portable tenant isolation; this applied PostgreSQL migration retains historical RLS for defense in depth.

[DbContext(typeof(AegisDbContext))]
[Migration("20260731006000_FinancePqcKeys")]
public partial class FinancePqcKeys : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.CreateTable("FinancePqcKeys", t => new
        {
            Id = t.Column<Guid>("uuid", nullable: false), Purpose = t.Column<string>("character varying(50)", maxLength: 50, nullable: false), Algorithm = t.Column<string>("character varying(80)", maxLength: 80, nullable: false), KeyId = t.Column<string>("character varying(200)", maxLength: 200, nullable: false), SecretNamespace = t.Column<string>("character varying(400)", maxLength: 400, nullable: false), PublicKey = t.Column<byte[]>("bytea", nullable: false), IsActive = t.Column<bool>("boolean", nullable: false), CreatedAt = t.Column<DateTime>("timestamp with time zone", nullable: false), ExpiresAt = t.Column<DateTime>("timestamp with time zone", nullable: false), LastUsedAt = t.Column<DateTime>("timestamp with time zone", nullable: true), TenantId = t.Column<Guid>("uuid", nullable: false)
        }, constraints: t => t.PrimaryKey("PK_FinancePqcKeys", x => x.Id));
        m.CreateIndex("IX_FinancePqcKeys_TenantId_Purpose_KeyId", "FinancePqcKeys", ["TenantId", "Purpose", "KeyId"], unique: true);
        m.CreateIndex("IX_FinancePqcKeys_TenantId_Purpose_IsActive_ExpiresAt", "FinancePqcKeys", ["TenantId", "Purpose", "IsActive", "ExpiresAt"]);
        m.Sql("""
            ALTER TABLE "FinancePqcKeys" ENABLE ROW LEVEL SECURITY; ALTER TABLE "FinancePqcKeys" FORCE ROW LEVEL SECURITY;
            CREATE POLICY finance_pqc_key_tenant_isolation ON "FinancePqcKeys" USING ("TenantId" = finance_tenant_id()) WITH CHECK ("TenantId" = finance_tenant_id());
            """);
    }

    protected override void Down(MigrationBuilder m)
    {
        m.Sql("DROP POLICY IF EXISTS finance_pqc_key_tenant_isolation ON \"FinancePqcKeys\";");
        m.DropTable("FinancePqcKeys");
    }
}
