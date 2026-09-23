using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FormIntegrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProgramOutcomes_Code",
                table: "ProgramOutcomes");

            migrationBuilder.DropIndex(
                name: "IX_CourseLearningOutcomes_Code",
                table: "CourseLearningOutcomes");

            migrationBuilder.CreateTable(
                name: "FormFieldMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalFieldId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    QuestionText = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Target = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CloCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ProgramOutcomeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    FirstSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormFieldMappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FormIntegrationSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Provider = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ExternalFormId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TokenHint = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    ScaleMax = table.Column<int>(type: "integer", nullable: false),
                    DefaultCourseId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultSemester = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TokenRotatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormIntegrationSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FormIntegrationSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalSubmissionId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FieldCount = table.Column<int>(type: "integer", nullable: false),
                    RecordedCount = table.Column<int>(type: "integer", nullable: false),
                    UnmappedCount = table.Column<int>(type: "integer", nullable: false),
                    Problems = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: true),
                    InstructorName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Semester = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AnswersJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormIntegrationSubmissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutcomeSurveyResponses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Channel = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResponseKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseLearningOutcomeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProgramOutcomeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    QuestionKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    QuestionText = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Score = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    ScaleMax = table.Column<int>(type: "integer", nullable: false),
                    InstructorName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Semester = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutcomeSurveyResponses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProgramOutcomes_TenantId_ProgramId_Code",
                table: "ProgramOutcomes",
                columns: new[] { "TenantId", "ProgramId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseLearningOutcomes_TenantId_CourseId_Code",
                table: "CourseLearningOutcomes",
                columns: new[] { "TenantId", "CourseId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormFieldMappings_TenantId_SourceId_ExternalFieldId",
                table: "FormFieldMappings",
                columns: new[] { "TenantId", "SourceId", "ExternalFieldId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormIntegrationSources_TokenHash",
                table: "FormIntegrationSources",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormIntegrationSubmissions_TenantId_SourceId_ExternalSubmis~",
                table: "FormIntegrationSubmissions",
                columns: new[] { "TenantId", "SourceId", "ExternalSubmissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutcomeSurveyResponses_SubmissionId",
                table: "OutcomeSurveyResponses",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_OutcomeSurveyResponses_TenantId_CourseId",
                table: "OutcomeSurveyResponses",
                columns: new[] { "TenantId", "CourseId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FormFieldMappings");

            migrationBuilder.DropTable(
                name: "FormIntegrationSources");

            migrationBuilder.DropTable(
                name: "FormIntegrationSubmissions");

            migrationBuilder.DropTable(
                name: "OutcomeSurveyResponses");

            migrationBuilder.DropIndex(
                name: "IX_ProgramOutcomes_TenantId_ProgramId_Code",
                table: "ProgramOutcomes");

            migrationBuilder.DropIndex(
                name: "IX_CourseLearningOutcomes_TenantId_CourseId_Code",
                table: "CourseLearningOutcomes");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramOutcomes_Code",
                table: "ProgramOutcomes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseLearningOutcomes_Code",
                table: "CourseLearningOutcomes",
                column: "Code",
                unique: true);
        }
    }
}
