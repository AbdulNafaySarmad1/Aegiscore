namespace AegisCore.Infrastructure.Licensing;

/// <summary>
/// Raised when a write reaches the database while the installation licence is locked. It is an
/// InvalidOperationException so existing handlers keep working, and its own type so the API can
/// answer 403 with the licence page instead of a 500 with a reference number.
/// </summary>
public sealed class LicenseReadOnlyException(string reason)
    : InvalidOperationException("The installation licence is read-only: " + reason + ". No business records were changed. An administrator can restore write access on the licence page (/admin/license) by entering an offline licence key or reconnecting to the licence authority.")
{
    public string Reason { get; } = reason;
}
