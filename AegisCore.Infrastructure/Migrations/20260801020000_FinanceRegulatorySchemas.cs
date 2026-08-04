using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FinanceRegulatorySchemas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinanceRegulatorySchemas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Jurisdiction = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Version = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Format = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    RootElement = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NamespaceUri = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DefinitionJson = table.Column<string>(type: "jsonb", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceRegulatorySchemas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceRegulatorySubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RegulatorySchemaId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodLabel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PqcAlgorithm = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    PqcKeyId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PqcSignature = table.Column<byte[]>(type: "bytea", nullable: false),
                    PqcPublicKey = table.Column<byte[]>(type: "bytea", nullable: false),
                    GeneratedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceRegulatorySubmissions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceRegulatorySchemas_TenantId_Code_IsActive",
                table: "FinanceRegulatorySchemas",
                columns: new[] { "TenantId", "Code", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceRegulatorySchemas_TenantId_Code_Version",
                table: "FinanceRegulatorySchemas",
                columns: new[] { "TenantId", "Code", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceRegulatorySubmissions_TenantId_RegulatorySchemaId_Ge~",
                table: "FinanceRegulatorySubmissions",
                columns: new[] { "TenantId", "RegulatorySchemaId", "GeneratedAt" });
            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX "UX_FinanceRegulatorySchemas_TenantId_Id" ON "FinanceRegulatorySchemas" ("TenantId", "Id");
                ALTER TABLE "FinanceRegulatorySubmissions" ADD CONSTRAINT "FK_FinanceRegulatorySubmissions_TenantSchema" FOREIGN KEY ("TenantId", "RegulatorySchemaId") REFERENCES "FinanceRegulatorySchemas" ("TenantId", "Id") ON DELETE RESTRICT;
                ALTER TABLE "FinanceRegulatorySchemas" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "FinanceRegulatorySchemas" FORCE ROW LEVEL SECURITY;
                CREATE POLICY finance_regulatory_schema_tenant_isolation ON "FinanceRegulatorySchemas" USING ("TenantId" = finance_tenant_id()) WITH CHECK ("TenantId" = finance_tenant_id());
                ALTER TABLE "FinanceRegulatorySubmissions" ENABLE ROW LEVEL SECURITY;
                ALTER TABLE "FinanceRegulatorySubmissions" FORCE ROW LEVEL SECURITY;
                CREATE POLICY finance_regulatory_submission_tenant_isolation ON "FinanceRegulatorySubmissions" USING ("TenantId" = finance_tenant_id()) WITH CHECK ("TenantId" = finance_tenant_id());
                CREATE TRIGGER finance_regulatory_submissions_append_only BEFORE UPDATE OR DELETE ON "FinanceRegulatorySubmissions" FOR EACH ROW EXECUTE FUNCTION finance_append_only_guard();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FinanceRegulatorySubmissions");

            migrationBuilder.DropTable(
                name: "FinanceRegulatorySchemas");
        }
    }
}
