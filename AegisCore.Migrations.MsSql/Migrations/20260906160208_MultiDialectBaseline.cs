using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AegisCore.Migrations.MsSql.Migrations
{
    /// <inheritdoc />
    public partial class MultiDialectBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AcademicYears",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicYears", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccessAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceIp = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RequestPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StatusCode = table.Column<int>(type: "int", nullable: false),
                    IsBlocked = table.Column<bool>(type: "bit", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccreditationAuditFindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FrameworkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CriterionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FindingType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CorrectiveAction = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CorrectiveStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DeadlineAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsOverdue = table.Column<bool>(type: "bit", nullable: false),
                    ResponsibleOwner = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccreditationAuditFindings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccreditationBodies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccreditationBodies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccreditationCriterionMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccreditationCriterionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProgramOutcomeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MappingNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccreditationCriterionMappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccreditationFrameworks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FrameworkCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FrameworkName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AccreditationBody = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LastAuditDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    NextAuditDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CurrentScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccreditationFrameworks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccreditationReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AccreditationBody = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccreditationReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccreditationScoreSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FrameworkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Interpretation = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SnapshotDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccreditationScoreSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AchievementBadges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Tier = table.Column<int>(type: "int", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CriteriaJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchievementBadges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AchievementRoadmaps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BadgeDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NextMilestone = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ProgressPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchievementRoadmaps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ActionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommitteeMeetingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssignedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ActivityParticipations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActivityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParticipationRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ParticipationStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PointsEarned = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityParticipations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdmissionApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnquiryLeadId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StudentName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Cnic = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ProgramId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuotaType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MatricPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    FscPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    EntryTestScore = table.Column<int>(type: "int", nullable: true),
                    IbccEquivalenceApplied = table.Column<bool>(type: "bit", nullable: false),
                    ApplicationStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionApplications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdmissionLeads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProgramInterest = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionLeads", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdvisorAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FacultyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdvisorAssignments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgenticGradeRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SuggestedMarks = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Confidence = table.Column<decimal>(type: "decimal(4,3)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AgentTraceJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgenticGradeRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AIFeedback",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Domain = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PromptId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    IsThumbsUp = table.Column<bool>(type: "bit", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIFeedback", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AIModels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ContextWindow = table.Column<int>(type: "int", nullable: false),
                    CostPerInputToken = table.Column<decimal>(type: "decimal(12,6)", nullable: false),
                    CostPerOutputToken = table.Column<decimal>(type: "decimal(12,6)", nullable: false),
                    Capabilities = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIModels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AIProviders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProviderType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BaseUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DefaultModel = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsFallbackOnly = table.Column<bool>(type: "bit", nullable: false),
                    TimeoutSeconds = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIProviders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AITokenUsage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Domain = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    InputTokens = table.Column<int>(type: "int", nullable: false),
                    OutputTokens = table.Column<int>(type: "int", nullable: false),
                    LatencyMs = table.Column<int>(type: "int", nullable: false),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AITokenUsage", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AlertRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RuleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TriggerType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Threshold = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AlumniDonations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlumniProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DonationAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DonationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DonationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ReceiptNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlumniDonations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AlumniProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GraduationYear = table.Column<int>(type: "int", nullable: false),
                    AlumniId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CurrentCompany = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CurrentDesignation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CurrentLocation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Bio = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlumniProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AlumniReferrals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlumniUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlumniReferrals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AnonymousFeedbackSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UniversityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FacultyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    SentimentScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    EncryptedPayload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ThemesCsv = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnonymousFeedbackSubmissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApiEndpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Method = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Module = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AuthRequired = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RegisteredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiEndpoints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApiKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    KeyHash = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    KeyPrefix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Tier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Permission = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AllowedDomains = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OwnerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OwnerEmail = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    RateLimitPerMinute = table.Column<int>(type: "int", nullable: false),
                    RateLimitPerDay = table.Column<int>(type: "int", nullable: false),
                    TotalRequests = table.Column<long>(type: "bigint", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevocationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppealCommitteeBriefs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppealId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BriefContent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EvidenceMatrixJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecommendedOptions = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppealCommitteeBriefs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppealEvidence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppealId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DocumentType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Relevance = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppealEvidence", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentSchemes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SemesterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamControllerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentSchemes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssetCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssetDisposals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Method = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SaleValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DisposalDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetDisposals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssetMaintenances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Cost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ScheduledDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetMaintenances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssetTransfers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransferredByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransferredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetTransfers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AttainmentThresholds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProgramOutcomeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Threshold = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttainmentThresholds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Method = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CheckIn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CheckOut = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ResourceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ResourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BackupConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BackupType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CronExpression = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    RetentionDaysHot = table.Column<int>(type: "int", nullable: false),
                    RetentionDaysCold = table.Column<int>(type: "int", nullable: false),
                    EncryptBackup = table.Column<bool>(type: "bit", nullable: false),
                    StoragePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DatabaseName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    VerifyAfterBackup = table.Column<bool>(type: "bit", nullable: false),
                    LastRunAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextRunAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackupConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BadgeAwards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BadgeDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SemesterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AchievementDetail = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VerificationHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AwardedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ParentNotified = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BadgeAwards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BadgeDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BadgeName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CriteriaJson = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BadgeImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BadgeDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ballots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ElectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EncryptedVoteData = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BallotHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CastAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ballots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BenchmarkComparisons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FactorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    InstitutionValue = table.Column<double>(type: "float", nullable: false),
                    NationalAverage = table.Column<double>(type: "float", nullable: false),
                    PeerAverage = table.Column<double>(type: "float", nullable: false),
                    PeerInstitutionName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsAboveAverage = table.Column<bool>(type: "bit", nullable: false),
                    AcademicYear = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenchmarkComparisons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BenchmarkRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScenarioName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Tier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequestsPerSecond = table.Column<double>(type: "float", nullable: true),
                    P95LatencyMs = table.Column<double>(type: "float", nullable: true),
                    ErrorRate = table.Column<double>(type: "float", nullable: true),
                    ResultsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenchmarkRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BoardAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RecommendedAction = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ResponsibleOfficer = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsAcknowledged = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardAlerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BoardDecisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Background = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OptionsCsv = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Recommendation = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ImpactAssessment = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DecidedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardDecisions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BoardingLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteStopId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BoardedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    BoardingStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardingLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BoardMeetingPacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MeetingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExecutiveSummary = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    AgendaItemsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PendingDecisionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PreviousResolutionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PdfUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDistributed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardMeetingPacks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BoardMeetings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ScheduledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ExecutiveSummary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PackPdfUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardMeetings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BoardResolutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResolutionNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ImplementationStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardResolutions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BookIssues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LibraryMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookCopyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IssueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReturnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FineAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookIssues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BookOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LibraryVendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BookReservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LibraryMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LibraryBookId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReservedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookReservations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BusAttendances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransportRouteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttendanceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsPresent = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusAttendances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CafeteriaInventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CafeteriaUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IngredientName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    QuantityOnHand = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    ReorderPoint = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    ExpiryDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CafeteriaInventory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CafeteriaTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    WalletDebit = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    MealPlanDebit = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    TransactionDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CafeteriaTransactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CafeteriaUnits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    OperatingHours = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CafeteriaUnits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CallLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdmissionLeadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Disposition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DurationSeconds = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CallDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CallLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CampusDrives",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DriveType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScheduledDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampusDrives", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Campuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CampusCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CalendarType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GradingScheme = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timezone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    IsMain = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CampusPolygons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Vertices = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampusPolygons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CandidateNominations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ElectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PositionName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    EndorsementCount = table.Column<int>(type: "int", nullable: false),
                    RequiredEndorsements = table.Column<int>(type: "int", nullable: false),
                    IsEligible = table.Column<bool>(type: "bit", nullable: false),
                    IneligibilityReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateNominations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CareerEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EventDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CareerGoals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Goal = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerGoals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CashFlowForecasts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SemesterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpectedCollection = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    ProjectedShortfall = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    RecoveryRateEstimate = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    ForecastWeek = table.Column<int>(type: "int", nullable: false),
                    WeeklyExpectedInflow = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    WeeklyActualInflow = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    ForecastDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashFlowForecasts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CatalogCourseRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DegreeProgram = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DegreeProgramSlug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SemesterSequence = table.Column<int>(type: "int", nullable: false),
                    SemesterLabel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CourseCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CourseCodeNormalised = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CourseTitle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    CourseTitleNormalised = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreditHoursTheory = table.Column<int>(type: "int", nullable: false),
                    CreditHoursLab = table.Column<int>(type: "int", nullable: false),
                    CreditHoursTotal = table.Column<int>(type: "int", nullable: false),
                    PrerequisitesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CourseType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ElectiveSlot = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AnnotationsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExtractionConfidence = table.Column<double>(type: "float", nullable: false),
                    RequiresReview = table.Column<bool>(type: "bit", nullable: false),
                    ReviewReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsUpdateCandidate = table.Column<bool>(type: "bit", nullable: false),
                    IsInserted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogCourseRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CatalogVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DegreeProgramId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IngestionJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangeLogJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChangeImpactSimulations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    ChangedElementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DirectlyAffectedCourses = table.Column<int>(type: "int", nullable: false),
                    IndirectlyAffectedCourses = table.Column<int>(type: "int", nullable: false),
                    AffectedStudents = table.Column<int>(type: "int", nullable: false),
                    InvalidRegistrations = table.Column<int>(type: "int", nullable: false),
                    RippleEffectJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SimulatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangeImpactSimulations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChartConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    DataSourceKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ConfigJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InsightI18nKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChartConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChartOfAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AccountName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CashFlowCategory = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AccountType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParentAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChartOfAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChatbotConversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeadId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatbotConversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CisUploads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FacultyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CisFilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploadDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ProcessingStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ExtractedData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HumanReviewStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReviewedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CisUploads", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Classes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseOfferingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SectionNumber = table.Column<int>(type: "int", nullable: false),
                    SectionLabel = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AcademicYear = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Semester = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PrimaryFacultyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CoFacultyIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EnrolledStudentIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    EnrolledCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    GenerationJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Classes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClassGenerationJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcademicYear = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Semester = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TotalOfferings = table.Column<int>(type: "int", nullable: false),
                    SectionsGenerated = table.Column<int>(type: "int", nullable: false),
                    StudentsDistributed = table.Column<int>(type: "int", nullable: false),
                    FacultyAssigned = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TriggeredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassGenerationJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CLOAssessmentMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseLearningOutcomeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessmentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Weightage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CLOAssessmentMappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CLOAttainments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseLearningOutcomeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttainmentLevel = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Semester = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CLOAttainments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ComplianceBreaches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ForeignStudentRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BreachType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ResolutionType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplianceBreaches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ComplianceFrameworks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Version = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplianceFrameworks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConferenceTravels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConferenceName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TravelDateFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TravelDateTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedBudget = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FacultyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConferenceTravels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConfigurationEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Domain = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ValueJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SchemaUrl = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Environment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CurrentVersion = table.Column<int>(type: "int", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigurationEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConsentLedgers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataCategory = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ConsentedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConsentText = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    IpAddress = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsentLedgers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContinuousImprovementPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContinuousImprovementPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConvocationAttendance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConvocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CheckInTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GuestCount = table.Column<int>(type: "int", nullable: false),
                    CertificateCollected = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConvocationAttendance", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConvocationEligibility",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConvocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    BlockingReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreditsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    CgpaMet = table.Column<bool>(type: "bit", nullable: false),
                    DuesCleared = table.Column<bool>(type: "bit", nullable: false),
                    ThesisApproved = table.Column<bool>(type: "bit", nullable: false),
                    LibraryCleared = table.Column<bool>(type: "bit", nullable: false),
                    DisciplinaryCleared = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConvocationEligibility", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Convocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AcademicYear = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CeremonyDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Venue = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    GuestTicketsPerStudent = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Convocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CostCenters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BudgetAllocation = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostCenters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CounselingCaseNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CounselorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NoteContent = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: true),
                    IsPrivateToCounselor = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CounselingCaseNotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CounselorProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SpecializationsCsv = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LanguagesCsv = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsOnCall = table.Column<bool>(type: "bit", nullable: false),
                    CurrentCaseload = table.Column<int>(type: "int", nullable: false),
                    MaxCaseload = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CounselorProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CountryPacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CountryCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DefaultLanguage = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TextDirection = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DefaultCurrency = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Timezone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DefaultCalendarType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DefaultGradingScheme = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsInstalled = table.Column<bool>(type: "bit", nullable: false),
                    InstalledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CountryPacks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CourseEquivalencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceCourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetCourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceInstitution = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseEquivalencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CourseGradePolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GradingScheme = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PassingGrade = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    GpaWeight = table.Column<double>(type: "float", nullable: false),
                    CanRepeat = table.Column<bool>(type: "bit", nullable: false),
                    MaxRepeatAttempts = table.Column<int>(type: "int", nullable: true),
                    GradeReplacement = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseGradePolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CourseGrades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SemesterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalMarksObtained = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PercentageObtained = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    LetterGrade = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    GradePoints = table.Column<decimal>(type: "decimal(3,2)", nullable: false),
                    IsPassing = table.Column<bool>(type: "bit", nullable: false),
                    ResultStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CalculatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseGrades", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CourseLearningOutcomes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BloomsLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseLearningOutcomes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CourseOfferings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcademicYear = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Semester = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SectionNumber = table.Column<int>(type: "int", nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    EnrolledCount = table.Column<int>(type: "int", nullable: false),
                    CampusId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseOfferings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CourseReEnrollments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceSemesterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetSemesterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FeeWaived = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseReEnrollments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CriterionEvidences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CriterionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvidenceTitle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    EvidenceUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EvidenceDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    EvidenceDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiryDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsStale = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CriterionEvidences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Currencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DecimalPlaces = table.Column<int>(type: "int", nullable: false),
                    DecimalSeparator = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    ThousandsSeparator = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    ExchangeRateToBase = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    ExchangeRateUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsBaseCurrency = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CurriculumChangeHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurriculumVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangeType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BeforeValueJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AfterValueJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumChangeHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CurriculumProposals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProposedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumProposals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CurriculumVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProgramId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    YearEffective = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Changelog = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Dashboards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DashboardType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CampusId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dashboards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataImportJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConnectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SourceQueryJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FieldMappingsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalRows = table.Column<int>(type: "int", nullable: false),
                    SuccessRows = table.Column<int>(type: "int", nullable: false),
                    ErrorRows = table.Column<int>(type: "int", nullable: false),
                    SkippedRows = table.Column<int>(type: "int", nullable: false),
                    ErrorLogJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TriggeredByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataImportJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataWarehouseTables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TableName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SourceModule = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataWarehouseTables", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeansListEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SemesterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Cgpa = table.Column<decimal>(type: "decimal(4,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ParentNotified = table.Column<bool>(type: "bit", nullable: false),
                    FormalLetterUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeansListEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DecisionTables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Domain = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InputColumnsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OutputColumnsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RowsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DecisionTables", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DefaulterInterventions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiskScoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    TriggeredByTier = table.Column<int>(type: "int", nullable: false),
                    MessageContent = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    InitiatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Outcome = table.Column<int>(type: "int", nullable: true),
                    IsSuccessful = table.Column<bool>(type: "bit", nullable: false),
                    OutcomeNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefaulterInterventions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DefaulterRiskScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Semester = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ProbabilityScore = table.Column<double>(type: "float", nullable: false),
                    Tier = table.Column<int>(type: "int", nullable: false),
                    RiskDriversJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PredictionConfidence = table.Column<double>(type: "float", nullable: false),
                    PaymentBehaviorJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AcademicCorrelationJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FinancialProfileJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PredictedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefaulterRiskScores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DefaultRiskAssessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SemesterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefaultProbabilityScore = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    RiskTier = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    KeyRiskDrivers = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PredictionConfidence = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    OutstandingAmount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    AverageDaysLate = table.Column<int>(type: "int", nullable: false),
                    MaxDaysLate = table.Column<int>(type: "int", nullable: false),
                    PartialPaymentCount = table.Column<int>(type: "int", nullable: false),
                    BouncedPaymentCount = table.Column<int>(type: "int", nullable: false),
                    PaymentTrendScore = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    ScholarshipCoveragePercent = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    IsHostelStudent = table.Column<bool>(type: "bit", nullable: false),
                    AssessedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefaultRiskAssessments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DegreeCertificates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConvocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DegreeTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AttestationStatus = table.Column<int>(type: "int", nullable: false),
                    HECReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DegreeCertificates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DegreePrograms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DegreeType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DurationYears = table.Column<int>(type: "int", nullable: false),
                    MinCreditHours = table.Column<int>(type: "int", nullable: false),
                    MinCGPA = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CurriculumVersion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DegreePrograms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DegreeTrees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DegreeProgramId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CatalogVersion = table.Column<int>(type: "int", nullable: false),
                    NodesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EdgesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CriticalPathJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SemesterLanesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ComputedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DegreeTrees", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DegreeVerifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VerificationToken = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestedBy = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    EmployerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DegreeVerifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DemandForecasts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CafeteriaUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ForecastDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PredictedQuantity = table.Column<int>(type: "int", nullable: false),
                    ActualQuantity = table.Column<int>(type: "int", nullable: false),
                    Accuracy = table.Column<double>(type: "float", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandForecasts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DiagnosticFindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Recommendation = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FoundAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiagnosticFindings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DirectorySyncLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<int>(type: "int", nullable: false),
                    UsersSynced = table.Column<int>(type: "int", nullable: false),
                    UsersCreated = table.Column<int>(type: "int", nullable: false),
                    UsersUpdated = table.Column<int>(type: "int", nullable: false),
                    Errors = table.Column<int>(type: "int", nullable: false),
                    ErrorDetailsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DirectorySyncLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DistributionAnomalyAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DistributionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnomalyType = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    RecommendedAction = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DistributionAnomalyAlerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentAccessLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccessType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AccessedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AccessedByRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    WasDenied = table.Column<bool>(type: "bit", nullable: false),
                    DenialReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AccessedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentAccessLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentRetentionPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentCategory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TierLevel = table.Column<int>(type: "int", nullable: false),
                    RetentionYears = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsOverride = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentRetentionPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RawHtml = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExtractedPalette = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExtractedFonts = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LogoUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentTiers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TierLevel = table.Column<int>(type: "int", nullable: false),
                    RetentionYears = table.Column<int>(type: "int", nullable: false),
                    RetentionDays = table.Column<int>(type: "int", nullable: false),
                    ObjectLockMode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BucketName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StoragePrefix = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RequirePqcVerification = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTiers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentVaultEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    DocumentCategory = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TierLevel = table.Column<int>(type: "int", nullable: false),
                    PqcSignatureBase64 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PqcPublicKeyBase64 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Sha512Hash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IsPqcVerified = table.Column<bool>(type: "bit", nullable: false),
                    PqcVerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StorageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    StorageBucket = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    StorageKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RetentionExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRetentionLocked = table.Column<bool>(type: "bit", nullable: false),
                    LockMode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LockedUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ObjectLockLegalHoldId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadedByRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    VerifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentVaultEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DomainEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AggregateId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CausationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SourceModule = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DomainEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DriverIncidents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransportDriverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IncidentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverIncidents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ElectionCandidates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ElectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PositionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EndorsementCount = table.Column<int>(type: "int", nullable: false),
                    EligibilityVerified = table.Column<bool>(type: "bit", nullable: false),
                    NominationStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CandidateStatement = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElectionCandidates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ElectionResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ElectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PositionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoteCount = table.Column<int>(type: "int", nullable: false),
                    IsElected = table.Column<bool>(type: "bit", nullable: false),
                    CalculatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElectionResults", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Elections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ElectionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VotingMethod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NominationStart = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    NominationEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CampaigningStart = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CampaigningEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    VotingStart = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    VotingEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ResultsPublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TurnoutRate = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Elections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ElectiveSlots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DegreeProgramId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SlotLabel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SlotCategory = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SemesterSequence = table.Column<int>(type: "int", nullable: false),
                    CreditHoursTheory = table.Column<int>(type: "int", nullable: false),
                    CreditHoursLab = table.Column<int>(type: "int", nullable: false),
                    EligibleCourseIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MinGradeRequired = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    IsStudentChoice = table.Column<bool>(type: "bit", nullable: false),
                    RequiresApproval = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElectiveSlots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmergencyAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlertMessage = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SentByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientCount = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmergencyAlerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmergencyContacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContactName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AlternatePhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmergencyContacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EmployeeCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    EmployeeType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Designation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateOfJoining = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateOfLeaving = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Salary = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Employers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EncryptedBallots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ElectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoterTokenHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EncryptedVoteData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    VerificationHash = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CastAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EncryptedBallots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EnrollmentLetters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PdfUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    QrVerificationCode = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IssuedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IssuedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnrollmentLetters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ErasureRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeadlineAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ErasedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false, defaultValue: "Pending"),
                    FailureReason = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErasureRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EscalationLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoiceSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EscalationType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ReasonDetail = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EscalatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AssignedTo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EscalationLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EthicalAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RelatedAssessmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AuditCategory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AuditDetail = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsViolation = table.Column<bool>(type: "bit", nullable: false),
                    LoggedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EthicalAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EthicsProtocols",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubmittedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewComments = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EthicsProtocols", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ETLJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastRunAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RecordsProcessed = table.Column<int>(type: "int", nullable: false),
                    CronExpression = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ETLJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EventSchemaVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    SchemaJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventSchemaVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EvidenceFreshnessLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvidenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DaysSinceUpdate = table.Column<int>(type: "int", nullable: true),
                    NotificationMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CheckedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvidenceFreshnessLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExamPaperBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaperCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PreparedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamPaperBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExamPaperScans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QrPayload = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    PageCount = table.Column<int>(type: "int", nullable: false),
                    BlurDetected = table.Column<bool>(type: "bit", nullable: false),
                    BlankDetected = table.Column<bool>(type: "bit", nullable: false),
                    OcrText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScannedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamPaperScans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExamRegistrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExamTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    ExamHall = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SeatNumber = table.Column<int>(type: "int", nullable: true),
                    AttendancePercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    IsAttendanceEligible = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamRegistrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExchangeRateSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BaseCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    QuoteCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Rate = table.Column<decimal>(type: "numeric(24,12)", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ValidForDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PulledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeRateSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExecutiveDashboards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserRole = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FiscalYear = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AcademicExcellenceScore = table.Column<double>(type: "float", nullable: false),
                    FinancialStabilityScore = table.Column<double>(type: "float", nullable: false),
                    AccreditationScore = table.Column<double>(type: "float", nullable: false),
                    StudentSuccessScore = table.Column<double>(type: "float", nullable: false),
                    FacultyQualityScore = table.Column<double>(type: "float", nullable: false),
                    CompositeRankingScore = table.Column<double>(type: "float", nullable: false),
                    KPIsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StrategicGoalStatusesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutiveDashboards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExportLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Domain = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Format = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RowCount = table.Column<int>(type: "int", nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExportedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExportedByName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsEncrypted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExportLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Extensions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PricingModel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IconUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ScreenshotsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ManifestJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeveloperName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DeveloperEmail = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    DeveloperWebsite = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PackageChecksum = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PackageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DependenciesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequiredPermissionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DownloadCount = table.Column<long>(type: "bigint", nullable: false),
                    AverageRating = table.Column<double>(type: "float", nullable: false),
                    ReviewCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Extensions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExternalDbConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DbType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ConnectionString = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Schema = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastTestedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastTestSuccessful = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalDbConnections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FacultyCompensations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EngagementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreditHours = table.Column<int>(type: "int", nullable: false),
                    PerCreditRate = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    GrossAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    TaxDeduction = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PaymentStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    VoucherReference = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacultyCompensations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FacultyCVs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FacultyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CvJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacultyCVs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FacultyPerformanceRatings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EngagementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttendanceScore = table.Column<int>(type: "int", nullable: false),
                    StudentFeedbackScore = table.Column<int>(type: "int", nullable: false),
                    GradeComplianceScore = table.Column<int>(type: "int", nullable: false),
                    OverallRating = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    DepartmentEndorsement = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RehireRecommendation = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacultyPerformanceRatings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FacultySlotMatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OpenSlotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FacultyProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompositeScore = table.Column<double>(type: "float", nullable: false),
                    Rank = table.Column<int>(type: "int", nullable: false),
                    MatchRationaleJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsSelected = table.Column<bool>(type: "bit", nullable: false),
                    MatchedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacultySlotMatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FacultyWorkloads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FacultyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HoursPerWeek = table.Column<int>(type: "int", nullable: false),
                    StudentCount = table.Column<int>(type: "int", nullable: false),
                    Schedule = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacultyWorkloads", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FeatureFlags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FlagType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    Module = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TenantOverridesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RulesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActiveFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureFlags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FeeDefaulterRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AmountDue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Level = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NotificationsSent = table.Column<int>(type: "int", nullable: false),
                    LastNotifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RTPAttemptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RTPAttempted = table.Column<bool>(type: "bit", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeeDefaulterRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FeeDemands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DemandNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    OutstandingAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeeDemands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FeeStructures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProgramId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Semester = table.Column<int>(type: "int", nullable: false),
                    FeeType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TotalFee = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeeStructures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FerpaAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserRole = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ResourceType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ResourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AccessedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    Justified = table.Column<bool>(type: "bit", nullable: false),
                    UserErased = table.Column<bool>(type: "bit", nullable: false),
                    StudentErased = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FerpaAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FieldMappingTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SourceDbType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MappingJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsBuiltIn = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldMappingTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceAccountingStandardPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StandardCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PolicyCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ValueJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceAccountingStandardPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceAssetAccountingEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PreviousCarryingValue = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    NewCarryingValue = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Proceeds = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceAssetAccountingEvents", x => x.Id);
                    table.CheckConstraint("CK_FinanceAssetAccountingEvents_Values", "[PreviousCarryingValue] >= 0 AND [NewCarryingValue] >= 0 AND [Proceeds] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "FinanceAuditEvidencePackages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuditorEngagementId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Framework = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PeriodFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PqcAlgorithm = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PqcKeyId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PqcSignature = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    PqcPublicKey = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    GeneratedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceAuditEvidencePackages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceAuditorEngagements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuditorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AuditorEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ScopesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceAuditorEngagements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceAuditSamples",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuditorEngagementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PopulationType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PeriodFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PopulationCount = table.Column<int>(type: "int", nullable: false),
                    SampleSize = table.Column<int>(type: "int", nullable: false),
                    SeedHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SelectedReferencesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceAuditSamples", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceAuditSeals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EventCount = table.Column<int>(type: "int", nullable: false),
                    Algorithm = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    KeyId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MerkleRoot = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    Signature = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    PublicKey = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    SealedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceAuditSeals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceBankAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    AccountReference = table.Column<string>(type: "nvarchar(34)", maxLength: 34, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    ChartOfAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceBankAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceConsolidationGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    PresentationCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceConsolidationGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceConsolidationRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConsolidationGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceConsolidationRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceControlEvaluationBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Trigger = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ReferenceType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ControlCount = table.Column<int>(type: "int", nullable: false),
                    PassedCount = table.Column<int>(type: "int", nullable: false),
                    FlaggedCount = table.Column<int>(type: "int", nullable: false),
                    ResultsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EvaluatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceControlEvaluationBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceEliminationRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConsolidationGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CounterpartAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceEliminationRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceFleetCostPostings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransportVehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CostCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CostType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Litres = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Odometer = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CostDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    JournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceFleetCostPostings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceFleetProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransportVehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CostCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustodianUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Make = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ModelYear = table.Column<int>(type: "int", nullable: false),
                    FuelType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ServiceIntervalDays = table.Column<int>(type: "int", nullable: false),
                    ServiceIntervalKm = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    CurrentOdometer = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    LastServiceAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextServiceAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceFleetProfiles", x => x.Id);
                    table.CheckConstraint("CK_FinanceFleetProfiles_Service", "[ModelYear] >= 1900 AND [ServiceIntervalDays] > 0 AND [ServiceIntervalKm] > 0 AND [CurrentOdometer] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "FinanceFleetTrips",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransportVehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CostCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DistanceKm = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FromLocation = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    ToLocation = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    TripDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceFleetTrips", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceForeignCurrencyTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ForeignAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    ForeignCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    FunctionalAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    FunctionalCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    RateUsed = table.Column<decimal>(type: "numeric(24,12)", nullable: false),
                    RateSource = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsSettled = table.Column<bool>(type: "bit", nullable: false),
                    SettledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RealizedFxGainLoss = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceForeignCurrencyTransactions", x => x.Id);
                    table.CheckConstraint("CK_FinanceForeignCurrencyTransactions_CurrenciesDiffer", "[ForeignCurrency] <> [FunctionalCurrency]");
                });

            migrationBuilder.CreateTable(
                name: "FinanceGrants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DonorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    AwardAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    ReceivedAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    SpentAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceGrants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceGratuityAssumptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DiscountRatePercent = table.Column<decimal>(type: "numeric(9,6)", nullable: false),
                    SalaryGrowthRatePercent = table.Column<decimal>(type: "numeric(9,6)", nullable: false),
                    TurnoverRatePercent = table.Column<decimal>(type: "numeric(9,6)", nullable: false),
                    BenefitDivisor = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    RetirementHorizonYears = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceGratuityAssumptions", x => x.Id);
                    table.CheckConstraint("CK_FinanceGratuityAssumptions_Rates", "[DiscountRatePercent] >= 0 AND [SalaryGrowthRatePercent] >= 0 AND [TurnoverRatePercent] >= 0 AND [BenefitDivisor] > 0 AND [RetirementHorizonYears] > 0");
                });

            migrationBuilder.CreateTable(
                name: "FinanceGratuityValuations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssumptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ValuationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DefinedBenefitObligation = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    CurrentServiceCost = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    InterestCost = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    JournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceGratuityValuations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceInsurancePolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Insurer = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PolicyNumber = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PolicyType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    CoverageAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Premium = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RenewalDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AmortizedThrough = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceInsurancePolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceInventoryItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemCode = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CostMethod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OnHandQuantity = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    WeightedAverageCost = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    ReorderLevel = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    ReorderQuantity = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceInventoryItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceInvestmentBenchmarks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ObservationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AnnualReturnPercent = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceInvestmentBenchmarks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceInvestmentFunds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    FundType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Principal = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    CurrentValue = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    SpendingRatePercent = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    ReserveTarget = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceInvestmentFunds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceLedgerSignatures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Algorithm = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    KeyId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Signature = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    PublicKey = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    SignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceLedgerSignatures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceLibraryAcquisitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LibraryBookId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResourceType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Cost = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    FairValue = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    AccumulatedAmortization = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    UsefulLifeMonths = table.Column<int>(type: "int", nullable: false),
                    AcquiredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubscriptionEndsAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AccountingTreatment = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    JournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceLibraryAcquisitions", x => x.Id);
                    table.CheckConstraint("CK_FinanceLibraryAcquisitions_NonNegativeAmounts", "[Cost] >= 0 AND [FairValue] >= 0 AND [AccumulatedAmortization] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "FinanceLibraryFines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LibraryBookId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FineType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AssessedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    JournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceLibraryFines", x => x.Id);
                    table.CheckConstraint("CK_FinanceLibraryFines_PositiveAmount", "[Amount] > 0");
                });

            migrationBuilder.CreateTable(
                name: "FinancePaymentPlanConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    SurchargePercent = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    MinimumDownPaymentPercent = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    MaximumDuration = table.Column<int>(type: "int", nullable: false),
                    LatePenaltyAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    LatePenaltyPercent = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    MaximumMissedInstallments = table.Column<int>(type: "int", nullable: false),
                    RequiresApproval = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancePaymentPlanConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinancePayrollBankInstructions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EncryptedAccountReference = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    RoutingCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AccountName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancePayrollBankInstructions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinancePayrollPaymentFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayrollPostingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Format = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Sha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PqcAlgorithm = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PqcKeyId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PqcSignature = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    RecordCount = table.Column<int>(type: "int", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GeneratedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancePayrollPaymentFiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinancePayrollPostings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Period = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GrossSalary = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    NetSalary = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    IncomeTax = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Eobi = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    SocialSecurity = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Gratuity = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    JournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ImportedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancePayrollPostings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinancePettyCashFunds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustodianUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    FloatAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    CurrentBalance = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancePettyCashFunds", x => x.Id);
                    table.CheckConstraint("CK_FinancePettyCashFunds_PositiveFloat", "[FloatAmount] > 0 AND [CurrentBalance] >= 0 AND [CurrentBalance] <= [FloatAmount]");
                });

            migrationBuilder.CreateTable(
                name: "FinancePqcKeyCeremonies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Algorithm = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    RequiredApprovals = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActivatedKeyId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancePqcKeyCeremonies", x => x.Id);
                    table.CheckConstraint("CK_FinancePqcKeyCeremonies_Threshold", "[RequiredApprovals] BETWEEN 2 AND 20");
                });

            migrationBuilder.CreateTable(
                name: "FinancePqcKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Algorithm = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    KeyId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SecretNamespace = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    PublicKey = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancePqcKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceRegulatorySchemas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Jurisdiction = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Version = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Format = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    RootElement = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NamespaceUri = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DefinitionJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceRegulatorySchemas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceRegulatorySubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RegulatorySchemaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodLabel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PqcAlgorithm = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PqcKeyId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PqcSignature = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    PqcPublicKey = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    GeneratedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceRegulatorySubmissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceReportArtifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Format = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    PeriodLabel = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PqcAlgorithm = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PqcKeyId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PqcSignature = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    PqcPublicKey = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GeneratedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceReportArtifacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceReportingEntities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    FunctionalCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    ParentEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceReportingEntities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceReportProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    TargetStandard = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FunderName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    FundFilter = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PeriodType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReportCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    RateMethod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Schedule = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Delivery = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Format = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ComparativeYears = table.Column<int>(type: "int", nullable: false),
                    BudgetVsActual = table.Column<bool>(type: "bit", nullable: false),
                    RestrictedFundUtilisation = table.Column<bool>(type: "bit", nullable: false),
                    GrantDrawdown = table.Column<bool>(type: "bit", nullable: false),
                    PqcEnabled = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceReportProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceRestatementRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceStandard = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TargetStandard = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SourceAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Factor = table.Column<decimal>(type: "numeric(19,8)", nullable: false),
                    Rationale = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceRestatementRules", x => x.Id);
                    table.CheckConstraint("CK_FinanceRestatementRules_Factor", "[Factor] <> 0 AND [SourceAccountId] <> [TargetAccountId]");
                });

            migrationBuilder.CreateTable(
                name: "FinanceRestatementRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceStandard = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TargetStandard = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PeriodFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceRestatementRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceSecurityDeposits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepositType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    OriginalAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    RefundedAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    ForfeitedAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    RefundSlaDays = table.Column<int>(type: "int", nullable: false),
                    CollectedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RefundRequestedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AssessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolutionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceSecurityDeposits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceSodRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IncompatibleActionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceSodRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceTaxRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Jurisdiction = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    VendorCategory = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FilerRatePercent = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    NonFilerMultiplier = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceTaxRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceThreeWayMatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorInvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseOrderTotal = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    ReceivedTotal = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    InvoiceTotal = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Variance = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MatchedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MatchedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceThreeWayMatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceUtilityBills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UtilityType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    AmountDue = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    UnitsConsumed = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    BillingPeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BillingPeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    JournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceUtilityBills", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceVendors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LegalName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TradeName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Tier = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    TaxId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    InvoiceCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    PaymentTerms = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Iban = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    BankName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Swift = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    RaastId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SanctionsStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SanctionsReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RelationshipNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PerformanceScore = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    LastSanctionsScreenedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceVendors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceVoucherApprovalRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    MinimumAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    MaximumAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: true),
                    RequiredRolesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MinimumApprovals = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceVoucherApprovalRules", x => x.Id);
                    table.CheckConstraint("CK_FinanceVoucherApprovalRules_Range", "[MinimumAmount] >= 0 AND ([MaximumAmount] IS NULL OR [MaximumAmount] > [MinimumAmount]) AND [MinimumApprovals] > 0");
                });

            migrationBuilder.CreateTable(
                name: "FinanceVoucherAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinanceVoucherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Sha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceVoucherAttachments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceVoucherReversals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalVoucherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReversalVoucherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceVoucherReversals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceVouchers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoucherNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    VoucherType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TotalAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PostedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PostedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PqcAlgorithm = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    PqcKeyId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PqcSignature = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    PqcPublicKey = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceVouchers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceVoucherSequences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoucherType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FiscalYear = table.Column<int>(type: "int", nullable: false),
                    NextNumber = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceVoucherSequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceWithholdingRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TaxRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TaxType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FilingPeriod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PaymentReference = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    GrossAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    AppliedRatePercent = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    WithheldAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    FilerStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    JournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WithheldAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceWithholdingRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinePayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookIssueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinePayments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FiscalYears",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalYears", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FleetVehicles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VehicleType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SeatingCapacity = table.Column<int>(type: "int", nullable: false),
                    GpsDeviceId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RegistrationExpiry = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FitnessCertificateExpiry = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    InsuranceExpiry = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ConditionStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsGrounded = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FleetVehicles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FoodOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CafeteriaUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TransactionRef = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsPreOrder = table.Column<bool>(type: "bit", nullable: false),
                    EstimatedReadyAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoodOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ForeignStudentRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nationality = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PassportNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PassportExpiry = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    VisaType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VisaNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    VisaExpiry = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EntryDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PermittedStayDays = table.Column<int>(type: "int", nullable: false),
                    MoraRegistrationStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ComplianceStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForeignStudentRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ForeignStudentRegistries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nationality = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PassportNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PassportExpiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VisaType = table.Column<int>(type: "int", nullable: false),
                    VisaNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    VisaExpiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EntryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MoraStatus = table.Column<int>(type: "int", nullable: false),
                    MoraRegistrationNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Compliance = table.Column<int>(type: "int", nullable: false),
                    Program = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    YearOfStudy = table.Column<int>(type: "int", nullable: false),
                    ExpectedGraduation = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForeignStudentRegistries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FormDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FieldsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PagesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AllowMultipleSubmissions = table.Column<bool>(type: "bit", nullable: false),
                    AllowDraftSave = table.Column<bool>(type: "bit", nullable: false),
                    OpenAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CloseAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AccessRoles = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    WorkflowDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsTemplate = table.Column<bool>(type: "bit", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FreezeDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FreezeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DocumentType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    VerificationNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FreezeDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FuelLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransportVehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Litres = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Cost = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    FuelDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuelLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GapAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProgramOutcomeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Gap = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Action = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GapAnalyses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GapAnalysisItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CriterionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GapDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EvidenceNeeded = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ResponsibleOwner = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EstimatedEffort = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ImpactOnScore = table.Column<decimal>(type: "decimal(4,2)", nullable: false),
                    PriorityRank = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GapAnalysisItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GateLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LogTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GateLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GradeAppeals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Semester = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SubmittedGrade = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    AppealGrounds = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    GroundType = table.Column<int>(type: "int", nullable: false),
                    GroundStrength = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    FacultyRespondentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommitteeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CommitteeDecision = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AdjustedGrade = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    SubmissionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Deadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PolicyClauseCitation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsEligible = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeAppeals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GradeDistributions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Semester = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FacultyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Mean = table.Column<double>(type: "float", nullable: false),
                    Median = table.Column<double>(type: "float", nullable: false),
                    Mode = table.Column<double>(type: "float", nullable: true),
                    StdDev = table.Column<double>(type: "float", nullable: false),
                    Variance = table.Column<double>(type: "float", nullable: false),
                    Skewness = table.Column<double>(type: "float", nullable: false),
                    Kurtosis = table.Column<double>(type: "float", nullable: false),
                    PassRate = table.Column<double>(type: "float", nullable: false),
                    DistinctionRate = table.Column<double>(type: "float", nullable: false),
                    FailureRate = table.Column<double>(type: "float", nullable: false),
                    EnrollmentCount = table.Column<int>(type: "int", nullable: false),
                    CurveShape = table.Column<int>(type: "int", nullable: false),
                    LetterDistributionJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PercentileBreakdownJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AnomaliesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HistoricalComparisonJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeDistributions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GradeOverrideRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UniversityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SemesterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalGrade = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    OriginalMarks = table.Column<int>(type: "int", nullable: false),
                    OriginalGpa = table.Column<decimal>(type: "decimal(3,2)", nullable: false),
                    OverrideGrade = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    OverrideMarks = table.Column<int>(type: "int", nullable: false),
                    OverrideGpa = table.Column<decimal>(type: "decimal(3,2)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Justification = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    RequestedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovalStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ApprovalThreshold = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeOverrideRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GradeScales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UniversityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeScales", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GrievanceTickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SubmitterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AssignedTo = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrievanceTickets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuardrailRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Domain = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Pattern = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuardrailRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuardrailViolations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Domain = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    InputPreview = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    OutputPreview = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    ActionTaken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuardrailViolations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HardcodingFindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    CodeSnippet = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Recommendation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    FoundAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HardcodingFindings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HealthScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OverallScore = table.Column<double>(type: "float", nullable: false),
                    PerformanceScore = table.Column<double>(type: "float", nullable: false),
                    SecurityScore = table.Column<double>(type: "float", nullable: false),
                    AvailabilityScore = table.Column<double>(type: "float", nullable: false),
                    ComplianceScore = table.Column<double>(type: "float", nullable: false),
                    StorageScore = table.Column<double>(type: "float", nullable: false),
                    DatabaseScore = table.Column<double>(type: "float", nullable: false),
                    IntegrationScore = table.Column<double>(type: "float", nullable: false),
                    Grade = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TopIssuesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthScores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HostelBlocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BlockType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalFloors = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HostelBlocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HostelDisciplines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IncidentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HostelDisciplines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HumanGradeReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GradeRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FinalMarks = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HumanGradeReviews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Format = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Domain = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ColumnMappingsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalRows = table.Column<int>(type: "int", nullable: false),
                    SuccessCount = table.Column<int>(type: "int", nullable: false),
                    ErrorCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErrorReportJson = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsRolledBack = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IngestionJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OriginalDocumentUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RawExtraction = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExtractionConfidence = table.Column<double>(type: "float", nullable: false),
                    ClassificationLogJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReviewLogJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InsertionLogJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErrorLogJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CoursesExtracted = table.Column<int>(type: "int", nullable: false),
                    CoursesInserted = table.Column<int>(type: "int", nullable: false),
                    CoursesUpdated = table.Column<int>(type: "int", nullable: false),
                    CoursesSkipped = table.Column<int>(type: "int", nullable: false),
                    CoursesRequiringReview = table.Column<int>(type: "int", nullable: false),
                    Warnings = table.Column<int>(type: "int", nullable: false),
                    Errors = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngestionJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InsightChips",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChartConfigurationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    I18nKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ComputedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsightChips", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InstitutionBenchmarks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MetricName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    InstitutionValue = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    NationalAverage = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PeerInstitutionValue = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    PerformanceVsNational = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstitutionBenchmarks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationIncidents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntegrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    Resolution = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationIncidents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationRegistrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Endpoint = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CheckIntervalSeconds = table.Column<int>(type: "int", nullable: false),
                    LastCheckedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSuccessAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastFailureAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UptimePercentage = table.Column<double>(type: "float", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationRegistrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InterventionRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SemesterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InterventionType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RiskTier = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    InterventionDetails = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    InitiatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InitiatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OutcomeNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OutcomeAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    WasSuccessful = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterventionRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Interventions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Interventions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InterviewSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewSchedules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InterviewSlots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobPostingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewSlots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InventoryAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    TriggeredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryAlerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InventoryItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CafeteriaUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    QuantityOnHand = table.Column<double>(type: "float", nullable: false),
                    ReorderPoint = table.Column<double>(type: "float", nullable: false),
                    ReorderQuantity = table.Column<double>(type: "float", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IoTSensors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HardwareId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SensorType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BuildingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    LocationDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ConfigJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastReadingAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InstalledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IoTSensors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobPostings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    EmployerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PostedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobPostings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JournalEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntryNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntryType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EntryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalDebit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCredit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsPosted = table.Column<bool>(type: "bit", nullable: false),
                    IsSandbox = table.Column<bool>(type: "bit", nullable: false),
                    PostedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PostedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PreviousHash = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    CanonicalPayload = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: true),
                    EntryHash = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LabInventories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LabId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    LastVerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabInventories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeaderboardEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SemesterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Metric = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Score = table.Column<decimal>(type: "decimal(4,2)", nullable: false),
                    Rank = table.Column<int>(type: "int", nullable: false),
                    IsAnonymized = table.Column<bool>(type: "bit", nullable: false),
                    IsOptedOut = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaderboardEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeadTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdmissionLeadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssignedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadTasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeaveApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeaveType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveApplications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LibraryBooks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Author = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Isbn = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Publisher = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PublicationYear = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TotalCopies = table.Column<int>(type: "int", nullable: false),
                    AvailableCopies = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryBooks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LibraryBudgets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FiscalYear = table.Column<int>(type: "int", nullable: false),
                    TotalBudget = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SpentAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryBudgets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LibraryMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MembershipExpiry = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaxBooksAllowed = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryMembers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LibraryUsages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LibraryMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntryTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExitTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryUsages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LibraryVendors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Contact = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibraryVendors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LicenseHeartbeats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LicenseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstanceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ActiveUsers = table.Column<int>(type: "int", nullable: false),
                    HeartbeatAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LicenseHeartbeats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Licenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CustomerEmail = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    LicenseKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Edition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaxUsers = table.Column<int>(type: "int", nullable: false),
                    MaxCampuses = table.Column<int>(type: "int", nullable: false),
                    FeatureEntitlementsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevocationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Licenses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LicenseStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LicenseKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UniversityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UniversityName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LicenseJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActivatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastHeartbeatAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastHeartbeatStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FailedHeartbeatCount = table.Column<int>(type: "int", nullable: false),
                    LockedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OverrideBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OverrideReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OverrideUntil = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LicenseStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LmsConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    CoexistenceMode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RetryAttempts = table.Column<int>(type: "int", nullable: false),
                    RetryDelayMs = table.Column<int>(type: "int", nullable: false),
                    SyncSchedule = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ConflictResolution = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ConflictAlertThreshold = table.Column<int>(type: "int", nullable: false),
                    ProviderConfigJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LmsConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LmsProvisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<int>(type: "int", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ExternalUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    LastSyncAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProvisionedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorLogJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LmsProvisions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LmsRosterSyncLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProvisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    MembersAdded = table.Column<int>(type: "int", nullable: false),
                    MembersRemoved = table.Column<int>(type: "int", nullable: false),
                    ConflictsDetected = table.Column<int>(type: "int", nullable: false),
                    ConflictDetailsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LmsRosterSyncLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LmsSyncLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SyncType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SyncStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LmsSyncLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoginAuditRecords",
                columns: table => new
                {
                    AuditId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserEmail = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    UserRole = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IpCountry = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IpCity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    GpsLatitude = table.Column<double>(type: "float", nullable: true),
                    GpsLongitude = table.Column<double>(type: "float", nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DeviceType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OperatingSystem = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Browser = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsSuccessful = table.Column<bool>(type: "bit", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MicrosoftOid = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginAuditRecords", x => x.AuditId);
                });

            migrationBuilder.CreateTable(
                name: "LostFoundItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FoundDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FoundLocation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhotoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClaimedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HandedOverByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LostFoundItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceContractors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContactPerson = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Specialization = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceContractors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MandatoryReportLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TriggerCriterion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ActionTaken = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReportedTo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReportedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MandatoryReportLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MandatoryReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Trigger = table.Column<int>(type: "int", nullable: false),
                    ReportContent = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AuthorityNotified = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ReportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MandatoryReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MarketingCampaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CampaignType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Budget = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketingCampaigns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MarkModerations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SemesterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SubmittedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReviewedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReviewComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarkModerations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MealPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DailyAllowance = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    EligibleItemsCsv = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MedicalVisits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VisitDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VisitReasonEncrypted = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    MedicalOfficerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DiagnosisEncrypted = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    TreatmentEncrypted = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    RestDays = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalVisits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MentalHealthTickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnonymousToken = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ConcernType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ConcernDescription = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    PreferredLanguage = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AssignedCounselorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FirstResponseAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ResolutionType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CrisisProtocolsTriggered = table.Column<bool>(type: "bit", nullable: false),
                    MandatoryReportTriggered = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MentalHealthTickets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MentorAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MentorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MentorAssignments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MenuItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CafeteriaUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    IngredientsCsv = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AllergensCsv = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Calories = table.Column<int>(type: "int", nullable: false),
                    IsHalalCertified = table.Column<bool>(type: "bit", nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MeritLists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProgramId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SemesterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuotaType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RoundNumber = table.Column<int>(type: "int", nullable: false),
                    TotalSeats = table.Column<int>(type: "int", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    GeneratedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeritLists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MessAttendances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MealType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AttendanceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessAttendances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MessFeedbacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessFeedbacks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MessMenus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MealType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Items = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MenuDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessMenus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MisconductCases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    Severity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReportedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HearingDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Sanction = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Resolution = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MisconductCases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MisconductEvidence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MisconductEvidence", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MoraSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SemesterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    MoraReferenceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ResponseNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MoraSubmissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NoticeBoardItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: false),
                    PostedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Visibility = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PostedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoticeBoardItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationDeliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Recipient = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Module = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OpenedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationDeliveries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EnabledChannelsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DoNotDisturbFrom = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DoNotDisturbTo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationPreferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: false),
                    Module = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LanguageCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OcrProviderConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ApiEndpoint = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CredentialRef = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OcrProviderConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OfficialModules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfficialModules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OfflineAttendanceEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientEventId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MarkedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    WasOfflineAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SyncedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfflineAttendanceEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaperDestructionManifests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaperCount = table.Column<int>(type: "int", nullable: false),
                    ManifestHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WitnessedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DestroyedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaperDestructionManifests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentGatewayConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GatewaySlug = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Environment = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    SecretNamespace = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    HealthStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LastHealthCheckAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HealthError = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ConfiguredByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConfiguredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentGatewayConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentGatewayEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GatewaySlug = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ReferenceId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FinancePaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(19,4)", nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsSandbox = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SanitizedPayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentGatewayEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentPlanArrangements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SemesterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    InstallmentCount = table.Column<int>(type: "int", nullable: false),
                    InstallmentAmount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FirstInstallmentDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastInstallmentDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentPlanArrangements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayrollRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Month = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TotalEmployees = table.Column<int>(type: "int", nullable: false),
                    TotalSalary = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    RunDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RunBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PerformanceReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReviewPeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewPeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OverallRating = table.Column<int>(type: "int", nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceReviews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Resource = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Module = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PersonalityConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    IsLockedOff = table.Column<bool>(type: "bit", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActiveForUser = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalityConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlacementRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Salary = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlacementRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlagiarismApiSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<int>(type: "int", nullable: false),
                    ExternalSubmissionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RawSimilarity = table.Column<double>(type: "float", nullable: true),
                    AdjustedSimilarity = table.Column<double>(type: "float", nullable: true),
                    Classification = table.Column<int>(type: "int", nullable: true),
                    SourceBreakdownJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FacultySummary = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    StudentSummary = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: true),
                    ExcludedSourcesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErrorLogJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlagiarismApiSubmissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlagiarismScans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Semester = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OverallSimilarity = table.Column<double>(type: "float", nullable: false),
                    Tier = table.Column<int>(type: "int", nullable: false),
                    VerbatimMatchesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParaphraseMatchesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StructuralMatchesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StyleDeviationJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CollusionGraphJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AiGenerationFlagsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InternetMatchesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CaseRecommendation = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RequiresReview = table.Column<bool>(type: "bit", nullable: false),
                    ScannedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlagiarismScans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlatformMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MetricName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformMetrics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "POAttainments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProgramOutcomeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttainmentLevel = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_POAttainments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PolicyConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Module = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Key = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ValueType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EffectiveTo = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PowerBiReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PowerBiReportId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PowerBiWorkspaceId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DatasetId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EmbedUrl = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: false),
                    TargetRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PowerBiReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PreOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReadyAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PrerequisiteConflicts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    SourceCourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetCourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TriggerEvent = table.Column<int>(type: "int", nullable: true),
                    AffectedStudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ResolutionSuggestion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DetectedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrerequisiteConflicts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PreviousVisitFindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccreditationCycleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FindingType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FindingSeverity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreviousVisitFindings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProctorEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ScreenshotUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProctorEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProctoringSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Mode = table.Column<int>(type: "int", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    LockdownEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CameraRequired = table.Column<bool>(type: "bit", nullable: false),
                    MicRequired = table.Column<bool>(type: "bit", nullable: false),
                    Sensitivity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ScheduledStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProctoringSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProcurementRequisitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequisitionNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RequestingDepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequisitionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RequiredByDate = table.Column<DateOnly>(type: "date", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcurementRequisitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProfilePictureRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    FaceValidated = table.Column<bool>(type: "bit", nullable: false),
                    FaceConfidence = table.Column<double>(type: "float", nullable: true),
                    EditingDetected = table.Column<bool>(type: "bit", nullable: false),
                    EditingProbability = table.Column<double>(type: "float", nullable: true),
                    FullStorageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ThumbnailStorageUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    StorageKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastValidatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Sha512Hash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfilePictureRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProgramEducationalObjectives",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ProgramId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramEducationalObjectives", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProgramOutcomes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ProgramId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramOutcomes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PromptABTests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PromptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionA = table.Column<int>(type: "int", nullable: false),
                    VersionB = table.Column<int>(type: "int", nullable: false),
                    TrafficSplitA = table.Column<double>(type: "float", nullable: false),
                    ResponsesA = table.Column<int>(type: "int", nullable: false),
                    ResponsesB = table.Column<int>(type: "int", nullable: false),
                    ThumbsUpA = table.Column<int>(type: "int", nullable: false),
                    ThumbsUpB = table.Column<int>(type: "int", nullable: false),
                    AvgLatencyMsA = table.Column<double>(type: "float", nullable: false),
                    AvgLatencyMsB = table.Column<double>(type: "float", nullable: false),
                    AvgTokensA = table.Column<double>(type: "float", nullable: false),
                    AvgTokensB = table.Column<double>(type: "float", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromptABTests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Prompts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Domain = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModelTarget = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CurrentVersion = table.Column<int>(type: "int", nullable: false),
                    ApprovalStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActiveVersion = table.Column<int>(type: "int", nullable: false),
                    SystemPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserPromptTemplate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VariablesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModelParametersJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prompts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PromptVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PromptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    SystemPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserPromptTemplate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangeDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromptVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PoNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RequisitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PoDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DeliveryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QueueDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CurrentDepth = table.Column<int>(type: "int", nullable: false),
                    ConsumerCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    JobsPerMinute = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueueDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QueueJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QueueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    EnqueuedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueueJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReadinessAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsAcknowledged = table.Column<bool>(type: "bit", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TriggeredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReadinessAlerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReadinessGaps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CriterionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GapDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EvidenceNeeded = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ResponsibleOwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Effort = table.Column<int>(type: "int", nullable: false),
                    ImpactOnScore = table.Column<double>(type: "float", nullable: false),
                    PriorityRank = table.Column<int>(type: "int", nullable: false),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReadinessGaps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReadinessScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FrameworkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FrameworkName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OverallScore = table.Column<double>(type: "float", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CategoryScoresJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CriterionScoresJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScoreHistoryJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NextAuditDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DaysUntilAudit = table.Column<int>(type: "int", nullable: false),
                    ComplianceVelocity = table.Column<double>(type: "float", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReadinessScores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReadReplicaConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConnectionString = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaxLagSeconds = table.Column<int>(type: "int", nullable: false),
                    CurrentLagSeconds = table.Column<int>(type: "int", nullable: true),
                    Weight = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LastHealthCheckAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReadReplicaConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegistrationEnrollments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WindowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsWaitlisted = table.Column<bool>(type: "bit", nullable: false),
                    WaitlistPosition = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationEnrollments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegistrationWindows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Semester = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OpensAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosesAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaxCoursesPerStudent = table.Column<int>(type: "int", nullable: false),
                    RequiresAdvisorApproval = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationWindows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReleaseDownloads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DownloadUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Checksum = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReleaseNotes = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    ReleasedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReleaseDownloads", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReplicaHealthLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReplicaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsHealthy = table.Column<bool>(type: "bit", nullable: false),
                    LagSeconds = table.Column<int>(type: "int", nullable: true),
                    ActiveConnections = table.Column<int>(type: "int", nullable: true),
                    QueriesPerSecond = table.Column<double>(type: "float", nullable: true),
                    LagThresholdBreached = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CheckedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplicaHealthLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReplicationLagAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReplicaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LagSeconds = table.Column<int>(type: "int", nullable: false),
                    ThresholdSeconds = table.Column<int>(type: "int", nullable: false),
                    IsCritical = table.Column<bool>(type: "bit", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TriggeredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplicationLagAlerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReplicationScaleEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PreviousReplicaCount = table.Column<int>(type: "int", nullable: false),
                    NewReplicaCount = table.Column<int>(type: "int", nullable: false),
                    TriggerReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TriggeredBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    WasSuccessful = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TriggeredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplicationScaleEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReplicationScalingConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MinReplicas = table.Column<int>(type: "int", nullable: false),
                    MaxReplicas = table.Column<int>(type: "int", nullable: false),
                    TargetReplicas = table.Column<int>(type: "int", nullable: false),
                    ScaleUpThresholdConnections = table.Column<int>(type: "int", nullable: false),
                    ScaleUpThresholdQueriesPerSecond = table.Column<int>(type: "int", nullable: false),
                    ScaleDownThresholdConnections = table.Column<int>(type: "int", nullable: false),
                    ScaleDownCooldownMinutes = table.Column<int>(type: "int", nullable: false),
                    DefaultMaxLagSeconds = table.Column<int>(type: "int", nullable: false),
                    CriticalLagSeconds = table.Column<int>(type: "int", nullable: false),
                    AutoScalingEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AutoFailoverEnabled = table.Column<bool>(type: "bit", nullable: false),
                    ReadRoutingEnabled = table.Column<bool>(type: "bit", nullable: false),
                    ReadDistributionStrategy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplicationScalingConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BIReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Frequency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Format = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Recipients = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportSchedules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResearchGrants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    FundingAgency = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApplicationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AwardDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchGrants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResearchPatents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    PatentNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FiledDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GrantedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchPatents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResearchProjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    PrincipalInvestigatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Budget = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchProjects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResearchPublications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    JournalName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PublishedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Doi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchPublications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoommatePreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreferredStudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsMutual = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoommatePreferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rubrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    CourseLearningOutcomeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rubrics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RuleDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Domain = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MatchStrategy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    CurrentVersion = table.Column<int>(type: "int", nullable: false),
                    ConditionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SamlConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IdpSsoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IdpSloUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IdpCertificate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SpEntityId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SpAcsUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AttributeMappingJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SamlConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScalingEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReplicaName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TotalReplicas = table.Column<int>(type: "int", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScalingEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScheduledJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Module = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CronExpression = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    TimeoutSeconds = table.Column<int>(type: "int", nullable: false),
                    MaxRetries = table.Column<int>(type: "int", nullable: false),
                    ParametersJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastRunAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextRunAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SchoolVisits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VisitDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssignedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolVisits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SeatAllocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MeritListEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeatType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AllocatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeatAllocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SectionOverrideRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromSectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToSectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentRegularCount = table.Column<int>(type: "int", nullable: false),
                    CurrentExamCount = table.Column<int>(type: "int", nullable: false),
                    CapacityAllowed = table.Column<bool>(type: "bit", nullable: false),
                    FeeAdjustment = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RequestedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SectionOverrideRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SemesterFreezes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    ReasonCategory = table.Column<int>(type: "int", nullable: false),
                    ReasonDescription = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CurrentSemester = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TotalSemesters = table.Column<int>(type: "int", nullable: false),
                    TimingRule = table.Column<int>(type: "int", nullable: false),
                    RefundAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OutstandingBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FreezesUsed = table.Column<int>(type: "int", nullable: false),
                    MaxFreezeSemesters = table.Column<int>(type: "int", nullable: false),
                    AcademicContinuityPlanJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EnrolledCoursesSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpectedReturnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReturnStatus = table.Column<int>(type: "int", nullable: true),
                    IsInternational = table.Column<bool>(type: "bit", nullable: false),
                    HasScholarship = table.Column<bool>(type: "bit", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SemesterFreezes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceDependencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DependencyType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceDependencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceRegistrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Runtime = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Port = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HealthEndpoint = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RegisteredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastHeartbeatAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceRegistrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SickLeaveApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ToDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MedicalVisitId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SickLeaveApplications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SkillGaps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkillName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CurrentLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TargetLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillGaps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SLADefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ResponseTimeHours = table.Column<int>(type: "int", nullable: false),
                    ResolutionTimeHours = table.Column<int>(type: "int", nullable: false),
                    EscalationTimeHours = table.Column<int>(type: "int", nullable: false),
                    EscalationLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SLADefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SlotCandidateMatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeachingSlotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FacultyProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MatchScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    SpecializationScore = table.Column<int>(type: "int", nullable: false),
                    QualificationScore = table.Column<int>(type: "int", nullable: false),
                    ExperienceScore = table.Column<int>(type: "int", nullable: false),
                    PriorPerformanceScore = table.Column<int>(type: "int", nullable: false),
                    CompensationScore = table.Column<int>(type: "int", nullable: false),
                    IsAvailabilityConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    MatchRationale = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Rank = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlotCandidateMatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SlowQueries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QueryText = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
                    DurationMs = table.Column<double>(type: "float", nullable: false),
                    SourceModule = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Frequency = table.Column<int>(type: "int", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlowQueries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SocietyMemberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SocietyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    JoinedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocietyMemberships", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SsoSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AccessTokenEncrypted = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RefreshTokenEncrypted = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TokenExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DeviceName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SsoSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StrategicGoals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GoalTitle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TargetValue = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    CurrentValue = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TargetDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrategicGoals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StrategicPlanGoals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CurrentValue = table.Column<double>(type: "float", nullable: false),
                    TargetValue = table.Column<double>(type: "float", nullable: false),
                    PercentAchieved = table.Column<double>(type: "float", nullable: false),
                    TargetDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    InterventionRecommendation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrategicPlanGoals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudentAchievements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AchievedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentAchievements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudentActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActivityName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ActivityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OrganizingSocietyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ScheduledDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    OrganizerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovalStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentActivities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudentAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TriggerType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentAlerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudentBadges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BadgeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AchievementDetail = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    VerificationHash = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IssuedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NotifiedParent = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentBadges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudentClearances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ClearedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClearedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentClearances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudentCounselings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SessionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CounselorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentCounselings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudentDisciplines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IncidentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentDisciplines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudentDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudentFeeDues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SemesterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FeeStructureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalDue = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    ScholarshipDeduction = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    OutstandingAmount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    WaivedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WaivedReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentFeeDues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudentFeePayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FeeDueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PaymentDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    VerifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VerificationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentFeePayments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudentFees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentFees", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudentGuardians",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Relationship = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentGuardians", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudentHealthRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BloodType = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    AllergiesEncrypted = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ChronicConditionsEncrypted = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    EmergencyContactName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EmergencyContactPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentHealthRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudentHostels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HostelRoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CheckInDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CheckOutDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentHostels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudentProctoringRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntegrityStatus = table.Column<int>(type: "int", nullable: false),
                    WarningCount = table.Column<int>(type: "int", nullable: false),
                    CriticalCount = table.Column<int>(type: "int", nullable: false),
                    EventLogJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AnswerPatternAnalysisJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VerificationStatus = table.Column<int>(type: "int", nullable: false),
                    CollusionFlagsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsEscalated = table.Column<bool>(type: "bit", nullable: false),
                    EscalatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentProctoringRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudentProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfileJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudentResumes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResumeJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentResumes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudentRiskScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Score = table.Column<double>(type: "float", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentRiskScores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudentRiskTrends",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScoresJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FromDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ToDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentRiskTrends", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudentScholarships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScholarshipName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AwardedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentScholarships", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudentSectionEnrollments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseOfferingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SectionNumber = table.Column<int>(type: "int", nullable: false),
                    DistributionStrategy = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentSectionEnrollments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudentSocieties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SocietyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FacultyAdvisorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PresidentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FoundedYear = table.Column<int>(type: "int", nullable: true),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentSocieties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudentTransports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransportRouteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentTransports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupportTickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    Priority = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportTickets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyncLagConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefaultGracePeriodSeconds = table.Column<int>(type: "int", nullable: false),
                    MaxGracePeriodSeconds = table.Column<int>(type: "int", nullable: false),
                    AutoCommitOnGraceExpiry = table.Column<bool>(type: "bit", nullable: false),
                    RequireAdminApprovalForCorrections = table.Column<bool>(type: "bit", nullable: false),
                    CorrectionPermission = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NotifyOwnerOnCorrection = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncLagConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyncLagEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReplicaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OriginalValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    WrittenValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    WrittenAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GraceExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CommittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CorrectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WrittenByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrectedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorrectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    LockedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncLagEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UniversityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReplicationLagHours = table.Column<int>(type: "int", nullable: false),
                    PrimaryDbConnectionString = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReadReplica1ConnectionString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReadReplica2ConnectionString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WriteReplicaConnectionString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RedisConnectionString = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RedisCacheTtlMinutes = table.Column<int>(type: "int", nullable: false),
                    SubdomainName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CustomDnsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    DnsNameserver1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DnsNameserver2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RegularClassCapacity = table.Column<int>(type: "int", nullable: false),
                    ExamClassCapacity = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TeachingSlots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SemesterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreditHours = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SpecializationRequired = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    StartDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompensationRate = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeachingSlots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TeamsSyncLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TimetableEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeamsTeamId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TeamsChannelId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SyncStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamsSyncLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantFinanceConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrimaryStandard = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FiscalYearStartMonth = table.Column<int>(type: "int", nullable: false),
                    FiscalYearLabel = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    BaseCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    AdditionalCurrenciesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RateSource = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    LibraryCapitalizationThreshold = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    RateApiKeySecretName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    TaxSettingsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantFinanceConfigurations", x => x.Id);
                    table.CheckConstraint("CK_TenantFinanceConfigurations_LibraryThreshold", "[LibraryCapitalizationThreshold] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ShortName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LicenseMode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ActivatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrainingAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrainingTitle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Provider = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingAssignments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Transcripts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PdfFilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VerificationHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IsOfficial = table.Column<bool>(type: "bit", nullable: false),
                    RequestedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transcripts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TranslationKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Module = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Context = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TranslationKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TransportDrivers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LicenseNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssignedVehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LicenseExpiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransportDrivers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TransportIncidents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DriverId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IncidentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransportIncidents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TransportInspections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DriverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BrakesOk = table.Column<bool>(type: "bit", nullable: false),
                    TiresOk = table.Column<bool>(type: "bit", nullable: false),
                    LightsOk = table.Column<bool>(type: "bit", nullable: false),
                    EmergencyExitsOk = table.Column<bool>(type: "bit", nullable: false),
                    FirstAidKitOk = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InspectionResult = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    InspectedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransportInspections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TransportRegistrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteStopId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeatNumber = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PaymentStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransportRegistrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TransportRoutes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RouteType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DistanceKm = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransportRoutes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TransportVehicles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RegistrationNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssignedRouteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransportVehicles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TunnelConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TunnelUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TunnelConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UniversityMissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Statement = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UniversityMissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UniversitySsoProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Domain = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SsoProvider = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SsoTenantId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SsoClientId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SsoClientSecretEncrypted = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Require2Fa = table.Column<bool>(type: "bit", nullable: false),
                    RequirePasswordChangeOnFirstLogin = table.Column<bool>(type: "bit", nullable: false),
                    LogoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrimaryColor = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    SecondaryColor = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UniversitySsoProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UpdateDeployments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ToVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Log = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    IsRolledBack = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UpdateDeployments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AuthProvider = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExternalClaimsJson = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeactivatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RefreshToken = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RefreshTokenExpiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserSecondFactors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Method = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SecretEncrypted = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BackupCodesHashCsv = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSecondFactors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VehicleMaintenances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransportVehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Cost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ScheduledDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleMaintenances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VendorPerformanceScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CafeteriaUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HygieneScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    MenuAdherenceScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    ComplaintsScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    OverallRating = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EvaluatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorPerformanceScores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Vendors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    VendorCnic = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    BankAccount = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ContactPerson = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vendors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VendorScorecards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Month = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    FoodQualityScore = table.Column<double>(type: "float", nullable: false),
                    HygieneScore = table.Column<double>(type: "float", nullable: false),
                    ServiceScore = table.Column<double>(type: "float", nullable: false),
                    ComplaintCount = table.Column<int>(type: "int", nullable: false),
                    HalalCertValid = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EvaluatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorScorecards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VerificationCycles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificationCycles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VisaDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RegistryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisaDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VisaExpiryAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RegistryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Urgency = table.Column<int>(type: "int", nullable: false),
                    DaysRemaining = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsNotified = table.Column<bool>(type: "bit", nullable: false),
                    TriggeredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisaExpiryAlerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VisaRenewals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RegistryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DocumentChecklistJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApplicationDeadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisaRenewals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VisaRenewalWorkflows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ForeignStudentRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ApplicationDeadline = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RenewalOutcome = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisaRenewalWorkflows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VisitingFacultyContracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FacultyProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreditHours = table.Column<int>(type: "int", nullable: false),
                    CompensationAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ContractUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SignedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RehireStatus = table.Column<int>(type: "int", nullable: true),
                    PerformanceRating = table.Column<double>(type: "float", nullable: true),
                    PerformanceDetailsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitingFacultyContracts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VisitingFacultyProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Cnic = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    HighestQualification = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DegreeTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Institution = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    YearsOfTeachingExperience = table.Column<int>(type: "int", nullable: false),
                    YearsOfIndustryExperience = table.Column<int>(type: "int", nullable: true),
                    SpecializationsCsv = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ResearchPublicationsCsv = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ProfessionalCertificationsCsv = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AvailabilityWindowsCsv = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PerCreditHourRate = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    HecEligibilityStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ProfileStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    VerdictForRehire = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DocumentsVerified = table.Column<bool>(type: "bit", nullable: false),
                    ReferencesChecked = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitingFacultyProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VisitorLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VisitorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    VisitorCNIC = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EntryTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExitTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitorLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VoiceIntents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntentName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UrduTriggerPhrases = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EnglishTriggerPhrases = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ActionEndpoint = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PermittedRole = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoiceIntents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VoiceSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserRole = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DetectedLanguage = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Transcript = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Intent = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EntitiesJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ResponseText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ResponseAudioUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AsrConfidence = table.Column<decimal>(type: "decimal(5,4)", nullable: true),
                    IsSlowMode = table.Column<bool>(type: "bit", nullable: false),
                    WasEscalated = table.Column<bool>(type: "bit", nullable: false),
                    EscalationReason = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoiceSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VoterTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ElectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HasVoted = table.Column<bool>(type: "bit", nullable: false),
                    VotedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoterTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WasteLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CafeteriaUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuantityWasted = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LoggedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WasteLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WebhookSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EndpointUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Secret = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaxRetries = table.Column<int>(type: "int", nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    LastDeliveredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastFailedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookSubscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WellnessCheckIns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnonymousToken = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WellbeingScore = table.Column<int>(type: "int", nullable: false),
                    CheckInDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WellnessCheckIns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WizardAdminCredentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WizardAdminCredentials", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WizardSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentStep = table.Column<int>(type: "int", nullable: false),
                    LicenseMode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    LicenseKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    StepData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BootstrapTokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByIp = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WizardSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NodesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TransitionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TriggerType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TriggerConfig = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsTemplate = table.Column<bool>(type: "bit", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Semesters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExamStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ExamEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ResultDeclarationDate = table.Column<DateOnly>(type: "date", nullable: true),
                    AddDropEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Semesters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Semesters_AcademicYears_AcademicYearId",
                        column: x => x.AcademicYearId,
                        principalTable: "AcademicYears",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AccreditationCriteria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccreditationBodyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccreditationCriteria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccreditationCriteria_AccreditationBodies_AccreditationBodyId",
                        column: x => x.AccreditationBodyId,
                        principalTable: "AccreditationBodies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AccreditationCycles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccreditationBodyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VisitDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccreditationCycles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccreditationCycles_AccreditationBodies_AccreditationBodyId",
                        column: x => x.AccreditationBodyId,
                        principalTable: "AccreditationBodies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ReadinessCriteria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FrameworkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CriterionCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SectionName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Weight = table.Column<decimal>(type: "decimal(3,2)", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(3,2)", nullable: false),
                    ComplianceStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EvidenceSource = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReadinessCriteria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReadinessCriteria_AccreditationFrameworks_FrameworkId",
                        column: x => x.FrameworkId,
                        principalTable: "AccreditationFrameworks",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ApplicationDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploadDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    VerifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    VerificationStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationDocuments_AdmissionApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "AdmissionApplications",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LeadActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdmissionLeadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActivityType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ActivityDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeadActivities_AdmissionLeads_AdmissionLeadId",
                        column: x => x.AdmissionLeadId,
                        principalTable: "AdmissionLeads",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AdvisorMeetings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdvisorAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdvisorMeetings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdvisorMeetings_AdvisorAssignments_AdvisorAssignmentId",
                        column: x => x.AdvisorAssignmentId,
                        principalTable: "AdvisorAssignments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AdvisorNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdvisorAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdvisorNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdvisorNotes_AdvisorAssignments_AdvisorAssignmentId",
                        column: x => x.AdvisorAssignmentId,
                        principalTable: "AdvisorAssignments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AssessmentComponents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchemeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComponentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WeightPercentage = table.Column<int>(type: "int", nullable: false),
                    TotalMarks = table.Column<int>(type: "int", nullable: false),
                    PassingMarks = table.Column<int>(type: "int", nullable: true),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentComponents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentComponents_AssessmentSchemes_SchemeId",
                        column: x => x.SchemeId,
                        principalTable: "AssessmentSchemes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AssetCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LocationBuildingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LocationRoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PurchaseCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PurchaseDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WarrantyExpiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DepreciationMethod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UsefulLifeYears = table.Column<int>(type: "int", nullable: false),
                    CurrentValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assets_AssetCategories_AssetCategoryId",
                        column: x => x.AssetCategoryId,
                        principalTable: "AssetCategories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BackupRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConfigurationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BackupType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Checksum = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsEncrypted = table.Column<bool>(type: "bit", nullable: false),
                    RetentionTier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TriggeredBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackupRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BackupRecords_BackupConfigurations_ConfigurationId",
                        column: x => x.ConfigurationId,
                        principalTable: "BackupConfigurations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItems_BookOrders_BookOrderId",
                        column: x => x.BookOrderId,
                        principalTable: "BookOrders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DriveRegistrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CampusDriveId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriveRegistrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriveRegistrations_CampusDrives_CampusDriveId",
                        column: x => x.CampusDriveId,
                        principalTable: "CampusDrives",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Buildings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BuildingCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CampusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FloorCount = table.Column<int>(type: "int", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Buildings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Buildings_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CampusConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CampusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcademicCalendarJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GradingPolicyJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AdmissionRulesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FeeStructureJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PoliciesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampusConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampusConfigurations_Campuses_CampusId",
                        column: x => x.CampusId,
                        principalTable: "Campuses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CareerEventRegistrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CareerEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerEventRegistrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CareerEventRegistrations_CareerEvents_CareerEventId",
                        column: x => x.CareerEventId,
                        principalTable: "CareerEvents",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ChatbotMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChatbotConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sender = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatbotMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatbotMessages_ChatbotConversations_ChatbotConversationId",
                        column: x => x.ChatbotConversationId,
                        principalTable: "ChatbotConversations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ComplianceControls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FrameworkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ControlCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OwnerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ReviewFrequency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextReviewDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AutomatedCheckConfigJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsAutomated = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplianceControls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComplianceControls_ComplianceFrameworks_FrameworkId",
                        column: x => x.FrameworkId,
                        principalTable: "ComplianceFrameworks",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ConfigAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConfigurationEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConfigKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PreviousValueJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValueJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FromVersion = table.Column<int>(type: "int", nullable: true),
                    ToVersion = table.Column<int>(type: "int", nullable: true),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedByName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfigAuditLogs_ConfigurationEntries_ConfigurationEntryId",
                        column: x => x.ConfigurationEntryId,
                        principalTable: "ConfigurationEntries",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ConfigVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConfigurationEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    ValueJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangeDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedByName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfigVersions_ConfigurationEntries_ConfigurationEntryId",
                        column: x => x.ConfigurationEntryId,
                        principalTable: "ConfigurationEntries",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TaxSlabs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CountryPackId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MinAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Rate = table.Column<decimal>(type: "decimal(8,4)", nullable: false),
                    FixedDeduction = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxSlabs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxSlabs_CountryPacks_CountryPackId",
                        column: x => x.CountryPackId,
                        principalTable: "CountryPacks",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CurriculumCourses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurriculumVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Semester = table.Column<int>(type: "int", nullable: false),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    IsElective = table.Column<bool>(type: "bit", nullable: false),
                    TheoryCreditHours = table.Column<int>(type: "int", nullable: false),
                    LabCreditHours = table.Column<int>(type: "int", nullable: false),
                    TotalCreditHours = table.Column<int>(type: "int", nullable: false, computedColumnSql: "[TheoryCreditHours] + [LabCreditHours]", stored: true),
                    MappedClosCsv = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumCourses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurriculumCourses_CurriculumVersions_CurriculumVersionId",
                        column: x => x.CurriculumVersionId,
                        principalTable: "CurriculumVersions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CurriculumPrerequisites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurriculumVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrerequisiteCourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Relationship = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SubstituteCourseIdsCsv = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumPrerequisites", x => x.Id);
                    table.CheckConstraint("CK_CurriculumPrerequisites_NoSelf", "[CourseId] <> [PrerequisiteCourseId]");
                    table.ForeignKey(
                        name: "FK_CurriculumPrerequisites_CurriculumVersions_CurriculumVersionId",
                        column: x => x.CurriculumVersionId,
                        principalTable: "CurriculumVersions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DashboardSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DashboardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RefreshFrequency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DashboardSubscriptions_Dashboards_DashboardId",
                        column: x => x.DashboardId,
                        principalTable: "Dashboards",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DashboardWidgets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DashboardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VisualizationType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ConfigJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardWidgets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DashboardWidgets_Dashboards_DashboardId",
                        column: x => x.DashboardId,
                        principalTable: "Dashboards",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BIReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ReportCategory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataWarehouseTableId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QueryJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BIReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BIReports_DataWarehouseTables_DataWarehouseTableId",
                        column: x => x.DataWarehouseTableId,
                        principalTable: "DataWarehouseTables",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DWColumns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataWarehouseTableId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ColumnName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DWColumns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DWColumns_DataWarehouseTables_DataWarehouseTableId",
                        column: x => x.DataWarehouseTableId,
                        principalTable: "DataWarehouseTables",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DegreeRequirements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProgramId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequirementType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MinCredits = table.Column<int>(type: "int", nullable: true),
                    CourseCount = table.Column<int>(type: "int", nullable: true),
                    MinGradePoints = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AllowedCourseIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DegreeRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DegreeRequirements_DegreePrograms_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "DegreePrograms",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Batches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartYear = table.Column<int>(type: "int", nullable: false),
                    EndYear = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Batches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Batches_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreditHours = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrerequisiteCourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CloMappingJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PloMappingJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Courses_Courses_PrerequisiteCourseId",
                        column: x => x.PrerequisiteCourseId,
                        principalTable: "Courses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Courses_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Faculties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EmployeeCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Designation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Faculties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Faculties_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ElectionPositions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ElectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PositionName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Seats = table.Column<int>(type: "int", nullable: false),
                    TermDuration = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElectionPositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ElectionPositions_Elections_ElectionId",
                        column: x => x.ElectionId,
                        principalTable: "Elections",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EmployeeContracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Salary = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ContractType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeContracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeContracts_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EmployeeDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeDocuments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ResearchLabs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LabName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LabCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RoomNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LabSupervisorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchLabs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResearchLabs_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ResearchLabs_Employees_LabSupervisorId",
                        column: x => x.LabSupervisorId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SecurityGuards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BadgeNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsArmed = table.Column<bool>(type: "bit", nullable: false),
                    TrainingExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityGuards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecurityGuards_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ExtensionInstallations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExtensionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstalledVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConfigJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InstallLog = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    InstalledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UninstalledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InstalledByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtensionInstallations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExtensionInstallations_Extensions_ExtensionId",
                        column: x => x.ExtensionId,
                        principalTable: "Extensions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ExtensionReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExtensionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ReviewedByName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeveloperResponse = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DeveloperResponseAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtensionReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExtensionReviews_Extensions_ExtensionId",
                        column: x => x.ExtensionId,
                        principalTable: "Extensions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PluginRevenueShares",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExtensionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeveloperEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GrossAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AegisCoreAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DeveloperAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PluginRevenueShares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PluginRevenueShares_Extensions_ExtensionId",
                        column: x => x.ExtensionId,
                        principalTable: "Extensions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FeeInstallments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FeeDemandId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    PenaltyAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    RecognizedAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastPenaltyAppliedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeeInstallments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeeInstallments_FeeDemands_FeeDemandId",
                        column: x => x.FeeDemandId,
                        principalTable: "FeeDemands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinancePaymentPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FeeDemandId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    SurchargeAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    InstallmentCount = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MissedInstallments = table.Column<int>(type: "int", nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancePaymentPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancePaymentPlans_FeeDemands_FeeDemandId",
                        column: x => x.FeeDemandId,
                        principalTable: "FeeDemands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinancePayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FeeDemandId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FeeInstallmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Method = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    GatewaySlug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ExternalReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    WebhookId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SettledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancePayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancePayments_FeeDemands_FeeDemandId",
                        column: x => x.FeeDemandId,
                        principalTable: "FeeDemands",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FeeComponents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FeeStructureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComponentName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeeComponents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeeComponents_FeeStructures_FeeStructureId",
                        column: x => x.FeeStructureId,
                        principalTable: "FeeStructures",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FinanceBankFeedTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinanceBankAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    BookingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Narration = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MatchedJournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ImportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceBankFeedTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceBankFeedTransactions_FinanceBankAccounts_FinanceBankAccountId",
                        column: x => x.FinanceBankAccountId,
                        principalTable: "FinanceBankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceConsolidationMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConsolidationGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportingEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceConsolidationMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceConsolidationMembers_FinanceConsolidationGroups_ConsolidationGroupId",
                        column: x => x.ConsolidationGroupId,
                        principalTable: "FinanceConsolidationGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceConsolidationMemoLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConsolidationRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChartOfAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LedgerBalance = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    EliminationAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    ConsolidatedBalance = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceConsolidationMemoLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceConsolidationMemoLines_FinanceConsolidationRuns_ConsolidationRunId",
                        column: x => x.ConsolidationRunId,
                        principalTable: "FinanceConsolidationRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceScholarshipAwards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GrantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ScholarshipType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CoverageType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    DisbursementMethod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AwardedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RenewalReviewAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    JournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceScholarshipAwards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceScholarshipAwards_FinanceGrants_GrantId",
                        column: x => x.GrantId,
                        principalTable: "FinanceGrants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceScholarshipPrograms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GrantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ScholarshipType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CoverageType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CoverageValue = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    DisbursementMethod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MinimumCgpa = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    AllowsStacking = table.Column<bool>(type: "bit", nullable: false),
                    OpensAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosesAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DurationMonths = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceScholarshipPrograms", x => x.Id);
                    table.CheckConstraint("CK_FinanceScholarshipPrograms_Values", "[CoverageValue] > 0 AND [DurationMonths] > 0 AND [ClosesAt] > [OpensAt]");
                    table.ForeignKey(
                        name: "FK_FinanceScholarshipPrograms_FinanceGrants_GrantId",
                        column: x => x.GrantId,
                        principalTable: "FinanceGrants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceInsuranceClaims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinanceInsurancePolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimNumber = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ClaimedAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    SettledAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FiledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SettledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    JournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceInsuranceClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceInsuranceClaims_FinanceInsurancePolicies_FinanceInsurancePolicyId",
                        column: x => x.FinanceInsurancePolicyId,
                        principalTable: "FinanceInsurancePolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceInventoryMovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinanceInventoryItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MovementType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    CostCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Reference = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    MovementDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    JournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceInventoryMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceInventoryMovements_FinanceInventoryItems_FinanceInventoryItemId",
                        column: x => x.FinanceInventoryItemId,
                        principalTable: "FinanceInventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceInvestmentHoldings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinanceInvestmentFundId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstrumentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Issuer = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AssetClass = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Measurement = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    CostBasis = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    MarketValue = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    AnnualYieldPercent = table.Column<decimal>(type: "numeric(9,4)", nullable: false),
                    PurchaseDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaturityDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceInvestmentHoldings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceInvestmentHoldings_FinanceInvestmentFunds_FinanceInvestmentFundId",
                        column: x => x.FinanceInvestmentFundId,
                        principalTable: "FinanceInvestmentFunds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinancePayrollPostingLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinancePayrollPostingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CostCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GrossSalary = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    IncomeTax = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Eobi = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    SocialSecurity = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Gratuity = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    NetSalary = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancePayrollPostingLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancePayrollPostingLines_FinancePayrollPostings_FinancePayrollPostingId",
                        column: x => x.FinancePayrollPostingId,
                        principalTable: "FinancePayrollPostings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinancePettyCashCounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinancePettyCashFundId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SystemBalance = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    PhysicalBalance = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Discrepancy = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InitiatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConfirmedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CountedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancePettyCashCounts", x => x.Id);
                    table.CheckConstraint("CK_FinancePettyCashCounts_NonNegativeBalances", "[SystemBalance] >= 0 AND [PhysicalBalance] >= 0");
                    table.ForeignKey(
                        name: "FK_FinancePettyCashCounts_FinancePettyCashFunds_FinancePettyCashFundId",
                        column: x => x.FinancePettyCashFundId,
                        principalTable: "FinancePettyCashFunds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinancePettyCashExpenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinancePettyCashFundId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    VendorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    ReceiptHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SpentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancePettyCashExpenses", x => x.Id);
                    table.CheckConstraint("CK_FinancePettyCashExpenses_PositiveAmount", "[Amount] > 0");
                    table.ForeignKey(
                        name: "FK_FinancePettyCashExpenses_FinancePettyCashFunds_FinancePettyCashFundId",
                        column: x => x.FinancePettyCashFundId,
                        principalTable: "FinancePettyCashFunds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinancePqcKeyCeremonyApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CeremonyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovalDigest = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancePqcKeyCeremonyApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancePqcKeyCeremonyApprovals_FinancePqcKeyCeremonies_CeremonyId",
                        column: x => x.CeremonyId,
                        principalTable: "FinancePqcKeyCeremonies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceRestatementMemoLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RestatementRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RestatementRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChartOfAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Debit = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Credit = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceRestatementMemoLines", x => x.Id);
                    table.CheckConstraint("CK_FinanceRestatementMemoLines_OneSide", "([Debit] > 0 AND [Credit] = 0) OR ([Credit] > 0 AND [Debit] = 0)");
                    table.ForeignKey(
                        name: "FK_FinanceRestatementMemoLines_FinanceRestatementRuns_RestatementRunId",
                        column: x => x.RestatementRunId,
                        principalTable: "FinanceRestatementRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceSecurityDepositDeductions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SecurityDepositId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceSecurityDepositDeductions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceSecurityDepositDeductions_FinanceSecurityDeposits_SecurityDepositId",
                        column: x => x.SecurityDepositId,
                        principalTable: "FinanceSecurityDeposits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceUtilityAllocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinanceUtilityBillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CostCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceUtilityAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceUtilityAllocations_FinanceUtilityBills_FinanceUtilityBillId",
                        column: x => x.FinanceUtilityBillId,
                        principalTable: "FinanceUtilityBills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinancePurchaseOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancePurchaseOrders", x => x.Id);
                    table.CheckConstraint("CK_FinancePurchaseOrders_PositiveTotal", "[TotalAmount] > 0");
                    table.ForeignKey(
                        name: "FK_FinancePurchaseOrders_FinanceVendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "FinanceVendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceStandingPurchaseOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Period = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LimitAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    UsedAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    StartsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    EndsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceStandingPurchaseOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceStandingPurchaseOrders_FinanceVendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "FinanceVendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceVendorContacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Designation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Mobile = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceVendorContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceVendorContacts_FinanceVendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "FinanceVendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceVendorContracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContractNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    StartsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    EndsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    RenewalNoticeDays = table.Column<int>(type: "int", nullable: false),
                    SlaScore = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceVendorContracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceVendorContracts_FinanceVendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "FinanceVendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceVendorDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Sha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PqcAlgorithm = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PqcKeyId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PqcSignature = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceVendorDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceVendorDocuments_FinanceVendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "FinanceVendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceVoucherApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinanceVoucherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApproverUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApproverRole = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Decision = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PqcAlgorithm = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    PqcKeyId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PqcSignature = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceVoucherApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceVoucherApprovals_FinanceVouchers_FinanceVoucherId",
                        column: x => x.FinanceVoucherId,
                        principalTable: "FinanceVouchers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceVoucherLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinanceVoucherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChartOfAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CostCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReportingEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CounterpartyReportingEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Debit = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Credit = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceVoucherLines", x => x.Id);
                    table.CheckConstraint("CK_FinanceVoucherLines_OneSided", "([Debit] > 0 AND [Credit] = 0) OR ([Credit] > 0 AND [Debit] = 0)");
                    table.ForeignKey(
                        name: "FK_FinanceVoucherLines_FinanceVouchers_FinanceVoucherId",
                        column: x => x.FinanceVoucherId,
                        principalTable: "FinanceVouchers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FiscalPeriods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FiscalYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalPeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FiscalPeriods_FiscalYears_FiscalYearId",
                        column: x => x.FiscalYearId,
                        principalTable: "FiscalYears",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DocumentExpiryTrackers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ForeignStudentRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExpiryDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Alert90DaysSent = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Alert60DaysSent = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Alert30DaysSent = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Alert14DaysSent = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Alert7DaysSent = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsExpired = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentExpiryTrackers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentExpiryTrackers_ForeignStudentRecords_ForeignStudentRecordId",
                        column: x => x.ForeignStudentRecordId,
                        principalTable: "ForeignStudentRecords",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FormSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FormId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmittedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SubmittedByName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SubmittedByEmail = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormSubmissions_FormDefinitions_FormId",
                        column: x => x.FormId,
                        principalTable: "FormDefinitions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GradeBands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GradeScaleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LetterGrade = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    GpaPoints = table.Column<decimal>(type: "decimal(3,2)", nullable: false),
                    PercentageFrom = table.Column<int>(type: "int", nullable: false),
                    PercentageTo = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeBands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GradeBands_GradeScales_GradeScaleId",
                        column: x => x.GradeScaleId,
                        principalTable: "GradeScales",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GrievanceResponses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResponderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResponseText = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: false),
                    StatusUpdate = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrievanceResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GrievanceResponses_GrievanceTickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "GrievanceTickets",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "HostelFloors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HostelBlockId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FloorNumber = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HostelFloors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HostelFloors_HostelBlocks_HostelBlockId",
                        column: x => x.HostelBlockId,
                        principalTable: "HostelBlocks",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SensorReadings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SensorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(12,4)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExtraDataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SensorReadings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SensorReadings_IoTSensors_SensorId",
                        column: x => x.SensorId,
                        principalTable: "IoTSensors",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "JobApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobPostingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CoverLetter = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AppliedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobApplications_JobPostings_JobPostingId",
                        column: x => x.JobPostingId,
                        principalTable: "JobPostings",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "JournalEntryLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChartOfAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CostCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReportingEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CounterpartyReportingEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Debit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Credit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalEntryLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JournalEntryLines_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalTable: "JournalEntries",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BookCopies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LibraryBookId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Barcode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookCopies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookCopies_LibraryBooks_LibraryBookId",
                        column: x => x.LibraryBookId,
                        principalTable: "LibraryBooks",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MentoringSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MentorAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MentoringSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MentoringSessions_MentorAssignments_MentorAssignmentId",
                        column: x => x.MentorAssignmentId,
                        principalTable: "MentorAssignments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MeritListEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MeritListId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MeritRank = table.Column<int>(type: "int", nullable: false),
                    MeritScore = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    MatricContribution = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    FscContribution = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    EntryTestContribution = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    SeatAllocated = table.Column<bool>(type: "bit", nullable: false),
                    AllocationStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeritListEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeritListEntries_AdmissionApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "AdmissionApplications",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MeritListEntries_MeritLists_MeritListId",
                        column: x => x.MeritListId,
                        principalTable: "MeritLists",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PaymentPlanInstallments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentPlanArrangementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstallmentNumber = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    DueDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    PaidAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentPlanInstallments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentPlanInstallments_PaymentPlanArrangements_PaymentPlanArrangementId",
                        column: x => x.PaymentPlanArrangementId,
                        principalTable: "PaymentPlanArrangements",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Payslips",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayrollRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Month = table.Column<DateOnly>(type: "date", nullable: false),
                    BasicSalary = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    GrossSalary = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    EobiDeduction = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    IncomeTaxDeduction = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    ProvidentFundDeduction = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    TotalDeductions = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    NetSalary = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    PdfFilePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payslips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payslips_PayrollRuns_PayrollRunId",
                        column: x => x.PayrollRunId,
                        principalTable: "PayrollRuns",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CrossDeptPermissionRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestingUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestingDeptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetDeptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReviewedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrossDeptPermissionRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrossDeptPermissionRequests_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RequisitionLineItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequisitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequisitionLineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequisitionLineItems_ProcurementRequisitions_RequisitionId",
                        column: x => x.RequisitionId,
                        principalTable: "ProcurementRequisitions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CLOPOMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseLearningOutcomeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProgramOutcomeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Strength = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CLOPOMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CLOPOMappings_CourseLearningOutcomes_CourseLearningOutcomeId",
                        column: x => x.CourseLearningOutcomeId,
                        principalTable: "CourseLearningOutcomes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CLOPOMappings_ProgramOutcomes_ProgramOutcomeId",
                        column: x => x.ProgramOutcomeId,
                        principalTable: "ProgramOutcomes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GrantExpenditures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResearchGrantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ExpenditureDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrantExpenditures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GrantExpenditures_ResearchGrants_ResearchGrantId",
                        column: x => x.ResearchGrantId,
                        principalTable: "ResearchGrants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PatentInventors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResearchPatentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FacultyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatentInventors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatentInventors_ResearchPatents_ResearchPatentId",
                        column: x => x.ResearchPatentId,
                        principalTable: "ResearchPatents",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ResearchDeliverables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResearchProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchDeliverables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResearchDeliverables_ResearchProjects_ResearchProjectId",
                        column: x => x.ResearchProjectId,
                        principalTable: "ResearchProjects",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ResearchMilestones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResearchProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchMilestones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResearchMilestones_ResearchProjects_ResearchProjectId",
                        column: x => x.ResearchProjectId,
                        principalTable: "ResearchProjects",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ResearchProjectMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResearchProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FacultyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchProjectMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResearchProjectMembers_ResearchProjects_ResearchProjectId",
                        column: x => x.ResearchProjectId,
                        principalTable: "ResearchProjects",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PublicationAuthors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResearchPublicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FacultyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorOrder = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicationAuthors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PublicationAuthors_ResearchPublications_ResearchPublicationId",
                        column: x => x.ResearchPublicationId,
                        principalTable: "ResearchPublications",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RubricCriteria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RubricId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    MaxScore = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RubricCriteria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RubricCriteria_Rubrics_RubricId",
                        column: x => x.RubricId,
                        principalTable: "Rubrics",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RuleVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    ConditionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangeDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedByName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuleVersions_RuleDefinitions_RuleId",
                        column: x => x.RuleId,
                        principalTable: "RuleDefinitions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "JobExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: true),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    Output = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TriggeredBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobExecutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobExecutions_ScheduledJobs_JobId",
                        column: x => x.JobId,
                        principalTable: "ScheduledJobs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FeePayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentFeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeePayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeePayments_StudentFees_StudentFeeId",
                        column: x => x.StudentFeeId,
                        principalTable: "StudentFees",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TranslationValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TranslationKeyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LanguageCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsMachineTranslated = table.Column<bool>(type: "bit", nullable: false),
                    LastReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TranslationValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TranslationValues_TranslationKeys_TranslationKeyId",
                        column: x => x.TranslationKeyId,
                        principalTable: "TranslationKeys",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DriverShifts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransportDriverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShiftStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ShiftEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverShifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverShifts_TransportDrivers_TransportDriverId",
                        column: x => x.TransportDriverId,
                        principalTable: "TransportDrivers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RouteStops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransportRouteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StopName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    EstimatedTime = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteStops", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteStops_TransportRoutes_TransportRouteId",
                        column: x => x.TransportRouteId,
                        principalTable: "TransportRoutes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GPSLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransportVehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(12,8)", nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(12,8)", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GPSLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GPSLocations_TransportVehicles_TransportVehicleId",
                        column: x => x.TransportVehicleId,
                        principalTable: "TransportVehicles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AssetVerifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VerificationCycleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VerifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetVerifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetVerifications_VerificationCycles_VerificationCycleId",
                        column: x => x.VerificationCycleId,
                        principalTable: "VerificationCycles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FacultyEngagements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeachingSlotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FacultyProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContractNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TotalCompensation = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    StartDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ContractStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IpClause = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AttendanceCompliant = table.Column<bool>(type: "bit", nullable: false),
                    GradeSubmissionCompliant = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacultyEngagements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FacultyEngagements_VisitingFacultyProfiles_FacultyProfileId",
                        column: x => x.FacultyProfileId,
                        principalTable: "VisitingFacultyProfiles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WorkflowInstances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefinitionVersion = table.Column<int>(type: "int", nullable: false),
                    InstanceName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CurrentNodeId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TriggeredByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TriggerType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContextJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VariablesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowInstances_WorkflowDefinitions_DefinitionId",
                        column: x => x.DefinitionId,
                        principalTable: "WorkflowDefinitions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AcademicHolidays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SemesterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicHolidays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcademicHolidays_AcademicYears_AcademicYearId",
                        column: x => x.AcademicYearId,
                        principalTable: "AcademicYears",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AcademicHolidays_Semesters_SemesterId",
                        column: x => x.SemesterId,
                        principalTable: "Semesters",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AccreditationEvidences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccreditationCriterionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccreditationEvidences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccreditationEvidences_AccreditationCriteria_AccreditationCriterionId",
                        column: x => x.AccreditationCriterionId,
                        principalTable: "AccreditationCriteria",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AccreditationAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccreditationCycleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PerformedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PerformedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccreditationAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccreditationAuditLogs_AccreditationCycles_AccreditationCycleId",
                        column: x => x.AccreditationCycleId,
                        principalTable: "AccreditationCycles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AccreditationCommittees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccreditationCycleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccreditationCommittees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccreditationCommittees_AccreditationCycles_AccreditationCycleId",
                        column: x => x.AccreditationCycleId,
                        principalTable: "AccreditationCycles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SelfStudyReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccreditationCycleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SelfStudyReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SelfStudyReports_AccreditationCycles_AccreditationCycleId",
                        column: x => x.AccreditationCycleId,
                        principalTable: "AccreditationCycles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CourseMarks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessmentComponentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObtainedMarks = table.Column<int>(type: "int", nullable: false),
                    EntryDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EnteredBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseMarks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseMarks_AssessmentComponents_AssessmentComponentId",
                        column: x => x.AssessmentComponentId,
                        principalTable: "AssessmentComponents",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EnergyReadings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BuildingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SensorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReadingType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Consumption = table.Column<decimal>(type: "decimal(14,4)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ReadingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Cost = table.Column<decimal>(type: "decimal(14,4)", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnergyReadings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnergyReadings_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalTable: "Buildings",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Floors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FloorNumber = table.Column<int>(type: "int", nullable: false),
                    BuildingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Floors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Floors_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalTable: "Buildings",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AuditFindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ControlId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IdentifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdentifiedByName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IdentifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TargetResolutionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsLegalHold = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditFindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditFindings_ComplianceControls_ControlId",
                        column: x => x.ControlId,
                        principalTable: "ComplianceControls",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ControlEvidences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ControlId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    EvidenceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ControlEvidences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ControlEvidences_ComplianceControls_ControlId",
                        column: x => x.ControlId,
                        principalTable: "ComplianceControls",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "KPIAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DashboardWidgetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlertName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AlertCondition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Threshold = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KPIAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KPIAlerts_DashboardWidgets_DashboardWidgetId",
                        column: x => x.DashboardWidgetId,
                        principalTable: "DashboardWidgets",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DataExportLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BIReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DashboardId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Format = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RowCount = table.Column<int>(type: "int", nullable: false),
                    ExportedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataExportLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataExportLogs_BIReports_BIReportId",
                        column: x => x.BIReportId,
                        principalTable: "BIReports",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DataExportLogs_Dashboards_DashboardId",
                        column: x => x.DashboardId,
                        principalTable: "Dashboards",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UniversityId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EnrollmentStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DirectoryOptOut = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Students_Batches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "Batches",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CloPlomappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CloCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PloCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AlignmentLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CloPlomappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CloPlomappings_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ExamSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScheduledDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Venue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamSchedules_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CourseAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FacultyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseAssignments_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CourseAssignments_Faculties_FacultyId",
                        column: x => x.FacultyId,
                        principalTable: "Faculties",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Timetables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FacultyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayOfWeek = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    Room = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Timetables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Timetables_Batches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "Batches",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Timetables_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Timetables_Faculties_FacultyId",
                        column: x => x.FacultyId,
                        principalTable: "Faculties",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LabBookings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResearchLabId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ExpectedAttendees = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabBookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabBookings_ResearchLabs_ResearchLabId",
                        column: x => x.ResearchLabId,
                        principalTable: "ResearchLabs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LabConsumables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResearchLabId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    QuantityInStock = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MinimumStockLevel = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReorderQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StorageLocation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsHazardous = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabConsumables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabConsumables_ResearchLabs_ResearchLabId",
                        column: x => x.ResearchLabId,
                        principalTable: "ResearchLabs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LabEquipments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResearchLabId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EquipmentName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AssetTag = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Manufacturer = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PurchaseDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PurchaseCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Specifications = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabEquipments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabEquipments_ResearchLabs_ResearchLabId",
                        column: x => x.ResearchLabId,
                        principalTable: "ResearchLabs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LabSafetyInspections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResearchLabId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InspectionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InspectedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Findings = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CorrectiveActions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CorrectiveActionsDue = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsCompliant = table.Column<bool>(type: "bit", nullable: false),
                    SignedOffByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SignedOffAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabSafetyInspections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabSafetyInspections_ResearchLabs_ResearchLabId",
                        column: x => x.ResearchLabId,
                        principalTable: "ResearchLabs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GuardRounds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SecurityGuardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoundStartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RoundEndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuardRounds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuardRounds_SecurityGuards_SecurityGuardId",
                        column: x => x.SecurityGuardId,
                        principalTable: "SecurityGuards",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GuardShifts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SecurityGuardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShiftDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Post = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Gate = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuardShifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuardShifts_SecurityGuards_SecurityGuardId",
                        column: x => x.SecurityGuardId,
                        principalTable: "SecurityGuards",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FinanceScholarshipApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProgramId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Cgpa = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ApplicantStatement = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DecisionNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DecidedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AppliedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DecidedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AwardId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceScholarshipApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceScholarshipApplications_FinanceScholarshipPrograms_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "FinanceScholarshipPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceInvestmentTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinanceInvestmentHoldingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransactionType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    GainOrLoss = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    JournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceInvestmentTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceInvestmentTransactions_FinanceInvestmentHoldings_FinanceInvestmentHoldingId",
                        column: x => x.FinanceInvestmentHoldingId,
                        principalTable: "FinanceInvestmentHoldings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceGoodsReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ConfirmedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceGoodsReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceGoodsReceipts_FinancePurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "FinancePurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinancePurchaseOrderLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpenseAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancePurchaseOrderLines", x => x.Id);
                    table.CheckConstraint("CK_FinancePurchaseOrderLines_Amounts", "[Quantity] > 0 AND [UnitPrice] >= 0 AND [LineTotal] = [Quantity] * [UnitPrice]");
                    table.ForeignKey(
                        name: "FK_FinancePurchaseOrderLines_FinancePurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "FinancePurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceVendorInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentVoucherId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceVendorInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceVendorInvoices_FinancePurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "FinancePurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceVendorInvoices_FinanceVendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "FinanceVendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceStandingPurchaseOrderSpend",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StandingPurchaseOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SpentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceStandingPurchaseOrderSpend", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceStandingPurchaseOrderSpend_FinanceStandingPurchaseOrders_StandingPurchaseOrderId",
                        column: x => x.StandingPurchaseOrderId,
                        principalTable: "FinanceStandingPurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HostelRooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HostelFloorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RoomType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HostelRooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HostelRooms_HostelFloors_HostelFloorId",
                        column: x => x.HostelFloorId,
                        principalTable: "HostelFloors",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "NotificationRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Recipient = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationRecords_WorkflowInstances_InstanceId",
                        column: x => x.InstanceId,
                        principalTable: "WorkflowInstances",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WorkflowSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NodeId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NodeLabel = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NodeType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InputJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OutputJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowSteps_WorkflowInstances_InstanceId",
                        column: x => x.InstanceId,
                        principalTable: "WorkflowInstances",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EvidenceVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccreditationEvidenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvidenceVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvidenceVersions_AccreditationEvidences_AccreditationEvidenceId",
                        column: x => x.AccreditationEvidenceId,
                        principalTable: "AccreditationEvidences",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CommitteeMeetings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccreditationCommitteeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MeetingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Minutes = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommitteeMeetings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommitteeMeetings_AccreditationCommittees_AccreditationCommitteeId",
                        column: x => x.AccreditationCommitteeId,
                        principalTable: "AccreditationCommittees",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CommitteeMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccreditationCommitteeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FacultyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommitteeMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommitteeMembers_AccreditationCommittees_AccreditationCommitteeId",
                        column: x => x.AccreditationCommitteeId,
                        principalTable: "AccreditationCommittees",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SSRReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SelfStudyReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SSRReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SSRReviews_SelfStudyReports_SelfStudyReportId",
                        column: x => x.SelfStudyReportId,
                        principalTable: "SelfStudyReports",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SSRSections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SelfStudyReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SSRSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SSRSections_SelfStudyReports_SelfStudyReportId",
                        column: x => x.SelfStudyReportId,
                        principalTable: "SelfStudyReports",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FloorPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FloorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RoomsLayoutJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageWidth = table.Column<int>(type: "int", nullable: false),
                    ImageHeight = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FloorPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FloorPlans_Floors_FloorId",
                        column: x => x.FloorId,
                        principalTable: "Floors",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Rooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RoomNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FloorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    RoomType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rooms_Floors_FloorId",
                        column: x => x.FloorId,
                        principalTable: "Floors",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CorrectiveActionPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FindingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AssignedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedToName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletionNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CorrectiveActionPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CorrectiveActionPlans_AuditFindings_FindingId",
                        column: x => x.FindingId,
                        principalTable: "AuditFindings",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CreditTransfers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalCourseName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ExternalCourseCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ExternalInstitution = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreditHours = table.Column<int>(type: "int", nullable: false),
                    InternalCourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InternalCourseCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditTransfers_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DegreeAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProgramId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequirementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EarnedValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    RequiredValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CompletedCount = table.Column<int>(type: "int", nullable: true),
                    RequiredCount = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LastEvaluatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DegreeAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DegreeAudits_DegreePrograms_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "DegreePrograms",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DegreeAudits_DegreeRequirements_RequirementId",
                        column: x => x.RequirementId,
                        principalTable: "DegreeRequirements",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DegreeAudits_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Enrollments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Grade = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    GpaPoints = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    EnrollmentStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Enrollments_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Enrollments_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StudentTransfers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceCampusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DestinationCampusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SourceApprovedBy = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SourceApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DestApprovedBy = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DestApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentTransfers_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Visitors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VisitorName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CNIC = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Company = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HostEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HostStudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CheckInTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CheckOutTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BadgeNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsBlacklisted = table.Column<bool>(type: "bit", nullable: false),
                    BlacklistReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Visitors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Visitors_Employees_HostEmployeeId",
                        column: x => x.HostEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Visitors_Students_HostStudentId",
                        column: x => x.HostStudentId,
                        principalTable: "Students",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ExamSeatings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeatNumber = table.Column<int>(type: "int", nullable: false),
                    Classroom = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RowNumber = table.Column<int>(type: "int", nullable: false),
                    ColumnNumber = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamSeatings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamSeatings_ExamSchedules_ExamScheduleId",
                        column: x => x.ExamScheduleId,
                        principalTable: "ExamSchedules",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExamSeatings_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ConsumableRequisitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LabConsumableId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IssuedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IssuedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumableRequisitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsumableRequisitions_LabConsumables_LabConsumableId",
                        column: x => x.LabConsumableId,
                        principalTable: "LabConsumables",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CalibrationRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LabEquipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CalibrationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NextCalibrationDue = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CalibratedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Results = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CertificateUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalibrationRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalibrationRecords_LabEquipments_LabEquipmentId",
                        column: x => x.LabEquipmentId,
                        principalTable: "LabEquipments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EquipmentBookings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LabEquipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentBookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentBookings_LabEquipments_LabEquipmentId",
                        column: x => x.LabEquipmentId,
                        principalTable: "LabEquipments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EquipmentMaintenances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LabEquipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IssueDescription = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ReportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReportedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssignedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Cost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentMaintenances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentMaintenances_LabEquipments_LabEquipmentId",
                        column: x => x.LabEquipmentId,
                        principalTable: "LabEquipments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EquipmentUsageLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LabEquipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentUsageLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentUsageLogs_LabEquipments_LabEquipmentId",
                        column: x => x.LabEquipmentId,
                        principalTable: "LabEquipments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SafetyChecklistItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LabSafetyInspectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemDescription = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    IsCompliant = table.Column<bool>(type: "bit", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SafetyChecklistItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SafetyChecklistItems_LabSafetyInspections_LabSafetyInspectionId",
                        column: x => x.LabSafetyInspectionId,
                        principalTable: "LabSafetyInspections",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CheckpointScans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GuardRoundId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CheckpointName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ScanTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckpointScans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CheckpointScans_GuardRounds_GuardRoundId",
                        column: x => x.GuardRoundId,
                        principalTable: "GuardRounds",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FinanceScholarshipApplicationEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ToStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceScholarshipApplicationEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceScholarshipApplicationEvents_FinanceScholarshipApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "FinanceScholarshipApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceGoodsReceiptLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GoodsReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseOrderLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuantityAccepted = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    QuantityRejected = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceGoodsReceiptLines", x => x.Id);
                    table.CheckConstraint("CK_FinanceGoodsReceiptLines_Amounts", "[QuantityAccepted] >= 0 AND [QuantityRejected] >= 0 AND ([QuantityAccepted] + [QuantityRejected]) > 0");
                    table.ForeignKey(
                        name: "FK_FinanceGoodsReceiptLines_FinanceGoodsReceipts_GoodsReceiptId",
                        column: x => x.GoodsReceiptId,
                        principalTable: "FinanceGoodsReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceGoodsReceiptLines_FinancePurchaseOrderLines_PurchaseOrderLineId",
                        column: x => x.PurchaseOrderLineId,
                        principalTable: "FinancePurchaseOrderLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceVendorInvoiceLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorInvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseOrderLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceVendorInvoiceLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceVendorInvoiceLines_FinancePurchaseOrderLines_PurchaseOrderLineId",
                        column: x => x.PurchaseOrderLineId,
                        principalTable: "FinancePurchaseOrderLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceVendorInvoiceLines_FinanceVendorInvoices_VendorInvoiceId",
                        column: x => x.VendorInvoiceId,
                        principalTable: "FinanceVendorInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HostelAllocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HostelRoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CheckInDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CheckOutDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HostelAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HostelAllocations_HostelRooms_HostelRoomId",
                        column: x => x.HostelRoomId,
                        principalTable: "HostelRooms",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RoomInspections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HostelRoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InspectionType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InspectionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Findings = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    InspectedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomInspections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomInspections_HostelRooms_HostelRoomId",
                        column: x => x.HostelRoomId,
                        principalTable: "HostelRooms",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UtilityReadings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HostelRoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UtilityType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reading = table.Column<decimal>(type: "decimal(12,4)", nullable: false),
                    ReadingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UtilityReadings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UtilityReadings_HostelRooms_HostelRoomId",
                        column: x => x.HostelRoomId,
                        principalTable: "HostelRooms",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ApprovalRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApproverUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DelegatedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DelegationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ApproverRole = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DecidedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TimeoutAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsEscalated = table.Column<bool>(type: "bit", nullable: false),
                    EscalatedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalRecords_WorkflowSteps_StepId",
                        column: x => x.StepId,
                        principalTable: "WorkflowSteps",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FireEquipments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EquipmentCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EquipmentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BuildingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastInspectionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextInspectionDue = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FireEquipments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FireEquipments_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalTable: "Buildings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FireEquipments_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceTickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BuildingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SLADeadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsSLABreached = table.Column<bool>(type: "bit", nullable: false),
                    PhotoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceTickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceTickets_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaintenanceTickets_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalTable: "Buildings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaintenanceTickets_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PreventiveMaintenanceSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduleName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Frequency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IntervalDays = table.Column<int>(type: "int", nullable: false),
                    LastPerformedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextDueAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreventiveMaintenanceSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreventiveMaintenanceSchedules_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PreventiveMaintenanceSchedules_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RoomOccupancies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CurrentOccupants = table.Column<int>(type: "int", nullable: true),
                    MaxCapacity = table.Column<int>(type: "int", nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CurrentEvent = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BookingUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomOccupancies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomOccupancies_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SecurityIncidents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IncidentNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IncidentDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LocationBuildingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LocationRoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReportedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssignedToEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsImmutable = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityIncidents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecurityIncidents_Buildings_LocationBuildingId",
                        column: x => x.LocationBuildingId,
                        principalTable: "Buildings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SecurityIncidents_Employees_AssignedToEmployeeId",
                        column: x => x.AssignedToEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SecurityIncidents_Rooms_LocationRoomId",
                        column: x => x.LocationRoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Attendances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FacultyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Latitude = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    Longitude = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    GeofenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LocationStatus = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false, defaultValue: "Unverifiable"),
                    IsManualOverride = table.Column<bool>(type: "bit", nullable: false),
                    OverrideReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attendances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Attendances_CampusPolygons_GeofenceId",
                        column: x => x.GeofenceId,
                        principalTable: "CampusPolygons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Attendances_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Attendances_Enrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "Enrollments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Attendances_Faculties_FacultyId",
                        column: x => x.FacultyId,
                        principalTable: "Faculties",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Results",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalMarks = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ObtainedMarks = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Results", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Results_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Results_Enrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "Enrollments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VehiclePasses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    VehicleModel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VehicleColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PassType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OwnerEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OwnerStudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VisitorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IssueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehiclePasses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehiclePasses_Employees_OwnerEmployeeId",
                        column: x => x.OwnerEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VehiclePasses_Students_OwnerStudentId",
                        column: x => x.OwnerStudentId,
                        principalTable: "Students",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VehiclePasses_Visitors_VisitorId",
                        column: x => x.VisitorId,
                        principalTable: "Visitors",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InspectionChecklistItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomInspectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsCompliant = table.Column<bool>(type: "bit", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspectionChecklistItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InspectionChecklistItems_RoomInspections_RoomInspectionId",
                        column: x => x.RoomInspectionId,
                        principalTable: "RoomInspections",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FireDrills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FireEquipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DrillDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DrillType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ParticipantsCount = table.Column<int>(type: "int", nullable: false),
                    EvacuationTimeMinutes = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Observations = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsSuccessful = table.Column<bool>(type: "bit", nullable: false),
                    ConductedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FireDrills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FireDrills_FireEquipments_FireEquipmentId",
                        column: x => x.FireEquipmentId,
                        principalTable: "FireEquipments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TicketComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaintenanceTicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CommentedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommentedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketComments_MaintenanceTickets_MaintenanceTicketId",
                        column: x => x.MaintenanceTicketId,
                        principalTable: "MaintenanceTickets",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TicketEscalations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaintenanceTicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EscalationLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EscalatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EscalatedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketEscalations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketEscalations_MaintenanceTickets_MaintenanceTicketId",
                        column: x => x.MaintenanceTicketId,
                        principalTable: "MaintenanceTickets",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WorkOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkOrderNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MaintenanceTicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedToEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedToContractorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SignedOffAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SignedOffByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    LaborCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaterialCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    HoursWorked = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsPreventiveMaintenance = table.Column<bool>(type: "bit", nullable: false),
                    PMScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrders_Employees_AssignedToEmployeeId",
                        column: x => x.AssignedToEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkOrders_MaintenanceContractors_AssignedToContractorId",
                        column: x => x.AssignedToContractorId,
                        principalTable: "MaintenanceContractors",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkOrders_MaintenanceTickets_MaintenanceTicketId",
                        column: x => x.MaintenanceTicketId,
                        principalTable: "MaintenanceTickets",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkOrders_PreventiveMaintenanceSchedules_PMScheduleId",
                        column: x => x.PMScheduleId,
                        principalTable: "PreventiveMaintenanceSchedules",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "IncidentEvidences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SecurityIncidentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvidenceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentEvidences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncidentEvidences_SecurityIncidents_SecurityIncidentId",
                        column: x => x.SecurityIncidentId,
                        principalTable: "SecurityIncidents",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "IncidentWitnesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SecurityIncidentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WitnessName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Statement = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentWitnesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncidentWitnesses_SecurityIncidents_SecurityIncidentId",
                        column: x => x.SecurityIncidentId,
                        principalTable: "SecurityIncidents",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GateEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehiclePassId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VisitorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GateName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntryTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExitTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GuardEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GateEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GateEntries_Employees_GuardEmployeeId",
                        column: x => x.GuardEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GateEntries_VehiclePasses_VehiclePassId",
                        column: x => x.VehiclePassId,
                        principalTable: "VehiclePasses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GateEntries_Visitors_VisitorId",
                        column: x => x.VisitorId,
                        principalTable: "Visitors",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WorkOrderMaterials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaterialName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrderMaterials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrderMaterials_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WorkOrderTimeEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HoursWorked = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrderTimeEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrderTimeEntries_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkOrderTimeEntries_WorkOrders_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrders",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "PolicyConfigs",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "Description", "EffectiveFrom", "EffectiveTo", "Key", "Module", "TenantId", "UpdatedAt", "UpdatedBy", "Value", "ValueType" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111101"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("00000000-0000-0000-0000-000000000000"), "maximum % a dept head can change merit unilaterally", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "merit_change_max_percent", "admissions", new Guid("00000000-0000-0000-0000-000000000000"), null, null, "5", "int" },
                    { new Guid("11111111-1111-1111-1111-111111111102"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("00000000-0000-0000-0000-000000000000"), "VC approval required for merit overrides", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "merit_override_requires_vc", "admissions", new Guid("00000000-0000-0000-0000-000000000000"), null, null, "true", "bool" },
                    { new Guid("11111111-1111-1111-1111-111111111103"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("00000000-0000-0000-0000-000000000000"), "Chancellor approval required for major merit overrides", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "merit_override_requires_chancellor", "admissions", new Guid("00000000-0000-0000-0000-000000000000"), null, null, "true", "bool" },
                    { new Guid("11111111-1111-1111-1111-111111111104"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("00000000-0000-0000-0000-000000000000"), "maximum grace marks", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "grace_marks_max", "examination", new Guid("00000000-0000-0000-0000-000000000000"), null, null, "2", "int" },
                    { new Guid("11111111-1111-1111-1111-111111111105"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("00000000-0000-0000-0000-000000000000"), "attendance threshold for exam eligibility", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "attendance_hold_threshold_percent", "examination", new Guid("00000000-0000-0000-0000-000000000000"), null, null, "75", "int" },
                    { new Guid("11111111-1111-1111-1111-111111111106"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("00000000-0000-0000-0000-000000000000"), "course add/drop window", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "add_drop_window_days", "academic", new Guid("00000000-0000-0000-0000-000000000000"), null, null, "7", "int" },
                    { new Guid("11111111-1111-1111-1111-111111111107"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("00000000-0000-0000-0000-000000000000"), "late fee grace period", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "late_fee_grace_days", "finance", new Guid("00000000-0000-0000-0000-000000000000"), null, null, "3", "int" },
                    { new Guid("11111111-1111-1111-1111-111111111108"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("00000000-0000-0000-0000-000000000000"), "daily license heartbeat time", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "heartbeat_time", "license", new Guid("00000000-0000-0000-0000-000000000000"), null, null, "08:00", "string" },
                    { new Guid("11111111-1111-1111-1111-111111111109"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("00000000-0000-0000-0000-000000000000"), "lock system on first failed heartbeat", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "lockout_on_first_failure", "license", new Guid("00000000-0000-0000-0000-000000000000"), null, null, "true", "bool" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcademicHolidays_AcademicYearId_StartDate",
                table: "AcademicHolidays",
                columns: new[] { "AcademicYearId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AcademicHolidays_SemesterId",
                table: "AcademicHolidays",
                column: "SemesterId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicYears_IsCurrent",
                table: "AcademicYears",
                column: "IsCurrent");

            migrationBuilder.CreateIndex(
                name: "IX_AccreditationAuditLogs_AccreditationCycleId",
                table: "AccreditationAuditLogs",
                column: "AccreditationCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_AccreditationBodies_Code",
                table: "AccreditationBodies",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccreditationCommittees_AccreditationCycleId",
                table: "AccreditationCommittees",
                column: "AccreditationCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_AccreditationCriteria_AccreditationBodyId",
                table: "AccreditationCriteria",
                column: "AccreditationBodyId");

            migrationBuilder.CreateIndex(
                name: "IX_AccreditationCycles_AccreditationBodyId",
                table: "AccreditationCycles",
                column: "AccreditationBodyId");

            migrationBuilder.CreateIndex(
                name: "IX_AccreditationEvidences_AccreditationCriterionId",
                table: "AccreditationEvidences",
                column: "AccreditationCriterionId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityParticipations_ActivityId_StudentId",
                table: "ActivityParticipations",
                columns: new[] { "ActivityId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplications_Cnic",
                table: "AdmissionApplications",
                column: "Cnic",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplications_ProgramId_QuotaType_ApplicationStatus",
                table: "AdmissionApplications",
                columns: new[] { "ProgramId", "QuotaType", "ApplicationStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionLeads_Email",
                table: "AdmissionLeads",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionLeads_Phone",
                table: "AdmissionLeads",
                column: "Phone");

            migrationBuilder.CreateIndex(
                name: "IX_AdvisorMeetings_AdvisorAssignmentId",
                table: "AdvisorMeetings",
                column: "AdvisorAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AdvisorNotes_AdvisorAssignmentId",
                table: "AdvisorNotes",
                column: "AdvisorAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AgenticGradeRuns_Confidence",
                table: "AgenticGradeRuns",
                column: "Confidence");

            migrationBuilder.CreateIndex(
                name: "IX_AgenticGradeRuns_ScanId_Status",
                table: "AgenticGradeRuns",
                columns: new[] { "ScanId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AlertRules_RuleName",
                table: "AlertRules",
                column: "RuleName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AlumniDonations_AlumniProfileId",
                table: "AlumniDonations",
                column: "AlumniProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_AlumniDonations_ReceiptNumber",
                table: "AlumniDonations",
                column: "ReceiptNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AlumniProfiles_AlumniId",
                table: "AlumniProfiles",
                column: "AlumniId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AlumniProfiles_UserId",
                table: "AlumniProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnonymousFeedbackSubmissions_CourseId_FacultyId",
                table: "AnonymousFeedbackSubmissions",
                columns: new[] { "CourseId", "FacultyId" });

            migrationBuilder.CreateIndex(
                name: "IX_AnonymousFeedbackSubmissions_SubmittedAt",
                table: "AnonymousFeedbackSubmissions",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationDocuments_ApplicationId_DocumentType",
                table: "ApplicationDocuments",
                columns: new[] { "ApplicationId", "DocumentType" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRecords_StepId",
                table: "ApprovalRecords",
                column: "StepId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentComponents_SchemeId_OrderIndex",
                table: "AssessmentComponents",
                columns: new[] { "SchemeId", "OrderIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentSchemes_CourseId_SemesterId",
                table: "AssessmentSchemes",
                columns: new[] { "CourseId", "SemesterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assets_AssetCategoryId",
                table: "Assets",
                column: "AssetCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetVerifications_VerificationCycleId",
                table: "AssetVerifications",
                column: "VerificationCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_CourseId",
                table: "Attendances",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_EnrollmentId",
                table: "Attendances",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_FacultyId",
                table: "Attendances",
                column: "FacultyId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_GeofenceId",
                table: "Attendances",
                column: "GeofenceId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditFindings_ControlId",
                table: "AuditFindings",
                column: "ControlId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_TenantId",
                table: "AuditLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BackupRecords_ConfigurationId",
                table: "BackupRecords",
                column: "ConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_Batches_DepartmentId",
                table: "Batches",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_BIReports_DataWarehouseTableId",
                table: "BIReports",
                column: "DataWarehouseTableId");

            migrationBuilder.CreateIndex(
                name: "IX_BookCopies_Barcode",
                table: "BookCopies",
                column: "Barcode",
                unique: true,
                filter: "[Barcode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BookCopies_LibraryBookId",
                table: "BookCopies",
                column: "LibraryBookId");

            migrationBuilder.CreateIndex(
                name: "IX_Buildings_CampusId",
                table: "Buildings",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_CalibrationRecords_LabEquipmentId",
                table: "CalibrationRecords",
                column: "LabEquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CampusConfigurations_CampusId",
                table: "CampusConfigurations",
                column: "CampusId");

            migrationBuilder.CreateIndex(
                name: "IX_Campuses_CampusCode",
                table: "Campuses",
                column: "CampusCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CampusPolygons_TenantId_IsActive",
                table: "CampusPolygons",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CareerEventRegistrations_CareerEventId",
                table: "CareerEventRegistrations",
                column: "CareerEventId");

            migrationBuilder.CreateIndex(
                name: "IX_CareerEvents_EventDate",
                table: "CareerEvents",
                column: "EventDate");

            migrationBuilder.CreateIndex(
                name: "IX_ChartOfAccounts_AccountCode",
                table: "ChartOfAccounts",
                column: "AccountCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatbotMessages_ChatbotConversationId",
                table: "ChatbotMessages",
                column: "ChatbotConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_CheckpointScans_GuardRoundId",
                table: "CheckpointScans",
                column: "GuardRoundId");

            migrationBuilder.CreateIndex(
                name: "IX_CisUploads_CourseId_ProcessingStatus",
                table: "CisUploads",
                columns: new[] { "CourseId", "ProcessingStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_CisUploads_HumanReviewStatus",
                table: "CisUploads",
                column: "HumanReviewStatus");

            migrationBuilder.CreateIndex(
                name: "IX_CloPlomappings_CourseId",
                table: "CloPlomappings",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CLOPOMappings_CourseLearningOutcomeId",
                table: "CLOPOMappings",
                column: "CourseLearningOutcomeId");

            migrationBuilder.CreateIndex(
                name: "IX_CLOPOMappings_ProgramOutcomeId",
                table: "CLOPOMappings",
                column: "ProgramOutcomeId");

            migrationBuilder.CreateIndex(
                name: "IX_CommitteeMeetings_AccreditationCommitteeId",
                table: "CommitteeMeetings",
                column: "AccreditationCommitteeId");

            migrationBuilder.CreateIndex(
                name: "IX_CommitteeMembers_AccreditationCommitteeId",
                table: "CommitteeMembers",
                column: "AccreditationCommitteeId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceControls_FrameworkId",
                table: "ComplianceControls",
                column: "FrameworkId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigAuditLogs_ConfigurationEntryId",
                table: "ConfigAuditLogs",
                column: "ConfigurationEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigVersions_ConfigurationEntryId",
                table: "ConfigVersions",
                column: "ConfigurationEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsentLedgers_TenantId_RevokedAt",
                table: "ConsentLedgers",
                columns: new[] { "TenantId", "RevokedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ConsentLedgers_TenantId_UserId_DataCategory",
                table: "ConsentLedgers",
                columns: new[] { "TenantId", "UserId", "DataCategory" });

            migrationBuilder.CreateIndex(
                name: "IX_ConsumableRequisitions_LabConsumableId",
                table: "ConsumableRequisitions",
                column: "LabConsumableId");

            migrationBuilder.CreateIndex(
                name: "IX_ControlEvidences_ControlId",
                table: "ControlEvidences",
                column: "ControlId");

            migrationBuilder.CreateIndex(
                name: "IX_CorrectiveActionPlans_FindingId",
                table: "CorrectiveActionPlans",
                column: "FindingId");

            migrationBuilder.CreateIndex(
                name: "IX_CostCenters_TenantId_Code",
                table: "CostCenters",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseAssignments_CourseId",
                table: "CourseAssignments",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseAssignments_FacultyId",
                table: "CourseAssignments",
                column: "FacultyId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseGrades_CourseId_StudentId",
                table: "CourseGrades",
                columns: new[] { "CourseId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseGrades_ResultStatus",
                table: "CourseGrades",
                column: "ResultStatus");

            migrationBuilder.CreateIndex(
                name: "IX_CourseGrades_StudentId_SemesterId",
                table: "CourseGrades",
                columns: new[] { "StudentId", "SemesterId" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseLearningOutcomes_Code",
                table: "CourseLearningOutcomes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseMarks_AssessmentComponentId",
                table: "CourseMarks",
                column: "AssessmentComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseMarks_CourseId_StudentId_AssessmentComponentId",
                table: "CourseMarks",
                columns: new[] { "CourseId", "StudentId", "AssessmentComponentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseReEnrollments_Status",
                table: "CourseReEnrollments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CourseReEnrollments_StudentId_CourseId_TargetSemesterId",
                table: "CourseReEnrollments",
                columns: new[] { "StudentId", "CourseId", "TargetSemesterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Courses_Code",
                table: "Courses",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_DepartmentId",
                table: "Courses",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_PrerequisiteCourseId",
                table: "Courses",
                column: "PrerequisiteCourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditTransfers_StudentId",
                table: "CreditTransfers",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_CrossDeptPermissionRequests_PermissionId",
                table: "CrossDeptPermissionRequests",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_CrossDeptPermissionRequests_Status",
                table: "CrossDeptPermissionRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumChangeHistory_CurriculumVersionId_ChangedAt",
                table: "CurriculumChangeHistory",
                columns: new[] { "CurriculumVersionId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumCourses_CurriculumVersionId_CourseId_Semester",
                table: "CurriculumCourses",
                columns: new[] { "CurriculumVersionId", "CourseId", "Semester" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumPrerequisites_CurriculumVersionId_CourseId_PrerequisiteCourseId",
                table: "CurriculumPrerequisites",
                columns: new[] { "CurriculumVersionId", "CourseId", "PrerequisiteCourseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumVersions_ProgramId_VersionNumber",
                table: "CurriculumVersions",
                columns: new[] { "ProgramId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumVersions_ProgramId_YearEffective",
                table: "CurriculumVersions",
                columns: new[] { "ProgramId", "YearEffective" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DashboardSubscriptions_DashboardId",
                table: "DashboardSubscriptions",
                column: "DashboardId");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardWidgets_DashboardId",
                table: "DashboardWidgets",
                column: "DashboardId");

            migrationBuilder.CreateIndex(
                name: "IX_DataExportLogs_BIReportId",
                table: "DataExportLogs",
                column: "BIReportId");

            migrationBuilder.CreateIndex(
                name: "IX_DataExportLogs_DashboardId",
                table: "DataExportLogs",
                column: "DashboardId");

            migrationBuilder.CreateIndex(
                name: "IX_DataWarehouseTables_TableName",
                table: "DataWarehouseTables",
                column: "TableName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DegreeAudits_ProgramId",
                table: "DegreeAudits",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_DegreeAudits_RequirementId",
                table: "DegreeAudits",
                column: "RequirementId");

            migrationBuilder.CreateIndex(
                name: "IX_DegreeAudits_StudentId",
                table: "DegreeAudits",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_DegreeRequirements_ProgramId",
                table: "DegreeRequirements",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_TenantId_Code",
                table: "Departments",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentExpiryTrackers_ForeignStudentRecordId",
                table: "DocumentExpiryTrackers",
                column: "ForeignStudentRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTemplates_TenantId_CreatedAt",
                table: "DocumentTemplates",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DriveRegistrations_CampusDriveId",
                table: "DriveRegistrations",
                column: "CampusDriveId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverShifts_TransportDriverId",
                table: "DriverShifts",
                column: "TransportDriverId");

            migrationBuilder.CreateIndex(
                name: "IX_DWColumns_DataWarehouseTableId",
                table: "DWColumns",
                column: "DataWarehouseTableId");

            migrationBuilder.CreateIndex(
                name: "IX_ElectionPositions_ElectionId",
                table: "ElectionPositions",
                column: "ElectionId");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyAlerts_SentAt",
                table: "EmergencyAlerts",
                column: "SentAt");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyContacts_Phone",
                table: "EmergencyContacts",
                column: "Phone");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeContracts_EmployeeId",
                table: "EmployeeContracts",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDocuments_EmployeeId",
                table: "EmployeeDocuments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Email",
                table: "Employees",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_EmployeeCode",
                table: "Employees",
                column: "EmployeeCode",
                unique: true,
                filter: "[EmployeeCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Employers_ContactEmail",
                table: "Employers",
                column: "ContactEmail");

            migrationBuilder.CreateIndex(
                name: "IX_EnergyReadings_BuildingId",
                table: "EnergyReadings",
                column: "BuildingId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_CourseId",
                table: "Enrollments",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_StudentId_CourseId",
                table: "Enrollments",
                columns: new[] { "StudentId", "CourseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentBookings_LabEquipmentId",
                table: "EquipmentBookings",
                column: "LabEquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentMaintenances_LabEquipmentId",
                table: "EquipmentMaintenances",
                column: "LabEquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentUsageLogs_LabEquipmentId",
                table: "EquipmentUsageLogs",
                column: "LabEquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ErasureRequests_TenantId_Status_DeadlineAt",
                table: "ErasureRequests",
                columns: new[] { "TenantId", "Status", "DeadlineAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceVersions_AccreditationEvidenceId",
                table: "EvidenceVersions",
                column: "AccreditationEvidenceId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamPaperBatches_ExamId_Status",
                table: "ExamPaperBatches",
                columns: new[] { "ExamId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamPaperScans_BatchId_StudentId",
                table: "ExamPaperScans",
                columns: new[] { "BatchId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamPaperScans_QrPayload",
                table: "ExamPaperScans",
                column: "QrPayload",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamRegistrations_StudentId_CourseId",
                table: "ExamRegistrations",
                columns: new[] { "StudentId", "CourseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamSchedules_CourseId",
                table: "ExamSchedules",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSeatings_ExamScheduleId_SeatNumber",
                table: "ExamSeatings",
                columns: new[] { "ExamScheduleId", "SeatNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamSeatings_StudentId",
                table: "ExamSeatings",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRateSnapshots_TenantId_BaseCurrency_QuoteCurrency_ValidForDate",
                table: "ExchangeRateSnapshots",
                columns: new[] { "TenantId", "BaseCurrency", "QuoteCurrency", "ValidForDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExtensionInstallations_ExtensionId",
                table: "ExtensionInstallations",
                column: "ExtensionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExtensionReviews_ExtensionId",
                table: "ExtensionReviews",
                column: "ExtensionId");

            migrationBuilder.CreateIndex(
                name: "IX_Faculties_DepartmentId",
                table: "Faculties",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_FacultyEngagements_FacultyProfileId",
                table: "FacultyEngagements",
                column: "FacultyProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_FeeComponents_FeeStructureId_OrderIndex",
                table: "FeeComponents",
                columns: new[] { "FeeStructureId", "OrderIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeeDemands_TenantId_DemandNumber",
                table: "FeeDemands",
                columns: new[] { "TenantId", "DemandNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeeDemands_TenantId_Status_DueDate",
                table: "FeeDemands",
                columns: new[] { "TenantId", "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_FeeDemands_TenantId_StudentId_Status",
                table: "FeeDemands",
                columns: new[] { "TenantId", "StudentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FeeInstallments_FeeDemandId",
                table: "FeeInstallments",
                column: "FeeDemandId");

            migrationBuilder.CreateIndex(
                name: "IX_FeeInstallments_TenantId_FeeDemandId_Sequence",
                table: "FeeInstallments",
                columns: new[] { "TenantId", "FeeDemandId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeePayments_StudentFeeId",
                table: "FeePayments",
                column: "StudentFeeId");

            migrationBuilder.CreateIndex(
                name: "IX_FeeStructures_AcademicYearId_ProgramId_Semester_FeeType",
                table: "FeeStructures",
                columns: new[] { "AcademicYearId", "ProgramId", "Semester", "FeeType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FerpaAuditLogs_TenantId_AccessedAt",
                table: "FerpaAuditLogs",
                columns: new[] { "TenantId", "AccessedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FerpaAuditLogs_TenantId_StudentId_AccessedAt",
                table: "FerpaAuditLogs",
                columns: new[] { "TenantId", "StudentId", "AccessedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceAccountingStandardPolicies_TenantId_StandardCode_PolicyCode_EffectiveFrom",
                table: "FinanceAccountingStandardPolicies",
                columns: new[] { "TenantId", "StandardCode", "PolicyCode", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceAssetAccountingEvents_JournalEntryId",
                table: "FinanceAssetAccountingEvents",
                column: "JournalEntryId",
                unique: true,
                filter: "[JournalEntryId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceAssetAccountingEvents_TenantId_AssetId_OccurredAt",
                table: "FinanceAssetAccountingEvents",
                columns: new[] { "TenantId", "AssetId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceAuditEvidencePackages_TenantId_GeneratedAt",
                table: "FinanceAuditEvidencePackages",
                columns: new[] { "TenantId", "GeneratedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceAuditorEngagements_TenantId_ExpiresAt",
                table: "FinanceAuditorEngagements",
                columns: new[] { "TenantId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceAuditorEngagements_TokenHash",
                table: "FinanceAuditorEngagements",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceAuditSamples_TenantId_AuditorEngagementId_CreatedAt",
                table: "FinanceAuditSamples",
                columns: new[] { "TenantId", "AuditorEngagementId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceAuditSeals_TenantId_SealedAt",
                table: "FinanceAuditSeals",
                columns: new[] { "TenantId", "SealedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceBankAccounts_TenantId_AccountReference",
                table: "FinanceBankAccounts",
                columns: new[] { "TenantId", "AccountReference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceBankFeedTransactions_FinanceBankAccountId",
                table: "FinanceBankFeedTransactions",
                column: "FinanceBankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceBankFeedTransactions_TenantId_FinanceBankAccountId_ExternalId",
                table: "FinanceBankFeedTransactions",
                columns: new[] { "TenantId", "FinanceBankAccountId", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceBankFeedTransactions_TenantId_Status_BookingDate",
                table: "FinanceBankFeedTransactions",
                columns: new[] { "TenantId", "Status", "BookingDate" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceConsolidationGroups_TenantId_Name",
                table: "FinanceConsolidationGroups",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceConsolidationMembers_ConsolidationGroupId",
                table: "FinanceConsolidationMembers",
                column: "ConsolidationGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceConsolidationMembers_TenantId_ConsolidationGroupId_ReportingEntityId_EffectiveFrom",
                table: "FinanceConsolidationMembers",
                columns: new[] { "TenantId", "ConsolidationGroupId", "ReportingEntityId", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceConsolidationMemoLines_ConsolidationRunId",
                table: "FinanceConsolidationMemoLines",
                column: "ConsolidationRunId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceConsolidationMemoLines_TenantId_ConsolidationRunId_ChartOfAccountId",
                table: "FinanceConsolidationMemoLines",
                columns: new[] { "TenantId", "ConsolidationRunId", "ChartOfAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceConsolidationRuns_TenantId_ConsolidationGroupId_CreatedAt",
                table: "FinanceConsolidationRuns",
                columns: new[] { "TenantId", "ConsolidationGroupId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceControlEvaluationBatches_TenantId_EvaluatedAt",
                table: "FinanceControlEvaluationBatches",
                columns: new[] { "TenantId", "EvaluatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceControlEvaluationBatches_TenantId_ReferenceType_ReferenceId",
                table: "FinanceControlEvaluationBatches",
                columns: new[] { "TenantId", "ReferenceType", "ReferenceId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceEliminationRules_TenantId_ConsolidationGroupId_IsActive",
                table: "FinanceEliminationRules",
                columns: new[] { "TenantId", "ConsolidationGroupId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceFleetCostPostings_JournalEntryId",
                table: "FinanceFleetCostPostings",
                column: "JournalEntryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceFleetCostPostings_TenantId_TransportVehicleId_CostDate",
                table: "FinanceFleetCostPostings",
                columns: new[] { "TenantId", "TransportVehicleId", "CostDate" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceFleetProfiles_TenantId_NextServiceAt_Status",
                table: "FinanceFleetProfiles",
                columns: new[] { "TenantId", "NextServiceAt", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceFleetProfiles_TenantId_TransportVehicleId",
                table: "FinanceFleetProfiles",
                columns: new[] { "TenantId", "TransportVehicleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceFleetTrips_TenantId_TransportVehicleId_TripDate",
                table: "FinanceFleetTrips",
                columns: new[] { "TenantId", "TransportVehicleId", "TripDate" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceForeignCurrencyTransactions_TenantId_ForeignCurrency_CreatedAt",
                table: "FinanceForeignCurrencyTransactions",
                columns: new[] { "TenantId", "ForeignCurrency", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceForeignCurrencyTransactions_TenantId_SourceType_SourceId",
                table: "FinanceForeignCurrencyTransactions",
                columns: new[] { "TenantId", "SourceType", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceGoodsReceiptLines_GoodsReceiptId",
                table: "FinanceGoodsReceiptLines",
                column: "GoodsReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceGoodsReceiptLines_PurchaseOrderLineId",
                table: "FinanceGoodsReceiptLines",
                column: "PurchaseOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceGoodsReceiptLines_TenantId_PurchaseOrderLineId",
                table: "FinanceGoodsReceiptLines",
                columns: new[] { "TenantId", "PurchaseOrderLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceGoodsReceipts_PurchaseOrderId",
                table: "FinanceGoodsReceipts",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceGoodsReceipts_TenantId_Number",
                table: "FinanceGoodsReceipts",
                columns: new[] { "TenantId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceGrants_TenantId_DonorName_Purpose",
                table: "FinanceGrants",
                columns: new[] { "TenantId", "DonorName", "Purpose" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceGratuityAssumptions_TenantId_EffectiveFrom",
                table: "FinanceGratuityAssumptions",
                columns: new[] { "TenantId", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceGratuityValuations_TenantId_ValuationDate",
                table: "FinanceGratuityValuations",
                columns: new[] { "TenantId", "ValuationDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceInsuranceClaims_FinanceInsurancePolicyId",
                table: "FinanceInsuranceClaims",
                column: "FinanceInsurancePolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceInsuranceClaims_TenantId_ClaimNumber",
                table: "FinanceInsuranceClaims",
                columns: new[] { "TenantId", "ClaimNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceInsuranceClaims_TenantId_Status_FiledAt",
                table: "FinanceInsuranceClaims",
                columns: new[] { "TenantId", "Status", "FiledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceInsurancePolicies_TenantId_PolicyNumber",
                table: "FinanceInsurancePolicies",
                columns: new[] { "TenantId", "PolicyNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceInsurancePolicies_TenantId_RenewalDate_Status",
                table: "FinanceInsurancePolicies",
                columns: new[] { "TenantId", "RenewalDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceInventoryItems_TenantId_ItemCode",
                table: "FinanceInventoryItems",
                columns: new[] { "TenantId", "ItemCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceInventoryItems_TenantId_Status_OnHandQuantity",
                table: "FinanceInventoryItems",
                columns: new[] { "TenantId", "Status", "OnHandQuantity" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceInventoryMovements_FinanceInventoryItemId",
                table: "FinanceInventoryMovements",
                column: "FinanceInventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceInventoryMovements_TenantId_FinanceInventoryItemId_MovementDate",
                table: "FinanceInventoryMovements",
                columns: new[] { "TenantId", "FinanceInventoryItemId", "MovementDate" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceInvestmentBenchmarks_TenantId_Name_ObservationDate",
                table: "FinanceInvestmentBenchmarks",
                columns: new[] { "TenantId", "Name", "ObservationDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceInvestmentFunds_TenantId_Name",
                table: "FinanceInvestmentFunds",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceInvestmentHoldings_FinanceInvestmentFundId",
                table: "FinanceInvestmentHoldings",
                column: "FinanceInvestmentFundId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceInvestmentHoldings_TenantId_Issuer_InstrumentType",
                table: "FinanceInvestmentHoldings",
                columns: new[] { "TenantId", "Issuer", "InstrumentType" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceInvestmentHoldings_TenantId_MaturityDate_Status",
                table: "FinanceInvestmentHoldings",
                columns: new[] { "TenantId", "MaturityDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceInvestmentTransactions_FinanceInvestmentHoldingId",
                table: "FinanceInvestmentTransactions",
                column: "FinanceInvestmentHoldingId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceInvestmentTransactions_TenantId_FinanceInvestmentHoldingId_TransactionDate",
                table: "FinanceInvestmentTransactions",
                columns: new[] { "TenantId", "FinanceInvestmentHoldingId", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceLedgerSignatures_TenantId_JournalEntryId",
                table: "FinanceLedgerSignatures",
                columns: new[] { "TenantId", "JournalEntryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceLibraryAcquisitions_TenantId_Status_AcquiredAt",
                table: "FinanceLibraryAcquisitions",
                columns: new[] { "TenantId", "Status", "AcquiredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceLibraryFines_TenantId_StudentId_Status",
                table: "FinanceLibraryFines",
                columns: new[] { "TenantId", "StudentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancePaymentPlanConfigurations_TenantId_PlanType",
                table: "FinancePaymentPlanConfigurations",
                columns: new[] { "TenantId", "PlanType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancePaymentPlans_FeeDemandId",
                table: "FinancePaymentPlans",
                column: "FeeDemandId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancePaymentPlans_TenantId_FeeDemandId",
                table: "FinancePaymentPlans",
                columns: new[] { "TenantId", "FeeDemandId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancePaymentPlans_TenantId_StudentId_Status",
                table: "FinancePaymentPlans",
                columns: new[] { "TenantId", "StudentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancePayments_FeeDemandId",
                table: "FinancePayments",
                column: "FeeDemandId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancePayments_TenantId_FeeDemandId_Status",
                table: "FinancePayments",
                columns: new[] { "TenantId", "FeeDemandId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancePayments_TenantId_IdempotencyKey",
                table: "FinancePayments",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancePayments_TenantId_WebhookId",
                table: "FinancePayments",
                columns: new[] { "TenantId", "WebhookId" },
                unique: true,
                filter: "[WebhookId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FinancePayrollBankInstructions_TenantId_EmployeeId",
                table: "FinancePayrollBankInstructions",
                columns: new[] { "TenantId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancePayrollPaymentFiles_TenantId_PayrollPostingId_Format",
                table: "FinancePayrollPaymentFiles",
                columns: new[] { "TenantId", "PayrollPostingId", "Format" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancePayrollPostingLines_FinancePayrollPostingId",
                table: "FinancePayrollPostingLines",
                column: "FinancePayrollPostingId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancePayrollPostingLines_TenantId_CostCenterId",
                table: "FinancePayrollPostingLines",
                columns: new[] { "TenantId", "CostCenterId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancePayrollPostingLines_TenantId_FinancePayrollPostingId_EmployeeId",
                table: "FinancePayrollPostingLines",
                columns: new[] { "TenantId", "FinancePayrollPostingId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancePayrollPostings_TenantId_Period",
                table: "FinancePayrollPostings",
                columns: new[] { "TenantId", "Period" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancePettyCashCounts_FinancePettyCashFundId",
                table: "FinancePettyCashCounts",
                column: "FinancePettyCashFundId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancePettyCashCounts_TenantId_FinancePettyCashFundId_CountedAt",
                table: "FinancePettyCashCounts",
                columns: new[] { "TenantId", "FinancePettyCashFundId", "CountedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancePettyCashExpenses_FinancePettyCashFundId",
                table: "FinancePettyCashExpenses",
                column: "FinancePettyCashFundId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancePettyCashExpenses_TenantId_FinancePettyCashFundId_SpentAt",
                table: "FinancePettyCashExpenses",
                columns: new[] { "TenantId", "FinancePettyCashFundId", "SpentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancePettyCashFunds_TenantId_DepartmentId",
                table: "FinancePettyCashFunds",
                columns: new[] { "TenantId", "DepartmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancePettyCashFunds_TenantId_IsActive",
                table: "FinancePettyCashFunds",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancePqcKeyCeremonies_TenantId_Purpose_Status",
                table: "FinancePqcKeyCeremonies",
                columns: new[] { "TenantId", "Purpose", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancePqcKeyCeremonyApprovals_CeremonyId",
                table: "FinancePqcKeyCeremonyApprovals",
                column: "CeremonyId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancePqcKeyCeremonyApprovals_TenantId_CeremonyId_ApprovedByUserId",
                table: "FinancePqcKeyCeremonyApprovals",
                columns: new[] { "TenantId", "CeremonyId", "ApprovedByUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancePqcKeys_TenantId_Purpose_IsActive_ExpiresAt",
                table: "FinancePqcKeys",
                columns: new[] { "TenantId", "Purpose", "IsActive", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancePqcKeys_TenantId_Purpose_KeyId",
                table: "FinancePqcKeys",
                columns: new[] { "TenantId", "Purpose", "KeyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancePurchaseOrderLines_PurchaseOrderId",
                table: "FinancePurchaseOrderLines",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancePurchaseOrderLines_TenantId_PurchaseOrderId",
                table: "FinancePurchaseOrderLines",
                columns: new[] { "TenantId", "PurchaseOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancePurchaseOrders_TenantId_Number",
                table: "FinancePurchaseOrders",
                columns: new[] { "TenantId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancePurchaseOrders_TenantId_VendorId_Status",
                table: "FinancePurchaseOrders",
                columns: new[] { "TenantId", "VendorId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancePurchaseOrders_VendorId",
                table: "FinancePurchaseOrders",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceRegulatorySchemas_TenantId_Code_IsActive",
                table: "FinanceRegulatorySchemas",
                columns: new[] { "TenantId", "Code", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceRegulatorySchemas_TenantId_Code_Version",
                table: "FinanceRegulatorySchemas",
                columns: new[] { "TenantId", "Code", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceRegulatorySubmissions_TenantId_RegulatorySchemaId_GeneratedAt",
                table: "FinanceRegulatorySubmissions",
                columns: new[] { "TenantId", "RegulatorySchemaId", "GeneratedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceReportingEntities_TenantId_Code",
                table: "FinanceReportingEntities",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceReportingEntities_TenantId_ParentEntityId_IsActive",
                table: "FinanceReportingEntities",
                columns: new[] { "TenantId", "ParentEntityId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceRestatementMemoLines_RestatementRunId",
                table: "FinanceRestatementMemoLines",
                column: "RestatementRunId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceRestatementMemoLines_TenantId_RestatementRunId_ChartOfAccountId",
                table: "FinanceRestatementMemoLines",
                columns: new[] { "TenantId", "RestatementRunId", "ChartOfAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceRestatementRules_TenantId_SourceStandard_TargetStandard_IsActive",
                table: "FinanceRestatementRules",
                columns: new[] { "TenantId", "SourceStandard", "TargetStandard", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceRestatementRuns_TenantId_SourceStandard_TargetStandard_CreatedAt",
                table: "FinanceRestatementRuns",
                columns: new[] { "TenantId", "SourceStandard", "TargetStandard", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceScholarshipApplicationEvents_ApplicationId",
                table: "FinanceScholarshipApplicationEvents",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceScholarshipApplicationEvents_TenantId_ApplicationId_OccurredAt",
                table: "FinanceScholarshipApplicationEvents",
                columns: new[] { "TenantId", "ApplicationId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceScholarshipApplications_ProgramId",
                table: "FinanceScholarshipApplications",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceScholarshipApplications_TenantId_ProgramId_StudentId",
                table: "FinanceScholarshipApplications",
                columns: new[] { "TenantId", "ProgramId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceScholarshipApplications_TenantId_Status_AppliedAt",
                table: "FinanceScholarshipApplications",
                columns: new[] { "TenantId", "Status", "AppliedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceScholarshipAwards_GrantId",
                table: "FinanceScholarshipAwards",
                column: "GrantId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceScholarshipAwards_TenantId_ApplicationId",
                table: "FinanceScholarshipAwards",
                columns: new[] { "TenantId", "ApplicationId" },
                unique: true,
                filter: "[ApplicationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceScholarshipAwards_TenantId_GrantId",
                table: "FinanceScholarshipAwards",
                columns: new[] { "TenantId", "GrantId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceScholarshipAwards_TenantId_StudentId_AwardedAt",
                table: "FinanceScholarshipAwards",
                columns: new[] { "TenantId", "StudentId", "AwardedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceScholarshipPrograms_GrantId",
                table: "FinanceScholarshipPrograms",
                column: "GrantId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceScholarshipPrograms_TenantId_Name",
                table: "FinanceScholarshipPrograms",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceScholarshipPrograms_TenantId_Status_ClosesAt",
                table: "FinanceScholarshipPrograms",
                columns: new[] { "TenantId", "Status", "ClosesAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceSecurityDepositDeductions_SecurityDepositId",
                table: "FinanceSecurityDepositDeductions",
                column: "SecurityDepositId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceSecurityDepositDeductions_TenantId_SecurityDepositId_CreatedAt",
                table: "FinanceSecurityDepositDeductions",
                columns: new[] { "TenantId", "SecurityDepositId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceSecurityDeposits_TenantId_StudentId_Status",
                table: "FinanceSecurityDeposits",
                columns: new[] { "TenantId", "StudentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceSodRules_TenantId_Code",
                table: "FinanceSodRules",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceStandingPurchaseOrders_TenantId_Number",
                table: "FinanceStandingPurchaseOrders",
                columns: new[] { "TenantId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceStandingPurchaseOrders_TenantId_Status_EndsOn",
                table: "FinanceStandingPurchaseOrders",
                columns: new[] { "TenantId", "Status", "EndsOn" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceStandingPurchaseOrders_VendorId",
                table: "FinanceStandingPurchaseOrders",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceStandingPurchaseOrderSpend_StandingPurchaseOrderId",
                table: "FinanceStandingPurchaseOrderSpend",
                column: "StandingPurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceStandingPurchaseOrderSpend_TenantId_StandingPurchaseOrderId_SpentAt",
                table: "FinanceStandingPurchaseOrderSpend",
                columns: new[] { "TenantId", "StandingPurchaseOrderId", "SpentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceTaxRules_TenantId_TaxType_Jurisdiction_VendorCategory_EffectiveFrom",
                table: "FinanceTaxRules",
                columns: new[] { "TenantId", "TaxType", "Jurisdiction", "VendorCategory", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceThreeWayMatches_TenantId_VendorInvoiceId",
                table: "FinanceThreeWayMatches",
                columns: new[] { "TenantId", "VendorInvoiceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceUtilityAllocations_FinanceUtilityBillId",
                table: "FinanceUtilityAllocations",
                column: "FinanceUtilityBillId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceUtilityAllocations_TenantId_FinanceUtilityBillId_CostCenterId",
                table: "FinanceUtilityAllocations",
                columns: new[] { "TenantId", "FinanceUtilityBillId", "CostCenterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceUtilityBills_TenantId_AccountNumber_BillingPeriodStart_BillingPeriodEnd",
                table: "FinanceUtilityBills",
                columns: new[] { "TenantId", "AccountNumber", "BillingPeriodStart", "BillingPeriodEnd" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceUtilityBills_TenantId_Status_DueDate",
                table: "FinanceUtilityBills",
                columns: new[] { "TenantId", "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVendorContacts_TenantId_VendorId",
                table: "FinanceVendorContacts",
                columns: new[] { "TenantId", "VendorId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVendorContacts_VendorId",
                table: "FinanceVendorContacts",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVendorContracts_TenantId_ContractNumber",
                table: "FinanceVendorContracts",
                columns: new[] { "TenantId", "ContractNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVendorContracts_TenantId_Status_EndsOn",
                table: "FinanceVendorContracts",
                columns: new[] { "TenantId", "Status", "EndsOn" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVendorContracts_VendorId",
                table: "FinanceVendorContracts",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVendorDocuments_TenantId_VendorId_UploadedAt",
                table: "FinanceVendorDocuments",
                columns: new[] { "TenantId", "VendorId", "UploadedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVendorDocuments_VendorId",
                table: "FinanceVendorDocuments",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVendorInvoiceLines_PurchaseOrderLineId",
                table: "FinanceVendorInvoiceLines",
                column: "PurchaseOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVendorInvoiceLines_TenantId_VendorInvoiceId",
                table: "FinanceVendorInvoiceLines",
                columns: new[] { "TenantId", "VendorInvoiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVendorInvoiceLines_VendorInvoiceId",
                table: "FinanceVendorInvoiceLines",
                column: "VendorInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVendorInvoices_PurchaseOrderId",
                table: "FinanceVendorInvoices",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVendorInvoices_TenantId_VendorId_InvoiceNumber",
                table: "FinanceVendorInvoices",
                columns: new[] { "TenantId", "VendorId", "InvoiceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVendorInvoices_VendorId",
                table: "FinanceVendorInvoices",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVendors_TenantId_LegalName",
                table: "FinanceVendors",
                columns: new[] { "TenantId", "LegalName" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVendors_TenantId_Status_Category",
                table: "FinanceVendors",
                columns: new[] { "TenantId", "Status", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVoucherApprovalRules_TenantId_Currency_MinimumAmount",
                table: "FinanceVoucherApprovalRules",
                columns: new[] { "TenantId", "Currency", "MinimumAmount" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVoucherApprovals_FinanceVoucherId",
                table: "FinanceVoucherApprovals",
                column: "FinanceVoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVoucherApprovals_TenantId_FinanceVoucherId_ApproverRole",
                table: "FinanceVoucherApprovals",
                columns: new[] { "TenantId", "FinanceVoucherId", "ApproverRole" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVoucherApprovals_TenantId_FinanceVoucherId_ApproverUserId",
                table: "FinanceVoucherApprovals",
                columns: new[] { "TenantId", "FinanceVoucherId", "ApproverUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVoucherAttachments_TenantId_FinanceVoucherId",
                table: "FinanceVoucherAttachments",
                columns: new[] { "TenantId", "FinanceVoucherId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVoucherAttachments_TenantId_Sha256",
                table: "FinanceVoucherAttachments",
                columns: new[] { "TenantId", "Sha256" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVoucherLines_FinanceVoucherId",
                table: "FinanceVoucherLines",
                column: "FinanceVoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVoucherLines_TenantId_ChartOfAccountId",
                table: "FinanceVoucherLines",
                columns: new[] { "TenantId", "ChartOfAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVoucherLines_TenantId_FinanceVoucherId",
                table: "FinanceVoucherLines",
                columns: new[] { "TenantId", "FinanceVoucherId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVoucherReversals_TenantId_OriginalVoucherId",
                table: "FinanceVoucherReversals",
                columns: new[] { "TenantId", "OriginalVoucherId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVoucherReversals_TenantId_ReversalVoucherId",
                table: "FinanceVoucherReversals",
                columns: new[] { "TenantId", "ReversalVoucherId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVouchers_TenantId_Status_CreatedAt",
                table: "FinanceVouchers",
                columns: new[] { "TenantId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVouchers_TenantId_VoucherNumber",
                table: "FinanceVouchers",
                columns: new[] { "TenantId", "VoucherNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceVoucherSequences_TenantId_VoucherType_FiscalYear",
                table: "FinanceVoucherSequences",
                columns: new[] { "TenantId", "VoucherType", "FiscalYear" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceWithholdingRecords_TenantId_FilingPeriod_TaxType",
                table: "FinanceWithholdingRecords",
                columns: new[] { "TenantId", "FilingPeriod", "TaxType" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceWithholdingRecords_TenantId_PaymentReference",
                table: "FinanceWithholdingRecords",
                columns: new[] { "TenantId", "PaymentReference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FireDrills_FireEquipmentId",
                table: "FireDrills",
                column: "FireEquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_FireEquipments_BuildingId",
                table: "FireEquipments",
                column: "BuildingId");

            migrationBuilder.CreateIndex(
                name: "IX_FireEquipments_EquipmentCode",
                table: "FireEquipments",
                column: "EquipmentCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FireEquipments_RoomId",
                table: "FireEquipments",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalPeriods_FiscalYearId",
                table: "FiscalPeriods",
                column: "FiscalYearId");

            migrationBuilder.CreateIndex(
                name: "IX_FloorPlans_FloorId",
                table: "FloorPlans",
                column: "FloorId");

            migrationBuilder.CreateIndex(
                name: "IX_Floors_BuildingId",
                table: "Floors",
                column: "BuildingId");

            migrationBuilder.CreateIndex(
                name: "IX_FormSubmissions_FormId",
                table: "FormSubmissions",
                column: "FormId");

            migrationBuilder.CreateIndex(
                name: "IX_GateEntries_GuardEmployeeId",
                table: "GateEntries",
                column: "GuardEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_GateEntries_VehiclePassId",
                table: "GateEntries",
                column: "VehiclePassId");

            migrationBuilder.CreateIndex(
                name: "IX_GateEntries_VisitorId",
                table: "GateEntries",
                column: "VisitorId");

            migrationBuilder.CreateIndex(
                name: "IX_GPSLocations_TransportVehicleId",
                table: "GPSLocations",
                column: "TransportVehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_GradeBands_GradeScaleId_LetterGrade",
                table: "GradeBands",
                columns: new[] { "GradeScaleId", "LetterGrade" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GradeOverrideRequests_ApprovalStatus_ApprovalThreshold",
                table: "GradeOverrideRequests",
                columns: new[] { "ApprovalStatus", "ApprovalThreshold" });

            migrationBuilder.CreateIndex(
                name: "IX_GradeOverrideRequests_StudentId_CourseId_SemesterId",
                table: "GradeOverrideRequests",
                columns: new[] { "StudentId", "CourseId", "SemesterId" });

            migrationBuilder.CreateIndex(
                name: "IX_GrantExpenditures_ResearchGrantId",
                table: "GrantExpenditures",
                column: "ResearchGrantId");

            migrationBuilder.CreateIndex(
                name: "IX_GrievanceResponses_TicketId",
                table: "GrievanceResponses",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_GrievanceTickets_Status",
                table: "GrievanceTickets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_GrievanceTickets_SubmitterId",
                table: "GrievanceTickets",
                column: "SubmitterId");

            migrationBuilder.CreateIndex(
                name: "IX_GrievanceTickets_TicketNumber",
                table: "GrievanceTickets",
                column: "TicketNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuardRounds_SecurityGuardId",
                table: "GuardRounds",
                column: "SecurityGuardId");

            migrationBuilder.CreateIndex(
                name: "IX_GuardShifts_SecurityGuardId",
                table: "GuardShifts",
                column: "SecurityGuardId");

            migrationBuilder.CreateIndex(
                name: "IX_HostelAllocations_HostelRoomId",
                table: "HostelAllocations",
                column: "HostelRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_HostelFloors_HostelBlockId",
                table: "HostelFloors",
                column: "HostelBlockId");

            migrationBuilder.CreateIndex(
                name: "IX_HostelRooms_HostelFloorId",
                table: "HostelRooms",
                column: "HostelFloorId");

            migrationBuilder.CreateIndex(
                name: "IX_HumanGradeReviews_GradeRunId_Status",
                table: "HumanGradeReviews",
                columns: new[] { "GradeRunId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_IncidentEvidences_SecurityIncidentId",
                table: "IncidentEvidences",
                column: "SecurityIncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentWitnesses_SecurityIncidentId",
                table: "IncidentWitnesses",
                column: "SecurityIncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionChecklistItems_RoomInspectionId",
                table: "InspectionChecklistItems",
                column: "RoomInspectionId");

            migrationBuilder.CreateIndex(
                name: "IX_JobApplications_JobPostingId",
                table: "JobApplications",
                column: "JobPostingId");

            migrationBuilder.CreateIndex(
                name: "IX_JobExecutions_JobId",
                table: "JobExecutions",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_EntryNumber",
                table: "JournalEntries",
                column: "EntryNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_TenantId_IsPosted_IsSandbox_EntryDate",
                table: "JournalEntries",
                columns: new[] { "TenantId", "IsPosted", "IsSandbox", "EntryDate" });

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntryLines_JournalEntryId",
                table: "JournalEntryLines",
                column: "JournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_KPIAlerts_DashboardWidgetId",
                table: "KPIAlerts",
                column: "DashboardWidgetId");

            migrationBuilder.CreateIndex(
                name: "IX_LabBookings_ResearchLabId",
                table: "LabBookings",
                column: "ResearchLabId");

            migrationBuilder.CreateIndex(
                name: "IX_LabConsumables_ResearchLabId",
                table: "LabConsumables",
                column: "ResearchLabId");

            migrationBuilder.CreateIndex(
                name: "IX_LabEquipments_ResearchLabId",
                table: "LabEquipments",
                column: "ResearchLabId");

            migrationBuilder.CreateIndex(
                name: "IX_LabSafetyInspections_ResearchLabId",
                table: "LabSafetyInspections",
                column: "ResearchLabId");

            migrationBuilder.CreateIndex(
                name: "IX_LeadActivities_AdmissionLeadId",
                table: "LeadActivities",
                column: "AdmissionLeadId");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryBooks_Author",
                table: "LibraryBooks",
                column: "Author");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryBooks_Category",
                table: "LibraryBooks",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_LibraryBooks_Title",
                table: "LibraryBooks",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_LicenseStates_LockedAt",
                table: "LicenseStates",
                column: "LockedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LicenseStates_Status",
                table: "LicenseStates",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_LmsSyncLogs_CourseId",
                table: "LmsSyncLogs",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_LmsSyncLogs_CourseId_SyncType",
                table: "LmsSyncLogs",
                columns: new[] { "CourseId", "SyncType" });

            migrationBuilder.CreateIndex(
                name: "IX_LostFoundItems_Status",
                table: "LostFoundItems",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTickets_AssetId",
                table: "MaintenanceTickets",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTickets_BuildingId",
                table: "MaintenanceTickets",
                column: "BuildingId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTickets_RoomId",
                table: "MaintenanceTickets",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTickets_TicketNumber",
                table: "MaintenanceTickets",
                column: "TicketNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarkModerations_CourseId_SemesterId",
                table: "MarkModerations",
                columns: new[] { "CourseId", "SemesterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarkModerations_Status",
                table: "MarkModerations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalVisits_StudentId",
                table: "MedicalVisits",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_MentoringSessions_MentorAssignmentId",
                table: "MentoringSessions",
                column: "MentorAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_MeritListEntries_ApplicationId",
                table: "MeritListEntries",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_MeritListEntries_MeritListId_MeritRank",
                table: "MeritListEntries",
                columns: new[] { "MeritListId", "MeritRank" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeritLists_ProgramId_SemesterId_QuotaType_RoundNumber",
                table: "MeritLists",
                columns: new[] { "ProgramId", "SemesterId", "QuotaType", "RoundNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NoticeBoardItems_ExpiryDate",
                table: "NoticeBoardItems",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_NoticeBoardItems_Visibility",
                table: "NoticeBoardItems",
                column: "Visibility");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationRecords_InstanceId",
                table: "NotificationRecords",
                column: "InstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_OfflineAttendanceEvents_ClientEventId",
                table: "OfflineAttendanceEvents",
                column: "ClientEventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfflineAttendanceEvents_CourseId_StudentId",
                table: "OfflineAttendanceEvents",
                columns: new[] { "CourseId", "StudentId" });

            migrationBuilder.CreateIndex(
                name: "IX_OfflineAttendanceEvents_WasOfflineAt",
                table: "OfflineAttendanceEvents",
                column: "WasOfflineAt");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_BookOrderId",
                table: "OrderItems",
                column: "BookOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PaperDestructionManifests_BatchId",
                table: "PaperDestructionManifests",
                column: "BatchId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaperDestructionManifests_ManifestHash",
                table: "PaperDestructionManifests",
                column: "ManifestHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatentInventors_ResearchPatentId",
                table: "PatentInventors",
                column: "ResearchPatentId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentGatewayConfigurations_TenantId_GatewaySlug",
                table: "PaymentGatewayConfigurations",
                columns: new[] { "TenantId", "GatewaySlug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentGatewayEvents_TenantId_CreatedAt",
                table: "PaymentGatewayEvents",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentGatewayEvents_TenantId_GatewaySlug_CreatedAt",
                table: "PaymentGatewayEvents",
                columns: new[] { "TenantId", "GatewaySlug", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentGatewayEvents_TenantId_GatewaySlug_ReferenceId",
                table: "PaymentGatewayEvents",
                columns: new[] { "TenantId", "GatewaySlug", "ReferenceId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentPlanInstallments_PaymentPlanArrangementId",
                table: "PaymentPlanInstallments",
                column: "PaymentPlanArrangementId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuns_Month",
                table: "PayrollRuns",
                column: "Month",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payslips_EmployeeId",
                table: "Payslips",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Payslips_PayrollRunId_EmployeeId",
                table: "Payslips",
                columns: new[] { "PayrollRunId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Module_Action_Resource",
                table: "Permissions",
                columns: new[] { "Module", "Action", "Resource" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PluginRevenueShares_ExtensionId",
                table: "PluginRevenueShares",
                column: "ExtensionId");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyConfigs_Module_Key_EffectiveFrom",
                table: "PolicyConfigs",
                columns: new[] { "Module", "Key", "EffectiveFrom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PowerBiReports_PowerBiReportId",
                table: "PowerBiReports",
                column: "PowerBiReportId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PowerBiReports_TargetRole",
                table: "PowerBiReports",
                column: "TargetRole");

            migrationBuilder.CreateIndex(
                name: "IX_PreventiveMaintenanceSchedules_AssetId",
                table: "PreventiveMaintenanceSchedules",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_PreventiveMaintenanceSchedules_RoomId",
                table: "PreventiveMaintenanceSchedules",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcurementRequisitions_RequisitionNumber",
                table: "ProcurementRequisitions",
                column: "RequisitionNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProgramOutcomes_Code",
                table: "ProgramOutcomes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PublicationAuthors_ResearchPublicationId",
                table: "PublicationAuthors",
                column: "ResearchPublicationId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_PoNumber",
                table: "PurchaseOrders",
                column: "PoNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_Status",
                table: "PurchaseOrders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ReadinessCriteria_FrameworkId",
                table: "ReadinessCriteria",
                column: "FrameworkId");

            migrationBuilder.CreateIndex(
                name: "IX_RequisitionLineItems_RequisitionId",
                table: "RequisitionLineItems",
                column: "RequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ResearchDeliverables_ResearchProjectId",
                table: "ResearchDeliverables",
                column: "ResearchProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ResearchLabs_DepartmentId",
                table: "ResearchLabs",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ResearchLabs_LabCode",
                table: "ResearchLabs",
                column: "LabCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResearchLabs_LabSupervisorId",
                table: "ResearchLabs",
                column: "LabSupervisorId");

            migrationBuilder.CreateIndex(
                name: "IX_ResearchMilestones_ResearchProjectId",
                table: "ResearchMilestones",
                column: "ResearchProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ResearchPatents_PatentNumber",
                table: "ResearchPatents",
                column: "PatentNumber",
                unique: true,
                filter: "[PatentNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ResearchProjectMembers_ResearchProjectId",
                table: "ResearchProjectMembers",
                column: "ResearchProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Results_CourseId",
                table: "Results",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Results_EnrollmentId",
                table: "Results",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId_PermissionId",
                table: "RolePermissions",
                columns: new[] { "RoleId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoomInspections_HostelRoomId",
                table: "RoomInspections",
                column: "HostelRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomOccupancies_RoomId",
                table: "RoomOccupancies",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_FloorId",
                table: "Rooms",
                column: "FloorId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteStops_TransportRouteId",
                table: "RouteStops",
                column: "TransportRouteId");

            migrationBuilder.CreateIndex(
                name: "IX_RubricCriteria_RubricId",
                table: "RubricCriteria",
                column: "RubricId");

            migrationBuilder.CreateIndex(
                name: "IX_RuleVersions_RuleId",
                table: "RuleVersions",
                column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_SafetyChecklistItems_LabSafetyInspectionId",
                table: "SafetyChecklistItems",
                column: "LabSafetyInspectionId");

            migrationBuilder.CreateIndex(
                name: "IX_SeatAllocations_ApplicationId",
                table: "SeatAllocations",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_SeatAllocations_MeritListEntryId",
                table: "SeatAllocations",
                column: "MeritListEntryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SectionOverrideRequests_StudentId_CourseId_Status",
                table: "SectionOverrideRequests",
                columns: new[] { "StudentId", "CourseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityGuards_BadgeNumber",
                table: "SecurityGuards",
                column: "BadgeNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecurityGuards_EmployeeId",
                table: "SecurityGuards",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityIncidents_AssignedToEmployeeId",
                table: "SecurityIncidents",
                column: "AssignedToEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityIncidents_IncidentNumber",
                table: "SecurityIncidents",
                column: "IncidentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecurityIncidents_LocationBuildingId",
                table: "SecurityIncidents",
                column: "LocationBuildingId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityIncidents_LocationRoomId",
                table: "SecurityIncidents",
                column: "LocationRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_SelfStudyReports_AccreditationCycleId",
                table: "SelfStudyReports",
                column: "AccreditationCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_Semesters_AcademicYearId",
                table: "Semesters",
                column: "AcademicYearId");

            migrationBuilder.CreateIndex(
                name: "IX_Semesters_IsCurrent",
                table: "Semesters",
                column: "IsCurrent");

            migrationBuilder.CreateIndex(
                name: "IX_SensorReadings_SensorId",
                table: "SensorReadings",
                column: "SensorId");

            migrationBuilder.CreateIndex(
                name: "IX_SickLeaveApplications_StudentId_Status",
                table: "SickLeaveApplications",
                columns: new[] { "StudentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SLADefinitions_Category_Priority",
                table: "SLADefinitions",
                columns: new[] { "Category", "Priority" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SocietyMemberships_SocietyId_StudentId",
                table: "SocietyMemberships",
                columns: new[] { "SocietyId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SsoSessions_ExpiresAt",
                table: "SsoSessions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_SsoSessions_UserId_IsActive",
                table: "SsoSessions",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_SSRReviews_SelfStudyReportId",
                table: "SSRReviews",
                column: "SelfStudyReportId");

            migrationBuilder.CreateIndex(
                name: "IX_SSRSections_SelfStudyReportId",
                table: "SSRSections",
                column: "SelfStudyReportId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentActivities_ScheduledDate",
                table: "StudentActivities",
                column: "ScheduledDate");

            migrationBuilder.CreateIndex(
                name: "IX_StudentFeeDues_Status",
                table: "StudentFeeDues",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_StudentFeeDues_StudentId_SemesterId",
                table: "StudentFeeDues",
                columns: new[] { "StudentId", "SemesterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentFeePayments_FeeDueId",
                table: "StudentFeePayments",
                column: "FeeDueId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentFeePayments_ReferenceNumber",
                table: "StudentFeePayments",
                column: "ReferenceNumber");

            migrationBuilder.CreateIndex(
                name: "IX_StudentHealthRecords_StudentId",
                table: "StudentHealthRecords",
                column: "StudentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentProfiles_StudentId",
                table: "StudentProfiles",
                column: "StudentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentResumes_StudentId",
                table: "StudentResumes",
                column: "StudentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_BatchId",
                table: "Students",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_UniversityId",
                table: "Students",
                column: "UniversityId",
                unique: true,
                filter: "[UniversityId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StudentSocieties_SocietyName",
                table: "StudentSocieties",
                column: "SocietyName");

            migrationBuilder.CreateIndex(
                name: "IX_StudentTransfers_StudentId",
                table: "StudentTransfers",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfigurations_ReplicationLagHours",
                table: "SystemConfigurations",
                column: "ReplicationLagHours");

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfigurations_UniversityId_SubdomainName",
                table: "SystemConfigurations",
                columns: new[] { "UniversityId", "SubdomainName" },
                unique: true,
                filter: "[SubdomainName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TaxSlabs_CountryPackId",
                table: "TaxSlabs",
                column: "CountryPackId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamsSyncLogs_SyncStatus",
                table: "TeamsSyncLogs",
                column: "SyncStatus");

            migrationBuilder.CreateIndex(
                name: "IX_TeamsSyncLogs_TimetableEntryId",
                table: "TeamsSyncLogs",
                column: "TimetableEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantFinanceConfigurations_TenantId",
                table: "TenantFinanceConfigurations",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_ShortName",
                table: "Tenants",
                column: "ShortName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketComments_MaintenanceTicketId",
                table: "TicketComments",
                column: "MaintenanceTicketId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketEscalations_MaintenanceTicketId",
                table: "TicketEscalations",
                column: "MaintenanceTicketId");

            migrationBuilder.CreateIndex(
                name: "IX_Timetables_BatchId",
                table: "Timetables",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Timetables_CourseId",
                table: "Timetables",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Timetables_FacultyId",
                table: "Timetables",
                column: "FacultyId");

            migrationBuilder.CreateIndex(
                name: "IX_Transcripts_VerificationHash",
                table: "Transcripts",
                column: "VerificationHash",
                unique: true,
                filter: "[VerificationHash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TranslationValues_TranslationKeyId",
                table: "TranslationValues",
                column: "TranslationKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportDrivers_LicenseNumber",
                table: "TransportDrivers",
                column: "LicenseNumber");

            migrationBuilder.CreateIndex(
                name: "IX_UniversitySsoProfiles_Code",
                table: "UniversitySsoProfiles",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UniversitySsoProfiles_Domain",
                table: "UniversitySsoProfiles",
                column: "Domain",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UniversitySsoProfiles_SsoProvider",
                table: "UniversitySsoProfiles",
                column: "SsoProvider");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId_RoleId",
                table: "UserRoles",
                columns: new[] { "UserId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSecondFactors_UserId",
                table: "UserSecondFactors",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UtilityReadings_HostelRoomId",
                table: "UtilityReadings",
                column: "HostelRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_VehiclePasses_OwnerEmployeeId",
                table: "VehiclePasses",
                column: "OwnerEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_VehiclePasses_OwnerStudentId",
                table: "VehiclePasses",
                column: "OwnerStudentId");

            migrationBuilder.CreateIndex(
                name: "IX_VehiclePasses_VehicleNumber",
                table: "VehiclePasses",
                column: "VehicleNumber");

            migrationBuilder.CreateIndex(
                name: "IX_VehiclePasses_VisitorId",
                table: "VehiclePasses",
                column: "VisitorId");

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_VendorName",
                table: "Vendors",
                column: "VendorName");

            migrationBuilder.CreateIndex(
                name: "IX_Visitors_CNIC",
                table: "Visitors",
                column: "CNIC");

            migrationBuilder.CreateIndex(
                name: "IX_Visitors_HostEmployeeId",
                table: "Visitors",
                column: "HostEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Visitors_HostStudentId",
                table: "Visitors",
                column: "HostStudentId");

            migrationBuilder.CreateIndex(
                name: "IX_WizardAdminCredentials_TenantId_SessionId_Email",
                table: "WizardAdminCredentials",
                columns: new[] { "TenantId", "SessionId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WizardSessions_BootstrapTokenHash",
                table: "WizardSessions",
                column: "BootstrapTokenHash",
                unique: true,
                filter: "[BootstrapTokenHash] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_WizardSessions_TenantId_CompletedAt",
                table: "WizardSessions",
                columns: new[] { "TenantId", "CompletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstances_DefinitionId",
                table: "WorkflowInstances",
                column: "DefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowSteps_InstanceId",
                table: "WorkflowSteps",
                column: "InstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderMaterials_WorkOrderId",
                table: "WorkOrderMaterials",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_AssignedToContractorId",
                table: "WorkOrders",
                column: "AssignedToContractorId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_AssignedToEmployeeId",
                table: "WorkOrders",
                column: "AssignedToEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_MaintenanceTicketId",
                table: "WorkOrders",
                column: "MaintenanceTicketId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_PMScheduleId",
                table: "WorkOrders",
                column: "PMScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_WorkOrderNumber",
                table: "WorkOrders",
                column: "WorkOrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderTimeEntries_EmployeeId",
                table: "WorkOrderTimeEntries",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderTimeEntries_WorkOrderId",
                table: "WorkOrderTimeEntries",
                column: "WorkOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcademicHolidays");

            migrationBuilder.DropTable(
                name: "AccessAuditLogs");

            migrationBuilder.DropTable(
                name: "AccreditationAuditFindings");

            migrationBuilder.DropTable(
                name: "AccreditationAuditLogs");

            migrationBuilder.DropTable(
                name: "AccreditationCriterionMappings");

            migrationBuilder.DropTable(
                name: "AccreditationReports");

            migrationBuilder.DropTable(
                name: "AccreditationScoreSnapshots");

            migrationBuilder.DropTable(
                name: "AchievementBadges");

            migrationBuilder.DropTable(
                name: "AchievementRoadmaps");

            migrationBuilder.DropTable(
                name: "ActionItems");

            migrationBuilder.DropTable(
                name: "ActivityParticipations");

            migrationBuilder.DropTable(
                name: "AdvisorMeetings");

            migrationBuilder.DropTable(
                name: "AdvisorNotes");

            migrationBuilder.DropTable(
                name: "AgenticGradeRuns");

            migrationBuilder.DropTable(
                name: "AIFeedback");

            migrationBuilder.DropTable(
                name: "AIModels");

            migrationBuilder.DropTable(
                name: "AIProviders");

            migrationBuilder.DropTable(
                name: "AITokenUsage");

            migrationBuilder.DropTable(
                name: "AlertRules");

            migrationBuilder.DropTable(
                name: "AlumniDonations");

            migrationBuilder.DropTable(
                name: "AlumniProfiles");

            migrationBuilder.DropTable(
                name: "AlumniReferrals");

            migrationBuilder.DropTable(
                name: "AnonymousFeedbackSubmissions");

            migrationBuilder.DropTable(
                name: "ApiEndpoints");

            migrationBuilder.DropTable(
                name: "ApiKeys");

            migrationBuilder.DropTable(
                name: "AppealCommitteeBriefs");

            migrationBuilder.DropTable(
                name: "AppealEvidence");

            migrationBuilder.DropTable(
                name: "ApplicationDocuments");

            migrationBuilder.DropTable(
                name: "ApprovalRecords");

            migrationBuilder.DropTable(
                name: "AssetDisposals");

            migrationBuilder.DropTable(
                name: "AssetMaintenances");

            migrationBuilder.DropTable(
                name: "AssetTransfers");

            migrationBuilder.DropTable(
                name: "AssetVerifications");

            migrationBuilder.DropTable(
                name: "AttainmentThresholds");

            migrationBuilder.DropTable(
                name: "AttendanceRecords");

            migrationBuilder.DropTable(
                name: "Attendances");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "BackupRecords");

            migrationBuilder.DropTable(
                name: "BadgeAwards");

            migrationBuilder.DropTable(
                name: "BadgeDefinitions");

            migrationBuilder.DropTable(
                name: "Ballots");

            migrationBuilder.DropTable(
                name: "BenchmarkComparisons");

            migrationBuilder.DropTable(
                name: "BenchmarkRuns");

            migrationBuilder.DropTable(
                name: "BoardAlerts");

            migrationBuilder.DropTable(
                name: "BoardDecisions");

            migrationBuilder.DropTable(
                name: "BoardingLogs");

            migrationBuilder.DropTable(
                name: "BoardMeetingPacks");

            migrationBuilder.DropTable(
                name: "BoardMeetings");

            migrationBuilder.DropTable(
                name: "BoardResolutions");

            migrationBuilder.DropTable(
                name: "BookCopies");

            migrationBuilder.DropTable(
                name: "BookIssues");

            migrationBuilder.DropTable(
                name: "BookReservations");

            migrationBuilder.DropTable(
                name: "BusAttendances");

            migrationBuilder.DropTable(
                name: "CafeteriaInventory");

            migrationBuilder.DropTable(
                name: "CafeteriaTransactions");

            migrationBuilder.DropTable(
                name: "CafeteriaUnits");

            migrationBuilder.DropTable(
                name: "CalibrationRecords");

            migrationBuilder.DropTable(
                name: "CallLogs");

            migrationBuilder.DropTable(
                name: "CampusConfigurations");

            migrationBuilder.DropTable(
                name: "CandidateNominations");

            migrationBuilder.DropTable(
                name: "CareerEventRegistrations");

            migrationBuilder.DropTable(
                name: "CareerGoals");

            migrationBuilder.DropTable(
                name: "CashFlowForecasts");

            migrationBuilder.DropTable(
                name: "CatalogCourseRecords");

            migrationBuilder.DropTable(
                name: "CatalogVersions");

            migrationBuilder.DropTable(
                name: "ChangeImpactSimulations");

            migrationBuilder.DropTable(
                name: "ChartConfigurations");

            migrationBuilder.DropTable(
                name: "ChartOfAccounts");

            migrationBuilder.DropTable(
                name: "ChatbotMessages");

            migrationBuilder.DropTable(
                name: "CheckpointScans");

            migrationBuilder.DropTable(
                name: "CisUploads");

            migrationBuilder.DropTable(
                name: "Classes");

            migrationBuilder.DropTable(
                name: "ClassGenerationJobs");

            migrationBuilder.DropTable(
                name: "CLOAssessmentMappings");

            migrationBuilder.DropTable(
                name: "CLOAttainments");

            migrationBuilder.DropTable(
                name: "CloPlomappings");

            migrationBuilder.DropTable(
                name: "CLOPOMappings");

            migrationBuilder.DropTable(
                name: "CommitteeMeetings");

            migrationBuilder.DropTable(
                name: "CommitteeMembers");

            migrationBuilder.DropTable(
                name: "ComplianceBreaches");

            migrationBuilder.DropTable(
                name: "ConferenceTravels");

            migrationBuilder.DropTable(
                name: "ConfigAuditLogs");

            migrationBuilder.DropTable(
                name: "ConfigVersions");

            migrationBuilder.DropTable(
                name: "ConsentLedgers");

            migrationBuilder.DropTable(
                name: "ConsumableRequisitions");

            migrationBuilder.DropTable(
                name: "ContinuousImprovementPlans");

            migrationBuilder.DropTable(
                name: "ControlEvidences");

            migrationBuilder.DropTable(
                name: "ConvocationAttendance");

            migrationBuilder.DropTable(
                name: "ConvocationEligibility");

            migrationBuilder.DropTable(
                name: "Convocations");

            migrationBuilder.DropTable(
                name: "CorrectiveActionPlans");

            migrationBuilder.DropTable(
                name: "CostCenters");

            migrationBuilder.DropTable(
                name: "CounselingCaseNotes");

            migrationBuilder.DropTable(
                name: "CounselorProfiles");

            migrationBuilder.DropTable(
                name: "CourseAssignments");

            migrationBuilder.DropTable(
                name: "CourseEquivalencies");

            migrationBuilder.DropTable(
                name: "CourseGradePolicies");

            migrationBuilder.DropTable(
                name: "CourseGrades");

            migrationBuilder.DropTable(
                name: "CourseMarks");

            migrationBuilder.DropTable(
                name: "CourseOfferings");

            migrationBuilder.DropTable(
                name: "CourseReEnrollments");

            migrationBuilder.DropTable(
                name: "CreditTransfers");

            migrationBuilder.DropTable(
                name: "CriterionEvidences");

            migrationBuilder.DropTable(
                name: "CrossDeptPermissionRequests");

            migrationBuilder.DropTable(
                name: "Currencies");

            migrationBuilder.DropTable(
                name: "CurriculumChangeHistory");

            migrationBuilder.DropTable(
                name: "CurriculumCourses");

            migrationBuilder.DropTable(
                name: "CurriculumPrerequisites");

            migrationBuilder.DropTable(
                name: "CurriculumProposals");

            migrationBuilder.DropTable(
                name: "DashboardSubscriptions");

            migrationBuilder.DropTable(
                name: "DataExportLogs");

            migrationBuilder.DropTable(
                name: "DataImportJobs");

            migrationBuilder.DropTable(
                name: "DeansListEntries");

            migrationBuilder.DropTable(
                name: "DecisionTables");

            migrationBuilder.DropTable(
                name: "DefaulterInterventions");

            migrationBuilder.DropTable(
                name: "DefaulterRiskScores");

            migrationBuilder.DropTable(
                name: "DefaultRiskAssessments");

            migrationBuilder.DropTable(
                name: "DegreeAudits");

            migrationBuilder.DropTable(
                name: "DegreeCertificates");

            migrationBuilder.DropTable(
                name: "DegreeTrees");

            migrationBuilder.DropTable(
                name: "DegreeVerifications");

            migrationBuilder.DropTable(
                name: "DemandForecasts");

            migrationBuilder.DropTable(
                name: "DiagnosticFindings");

            migrationBuilder.DropTable(
                name: "DirectorySyncLogs");

            migrationBuilder.DropTable(
                name: "DistributionAnomalyAlerts");

            migrationBuilder.DropTable(
                name: "DocumentAccessLogs");

            migrationBuilder.DropTable(
                name: "DocumentExpiryTrackers");

            migrationBuilder.DropTable(
                name: "DocumentRetentionPolicies");

            migrationBuilder.DropTable(
                name: "DocumentTemplates");

            migrationBuilder.DropTable(
                name: "DocumentTiers");

            migrationBuilder.DropTable(
                name: "DocumentVaultEntries");

            migrationBuilder.DropTable(
                name: "DomainEvents");

            migrationBuilder.DropTable(
                name: "DriveRegistrations");

            migrationBuilder.DropTable(
                name: "DriverIncidents");

            migrationBuilder.DropTable(
                name: "DriverShifts");

            migrationBuilder.DropTable(
                name: "DWColumns");

            migrationBuilder.DropTable(
                name: "ElectionCandidates");

            migrationBuilder.DropTable(
                name: "ElectionPositions");

            migrationBuilder.DropTable(
                name: "ElectionResults");

            migrationBuilder.DropTable(
                name: "ElectiveSlots");

            migrationBuilder.DropTable(
                name: "EmergencyAlerts");

            migrationBuilder.DropTable(
                name: "EmergencyContacts");

            migrationBuilder.DropTable(
                name: "EmployeeContracts");

            migrationBuilder.DropTable(
                name: "EmployeeDocuments");

            migrationBuilder.DropTable(
                name: "Employers");

            migrationBuilder.DropTable(
                name: "EncryptedBallots");

            migrationBuilder.DropTable(
                name: "EnergyReadings");

            migrationBuilder.DropTable(
                name: "EnrollmentLetters");

            migrationBuilder.DropTable(
                name: "EquipmentBookings");

            migrationBuilder.DropTable(
                name: "EquipmentMaintenances");

            migrationBuilder.DropTable(
                name: "EquipmentUsageLogs");

            migrationBuilder.DropTable(
                name: "ErasureRequests");

            migrationBuilder.DropTable(
                name: "EscalationLogs");

            migrationBuilder.DropTable(
                name: "EthicalAuditLogs");

            migrationBuilder.DropTable(
                name: "EthicsProtocols");

            migrationBuilder.DropTable(
                name: "ETLJobs");

            migrationBuilder.DropTable(
                name: "EventSchemaVersions");

            migrationBuilder.DropTable(
                name: "EvidenceFreshnessLogs");

            migrationBuilder.DropTable(
                name: "EvidenceVersions");

            migrationBuilder.DropTable(
                name: "ExamPaperBatches");

            migrationBuilder.DropTable(
                name: "ExamPaperScans");

            migrationBuilder.DropTable(
                name: "ExamRegistrations");

            migrationBuilder.DropTable(
                name: "ExamSeatings");

            migrationBuilder.DropTable(
                name: "ExchangeRateSnapshots");

            migrationBuilder.DropTable(
                name: "ExecutiveDashboards");

            migrationBuilder.DropTable(
                name: "ExportLogs");

            migrationBuilder.DropTable(
                name: "ExtensionInstallations");

            migrationBuilder.DropTable(
                name: "ExtensionReviews");

            migrationBuilder.DropTable(
                name: "ExternalDbConnections");

            migrationBuilder.DropTable(
                name: "FacultyCompensations");

            migrationBuilder.DropTable(
                name: "FacultyCVs");

            migrationBuilder.DropTable(
                name: "FacultyEngagements");

            migrationBuilder.DropTable(
                name: "FacultyPerformanceRatings");

            migrationBuilder.DropTable(
                name: "FacultySlotMatches");

            migrationBuilder.DropTable(
                name: "FacultyWorkloads");

            migrationBuilder.DropTable(
                name: "FeatureFlags");

            migrationBuilder.DropTable(
                name: "FeeComponents");

            migrationBuilder.DropTable(
                name: "FeeDefaulterRecords");

            migrationBuilder.DropTable(
                name: "FeeInstallments");

            migrationBuilder.DropTable(
                name: "FeePayments");

            migrationBuilder.DropTable(
                name: "FerpaAuditLogs");

            migrationBuilder.DropTable(
                name: "FieldMappingTemplates");

            migrationBuilder.DropTable(
                name: "FinanceAccountingStandardPolicies");

            migrationBuilder.DropTable(
                name: "FinanceAssetAccountingEvents");

            migrationBuilder.DropTable(
                name: "FinanceAuditEvidencePackages");

            migrationBuilder.DropTable(
                name: "FinanceAuditorEngagements");

            migrationBuilder.DropTable(
                name: "FinanceAuditSamples");

            migrationBuilder.DropTable(
                name: "FinanceAuditSeals");

            migrationBuilder.DropTable(
                name: "FinanceBankFeedTransactions");

            migrationBuilder.DropTable(
                name: "FinanceConsolidationMembers");

            migrationBuilder.DropTable(
                name: "FinanceConsolidationMemoLines");

            migrationBuilder.DropTable(
                name: "FinanceControlEvaluationBatches");

            migrationBuilder.DropTable(
                name: "FinanceEliminationRules");

            migrationBuilder.DropTable(
                name: "FinanceFleetCostPostings");

            migrationBuilder.DropTable(
                name: "FinanceFleetProfiles");

            migrationBuilder.DropTable(
                name: "FinanceFleetTrips");

            migrationBuilder.DropTable(
                name: "FinanceForeignCurrencyTransactions");

            migrationBuilder.DropTable(
                name: "FinanceGoodsReceiptLines");

            migrationBuilder.DropTable(
                name: "FinanceGratuityAssumptions");

            migrationBuilder.DropTable(
                name: "FinanceGratuityValuations");

            migrationBuilder.DropTable(
                name: "FinanceInsuranceClaims");

            migrationBuilder.DropTable(
                name: "FinanceInventoryMovements");

            migrationBuilder.DropTable(
                name: "FinanceInvestmentBenchmarks");

            migrationBuilder.DropTable(
                name: "FinanceInvestmentTransactions");

            migrationBuilder.DropTable(
                name: "FinanceLedgerSignatures");

            migrationBuilder.DropTable(
                name: "FinanceLibraryAcquisitions");

            migrationBuilder.DropTable(
                name: "FinanceLibraryFines");

            migrationBuilder.DropTable(
                name: "FinancePaymentPlanConfigurations");

            migrationBuilder.DropTable(
                name: "FinancePaymentPlans");

            migrationBuilder.DropTable(
                name: "FinancePayments");

            migrationBuilder.DropTable(
                name: "FinancePayrollBankInstructions");

            migrationBuilder.DropTable(
                name: "FinancePayrollPaymentFiles");

            migrationBuilder.DropTable(
                name: "FinancePayrollPostingLines");

            migrationBuilder.DropTable(
                name: "FinancePettyCashCounts");

            migrationBuilder.DropTable(
                name: "FinancePettyCashExpenses");

            migrationBuilder.DropTable(
                name: "FinancePqcKeyCeremonyApprovals");

            migrationBuilder.DropTable(
                name: "FinancePqcKeys");

            migrationBuilder.DropTable(
                name: "FinanceRegulatorySchemas");

            migrationBuilder.DropTable(
                name: "FinanceRegulatorySubmissions");

            migrationBuilder.DropTable(
                name: "FinanceReportArtifacts");

            migrationBuilder.DropTable(
                name: "FinanceReportingEntities");

            migrationBuilder.DropTable(
                name: "FinanceReportProfiles");

            migrationBuilder.DropTable(
                name: "FinanceRestatementMemoLines");

            migrationBuilder.DropTable(
                name: "FinanceRestatementRules");

            migrationBuilder.DropTable(
                name: "FinanceScholarshipApplicationEvents");

            migrationBuilder.DropTable(
                name: "FinanceScholarshipAwards");

            migrationBuilder.DropTable(
                name: "FinanceSecurityDepositDeductions");

            migrationBuilder.DropTable(
                name: "FinanceSodRules");

            migrationBuilder.DropTable(
                name: "FinanceStandingPurchaseOrderSpend");

            migrationBuilder.DropTable(
                name: "FinanceTaxRules");

            migrationBuilder.DropTable(
                name: "FinanceThreeWayMatches");

            migrationBuilder.DropTable(
                name: "FinanceUtilityAllocations");

            migrationBuilder.DropTable(
                name: "FinanceVendorContacts");

            migrationBuilder.DropTable(
                name: "FinanceVendorContracts");

            migrationBuilder.DropTable(
                name: "FinanceVendorDocuments");

            migrationBuilder.DropTable(
                name: "FinanceVendorInvoiceLines");

            migrationBuilder.DropTable(
                name: "FinanceVoucherApprovalRules");

            migrationBuilder.DropTable(
                name: "FinanceVoucherApprovals");

            migrationBuilder.DropTable(
                name: "FinanceVoucherAttachments");

            migrationBuilder.DropTable(
                name: "FinanceVoucherLines");

            migrationBuilder.DropTable(
                name: "FinanceVoucherReversals");

            migrationBuilder.DropTable(
                name: "FinanceVoucherSequences");

            migrationBuilder.DropTable(
                name: "FinanceWithholdingRecords");

            migrationBuilder.DropTable(
                name: "FinePayments");

            migrationBuilder.DropTable(
                name: "FireDrills");

            migrationBuilder.DropTable(
                name: "FiscalPeriods");

            migrationBuilder.DropTable(
                name: "FleetVehicles");

            migrationBuilder.DropTable(
                name: "FloorPlans");

            migrationBuilder.DropTable(
                name: "FoodOrders");

            migrationBuilder.DropTable(
                name: "ForeignStudentRegistries");

            migrationBuilder.DropTable(
                name: "FormSubmissions");

            migrationBuilder.DropTable(
                name: "FreezeDocuments");

            migrationBuilder.DropTable(
                name: "FuelLogs");

            migrationBuilder.DropTable(
                name: "GapAnalyses");

            migrationBuilder.DropTable(
                name: "GapAnalysisItems");

            migrationBuilder.DropTable(
                name: "GateEntries");

            migrationBuilder.DropTable(
                name: "GateLogs");

            migrationBuilder.DropTable(
                name: "GPSLocations");

            migrationBuilder.DropTable(
                name: "GradeAppeals");

            migrationBuilder.DropTable(
                name: "GradeBands");

            migrationBuilder.DropTable(
                name: "GradeDistributions");

            migrationBuilder.DropTable(
                name: "GradeOverrideRequests");

            migrationBuilder.DropTable(
                name: "GrantExpenditures");

            migrationBuilder.DropTable(
                name: "GrievanceResponses");

            migrationBuilder.DropTable(
                name: "GuardrailRules");

            migrationBuilder.DropTable(
                name: "GuardrailViolations");

            migrationBuilder.DropTable(
                name: "GuardShifts");

            migrationBuilder.DropTable(
                name: "HardcodingFindings");

            migrationBuilder.DropTable(
                name: "HealthScores");

            migrationBuilder.DropTable(
                name: "HostelAllocations");

            migrationBuilder.DropTable(
                name: "HostelDisciplines");

            migrationBuilder.DropTable(
                name: "HumanGradeReviews");

            migrationBuilder.DropTable(
                name: "ImportBatches");

            migrationBuilder.DropTable(
                name: "IncidentEvidences");

            migrationBuilder.DropTable(
                name: "IncidentWitnesses");

            migrationBuilder.DropTable(
                name: "IngestionJobs");

            migrationBuilder.DropTable(
                name: "InsightChips");

            migrationBuilder.DropTable(
                name: "InspectionChecklistItems");

            migrationBuilder.DropTable(
                name: "InstitutionBenchmarks");

            migrationBuilder.DropTable(
                name: "IntegrationIncidents");

            migrationBuilder.DropTable(
                name: "IntegrationRegistrations");

            migrationBuilder.DropTable(
                name: "InterventionRecords");

            migrationBuilder.DropTable(
                name: "Interventions");

            migrationBuilder.DropTable(
                name: "InterviewSchedules");

            migrationBuilder.DropTable(
                name: "InterviewSlots");

            migrationBuilder.DropTable(
                name: "InventoryAlerts");

            migrationBuilder.DropTable(
                name: "InventoryItems");

            migrationBuilder.DropTable(
                name: "JobApplications");

            migrationBuilder.DropTable(
                name: "JobExecutions");

            migrationBuilder.DropTable(
                name: "JournalEntryLines");

            migrationBuilder.DropTable(
                name: "KPIAlerts");

            migrationBuilder.DropTable(
                name: "LabBookings");

            migrationBuilder.DropTable(
                name: "LabInventories");

            migrationBuilder.DropTable(
                name: "LeadActivities");

            migrationBuilder.DropTable(
                name: "LeaderboardEntries");

            migrationBuilder.DropTable(
                name: "LeadTasks");

            migrationBuilder.DropTable(
                name: "LeaveApplications");

            migrationBuilder.DropTable(
                name: "LibraryBudgets");

            migrationBuilder.DropTable(
                name: "LibraryMembers");

            migrationBuilder.DropTable(
                name: "LibraryUsages");

            migrationBuilder.DropTable(
                name: "LibraryVendors");

            migrationBuilder.DropTable(
                name: "LicenseHeartbeats");

            migrationBuilder.DropTable(
                name: "Licenses");

            migrationBuilder.DropTable(
                name: "LicenseStates");

            migrationBuilder.DropTable(
                name: "LmsConfigurations");

            migrationBuilder.DropTable(
                name: "LmsProvisions");

            migrationBuilder.DropTable(
                name: "LmsRosterSyncLogs");

            migrationBuilder.DropTable(
                name: "LmsSyncLogs");

            migrationBuilder.DropTable(
                name: "LoginAuditRecords");

            migrationBuilder.DropTable(
                name: "LostFoundItems");

            migrationBuilder.DropTable(
                name: "MandatoryReportLogs");

            migrationBuilder.DropTable(
                name: "MandatoryReports");

            migrationBuilder.DropTable(
                name: "MarketingCampaigns");

            migrationBuilder.DropTable(
                name: "MarkModerations");

            migrationBuilder.DropTable(
                name: "MealPlans");

            migrationBuilder.DropTable(
                name: "MedicalVisits");

            migrationBuilder.DropTable(
                name: "MentalHealthTickets");

            migrationBuilder.DropTable(
                name: "MentoringSessions");

            migrationBuilder.DropTable(
                name: "MenuItems");

            migrationBuilder.DropTable(
                name: "MeritListEntries");

            migrationBuilder.DropTable(
                name: "MessAttendances");

            migrationBuilder.DropTable(
                name: "MessFeedbacks");

            migrationBuilder.DropTable(
                name: "MessMenus");

            migrationBuilder.DropTable(
                name: "MisconductCases");

            migrationBuilder.DropTable(
                name: "MisconductEvidence");

            migrationBuilder.DropTable(
                name: "MoraSubmissions");

            migrationBuilder.DropTable(
                name: "NoticeBoardItems");

            migrationBuilder.DropTable(
                name: "NotificationDeliveries");

            migrationBuilder.DropTable(
                name: "NotificationPreferences");

            migrationBuilder.DropTable(
                name: "NotificationRecords");

            migrationBuilder.DropTable(
                name: "NotificationTemplates");

            migrationBuilder.DropTable(
                name: "OcrProviderConfigs");

            migrationBuilder.DropTable(
                name: "OfficialModules");

            migrationBuilder.DropTable(
                name: "OfflineAttendanceEvents");

            migrationBuilder.DropTable(
                name: "OrderItems");

            migrationBuilder.DropTable(
                name: "PaperDestructionManifests");

            migrationBuilder.DropTable(
                name: "PatentInventors");

            migrationBuilder.DropTable(
                name: "PaymentGatewayConfigurations");

            migrationBuilder.DropTable(
                name: "PaymentGatewayEvents");

            migrationBuilder.DropTable(
                name: "PaymentPlanInstallments");

            migrationBuilder.DropTable(
                name: "Payslips");

            migrationBuilder.DropTable(
                name: "PerformanceReviews");

            migrationBuilder.DropTable(
                name: "PersonalityConfigs");

            migrationBuilder.DropTable(
                name: "PlacementRecords");

            migrationBuilder.DropTable(
                name: "PlagiarismApiSubmissions");

            migrationBuilder.DropTable(
                name: "PlagiarismScans");

            migrationBuilder.DropTable(
                name: "PlatformMetrics");

            migrationBuilder.DropTable(
                name: "PluginRevenueShares");

            migrationBuilder.DropTable(
                name: "POAttainments");

            migrationBuilder.DropTable(
                name: "PolicyConfigs");

            migrationBuilder.DropTable(
                name: "PowerBiReports");

            migrationBuilder.DropTable(
                name: "PreOrders");

            migrationBuilder.DropTable(
                name: "PrerequisiteConflicts");

            migrationBuilder.DropTable(
                name: "PreviousVisitFindings");

            migrationBuilder.DropTable(
                name: "ProctorEvents");

            migrationBuilder.DropTable(
                name: "ProctoringSessions");

            migrationBuilder.DropTable(
                name: "ProfilePictureRecords");

            migrationBuilder.DropTable(
                name: "ProgramEducationalObjectives");

            migrationBuilder.DropTable(
                name: "PromptABTests");

            migrationBuilder.DropTable(
                name: "Prompts");

            migrationBuilder.DropTable(
                name: "PromptVersions");

            migrationBuilder.DropTable(
                name: "PublicationAuthors");

            migrationBuilder.DropTable(
                name: "PurchaseOrders");

            migrationBuilder.DropTable(
                name: "QueueDefinitions");

            migrationBuilder.DropTable(
                name: "QueueJobs");

            migrationBuilder.DropTable(
                name: "ReadinessAlerts");

            migrationBuilder.DropTable(
                name: "ReadinessCriteria");

            migrationBuilder.DropTable(
                name: "ReadinessGaps");

            migrationBuilder.DropTable(
                name: "ReadinessScores");

            migrationBuilder.DropTable(
                name: "ReadReplicaConfigs");

            migrationBuilder.DropTable(
                name: "RegistrationEnrollments");

            migrationBuilder.DropTable(
                name: "RegistrationWindows");

            migrationBuilder.DropTable(
                name: "ReleaseDownloads");

            migrationBuilder.DropTable(
                name: "ReplicaHealthLogs");

            migrationBuilder.DropTable(
                name: "ReplicationLagAlerts");

            migrationBuilder.DropTable(
                name: "ReplicationScaleEvents");

            migrationBuilder.DropTable(
                name: "ReplicationScalingConfigs");

            migrationBuilder.DropTable(
                name: "ReportSchedules");

            migrationBuilder.DropTable(
                name: "RequisitionLineItems");

            migrationBuilder.DropTable(
                name: "ResearchDeliverables");

            migrationBuilder.DropTable(
                name: "ResearchMilestones");

            migrationBuilder.DropTable(
                name: "ResearchProjectMembers");

            migrationBuilder.DropTable(
                name: "Results");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "RoommatePreferences");

            migrationBuilder.DropTable(
                name: "RoomOccupancies");

            migrationBuilder.DropTable(
                name: "RouteStops");

            migrationBuilder.DropTable(
                name: "RubricCriteria");

            migrationBuilder.DropTable(
                name: "RuleVersions");

            migrationBuilder.DropTable(
                name: "SafetyChecklistItems");

            migrationBuilder.DropTable(
                name: "SamlConfigurations");

            migrationBuilder.DropTable(
                name: "ScalingEvents");

            migrationBuilder.DropTable(
                name: "SchoolVisits");

            migrationBuilder.DropTable(
                name: "SeatAllocations");

            migrationBuilder.DropTable(
                name: "SectionOverrideRequests");

            migrationBuilder.DropTable(
                name: "SemesterFreezes");

            migrationBuilder.DropTable(
                name: "SensorReadings");

            migrationBuilder.DropTable(
                name: "ServiceDependencies");

            migrationBuilder.DropTable(
                name: "ServiceRegistrations");

            migrationBuilder.DropTable(
                name: "SickLeaveApplications");

            migrationBuilder.DropTable(
                name: "SkillGaps");

            migrationBuilder.DropTable(
                name: "SLADefinitions");

            migrationBuilder.DropTable(
                name: "SlotCandidateMatches");

            migrationBuilder.DropTable(
                name: "SlowQueries");

            migrationBuilder.DropTable(
                name: "SocietyMemberships");

            migrationBuilder.DropTable(
                name: "SsoSessions");

            migrationBuilder.DropTable(
                name: "SSRReviews");

            migrationBuilder.DropTable(
                name: "SSRSections");

            migrationBuilder.DropTable(
                name: "StrategicGoals");

            migrationBuilder.DropTable(
                name: "StrategicPlanGoals");

            migrationBuilder.DropTable(
                name: "StudentAchievements");

            migrationBuilder.DropTable(
                name: "StudentActivities");

            migrationBuilder.DropTable(
                name: "StudentAlerts");

            migrationBuilder.DropTable(
                name: "StudentBadges");

            migrationBuilder.DropTable(
                name: "StudentClearances");

            migrationBuilder.DropTable(
                name: "StudentCounselings");

            migrationBuilder.DropTable(
                name: "StudentDisciplines");

            migrationBuilder.DropTable(
                name: "StudentDocuments");

            migrationBuilder.DropTable(
                name: "StudentFeeDues");

            migrationBuilder.DropTable(
                name: "StudentFeePayments");

            migrationBuilder.DropTable(
                name: "StudentGuardians");

            migrationBuilder.DropTable(
                name: "StudentHealthRecords");

            migrationBuilder.DropTable(
                name: "StudentHostels");

            migrationBuilder.DropTable(
                name: "StudentProctoringRecords");

            migrationBuilder.DropTable(
                name: "StudentProfiles");

            migrationBuilder.DropTable(
                name: "StudentResumes");

            migrationBuilder.DropTable(
                name: "StudentRiskScores");

            migrationBuilder.DropTable(
                name: "StudentRiskTrends");

            migrationBuilder.DropTable(
                name: "StudentScholarships");

            migrationBuilder.DropTable(
                name: "StudentSectionEnrollments");

            migrationBuilder.DropTable(
                name: "StudentSocieties");

            migrationBuilder.DropTable(
                name: "StudentTransfers");

            migrationBuilder.DropTable(
                name: "StudentTransports");

            migrationBuilder.DropTable(
                name: "SupportTickets");

            migrationBuilder.DropTable(
                name: "SyncLagConfigs");

            migrationBuilder.DropTable(
                name: "SyncLagEntries");

            migrationBuilder.DropTable(
                name: "SystemConfigurations");

            migrationBuilder.DropTable(
                name: "TaxSlabs");

            migrationBuilder.DropTable(
                name: "TeachingSlots");

            migrationBuilder.DropTable(
                name: "TeamsSyncLogs");

            migrationBuilder.DropTable(
                name: "TenantFinanceConfigurations");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropTable(
                name: "TicketComments");

            migrationBuilder.DropTable(
                name: "TicketEscalations");

            migrationBuilder.DropTable(
                name: "Timetables");

            migrationBuilder.DropTable(
                name: "TrainingAssignments");

            migrationBuilder.DropTable(
                name: "Transcripts");

            migrationBuilder.DropTable(
                name: "TranslationValues");

            migrationBuilder.DropTable(
                name: "TransportIncidents");

            migrationBuilder.DropTable(
                name: "TransportInspections");

            migrationBuilder.DropTable(
                name: "TransportRegistrations");

            migrationBuilder.DropTable(
                name: "TunnelConfigurations");

            migrationBuilder.DropTable(
                name: "UniversityMissions");

            migrationBuilder.DropTable(
                name: "UniversitySsoProfiles");

            migrationBuilder.DropTable(
                name: "UpdateDeployments");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "UserSecondFactors");

            migrationBuilder.DropTable(
                name: "UtilityReadings");

            migrationBuilder.DropTable(
                name: "VehicleMaintenances");

            migrationBuilder.DropTable(
                name: "VendorPerformanceScores");

            migrationBuilder.DropTable(
                name: "Vendors");

            migrationBuilder.DropTable(
                name: "VendorScorecards");

            migrationBuilder.DropTable(
                name: "VisaDocuments");

            migrationBuilder.DropTable(
                name: "VisaExpiryAlerts");

            migrationBuilder.DropTable(
                name: "VisaRenewals");

            migrationBuilder.DropTable(
                name: "VisaRenewalWorkflows");

            migrationBuilder.DropTable(
                name: "VisitingFacultyContracts");

            migrationBuilder.DropTable(
                name: "VisitorLogs");

            migrationBuilder.DropTable(
                name: "VoiceIntents");

            migrationBuilder.DropTable(
                name: "VoiceSessions");

            migrationBuilder.DropTable(
                name: "VoterTokens");

            migrationBuilder.DropTable(
                name: "WasteLogs");

            migrationBuilder.DropTable(
                name: "WebhookSubscriptions");

            migrationBuilder.DropTable(
                name: "WellnessCheckIns");

            migrationBuilder.DropTable(
                name: "WizardAdminCredentials");

            migrationBuilder.DropTable(
                name: "WizardSessions");

            migrationBuilder.DropTable(
                name: "WorkOrderMaterials");

            migrationBuilder.DropTable(
                name: "WorkOrderTimeEntries");

            migrationBuilder.DropTable(
                name: "Semesters");

            migrationBuilder.DropTable(
                name: "AdvisorAssignments");

            migrationBuilder.DropTable(
                name: "WorkflowSteps");

            migrationBuilder.DropTable(
                name: "VerificationCycles");

            migrationBuilder.DropTable(
                name: "CampusPolygons");

            migrationBuilder.DropTable(
                name: "BackupConfigurations");

            migrationBuilder.DropTable(
                name: "LibraryBooks");

            migrationBuilder.DropTable(
                name: "CareerEvents");

            migrationBuilder.DropTable(
                name: "ChatbotConversations");

            migrationBuilder.DropTable(
                name: "GuardRounds");

            migrationBuilder.DropTable(
                name: "CourseLearningOutcomes");

            migrationBuilder.DropTable(
                name: "ProgramOutcomes");

            migrationBuilder.DropTable(
                name: "AccreditationCommittees");

            migrationBuilder.DropTable(
                name: "ConfigurationEntries");

            migrationBuilder.DropTable(
                name: "LabConsumables");

            migrationBuilder.DropTable(
                name: "AuditFindings");

            migrationBuilder.DropTable(
                name: "AssessmentComponents");

            migrationBuilder.DropTable(
                name: "CurriculumVersions");

            migrationBuilder.DropTable(
                name: "BIReports");

            migrationBuilder.DropTable(
                name: "DegreeRequirements");

            migrationBuilder.DropTable(
                name: "ForeignStudentRecords");

            migrationBuilder.DropTable(
                name: "CampusDrives");

            migrationBuilder.DropTable(
                name: "TransportDrivers");

            migrationBuilder.DropTable(
                name: "Elections");

            migrationBuilder.DropTable(
                name: "LabEquipments");

            migrationBuilder.DropTable(
                name: "AccreditationEvidences");

            migrationBuilder.DropTable(
                name: "ExamSchedules");

            migrationBuilder.DropTable(
                name: "VisitingFacultyProfiles");

            migrationBuilder.DropTable(
                name: "FeeStructures");

            migrationBuilder.DropTable(
                name: "StudentFees");

            migrationBuilder.DropTable(
                name: "FinanceBankAccounts");

            migrationBuilder.DropTable(
                name: "FinanceConsolidationGroups");

            migrationBuilder.DropTable(
                name: "FinanceConsolidationRuns");

            migrationBuilder.DropTable(
                name: "FinanceGoodsReceipts");

            migrationBuilder.DropTable(
                name: "FinanceInsurancePolicies");

            migrationBuilder.DropTable(
                name: "FinanceInventoryItems");

            migrationBuilder.DropTable(
                name: "FinanceInvestmentHoldings");

            migrationBuilder.DropTable(
                name: "FeeDemands");

            migrationBuilder.DropTable(
                name: "FinancePayrollPostings");

            migrationBuilder.DropTable(
                name: "FinancePettyCashFunds");

            migrationBuilder.DropTable(
                name: "FinancePqcKeyCeremonies");

            migrationBuilder.DropTable(
                name: "FinanceRestatementRuns");

            migrationBuilder.DropTable(
                name: "FinanceScholarshipApplications");

            migrationBuilder.DropTable(
                name: "FinanceSecurityDeposits");

            migrationBuilder.DropTable(
                name: "FinanceStandingPurchaseOrders");

            migrationBuilder.DropTable(
                name: "FinanceUtilityBills");

            migrationBuilder.DropTable(
                name: "FinancePurchaseOrderLines");

            migrationBuilder.DropTable(
                name: "FinanceVendorInvoices");

            migrationBuilder.DropTable(
                name: "FinanceVouchers");

            migrationBuilder.DropTable(
                name: "FireEquipments");

            migrationBuilder.DropTable(
                name: "FiscalYears");

            migrationBuilder.DropTable(
                name: "FormDefinitions");

            migrationBuilder.DropTable(
                name: "VehiclePasses");

            migrationBuilder.DropTable(
                name: "TransportVehicles");

            migrationBuilder.DropTable(
                name: "GradeScales");

            migrationBuilder.DropTable(
                name: "ResearchGrants");

            migrationBuilder.DropTable(
                name: "GrievanceTickets");

            migrationBuilder.DropTable(
                name: "SecurityIncidents");

            migrationBuilder.DropTable(
                name: "RoomInspections");

            migrationBuilder.DropTable(
                name: "JobPostings");

            migrationBuilder.DropTable(
                name: "ScheduledJobs");

            migrationBuilder.DropTable(
                name: "JournalEntries");

            migrationBuilder.DropTable(
                name: "DashboardWidgets");

            migrationBuilder.DropTable(
                name: "AdmissionLeads");

            migrationBuilder.DropTable(
                name: "MentorAssignments");

            migrationBuilder.DropTable(
                name: "AdmissionApplications");

            migrationBuilder.DropTable(
                name: "MeritLists");

            migrationBuilder.DropTable(
                name: "BookOrders");

            migrationBuilder.DropTable(
                name: "ResearchPatents");

            migrationBuilder.DropTable(
                name: "PaymentPlanArrangements");

            migrationBuilder.DropTable(
                name: "PayrollRuns");

            migrationBuilder.DropTable(
                name: "Extensions");

            migrationBuilder.DropTable(
                name: "ResearchPublications");

            migrationBuilder.DropTable(
                name: "AccreditationFrameworks");

            migrationBuilder.DropTable(
                name: "ProcurementRequisitions");

            migrationBuilder.DropTable(
                name: "ResearchProjects");

            migrationBuilder.DropTable(
                name: "Enrollments");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "TransportRoutes");

            migrationBuilder.DropTable(
                name: "Rubrics");

            migrationBuilder.DropTable(
                name: "RuleDefinitions");

            migrationBuilder.DropTable(
                name: "LabSafetyInspections");

            migrationBuilder.DropTable(
                name: "IoTSensors");

            migrationBuilder.DropTable(
                name: "SelfStudyReports");

            migrationBuilder.DropTable(
                name: "CountryPacks");

            migrationBuilder.DropTable(
                name: "Faculties");

            migrationBuilder.DropTable(
                name: "TranslationKeys");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "WorkOrders");

            migrationBuilder.DropTable(
                name: "AcademicYears");

            migrationBuilder.DropTable(
                name: "WorkflowInstances");

            migrationBuilder.DropTable(
                name: "SecurityGuards");

            migrationBuilder.DropTable(
                name: "ComplianceControls");

            migrationBuilder.DropTable(
                name: "AssessmentSchemes");

            migrationBuilder.DropTable(
                name: "DataWarehouseTables");

            migrationBuilder.DropTable(
                name: "DegreePrograms");

            migrationBuilder.DropTable(
                name: "AccreditationCriteria");

            migrationBuilder.DropTable(
                name: "FinanceInvestmentFunds");

            migrationBuilder.DropTable(
                name: "FinanceScholarshipPrograms");

            migrationBuilder.DropTable(
                name: "FinancePurchaseOrders");

            migrationBuilder.DropTable(
                name: "Visitors");

            migrationBuilder.DropTable(
                name: "HostelRooms");

            migrationBuilder.DropTable(
                name: "Dashboards");

            migrationBuilder.DropTable(
                name: "Courses");

            migrationBuilder.DropTable(
                name: "ResearchLabs");

            migrationBuilder.DropTable(
                name: "AccreditationCycles");

            migrationBuilder.DropTable(
                name: "MaintenanceContractors");

            migrationBuilder.DropTable(
                name: "MaintenanceTickets");

            migrationBuilder.DropTable(
                name: "PreventiveMaintenanceSchedules");

            migrationBuilder.DropTable(
                name: "WorkflowDefinitions");

            migrationBuilder.DropTable(
                name: "ComplianceFrameworks");

            migrationBuilder.DropTable(
                name: "FinanceGrants");

            migrationBuilder.DropTable(
                name: "FinanceVendors");

            migrationBuilder.DropTable(
                name: "Students");

            migrationBuilder.DropTable(
                name: "HostelFloors");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "AccreditationBodies");

            migrationBuilder.DropTable(
                name: "Assets");

            migrationBuilder.DropTable(
                name: "Rooms");

            migrationBuilder.DropTable(
                name: "Batches");

            migrationBuilder.DropTable(
                name: "HostelBlocks");

            migrationBuilder.DropTable(
                name: "AssetCategories");

            migrationBuilder.DropTable(
                name: "Floors");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "Buildings");

            migrationBuilder.DropTable(
                name: "Campuses");
        }
    }
}
