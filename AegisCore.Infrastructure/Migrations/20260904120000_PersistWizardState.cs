using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

/// <inheritdoc />
public partial class PersistWizardState : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "BootstrapTokenHash",
            table: "WizardSessions",
            type: "character varying(64)",
            maxLength: 64,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "Payload",
            table: "WizardSessions",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.CreateIndex(
            name: "IX_WizardSessions_BootstrapTokenHash",
            table: "WizardSessions",
            column: "BootstrapTokenHash",
            unique: true,
            filter: "\"BootstrapTokenHash\" <> ''");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_WizardSessions_BootstrapTokenHash",
            table: "WizardSessions");

        migrationBuilder.DropColumn(name: "BootstrapTokenHash", table: "WizardSessions");
        migrationBuilder.DropColumn(name: "Payload", table: "WizardSessions");
    }
}
