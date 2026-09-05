using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFerpaGdprCompliance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DirectoryOptOut",
                table: "Students",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ConsentLedgers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DataCategory = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ConsentedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConsentText = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    IpAddress = table.Column<byte[]>(type: "bytea", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsentLedgers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ErasureRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeadlineAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ErasedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false, defaultValue: "Pending"),
                    FailureReason = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErasureRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FerpaAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserRole = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IpAddress = table.Column<byte[]>(type: "bytea", nullable: true),
                    Justified = table.Column<bool>(type: "boolean", nullable: false),
                    UserErased = table.Column<bool>(type: "boolean", nullable: false),
                    StudentErased = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FerpaAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsentLedgers_TenantId_RevokedAt",
                table: "ConsentLedgers",
                columns: new[] { "TenantId", "RevokedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ConsentLedgers_TenantId_UserId_DataCategory",
                table: "ConsentLedgers",
                columns: new[] { "TenantId", "UserId", "DataCategory" });

            migrationBuilder.CreateIndex(
                name: "IX_ErasureRequests_TenantId_Status_DeadlineAt",
                table: "ErasureRequests",
                columns: new[] { "TenantId", "Status", "DeadlineAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FerpaAuditLogs_TenantId_AccessedAt",
                table: "FerpaAuditLogs",
                columns: new[] { "TenantId", "AccessedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FerpaAuditLogs_TenantId_StudentId_AccessedAt",
                table: "FerpaAuditLogs",
                columns: new[] { "TenantId", "StudentId", "AccessedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsentLedgers");

            migrationBuilder.DropTable(
                name: "ErasureRequests");

            migrationBuilder.DropTable(
                name: "FerpaAuditLogs");

            migrationBuilder.DropColumn(
                name: "DirectoryOptOut",
                table: "Students");
        }
    }
}
