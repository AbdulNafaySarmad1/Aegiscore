using System.Security.Cryptography;
using AegisCore.Application.Common.Interfaces;
using AegisCore.Contracts.Common.Interfaces;
using AegisCore.Application.Workflow;
using AegisCore.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace AegisCore.API.Controllers;

[ApiController, AllowAnonymous, Route("api/auth/activation")]
public sealed class AccountActivationController(AegisDbContext db, IConnectionMultiplexer redis,
    IPasswordHashingService passwords, INotificationService notifications, IConfiguration configuration) : ControllerBase
{
    private Guid Tenant => Guid.TryParse(configuration["TENANT_ID"], out var id) ? id : Guid.Empty;
    private string Key(string token) => $"aegiscore:activation:{Tenant:N}:{token}";

    [HttpPost("request")]
    public async Task<IActionResult> RequestLink(ActivationRequest input, CancellationToken ct)
    {
        var tenant = Tenant;
        if (tenant == Guid.Empty) return StatusCode(503);
        if (string.IsNullOrWhiteSpace(input.Email) || input.Email.Length > 320) return BadRequest();
        var email = input.Email.Trim().ToLowerInvariant();
        var cache = redis.GetDatabase();
        var throttle = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(email)));
        if (!await cache.StringSetAsync($"aegiscore:activation:throttle:{tenant:N}:{throttle}", "1", TimeSpan.FromMinutes(1), When.NotExists))
            return Ok(new { message = "If an account is awaiting activation, a link will arrive at its verified recovery email." });
        var user = await db.Users.IgnoreQueryFilters().SingleOrDefaultAsync(u => u.TenantId == tenant
            && (u.Email.ToLower() == email || u.RecoveryEmail != null && u.RecoveryEmail.ToLower() == email)
            && u.PasswordHash == null && u.Status == AegisCore.Domain.UserStatus.Active, ct);
        if (user is not null && !string.IsNullOrWhiteSpace(user.RecoveryEmail)
            && await db.Students.IgnoreQueryFilters().AnyAsync(s => s.TenantId == tenant && s.Id == user.Id && s.AdmissionApplicantProfileId != null, ct))
        {
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            await cache.StringSetAsync(Key(token), user.Id.ToString(), TimeSpan.FromMinutes(20));
            var origin = configuration["APP_URL"]?.TrimEnd('/') ?? throw new InvalidOperationException("APP_URL is required.");
            await notifications.SendEmailAsync(user.RecoveryEmail, "Activate your student account",
                $"<p>Set your password using this link within 20 minutes: <a href=\"{System.Net.WebUtility.HtmlEncode(origin)}/activate#{token}\">Activate account</a>.</p>", ct);
        }
        return Ok(new { message = "If an account is awaiting activation, a link will arrive at its verified recovery email." });
    }

    [HttpPost("complete")]
    public async Task<IActionResult> Complete(ActivationComplete input, CancellationToken ct)
    {
        if (Tenant == Guid.Empty || input.Token is null || input.Token.Length != 64
            || !input.Token.All(Uri.IsHexDigit) || string.IsNullOrWhiteSpace(input.Password) || input.Password.Length is < 16 or > 128)
            return BadRequest(new { error = "Use a valid activation link and a password of 16–128 characters." });
        var cache = redis.GetDatabase();
        var value = await cache.StringGetAsync(Key(input.Token));
        if (!Guid.TryParse(value.ToString(), out var userId)) return BadRequest(new { error = "Activation link is invalid or expired." });
        var hash = passwords.HashPassword(input.Password);
        // The conditional update makes multiple outstanding links and concurrent requests single-use.
        var changed = await db.Users.IgnoreQueryFilters().Where(u => u.TenantId == Tenant && u.Id == userId
            && u.PasswordHash == null && u.Status == AegisCore.Domain.UserStatus.Active)
            .ExecuteUpdateAsync(update => update.SetProperty(u => u.PasswordHash, hash), ct);
        await cache.KeyDeleteAsync(Key(input.Token));
        return changed == 1 ? Ok(new { activated = true }) : Conflict(new { error = "The account has already been activated or is unavailable." });
    }

    public sealed record ActivationRequest(string Email);
    public sealed record ActivationComplete(string Token, string Password);
}
