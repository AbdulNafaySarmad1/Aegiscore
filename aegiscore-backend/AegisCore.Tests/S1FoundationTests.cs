using System.Security.Claims;
using AegisCore.API.Authorization;
using AegisCore.Application.Foundation;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Foundation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AegisCore.Tests;

public sealed class S1FoundationTests
{
    [Fact]
    public async Task PolicyService_Reads_Updates_And_Audits()
    {
        await using var fixture = await DbFixture.CreateAsync();
        var audit = new AuditService(fixture.Db);
        var service = new PolicyService(fixture.Db, audit, NullLogger<PolicyService>.Instance);
        var actor = Guid.NewGuid();

        fixture.Db.PolicyConfigs.Add(new PolicyConfig
        {
            Module = "academic",
            Key = "add_drop_window_days",
            Value = "7",
            ValueType = "int",
            CreatedBy = actor
        });
        await fixture.Db.SaveChangesAsync();

        Assert.Equal(7, await service.GetAsync("academic", "add_drop_window_days", 0));

        await service.SetAsync("academic", "add_drop_window_days", 10, "extended window", actor);

        Assert.Equal(10, await service.GetAsync("academic", "add_drop_window_days", 0));
        Assert.Contains(await fixture.Db.AuditLogs.ToListAsync(), l => l.Action == "policy.updated");
    }

    [Fact]
    public async Task AcademicCalendarService_Counts_WorkingDays_And_Excludes_Holidays()
    {
        await using var fixture = await DbFixture.CreateAsync();
        var yearId = Guid.NewGuid();
        var semesterId = Guid.NewGuid();
        fixture.Db.AcademicYears.Add(new AcademicYear
        {
            Id = yearId,
            Name = "2026-27",
            StartDate = new DateOnly(2026, 8, 1),
            EndDate = new DateOnly(2027, 7, 31),
            IsCurrent = true
        });
        fixture.Db.Semesters.Add(new Semester
        {
            Id = semesterId,
            AcademicYearId = yearId,
            Name = "Fall 2026",
            Type = "fall",
            StartDate = new DateOnly(2026, 8, 17),
            EndDate = new DateOnly(2026, 12, 31),
            AddDropEndDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1),
            IsCurrent = true,
            Status = "active"
        });
        fixture.Db.AcademicHolidays.Add(new AcademicHoliday
        {
            AcademicYearId = yearId,
            SemesterId = semesterId,
            Name = "University Holiday",
            StartDate = new DateOnly(2026, 8, 19),
            EndDate = new DateOnly(2026, 8, 19),
            Type = "university_holiday"
        });
        await fixture.Db.SaveChangesAsync();

        var service = new AcademicCalendarService(fixture.Db);

        Assert.Equal("Fall 2026", (await service.GetCurrentSemesterAsync()).Name);
        Assert.True(await service.IsAddDropOpenAsync());
        Assert.Equal(4, await service.GetWorkingDaysAsync(new DateOnly(2026, 8, 17), new DateOnly(2026, 8, 21)));
    }

    [Fact]
    public async Task PermissionAuthorizationHandler_Allows_Granted_RolePermission()
    {
        await using var fixture = await DbFixture.CreateAsync();
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        fixture.Db.Users.Add(new User { Id = userId, Username = "hod", Email = "hod@example.test" });
        fixture.Db.Roles.Add(new Role { Id = roleId, Name = "HOD", Description = "Head of department" });
        fixture.Db.Permissions.Add(new Permission
        {
            Id = permissionId,
            Name = "admissions.edit.merit_list",
            Module = "admissions",
            Action = "edit",
            Resource = "merit_list"
        });
        fixture.Db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permissionId });
        fixture.Db.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId, AssignedByUserId = Guid.NewGuid() });
        await fixture.Db.SaveChangesAsync();

        var requirement = new PermissionRequirement("admissions", "edit", "merit_list");
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
            "test"));
        var context = new AuthorizationHandlerContext([requirement], principal, null);

        await new PermissionAuthorizationHandler(fixture.Db).HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public void BaselineMigration_Enforces_AuditLog_Immutability()
    {
        var sql = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "AegisCore.Infrastructure",
            "Migrations",
            "V1.0_AegisCoreBaseline.sql"));

        Assert.Contains("ALTER TABLE \"AuditLogs\" ENABLE ROW LEVEL SECURITY", sql);
        Assert.Contains("CREATE POLICY audit_log_no_delete", sql);
        Assert.Contains("CREATE POLICY audit_log_no_update", sql);
    }

    private sealed class DbFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private DbFixture(SqliteConnection connection, AegisDbContext db)
        {
            _connection = connection;
            Db = db;
        }

        public AegisDbContext Db { get; }

        public static async Task<DbFixture> CreateAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AegisDbContext>()
                .UseSqlite(connection)
                .Options;
            var db = new AegisDbContext(options);
            await db.Database.EnsureCreatedAsync();
            return new DbFixture(connection, db);
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
