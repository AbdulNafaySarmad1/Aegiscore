using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    OptionA = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    OptionB = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    OptionC = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    OptionD = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    CorrectOption = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    DifficultyLevel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SourceChunk = table.Column<string>(type: "text", nullable: true),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionQuestions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdmissionSubjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionSubjects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApplicantTests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicantId = table.Column<Guid>(type: "uuid", nullable: false),
                    TestProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PaperHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PaperJson = table.Column<string>(type: "text", nullable: false),
                    ResponsesJson = table.Column<string>(type: "text", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: true),
                    Passed = table.Column<bool>(type: "boolean", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProctorId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProctorNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicantTests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProgramTestProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    IntakeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    PassMark = table.Column<int>(type: "integer", nullable: false),
                    TimeLimitMinutes = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramTestProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuestionGenerationPrompts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    SystemPrompt = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionGenerationPrompts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyllabusDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoragePath = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    OcrStatus = table.Column<int>(type: "integer", nullable: false),
                    ExtractedText = table.Column<string>(type: "text", nullable: true),
                    OcrMethod = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyllabusDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProgramTestSubjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    TestProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionCount = table.Column<int>(type: "integer", nullable: false),
                    MarksPerQuestion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramTestSubjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgramTestSubjects_ProgramTestProfiles_TestProfileId",
                        column: x => x.TestProfileId,
                        principalTable: "ProgramTestProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
