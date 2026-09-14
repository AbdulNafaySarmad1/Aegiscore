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

public sealed record DemoSeedResult(string Scenario, string Version, Guid TenantId);

public sealed class DemoSeeder(AegisDbContext db, ILogger<DemoSeeder> logger,
    IPasswordHashingService passwords, IConfiguration configuration)
{
    public static Guid RecordId(Guid tenant, string version, string key)
        => new(SHA256.HashData(Encoding.UTF8.GetBytes($"{version}:{tenant:N}:{key}"))[..16]);

    public async Task<DemoSeedResult> SeedAsync(Guid tenantId, CancellationToken ct = default)
    {
        if (!configuration.GetValue<bool>("DemoSeed:Enabled") || tenantId == Guid.Empty ||
            !Guid.TryParse(configuration["DemoSeed:TenantId"], out var allowed) || allowed != tenantId)
            throw new InvalidOperationException("Demo seeding requires explicit opt-in for this tenant.");
        var password = configuration["DemoSeed:Password"];
        if (string.IsNullOrWhiteSpace(password) || password.Length < 16)
            throw new InvalidOperationException("Supply a demo-only password of at least 16 characters.");
        if (db.CurrentTenantId != tenantId)
            throw new InvalidOperationException("The seed context must resolve the dedicated demo tenant.");
        var scenarioName = configuration["DemoSeed:Scenario"] ?? string.Empty;
        var scenario = DemoScenario.Load(configuration["DemoSeed:ScenarioRoot"] ?? string.Empty, scenarioName);

        if (db.ChangeTracker.HasChanges()) throw new InvalidOperationException("Seed requires a clean unit of work.");
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            Guid Id(string key) => RecordId(tenantId, scenario.Version, key);
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

            await Add(new Department { Id = Id(scenario.Department.Key), TenantId = tenantId, Name = scenario.Department.Name, Code = scenario.Department.Code }, Id(scenario.Department.Key));
            await Add(new Batch { Id = Id(scenario.Batch.Key), TenantId = tenantId, Name = scenario.Batch.Name, DepartmentId = Id(scenario.Department.Key), StartYear = scenario.Batch.StartYear, EndYear = scenario.Batch.EndYear }, Id(scenario.Batch.Key));
            await Add(new DegreeProgram { Id = Id(scenario.Program.Key), TenantId = tenantId, Name = scenario.Program.Name, Code = scenario.Program.Code, DegreeType = scenario.Program.DegreeType,
                DurationYears = scenario.Program.DurationYears, DepartmentId = Id(scenario.Department.Key), MinCreditHours = scenario.Program.MinimumCredits, MinCGPA = scenario.Program.MinimumCgpa }, Id(scenario.Program.Key));
            await Add(new AcademicYear { Id = Id(scenario.AcademicYear.Key), TenantId = tenantId, Name = scenario.AcademicYear.Name, StartDate = scenario.AcademicYear.Start, EndDate = scenario.AcademicYear.End, IsCurrent = true }, Id(scenario.AcademicYear.Key));
            await Add(new Semester { Id = Id(scenario.Semester.Key), TenantId = tenantId, AcademicYearId = Id(scenario.AcademicYear.Key), Name = scenario.Semester.Name, Type = "Semester",
                StartDate = scenario.Semester.Start, EndDate = scenario.Semester.End, Status = scenario.Semester.Status, IsCurrent = true }, Id(scenario.Semester.Key));
            await Add(new AdmissionIntake { Id = Id(scenario.Intake.Key), TenantId = tenantId, Name = scenario.Intake.Name, Year = scenario.Intake.Year, Semester = scenario.Intake.Semester,
                Status = IntakeStatus.Open, BatchId = Id(scenario.Batch.Key) }, Id(scenario.Intake.Key));

            var roleIds = new Dictionary<string, Guid>(StringComparer.Ordinal);
            foreach (var configuredRole in scenario.Roles)
            {
                var role = await db.Roles.SingleOrDefaultAsync(r => r.TenantId == tenantId && r.Name == configuredRole.Name, ct);
                var roleId = role?.Id ?? Id(configuredRole.Key);
                if (role is null) await Add(new Role { Id = roleId, TenantId = tenantId, Name = configuredRole.Name, Description = configuredRole.Description }, roleId);
                roleIds[configuredRole.Key] = roleId;
                foreach (var value in configuredRole.Permissions ?? [])
                {
                    var parts = value.Split('.', 2);
                    if (parts.Length != 2 || parts.Any(string.IsNullOrWhiteSpace)) throw new InvalidOperationException($"Invalid permission {value}.");
                    var permission = await db.Permissions.SingleOrDefaultAsync(p => p.TenantId == tenantId && p.Resource == parts[0] && p.Action == parts[1], ct);
                    var permissionId = permission?.Id ?? Id($"permission:{value}");
                    if (permission is null) await Add(new Permission { Id = permissionId, TenantId = tenantId, Name = value, Resource = parts[0], Action = parts[1], Module = parts[0] }, permissionId);
                    if (!await db.RolePermissions.AnyAsync(p => p.RoleId == roleId && p.PermissionId == permissionId, ct))
                        await Add(new RolePermission { Id = Id($"permission-grant:{configuredRole.Key}:{value}"), TenantId = tenantId, RoleId = roleId, PermissionId = permissionId }, Id($"permission-grant:{configuredRole.Key}:{value}"));
                }
            }

            foreach (var account in scenario.Accounts)
            {
                var userId = Id(account.Key);
                if (!await db.Users.AnyAsync(u => u.Id == userId, ct))
                    await Add(new User { Id = userId, TenantId = tenantId, Username = account.Email, Email = account.Email, FirstName = account.FirstName, LastName = account.LastName,
                        PasswordHash = passwords.HashPassword(password), Status = UserStatus.Active, AuthProvider = AuthProvider.Local }, userId);
                await Add(new UserRole { Id = Id($"role-grant:{account.Key}"), TenantId = tenantId, UserId = userId, RoleId = roleIds[account.RoleKey] }, Id($"role-grant:{account.Key}"));
                if (account.Kind == DemoAccountKind.Faculty)
                    await Add(new Faculty { Id = userId, TenantId = tenantId, FirstName = account.FirstName, LastName = account.LastName, DepartmentId = Id(scenario.Department.Key) }, userId);
            }

            var studentAccount = scenario.Accounts.Single(a => a.Key == scenario.Student.AccountKey);
            var studentId = Id(studentAccount.Key);
            await Add(new Student { Id = studentId, TenantId = tenantId, FirstName = studentAccount.FirstName, LastName = studentAccount.LastName, BatchId = Id(scenario.Batch.Key),
                UniversityId = scenario.Student.UniversityId, RollNumber = scenario.Student.RollNumber, EnrollmentStatus = EnrollmentStatus.Active }, studentId);
            foreach (var course in scenario.Courses)
            {
                await Add(new Course { Id = Id(course.Key), TenantId = tenantId, Title = course.Title, Code = course.Code, CreditHours = course.Credits,
                    DepartmentId = Id(scenario.Department.Key), PrerequisiteCourseId = course.PrerequisiteKey is null ? null : Id(course.PrerequisiteKey) }, Id(course.Key));
                if (course.FacultyAccountKey is not null)
                    await Add(new CourseAssignment { Id = Id($"assignment:{course.Key}"), TenantId = tenantId, CourseId = Id(course.Key), FacultyId = Id(course.FacultyAccountKey) }, Id($"assignment:{course.Key}"));
            }
            foreach (var enrollment in scenario.Enrollments)
                await Add(new Enrollment { Id = Id(enrollment.Key), TenantId = tenantId, StudentId = studentId, CourseId = Id(enrollment.CourseKey), Grade = enrollment.Grade,
                    GpaPoints = enrollment.GpaPoints, EnrollmentStatus = enrollment.Status }, Id(enrollment.Key));

            await Add(new FeeStructure { Id = Id(scenario.Fee.Key), TenantId = tenantId, AcademicYearId = Id(scenario.AcademicYear.Key), ProgramId = Id(scenario.Program.Key),
                Semester = scenario.Fee.SemesterNumber, FeeType = scenario.Fee.Type, TotalFee = scenario.Fee.Total }, Id(scenario.Fee.Key));
            foreach (var component in scenario.Fee.Components ?? [])
                await Add(new FeeComponent { Id = Id(component.Key), TenantId = tenantId, FeeStructureId = Id(scenario.Fee.Key), ComponentName = component.Name, Amount = component.Amount, OrderIndex = component.Order }, Id(component.Key));
            if (scenario.Fee.StudentDue > 0)
                await Add(new StudentFeeDue { Id = Id($"due:{scenario.Student.AccountKey}"), TenantId = tenantId, StudentId = studentId, SemesterId = Id(scenario.Semester.Key),
                    FeeStructureId = Id(scenario.Fee.Key), TotalDue = scenario.Fee.StudentDue, OutstandingAmount = scenario.Fee.StudentDue }, Id($"due:{scenario.Student.AccountKey}"));

            await Add(new ApplicantProfile { Id = Id(scenario.Applicant.Key), TenantId = tenantId, ApplicantId = scenario.Applicant.ApplicationNumber, Email = scenario.Applicant.Email,
                FullName = scenario.Applicant.FullName, IntakeId = Id(scenario.Intake.Key), ProgramId = Id(scenario.Program.Key), Status = ApplicantStatus.Draft }, Id(scenario.Applicant.Key));
            if (!await db.EmailProvisioningConfigs.AnyAsync(c => c.TenantId == tenantId, ct))
                await Add(new EmailProvisioningConfig { Id = Id("email-provisioning"), TenantId = tenantId, Mode = EmailProvisioningMode.Manual, DomainName = scenario.Institution.Domain }, Id("email-provisioning"));
            await transaction.CommitAsync(ct);
        });
        logger.LogInformation("Scenario {Scenario} version {Version} seeded for dedicated tenant {TenantId}", scenarioName, scenario.Version, tenantId);
        return new(scenarioName, scenario.Version, tenantId);
    }
}
