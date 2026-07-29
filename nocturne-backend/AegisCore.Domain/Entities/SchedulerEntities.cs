using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("ScheduledJobs")]
public class ScheduledJob
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public JobCategory Category { get; set; } = JobCategory.ReportGeneration;

    [MaxLength(100)]
    public string? Module { get; set; }

    [MaxLength(100)]
    public string CronExpression { get; set; } = "0 0 * * * ?";

    public bool IsEnabled { get; set; } = true;

    public int TimeoutSeconds { get; set; } = 300;

    public int MaxRetries { get; set; } = 3;

    [Column(TypeName = "jsonb")]
    public string ParametersJson { get; set; } = "{}";

    public DateTime? LastRunAt { get; set; }

    public DateTime? NextRunAt { get; set; }

    public Guid CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid TenantId { get; set; }

    public ICollection<JobExecution> Executions { get; set; } = new List<JobExecution>();
}

[Table("JobExecutions")]
public class JobExecution
{
    [Key]
    public Guid Id { get; set; }

    public Guid JobId { get; set; }

    public JobExecutionStatus Status { get; set; } = JobExecutionStatus.Queued;

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public long? DurationMs { get; set; }

    public int AttemptNumber { get; set; } = 1;

    [MaxLength(5000)]
    public string? ErrorMessage { get; set; }

    [MaxLength(2000)]
    public string? Output { get; set; }

    [MaxLength(100)]
    public string? TriggeredBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid TenantId { get; set; }

    [ForeignKey(nameof(JobId))]
    public ScheduledJob Job { get; set; } = null!;
}
