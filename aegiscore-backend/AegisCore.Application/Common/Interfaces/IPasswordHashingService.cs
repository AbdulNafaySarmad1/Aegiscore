namespace AegisCore.Application.Common.Interfaces;

/// <summary>
/// Service for password hashing and verification.
/// </summary>
public interface IPasswordHashingService
{
    /// <summary>
    /// Hashes a password using Argon2id.
    /// </summary>
    /// <param name="password">The plain text password.</param>
    /// <returns>The hashed password.</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verifies a password against a stored hash.
    /// </summary>
    /// <param name="password">The plain text password.</param>
    /// <param name="hash">The stored hash.</param>
    /// <returns>True if the password matches the hash.</returns>
    bool VerifyPassword(string password, string hash);
}
