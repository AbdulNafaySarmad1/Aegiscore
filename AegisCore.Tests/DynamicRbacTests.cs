using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using AegisCore.API.Authorization;
using AegisCore.API.Controllers;
using AegisCore.Application.Onboarding;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Onboarding;
using AegisCore.Infrastructure.Security;
using Casbin;
using Casbin.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using StackExchange.Redis;
using DomainRole = AegisCore.Domain.Entities.Role;

namespace AegisCore.Tests;

public sealed class DynamicRbacTests
{
    private const string SetupToken = "rbac-test-bootstrap-token-with-at-least-32-bytes";

    [Fact]
    public async Task Wizard_persists_arbitrary_roles_permissions_and_matrix_by_tenant()
    {
        await using var fixture = await RbacFixture.CreateAsync();
        var service = new WizardRolePermissionService(fixture.Db, fixture.State);

        var roles = await service.SaveRolesAsync(SetupToken, new SaveWizardRolesRequest(["Custom Registrar", "Learner"]));
        await service.SavePermissionsAsync(SetupToken, new SaveWizardPermissionDefinitionsRequest([
            new("Students", "Read"), new("Students", "Write")
        ]));
        var matrix = await service.SaveMatrixAsync(SetupToken, new SaveWizardPermissionMatrixRequest([
            new("Custom Registrar", "Students", "Read", true),
            new("Custom Registrar", "Students", "Write", true),
            new("Learner", "Students", "Read", true),
            new("Learner", "Students", "Write", false)
        ]));

        Assert.Equal(2, roles.Count);
        Assert.Equal(3, matrix.Count);
        Assert.All(roles, role => Assert.Equal(fixture.TenantId, role.TenantId));
        Assert.All(matrix, grant => Assert.Equal(fixture.TenantId, grant.TenantId));

        await service.SaveRolesAsync(SetupToken, new SaveWizardRolesRequest(["Custom Registrar"]));
        Assert.DoesNotContain(await fixture.Db.Roles.IgnoreQueryFilters().ToListAsync(), role => role.Name == "Learner");
    }

    [Theory]
    [InlineData("")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public async Task Wizard_rejects_invalid_role_names(string roleName)
    {
        await using var fixture = await RbacFixture.CreateAsync();
        var service = new WizardRolePermissionService(fixture.Db, fixture.State);
        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveRolesAsync(SetupToken, new SaveWizardRolesRequest([roleName])));
    }

    [Fact]
    public async Task Casbin_syncs_tenant_grants_and_invalidates_cached_policy()
    {
        await using var fixture = await RbacFixture.CreateAsync();
        var role = new DomainRole { Id = Guid.NewGuid(), TenantId = fixture.TenantId, Name = "Budget Reviewer" };
        var permission = new Permission
        {
            Id = Guid.NewGuid(), TenantId = fixture.TenantId, Name = "Finance.Approve",
            Module = "Finance", Resource = "Finance", Action = "Approve"
        };
        var grant = new RolePermission
        {
            Id = Guid.NewGuid(), TenantId = fixture.TenantId, RoleId = role.Id, PermissionId = permission.Id,
            Role = role, Permission = permission
        };
        fixture.Db.AddRange(role, permission, grant);
        await fixture.Db.SaveChangesAsync();

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new CasbinPolicySyncService(fixture.Db, CreateEnforcer(), cache);
        Assert.True(await service.EnforceAsync(fixture.TenantId, ["Budget Reviewer"], "Finance", "Approve"));
        Assert.False(await service.EnforceAsync(fixture.TenantId, ["Budget Reviewer"], "Finance", "Delete"));

        fixture.Db.RolePermissions.Remove(grant);
        await fixture.Db.SaveChangesAsync();
        Assert.True(await service.EnforceAsync(fixture.TenantId, ["Budget Reviewer"], "Finance", "Approve"));
        service.Invalidate(fixture.TenantId);
        Assert.False(await service.EnforceAsync(fixture.TenantId, ["Budget Reviewer"], "Finance", "Approve"));
    }

    [Fact]
    public async Task Permission_handler_uses_JWT_roles_tenant_and_Casbin()
    {
        await using var fixture = await RbacFixture.CreateAsync();
        var role = new DomainRole { Id = Guid.NewGuid(), TenantId = fixture.TenantId, Name = "Grade Publisher" };
        var permission = new Permission { Id = Guid.NewGuid(), TenantId = fixture.TenantId, Name = "Grades.Approve", Module = "Grades", Resource = "Grades", Action = "Approve" };
        fixture.Db.AddRange(role, permission, new RolePermission
        {
            Id = Guid.NewGuid(), TenantId = fixture.TenantId, RoleId = role.Id, PermissionId = permission.Id,
            Role = role, Permission = permission
        });
        await fixture.Db.SaveChangesAsync();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sync = new CasbinPolicySyncService(fixture.Db, CreateEnforcer(), cache);
        var requirement = new PermissionRequirement("Grades", "Approve");
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim("tenant_id", fixture.TenantId.ToString()),
            new Claim(ClaimTypes.Role, "Grade Publisher")
        ], "test"));
        var context = new AuthorizationHandlerContext([requirement], principal, null);

        await new PermissionAuthorizationHandler(fixture.Db, sync).HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public void All_97_role_literals_are_replaced_with_dynamic_permissions()
    {
        var controllerTypes = typeof(AuthController).Assembly.GetTypes().Where(type => typeof(Microsoft.AspNetCore.Mvc.ControllerBase).IsAssignableFrom(type)).ToArray();
        var permissionAttributes = controllerTypes.Sum(type =>
            type.GetCustomAttributes<RequirePermissionAttribute>(true).Count() +
            type.GetMethods(BindingFlags.Instance | BindingFlags.Public).Sum(method => method.GetCustomAttributes<RequirePermissionAttribute>(true).Count()));
        var legacyRoleAttributes = controllerTypes.SelectMany(type =>
                type.GetCustomAttributes<AuthorizeAttribute>(true)
                    .Concat(type.GetMethods(BindingFlags.Instance | BindingFlags.Public).SelectMany(method => method.GetCustomAttributes<AuthorizeAttribute>(true))))
            .Count(attribute => !string.IsNullOrWhiteSpace(attribute.Roles));

        Assert.Equal(97, permissionAttributes);
        Assert.Equal(0, legacyRoleAttributes);
    }

    [Fact]
    public async Task Permission_denial_returns_the_resource_and_action_message()
    {
        var requirement = new PermissionRequirement("Finance", "Approve");
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var policy = new AuthorizationPolicyBuilder().AddRequirements(requirement).Build();
        var result = PolicyAuthorizationResult.Forbid(AuthorizationFailure.Failed([requirement]));

        await new PermissionAuthorizationResultHandler().HandleAsync(_ => Task.CompletedTask, context, policy, result);

        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.Contains("You do not have Approve permission on Finance.", body, StringComparison.Ordinal);
    }

    [Fact]
    public void Jwt_access_token_uses_SESSION_LIFETIME()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AEGISCORE_JWT_SECRET"] = new string('s', 64),
            ["SESSION_LIFETIME"] = "1234"
        }).Build();
        var service = new JwtTokenService(configuration, Mock.Of<IConnectionMultiplexer>());
        var before = DateTime.UtcNow;
        var token = new JwtSecurityTokenHandler().ReadJwtToken(service.GenerateAccessToken(new User
        {
            Id = Guid.NewGuid(), TenantId = Guid.NewGuid(), Email = "rbac@example.test", Username = "rbac"
        }));

        Assert.InRange((token.ValidTo - before).TotalSeconds, 1230, 1238);
    }

    private static IEnforcer CreateEnforcer()
    {
        var model = DefaultModel.Create();
        model.LoadModelFromText("""
            [request_definition]
            r = sub, obj, act

            [policy_definition]
            p = sub, obj, act

            [policy_effect]
            e = some(where (p.eft == allow))

            [matchers]
            m = r.sub == p.sub && r.obj == p.obj && r.act == p.act
            """);
        return new Enforcer(model);
    }

    private sealed class RbacFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        public AegisDbContext Db { get; }
        public WizardStateService State { get; }
        public Guid TenantId { get; private set; }

        private RbacFixture(SqliteConnection connection, AegisDbContext db)
        {
            _connection = connection;
            Db = db;
            State = WizardTestState.Create(db);
        }

        public static async Task<RbacFixture> CreateAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();
            var db = new AegisDbContext(new DbContextOptionsBuilder<AegisDbContext>().UseSqlite(connection).Options);
            await db.Database.EnsureCreatedAsync();
            var fixture = new RbacFixture(connection, db);
            await fixture.State.GetOrCreateStateAsync(SetupToken);
            var state = await fixture.State.InitializeTenantAtStepOneAsync(SetupToken);
            fixture.TenantId = state.TenantId;
            return fixture;
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
