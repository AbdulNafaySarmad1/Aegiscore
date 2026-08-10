using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations;

[DbContext(typeof(AegisDbContext))]
[Migration("20260810121000_LinkAdmissionApplicant")]
public sealed class LinkAdmissionApplicant : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "ApplicantUserId",
            table: "AdmissionApplications",
            type: "uuid",
            nullable: true);
        migrationBuilder.CreateIndex(
            name: "IX_AdmissionApplications_ApplicantUserId",
            table: "AdmissionApplications",
            column: "ApplicantUserId");
        migrationBuilder.AddForeignKey(
            name: "FK_AdmissionApplications_Users_ApplicantUserId",
            table: "AdmissionApplications",
            column: "ApplicantUserId",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_AdmissionApplications_Users_ApplicantUserId",
            table: "AdmissionApplications");
        migrationBuilder.DropIndex(
            name: "IX_AdmissionApplications_ApplicantUserId",
            table: "AdmissionApplications");
        migrationBuilder.DropColumn(
            name: "ApplicantUserId",
            table: "AdmissionApplications");
    }
}
