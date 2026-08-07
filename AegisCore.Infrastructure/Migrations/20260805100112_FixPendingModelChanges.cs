using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixPendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataImportJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SourceQueryJson = table.Column<string>(type: "jsonb", nullable: false),
                    FieldMappingsJson = table.Column<string>(type: "jsonb", nullable: false),
                    TotalRows = table.Column<int>(type: "integer", nullable: false),
                    SuccessRows = table.Column<int>(type: "integer", nullable: false),
                    ErrorRows = table.Column<int>(type: "integer", nullable: false),
                    SkippedRows = table.Column<int>(type: "integer", nullable: false),
                    ErrorLogJson = table.Column<string>(type: "jsonb", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TriggeredByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataImportJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentAccessLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AccessedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AccessedByRole = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    WasDenied = table.Column<bool>(type: "boolean", nullable: false),
                    DenialReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentAccessLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentRetentionPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentCategory = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TierLevel = table.Column<int>(type: "integer", nullable: false),
                    RetentionYears = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsOverride = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentRetentionPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentTiers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TierLevel = table.Column<int>(type: "integer", nullable: false),
                    RetentionYears = table.Column<int>(type: "integer", nullable: false),
                    RetentionDays = table.Column<int>(type: "integer", nullable: false),
                    ObjectLockMode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BucketName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    StoragePrefix = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RequirePqcVerification = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTiers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentVaultEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    DocumentCategory = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TierId = table.Column<Guid>(type: "uuid", nullable: false),
                    TierLevel = table.Column<int>(type: "integer", nullable: false),
                    PqcSignatureBase64 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PqcPublicKeyBase64 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Sha512Hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IsPqcVerified = table.Column<bool>(type: "boolean", nullable: false),
                    PqcVerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StorageUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    StorageBucket = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RetentionExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRetentionLocked = table.Column<bool>(type: "boolean", nullable: false),
                    LockMode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LockedUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ObjectLockLegalHoldId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedByRole = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    VerifiedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletionReason = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentVaultEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExternalDbConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DbType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ConnectionString = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Schema = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastTestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastTestSuccessful = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalDbConnections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FieldMappingTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SourceDbType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MappingJson = table.Column<string>(type: "jsonb", nullable: false),
                    IsBuiltIn = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldMappingTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProfilePictureRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    FaceValidated = table.Column<bool>(type: "boolean", nullable: false),
                    FaceConfidence = table.Column<double>(type: "double precision", nullable: true),
                    EditingDetected = table.Column<bool>(type: "boolean", nullable: false),
                    EditingProbability = table.Column<double>(type: "double precision", nullable: true),
                    FullStorageUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ThumbnailStorageUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastValidatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RejectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Sha512Hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfilePictureRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReplicaHealthLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReplicaId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsHealthy = table.Column<bool>(type: "boolean", nullable: false),
                    LagSeconds = table.Column<int>(type: "integer", nullable: true),
                    ActiveConnections = table.Column<int>(type: "integer", nullable: true),
                    QueriesPerSecond = table.Column<double>(type: "double precision", nullable: true),
                    LagThresholdBreached = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CheckedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplicaHealthLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReplicationLagAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReplicaId = table.Column<Guid>(type: "uuid", nullable: false),
                    LagSeconds = table.Column<int>(type: "integer", nullable: false),
                    ThresholdSeconds = table.Column<int>(type: "integer", nullable: false),
                    IsCritical = table.Column<bool>(type: "boolean", nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsResolved = table.Column<bool>(type: "boolean", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TriggeredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplicationLagAlerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReplicationScaleEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PreviousReplicaCount = table.Column<int>(type: "integer", nullable: false),
                    NewReplicaCount = table.Column<int>(type: "integer", nullable: false),
                    TriggerReason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TriggeredBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    WasSuccessful = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TriggeredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplicationScaleEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReplicationScalingConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MinReplicas = table.Column<int>(type: "integer", nullable: false),
                    MaxReplicas = table.Column<int>(type: "integer", nullable: false),
                    TargetReplicas = table.Column<int>(type: "integer", nullable: false),
                    ScaleUpThresholdConnections = table.Column<int>(type: "integer", nullable: false),
                    ScaleUpThresholdQueriesPerSecond = table.Column<int>(type: "integer", nullable: false),
                    ScaleDownThresholdConnections = table.Column<int>(type: "integer", nullable: false),
                    ScaleDownCooldownMinutes = table.Column<int>(type: "integer", nullable: false),
                    DefaultMaxLagSeconds = table.Column<int>(type: "integer", nullable: false),
                    CriticalLagSeconds = table.Column<int>(type: "integer", nullable: false),
                    AutoScalingEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AutoFailoverEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ReadRoutingEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ReadDistributionStrategy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplicationScalingConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyncLagConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DefaultGracePeriodSeconds = table.Column<int>(type: "integer", nullable: false),
                    MaxGracePeriodSeconds = table.Column<int>(type: "integer", nullable: false),
                    AutoCommitOnGraceExpiry = table.Column<bool>(type: "boolean", nullable: false),
                    RequireAdminApprovalForCorrections = table.Column<bool>(type: "boolean", nullable: false),
                    CorrectionPermission = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NotifyOwnerOnCorrection = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncLagConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyncLagEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReplicaId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OriginalValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    WrittenValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    WrittenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GraceExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CommittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CorrectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WrittenByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrectedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                    LockedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncLagEntries", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataImportJobs");

            migrationBuilder.DropTable(
                name: "DocumentAccessLogs");

            migrationBuilder.DropTable(
                name: "DocumentRetentionPolicies");

            migrationBuilder.DropTable(
                name: "DocumentTiers");

            migrationBuilder.DropTable(
                name: "DocumentVaultEntries");

            migrationBuilder.DropTable(
                name: "ExternalDbConnections");

            migrationBuilder.DropTable(
                name: "FieldMappingTemplates");

            migrationBuilder.DropTable(
                name: "ProfilePictureRecords");

            migrationBuilder.DropTable(
                name: "ReplicaHealthLogs");

            migrationBuilder.DropTable(
                name: "ReplicationLagAlerts");

            migrationBuilder.DropTable(
                name: "ReplicationScaleEvents");

            migrationBuilder.DropTable(
                name: "ReplicationScalingConfigs");

            migrationBuilder.DropTable(
                name: "SyncLagConfigs");

            migrationBuilder.DropTable(
                name: "SyncLagEntries");
        }
    }
}
