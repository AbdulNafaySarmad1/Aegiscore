using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V1_3_WizardSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WizardSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentStep = table.Column<int>(type: "integer", nullable: false),
                    LicenseMode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    LicenseKey = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    StepData = table.Column<string>(type: "jsonb", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByIp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WizardSessions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WizardSessions_TenantId_CompletedAt",
                table: "WizardSessions",
                columns: new[] { "TenantId", "CompletedAt" });

            migrationBuilder.Sql("ALTER TABLE \"WizardSessions\" ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("CREATE POLICY wizard_session_tenant_isolation ON \"WizardSessions\" USING (\"TenantId\" = finance_tenant_id()) WITH CHECK (\"TenantId\" = finance_tenant_id());");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WizardSessions");
        }
    }
}
