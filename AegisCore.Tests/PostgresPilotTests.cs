using AegisCore.Domain.Entities;
using AegisCore.API.Controllers;
using AegisCore.Infrastructure.StudentLifecycle;
using Microsoft.AspNetCore.Mvc;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Database;
using AegisCore.Infrastructure.Deployment.Seeders;
using AegisCore.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Moq;
using StackExchange.Redis;
using AegisCore.Application.Workflow;

namespace AegisCore.Tests;

public sealed class PilotPostgresFactAttribute : FactAttribute
{
    public PilotPostgresFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PILOT_POSTGRES_CONNECTION")))
            Skip = "Requires the dedicated local aegiscore_pilot PostgreSQL database.";
    }
}

public class PostgresPilotTests
{
    [PilotPostgresFact]
    public async Task Migrations_Seed_Repetition_Passwords_And_Tenant_Isolation()
    {
        var connection = Environment.GetEnvironmentVariable("PILOT_POSTGRES_CONNECTION")!;
        var parsed = new NpgsqlConnectionStringBuilder(connection);
        Assert.StartsWith("aegiscore_", parsed.Database, StringComparison.Ordinal);
        Assert.Contains(parsed.Host, new[] { "127.0.0.1", "localhost" });
        var tenant = Guid.Parse("eba35470-ad75-4cac-bb60-c8d4886ebfd7");
        var http = new DefaultHttpContext(); http.Items["TenantId"] = tenant;
        var adapter = new PostgresDialectAdapter();
        var options = new DbContextOptionsBuilder<AegisDbContext>();
        adapter.ConfigureOptions(options, connection);
        await using var db = new AegisDbContext(options.Options, new HttpContextAccessor { HttpContext = http }, adapter);
        await db.Database.MigrateAsync();
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.Contains("20260904120000_PersistWizardState", await db.Database.GetAppliedMigrationsAsync());
        // Query the real mapped columns: migration history alone cannot detect an
        // undiscoverable hand-written migration.
        await db.WizardSessions.IgnoreQueryFilters().Select(s => new { s.BootstrapTokenHash, s.Payload }).ToListAsync();
        var scenarioRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../demo-scenarios"));
        var scenario = DemoScenario.Load(scenarioRoot, "pakistan-bscs-v1");
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["DemoSeed:Enabled"] = "true", ["DemoSeed:TenantId"] = tenant.ToString(),
            ["DemoSeed:Password"] = "Fictional-Pilot-Test-Password!",
            ["DemoSeed:ScenarioRoot"] = scenarioRoot, ["DemoSeed:Scenario"] = "pakistan-bscs-v1"
        }).Build();
        var hasher = new PasswordHashingService();
        var seeder = new DemoSeeder(db, NullLogger<DemoSeeder>.Instance, hasher, config);
        await seeder.SeedAsync(tenant);
        var count = await db.Users.CountAsync();
        await seeder.SeedAsync(tenant);
        Assert.Equal(count, await db.Users.CountAsync());
        var student = await db.Users.SingleAsync(u => u.Id == DemoSeeder.RecordId(tenant, scenario.Version, scenario.Student.AccountKey));
        Assert.True(hasher.VerifyPassword("Fictional-Pilot-Test-Password!", student.PasswordHash!));
        Assert.True(await db.Students.AnyAsync(s => s.Id == student.Id));
        Assert.True(await db.UserRoles.AnyAsync(r => r.UserId == student.Id));
        Assert.True(await db.StudentFeeDues.AnyAsync(d => d.Id == DemoSeeder.RecordId(tenant, scenario.Version, $"due:{scenario.Student.AccountKey}")));
        Assert.True(await db.ApplicantProfiles.AnyAsync(a => a.Id == DemoSeeder.RecordId(tenant, scenario.Version, scenario.Applicant.Key)));
        var lifecycle = new StudentLifecycleController(db, new StudentLifecycleService());
        Assert.IsType<ConflictObjectResult>(await lifecycle.UpsertAlumni(new AlumniProfile { UserId = student.Id, GraduationYear = 2030 }, default));
        Assert.Empty(await db.AlumniProfiles.ToListAsync());
        var graduation = new GraduationService(db);
        var progress = await graduation.EvaluateAsync(student.Id);
        Assert.False(progress.Approved);
        Assert.Equal(3, progress.EarnedCredits);
        Assert.Equal(3.0m, progress.Cgpa);
        Assert.Contains(progress.Blockers, b => b.Contains("fees"));
        var approver = DemoSeeder.RecordId(tenant, scenario.Version, "account.administrator");
        await Assert.ThrowsAsync<InvalidOperationException>(() => graduation.ClearAsync(student.Id, "Finance", approver));
        await Assert.ThrowsAsync<InvalidOperationException>(() => graduation.ApproveAsync(student.Id, approver));
        Assert.Empty(await db.AlumniProfiles.ToListAsync());
        var activationUser = new User { Id = Guid.NewGuid(), TenantId = tenant, Email = $"activation-{Guid.NewGuid():N}@example.invalid" };
        db.Users.Add(activationUser);
        await db.SaveChangesAsync();
        var redisDb = new Mock<StackExchange.Redis.IDatabase>();
        redisDb.Setup(c => c.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)activationUser.Id.ToString());
        var redis = new Mock<IConnectionMultiplexer>();
        redis.Setup(c => c.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(redisDb.Object);
        var activationConfig = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["TENANT_ID"] = tenant.ToString() }).Build();
        var activation = new AccountActivationController(db, redis.Object, hasher, Mock.Of<INotificationService>(), activationConfig);
        var request = new AccountActivationController.ActivationComplete(new string('A', 64), "Unique-Activation-Password!");
        Assert.IsType<OkObjectResult>(await activation.Complete(request, default));
        // Even replay from a cache that has not deleted the key cannot reset a password.
        Assert.IsType<ConflictObjectResult>(await activation.Complete(request, default));
        await db.Entry(activationUser).ReloadAsync();
        Assert.True(hasher.VerifyPassword(request.Password, activationUser.PasswordHash!));
        db.Users.Remove(activationUser);
        await db.SaveChangesAsync();
        http.Items["TenantId"] = Guid.NewGuid();
        Assert.Empty(await db.Users.ToListAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => seeder.SeedAsync(tenant));
    }
}
