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
    /// <param name="extraClaims">
    /// Claims the caller knows and the token service cannot — a student's own id, their department.
    /// </param>
    string GenerateAccessToken(User user, IReadOnlyDictionary<string, string>? extraClaims = null);

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

    /// <summary>
    /// Generates a new refresh token family and stores it in Redis.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>The generated family identifier.</returns>
    Task<Guid> GenerateRefreshTokenFamilyAsync(Guid userId);

    /// <summary>
    /// Marks a refresh token family as invalid in Redis.
    /// </summary>
    /// <param name="familyId">The family identifier.</param>
    Task InvalidateRefreshTokenFamilyAsync(Guid familyId);

    /// <summary>
    /// Checks whether a refresh token family is still valid.
    /// </summary>
    /// <param name="familyId">The family identifier.</param>
    /// <returns>True if the family is valid; otherwise false.</returns>
    Task<bool> IsRefreshTokenFamilyValidAsync(Guid familyId);
}
