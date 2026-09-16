using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingModuleTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnonymousToken",
                table: "WellnessCheckIns",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CheckInDate",
                table: "WellnessCheckIns",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<int>(
                name: "WellbeingScore",
                table: "WellnessCheckIns",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "MenuItemId",
                table: "WasteLogs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "QuantityWasted",
                table: "WasteLogs",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "VoterTokens",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<bool>(
                name: "HasVoted",
                table: "VoterTokens",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TokenHash",
                table: "VoterTokens",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "VotedAt",
                table: "VoterTokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EscalationReason",
                table: "VoiceSessions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSlowMode",
                table: "VoiceSessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UserRole",
                table: "VoiceSessions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "WasEscalated",
                table: "VoiceSessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AvailabilityWindowsCsv",
                table: "VisitingFacultyProfiles",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cnic",
                table: "VisitingFacultyProfiles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DegreeTitle",
                table: "VisitingFacultyProfiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DocumentsVerified",
                table: "VisitingFacultyProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "VisitingFacultyProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HecEligibilityStatus",
                table: "VisitingFacultyProfiles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Institution",
                table: "VisitingFacultyProfiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "VisitingFacultyProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PerCreditHourRate",
                table: "VisitingFacultyProfiles",
                type: "numeric(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfessionalCertificationsCsv",
                table: "VisitingFacultyProfiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfileStatus",
                table: "VisitingFacultyProfiles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "ReferencesChecked",
                table: "VisitingFacultyProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ResearchPublicationsCsv",
                table: "VisitingFacultyProfiles",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpecializationsCsv",
                table: "VisitingFacultyProfiles",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerdictForRehire",
                table: "VisitingFacultyProfiles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "YearsOfIndustryExperience",
                table: "VisitingFacultyProfiles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "YearsOfTeachingExperience",
                table: "VisitingFacultyProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "MoraSubmissions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "MoraReferenceNumber",
                table: "MoraSubmissions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SemesterId",
                table: "MoraSubmissions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "MoraSubmissions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SubmissionType",
                table: "MoraSubmissions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AllergensCsv",
                table: "MenuItems",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "MenuItems",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "IngredientsCsv",
                table: "MenuItems",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHalalCertified",
                table: "MenuItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ItemName",
                table: "MenuItems",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AssignedAt",
                table: "MentalHealthTickets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConcernDescription",
                table: "MentalHealthTickets",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CrisisProtocolsTriggered",
                table: "MentalHealthTickets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FirstResponseAt",
                table: "MentalHealthTickets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MandatoryReportTriggered",
                table: "MentalHealthTickets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PreferredLanguage",
                table: "MentalHealthTickets",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolutionType",
                table: "MentalHealthTickets",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ResolvedAt",
                table: "MentalHealthTickets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "MentalHealthTickets",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "MealPlans",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "EligibleItemsCsv",
                table: "MealPlans",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlanName",
                table: "MealPlans",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "LeaderboardEntries",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<bool>(
                name: "IsAnonymized",
                table: "LeaderboardEntries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsOptedOut",
                table: "LeaderboardEntries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Metric",
                table: "LeaderboardEntries",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "SemesterId",
                table: "LeaderboardEntries",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "Elections",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "ElectionType",
                table: "Elections",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ResultsPublishedAt",
                table: "Elections",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TurnoutRate",
                table: "Elections",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CalculatedAt",
                table: "ElectionResults",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "CandidateId",
                table: "ElectionResults",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "IsElected",
                table: "ElectionResults",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "PositionId",
                table: "ElectionResults",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "DeansListEntries",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "FormalLetterUrl",
                table: "DeansListEntries",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ParentNotified",
                table: "DeansListEntries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "SemesterId",
                table: "DeansListEntries",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "CounselorProfiles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "CounselorProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "CounselorProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LanguagesCsv",
                table: "CounselorProfiles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SpecializationsCsv",
                table: "CounselorProfiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BreachType",
                table: "ComplianceBreaches",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "ComplianceBreaches",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "ForeignStudentRecordId",
                table: "ComplianceBreaches",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "OccurredAt",
                table: "ComplianceBreaches",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "ResolutionType",
                table: "ComplianceBreaches",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "ComplianceBreaches",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "AcademicYearId",
                table: "CashFlowForecasts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "CashFlowForecasts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ForecastDate",
                table: "CashFlowForecasts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "SemesterId",
                table: "CashFlowForecasts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "WeeklyActualInflow",
                table: "CashFlowForecasts",
                type: "numeric(15,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WeeklyExpectedInflow",
                table: "CashFlowForecasts",
                type: "numeric(15,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "CafeteriaUnits",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "BoardAlerts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<bool>(
                name: "IsAcknowledged",
                table: "BoardAlerts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Message",
                table: "BoardAlerts",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Severity",
                table: "BoardAlerts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "BadgeDefinitionId",
                table: "AchievementRoadmaps",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NextMilestone",
                table: "AchievementRoadmaps",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ProgressPercent",
                table: "AchievementRoadmaps",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "AchievementRoadmaps",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateTable(
                name: "AccreditationAuditFindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FrameworkId = table.Column<Guid>(type: "uuid", nullable: false),
                    CriterionId = table.Column<Guid>(type: "uuid", nullable: true),
                    FindingType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CorrectiveAction = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CorrectiveStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DeadlineAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsOverdue = table.Column<bool>(type: "boolean", nullable: false),
                    ResponsibleOwner = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccreditationAuditFindings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccreditationFrameworks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FrameworkCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FrameworkName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AccreditationBody = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LastAuditDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NextAuditDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CurrentScore = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccreditationFrameworks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccreditationScoreSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FrameworkId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    Interpretation = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SnapshotDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccreditationScoreSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BadgeAwards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BadgeDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SemesterId = table.Column<Guid>(type: "uuid", nullable: true),
                    AchievementDetail = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    VerificationHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AwardedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ParentNotified = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BadgeAwards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BadgeDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BadgeName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CriteriaJson = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BadgeImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BadgeDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ballots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ElectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    EncryptedVoteData = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BallotHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CastAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ballots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BoardDecisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Background = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    OptionsCsv = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Recommendation = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ImpactAssessment = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DecidedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardDecisions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BoardingLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RouteId = table.Column<Guid>(type: "uuid", nullable: false),
                    RouteStopId = table.Column<Guid>(type: "uuid", nullable: false),
                    Direction = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BoardedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    BoardingStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardingLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BoardMeetings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ScheduledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ExecutiveSummary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PackPdfUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardMeetings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BoardResolutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResolutionNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ImplementationStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardResolutions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CafeteriaInventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CafeteriaUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    IngredientName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    QuantityOnHand = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    ReorderPoint = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    ExpiryDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CafeteriaInventory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CafeteriaTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    PaymentMethod = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    WalletDebit = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    MealPlanDebit = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    TransactionDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CafeteriaTransactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CounselingCaseNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketId = table.Column<Guid>(type: "uuid", nullable: false),
                    CounselorId = table.Column<Guid>(type: "uuid", nullable: false),
                    NoteContent = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    IsPrivateToCounselor = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CounselingCaseNotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CriterionEvidences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CriterionId = table.Column<Guid>(type: "uuid", nullable: false),
                    EvidenceTitle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    EvidenceUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EvidenceDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    EvidenceDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiryDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsStale = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CriterionEvidences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DefaultRiskAssessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SemesterId = table.Column<Guid>(type: "uuid", nullable: false),
                    DefaultProbabilityScore = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    RiskTier = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    KeyRiskDrivers = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PredictionConfidence = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    OutstandingAmount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    AverageDaysLate = table.Column<int>(type: "integer", nullable: false),
                    MaxDaysLate = table.Column<int>(type: "integer", nullable: false),
                    PartialPaymentCount = table.Column<int>(type: "integer", nullable: false),
                    BouncedPaymentCount = table.Column<int>(type: "integer", nullable: false),
                    PaymentTrendScore = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    ScholarshipCoveragePercent = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    IsHostelStudent = table.Column<bool>(type: "boolean", nullable: false),
                    AssessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefaultRiskAssessments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ElectionCandidates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ElectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PositionId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    EndorsementCount = table.Column<int>(type: "integer", nullable: false),
                    EligibilityVerified = table.Column<bool>(type: "boolean", nullable: false),
                    NominationStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CandidateStatement = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElectionCandidates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ElectionPositions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ElectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PositionName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Seats = table.Column<int>(type: "integer", nullable: false),
                    TermDuration = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElectionPositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ElectionPositions_Elections_ElectionId",
                        column: x => x.ElectionId,
                        principalTable: "Elections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EnrollmentLetters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Purpose = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PdfUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    QrVerificationCode = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IssuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IssuedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnrollmentLetters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EscalationLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VoiceSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    EscalationType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ReasonDetail = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EscalatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AssignedTo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EscalationLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EthicalAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RelatedAssessmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuditCategory = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AuditDetail = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsViolation = table.Column<bool>(type: "boolean", nullable: false),
                    LoggedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EthicalAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FacultyCompensations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EngagementId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditHours = table.Column<int>(type: "integer", nullable: false),
                    PerCreditRate = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    GrossAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    TaxDeduction = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    PaymentStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    VoucherReference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacultyCompensations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FacultyEngagements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeachingSlotId = table.Column<Guid>(type: "uuid", nullable: false),
                    FacultyProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TotalCompensation = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    StartDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ContractStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IpClause = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AttendanceCompliant = table.Column<bool>(type: "boolean", nullable: false),
                    GradeSubmissionCompliant = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacultyEngagements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FacultyEngagements_VisitingFacultyProfiles_FacultyProfileId",
                        column: x => x.FacultyProfileId,
                        principalTable: "VisitingFacultyProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FacultyPerformanceRatings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EngagementId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttendanceScore = table.Column<int>(type: "integer", nullable: false),
                    StudentFeedbackScore = table.Column<int>(type: "integer", nullable: false),
                    GradeComplianceScore = table.Column<int>(type: "integer", nullable: false),
                    OverallRating = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    DepartmentEndorsement = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RehireRecommendation = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacultyPerformanceRatings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FleetVehicles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    VehicleType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SeatingCapacity = table.Column<int>(type: "integer", nullable: false),
                    GpsDeviceId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RegistrationExpiry = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FitnessCertificateExpiry = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    InsuranceExpiry = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConditionStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IsGrounded = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FleetVehicles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ForeignStudentRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nationality = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PassportNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PassportExpiry = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    VisaType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    VisaNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    VisaExpiry = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EntryDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PermittedStayDays = table.Column<int>(type: "integer", nullable: false),
                    MoraRegistrationStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ComplianceStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForeignStudentRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GapAnalysisItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CriterionId = table.Column<Guid>(type: "uuid", nullable: false),
                    GapDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EvidenceNeeded = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ResponsibleOwner = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EstimatedEffort = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ImpactOnScore = table.Column<decimal>(type: "numeric(4,2)", nullable: false),
                    PriorityRank = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GapAnalysisItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InstitutionBenchmarks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MetricName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    InstitutionValue = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    NationalAverage = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    PeerInstitutionValue = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    PerformanceVsNational = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstitutionBenchmarks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InterventionRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SemesterId = table.Column<Guid>(type: "uuid", nullable: false),
                    InterventionType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RiskTier = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    InterventionDetails = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    InitiatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    InitiatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Outcome = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OutcomeNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    OutcomeAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    WasSuccessful = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterventionRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MandatoryReportLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketId = table.Column<Guid>(type: "uuid", nullable: false),
                    TriggerCriterion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ActionTaken = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReportedTo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ReportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MandatoryReportLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentPlanArrangements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SemesterId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    InstallmentCount = table.Column<int>(type: "integer", nullable: false),
                    InstallmentAmount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FirstInstallmentDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastInstallmentDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentPlanArrangements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PreOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ReadyAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SlotCandidateMatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeachingSlotId = table.Column<Guid>(type: "uuid", nullable: false),
                    FacultyProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchScore = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    SpecializationScore = table.Column<int>(type: "integer", nullable: false),
                    QualificationScore = table.Column<int>(type: "integer", nullable: false),
                    ExperienceScore = table.Column<int>(type: "integer", nullable: false),
                    PriorPerformanceScore = table.Column<int>(type: "integer", nullable: false),
                    CompensationScore = table.Column<int>(type: "integer", nullable: false),
                    IsAvailabilityConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    MatchRationale = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlotCandidateMatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StrategicGoals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GoalTitle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TargetValue = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    CurrentValue = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TargetDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrategicGoals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TeachingSlots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    SemesterId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditHours = table.Column<int>(type: "integer", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecializationRequired = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    StartDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompensationRate = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeachingSlots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TransportIncidents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: true),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: true),
                    IncidentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransportIncidents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TransportInspections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: false),
                    BrakesOk = table.Column<bool>(type: "boolean", nullable: false),
                    TiresOk = table.Column<bool>(type: "boolean", nullable: false),
                    LightsOk = table.Column<bool>(type: "boolean", nullable: false),
                    EmergencyExitsOk = table.Column<bool>(type: "boolean", nullable: false),
                    FirstAidKitOk = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    InspectionResult = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    InspectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransportInspections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TransportRegistrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RouteId = table.Column<Guid>(type: "uuid", nullable: false),
                    RouteStopId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeatNumber = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PaymentStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StartDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransportRegistrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VendorPerformanceScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorId = table.Column<Guid>(type: "uuid", nullable: false),
                    CafeteriaUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    HygieneScore = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    MenuAdherenceScore = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    ComplaintsScore = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    OverallRating = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EvaluatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorPerformanceScores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VisaRenewalWorkflows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ForeignStudentRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ApplicationDeadline = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RenewalOutcome = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisaRenewalWorkflows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VoiceIntents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IntentName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UrduTriggerPhrases = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EnglishTriggerPhrases = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ActionEndpoint = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PermittedRole = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoiceIntents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReadinessCriteria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FrameworkId = table.Column<Guid>(type: "uuid", nullable: false),
                    CriterionCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SectionName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Weight = table.Column<decimal>(type: "numeric(3,2)", nullable: false),
                    Score = table.Column<decimal>(type: "numeric(3,2)", nullable: false),
                    ComplianceStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EvidenceSource = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReadinessCriteria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReadinessCriteria_AccreditationFrameworks_FrameworkId",
                        column: x => x.FrameworkId,
                        principalTable: "AccreditationFrameworks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentExpiryTrackers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ForeignStudentRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExpiryDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Alert90DaysSent = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Alert60DaysSent = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Alert30DaysSent = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Alert14DaysSent = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Alert7DaysSent = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsExpired = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentExpiryTrackers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentExpiryTrackers_ForeignStudentRecords_ForeignStudent~",
                        column: x => x.ForeignStudentRecordId,
                        principalTable: "ForeignStudentRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentPlanInstallments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentPlanArrangementId = table.Column<Guid>(type: "uuid", nullable: false),
                    InstallmentNumber = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    DueDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    PaidAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentPlanInstallments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentPlanInstallments_PaymentPlanArrangements_PaymentPlan~",
                        column: x => x.PaymentPlanArrangementId,
                        principalTable: "PaymentPlanArrangements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentExpiryTrackers_ForeignStudentRecordId",
                table: "DocumentExpiryTrackers",
                column: "ForeignStudentRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_ElectionPositions_ElectionId",
                table: "ElectionPositions",
                column: "ElectionId");

            migrationBuilder.CreateIndex(
                name: "IX_FacultyEngagements_FacultyProfileId",
                table: "FacultyEngagements",
                column: "FacultyProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentPlanInstallments_PaymentPlanArrangementId",
                table: "PaymentPlanInstallments",
                column: "PaymentPlanArrangementId");

            migrationBuilder.CreateIndex(
                name: "IX_ReadinessCriteria_FrameworkId",
                table: "ReadinessCriteria",
                column: "FrameworkId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccreditationAuditFindings");

            migrationBuilder.DropTable(
                name: "AccreditationScoreSnapshots");

            migrationBuilder.DropTable(
                name: "BadgeAwards");

            migrationBuilder.DropTable(
                name: "BadgeDefinitions");

            migrationBuilder.DropTable(
                name: "Ballots");

            migrationBuilder.DropTable(
                name: "BoardDecisions");

            migrationBuilder.DropTable(
                name: "BoardingLogs");

            migrationBuilder.DropTable(
                name: "BoardMeetings");

            migrationBuilder.DropTable(
                name: "BoardResolutions");

            migrationBuilder.DropTable(
                name: "CafeteriaInventory");

            migrationBuilder.DropTable(
                name: "CafeteriaTransactions");

            migrationBuilder.DropTable(
                name: "CounselingCaseNotes");

            migrationBuilder.DropTable(
                name: "CriterionEvidences");

            migrationBuilder.DropTable(
                name: "DefaultRiskAssessments");

            migrationBuilder.DropTable(
                name: "DocumentExpiryTrackers");

            migrationBuilder.DropTable(
                name: "ElectionCandidates");

            migrationBuilder.DropTable(
                name: "ElectionPositions");

            migrationBuilder.DropTable(
                name: "EnrollmentLetters");

            migrationBuilder.DropTable(
                name: "EscalationLogs");

            migrationBuilder.DropTable(
                name: "EthicalAuditLogs");

            migrationBuilder.DropTable(
                name: "FacultyCompensations");

            migrationBuilder.DropTable(
                name: "FacultyEngagements");

            migrationBuilder.DropTable(
                name: "FacultyPerformanceRatings");

            migrationBuilder.DropTable(
                name: "FleetVehicles");

            migrationBuilder.DropTable(
                name: "GapAnalysisItems");

            migrationBuilder.DropTable(
                name: "InstitutionBenchmarks");

            migrationBuilder.DropTable(
                name: "InterventionRecords");

            migrationBuilder.DropTable(
                name: "MandatoryReportLogs");

            migrationBuilder.DropTable(
                name: "PaymentPlanInstallments");

            migrationBuilder.DropTable(
                name: "PreOrders");

            migrationBuilder.DropTable(
                name: "ReadinessCriteria");

            migrationBuilder.DropTable(
                name: "SlotCandidateMatches");

            migrationBuilder.DropTable(
                name: "StrategicGoals");

            migrationBuilder.DropTable(
                name: "TeachingSlots");

            migrationBuilder.DropTable(
                name: "TransportIncidents");

            migrationBuilder.DropTable(
                name: "TransportInspections");

            migrationBuilder.DropTable(
                name: "TransportRegistrations");

            migrationBuilder.DropTable(
                name: "VendorPerformanceScores");

            migrationBuilder.DropTable(
                name: "VisaRenewalWorkflows");

            migrationBuilder.DropTable(
                name: "VoiceIntents");

            migrationBuilder.DropTable(
                name: "ForeignStudentRecords");

            migrationBuilder.DropTable(
                name: "PaymentPlanArrangements");

            migrationBuilder.DropTable(
                name: "AccreditationFrameworks");

            migrationBuilder.DropColumn(
                name: "AnonymousToken",
                table: "WellnessCheckIns");

            migrationBuilder.DropColumn(
                name: "CheckInDate",
                table: "WellnessCheckIns");

            migrationBuilder.DropColumn(
                name: "WellbeingScore",
                table: "WellnessCheckIns");

            migrationBuilder.DropColumn(
                name: "MenuItemId",
                table: "WasteLogs");

            migrationBuilder.DropColumn(
                name: "QuantityWasted",
                table: "WasteLogs");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "VoterTokens");

            migrationBuilder.DropColumn(
                name: "HasVoted",
                table: "VoterTokens");

            migrationBuilder.DropColumn(
                name: "TokenHash",
                table: "VoterTokens");

            migrationBuilder.DropColumn(
                name: "VotedAt",
                table: "VoterTokens");

            migrationBuilder.DropColumn(
                name: "EscalationReason",
                table: "VoiceSessions");

            migrationBuilder.DropColumn(
                name: "IsSlowMode",
                table: "VoiceSessions");

            migrationBuilder.DropColumn(
                name: "UserRole",
                table: "VoiceSessions");

            migrationBuilder.DropColumn(
                name: "WasEscalated",
                table: "VoiceSessions");

            migrationBuilder.DropColumn(
                name: "AvailabilityWindowsCsv",
                table: "VisitingFacultyProfiles");

            migrationBuilder.DropColumn(
                name: "Cnic",
                table: "VisitingFacultyProfiles");

            migrationBuilder.DropColumn(
                name: "DegreeTitle",
                table: "VisitingFacultyProfiles");

            migrationBuilder.DropColumn(
                name: "DocumentsVerified",
                table: "VisitingFacultyProfiles");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "VisitingFacultyProfiles");

            migrationBuilder.DropColumn(
                name: "HecEligibilityStatus",
                table: "VisitingFacultyProfiles");

            migrationBuilder.DropColumn(
                name: "Institution",
                table: "VisitingFacultyProfiles");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "VisitingFacultyProfiles");

            migrationBuilder.DropColumn(
                name: "PerCreditHourRate",
                table: "VisitingFacultyProfiles");

            migrationBuilder.DropColumn(
                name: "ProfessionalCertificationsCsv",
                table: "VisitingFacultyProfiles");

            migrationBuilder.DropColumn(
                name: "ProfileStatus",
                table: "VisitingFacultyProfiles");

            migrationBuilder.DropColumn(
                name: "ReferencesChecked",
                table: "VisitingFacultyProfiles");

            migrationBuilder.DropColumn(
                name: "ResearchPublicationsCsv",
                table: "VisitingFacultyProfiles");

            migrationBuilder.DropColumn(
                name: "SpecializationsCsv",
                table: "VisitingFacultyProfiles");

            migrationBuilder.DropColumn(
                name: "VerdictForRehire",
                table: "VisitingFacultyProfiles");

            migrationBuilder.DropColumn(
                name: "YearsOfIndustryExperience",
                table: "VisitingFacultyProfiles");

            migrationBuilder.DropColumn(
                name: "YearsOfTeachingExperience",
                table: "VisitingFacultyProfiles");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "MoraSubmissions");

            migrationBuilder.DropColumn(
                name: "MoraReferenceNumber",
                table: "MoraSubmissions");

            migrationBuilder.DropColumn(
                name: "SemesterId",
                table: "MoraSubmissions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "MoraSubmissions");

            migrationBuilder.DropColumn(
                name: "SubmissionType",
                table: "MoraSubmissions");

            migrationBuilder.DropColumn(
                name: "AllergensCsv",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "IngredientsCsv",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "IsHalalCertified",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "ItemName",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "AssignedAt",
                table: "MentalHealthTickets");

            migrationBuilder.DropColumn(
                name: "ConcernDescription",
                table: "MentalHealthTickets");

            migrationBuilder.DropColumn(
                name: "CrisisProtocolsTriggered",
                table: "MentalHealthTickets");

            migrationBuilder.DropColumn(
                name: "FirstResponseAt",
                table: "MentalHealthTickets");

            migrationBuilder.DropColumn(
                name: "MandatoryReportTriggered",
                table: "MentalHealthTickets");

            migrationBuilder.DropColumn(
                name: "PreferredLanguage",
                table: "MentalHealthTickets");

            migrationBuilder.DropColumn(
                name: "ResolutionType",
                table: "MentalHealthTickets");

            migrationBuilder.DropColumn(
                name: "ResolvedAt",
                table: "MentalHealthTickets");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "MentalHealthTickets");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "MealPlans");

            migrationBuilder.DropColumn(
                name: "EligibleItemsCsv",
                table: "MealPlans");

            migrationBuilder.DropColumn(
                name: "PlanName",
                table: "MealPlans");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "LeaderboardEntries");

            migrationBuilder.DropColumn(
                name: "IsAnonymized",
                table: "LeaderboardEntries");

            migrationBuilder.DropColumn(
                name: "IsOptedOut",
                table: "LeaderboardEntries");

            migrationBuilder.DropColumn(
                name: "Metric",
                table: "LeaderboardEntries");

            migrationBuilder.DropColumn(
                name: "SemesterId",
                table: "LeaderboardEntries");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Elections");

            migrationBuilder.DropColumn(
                name: "ElectionType",
                table: "Elections");

            migrationBuilder.DropColumn(
                name: "ResultsPublishedAt",
                table: "Elections");

            migrationBuilder.DropColumn(
                name: "TurnoutRate",
                table: "Elections");

            migrationBuilder.DropColumn(
                name: "CalculatedAt",
                table: "ElectionResults");

            migrationBuilder.DropColumn(
                name: "CandidateId",
                table: "ElectionResults");

            migrationBuilder.DropColumn(
                name: "IsElected",
                table: "ElectionResults");

            migrationBuilder.DropColumn(
                name: "PositionId",
                table: "ElectionResults");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "DeansListEntries");

            migrationBuilder.DropColumn(
                name: "FormalLetterUrl",
                table: "DeansListEntries");

            migrationBuilder.DropColumn(
                name: "ParentNotified",
                table: "DeansListEntries");

            migrationBuilder.DropColumn(
                name: "SemesterId",
                table: "DeansListEntries");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "CounselorProfiles");

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "CounselorProfiles");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "CounselorProfiles");

            migrationBuilder.DropColumn(
                name: "LanguagesCsv",
                table: "CounselorProfiles");

            migrationBuilder.DropColumn(
                name: "SpecializationsCsv",
                table: "CounselorProfiles");

            migrationBuilder.DropColumn(
                name: "BreachType",
                table: "ComplianceBreaches");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ComplianceBreaches");

            migrationBuilder.DropColumn(
                name: "ForeignStudentRecordId",
                table: "ComplianceBreaches");

            migrationBuilder.DropColumn(
                name: "OccurredAt",
                table: "ComplianceBreaches");

            migrationBuilder.DropColumn(
                name: "ResolutionType",
                table: "ComplianceBreaches");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ComplianceBreaches");

            migrationBuilder.DropColumn(
                name: "AcademicYearId",
                table: "CashFlowForecasts");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "CashFlowForecasts");

            migrationBuilder.DropColumn(
                name: "ForecastDate",
                table: "CashFlowForecasts");

            migrationBuilder.DropColumn(
                name: "SemesterId",
                table: "CashFlowForecasts");

            migrationBuilder.DropColumn(
                name: "WeeklyActualInflow",
                table: "CashFlowForecasts");

            migrationBuilder.DropColumn(
                name: "WeeklyExpectedInflow",
                table: "CashFlowForecasts");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "CafeteriaUnits");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "BoardAlerts");

            migrationBuilder.DropColumn(
                name: "IsAcknowledged",
                table: "BoardAlerts");

            migrationBuilder.DropColumn(
                name: "Message",
                table: "BoardAlerts");

            migrationBuilder.DropColumn(
                name: "Severity",
                table: "BoardAlerts");

            migrationBuilder.DropColumn(
                name: "BadgeDefinitionId",
                table: "AchievementRoadmaps");

            migrationBuilder.DropColumn(
                name: "NextMilestone",
                table: "AchievementRoadmaps");

            migrationBuilder.DropColumn(
                name: "ProgressPercent",
                table: "AchievementRoadmaps");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AchievementRoadmaps");
        }
    }
}
