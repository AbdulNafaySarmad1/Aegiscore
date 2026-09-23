using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OutcomeSurveys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SurveyId",
                table: "OutcomeSurveyResponses",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OutcomeSurveyComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SurveyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutcomeSurveyComments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutcomeSurveyCompletions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SurveyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutcomeSurveyCompletions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutcomeSurveyCourses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SurveyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutcomeSurveyCourses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutcomeSurveyQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SurveyId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Section = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProgramOutcomeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Required = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutcomeSurveyQuestions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutcomeSurveys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Instructions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Semester = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ScaleMax = table.Column<int>(type: "integer", nullable: false),
                    AskCourseClos = table.Column<bool>(type: "boolean", nullable: false),
                    CloSection = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CloPrompt = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    OpenedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutcomeSurveys", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutcomeSurveyResponses_SurveyId",
                table: "OutcomeSurveyResponses",
                column: "SurveyId");

            migrationBuilder.CreateIndex(
                name: "IX_OutcomeSurveyComments_TenantId_SurveyId_CourseId",
                table: "OutcomeSurveyComments",
                columns: new[] { "TenantId", "SurveyId", "CourseId" });

            migrationBuilder.CreateIndex(
                name: "IX_OutcomeSurveyCompletions_TenantId_SurveyId_CourseId_Student~",
                table: "OutcomeSurveyCompletions",
                columns: new[] { "TenantId", "SurveyId", "CourseId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutcomeSurveyCourses_TenantId_SurveyId_CourseId",
                table: "OutcomeSurveyCourses",
                columns: new[] { "TenantId", "SurveyId", "CourseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutcomeSurveyQuestions_TenantId_SurveyId_SortOrder",
                table: "OutcomeSurveyQuestions",
                columns: new[] { "TenantId", "SurveyId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutcomeSurveyComments");

            migrationBuilder.DropTable(
                name: "OutcomeSurveyCompletions");

            migrationBuilder.DropTable(
                name: "OutcomeSurveyCourses");

            migrationBuilder.DropTable(
                name: "OutcomeSurveyQuestions");

            migrationBuilder.DropTable(
                name: "OutcomeSurveys");

            migrationBuilder.DropIndex(
                name: "IX_OutcomeSurveyResponses_SurveyId",
                table: "OutcomeSurveyResponses");

            migrationBuilder.DropColumn(
                name: "SurveyId",
                table: "OutcomeSurveyResponses");
        }
    }
}
