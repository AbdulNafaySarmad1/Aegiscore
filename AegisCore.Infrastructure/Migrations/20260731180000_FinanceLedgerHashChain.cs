using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FinanceLedgerHashChain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CanonicalPayload",
                table: "JournalEntries",
                type: "character varying(3000)",
                maxLength: 3000,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "EntryHash",
                table: "JournalEntries",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "PreviousHash",
                table: "JournalEntries",
                type: "bytea",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FinanceLedgerSignatures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JournalEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Algorithm = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    KeyId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Signature = table.Column<byte[]>(type: "bytea", nullable: false),
                    PublicKey = table.Column<byte[]>(type: "bytea", nullable: false),
                    SignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceLedgerSignatures", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceLedgerSignatures_TenantId_JournalEntryId",
                table: "FinanceLedgerSignatures",
                columns: new[] { "TenantId", "JournalEntryId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FinanceLedgerSignatures_JournalEntries_JournalEntryId",
                table: "FinanceLedgerSignatures",
                column: "JournalEntryId",
                principalTable: "JournalEntries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                CREATE EXTENSION IF NOT EXISTS pgcrypto;
                CREATE OR REPLACE FUNCTION finance_chain_journal_entry()
                RETURNS trigger LANGUAGE plpgsql AS $$
                DECLARE prior bytea;
                BEGIN
                  IF NOT NEW."IsPosted" THEN RETURN NEW; END IF;
                  PERFORM pg_advisory_xact_lock(hashtextextended(NEW."TenantId"::text, 179426549));
                  SELECT "EntryHash" INTO prior
                    FROM "JournalEntries"
                    WHERE "TenantId" = NEW."TenantId" AND "IsPosted" AND "EntryHash" IS NOT NULL
                    ORDER BY "CreatedAt" DESC, "Id" DESC LIMIT 1;
                  NEW."PreviousHash" := COALESCE(prior, decode(repeat('00', 32), 'hex'));
                  NEW."CanonicalPayload" := concat_ws('|', NEW."Id"::text, NEW."TenantId"::text,
                    NEW."EntryNumber", NEW."EntryType"::text, COALESCE(NEW."Description", ''),
                    NEW."EntryDate"::text, NEW."TotalDebit"::text, NEW."TotalCredit"::text,
                    NEW."IsSandbox"::text, COALESCE(NEW."PostedByUserId"::text, ''),
                    COALESCE(NEW."PostedAt"::text, ''), NEW."CreatedAt"::text);
                  NEW."EntryHash" := digest(NEW."PreviousHash" || convert_to(NEW."CanonicalPayload", 'UTF8'), 'sha256');
                  RETURN NEW;
                END;
                $$;

                ALTER TABLE "JournalEntries" NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE "JournalEntries" DISABLE TRIGGER finance_journal_entry_guard;
                DO $$
                DECLARE row_record record; prior bytea; canonical text; lag_tenant uuid;
                BEGIN
                  FOR row_record IN SELECT * FROM "JournalEntries" WHERE "IsPosted" ORDER BY "TenantId", "CreatedAt", "Id" LOOP
                    IF prior IS NULL OR row_record."TenantId" <> lag_tenant THEN
                      prior := decode(repeat('00', 32), 'hex');
                    END IF;
                    canonical := concat_ws('|', row_record."Id"::text, row_record."TenantId"::text,
                      row_record."EntryNumber", row_record."EntryType"::text, COALESCE(row_record."Description", ''),
                      row_record."EntryDate"::text, row_record."TotalDebit"::text, row_record."TotalCredit"::text,
                      row_record."IsSandbox"::text, COALESCE(row_record."PostedByUserId"::text, ''),
                      COALESCE(row_record."PostedAt"::text, ''), row_record."CreatedAt"::text);
                    UPDATE "JournalEntries" SET "PreviousHash" = prior, "CanonicalPayload" = canonical,
                      "EntryHash" = digest(prior || convert_to(canonical, 'UTF8'), 'sha256') WHERE "Id" = row_record."Id";
                    SELECT "EntryHash" INTO prior FROM "JournalEntries" WHERE "Id" = row_record."Id";
                    lag_tenant := row_record."TenantId";
                  END LOOP;
                END $$;
                ALTER TABLE "JournalEntries" ENABLE TRIGGER finance_journal_entry_guard;
                ALTER TABLE "JournalEntries" FORCE ROW LEVEL SECURITY;

                CREATE TRIGGER finance_chain_journal_entry
                  BEFORE INSERT ON "JournalEntries" FOR EACH ROW EXECUTE FUNCTION finance_chain_journal_entry();
                ALTER TABLE "FinanceLedgerSignatures" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "FinanceLedgerSignatures" FORCE ROW LEVEL SECURITY;
                CREATE POLICY finance_ledger_signature_tenant_isolation ON "FinanceLedgerSignatures"
                  USING ("TenantId" = finance_tenant_id()) WITH CHECK ("TenantId" = finance_tenant_id());
                CREATE TRIGGER finance_ledger_signature_immutable
                  BEFORE UPDATE OR DELETE ON "FinanceLedgerSignatures"
                  FOR EACH ROW EXECUTE FUNCTION finance_append_only_guard();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS finance_ledger_signature_immutable ON "FinanceLedgerSignatures";
                DROP POLICY IF EXISTS finance_ledger_signature_tenant_isolation ON "FinanceLedgerSignatures";
                DROP TRIGGER IF EXISTS finance_chain_journal_entry ON "JournalEntries";
                DROP FUNCTION IF EXISTS finance_chain_journal_entry();
                """);
            migrationBuilder.DropTable(
                name: "FinanceLedgerSignatures");

            migrationBuilder.DropColumn(
                name: "CanonicalPayload",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "EntryHash",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "PreviousHash",
                table: "JournalEntries");
        }
    }
}
