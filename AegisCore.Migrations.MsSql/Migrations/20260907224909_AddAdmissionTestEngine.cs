using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Migrations.MsSql.Migrations
{
    /// <inheritdoc />
    public partial class AddAdmissionTestEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdmissionQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    OptionA = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    OptionB = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    OptionC = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    OptionD = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    CorrectOption = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    DifficultyLevel = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SourceChunk = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ApprovedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionQuestions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdmissionSubjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionSubjects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApplicantTests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TestProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PaperHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PaperJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResponsesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: true),
                    Passed = table.Column<bool>(type: "bit", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ProctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProctorNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicantTests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProgramTestProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntakeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProgramId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PassMark = table.Column<int>(type: "int", nullable: false),
                    TimeLimitMinutes = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramTestProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuestionGenerationPrompts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    SystemPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionGenerationPrompts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyllabusDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoragePath = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    OcrStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExtractedText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OcrMethod = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyllabusDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProgramTestSubjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TestProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionCount = table.Column<int>(type: "int", nullable: false),
                    MarksPerQuestion = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramTestSubjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgramTestSubjects_ProgramTestProfiles_TestProfileId",
                        column: x => x.TestProfileId,
                        principalTable: "ProgramTestProfiles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionQuestions_TenantId_SubjectId_Status",
                table: "AdmissionQuestions",
                columns: new[] { "TenantId", "SubjectId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionSubjects_TenantId_Code",
                table: "AdmissionSubjects",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicantTests_TenantId_ApplicantId",
                table: "ApplicantTests",
                columns: new[] { "TenantId", "ApplicantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicantTests_TenantId_Status",
                table: "ApplicantTests",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ProgramTestProfiles_TenantId_IntakeId_ProgramId",
                table: "ProgramTestProfiles",
                columns: new[] { "TenantId", "IntakeId", "ProgramId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProgramTestSubjects_TenantId_TestProfileId_SubjectId",
                table: "ProgramTestSubjects",
                columns: new[] { "TenantId", "TestProfileId", "SubjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProgramTestSubjects_TestProfileId",
                table: "ProgramTestSubjects",
                column: "TestProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionGenerationPrompts_TenantId_IsActive",
                table: "QuestionGenerationPrompts",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_SyllabusDocuments_TenantId_SubjectId_UploadedAt",
                table: "SyllabusDocuments",
                columns: new[] { "TenantId", "SubjectId", "UploadedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdmissionQuestions");

            migrationBuilder.DropTable(
                name: "AdmissionSubjects");

            migrationBuilder.DropTable(
                name: "ApplicantTests");

            migrationBuilder.DropTable(
                name: "ProgramTestSubjects");

            migrationBuilder.DropTable(
                name: "QuestionGenerationPrompts");

            migrationBuilder.DropTable(
                name: "SyllabusDocuments");

            migrationBuilder.DropTable(
                name: "ProgramTestProfiles");
        }
    }
}
