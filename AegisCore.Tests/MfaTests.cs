using System.Security.Claims;
using System.Security.Cryptography;
using AegisCore.API.Authorization;
using AegisCore.API.Middleware;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure.Security.Mfa;
using AegisCore.Infrastructure.Security.Pqc;
using AegisCore.Infrastructure.SsoHitl;
using AegisCore.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AegisCore.Tests;

/// <summary>Compliance control AC-27: administrators sign in with a second factor, and a password-only session cannot reach the rest of the API.</summary>
public sealed class MfaTests
{
    private static MfaService Service(AegisCore.Infrastructure.AegisDbContext db) =>
        new(db, new TwoFactorService(), new PqcCryptographyService(Options.Create(new PqcCryptographyOptions { MasterKeyBase64 = TestKeys.MasterKeyBase64 }), NullLogger<PqcCryptographyService>.Instance));

    /// <summary>RFC 6238, the same arithmetic an authenticator app runs.</summary>
    private static string CodeFor(string base32Secret, DateTimeOffset at)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var bits = string.Concat(base32Secret.TrimEnd('=').Select(c => Convert.ToString(alphabet.IndexOf(c), 2).PadLeft(5, '0')));
        var key = Enumerable.Range(0, bits.Length / 8).Select(i => Convert.ToByte(bits.Substring(i * 8, 8), 2)).ToArray();
        var counter = BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(at.ToUnixTimeSeconds() / 30));
        var hash = HMACSHA1.HashData(key, counter);
        var offset = hash[^1] & 0x0f;
        var binary = ((hash[offset] & 0x7f) << 24) | ((hash[offset + 1] & 0xff) << 16) | ((hash[offset + 2] & 0xff) << 8) | (hash[offset + 3] & 0xff);
        return (binary % 1_000_000).ToString("D6");
    }

    [Theory, DatabaseProviders]
    public async Task Enrol_confirm_and_verify_with_an_authenticator_code_or_a_backup_code_once(bool postgres)
    {
        var tenant = Guid.NewGuid();
        await using var database = await TestDatabase.CreateAsync(postgres);
        await database.EnsureSchemaAsync(tenant);
        await using var db = database.CreateContext(tenant);
        var user = new User { Id = Guid.NewGuid(), TenantId = tenant, Username = "admin", Email = "admin@example.edu", FirstName = "A", LastName = "B", Status = UserStatus.Active };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var mfa = Service(db);

        Assert.False(await mfa.HasEnabledFactorAsync(user));
        var enrolment = await mfa.EnrollAsync(user);
        Assert.Equal(10, enrolment.BackupCodes.Count);
        Assert.StartsWith("otpauth://totp/", enrolment.OtpAuthUri);
        Assert.False(await mfa.HasEnabledFactorAsync(user), "nothing is enforced until the code is confirmed");

        await Assert.ThrowsAsync<InvalidOperationException>(() => mfa.ConfirmAsync(user, "000000"));
        await mfa.ConfirmAsync(user, CodeFor(enrolment.Secret, DateTimeOffset.UtcNow));
        Assert.True(await mfa.HasEnabledFactorAsync(user));

        var stored = await db.UserSecondFactors.IgnoreQueryFilters().SingleAsync(f => f.UserId == user.Id);
        Assert.DoesNotContain(enrolment.Secret, stored.SecretEncrypted);
        Assert.All(enrolment.BackupCodes, code => Assert.DoesNotContain(code, stored.BackupCodesHashCsv));

        Assert.True(await mfa.VerifyAsync(user, CodeFor(enrolment.Secret, DateTimeOffset.UtcNow)));
        Assert.False(await mfa.VerifyAsync(user, "123456"));
        Assert.True(await mfa.VerifyAsync(user, enrolment.BackupCodes[0]));
        Assert.False(await mfa.VerifyAsync(user, enrolment.BackupCodes[0]), "a backup code works once");
        Assert.Equal(9, (await mfa.StatusAsync(user, ["Admin"], null)).BackupCodesLeft);
    }

    private static async Task<int> RunMiddlewareAsync(string path, string[] roles, string? amr, string? requiredRoles = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        var claims = roles.Select(r => new Claim(ClaimTypes.Role, r)).ToList();
        if (amr is not null) claims.Add(new Claim(SessionTokenIssuer.AmrClaim, amr));
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var settings = new Dictionary<string, string?>();
        if (requiredRoles is not null)
            foreach (var (role, i) in requiredRoles.Split(',').Select((r, i) => (r, i))) settings[$"Security:MfaRequiredRoles:{i}"] = role;
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var reached = false;
        var middleware = new MfaEnforcementMiddleware(_ => { reached = true; return Task.CompletedTask; });
        await middleware.InvokeAsync(context, configuration);
        return reached ? 200 : context.Response.StatusCode;
    }

    [Fact]
    public async Task A_password_only_admin_session_is_refused_everywhere_except_sign_in_and_enrolment()
    {
        Assert.Equal(403, await RunMiddlewareAsync("/api/hr/employees", ["Admin"], SessionTokenIssuer.AmrPassword));
        Assert.Equal(403, await RunMiddlewareAsync("/api/hr/employees", ["SuperAdmin"], null));
        Assert.Equal(200, await RunMiddlewareAsync("/api/auth/mfa/enroll", ["Admin"], SessionTokenIssuer.AmrPassword));
        Assert.Equal(200, await RunMiddlewareAsync("/api/session", ["Admin"], SessionTokenIssuer.AmrPassword));
        Assert.Equal(200, await RunMiddlewareAsync("/api/hr/employees", ["Admin"], SessionTokenIssuer.AmrMfa));
        Assert.Equal(200, await RunMiddlewareAsync("/api/hr/employees", ["Admin"], SessionTokenIssuer.AmrSso));
        Assert.Equal(200, await RunMiddlewareAsync("/api/attendance", ["Student"], SessionTokenIssuer.AmrPassword));
        Assert.Equal(403, await RunMiddlewareAsync("/api/finance/v2/dashboard", ["CFO"], SessionTokenIssuer.AmrPassword, requiredRoles: "Admin,CFO"));
    }
}
