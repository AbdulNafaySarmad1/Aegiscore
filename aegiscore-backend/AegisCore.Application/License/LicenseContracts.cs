namespace AegisCore.Application.Licensing;

public interface ILicenseService
{
    Task<LicenseValidationResult> ValidateAsync(string licenseKey, CancellationToken ct = default);
    Task ActivateAsync(string licenseKey, Guid universityId, CancellationToken ct = default);
    Task<HeartbeatResult> SendHeartbeatAsync(CancellationToken ct = default);
    Task<bool> IsModuleEnabledAsync(string moduleName, CancellationToken ct = default);
    Task<LicenseRuntimeStatus> GetStatusAsync(CancellationToken ct = default);
    Task ManualOverrideAsync(Guid actorId, string reason, DateTimeOffset validUntil, CancellationToken ct = default);
}

public sealed record LicenseValidationResult(bool IsValid, string ValidationMessage, LicenseInfo? License, LicenseValidationFailureReason? FailureReason);

public sealed record LicenseInfo(
    Guid UniversityId,
    string UniversityName,
    DateOnly IssuedDate,
    DateOnly ExpiryDate,
    int MaxConcurrentUsers,
    IReadOnlyList<string> LicensedModules,
    string IssuedBy);

public sealed record HeartbeatResult(bool Success, string Status, string Message);

public sealed record LicenseRuntimeStatus(
    string Status,
    bool IsLocked,
    string? LockReason,
    DateTimeOffset? LockedAt,
    DateTimeOffset? LastHeartbeatAt,
    int FailedHeartbeatCount,
    DateTimeOffset? OverrideUntil);

public enum LicenseValidationFailureReason
{
    InvalidSignature,
    Expired,
    Revoked,
    UniversityMismatch,
    CouldNotContactServer
}
