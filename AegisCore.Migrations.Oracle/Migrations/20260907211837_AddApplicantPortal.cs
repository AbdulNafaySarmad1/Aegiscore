using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Migrations.Oracle.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicantPortal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdmissionDocumentRequirements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    TenantId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    IntakeId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    ProgramId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    DocumentType = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    IsRequired = table.Column<bool>(type: "BOOLEAN", nullable: false),
                    AcceptedFormat = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    MaxFileSizeMb = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionDocumentRequirements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdmissionIntakes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    TenantId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Year = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Semester = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    Status = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(150)", maxLength: 150, nullable: false),
                    OpensAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: true),
                    ClosesAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: true),
                    BatchId = table.Column<Guid>(type: "RAW(16)", nullable: true),
                    IsActive = table.Column<bool>(type: "BOOLEAN", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionIntakes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApplicantProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    TenantId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    ApplicantId = table.Column<string>(type: "NVARCHAR2(40)", maxLength: 40, nullable: false),
                    Email = table.Column<string>(type: "NVARCHAR2(320)", maxLength: 320, nullable: false),
                    EmailVerifiedAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: true),
                    IntakeId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    ProgramId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Status = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    FullName = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true),
                    DateOfBirth = table.Column<string>(type: "NVARCHAR2(10)", nullable: true),
                    Gender = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: true),
                    Nationality = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "NVARCHAR2(30)", maxLength: 30, nullable: true),
                    DecisionNote = table.Column<string>(type: "NVARCHAR2(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false),
                    ConvertedStudentId = table.Column<Guid>(type: "RAW(16)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicantProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApplicantDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    TenantId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    ApplicantId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    DocumentType = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    StoragePath = table.Column<string>(type: "NVARCHAR2(600)", maxLength: 600, nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false),
                    FileSize = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    MimeType = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    ValidationStatus = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    RejectionReason = table.Column<string>(type: "NVARCHAR2(1000)", maxLength: 1000, nullable: true),
                    ValidationConfidence = table.Column<double>(type: "BINARY_DOUBLE", nullable: true),
                    ValidationMethod = table.Column<string>(type: "NVARCHAR2(30)", maxLength: 30, nullable: true),
                    ReviewedBy = table.Column<Guid>(type: "RAW(16)", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicantDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicantDocuments_ApplicantProfiles_ApplicantId",
                        column: x => x.ApplicantId,
                        principalTable: "ApplicantProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionDocumentRequirements_TenantId_IntakeId_ProgramId_DocumentType",
                table: "AdmissionDocumentRequirements",
                columns: new[] { "TenantId", "IntakeId", "ProgramId", "DocumentType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionIntakes_TenantId_Year_Semester",
                table: "AdmissionIntakes",
                columns: new[] { "TenantId", "Year", "Semester" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicantDocuments_ApplicantId",
                table: "ApplicantDocuments",
                column: "ApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicantDocuments_TenantId_ApplicantId_DocumentType",
                table: "ApplicantDocuments",
                columns: new[] { "TenantId", "ApplicantId", "DocumentType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicantProfiles_TenantId_ApplicantId",
                table: "ApplicantProfiles",
                columns: new[] { "TenantId", "ApplicantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicantProfiles_TenantId_IntakeId_ProgramId",
                table: "ApplicantProfiles",
                columns: new[] { "TenantId", "IntakeId", "ProgramId" });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicantProfiles_TenantId_Status",
                table: "ApplicantProfiles",
                columns: new[] { "TenantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdmissionDocumentRequirements");

            migrationBuilder.DropTable(
                name: "AdmissionIntakes");

            migrationBuilder.DropTable(
                name: "ApplicantDocuments");

            migrationBuilder.DropTable(
                name: "ApplicantProfiles");
        }
    }
}
