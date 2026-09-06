using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

// P11 multi-dialect note: application ITenantScoped global filters enforce portable tenant isolation; this applied PostgreSQL migration retains historical RLS for defense in depth.

namespace AegisCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FinanceCostCenterTenantScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Departments_Code",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_CostCenters_Code",
                table: "CostCenters");

            migrationBuilder.AddColumn<Guid>(
                name: "CostCenterId",
                table: "JournalEntryLines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Departments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "CostCenters",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql("""
                DO $$
                DECLARE tenant_count integer;
                DECLARE only_tenant uuid;
                BEGIN
                    SELECT COUNT(*) INTO tenant_count FROM "Tenants";
                    SELECT "Id" INTO only_tenant FROM "Tenants" ORDER BY "Id" LIMIT 1;
                    IF tenant_count = 1 THEN
                        UPDATE "Departments" SET "TenantId" = only_tenant WHERE "TenantId" = '00000000-0000-0000-0000-000000000000';
                        UPDATE "CostCenters" SET "TenantId" = only_tenant WHERE "TenantId" = '00000000-0000-0000-0000-000000000000';
                    ELSIF EXISTS (SELECT 1 FROM "Departments" WHERE "TenantId" = '00000000-0000-0000-0000-000000000000')
                       OR EXISTS (SELECT 1 FROM "CostCenters" WHERE "TenantId" = '00000000-0000-0000-0000-000000000000') THEN
                        RAISE EXCEPTION 'Cannot infer tenant ownership for legacy departments or cost centres';
                    END IF;
                END $$;
                ALTER TABLE "Departments" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "Departments" FORCE ROW LEVEL SECURITY;
                ALTER TABLE "CostCenters" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "CostCenters" FORCE ROW LEVEL SECURITY;
                CREATE POLICY department_tenant_isolation ON "Departments" USING ("TenantId" = finance_tenant_id()) WITH CHECK ("TenantId" = finance_tenant_id());
                CREATE POLICY cost_center_tenant_isolation ON "CostCenters" USING ("TenantId" = finance_tenant_id()) WITH CHECK ("TenantId" = finance_tenant_id());
                CREATE INDEX "IX_JournalEntryLines_TenantId_CostCenterId" ON "JournalEntryLines" ("TenantId", "CostCenterId");
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_TenantId_Code",
                table: "Departments",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CostCenters_TenantId_Code",
                table: "CostCenters",
                columns: new[] { "TenantId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS "IX_JournalEntryLines_TenantId_CostCenterId";
                DROP POLICY IF EXISTS department_tenant_isolation ON "Departments";
                DROP POLICY IF EXISTS cost_center_tenant_isolation ON "CostCenters";
                ALTER TABLE "Departments" NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE "Departments" DISABLE ROW LEVEL SECURITY;
                ALTER TABLE "CostCenters" NO FORCE ROW LEVEL SECURITY;
                ALTER TABLE "CostCenters" DISABLE ROW LEVEL SECURITY;
                """);
            migrationBuilder.DropIndex(
                name: "IX_Departments_TenantId_Code",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_CostCenters_TenantId_Code",
                table: "CostCenters");

            migrationBuilder.DropColumn(
                name: "CostCenterId",
                table: "JournalEntryLines");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "CostCenters");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_Code",
                table: "Departments",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CostCenters_Code",
                table: "CostCenters",
                column: "Code",
                unique: true);
        }
    }
}
