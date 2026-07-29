namespace AegisCore.Application.Common.Interfaces;

/// <summary>
/// Service for brute force protection.
/// </summary>
public interface IBruteForceProtectionService
{
    /// <summary>
    /// Checks if the specified email is locked out.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <returns>True if locked out; otherwise false.</returns>
    Task<bool> IsLockedOutAsync(string email);

    /// <summary>
    /// Records a failed login attempt for the specified email.
    /// </summary>
    /// <param name="email">The email address.</param>
    Task RecordFailedAttemptAsync(string email);

    /// <summary>
    /// Clears failed login attempts for the specified email.
    /// </summary>
    /// <param name="email">The email address.</param>
    Task ClearFailedAttemptsAsync(string email);
}
