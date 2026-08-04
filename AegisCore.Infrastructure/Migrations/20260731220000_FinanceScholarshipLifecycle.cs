using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FinanceScholarshipLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ApplicationId",
                table: "FinanceScholarshipAwards",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RenewalReviewAt",
                table: "FinanceScholarshipAwards",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FinanceScholarshipPrograms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GrantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ScholarshipType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CoverageType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CoverageValue = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    DisbursementMethod = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MinimumCgpa = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    AllowsStacking = table.Column<bool>(type: "boolean", nullable: false),
                    OpensAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClosesAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DurationMonths = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceScholarshipPrograms", x => x.Id);
                    table.CheckConstraint("CK_FinanceScholarshipPrograms_Values", "\"CoverageValue\" > 0 AND \"DurationMonths\" > 0 AND \"ClosesAt\" > \"OpensAt\"");
                    table.ForeignKey(
                        name: "FK_FinanceScholarshipPrograms_FinanceGrants_GrantId",
                        column: x => x.GrantId,
                        principalTable: "FinanceGrants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceScholarshipApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Cgpa = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ApplicantStatement = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DecisionNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DecidedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AppliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DecidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AwardId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceScholarshipApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceScholarshipApplications_FinanceScholarshipPrograms_P~",
                        column: x => x.ProgramId,
                        principalTable: "FinanceScholarshipPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceScholarshipApplicationEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ToStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceScholarshipApplicationEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceScholarshipApplicationEvents_FinanceScholarshipAppli~",
                        column: x => x.ApplicationId,
                        principalTable: "FinanceScholarshipApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceScholarshipAwards_TenantId_ApplicationId",
                table: "FinanceScholarshipAwards",
                columns: new[] { "TenantId", "ApplicationId" },
                unique: true,
                filter: "\"ApplicationId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceScholarshipApplicationEvents_ApplicationId",
                table: "FinanceScholarshipApplicationEvents",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceScholarshipApplicationEvents_TenantId_ApplicationId_~",
                table: "FinanceScholarshipApplicationEvents",
                columns: new[] { "TenantId", "ApplicationId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceScholarshipApplications_ProgramId",
                table: "FinanceScholarshipApplications",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceScholarshipApplications_TenantId_ProgramId_StudentId",
                table: "FinanceScholarshipApplications",
                columns: new[] { "TenantId", "ProgramId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceScholarshipApplications_TenantId_Status_AppliedAt",
                table: "FinanceScholarshipApplications",
                columns: new[] { "TenantId", "Status", "AppliedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceScholarshipPrograms_GrantId",
                table: "FinanceScholarshipPrograms",
                column: "GrantId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceScholarshipPrograms_TenantId_Name",
                table: "FinanceScholarshipPrograms",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceScholarshipPrograms_TenantId_Status_ClosesAt",
                table: "FinanceScholarshipPrograms",
                columns: new[] { "TenantId", "Status", "ClosesAt" });

            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX "UX_FinanceScholarshipPrograms_TenantId_Id" ON "FinanceScholarshipPrograms" ("TenantId", "Id");
                CREATE UNIQUE INDEX "UX_FinanceScholarshipApplications_TenantId_Id" ON "FinanceScholarshipApplications" ("TenantId", "Id");
                ALTER TABLE "FinanceScholarshipApplications" ADD CONSTRAINT "FK_FinanceScholarshipApplications_TenantProgram" FOREIGN KEY ("TenantId", "ProgramId") REFERENCES "FinanceScholarshipPrograms" ("TenantId", "Id") ON DELETE RESTRICT;
                ALTER TABLE "FinanceScholarshipApplicationEvents" ADD CONSTRAINT "FK_FinanceScholarshipApplicationEvents_TenantApplication" FOREIGN KEY ("TenantId", "ApplicationId") REFERENCES "FinanceScholarshipApplications" ("TenantId", "Id") ON DELETE RESTRICT;
                ALTER TABLE "FinanceScholarshipAwards" ADD CONSTRAINT "FK_FinanceScholarshipAwards_TenantApplication" FOREIGN KEY ("TenantId", "ApplicationId") REFERENCES "FinanceScholarshipApplications" ("TenantId", "Id") ON DELETE RESTRICT;

                ALTER TABLE "FinanceScholarshipPrograms" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "FinanceScholarshipPrograms" FORCE ROW LEVEL SECURITY;
                CREATE POLICY finance_scholarship_program_tenant_isolation ON "FinanceScholarshipPrograms" USING ("TenantId" = finance_tenant_id()) WITH CHECK ("TenantId" = finance_tenant_id());
                ALTER TABLE "FinanceScholarshipApplications" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "FinanceScholarshipApplications" FORCE ROW LEVEL SECURITY;
                CREATE POLICY finance_scholarship_application_tenant_isolation ON "FinanceScholarshipApplications" USING ("TenantId" = finance_tenant_id()) WITH CHECK ("TenantId" = finance_tenant_id());
                ALTER TABLE "FinanceScholarshipApplicationEvents" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "FinanceScholarshipApplicationEvents" FORCE ROW LEVEL SECURITY;
                CREATE POLICY finance_scholarship_event_tenant_isolation ON "FinanceScholarshipApplicationEvents" USING ("TenantId" = finance_tenant_id()) WITH CHECK ("TenantId" = finance_tenant_id());
                CREATE TRIGGER finance_scholarship_events_append_only BEFORE UPDATE OR DELETE ON "FinanceScholarshipApplicationEvents" FOR EACH ROW EXECUTE FUNCTION finance_append_only_guard();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "FinanceScholarshipAwards" DROP CONSTRAINT IF EXISTS "FK_FinanceScholarshipAwards_TenantApplication";
                DROP TRIGGER IF EXISTS finance_scholarship_events_append_only ON "FinanceScholarshipApplicationEvents";
                DROP POLICY IF EXISTS finance_scholarship_event_tenant_isolation ON "FinanceScholarshipApplicationEvents";
                DROP POLICY IF EXISTS finance_scholarship_application_tenant_isolation ON "FinanceScholarshipApplications";
                DROP POLICY IF EXISTS finance_scholarship_program_tenant_isolation ON "FinanceScholarshipPrograms";
                """);
            migrationBuilder.DropTable(
                name: "FinanceScholarshipApplicationEvents");

            migrationBuilder.DropTable(
                name: "FinanceScholarshipApplications");

            migrationBuilder.DropTable(
                name: "FinanceScholarshipPrograms");

            migrationBuilder.DropIndex(
                name: "IX_FinanceScholarshipAwards_TenantId_ApplicationId",
                table: "FinanceScholarshipAwards");

            migrationBuilder.DropColumn(
                name: "ApplicationId",
                table: "FinanceScholarshipAwards");

            migrationBuilder.DropColumn(
                name: "RenewalReviewAt",
                table: "FinanceScholarshipAwards");
        }
    }
}
