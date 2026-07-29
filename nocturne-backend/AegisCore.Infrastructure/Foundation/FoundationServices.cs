using System.Text.Json;
using AegisCore.Application.Foundation;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Foundation;

public sealed class PolicyService(AegisDbContext db, IAuditService audit, ILogger<PolicyService> logger) : IPolicyService
{
    public async Task<T?> GetAsync<T>(string module, string key, T? defaultValue = default, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var candidates = await db.PolicyConfigs
            .Where(p => p.Module == module && p.Key == key && p.EffectiveTo == null)
            .ToListAsync(ct);
        var value = candidates
            .Where(p => p.EffectiveFrom <= now)
            .OrderByDescending(p => p.EffectiveFrom)
            .Select(p => p.Value)
            .FirstOrDefault();

        if (value is null)
        {
            return defaultValue;
        }

        try
        {
            if (typeof(T) == typeof(string))
            {
                return (T)(object)value;
            }

            return JsonSerializer.Deserialize<T>(value);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Policy {Module}.{Key} could not be converted to {Type}", module, key, typeof(T).Name);
            return defaultValue;
        }
    }

    public async Task SetAsync(string module, string key, object value, string description, Guid actorId, CancellationToken ct = default)
    {
        var activePolicies = await db.PolicyConfigs
            .Where(p => p.Module == module && p.Key == key && p.EffectiveTo == null)
            .ToListAsync(ct);
        var current = activePolicies
            .OrderByDescending(p => p.EffectiveFrom)
            .FirstOrDefault();

        var serialized = value is string text ? text : JsonSerializer.Serialize(value);
        var type = value switch
        {
            bool => "bool",
            int or long => "int",
            decimal or double or float => "decimal",
            string => "string",
            _ => "json"
        };

        if (current is null)
        {
            db.PolicyConfigs.Add(new PolicyConfig
            {
                Module = module,
                Key = key,
                Value = serialized,
                ValueType = type,
                Description = description,
                CreatedBy = actorId
            });
        }
        else
        {
            current.Value = serialized;
            current.ValueType = type;
            current.Description = description;
            current.UpdatedBy = actorId;
            current.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        await audit.LogAsync(new AuditEntry(actorId, null, "policy.updated", module, "PolicyConfig", $"{module}:{key}", null, serialized, null, null, null), ct);
    }

    public async Task<IReadOnlyList<PolicyConfig>> GetModulePoliciesAsync(string module, CancellationToken ct = default)
        => await db.PolicyConfigs
            .Where(p => p.Module == module && p.EffectiveTo == null)
            .OrderBy(p => p.Key)
            .ToListAsync(ct);
}

public sealed class AcademicCalendarService(AegisDbContext db) : IAcademicCalendarService
{
    public async Task<Semester> GetCurrentSemesterAsync(CancellationToken ct = default)
        => await db.Semesters
            .Include(s => s.AcademicYear)
            .FirstOrDefaultAsync(s => s.IsCurrent, ct)
            ?? throw new InvalidOperationException("No current semester is configured.");

    public async Task<bool> IsAddDropOpenAsync(CancellationToken ct = default)
    {
        var semester = await GetCurrentSemesterAsync(ct);
        return semester.AddDropEndDate is { } end && DateOnly.FromDateTime(DateTime.UtcNow) <= end;
    }

    public async Task<bool> IsExamPeriodAsync(CancellationToken ct = default)
    {
        var semester = await GetCurrentSemesterAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return semester.ExamStartDate is { } start && semester.ExamEndDate is { } end && today >= start && today <= end;
    }

    public async Task<int> GetWorkingDaysAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        if (to < from)
        {
            return 0;
        }

        var holidays = await db.AcademicHolidays
            .Where(h => h.StartDate <= to && h.EndDate >= from)
            .ToListAsync(ct);

        var count = 0;
        for (var day = from; day <= to; day = day.AddDays(1))
        {
            if (day.DayOfWeek is System.DayOfWeek.Saturday or System.DayOfWeek.Sunday)
            {
                continue;
            }

            if (holidays.Any(h => day >= h.StartDate && day <= h.EndDate))
            {
                continue;
            }

            count++;
        }

        return count;
    }

    public async Task<IReadOnlyList<AcademicHoliday>> GetUpcomingHolidaysAsync(int days = 30, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var until = today.AddDays(Math.Max(0, days));

        return await db.AcademicHolidays
            .Where(h => h.StartDate >= today && h.StartDate <= until)
            .OrderBy(h => h.StartDate)
            .ToListAsync(ct);
    }
}

public sealed class AuditService(AegisDbContext db) : IAuditService
{
    public async Task LogAsync(AuditEntry entry, CancellationToken ct = default)
    {
        db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = entry.ActorId ?? Guid.Empty,
            Action = entry.Action,
            ResourceType = $"{entry.Module}:{entry.EntityType}",
            ResourceId = Guid.TryParse(entry.EntityId, out var id) ? id : Guid.Empty,
            Details = JsonSerializer.Serialize(new
            {
                entry.ActorEmail,
                entry.EntityId,
                entry.OldValue,
                entry.NewValue,
                entry.UserAgent,
                entry.SessionId
            }),
            Success = true,
            IpAddress = entry.IpAddress,
            Timestamp = DateTime.UtcNow
        });

        await db.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<AuditEntry>> QueryAsync(AuditQuery query, CancellationToken ct = default)
    {
        var rows = db.AuditLogs.AsNoTracking().AsQueryable();

        if (query.ActorId is { } actorId)
        {
            rows = rows.Where(a => a.UserId == actorId);
        }

        if (!string.IsNullOrWhiteSpace(query.Module))
        {
            rows = rows.Where(a => a.ResourceType.StartsWith(query.Module + ":"));
        }

        if (!string.IsNullOrWhiteSpace(query.EntityType))
        {
            rows = rows.Where(a => a.ResourceType.EndsWith(":" + query.EntityType));
        }

        var total = await rows.CountAsync(ct);
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);
        var items = await rows.OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditEntry(a.UserId, null, a.Action, a.ResourceType, a.ResourceType, a.ResourceId.ToString(), null, a.Details, a.IpAddress, null, null))
            .ToListAsync(ct);

        return new PagedResult<AuditEntry>(items, total, page, pageSize);
    }
}
