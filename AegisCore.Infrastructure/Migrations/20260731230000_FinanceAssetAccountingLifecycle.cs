using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

// P11 multi-dialect note: application ITenantScoped global filters enforce portable tenant isolation; this applied PostgreSQL migration retains historical RLS for defense in depth.

namespace AegisCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FinanceAssetAccountingLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinanceAssetAccountingEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PreviousCarryingValue = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    NewCarryingValue = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Proceeds = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    JournalEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceAssetAccountingEvents", x => x.Id);
                    table.CheckConstraint("CK_FinanceAssetAccountingEvents_Values", "\"PreviousCarryingValue\" >= 0 AND \"NewCarryingValue\" >= 0 AND \"Proceeds\" >= 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceAssetAccountingEvents_JournalEntryId",
                table: "FinanceAssetAccountingEvents",
                column: "JournalEntryId",
                unique: true,
                filter: "\"JournalEntryId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceAssetAccountingEvents_TenantId_AssetId_OccurredAt",
                table: "FinanceAssetAccountingEvents",
                columns: new[] { "TenantId", "AssetId", "OccurredAt" });

            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX IF NOT EXISTS "UX_Assets_TenantId_Id" ON "Assets" ("TenantId", "Id");
                ALTER TABLE "FinanceAssetAccountingEvents" ADD CONSTRAINT "FK_FinanceAssetAccountingEvents_TenantAsset" FOREIGN KEY ("TenantId", "AssetId") REFERENCES "Assets" ("TenantId", "Id") ON DELETE RESTRICT;
                ALTER TABLE "FinanceAssetAccountingEvents" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "FinanceAssetAccountingEvents" FORCE ROW LEVEL SECURITY;
                CREATE POLICY finance_asset_event_tenant_isolation ON "FinanceAssetAccountingEvents" USING ("TenantId" = finance_tenant_id()) WITH CHECK ("TenantId" = finance_tenant_id());
                CREATE TRIGGER finance_asset_events_append_only BEFORE UPDATE OR DELETE ON "FinanceAssetAccountingEvents" FOR EACH ROW EXECUTE FUNCTION finance_append_only_guard();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS finance_asset_events_append_only ON \"FinanceAssetAccountingEvents\"; DROP POLICY IF EXISTS finance_asset_event_tenant_isolation ON \"FinanceAssetAccountingEvents\";");
            migrationBuilder.DropTable(
                name: "FinanceAssetAccountingEvents");
        }
    }
}
