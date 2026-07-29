using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("TeamsSyncLogs")]
public class TeamsSyncLog
{
    [Key] public Guid Id { get; set; }
    public Guid TimetableEntryId { get; set; }
    [MaxLength(200)] public string? TeamsTeamId { get; set; }
    [MaxLength(200)] public string? TeamsChannelId { get; set; }
    [MaxLength(20)] public string SyncStatus { get; set; } = "pending";
    [MaxLength(2000)] public string? ErrorMessage { get; set; }
    public DateTime? SyncedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("LmsSyncLogs")]
public class LmsSyncLog
{
    [Key] public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    [MaxLength(20)] public string SyncType { get; set; } = "enrollment";
    [MaxLength(20)] public string SyncStatus { get; set; } = "pending";
    [MaxLength(2000)] public string? ErrorMessage { get; set; }
    public DateTime? SyncedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("PowerBiReports")]
public class PowerBiReport
{
    [Key] public Guid Id { get; set; }
    [MaxLength(200)] public string ReportName { get; set; } = string.Empty;
    [MaxLength(200)] public string PowerBiReportId { get; set; } = string.Empty;
    [MaxLength(200)] public string PowerBiWorkspaceId { get; set; } = string.Empty;
    [MaxLength(200)] public string DatasetId { get; set; } = string.Empty;
    [MaxLength(800)] public string EmbedUrl { get; set; } = string.Empty;
    [MaxLength(50)] public string TargetRole { get; set; } = string.Empty;
    [MaxLength(2000)] public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
}
