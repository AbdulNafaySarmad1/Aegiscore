using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V2_0_ExtendedPlatformModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AchievementBadges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Tier = table.Column<int>(type: "integer", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CriteriaJson = table.Column<string>(type: "jsonb", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchievementBadges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AchievementRoadmaps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    NextBadgesJson = table.Column<string>(type: "jsonb", nullable: false),
                    ProgressBarsJson = table.Column<string>(type: "jsonb", nullable: false),
                    CgpaTarget = table.Column<double>(type: "double precision", nullable: true),
                    RequiredGradesJson = table.Column<string>(type: "jsonb", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AchievementRoadmaps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppealCommitteeBriefs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppealId = table.Column<Guid>(type: "uuid", nullable: false),
                    BriefContent = table.Column<string>(type: "text", nullable: true),
                    EvidenceMatrixJson = table.Column<string>(type: "jsonb", nullable: false),
                    RecommendedOptions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppealCommitteeBriefs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppealEvidence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppealId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DocumentType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Relevance = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppealEvidence", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BenchmarkComparisons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FactorName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    InstitutionValue = table.Column<double>(type: "double precision", nullable: false),
                    NationalAverage = table.Column<double>(type: "double precision", nullable: false),
                    PeerAverage = table.Column<double>(type: "double precision", nullable: false),
                    PeerInstitutionName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsAboveAverage = table.Column<bool>(type: "boolean", nullable: false),
                    AcademicYear = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenchmarkComparisons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BoardAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RecommendedAction = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ResponsibleOfficer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TriggeredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardAlerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BoardMeetingPacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MeetingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExecutiveSummary = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    AgendaItemsJson = table.Column<string>(type: "jsonb", nullable: false),
                    PendingDecisionsJson = table.Column<string>(type: "jsonb", nullable: false),
                    PreviousResolutionsJson = table.Column<string>(type: "jsonb", nullable: false),
                    PdfUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsDistributed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardMeetingPacks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CafeteriaUnits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    OperatingHours = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    VendorId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CafeteriaUnits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CandidateNominations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ElectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PositionName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    EndorsementCount = table.Column<int>(type: "integer", nullable: false),
                    RequiredEndorsements = table.Column<int>(type: "integer", nullable: false),
                    IsEligible = table.Column<bool>(type: "boolean", nullable: false),
                    IneligibilityReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateNominations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CashFlowForecasts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Semester = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ForecastWeek = table.Column<int>(type: "integer", nullable: false),
                    ExpectedCollection = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ProjectedShortfall = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    RecoveryRateEstimate = table.Column<double>(type: "double precision", nullable: false),
                    WeeklyInflowProjectionsJson = table.Column<string>(type: "jsonb", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashFlowForecasts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CatalogCourseRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    DegreeProgram = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DegreeProgramSlug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SemesterSequence = table.Column<int>(type: "integer", nullable: false),
                    SemesterLabel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CourseCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CourseCodeNormalised = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CourseTitle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    CourseTitleNormalised = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CreditHoursTheory = table.Column<int>(type: "integer", nullable: false),
                    CreditHoursLab = table.Column<int>(type: "integer", nullable: false),
                    CreditHoursTotal = table.Column<int>(type: "integer", nullable: false),
                    PrerequisitesJson = table.Column<string>(type: "jsonb", nullable: false),
                    CourseType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ElectiveSlot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AnnotationsJson = table.Column<string>(type: "jsonb", nullable: false),
                    ExtractionConfidence = table.Column<double>(type: "double precision", nullable: false),
                    RequiresReview = table.Column<bool>(type: "boolean", nullable: false),
                    ReviewReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsUpdateCandidate = table.Column<bool>(type: "boolean", nullable: false),
                    IsInserted = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogCourseRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CatalogVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DegreeProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IngestionJobId = table.Column<Guid>(type: "uuid", nullable: true),
                    SnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    ChangeLogJson = table.Column<string>(type: "jsonb", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PublishedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChangeImpactSimulations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<int>(type: "integer", nullable: false),
                    ChangedElementId = table.Column<Guid>(type: "uuid", nullable: false),
                    DirectlyAffectedCourses = table.Column<int>(type: "integer", nullable: false),
                    IndirectlyAffectedCourses = table.Column<int>(type: "integer", nullable: false),
                    AffectedStudents = table.Column<int>(type: "integer", nullable: false),
                    InvalidRegistrations = table.Column<int>(type: "integer", nullable: false),
                    RippleEffectJson = table.Column<string>(type: "jsonb", nullable: false),
                    SimulatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangeImpactSimulations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChartConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    DataSourceKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ConfigJson = table.Column<string>(type: "jsonb", nullable: false),
                    InsightI18nKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChartConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Classes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseOfferingId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    SectionNumber = table.Column<int>(type: "integer", nullable: false),
                    SectionLabel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AcademicYear = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Semester = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PrimaryFacultyId = table.Column<Guid>(type: "uuid", nullable: true),
                    CoFacultyIdsJson = table.Column<string>(type: "jsonb", nullable: false),
                    EnrolledStudentIdsJson = table.Column<string>(type: "jsonb", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    EnrolledCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    GenerationJobId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Classes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClassGenerationJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicYear = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Semester = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TotalOfferings = table.Column<int>(type: "integer", nullable: false),
                    SectionsGenerated = table.Column<int>(type: "integer", nullable: false),
                    StudentsDistributed = table.Column<int>(type: "integer", nullable: false),
                    FacultyAssigned = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TriggeredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassGenerationJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ComplianceBreaches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RegistryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Resolution = table.Column<int>(type: "integer", nullable: true),
                    DetectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AcademicServicesFrozen = table.Column<bool>(type: "boolean", nullable: false),
                    EmbassyNotified = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplianceBreaches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConvocationAttendance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConvocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CheckInTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GuestCount = table.Column<int>(type: "integer", nullable: false),
                    CertificateCollected = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConvocationAttendance", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConvocationEligibility",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConvocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    BlockingReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreditsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    CgpaMet = table.Column<bool>(type: "boolean", nullable: false),
                    DuesCleared = table.Column<bool>(type: "boolean", nullable: false),
                    ThesisApproved = table.Column<bool>(type: "boolean", nullable: false),
                    LibraryCleared = table.Column<bool>(type: "boolean", nullable: false),
                    DisciplinaryCleared = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConvocationEligibility", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Convocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AcademicYear = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CeremonyDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Venue = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    GuestTicketsPerStudent = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Convocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CounselorProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Specializations = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Language = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CurrentCaseload = table.Column<int>(type: "integer", nullable: false),
                    MaxCaseload = table.Column<int>(type: "integer", nullable: false),
                    IsOnCall = table.Column<bool>(type: "boolean", nullable: false),
                    AvailableHours = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CounselorProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CourseEquivalencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceCourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetCourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceInstitution = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseEquivalencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CourseGradePolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    GradingScheme = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PassingGrade = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    GpaWeight = table.Column<double>(type: "double precision", nullable: false),
                    CanRepeat = table.Column<bool>(type: "boolean", nullable: false),
                    MaxRepeatAttempts = table.Column<int>(type: "integer", nullable: true),
                    GradeReplacement = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseGradePolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CourseOfferings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicYear = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Semester = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SectionNumber = table.Column<int>(type: "integer", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    EnrolledCount = table.Column<int>(type: "integer", nullable: false),
                    CampusId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseOfferings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeansListEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Semester = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Cgpa = table.Column<double>(type: "double precision", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CertificateUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    QualifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeansListEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DefaulterInterventions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RiskScoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    TriggeredByTier = table.Column<int>(type: "integer", nullable: false),
                    MessageContent = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    InitiatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Outcome = table.Column<int>(type: "integer", nullable: true),
                    IsSuccessful = table.Column<bool>(type: "boolean", nullable: false),
                    OutcomeNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefaulterInterventions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DefaulterRiskScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Semester = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ProbabilityScore = table.Column<double>(type: "double precision", nullable: false),
                    Tier = table.Column<int>(type: "integer", nullable: false),
                    RiskDriversJson = table.Column<string>(type: "jsonb", nullable: false),
                    PredictionConfidence = table.Column<double>(type: "double precision", nullable: false),
                    PaymentBehaviorJson = table.Column<string>(type: "jsonb", nullable: false),
                    AcademicCorrelationJson = table.Column<string>(type: "jsonb", nullable: false),
                    FinancialProfileJson = table.Column<string>(type: "jsonb", nullable: false),
                    PredictedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefaulterRiskScores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DegreeCertificates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConvocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DegreeTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AttestationStatus = table.Column<int>(type: "integer", nullable: false),
                    HECReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DegreeCertificates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DegreeTrees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DegreeProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    CatalogVersion = table.Column<int>(type: "integer", nullable: false),
                    NodesJson = table.Column<string>(type: "jsonb", nullable: false),
                    EdgesJson = table.Column<string>(type: "jsonb", nullable: false),
                    CriticalPathJson = table.Column<string>(type: "jsonb", nullable: false),
                    SemesterLanesJson = table.Column<string>(type: "jsonb", nullable: false),
                    ComputedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DegreeTrees", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DemandForecasts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    CafeteriaUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    ForecastDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PredictedQuantity = table.Column<int>(type: "integer", nullable: false),
                    ActualQuantity = table.Column<int>(type: "integer", nullable: false),
                    Accuracy = table.Column<double>(type: "double precision", nullable: false),
                    Notes = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandForecasts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DirectorySyncLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<int>(type: "integer", nullable: false),
                    UsersSynced = table.Column<int>(type: "integer", nullable: false),
                    UsersCreated = table.Column<int>(type: "integer", nullable: false),
                    UsersUpdated = table.Column<int>(type: "integer", nullable: false),
                    Errors = table.Column<int>(type: "integer", nullable: false),
                    ErrorDetailsJson = table.Column<string>(type: "jsonb", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DirectorySyncLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DistributionAnomalyAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DistributionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AnomalyType = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsResolved = table.Column<bool>(type: "boolean", nullable: false),
                    RecommendedAction = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DistributionAnomalyAlerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ElectionResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ElectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PositionName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    WinnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    VoteCount = table.Column<int>(type: "integer", nullable: false),
                    VotePercentage = table.Column<double>(type: "double precision", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CertificationUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AnnouncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElectionResults", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Elections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    VotingMethod = table.Column<int>(type: "integer", nullable: false),
                    AcademicYear = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NominationStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NominationEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CampaigningStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CampaigningEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VotingStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VotingEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResultsDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PositionsJson = table.Column<string>(type: "jsonb", nullable: false),
                    ObserverId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Elections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ElectiveSlots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DegreeProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    SlotLabel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SlotCategory = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SemesterSequence = table.Column<int>(type: "integer", nullable: false),
                    CreditHoursTheory = table.Column<int>(type: "integer", nullable: false),
                    CreditHoursLab = table.Column<int>(type: "integer", nullable: false),
                    EligibleCourseIdsJson = table.Column<string>(type: "jsonb", nullable: false),
                    MinGradeRequired = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    IsStudentChoice = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresApproval = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElectiveSlots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EncryptedBallots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ElectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    VoterTokenHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EncryptedVoteData = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    VerificationHash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CastAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EncryptedBallots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EvidenceFreshnessLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EvidenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DaysSinceUpdate = table.Column<int>(type: "integer", nullable: true),
                    NotificationMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CheckedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvidenceFreshnessLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExecutiveDashboards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserRole = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FiscalYear = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AcademicExcellenceScore = table.Column<double>(type: "double precision", nullable: false),
                    FinancialStabilityScore = table.Column<double>(type: "double precision", nullable: false),
                    AccreditationScore = table.Column<double>(type: "double precision", nullable: false),
                    StudentSuccessScore = table.Column<double>(type: "double precision", nullable: false),
                    FacultyQualityScore = table.Column<double>(type: "double precision", nullable: false),
                    CompositeRankingScore = table.Column<double>(type: "double precision", nullable: false),
                    KPIsJson = table.Column<string>(type: "jsonb", nullable: false),
                    StrategicGoalStatusesJson = table.Column<string>(type: "jsonb", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutiveDashboards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FacultySlotMatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OpenSlotId = table.Column<Guid>(type: "uuid", nullable: false),
                    FacultyProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompositeScore = table.Column<double>(type: "double precision", nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    MatchRationaleJson = table.Column<string>(type: "jsonb", nullable: false),
                    IsSelected = table.Column<bool>(type: "boolean", nullable: false),
                    MatchedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacultySlotMatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FoodOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    CafeteriaUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PaymentMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TransactionRef = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsPreOrder = table.Column<bool>(type: "boolean", nullable: false),
                    EstimatedReadyAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoodOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ForeignStudentRegistries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nationality = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PassportNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PassportExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VisaType = table.Column<int>(type: "integer", nullable: false),
                    VisaNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    VisaExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EntryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MoraStatus = table.Column<int>(type: "integer", nullable: false),
                    MoraRegistrationNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Compliance = table.Column<int>(type: "integer", nullable: false),
                    Program = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    YearOfStudy = table.Column<int>(type: "integer", nullable: false),
                    ExpectedGraduation = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForeignStudentRegistries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FreezeDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FreezeId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DocumentType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    VerificationNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FreezeDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GradeAppeals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Semester = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SubmittedGrade = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    AppealGrounds = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    GroundType = table.Column<int>(type: "integer", nullable: false),
                    GroundStrength = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    FacultyRespondentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CommitteeId = table.Column<Guid>(type: "uuid", nullable: true),
                    CommitteeDecision = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AdjustedGrade = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    SubmissionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PolicyClauseCitation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsEligible = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeAppeals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GradeDistributions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Semester = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SectionId = table.Column<Guid>(type: "uuid", nullable: true),
                    FacultyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Mean = table.Column<double>(type: "double precision", nullable: false),
                    Median = table.Column<double>(type: "double precision", nullable: false),
                    Mode = table.Column<double>(type: "double precision", nullable: true),
                    StdDev = table.Column<double>(type: "double precision", nullable: false),
                    Variance = table.Column<double>(type: "double precision", nullable: false),
                    Skewness = table.Column<double>(type: "double precision", nullable: false),
                    Kurtosis = table.Column<double>(type: "double precision", nullable: false),
                    PassRate = table.Column<double>(type: "double precision", nullable: false),
                    DistinctionRate = table.Column<double>(type: "double precision", nullable: false),
                    FailureRate = table.Column<double>(type: "double precision", nullable: false),
                    EnrollmentCount = table.Column<int>(type: "integer", nullable: false),
                    CurveShape = table.Column<int>(type: "integer", nullable: false),
                    LetterDistributionJson = table.Column<string>(type: "jsonb", nullable: false),
                    PercentileBreakdownJson = table.Column<string>(type: "jsonb", nullable: false),
                    AnomaliesJson = table.Column<string>(type: "jsonb", nullable: false),
                    HistoricalComparisonJson = table.Column<string>(type: "jsonb", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeDistributions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IngestionJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    OriginalDocumentUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RawExtraction = table.Column<string>(type: "text", nullable: true),
                    ExtractionConfidence = table.Column<double>(type: "double precision", nullable: false),
                    ClassificationLogJson = table.Column<string>(type: "jsonb", nullable: false),
                    ReviewLogJson = table.Column<string>(type: "jsonb", nullable: false),
                    InsertionLogJson = table.Column<string>(type: "jsonb", nullable: false),
                    ErrorLogJson = table.Column<string>(type: "jsonb", nullable: false),
                    CoursesExtracted = table.Column<int>(type: "integer", nullable: false),
                    CoursesInserted = table.Column<int>(type: "integer", nullable: false),
                    CoursesUpdated = table.Column<int>(type: "integer", nullable: false),
                    CoursesSkipped = table.Column<int>(type: "integer", nullable: false),
                    CoursesRequiringReview = table.Column<int>(type: "integer", nullable: false),
                    Warnings = table.Column<int>(type: "integer", nullable: false),
                    Errors = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngestionJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InsightChips",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChartConfigurationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    I18nKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ComputedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsightChips", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InventoryAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsResolved = table.Column<bool>(type: "boolean", nullable: false),
                    TriggeredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryAlerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InventoryItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CafeteriaUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    QuantityOnHand = table.Column<double>(type: "double precision", nullable: false),
                    ReorderPoint = table.Column<double>(type: "double precision", nullable: false),
                    ReorderQuantity = table.Column<double>(type: "double precision", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UnitCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeaderboardEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Scope = table.Column<int>(type: "integer", nullable: false),
                    ScopeValue = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    Score = table.Column<double>(type: "double precision", nullable: false),
                    RankedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Semester = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsAnonymous = table.Column<bool>(type: "boolean", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaderboardEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LmsConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    CoexistenceMode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RetryAttempts = table.Column<int>(type: "integer", nullable: false),
                    RetryDelayMs = table.Column<int>(type: "integer", nullable: false),
                    SyncSchedule = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ConflictResolution = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ConflictAlertThreshold = table.Column<int>(type: "integer", nullable: false),
                    ProviderConfigJson = table.Column<string>(type: "jsonb", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LmsConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LmsProvisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<int>(type: "integer", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ExternalUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    LastSyncAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProvisionedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorLogJson = table.Column<string>(type: "jsonb", nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LmsProvisions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LmsRosterSyncLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProvisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    MembersAdded = table.Column<int>(type: "integer", nullable: false),
                    MembersRemoved = table.Column<int>(type: "integer", nullable: false),
                    ConflictsDetected = table.Column<int>(type: "integer", nullable: false),
                    ConflictDetailsJson = table.Column<string>(type: "jsonb", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LmsRosterSyncLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MandatoryReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketId = table.Column<Guid>(type: "uuid", nullable: false),
                    Trigger = table.Column<int>(type: "integer", nullable: false),
                    ReportContent = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AuthorityNotified = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ReportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MandatoryReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MealPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    DailyAllowance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    EligibleItemsJson = table.Column<string>(type: "jsonb", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MentalHealthTickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AnonymousToken = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Anonymization = table.Column<int>(type: "integer", nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    ConcernType = table.Column<int>(type: "integer", nullable: false),
                    CrisisLevel = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    PreferredContact = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AssignedCounselorId = table.Column<Guid>(type: "uuid", nullable: true),
                    Resolution = table.Column<int>(type: "integer", nullable: true),
                    CaseNotesJson = table.Column<string>(type: "jsonb", nullable: false),
                    CrisisProtocolsJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResponseDeadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MentalHealthTickets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MenuItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IngredientsJson = table.Column<string>(type: "jsonb", nullable: false),
                    AllergensJson = table.Column<string>(type: "jsonb", nullable: false),
                    Calories = table.Column<int>(type: "integer", nullable: false),
                    IsHalal = table.Column<bool>(type: "boolean", nullable: false),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CafeteriaUnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MoraSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Semester = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StudentCount = table.Column<int>(type: "integer", nullable: false),
                    BatchReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsSubmitted = table.Column<bool>(type: "boolean", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    ResponseNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MoraSubmissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OcrProviderConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ApiEndpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CredentialRef = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OcrProviderConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PersonalityConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    IsLockedOff = table.Column<bool>(type: "boolean", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActiveForUser = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalityConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlagiarismApiSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<int>(type: "integer", nullable: false),
                    ExternalSubmissionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RawSimilarity = table.Column<double>(type: "double precision", nullable: true),
                    AdjustedSimilarity = table.Column<double>(type: "double precision", nullable: true),
                    Classification = table.Column<int>(type: "integer", nullable: true),
                    SourceBreakdownJson = table.Column<string>(type: "jsonb", nullable: false),
                    FacultySummary = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    StudentSummary = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    ExcludedSourcesJson = table.Column<string>(type: "jsonb", nullable: false),
                    ErrorLogJson = table.Column<string>(type: "jsonb", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlagiarismApiSubmissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlagiarismScans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Semester = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OverallSimilarity = table.Column<double>(type: "double precision", nullable: false),
                    Tier = table.Column<int>(type: "integer", nullable: false),
                    VerbatimMatchesJson = table.Column<string>(type: "jsonb", nullable: false),
                    ParaphraseMatchesJson = table.Column<string>(type: "jsonb", nullable: false),
                    StructuralMatchesJson = table.Column<string>(type: "jsonb", nullable: false),
                    StyleDeviationJson = table.Column<string>(type: "jsonb", nullable: false),
                    CollusionGraphJson = table.Column<string>(type: "jsonb", nullable: false),
                    AiGenerationFlagsJson = table.Column<string>(type: "jsonb", nullable: false),
                    InternetMatchesJson = table.Column<string>(type: "jsonb", nullable: false),
                    CaseRecommendation = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RequiresReview = table.Column<bool>(type: "boolean", nullable: false),
                    ScannedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlagiarismScans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PrerequisiteConflicts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    SourceCourseId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetCourseId = table.Column<Guid>(type: "uuid", nullable: true),
                    TriggerEvent = table.Column<int>(type: "integer", nullable: true),
                    AffectedStudentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ResolutionSuggestion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsResolved = table.Column<bool>(type: "boolean", nullable: false),
                    ResolutionNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DetectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrerequisiteConflicts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProctorEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    EventType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ScreenshotUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProctorEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProctoringSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExamId = table.Column<Guid>(type: "uuid", nullable: false),
                    Mode = table.Column<int>(type: "integer", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    LockdownEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CameraRequired = table.Column<bool>(type: "boolean", nullable: false),
                    MicRequired = table.Column<bool>(type: "boolean", nullable: false),
                    Sensitivity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ScheduledStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActualStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ActualEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProctoringSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReadinessAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsAcknowledged = table.Column<bool>(type: "boolean", nullable: false),
                    Category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TriggeredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReadinessAlerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReadinessGaps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    CriterionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    GapDescription = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    EvidenceNeeded = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ResponsibleOwnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Effort = table.Column<int>(type: "integer", nullable: false),
                    ImpactOnScore = table.Column<double>(type: "double precision", nullable: false),
                    PriorityRank = table.Column<int>(type: "integer", nullable: false),
                    IsClosed = table.Column<bool>(type: "boolean", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReadinessGaps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReadinessScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FrameworkId = table.Column<Guid>(type: "uuid", nullable: false),
                    FrameworkName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OverallScore = table.Column<double>(type: "double precision", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CategoryScoresJson = table.Column<string>(type: "jsonb", nullable: false),
                    CriterionScoresJson = table.Column<string>(type: "jsonb", nullable: false),
                    ScoreHistoryJson = table.Column<string>(type: "jsonb", nullable: false),
                    NextAuditDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DaysUntilAudit = table.Column<int>(type: "integer", nullable: false),
                    ComplianceVelocity = table.Column<double>(type: "double precision", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReadinessScores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SemesterFreezes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    ReasonCategory = table.Column<int>(type: "integer", nullable: false),
                    ReasonDescription = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CurrentSemester = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TotalSemesters = table.Column<int>(type: "integer", nullable: false),
                    TimingRule = table.Column<int>(type: "integer", nullable: false),
                    RefundAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    OutstandingBalance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    FreezesUsed = table.Column<int>(type: "integer", nullable: false),
                    MaxFreezeSemesters = table.Column<int>(type: "integer", nullable: false),
                    AcademicContinuityPlanJson = table.Column<string>(type: "jsonb", nullable: false),
                    EnrolledCoursesSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    RequestDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpectedReturnDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReturnStatus = table.Column<int>(type: "integer", nullable: true),
                    IsInternational = table.Column<bool>(type: "boolean", nullable: false),
                    HasScholarship = table.Column<bool>(type: "boolean", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SemesterFreezes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StrategicPlanGoals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CurrentValue = table.Column<double>(type: "double precision", nullable: false),
                    TargetValue = table.Column<double>(type: "double precision", nullable: false),
                    PercentAchieved = table.Column<double>(type: "double precision", nullable: false),
                    TargetDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    InterventionRecommendation = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrategicPlanGoals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudentBadges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    BadgeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AchievementDetail = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    VerificationHash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NotifiedParent = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentBadges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudentProctoringRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    IntegrityStatus = table.Column<int>(type: "integer", nullable: false),
                    WarningCount = table.Column<int>(type: "integer", nullable: false),
                    CriticalCount = table.Column<int>(type: "integer", nullable: false),
                    EventLogJson = table.Column<string>(type: "jsonb", nullable: false),
                    AnswerPatternAnalysisJson = table.Column<string>(type: "jsonb", nullable: false),
                    VerificationStatus = table.Column<int>(type: "integer", nullable: false),
                    CollusionFlagsJson = table.Column<string>(type: "jsonb", nullable: false),
                    IsEscalated = table.Column<bool>(type: "boolean", nullable: false),
                    EscalatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentProctoringRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudentSectionEnrollments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseOfferingId = table.Column<Guid>(type: "uuid", nullable: false),
                    SectionNumber = table.Column<int>(type: "integer", nullable: false),
                    DistributionStrategy = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentSectionEnrollments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VendorScorecards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Month = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    FoodQualityScore = table.Column<double>(type: "double precision", nullable: false),
                    HygieneScore = table.Column<double>(type: "double precision", nullable: false),
                    ServiceScore = table.Column<double>(type: "double precision", nullable: false),
                    ComplaintCount = table.Column<int>(type: "integer", nullable: false),
                    HalalCertValid = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EvaluatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorScorecards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VisaDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RegistryId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FileUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisaDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VisaExpiryAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RegistryId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Urgency = table.Column<int>(type: "integer", nullable: false),
                    DaysRemaining = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsNotified = table.Column<bool>(type: "boolean", nullable: false),
                    TriggeredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisaExpiryAlerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VisaRenewals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RegistryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DocumentChecklistJson = table.Column<string>(type: "jsonb", nullable: false),
                    ApplicationDeadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisaRenewals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VisitingFacultyContracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FacultyProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreditHours = table.Column<int>(type: "integer", nullable: false),
                    CompensationAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ContractUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RehireStatus = table.Column<int>(type: "integer", nullable: true),
                    PerformanceRating = table.Column<double>(type: "double precision", nullable: true),
                    PerformanceDetailsJson = table.Column<string>(type: "jsonb", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitingFacultyContracts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VisitingFacultyProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CNIC = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    VettingStatus = table.Column<int>(type: "integer", nullable: false),
                    HighestQualification = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    University = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    YearsOfExperience = table.Column<int>(type: "integer", nullable: false),
                    SpecializationsJson = table.Column<string>(type: "jsonb", nullable: false),
                    PriorEngagementsJson = table.Column<string>(type: "jsonb", nullable: false),
                    AverageRating = table.Column<double>(type: "double precision", nullable: false),
                    HECEligible = table.Column<bool>(type: "boolean", nullable: false),
                    ExpectedCompensation = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AvailabilityJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitingFacultyProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VoiceSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DetectedLanguage = table.Column<int>(type: "integer", nullable: false),
                    Intent = table.Column<int>(type: "integer", nullable: true),
                    Transcript = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    AsrConfidence = table.Column<int>(type: "integer", nullable: false),
                    EntitiesJson = table.Column<string>(type: "jsonb", nullable: false),
                    ResponseText = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    ResponseAudioUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EscalationTriggered = table.Column<bool>(type: "boolean", nullable: false),
                    FallbackReason = table.Column<int>(type: "integer", nullable: true),
                    ClarificationAttempts = table.Column<int>(type: "integer", nullable: false),
                    SlowMode = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoiceSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VoterTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ElectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoterTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WasteLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CafeteriaUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EstimatedCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    LoggedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WasteLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WellnessCheckIns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketId = table.Column<Guid>(type: "uuid", nullable: true),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CheckedInAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RequiresFollowUp = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WellnessCheckIns", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AchievementBadges");

            migrationBuilder.DropTable(
                name: "AchievementRoadmaps");

            migrationBuilder.DropTable(
                name: "AppealCommitteeBriefs");

            migrationBuilder.DropTable(
                name: "AppealEvidence");

            migrationBuilder.DropTable(
                name: "BenchmarkComparisons");

            migrationBuilder.DropTable(
                name: "BoardAlerts");

            migrationBuilder.DropTable(
                name: "BoardMeetingPacks");

            migrationBuilder.DropTable(
                name: "CafeteriaUnits");

            migrationBuilder.DropTable(
                name: "CandidateNominations");

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
                name: "Classes");

            migrationBuilder.DropTable(
                name: "ClassGenerationJobs");

            migrationBuilder.DropTable(
                name: "ComplianceBreaches");

            migrationBuilder.DropTable(
                name: "ConvocationAttendance");

            migrationBuilder.DropTable(
                name: "ConvocationEligibility");

            migrationBuilder.DropTable(
                name: "Convocations");

            migrationBuilder.DropTable(
                name: "CounselorProfiles");

            migrationBuilder.DropTable(
                name: "CourseEquivalencies");

            migrationBuilder.DropTable(
                name: "CourseGradePolicies");

            migrationBuilder.DropTable(
                name: "CourseOfferings");

            migrationBuilder.DropTable(
                name: "DeansListEntries");

            migrationBuilder.DropTable(
                name: "DefaulterInterventions");

            migrationBuilder.DropTable(
                name: "DefaulterRiskScores");

            migrationBuilder.DropTable(
                name: "DegreeCertificates");

            migrationBuilder.DropTable(
                name: "DegreeTrees");

            migrationBuilder.DropTable(
                name: "DemandForecasts");

            migrationBuilder.DropTable(
                name: "DirectorySyncLogs");

            migrationBuilder.DropTable(
                name: "DistributionAnomalyAlerts");

            migrationBuilder.DropTable(
                name: "ElectionResults");

            migrationBuilder.DropTable(
                name: "Elections");

            migrationBuilder.DropTable(
                name: "ElectiveSlots");

            migrationBuilder.DropTable(
                name: "EncryptedBallots");

            migrationBuilder.DropTable(
                name: "EvidenceFreshnessLogs");

            migrationBuilder.DropTable(
                name: "ExecutiveDashboards");

            migrationBuilder.DropTable(
                name: "FacultySlotMatches");

            migrationBuilder.DropTable(
                name: "FoodOrders");

            migrationBuilder.DropTable(
                name: "ForeignStudentRegistries");

            migrationBuilder.DropTable(
                name: "FreezeDocuments");

            migrationBuilder.DropTable(
                name: "GradeAppeals");

            migrationBuilder.DropTable(
                name: "GradeDistributions");

            migrationBuilder.DropTable(
                name: "IngestionJobs");

            migrationBuilder.DropTable(
                name: "InsightChips");

            migrationBuilder.DropTable(
                name: "InventoryAlerts");

            migrationBuilder.DropTable(
                name: "InventoryItems");

            migrationBuilder.DropTable(
                name: "LeaderboardEntries");

            migrationBuilder.DropTable(
                name: "LmsConfigurations");

            migrationBuilder.DropTable(
                name: "LmsProvisions");

            migrationBuilder.DropTable(
                name: "LmsRosterSyncLogs");

            migrationBuilder.DropTable(
                name: "MandatoryReports");

            migrationBuilder.DropTable(
                name: "MealPlans");

            migrationBuilder.DropTable(
                name: "MentalHealthTickets");

            migrationBuilder.DropTable(
                name: "MenuItems");

            migrationBuilder.DropTable(
                name: "MoraSubmissions");

            migrationBuilder.DropTable(
                name: "OcrProviderConfigs");

            migrationBuilder.DropTable(
                name: "PersonalityConfigs");

            migrationBuilder.DropTable(
                name: "PlagiarismApiSubmissions");

            migrationBuilder.DropTable(
                name: "PlagiarismScans");

            migrationBuilder.DropTable(
                name: "PrerequisiteConflicts");

            migrationBuilder.DropTable(
                name: "ProctorEvents");

            migrationBuilder.DropTable(
                name: "ProctoringSessions");

            migrationBuilder.DropTable(
                name: "ReadinessAlerts");

            migrationBuilder.DropTable(
                name: "ReadinessGaps");

            migrationBuilder.DropTable(
                name: "ReadinessScores");

            migrationBuilder.DropTable(
                name: "SemesterFreezes");

            migrationBuilder.DropTable(
                name: "StrategicPlanGoals");

            migrationBuilder.DropTable(
                name: "StudentBadges");

            migrationBuilder.DropTable(
                name: "StudentProctoringRecords");

            migrationBuilder.DropTable(
                name: "StudentSectionEnrollments");

            migrationBuilder.DropTable(
                name: "VendorScorecards");

            migrationBuilder.DropTable(
                name: "VisaDocuments");

            migrationBuilder.DropTable(
                name: "VisaExpiryAlerts");

            migrationBuilder.DropTable(
                name: "VisaRenewals");

            migrationBuilder.DropTable(
                name: "VisitingFacultyContracts");

            migrationBuilder.DropTable(
                name: "VisitingFacultyProfiles");

            migrationBuilder.DropTable(
                name: "VoiceSessions");

            migrationBuilder.DropTable(
                name: "VoterTokens");

            migrationBuilder.DropTable(
                name: "WasteLogs");

            migrationBuilder.DropTable(
                name: "WellnessCheckIns");
        }
    }
}
