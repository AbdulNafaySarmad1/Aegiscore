using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IdentityProviderConfigurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalIssuer",
                table: "Users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "IdentityProviderConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderType = table.Column<int>(type: "integer", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ClientId = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    ClientSecretName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Authority = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RedirectUri = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Scopes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ExternalTenantId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AllowedDomain = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    GroupClaimName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    GroupRoleMappingJson = table.Column<string>(type: "jsonb", nullable: false),
                    DefaultRole = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityProviderConfigurations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_ExternalIdentity",
                table: "Users",
                columns: new[] { "TenantId", "ExternalIssuer", "ExternalId" },
                unique: true,
                filter: "\"ExternalIssuer\" IS NOT NULL AND \"ExternalId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_IdentityProviderConfigurations_TenantId_ProviderType",
                table: "IdentityProviderConfigurations",
                columns: new[] { "TenantId", "ProviderType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IdentityProviderConfigurations");

            migrationBuilder.DropIndex(
                name: "IX_Users_ExternalIdentity",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ExternalIssuer",
                table: "Users");
        }
    }
}
