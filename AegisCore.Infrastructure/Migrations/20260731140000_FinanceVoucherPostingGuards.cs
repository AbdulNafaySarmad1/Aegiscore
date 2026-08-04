using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260731140000_FinanceVoucherPostingGuards")]
public partial class FinanceVoucherPostingGuards : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.Sql("""
            CREATE OR REPLACE FUNCTION finance_voucher_posting_guard()
            RETURNS trigger LANGUAGE plpgsql AS $$
            DECLARE
              debit_total numeric(19,4);
              credit_total numeric(19,4);
              line_count integer;
            BEGIN
              IF TG_OP = 'DELETE' THEN
                IF OLD."Status" IN ('POSTED', 'ARCHIVED', 'VOID') THEN
                  RAISE EXCEPTION 'Posted finance vouchers are immutable';
                END IF;
                RETURN OLD;
              END IF;

              IF TG_OP = 'UPDATE' AND OLD."Status" IN ('POSTED', 'ARCHIVED', 'VOID') THEN
                RAISE EXCEPTION 'Posted finance vouchers are immutable';
              END IF;

              IF NEW."Status" = 'POSTED' THEN
                SELECT COALESCE(SUM("Debit"), 0), COALESCE(SUM("Credit"), 0), COUNT(*)
                  INTO debit_total, credit_total, line_count
                  FROM "FinanceVoucherLines"
                 WHERE "FinanceVoucherId" = NEW."Id" AND "TenantId" = NEW."TenantId";
                IF line_count < 2 OR debit_total <= 0 OR debit_total <> credit_total THEN
                  RAISE EXCEPTION 'Finance voucher % must contain balanced debit and credit lines before posting', NEW."Id";
                END IF;
              END IF;
              RETURN NEW;
            END;
            $$;
            DROP TRIGGER IF EXISTS finance_voucher_posting_guard ON "FinanceVouchers";
            CREATE TRIGGER finance_voucher_posting_guard
              BEFORE INSERT OR UPDATE OR DELETE ON "FinanceVouchers"
              FOR EACH ROW EXECUTE FUNCTION finance_voucher_posting_guard();

            CREATE OR REPLACE FUNCTION finance_posted_line_guard()
            RETURNS trigger LANGUAGE plpgsql AS $$
            DECLARE voucher_status text;
            BEGIN
              SELECT "Status" INTO voucher_status FROM "FinanceVouchers" WHERE "Id" = COALESCE(NEW."FinanceVoucherId", OLD."FinanceVoucherId");
              IF voucher_status IN ('POSTED', 'ARCHIVED', 'VOID') THEN
                RAISE EXCEPTION 'Lines of posted finance vouchers are immutable';
              END IF;
              RETURN COALESCE(NEW, OLD);
            END;
            $$;
            DROP TRIGGER IF EXISTS finance_posted_line_guard ON "FinanceVoucherLines";
            CREATE TRIGGER finance_posted_line_guard
              BEFORE INSERT OR UPDATE OR DELETE ON "FinanceVoucherLines"
              FOR EACH ROW EXECUTE FUNCTION finance_posted_line_guard();
            """);
    }

    protected override void Down(MigrationBuilder m)
    {
        m.Sql("DROP TRIGGER IF EXISTS finance_posted_line_guard ON \"FinanceVoucherLines\"; DROP TRIGGER IF EXISTS finance_voucher_posting_guard ON \"FinanceVouchers\"; DROP FUNCTION IF EXISTS finance_posted_line_guard(); DROP FUNCTION IF EXISTS finance_voucher_posting_guard();");
    }
}
