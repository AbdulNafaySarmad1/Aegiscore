using System.Security.Claims;
using AegisCore.Domain.Entities;

namespace AegisCore.Application.Common.Interfaces;

/// <summary>
/// Service for JWT token generation and validation.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates an access token for the specified user.
    /// </summary>
    /// <param name="user">The user entity.</param>
    /// <returns>The JWT access token.</returns>
    string GenerateAccessToken(User user);

    /// <summary>
    /// Generates a refresh token.
    /// </summary>
    /// <returns>A random refresh token.</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// Validates a JWT token and returns the claims principal.
    /// </summary>
    /// <param name="token">The JWT token.</param>
    /// <returns>The claims principal if valid; otherwise null.</returns>
    ClaimsPrincipal? ValidateToken(string token);
}
