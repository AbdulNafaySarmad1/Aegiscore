using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("LoginAuditRecords")]
public class LoginAuditRecord
{
    [Key] public Guid AuditId { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; }
    [MaxLength(300)] public string UserEmail { get; set; } = string.Empty;
    [MaxLength(100)] public string UserRole { get; set; } = string.Empty;
    public LoginEvent EventType { get; set; }
    public LoginProvider Provider { get; set; }
    [MaxLength(50)] public string IpAddress { get; set; } = string.Empty;
    [MaxLength(100)] public string? IpCountry { get; set; }
    [MaxLength(100)] public string? IpCity { get; set; }
    public double? GpsLatitude { get; set; }
    public double? GpsLongitude { get; set; }
    [MaxLength(500)] public string? UserAgent { get; set; }
    public DeviceType? DeviceType { get; set; }
    [MaxLength(100)] public string? OperatingSystem { get; set; }
    [MaxLength(100)] public string? Browser { get; set; }
    [MaxLength(200)] public string? SessionId { get; set; }
    public bool IsSuccessful { get; set; } = true;
    [MaxLength(200)] public string? FailureReason { get; set; }
    [MaxLength(200)] public string? MicrosoftOid { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
