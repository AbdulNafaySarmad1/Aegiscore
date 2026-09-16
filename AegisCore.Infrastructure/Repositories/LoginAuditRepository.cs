using System.Net;
using System.Text;
using System.Text.Json;
using AegisCore.Contracts.Abstractions.Cloud;
using AegisCore.Application.Common.Interfaces;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Repositories;

public class LoginAuditRecordRepository : Repository<LoginAuditRecord>, ILoginAuditRecordRepository
{
    public LoginAuditRecordRepository(AegisDbContext c) : base(c) { }

    public async Task<IReadOnlyList<LoginAuditRecord>> GetByUserAsync(Guid userId, DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
    {
        var q = _dbSet.Where(r => r.UserId == userId);
        if (from.HasValue) q = q.Where(r => r.OccurredAt >= from.Value);
        if (to.HasValue) q = q.Where(r => r.OccurredAt <= to.Value);
        return await q.OrderByDescending(r => r.OccurredAt).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<LoginAuditRecord>> GetByIpAsync(string ip, CancellationToken ct = default)
        => await _dbSet.Where(r => r.IpAddress == ip).OrderByDescending(r => r.OccurredAt).ToListAsync(ct);

    public async Task<IReadOnlyList<LoginAuditRecord>> GetFailedAsync(Guid? tenantId = null, int limit = 100, CancellationToken ct = default)
    {
        var q = _dbSet.Where(r => r.EventType == LoginEvent.LoginFailed);
        if (tenantId.HasValue) q = q.Where(r => r.TenantId == tenantId.Value);
        return await q.OrderByDescending(r => r.OccurredAt).Take(limit).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<LoginAuditRecord>> GetByEventTypeAsync(LoginEvent type, DateTime? from = null, CancellationToken ct = default)
    {
        var q = _dbSet.Where(r => r.EventType == type);
        if (from.HasValue) q = q.Where(r => r.OccurredAt >= from.Value);
        return await q.OrderByDescending(r => r.OccurredAt).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<LoginAuditRecord>> GetActiveSessionsAsync(CancellationToken ct = default)
    {
        var loginEventTypes = new[] { LoginEvent.Login, LoginEvent.TokenRefresh };
        return await _dbSet.Where(r => loginEventTypes.Contains(r.EventType) && r.UserId.HasValue)
            .GroupBy(r => r.UserId).Select(g => g.OrderByDescending(r => r.OccurredAt).First())
            .ToListAsync(ct);
    }
}
