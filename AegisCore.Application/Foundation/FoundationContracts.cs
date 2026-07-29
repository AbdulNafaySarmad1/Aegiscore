using AegisCore.Domain.Entities;

namespace AegisCore.Application.Foundation;

public interface IStorageProvider
{
    Task<string> UploadAsync(string path, Stream content, string contentType, CancellationToken ct = default);
    Task<Stream> DownloadAsync(string path, CancellationToken ct = default);
    Task DeleteAsync(string path, CancellationToken ct = default);
    Task<bool> ExistsAsync(string path, CancellationToken ct = default);
    Task<IReadOnlyList<string>> ListAsync(string prefix, CancellationToken ct = default);
}

public interface IPolicyService
{
    Task<T?> GetAsync<T>(string module, string key, T? defaultValue = default, CancellationToken ct = default);
    Task SetAsync(string module, string key, object value, string description, Guid actorId, CancellationToken ct = default);
    Task<IReadOnlyList<PolicyConfig>> GetModulePoliciesAsync(string module, CancellationToken ct = default);
}

public interface IAcademicCalendarService
{
    Task<Semester> GetCurrentSemesterAsync(CancellationToken ct = default);
    Task<bool> IsAddDropOpenAsync(CancellationToken ct = default);
    Task<bool> IsExamPeriodAsync(CancellationToken ct = default);
    Task<int> GetWorkingDaysAsync(DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<IReadOnlyList<AcademicHoliday>> GetUpcomingHolidaysAsync(int days = 30, CancellationToken ct = default);
}

public interface IAuditService
{
    Task LogAsync(AuditEntry entry, CancellationToken ct = default);
    Task<PagedResult<AuditEntry>> QueryAsync(AuditQuery query, CancellationToken ct = default);
}

public sealed record AuditEntry(
    Guid? ActorId,
    string? ActorEmail,
    string Action,
    string Module,
    string EntityType,
    string? EntityId,
    string? OldValue,
    string? NewValue,
    string? IpAddress,
    string? UserAgent,
    string? SessionId);

public sealed record AuditQuery(string? Module, string? EntityType, Guid? ActorId, int Page = 1, int PageSize = 50);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
