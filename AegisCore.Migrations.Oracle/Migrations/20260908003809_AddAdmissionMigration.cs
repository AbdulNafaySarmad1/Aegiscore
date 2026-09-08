using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Migrations.Oracle.Migrations
{
    /// <inheritdoc />
    public partial class AddAdmissionMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RecoveryEmail",
                table: "Users",
                type: "NVARCHAR2(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AdmissionApplicantProfileId",
                table: "Students",
                type: "RAW(16)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicantId",
                table: "Students",
                type: "NVARCHAR2(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RollNumber",
                table: "Students",
                type: "NVARCHAR2(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EmailProvisioningConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    TenantId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Mode = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    DomainName = table.Column<string>(type: "NVARCHAR2(255)", maxLength: 255, nullable: true),
                    CredentialsCipher = table.Column<string>(type: "CLOB", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "RAW(16)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailProvisioningConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProgramIntakeSequences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    TenantId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    IntakeId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    ProgramId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    NextValue = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    ConcurrencyStamp = table.Column<Guid>(type: "RAW(16)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramIntakeSequences", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailProvisioningConfigs_TenantId",
                table: "EmailProvisioningConfigs",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProgramIntakeSequences_TenantId_IntakeId_ProgramId",
                table: "ProgramIntakeSequences",
                columns: new[] { "TenantId", "IntakeId", "ProgramId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailProvisioningConfigs");

            migrationBuilder.DropTable(
                name: "ProgramIntakeSequences");

            migrationBuilder.DropColumn(
                name: "RecoveryEmail",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AdmissionApplicantProfileId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "ApplicantId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "RollNumber",
                table: "Students");
        }
    }
}
