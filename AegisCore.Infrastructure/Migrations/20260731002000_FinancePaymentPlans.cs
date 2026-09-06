using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using AegisCore.Infrastructure;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

// P11 multi-dialect note: application ITenantScoped global filters enforce portable tenant isolation; this applied PostgreSQL migration retains historical RLS for defense in depth.

[DbContext(typeof(AegisDbContext))]
[Migration("20260731002000_FinancePaymentPlans")]
public partial class FinancePaymentPlans : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "FinancePaymentPlanConfigurations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PlanType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                Enabled = table.Column<bool>(type: "boolean", nullable: false),
                SurchargePercent = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                MinimumDownPaymentPercent = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                MaximumDuration = table.Column<int>(type: "integer", nullable: false),
                LatePenaltyAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                LatePenaltyPercent = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                MaximumMissedInstallments = table.Column<int>(type: "integer", nullable: false),
                RequiresApproval = table.Column<bool>(type: "boolean", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            }, constraints: table => table.PrimaryKey("PK_FinancePaymentPlanConfigurations", x => x.Id));

        migrationBuilder.CreateTable(
            name: "FinancePaymentPlans",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                FeeDemandId = table.Column<Guid>(type: "uuid", nullable: false),
                PlanType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                Subtotal = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                SurchargeAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                TotalAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                InstallmentCount = table.Column<int>(type: "integer", nullable: false),
                StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                MissedInstallments = table.Column<int>(type: "integer", nullable: false),
                CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false)
            }, constraints: table =>
            {
                table.PrimaryKey("PK_FinancePaymentPlans", x => x.Id);
                table.ForeignKey("FK_FinancePaymentPlans_FeeDemands_FeeDemandId", x => x.FeeDemandId, "FeeDemands", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex("IX_FinancePaymentPlanConfigurations_TenantId_PlanType", "FinancePaymentPlanConfigurations", ["TenantId", "PlanType"], unique: true);
        migrationBuilder.CreateIndex("IX_FinancePaymentPlans_TenantId_StudentId_Status", "FinancePaymentPlans", ["TenantId", "StudentId", "Status"]);
        migrationBuilder.CreateIndex("IX_FinancePaymentPlans_TenantId_FeeDemandId", "FinancePaymentPlans", ["TenantId", "FeeDemandId"], unique: true);
        migrationBuilder.Sql("""
            ALTER TABLE "FinancePaymentPlanConfigurations" ENABLE ROW LEVEL SECURITY;
            ALTER TABLE "FinancePaymentPlanConfigurations" FORCE ROW LEVEL SECURITY;
            ALTER TABLE "FinancePaymentPlans" ENABLE ROW LEVEL SECURITY;
            ALTER TABLE "FinancePaymentPlans" FORCE ROW LEVEL SECURITY;
            CREATE POLICY finance_plan_config_tenant_isolation ON "FinancePaymentPlanConfigurations"
              USING ("TenantId" = finance_tenant_id()) WITH CHECK ("TenantId" = finance_tenant_id());
            CREATE POLICY finance_plan_tenant_isolation ON "FinancePaymentPlans"
              USING ("TenantId" = finance_tenant_id()) WITH CHECK ("TenantId" = finance_tenant_id());
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP POLICY IF EXISTS finance_plan_tenant_isolation ON "FinancePaymentPlans";
            DROP POLICY IF EXISTS finance_plan_config_tenant_isolation ON "FinancePaymentPlanConfigurations";
            ALTER TABLE "FinancePaymentPlans" DISABLE ROW LEVEL SECURITY;
            ALTER TABLE "FinancePaymentPlanConfigurations" DISABLE ROW LEVEL SECURITY;
            """);
        migrationBuilder.DropTable("FinancePaymentPlans");
        migrationBuilder.DropTable("FinancePaymentPlanConfigurations");
    }
}
