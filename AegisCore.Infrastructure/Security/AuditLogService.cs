using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using AegisCore.Infrastructure;
using AegisCore.Domain.Entities;
using AegisCore.Application.Common.Interfaces;
using AegisCore.Contracts.Common.Interfaces;

namespace AegisCore.Infrastructure.Security;

/// <summary>
/// Service for audit logging of critical actions.
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly AegisDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditLogService"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    public AuditLogService(AegisDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public async Task LogActionAsync(string action, string resourceType, Guid resourceId, string? details = null, bool success = true)
    {
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
        var tenantId = _httpContextAccessor.HttpContext?.Items["TenantId"] as Guid?;

        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = Guid.TryParse(userId, out var parsedUserId) ? parsedUserId : Guid.Empty,
            TenantId = tenantId ?? Guid.Empty,
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Details = details,
            Success = success,
            Timestamp = DateTime.UtcNow,
            IpAddress = ipAddress
        };

        _dbContext.AuditLogs.Add(auditLog);
        await _dbContext.SaveChangesAsync();
    }
}
