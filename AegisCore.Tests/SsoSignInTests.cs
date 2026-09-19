using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AegisCore.API.Authorization;
using AegisCore.API.Controllers;
using AegisCore.Application.Common.Interfaces;
using AegisCore.Application.Foundation;
using AegisCore.Application.Identity;
using AegisCore.Application.Licensing;
using AegisCore.Application.Onboarding;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Role = AegisCore.Domain.Entities.Role;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Identity;
using AegisCore.Infrastructure.Identity.Oidc;
using AegisCore.Infrastructure.Licensing;
using AegisCore.Infrastructure.Onboarding;
using AegisCore.Infrastructure.Repositories;
using AegisCore.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

namespace AegisCore.Tests;

/// <summary>
/// Sign-in through a tenant's identity provider: the parts that can be proven without a real
/// directory. What needs a real Entra, Google or Okta account is listed in docs/sso-verification.md.
/// </summary>
public sealed class SsoSignInTests
{
    // ── 5. External identity upsert: (issuer, subject, tenant), never subject alone ─────────────────

    [Fact]
    public async Task SameSubjectFromTwoIssuers_MapsToTwoDifferentUsers()
    {
        await using var fixture = await Fixture.CreateAsync();
        var identity = fixture.IdentityService();
        var tenant = fixture.TenantId;

        var fromDirectoryA = await identity.UpsertUserFromExternalIdentityAsync(
            External("https://login.microsoftonline.com/aaaaaaaa-0000-0000-0000-000000000001/v2.0", "oid-123", "ali@uni-a.test"), tenant, AuthProvider.AzureAd);
        var fromDirectoryB = await identity.UpsertUserFromExternalIdentityAsync(
            External("https://login.microsoftonline.com/bbbbbbbb-0000-0000-0000-000000000002/v2.0", "oid-123", "sara@uni-b.test"), tenant, AuthProvider.AzureAd);
        var fromDirectoryAAgain = await identity.UpsertUserFromExternalIdentityAsync(
            External("https://login.microsoftonline.com/aaaaaaaa-0000-0000-0000-000000000001/v2.0", "oid-123", "ali@uni-a.test"), tenant, AuthProvider.AzureAd);

        Assert.NotEqual(fromDirectoryA.Id, fromDirectoryB.Id);
        Assert.Equal(fromDirectoryA.Id, fromDirectoryAAgain.Id);
        Assert.Equal(2, await fixture.Db.Users.IgnoreQueryFilters().CountAsync(u => u.ExternalId == "oid-123"));
    }

    [Fact]
    public async Task SameIssuerAndSubject_InAnotherTenant_IsAnotherUser_AndTheIndexRefusesADuplicate()
    {
        await using var fixture = await Fixture.CreateAsync();
        var identity = fixture.IdentityService();
        const string issuer = "https://accounts.google.com";

        var first = await identity.UpsertUserFromExternalIdentityAsync(External(issuer, "sub-1", "p@uni.test"), fixture.TenantId, AuthProvider.Google);
        var second = await identity.UpsertUserFromExternalIdentityAsync(External(issuer, "sub-1", "p@other.test"), Guid.NewGuid(), AuthProvider.Google);
        Assert.NotEqual(first.Id, second.Id);

        // The database itself holds the line: a second row for the same (tenant, issuer, subject) is refused.
        fixture.Db.Users.Add(new User { Id = Guid.NewGuid(), TenantId = fixture.TenantId, Username = "dup", Email = "dup@uni.test", ExternalIssuer = issuer, ExternalId = "sub-1", AuthProvider = AuthProvider.Google });
        await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Db.SaveChangesAsync());
    }

    [Fact]
    public async Task JitProvisionedUser_HasNoPassword_AndPasswordControllerRefusesToSetOne()
    {
        await using var fixture = await Fixture.CreateAsync();
        var user = await fixture.IdentityService().UpsertUserFromExternalIdentityAsync(
            External("https://dev-1.okta.com/oauth2/default", "00u1", "new@uni.test"), fixture.TenantId, AuthProvider.Oidc);

        Assert.Null(user.PasswordHash);
        Assert.Equal(AuthProvider.Oidc, user.AuthProvider);

        // The controller runs inside the tenant, as it would behind TenantContextMiddleware.
        var http = new DefaultHttpContext();
        http.Items["TenantId"] = fixture.TenantId;
        fixture.Http.HttpContext = http;
        var passwords = new Mock<IPasswordHashingService>();
        var controller = new PasswordController(fixture.Db, passwords.Object, Mock.Of<IAuditService>())
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };
        var result = await controller.SetPassword(user.Id, new PasswordController.SetPasswordRequest(new string('x', 20)), CancellationToken.None);
        Assert.IsType<ConflictObjectResult>(result);
        passwords.Verify(p => p.HashPassword(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExistingLocalAccount_IsLinkedByEmail_AndKeepsItsPassword()
    {
        await using var fixture = await Fixture.CreateAsync();
        var admin = new User { Id = Guid.NewGuid(), TenantId = fixture.TenantId, Username = "admin", Email = "admin@uni.test", PasswordHash = "hash", AuthProvider = AuthProvider.Local, FirstName = "Ada" };
        fixture.Db.Users.Add(admin);
        await fixture.Db.SaveChangesAsync();

        var linked = await fixture.IdentityService().UpsertUserFromExternalIdentityAsync(
            External("https://login.microsoftonline.com/t/v2.0", "oid-admin", "Admin@Uni.test"), fixture.TenantId, AuthProvider.AzureAd);

        Assert.Equal(admin.Id, linked.Id);
        Assert.Equal("hash", linked.PasswordHash);
        Assert.Equal(AuthProvider.Local, linked.AuthProvider);
        Assert.Equal("oid-admin", linked.ExternalId);
        Assert.Equal("https://login.microsoftonline.com/t/v2.0", linked.ExternalIssuer);

        // The same email arriving from a different issuer is a different person or a misconfiguration: refused, not merged.
        var conflict = await Assert.ThrowsAsync<ExternalSignInException>(() => fixture.IdentityService().UpsertUserFromExternalIdentityAsync(
            External("https://accounts.google.com", "g-1", "admin@uni.test"), fixture.TenantId, AuthProvider.Google));
        Assert.Equal("identity_conflict", conflict.Code);
    }

    // ── 4. One token-issuance path: SSO and local tokens have the same claim shape ──────────────────

    [Fact]
    public async Task TokenIssuedAfterSso_HasTheSameClaimShape_AsTokenIssuedAfterPasswordLogin()
    {
        await using var fixture = await Fixture.CreateAsync();
        var role = new Role { Id = Guid.NewGuid(), TenantId = fixture.TenantId, Name = "Faculty" };
        fixture.Db.Roles.Add(role);
        var local = new User { Id = Guid.NewGuid(), TenantId = fixture.TenantId, Username = "local", Email = "local@uni.test", PasswordHash = "hash", FirstName = "Lo", LastName = "Cal", AuthProvider = AuthProvider.Local };
        var sso = new User { Id = Guid.NewGuid(), TenantId = fixture.TenantId, Username = "sso@uni.test", Email = "sso@uni.test", FirstName = "Ess", LastName = "Oh", AuthProvider = AuthProvider.AzureAd, ExternalIssuer = "https://login.microsoftonline.com/t/v2.0", ExternalId = "oid-sso" };
        fixture.Db.Users.AddRange(local, sso);
        fixture.Db.UserRoles.AddRange(
            new UserRole { Id = Guid.NewGuid(), TenantId = fixture.TenantId, UserId = local.Id, RoleId = role.Id, AssignedByUserId = local.Id },
            new UserRole { Id = Guid.NewGuid(), TenantId = fixture.TenantId, UserId = sso.Id, RoleId = role.Id, AssignedByUserId = sso.Id });
        await fixture.Db.SaveChangesAsync();

        var configuration = fixture.Configuration();
        var redis = FakeRedis.Create();
        var tokens = new JwtTokenService(configuration, redis.Object);
        var passwords = new Mock<IPasswordHashingService>();
        passwords.Setup(p => p.VerifyPassword("secret", "hash")).Returns(true);
        var bruteForce = new Mock<IBruteForceProtectionService>();
        bruteForce.Setup(b => b.IsLockedOutAsync(It.IsAny<string>())).ReturnsAsync(false);

        var auth = new AuthController(fixture.Db, passwords.Object, tokens, configuration, bruteForce.Object, redis.Object)
        { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        var localBody = Body(Assert.IsType<OkObjectResult>(await auth.Login(new AuthController.LoginRequest("local@uni.test", "secret"), CancellationToken.None)));

        var handoff = new Mock<ISignInHandoffService>();
        handoff.Setup(h => h.RedeemAsync("code-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalSignInResult(sso.Id, fixture.TenantId, IdentityProviderType.Entra, sso.ExternalIssuer!, new Dictionary<string, string>()));
        var ssoController = new SsoController(fixture.Db, tokens, configuration, redis.Object, Mock.Of<IExternalSignInService>(), handoff.Object, NullLogger<SsoController>.Instance)
        { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        var ssoBody = Body(Assert.IsType<OkObjectResult>(await ssoController.Exchange(new SsoController.ExchangeRequest("code-1"), CancellationToken.None)));

        // Same response shape.
        Assert.Equal(localBody.EnumerateObject().Select(p => p.Name).Order(), ssoBody.EnumerateObject().Select(p => p.Name).Order());
        Assert.Equal("Bearer", ssoBody.GetProperty("tokenType").GetString());

        // Same claim shape inside the access token: every claim type present for one is present for the other.
        var localClaims = new JwtSecurityTokenHandler().ReadJwtToken(localBody.GetProperty("accessToken").GetString()).Claims;
        var ssoClaims = new JwtSecurityTokenHandler().ReadJwtToken(ssoBody.GetProperty("accessToken").GetString()).Claims;
        Assert.Equal(localClaims.Select(c => c.Type).Distinct().Order(), ssoClaims.Select(c => c.Type).Distinct().Order());
        Assert.Equal(fixture.TenantId.ToString(), ssoClaims.Single(c => c.Type == "tenant_id").Value);
        Assert.Equal("Faculty", ssoClaims.Single(c => c.Type == ClaimTypes.Role).Value);
        Assert.Equal(sso.Id.ToString(), ssoClaims.Single(c => c.Type == ClaimTypes.NameIdentifier).Value);

        // A refresh token family was opened for the SSO session exactly as for the local one.
        Assert.True(redis.Store.ContainsKey(SessionTokenIssuer.RefreshTokenKeyPrefix + ssoBody.GetProperty("refreshToken").GetString()));
    }

    // ── 7. Licence lock: a brand-new person gets the licence sentence, a known person signs in ─────

    [Fact]
    public async Task LicenceLocked_FirstSignInOfNewPerson_IsRefusedWithTheLicenceSentence()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.AddProviderAsync(IdentityProviderType.Entra);
        var service = fixture.SignInService(External("https://login.microsoftonline.com/t/v2.0", "oid-new", "new@uni.test"), locked: true);
        var state = await fixture.StartAsync(service, IdentityProviderType.Entra);

        var refused = await Assert.ThrowsAsync<LicenseReadOnlyException>(() => service.CompleteAsync(state, "code", CancellationToken.None));
        Assert.Contains("the licence has expired", refused.Message);
        Assert.Contains("/admin/license", refused.Message);
        Assert.Equal(0, await fixture.Db.Users.IgnoreQueryFilters().CountAsync(u => u.ExternalId == "oid-new"));
    }

    [Fact]
    public async Task LicenceLocked_KnownPerson_StillSignsIn()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.AddProviderAsync(IdentityProviderType.Entra);
        fixture.Db.Users.Add(new User { Id = Guid.NewGuid(), TenantId = fixture.TenantId, Username = "known@uni.test", Email = "known@uni.test", AuthProvider = AuthProvider.AzureAd, ExternalIssuer = "https://login.microsoftonline.com/t/v2.0", ExternalId = "oid-known" });
        await fixture.Db.SaveChangesAsync();
        var service = fixture.SignInService(External("https://login.microsoftonline.com/t/v2.0", "oid-known", "known@uni.test"), locked: true);
        var state = await fixture.StartAsync(service, IdentityProviderType.Entra);

        var result = await service.CompleteAsync(state, "code", CancellationToken.None);
        Assert.Equal(IdentityProviderType.Entra, result.Provider);
    }

    // ── 6. Group → role mapping, per tenant, applied on every sign-in ───────────────────────────────

    [Fact]
    public async Task GroupMapping_GivesTheMappedRole_NotTheDefault_AndKeepsItInStepWithGroups()
    {
        await using var fixture = await Fixture.CreateAsync();
        var adminGroup = "3f2c0000-0000-0000-0000-000000000abc";
        await fixture.AddProviderAsync(IdentityProviderType.Entra, mapping: new() { [adminGroup] = "Admin", ["Faculty Staff"] = "Faculty" }, defaultRole: "Student");
        fixture.Db.Roles.AddRange(
            new Role { Id = Guid.NewGuid(), TenantId = fixture.TenantId, Name = "Admin" },
            new Role { Id = Guid.NewGuid(), TenantId = fixture.TenantId, Name = "Faculty" },
            new Role { Id = Guid.NewGuid(), TenantId = fixture.TenantId, Name = "Student" });
        await fixture.Db.SaveChangesAsync();

        // First sign-in: in the admin group (by object id) and the faculty group (by display name).
        var service = fixture.SignInService(External("https://login.microsoftonline.com/t/v2.0", "oid-1", "one@uni.test", groups: [adminGroup, "Faculty Staff", "Unmapped Group"]));
        var result = await service.CompleteAsync(await fixture.StartAsync(service, IdentityProviderType.Entra), "code", CancellationToken.None);
        Assert.Equal(new[] { "Admin", "Faculty" }, await RolesOf(fixture.Db, result.UserId));

        // Removed from the admin group at the directory: the next sign-in takes Admin away, keeps Faculty.
        service = fixture.SignInService(External("https://login.microsoftonline.com/t/v2.0", "oid-1", "one@uni.test", groups: ["Faculty Staff"]));
        await service.CompleteAsync(await fixture.StartAsync(service, IdentityProviderType.Entra), "code", CancellationToken.None);
        Assert.Equal(new[] { "Faculty" }, await RolesOf(fixture.Db, result.UserId));

        // In no mapped group at all: the default role, and nothing else.
        service = fixture.SignInService(External("https://login.microsoftonline.com/t/v2.0", "oid-1", "one@uni.test", groups: ["Unmapped Group"]));
        await service.CompleteAsync(await fixture.StartAsync(service, IdentityProviderType.Entra), "code", CancellationToken.None);
        Assert.Equal(new[] { "Student" }, await RolesOf(fixture.Db, result.UserId));
    }

    // ── 8/3. Login methods: Local only, SSO only, Local + SSO ───────────────────────────────────────

    [Fact]
    public async Task LoginMethods_CoverLocalOnly_SsoOnly_AndBoth()
    {
        await using var fixture = await Fixture.CreateAsync();
        var service = fixture.SignInService(External("https://accounts.google.com", "s", "s@uni.test"));

        // No configuration at all (an installation from before providers existed): local only.
        var none = await service.GetLoginMethodsAsync(fixture.TenantId);
        Assert.True(none.LocalEnabled);
        Assert.Empty(none.Providers);

        // SSO only.
        await fixture.AddProviderAsync(IdentityProviderType.Google);
        var ssoOnly = await service.GetLoginMethodsAsync(fixture.TenantId);
        Assert.False(ssoOnly.LocalEnabled);
        var google = Assert.Single(ssoOnly.Providers);
        Assert.Equal("google", google.Type);
        Assert.Equal("/api/auth/sso/google/start", google.StartPath);

        // Password login is then refused with a sentence that names the way in.
        var configuration = fixture.Configuration();
        var auth = new AuthController(fixture.Db, Mock.Of<IPasswordHashingService>(), Mock.Of<IJwtTokenService>(), configuration, Mock.Of<IBruteForceProtectionService>(), FakeRedis.Create().Object, null, service)
        { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        var refused = Assert.IsType<ObjectResult>(await auth.Login(new AuthController.LoginRequest("x@uni.test", "pw"), CancellationToken.None));
        Assert.Equal(403, refused.StatusCode);
        Assert.Contains("Google Workspace", Body(refused).GetProperty("error").GetString());

        // Local and one or more SSO providers together.
        await fixture.AddProviderAsync(IdentityProviderType.Local);
        await fixture.AddProviderAsync(IdentityProviderType.Okta);
        var both = await service.GetLoginMethodsAsync(fixture.TenantId);
        Assert.True(both.LocalEnabled);
        Assert.Equal(["google", "okta"], both.Providers.Select(p => p.Type));
    }

    // ── 3. The generic OIDC handler: discovery, JWKS, issuer, audience, nonce, hd ───────────────────

    [Fact]
    public async Task OidcClient_ValidatesAnIdToken_AgainstTheProvidersPublishedKeys()
    {
        var idp = new FakeIdp("https://idp.test/" + Guid.NewGuid().ToString("N"));
        var client = new OidcClient(idp.Clients, NullLogger<OidcClient>.Instance);
        var provider = new OidcProvider(IdentityProviderType.Okta, idp.Authority, "client-1", "secret", "https://portal.test/api/auth/sso/callback", "openid profile email groups", null, null, "groups");

        var url = await client.BuildAuthorizationUrlAsync(provider, "state-1", "nonce-1", "challenge", CancellationToken.None);
        Assert.StartsWith(idp.Authority + "/authorize?", url);
        Assert.Contains("code_challenge_method=S256", url);
        Assert.Contains("redirect_uri=https%3A%2F%2Fportal.test%2Fapi%2Fauth%2Fsso%2Fcallback", url);

        var idToken = idp.IdToken(new Dictionary<string, object> { ["sub"] = "00u1", ["email"] = "p@uni.test", ["given_name"] = "Pat", ["family_name"] = "Lee", ["nonce"] = "nonce-1", ["groups"] = new[] { "Everyone", "Faculty" } }, audience: "client-1");
        var identity = await client.ValidateAsync(provider, new OidcTokens(idToken, null), "nonce-1", CancellationToken.None);
        Assert.Equal(idp.Authority, identity.Issuer);
        Assert.Equal("00u1", identity.Subject);
        Assert.Equal("p@uni.test", identity.Email);
        Assert.Equal(("Pat", "Lee"), (identity.FirstName, identity.LastName));
        Assert.Equal(["Everyone", "Faculty"], identity.Groups);

        // The code exchange posts the secret from the API, never from the browser.
        var tokens = await client.ExchangeCodeAsync(provider, "auth-code", "verifier", CancellationToken.None);
        Assert.Equal("the-id-token", tokens.IdToken);
        Assert.Contains("client_secret=secret", idp.LastTokenRequestBody);
        Assert.Contains("code_verifier=verifier", idp.LastTokenRequestBody);
        Assert.Contains("grant_type=authorization_code", idp.LastTokenRequestBody);
    }

    [Fact]
    public async Task OidcClient_RejectsWrongIssuer_WrongAudience_WrongNonce_AndForeignKey()
    {
        var idp = new FakeIdp("https://idp.test/" + Guid.NewGuid().ToString("N"));
        var client = new OidcClient(idp.Clients, NullLogger<OidcClient>.Instance);
        var provider = new OidcProvider(IdentityProviderType.Okta, idp.Authority, "client-1", "secret", "https://portal.test/cb", "openid", null, null, "groups");
        var claims = new Dictionary<string, object> { ["sub"] = "1", ["email"] = "p@uni.test", ["nonce"] = "n" };

        var wrongIssuer = await Assert.ThrowsAsync<ExternalSignInException>(() => client.ValidateAsync(provider, new OidcTokens(idp.IdToken(claims, "client-1", issuer: "https://other.test"), null), "n", CancellationToken.None));
        Assert.Contains("different directory", wrongIssuer.Message);
        var wrongAudience = await Assert.ThrowsAsync<ExternalSignInException>(() => client.ValidateAsync(provider, new OidcTokens(idp.IdToken(claims, "someone-else"), null), "n", CancellationToken.None));
        Assert.Contains("client id mismatch", wrongAudience.Message);
        var wrongNonce = await Assert.ThrowsAsync<ExternalSignInException>(() => client.ValidateAsync(provider, new OidcTokens(idp.IdToken(claims, "client-1"), null), "other-nonce", CancellationToken.None));
        Assert.Equal("nonce_mismatch", wrongNonce.Code);
        var foreignKey = await Assert.ThrowsAsync<ExternalSignInException>(() => client.ValidateAsync(provider, new OidcTokens(idp.IdToken(claims, "client-1", signWith: RSA.Create(2048)), null), "n", CancellationToken.None));
        Assert.Equal("id_token_invalid", foreignKey.Code);
    }

    [Fact]
    public async Task OidcClient_Google_RequiresTheWorkspaceDomain_AndEntraPrefersOid()
    {
        var idp = new FakeIdp("https://idp.test/" + Guid.NewGuid().ToString("N"));
        var client = new OidcClient(idp.Clients, NullLogger<OidcClient>.Instance);
        var google = new OidcProvider(IdentityProviderType.Google, idp.Authority, "client-1", "secret", "https://portal.test/cb", "openid email", null, "uni.test", "groups");

        var consumer = await Assert.ThrowsAsync<ExternalSignInException>(() => client.ValidateAsync(google,
            new OidcTokens(idp.IdToken(new Dictionary<string, object> { ["sub"] = "g1", ["email"] = "someone@gmail.com", ["nonce"] = "n" }, "client-1"), null), "n", CancellationToken.None));
        Assert.Equal("wrong_domain", consumer.Code);
        var workspace = await client.ValidateAsync(google,
            new OidcTokens(idp.IdToken(new Dictionary<string, object> { ["sub"] = "g2", ["email"] = "staff@uni.test", ["hd"] = "uni.test", ["nonce"] = "n" }, "client-1"), null), "n", CancellationToken.None);
        Assert.Equal("g2", workspace.Subject);

        var entraTenant = Guid.NewGuid();
        var entra = new OidcProvider(IdentityProviderType.Entra, idp.Authority, "client-1", "secret", "https://portal.test/cb", "openid", entraTenant.ToString(), null, "groups");
        var fromEntra = await client.ValidateAsync(entra,
            new OidcTokens(idp.IdToken(new Dictionary<string, object> { ["sub"] = "pairwise-sub", ["oid"] = "directory-oid", ["tid"] = entraTenant.ToString(), ["preferred_username"] = "e@uni.test", ["nonce"] = "n", ["roles"] = new[] { "Registrar" } }, "client-1"), null), "n", CancellationToken.None);
        Assert.Equal("directory-oid", fromEntra.Subject);
        Assert.Equal("e@uni.test", fromEntra.Email);
        Assert.Contains("Registrar", fromEntra.Groups);
        var otherDirectory = await Assert.ThrowsAsync<ExternalSignInException>(() => client.ValidateAsync(entra,
            new OidcTokens(idp.IdToken(new Dictionary<string, object> { ["sub"] = "s", ["oid"] = "o", ["tid"] = Guid.NewGuid().ToString(), ["email"] = "e@uni.test", ["nonce"] = "n" }, "client-1"), null), "n", CancellationToken.None));
        Assert.Equal("wrong_directory", otherDirectory.Code);
    }

    // ── 1/2. Wizard: the tested list becomes provider rows at go-live ───────────────────────────────

    [Fact]
    public async Task WizardGoLive_TurnsStep3IntoProviderRows_WithSecretsLeftInTheVault()
    {
        await using var fixture = await Fixture.CreateAsync();
        var step3 = new
        {
            providers = new object[]
            {
                new { provider = "local", connectionTested = true, hasSecret = false },
                new { provider = "entra", tenantId = "11111111-2222-3333-4444-555555555555", clientId = "app-id", hasSecret = true, connectionTested = true,
                      clientSecretName = "/tenants/" + fixture.TenantId.ToString("D") + "/identity/entra/client-secret",
                      groupRoleMapping = new Dictionary<string, string> { ["grp-1"] = "Admin" }, defaultRole = "Student" },
                new { provider = "okta", authority = "https://dev-1.okta.com/oauth2/default/", clientId = "okta-id", hasSecret = true, connectionTested = true, defaultRole = "Faculty" }
            }
        };
        var stepData = JsonSerializer.Serialize(new
        {
            step1 = new { name = "Test University", shortName = "tu", timezone = "Asia/Karachi", contactEmail = "c@tu.test" },
            step3,
            step4 = new { roles = new[] { new { name = "Admin", permissions = new[] { "System.Write" } } } }
        });
        var configuration = fixture.Configuration();
        var provisioning = new WizardProvisioningService(fixture.Db, Mock.Of<AegisCore.Infrastructure.Security.Pqc.IPqcSignatureService>(), Mock.Of<AegisCore.Application.Finance.IAccountingStandardsEngine>(), configuration);

        await provisioning.ProvisionAsync(fixture.TenantId, stepData, CancellationToken.None);

        var rows = await fixture.Db.IdentityProviderConfigurations.IgnoreQueryFilters().Where(r => r.TenantId == fixture.TenantId).OrderBy(r => r.SortOrder).ToListAsync();
        Assert.Equal([IdentityProviderType.Local, IdentityProviderType.Entra, IdentityProviderType.Okta], rows.Select(r => r.ProviderType));
        var entra = rows[1];
        Assert.Equal("https://login.microsoftonline.com/11111111-2222-3333-4444-555555555555/v2.0", entra.Authority);
        Assert.Equal("11111111-2222-3333-4444-555555555555", entra.ExternalTenantId);
        Assert.Equal("/tenants/" + fixture.TenantId.ToString("D") + "/identity/entra/client-secret", entra.ClientSecretName);
        Assert.Equal("https://portal.test/api/auth/sso/callback", entra.RedirectUri);
        Assert.Equal("Student", entra.DefaultRole);
        Assert.Equal("Admin", JsonDocument.Parse(entra.GroupRoleMappingJson).RootElement.GetProperty("grp-1").GetString());
        var okta = rows[2];
        Assert.Equal("https://dev-1.okta.com/oauth2/default", okta.Authority);
        Assert.Equal("openid profile email groups", okta.Scopes);
        Assert.All(rows, row => Assert.DoesNotContain("secret-value", row.ClientSecretName ?? string.Empty));

        // The login screen now offers exactly these.
        var service = fixture.SignInService(External("https://x", "s", "s@uni.test"));
        var methods = await service.GetLoginMethodsAsync(fixture.TenantId);
        Assert.True(methods.LocalEnabled);
        Assert.Equal(["entra", "okta"], methods.Providers.Select(p => p.Type));
    }

    [Fact]
    public async Task WizardHandshake_FailsClearly_WhenDiscoveryIsUnreachable_AndPassesAgainstARealDocument()
    {
        var idp = new FakeIdp("https://idp.test/" + Guid.NewGuid().ToString("N"));
        var oidc = new OidcClient(idp.Clients, NullLogger<OidcClient>.Instance);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["APP_URL"] = "https://portal.test" }).Build();

        var okta = new OktaSsoProvider(oidc, configuration);
        await okta.ConfigureAsync(new SsoConfigDto("okta", ClientId: "c", ClientSecret: "s", Authority: idp.Authority));
        var pass = await okta.TestHandshakeAsync();
        Assert.True(pass.Success, pass.Error);
        Assert.Equal(idp.Authority + "/authorize", pass.LoginUrl);
        Assert.Contains("https://portal.test/api/auth/sso/callback", pass.FixHint);

        await okta.ConfigureAsync(new SsoConfigDto("okta", ClientId: "c", ClientSecret: "s", Authority: "https://idp.test/nowhere"));
        var fail = await okta.TestHandshakeAsync();
        Assert.False(fail.Success);
        Assert.Contains("could not be reached", fail.Error);

        var entra = new EntraSsoProvider(oidc, configuration);
        await entra.ConfigureAsync(new SsoConfigDto("entra", TenantId: "t", ClientId: "c"));
        var noSecret = await entra.TestHandshakeAsync();
        Assert.False(noSecret.Success);
        Assert.Contains("Client secret is required", noSecret.Error);
    }

    // ── helpers ────────────────────────────────────────────────────────────────────────────────────

    private static ExternalIdentity External(string issuer, string subject, string email, IReadOnlyList<string>? groups = null)
        => new(issuer, subject, email, "First", "Last", new Dictionary<string, string> { ["iss"] = issuer, ["sub"] = subject }, groups ?? []);

    private static JsonElement Body(ObjectResult result)
        => JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(result.Value));

    private static async Task<string[]> RolesOf(AegisDbContext db, Guid userId)
        => (await db.UserRoles.IgnoreQueryFilters().Include(ur => ur.Role).Where(ur => ur.UserId == userId).Select(ur => ur.Role.Name).ToListAsync()).Order().ToArray();

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        public AegisDbContext Db { get; }
        public Guid TenantId { get; } = Guid.NewGuid();
        public TestSecretService Secrets { get; } = new();
        public HttpContextAccessor Http { get; }

        private Fixture(SqliteConnection connection, AegisDbContext db, HttpContextAccessor http) { _connection = connection; Db = db; Http = http; }

        public static async Task<Fixture> CreateAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();
            var http = new HttpContextAccessor();
            var db = new AegisDbContext(new DbContextOptionsBuilder<AegisDbContext>().UseSqlite(connection).Options, http);
            await db.Database.EnsureCreatedAsync();
            return new Fixture(connection, db, http);
        }

        public IConfiguration Configuration() => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["TENANT_ID"] = TenantId.ToString("D"),
            ["APP_URL"] = "https://portal.test",
            ["ASPNETCORE_ENVIRONMENT"] = "Development",
            ["AEGISCORE_JWT_SECRET"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))
        }).Build();

        public IIdentityService IdentityService()
            => new IdentityService(new UserRepository(Db), new RoleRepository(Db), new PermissionRepository(Db), new UserRoleRepository(Db), NullLogger<IdentityService>.Instance);

        public async Task AddProviderAsync(IdentityProviderType type, Dictionary<string, string>? mapping = null, string? defaultRole = null)
        {
            var secretName = ExternalSignInService.SecretName(TenantId, type);
            await Secrets.SetSecretAsync(secretName, "client-secret-value");
            Db.IdentityProviderConfigurations.Add(new IdentityProviderConfiguration
            {
                Id = Guid.NewGuid(), TenantId = TenantId, ProviderType = type, DisplayName = ExternalSignInService.Display(type), IsEnabled = true,
                ClientId = "client-1", ClientSecretName = secretName, Authority = "https://idp.test/" + type.ToString().ToLowerInvariant(),
                RedirectUri = "https://portal.test/api/auth/sso/callback", SortOrder = (int)type,
                GroupRoleMappingJson = JsonSerializer.Serialize(mapping ?? new()), DefaultRole = defaultRole
            });
            await Db.SaveChangesAsync();
        }

        /// <summary>A sign-in service whose provider round-trip is faked to return one fixed identity.</summary>
        public ExternalSignInService SignInService(ExternalIdentity identity, bool locked = false)
        {
            var oidc = new Mock<IOidcClient>();
            oidc.Setup(o => o.BuildAuthorizationUrlAsync(It.IsAny<OidcProvider>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((OidcProvider p, string state, string nonce, string challenge, CancellationToken _) => $"{p.Authority}/authorize?state={state}&nonce={nonce}&code_challenge={challenge}");
            oidc.Setup(o => o.ExchangeCodeAsync(It.IsAny<OidcProvider>(), "code", It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new OidcTokens("id", "access"));
            oidc.Setup(o => o.ValidateAsync(It.IsAny<OidcProvider>(), It.IsAny<OidcTokens>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(identity);
            var license = new Mock<ILicenseService>();
            license.Setup(l => l.GetStatusAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new LicenseRuntimeStatus(
                locked ? "expired" : "active", locked, locked ? "expired" : null, locked ? DateTimeOffset.UtcNow : null, null, 0, null));
            return new ExternalSignInService(Db, Secrets, oidc.Object, FakeRedis.Create().Object, IdentityService(), new UserRepository(Db),
                Configuration(), NullLogger<ExternalSignInService>.Instance, license.Object);
        }

        public async Task<string> StartAsync(ExternalSignInService service, IdentityProviderType type)
        {
            var start = await service.StartAsync(TenantId, type, null, CancellationToken.None);
            var query = System.Web.HttpUtility.ParseQueryString(new Uri(start.AuthorizationUrl).Query);
            return query["state"]!;
        }

        public async ValueTask DisposeAsync() { await Db.DisposeAsync(); await _connection.DisposeAsync(); }
    }

    /// <summary>Just enough of Redis for state, handoff and refresh-token families: string set/get/get-delete/delete.</summary>
    private sealed class FakeRedis
    {
        public Dictionary<RedisKey, RedisValue> Store { get; } = new();
        public Mock<IConnectionMultiplexer> Mock { get; } = new();

        public static FakeRedis Create()
        {
            var fake = new FakeRedis();
            var db = new Mock<IDatabase>();
            db.Setup(x => x.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
                .Callback<RedisKey, RedisValue, TimeSpan?, bool, When, CommandFlags>((k, v, _, _, _, _) => fake.Store[k] = v).ReturnsAsync(true);
            db.Setup(x => x.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
                .Callback<RedisKey, RedisValue, TimeSpan?, When, CommandFlags>((k, v, _, _, _) => fake.Store[k] = v).ReturnsAsync(true);
            db.Setup(x => x.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<When>()))
                .Callback<RedisKey, RedisValue, TimeSpan?, When>((k, v, _, _) => fake.Store[k] = v).ReturnsAsync(true);
            db.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync((RedisKey k, CommandFlags _) => fake.Store.TryGetValue(k, out var v) ? v : RedisValue.Null);
            db.Setup(x => x.StringGetDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync((RedisKey k, CommandFlags _) => fake.Store.Remove(k, out var v) ? v : RedisValue.Null);
            db.Setup(x => x.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync((RedisKey k, CommandFlags _) => fake.Store.Remove(k));
            fake.Mock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object?>())).Returns(db.Object);
            return fake;
        }

        public IConnectionMultiplexer Object => Mock.Object;
    }

    /// <summary>An OpenID provider in a message handler: discovery, JWKS, a token endpoint, and ID tokens signed with its key.</summary>
    private sealed class FakeIdp
    {
        private readonly RSA _rsa = RSA.Create(2048);
        private readonly string _keyId = Guid.NewGuid().ToString("N");
        public string Authority { get; }
        public string? LastTokenRequestBody { get; private set; }
        public IHttpClientFactory Clients { get; }

        public FakeIdp(string authority)
        {
            Authority = authority;
            Clients = new Factory(new HttpClient(new Handler(this)));
        }

        public string IdToken(Dictionary<string, object> claims, string audience, string? issuer = null, RSA? signWith = null)
        {
            var key = new RsaSecurityKey(signWith ?? _rsa) { KeyId = _keyId };
            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = issuer ?? Authority,
                Audience = audience,
                Expires = DateTime.UtcNow.AddMinutes(5),
                IssuedAt = DateTime.UtcNow,
                Claims = claims,
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256)
            };
            return new JsonWebTokenHandler().CreateToken(descriptor);
        }

        private string Discovery() => JsonSerializer.Serialize(new
        {
            issuer = Authority,
            authorization_endpoint = Authority + "/authorize",
            token_endpoint = Authority + "/token",
            jwks_uri = Authority + "/keys",
            response_types_supported = new[] { "code" },
            subject_types_supported = new[] { "public" },
            id_token_signing_alg_values_supported = new[] { "RS256" }
        });

        private string Jwks()
        {
            var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(new RsaSecurityKey(_rsa.ExportParameters(false)) { KeyId = _keyId });
            return JsonSerializer.Serialize(new { keys = new[] { new { kty = jwk.Kty, use = "sig", kid = jwk.Kid, n = jwk.N, e = jwk.E, alg = "RS256" } } });
        }

        private sealed class Factory(HttpClient client) : IHttpClientFactory { public HttpClient CreateClient(string name) => client; }

        private sealed class Handler(FakeIdp idp) : HttpMessageHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var url = request.RequestUri!.ToString();
                if (url == idp.Authority + "/.well-known/openid-configuration") return Json(idp.Discovery());
                if (url == idp.Authority + "/keys") return Json(idp.Jwks());
                if (url == idp.Authority + "/token")
                {
                    idp.LastTokenRequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
                    return Json("""{"id_token":"the-id-token","access_token":"the-access-token","token_type":"Bearer"}""");
                }
                return new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("not found") };
            }

            private static HttpResponseMessage Json(string body)
                => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }
}
