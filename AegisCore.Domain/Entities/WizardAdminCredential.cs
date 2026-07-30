using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

public sealed class WizardAdminCredential : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public Guid TenantId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
