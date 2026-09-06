using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

// P11 multi-dialect note: application ITenantScoped global filters enforce portable tenant isolation; this applied PostgreSQL migration retains historical RLS for defense in depth.

[DbContext(typeof(AegisDbContext))]
[Migration("20260731143000_FinanceLedgerAppendOnlyGuards")]
public partial class FinanceLedgerAppendOnlyGuards : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.Sql("""
            CREATE OR REPLACE FUNCTION finance_journal_entry_guard()
            RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN
              IF TG_OP <> 'INSERT' THEN
                RAISE EXCEPTION 'Journal entries are append-only; use reversing entries';
              END IF;
              RETURN NEW;
            END;
            $$;
            DROP TRIGGER IF EXISTS finance_journal_entry_guard ON "JournalEntries";
            CREATE TRIGGER finance_journal_entry_guard
              BEFORE UPDATE OR DELETE ON "JournalEntries"
              FOR EACH ROW EXECUTE FUNCTION finance_journal_entry_guard();

            CREATE OR REPLACE FUNCTION finance_posted_journal_line_guard()
            RETURNS trigger LANGUAGE plpgsql AS $$
            DECLARE posted boolean;
            BEGIN
              SELECT "IsPosted" INTO posted FROM "JournalEntries" WHERE "Id" = COALESCE(NEW."JournalEntryId", OLD."JournalEntryId");
              IF posted THEN
                RAISE EXCEPTION 'Lines of posted journal entries are immutable';
              END IF;
              RETURN COALESCE(NEW, OLD);
            END;
            $$;
            DROP TRIGGER IF EXISTS finance_posted_journal_line_guard ON "JournalEntryLines";
            CREATE TRIGGER finance_posted_journal_line_guard
              BEFORE UPDATE OR DELETE ON "JournalEntryLines"
              FOR EACH ROW EXECUTE FUNCTION finance_posted_journal_line_guard();

            CREATE OR REPLACE FUNCTION finance_append_only_guard()
            RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN
              RAISE EXCEPTION 'This finance record is append-only';
            END;
            $$;
            DROP TRIGGER IF EXISTS finance_audit_log_append_only ON "AuditLogs";
            CREATE TRIGGER finance_audit_log_append_only
              BEFORE UPDATE OR DELETE ON "AuditLogs"
              FOR EACH ROW EXECUTE FUNCTION finance_append_only_guard();
            DROP TRIGGER IF EXISTS finance_audit_seal_append_only ON "FinanceAuditSeals";
            CREATE TRIGGER finance_audit_seal_append_only
              BEFORE UPDATE OR DELETE ON "FinanceAuditSeals"
              FOR EACH ROW EXECUTE FUNCTION finance_append_only_guard();
            """);
    }

    protected override void Down(MigrationBuilder m)
    {
        m.Sql("DROP TRIGGER IF EXISTS finance_audit_seal_append_only ON \"FinanceAuditSeals\"; DROP TRIGGER IF EXISTS finance_audit_log_append_only ON \"AuditLogs\"; DROP TRIGGER IF EXISTS finance_posted_journal_line_guard ON \"JournalEntryLines\"; DROP TRIGGER IF EXISTS finance_journal_entry_guard ON \"JournalEntries\"; DROP FUNCTION IF EXISTS finance_append_only_guard(); DROP FUNCTION IF EXISTS finance_posted_journal_line_guard(); DROP FUNCTION IF EXISTS finance_journal_entry_guard();");
    }
}
