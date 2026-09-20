using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ApplicantGuardianConsent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "GuardianConsentAt",
                table: "ApplicantProfiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuardianEmail",
                table: "ApplicantProfiles",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuardianName",
                table: "ApplicantProfiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuardianPhone",
                table: "ApplicantProfiles",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuardianRelationship",
                table: "ApplicantProfiles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GuardianConsentAt",
                table: "ApplicantProfiles");

            migrationBuilder.DropColumn(
                name: "GuardianEmail",
                table: "ApplicantProfiles");

            migrationBuilder.DropColumn(
                name: "GuardianName",
                table: "ApplicantProfiles");

            migrationBuilder.DropColumn(
                name: "GuardianPhone",
                table: "ApplicantProfiles");

            migrationBuilder.DropColumn(
                name: "GuardianRelationship",
                table: "ApplicantProfiles");
        }
    }
}
