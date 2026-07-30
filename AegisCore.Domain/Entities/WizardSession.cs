using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

public sealed class WizardSession : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public int CurrentStep { get; set; }
    public string LicenseMode { get; set; } = "demo";
    public string? LicenseKey { get; set; }
    public string StepData { get; set; } = "{}";
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public string? CreatedByIp { get; set; }
}
