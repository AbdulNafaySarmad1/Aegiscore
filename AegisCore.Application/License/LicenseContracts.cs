namespace AegisCore.Application.Licensing;

public enum LicenseTier { Basic, Standard, Premium }

public interface ILicenseService
{
    Task<LicenseValidationResult> ValidateAsync(string licenseKey, CancellationToken ct = default);
    Task ActivateAsync(string licenseKey, Guid universityId, CancellationToken ct = default);
    Task<HeartbeatResult> SendHeartbeatAsync(CancellationToken ct = default);
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
    string IssuedBy);

public sealed record HeartbeatResult(bool Success, string Status, string Message);

public sealed record LicenseRuntimeStatus(
    string Status,
    bool IsLocked,
    string? LockReason,
    DateTimeOffset? LockedAt,
    DateTimeOffset? LastHeartbeatAt,
    int FailedHeartbeatCount,
    DateTimeOffset? OverrideUntil,
    LicenseTier Tier = LicenseTier.Basic,
    /// <summary>The university the licence is bound to; an offline key must be issued for this id.</summary>
    Guid? UniversityId = null,
    string? UniversityName = null,
    /// <summary>"ok", "warning" (heartbeats failing, still writable) or "locked".</summary>
    string Severity = "ok",
    /// <summary>What is happening, in a sentence an IT administrator reads.</summary>
    string? Message = null,
    /// <summary>Exactly what to do about it.</summary>
    string? NextStep = null,
    /// <summary>While heartbeats fail, the moment the installation becomes read-only unless one succeeds.</summary>
    DateTimeOffset? GraceUntil = null,
    bool IsOffline = false,
    DateOnly? ExpiryDate = null);

public enum LicenseValidationFailureReason
{
    InvalidSignature,
    Expired,
    Revoked,
    UniversityMismatch,
    CouldNotContactServer
}
