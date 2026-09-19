using AegisCore.Application.Licensing;

namespace AegisCore.Infrastructure.Licensing;

/// <summary>
/// The one sentence fragment that explains an engaged licence lock, so a person sees the same
/// explanation whether the lock bit a write, a request, or their very first SSO sign-in.
/// </summary>
public static class LicenseLockDescription
{
    public static string Why(LicenseRuntimeStatus status) => status.LockReason switch
    {
        "heartbeat_failed" => "the licence authority could not be reached for two consecutive heartbeats",
        "expired" => "the licence has expired",
        "revoked" => "the licence was revoked by the vendor",
        "invalid" => "the licence key is not valid",
        "pending_activation" => "no licence has been activated yet",
        _ => "the licence is locked (" + (status.LockReason ?? status.Status) + ")"
    };
}
