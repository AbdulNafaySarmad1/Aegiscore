using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

/// <summary>
/// Something in the security signals that a person should look at: a sign-in from a new country,
/// a run of failed sign-ins, or one account reading or exporting an unusual number of student
/// records. Distinct from <see cref="SecurityIncident"/>, which is campus security (buildings,
/// gates, guards). Compliance control AC-54; the incident procedure is docs/compliance/11.
/// </summary>
[Table("SecurityAlerts")]
public class SecurityAlert : ITenantScoped
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }

    /// <summary>"NewCountryLogin", "BruteForce", "BulkRecordAccess", "BulkExport".</summary>
    [Required, MaxLength(60)] public string Type { get; set; } = string.Empty;

    /// <summary>"Low", "Medium", "High", "Critical" — the severities in docs/compliance/11 §3.</summary>
    [Required, MaxLength(20)] public string Severity { get; set; } = "Medium";

    [Required, MaxLength(1000)] public string Description { get; set; } = string.Empty;

    /// <summary>The account the alert is about, where there is one.</summary>
    public Guid? SubjectUserId { get; set; }

    /// <summary>How many events the detector counted, so a reviewer can judge without re-querying.</summary>
    public int Count { get; set; }

    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcknowledgedAt { get; set; }
    public Guid? AcknowledgedByUserId { get; set; }
    [MaxLength(1000)] public string? AcknowledgementNote { get; set; }
}
