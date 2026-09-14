using System.Security.Cryptography;
using System.Text;
using AegisCore.Application.Common.Interfaces;
using AegisCore.Domain;
using AegisCore.Domain.Common;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Deployment.Seeders;

public sealed class DemoSeeder(AegisDbContext db, ILogger<DemoSeeder> logger,
    IPasswordHashingService passwords, IConfiguration configuration)
{
    public const string Version = "pilot-foundation-v1";

    public static Guid RecordId(Guid tenant, string key)
        => new(SHA256.HashData(Encoding.UTF8.GetBytes($"{Version}:{tenant:N}:{key}"))[..16]);

    public async Task SeedAsync(Guid tenantId, CancellationToken ct = default)
    {
        if (!configuration.GetValue<bool>("DemoSeed:Enabled") || tenantId == Guid.Empty ||
            !Guid.TryParse(configuration["DemoSeed:TenantId"], out var allowed) || allowed != tenantId)
            throw new InvalidOperationException("Demo seeding requires explicit opt-in for this tenant.");
        var password = configuration["DemoSeed:Password"];
        if (string.IsNullOrWhiteSpace(password) || password.Length < 16)
            throw new InvalidOperationException("Supply a demo-only password of at least 16 characters.");
        if (db.CurrentTenantId != tenantId)
            throw new InvalidOperationException("The seed context must resolve the dedicated demo tenant.");

        if (db.ChangeTracker.HasChanges()) throw new InvalidOperationException("Seed requires a clean unit of work.");
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
        db.ChangeTracker.Clear();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        Guid Id(string key) => RecordId(tenantId, key);
        async Task Add<T>(T row, Guid id) where T : class, ITenantScoped
        {
            var existing = await db.Set<T>().IgnoreQueryFilters().SingleOrDefaultAsync(x => EF.Property<Guid>(x, "Id") == id, ct);
            if (existing is not null)
            {
                if (existing.TenantId != tenantId) throw new InvalidOperationException("Seed identity belongs to another tenant.");
                return;
            }
            db.Add(row);
            await db.SaveChangesAsync(ct);
        }

        await Add(new Department { Id = Id("department"), TenantId = tenantId, Name = "Fictional Computing Faculty" }, Id("department"));
        await Add(new Batch { Id = Id("batch"), TenantId = tenantId, Name = "Fictional 2026 intake", DepartmentId = Id("department"), StartYear = 2026, EndYear = 2030 }, Id("batch"));
        await Add(new DegreeProgram { Id = Id("program"), TenantId = tenantId, Name = "Fictional BS Computing", Code = "BCS", DepartmentId = Id("department"), MinCreditHours = 120, MinCGPA = 2 }, Id("program"));
        await Add(new AcademicYear { Id = Id("year"), TenantId = tenantId, Name = "2026-2027", StartDate = new(2026, 9, 1), EndDate = new(2027, 8, 31), IsCurrent = true }, Id("year"));
        await Add(new Semester { Id = Id("semester"), TenantId = tenantId, AcademicYearId = Id("year"), Name = "Fall 2026", Type = "Semester", StartDate = new(2026, 9, 1), EndDate = new(2027, 1, 31), Status = "active", IsCurrent = true }, Id("semester"));
        await Add(new AdmissionIntake { Id = Id("intake"), TenantId = tenantId, Name = "Fictional Fall 2026", Year = 2026, Semester = IntakeSemester.Fall, Status = IntakeStatus.Open, BatchId = Id("batch") }, Id("intake"));

        foreach (var name in new[] { "Admin", "Faculty", "Student" })
        {
            var role = await db.Roles.SingleOrDefaultAsync(r => r.TenantId == tenantId && r.Name == name, ct);
            var roleId = role?.Id ?? Id($"role:{name}");
            if (role is null) await Add(new Role { Id = roleId, TenantId = tenantId, Name = name, Description = "Fictional pilot role" }, roleId);
            var userId = Id($"user:{name}");
            if (!await db.Users.AnyAsync(u => u.Id == userId, ct))
                await Add(new User { Id = userId, TenantId = tenantId, Username = $"{name.ToLowerInvariant()}@pilot.example.invalid", Email = $"{name.ToLowerInvariant()}@pilot.example.invalid", FirstName = "Fictional", LastName = name, PasswordHash = passwords.HashPassword(password), Status = UserStatus.Active, AuthProvider = AuthProvider.Local }, userId);
            await Add(new UserRole { Id = Id($"grant:{name}"), TenantId = tenantId, UserId = userId, RoleId = roleId }, Id($"grant:{name}"));
            var permissions = name switch
            {
                "Admin" => new[] { "Admissions.Read", "Admissions.Configure", "Admissions.Approve", "Admissions.Admit", "Admissions.ApproveQuestions", "Admissions.Proctor", "Graduation.Approve", "Settings.Read", "Settings.Write" },
                "Faculty" => new[] { "Attendance.Read", "Attendance.Write", "Examinations.Read", "Examinations.Write" },
                _ => new[] { "Students.Read" }
            };
            foreach (var value in permissions)
            {
                var parts = value.Split('.');
                var permission = await db.Permissions.SingleOrDefaultAsync(p => p.TenantId == tenantId && p.Resource == parts[0] && p.Action == parts[1], ct);
                var permissionId = permission?.Id ?? Id($"permission:{value}");
                if (permission is null) await Add(new Permission { Id = permissionId, TenantId = tenantId, Name = value, Resource = parts[0], Action = parts[1], Module = parts[0] }, permissionId);
                if (!await db.RolePermissions.AnyAsync(p => p.RoleId == roleId && p.PermissionId == permissionId, ct))
                    await Add(new RolePermission { Id = Id($"permission-grant:{name}:{value}"), TenantId = tenantId, RoleId = roleId, PermissionId = permissionId }, Id($"permission-grant:{name}:{value}"));
            }
        }
        await Add(new Student { Id = Id("user:Student"), TenantId = tenantId, FirstName = "Fictional", LastName = "Student", BatchId = Id("batch"), UniversityId = "PILOT-HISTORY-1", EnrollmentStatus = EnrollmentStatus.Active }, Id("user:Student"));
        await Add(new Faculty { Id = Id("user:Faculty"), TenantId = tenantId, FirstName = "Fictional", LastName = "Faculty", DepartmentId = Id("department") }, Id("user:Faculty"));
        await Add(new Course { Id = Id("course"), TenantId = tenantId, Title = "Programming Fundamentals", Code = "PILOT101", CreditHours = 3, DepartmentId = Id("department") }, Id("course"));
        await Add(new CourseAssignment { Id = Id("assignment"), TenantId = tenantId, CourseId = Id("course"), FacultyId = Id("user:Faculty") }, Id("assignment"));
        await Add(new Enrollment { Id = Id("history"), TenantId = tenantId, StudentId = Id("user:Student"), CourseId = Id("course"), Grade = "B", GpaPoints = 3, EnrollmentStatus = EnrollmentStatus.Active }, Id("history"));
        await Add(new FeeStructure { Id = Id("fee"), TenantId = tenantId, AcademicYearId = Id("year"), ProgramId = Id("program"), Semester = 1, TotalFee = 10000 }, Id("fee"));
        await Add(new FeeComponent { Id = Id("fee-component"), TenantId = tenantId, FeeStructureId = Id("fee"), ComponentName = "Tuition", Amount = 10000 }, Id("fee-component"));
        await Add(new StudentFeeDue { Id = Id("due"), TenantId = tenantId, StudentId = Id("user:Student"), SemesterId = Id("semester"), FeeStructureId = Id("fee"), TotalDue = 10000, OutstandingAmount = 10000 }, Id("due"));
        // The recorded applicant must still verify OTP and submit through the real UI.
        await Add(new ApplicantProfile { Id = Id("applicant"), TenantId = tenantId, ApplicantId = "APP-2026F-BCS-PILOT001", Email = "applicant@pilot.example.invalid", FullName = "Aisha Fictional", IntakeId = Id("intake"), ProgramId = Id("program"), Status = ApplicantStatus.Draft }, Id("applicant"));
        if (!await db.EmailProvisioningConfigs.AnyAsync(c => c.TenantId == tenantId, ct))
            await Add(new EmailProvisioningConfig { Id = Id("email"), TenantId = tenantId, Mode = EmailProvisioningMode.Manual, DomainName = "pilot.example.invalid" }, Id("email"));
        await transaction.CommitAsync(ct);
        });
        logger.LogInformation("Seed {Version} completed for dedicated tenant {TenantId}", Version, tenantId);
    }
}
