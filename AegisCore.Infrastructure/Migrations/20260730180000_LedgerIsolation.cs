using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using AegisCore.Infrastructure;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

// P11 multi-dialect note: application ITenantScoped global filters enforce portable tenant isolation; this applied PostgreSQL migration retains historical RLS for defense in depth.

[DbContext(typeof(AegisDbContext))]
[Migration("20260730180000_LedgerIsolation")]
public partial class LedgerIsolation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE "JournalEntries" ENABLE ROW LEVEL SECURITY;
            ALTER TABLE "JournalEntries" FORCE ROW LEVEL SECURITY;
            ALTER TABLE "JournalEntryLines" ENABLE ROW LEVEL SECURITY;
            ALTER TABLE "JournalEntryLines" FORCE ROW LEVEL SECURITY;
            ALTER TABLE "FinanceVouchers" FORCE ROW LEVEL SECURITY;
            ALTER TABLE "FinanceVoucherLines" FORCE ROW LEVEL SECURITY;
            ALTER TABLE "FinanceVoucherApprovals" FORCE ROW LEVEL SECURITY;
            ALTER TABLE "FinanceVoucherSequences" FORCE ROW LEVEL SECURITY;
            ALTER TABLE "FeeDemands" FORCE ROW LEVEL SECURITY;
            ALTER TABLE "FeeInstallments" FORCE ROW LEVEL SECURITY;
            ALTER TABLE "FinancePayments" FORCE ROW LEVEL SECURITY;
            ALTER TABLE "PaymentGatewayConfigurations" FORCE ROW LEVEL SECURITY;
            ALTER TABLE "PaymentGatewayEvents" FORCE ROW LEVEL SECURITY;
            ALTER TABLE "ExchangeRateSnapshots" FORCE ROW LEVEL SECURITY;
            ALTER TABLE "WizardSessions" FORCE ROW LEVEL SECURITY;
            ALTER TABLE "WizardAdminCredentials" FORCE ROW LEVEL SECURITY;
            CREATE POLICY journal_entry_tenant_isolation ON "JournalEntries"
              USING ("TenantId" = finance_tenant_id()) WITH CHECK ("TenantId" = finance_tenant_id());
            CREATE POLICY journal_line_tenant_isolation ON "JournalEntryLines"
              USING ("TenantId" = finance_tenant_id()) WITH CHECK ("TenantId" = finance_tenant_id());
            CREATE OR REPLACE FUNCTION finance_journal_is_immutable() RETURNS trigger AS $$
            BEGIN
              RAISE EXCEPTION 'journal entries and lines are append-only';
            END; $$ LANGUAGE plpgsql;
            CREATE TRIGGER journal_entry_immutable BEFORE UPDATE OR DELETE ON "JournalEntries"
              FOR EACH ROW EXECUTE FUNCTION finance_journal_is_immutable();
            CREATE TRIGGER journal_line_immutable BEFORE UPDATE OR DELETE ON "JournalEntryLines"
              FOR EACH ROW EXECUTE FUNCTION finance_journal_is_immutable();
            CREATE OR REPLACE FUNCTION finance_journal_is_balanced() RETURNS trigger AS $$
            DECLARE entry_id uuid;
            DECLARE debit_total numeric;
            DECLARE credit_total numeric;
            BEGIN
              entry_id := COALESCE(NEW."JournalEntryId", OLD."JournalEntryId");
              SELECT COALESCE(SUM("Debit"), 0), COALESCE(SUM("Credit"), 0)
                INTO debit_total, credit_total FROM "JournalEntryLines" WHERE "JournalEntryId" = entry_id;
              IF debit_total <> credit_total THEN
                RAISE EXCEPTION 'journal entry % is not balanced', entry_id;
              END IF;
              RETURN NULL;
            END; $$ LANGUAGE plpgsql;
            CREATE CONSTRAINT TRIGGER journal_entry_balanced
              AFTER INSERT OR UPDATE OR DELETE ON "JournalEntryLines"
              DEFERRABLE INITIALLY DEFERRED FOR EACH ROW
              EXECUTE FUNCTION finance_journal_is_balanced();
            CREATE INDEX IF NOT EXISTS "IX_JournalEntries_TenantId_EntryDate"
              ON "JournalEntries" ("TenantId", "EntryDate");
            CREATE INDEX IF NOT EXISTS "IX_JournalEntryLines_TenantId_JournalEntryId"
              ON "JournalEntryLines" ("TenantId", "JournalEntryId");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TRIGGER IF EXISTS journal_entry_balanced ON "JournalEntryLines";
            DROP TRIGGER IF EXISTS journal_line_immutable ON "JournalEntryLines";
            DROP TRIGGER IF EXISTS journal_entry_immutable ON "JournalEntries";
            DROP FUNCTION IF EXISTS finance_journal_is_balanced();
            DROP FUNCTION IF EXISTS finance_journal_is_immutable();
            DROP POLICY IF EXISTS journal_line_tenant_isolation ON "JournalEntryLines";
            DROP POLICY IF EXISTS journal_entry_tenant_isolation ON "JournalEntries";
            ALTER TABLE "JournalEntryLines" DISABLE ROW LEVEL SECURITY;
            ALTER TABLE "JournalEntries" DISABLE ROW LEVEL SECURITY;
            DROP INDEX IF EXISTS "IX_JournalEntryLines_TenantId_JournalEntryId";
            DROP INDEX IF EXISTS "IX_JournalEntries_TenantId_EntryDate";
            """);
    }
}
