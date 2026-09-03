using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("ImportBatches")]
public class ImportBatch : ITenantScoped
{
    [Key]
    public Guid Id { get; set; }

    [MaxLength(300)]
    public string FileName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Format { get; set; } = "csv";

    [MaxLength(100)]
    public string Domain { get; set; } = string.Empty;

    [Column(TypeName = "jsonb")]
    public string ColumnMappingsJson { get; set; } = "{}";

    public int TotalRows { get; set; }

    public int SuccessCount { get; set; }

    public int ErrorCount { get; set; }

    public string Status { get; set; } = "Processing";

    [MaxLength(5000)]
    public string? ErrorReportJson { get; set; }

    public Guid UploadedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    public bool IsRolledBack { get; set; }

    public Guid TenantId { get; set; }
}

[Table("ExportLogs")]
public class ExportLog : ITenantScoped
{
    [Key]
    public Guid Id { get; set; }

    [MaxLength(100)]
    public string Domain { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Format { get; set; } = "csv";

    public int RowCount { get; set; }

    [MaxLength(500)]
    public string? FileUrl { get; set; }

    public Guid ExportedByUserId { get; set; }

    [MaxLength(200)]
    public string? ExportedByName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresAt { get; set; }

    public bool IsEncrypted { get; set; }

    public Guid TenantId { get; set; }
}

[Table("PlatformMetrics")]
public class PlatformMetric : ITenantScoped
{
    [Key]
    public Guid Id { get; set; }

    [MaxLength(100)]
    public string MetricName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,4)")]
    public decimal Value { get; set; }

    [MaxLength(50)]
    public string? Unit { get; set; }

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public Guid TenantId { get; set; }
}
