using System.Security.Cryptography;
using System.Text;
using AegisCore.Application.Common.Interfaces;
using AegisCore.Contracts.Common.Interfaces;
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
        var cohort = scenario.BuildCohort();
        var cohortIds = cohort.Select(member => RecordId(tenantId, scenario.Version, member.AccountKey)).ToList();
        var existingCohort = await db.Students.IgnoreQueryFilters().CountAsync(s => s.TenantId == tenantId && cohortIds.Contains(s.Id), ct);
        if (existingCohort > 0)
        {
            if (existingCohort != cohort.Count) throw new InvalidOperationException($"Scenario cohort is incomplete ({existingCohort}/{cohort.Count}); reset the dedicated demo snapshot before retrying.");
            var linkedApplicants = await db.ApplicantProfiles.IgnoreQueryFilters().CountAsync(a => a.TenantId == tenantId && a.ConvertedStudentId != null && cohortIds.Contains(a.ConvertedStudentId.Value), ct);
            if (linkedApplicants != cohort.Count) throw new InvalidOperationException("Scenario applicants are incomplete; reset the dedicated demo snapshot before retrying.");
            logger.LogInformation("Scenario {Scenario} version {Version} already contains its complete {Count}-student cohort", scenarioName, scenario.Version, cohort.Count);
            return new(scenarioName, scenario.Version, tenantId);
        }

        if (db.ChangeTracker.HasChanges()) throw new InvalidOperationException("Seed requires a clean unit of work.");
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            Guid Id(string key) => RecordId(tenantId, scenario.Version, key);
            Task Add<T>(T row, Guid id) where T : class, ITenantScoped
            {
                if (row.TenantId != tenantId || db.Entry(row).Property<Guid>("Id").CurrentValue != id)
                    throw new InvalidOperationException("Scenario seed identity is inconsistent.");
                db.Add(row);
                return Task.CompletedTask;
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
            await db.SaveChangesAsync(ct);

            var roleIds = new Dictionary<string, Guid>(StringComparer.Ordinal);
            var permissionIds = new Dictionary<string, Guid>(StringComparer.Ordinal);
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
                    if (!permissionIds.TryGetValue(value, out var permissionId))
                    {
                        var permission = await db.Permissions.SingleOrDefaultAsync(p => p.TenantId == tenantId && p.Resource == parts[0] && p.Action == parts[1], ct);
                        permissionId = permission?.Id ?? Id($"permission:{value}");
                        if (permission is null) await Add(new Permission { Id = permissionId, TenantId = tenantId, Name = value, Resource = parts[0], Action = parts[1], Module = parts[0] }, permissionId);
                        permissionIds[value] = permissionId;
                    }
                    if (!db.RolePermissions.Local.Any(p => p.RoleId == roleId && p.PermissionId == permissionId) &&
                        !await db.RolePermissions.AnyAsync(p => p.RoleId == roleId && p.PermissionId == permissionId, ct))
                        await Add(new RolePermission { Id = Id($"permission-grant:{configuredRole.Key}:{value}"), TenantId = tenantId, RoleId = roleId, PermissionId = permissionId }, Id($"permission-grant:{configuredRole.Key}:{value}"));
                }
            }
            await db.SaveChangesAsync(ct);

            foreach (var account in scenario.Accounts)
            {
                var email = account.Email.Trim().ToLowerInvariant();
                var existingUsers = await db.Users.IgnoreQueryFilters()
                    .Where(u => u.TenantId == tenantId && u.Email == email)
                    .Take(2)
                    .ToListAsync(ct);
                if (existingUsers.Count > 1)
                    throw new InvalidOperationException($"Scenario account {email} is duplicated; reset the dedicated demo database before retrying.");
                var userId = existingUsers.Count == 1 ? existingUsers[0].Id : Id(account.Key);
                if (existingUsers.Count == 0)
                    await Add(new User { Id = userId, TenantId = tenantId, Username = account.Email, Email = account.Email, FirstName = account.FirstName, LastName = account.LastName,
                        PasswordHash = passwords.HashPassword(password), Status = UserStatus.Active, AuthProvider = AuthProvider.Local }, userId);
                if (!await db.UserRoles.AnyAsync(r => r.TenantId == tenantId && r.UserId == userId && r.RoleId == roleIds[account.RoleKey], ct))
                    await Add(new UserRole { Id = Id($"role-grant:{account.Key}"), TenantId = tenantId, UserId = userId, RoleId = roleIds[account.RoleKey] }, Id($"role-grant:{account.Key}"));
                if (account.Kind == DemoAccountKind.Faculty)
                {
                    if (!await db.Faculties.AnyAsync(f => f.Id == userId, ct))
                        await Add(new Faculty { Id = userId, TenantId = tenantId, FirstName = account.FirstName, LastName = account.LastName, DepartmentId = Id(scenario.Department.Key) }, userId);
                }
            }
            await db.SaveChangesAsync(ct);

            var focal = cohort.Single(member => member.Number == scenario.Cohort.FocalStudentNumber);
            var studentRoleId = roleIds["role.student"];
            var passwordHash = passwords.HashPassword(password);
            foreach (var member in cohort)
            {
                var studentId = Id(member.AccountKey);
                var applicantId = Id($"applicant:{member.AccountKey}");
                if (!await db.Users.AnyAsync(u => u.Id == studentId, ct))
                    await Add(new User { Id = studentId, TenantId = tenantId, Username = member.Email, Email = member.Email,
                        RecoveryEmail = $"{member.FirstName.ToLowerInvariant()}.{member.LastName.ToLowerInvariant()}@applicant.example.invalid",
                        FirstName = member.FirstName, LastName = member.LastName, PasswordHash = passwordHash,
                        Status = UserStatus.Active, AuthProvider = AuthProvider.Local }, studentId);
                await Add(new UserRole { Id = Id($"role-grant:{member.AccountKey}"), TenantId = tenantId, UserId = studentId, RoleId = studentRoleId }, Id($"role-grant:{member.AccountKey}"));
                await Add(new ApplicantProfile { Id = applicantId, TenantId = tenantId, ApplicantId = member.ApplicationNumber,
                    Email = $"{member.FirstName.ToLowerInvariant()}.{member.LastName.ToLowerInvariant()}@applicant.example.invalid",
                    EmailVerifiedAt = new DateTimeOffset(2026, 3, 2, 4, 0, 0, TimeSpan.Zero), FullName = $"{member.FirstName} {member.LastName}",
                    IntakeId = Id(scenario.Intake.Key), ProgramId = Id(scenario.Program.Key), Status = ApplicantStatus.Admitted,
                    SubmittedAt = new DateTimeOffset(2026, 3, 3, 6, 30, 0, TimeSpan.Zero), ConvertedStudentId = studentId }, applicantId);
                await Add(new Student { Id = studentId, TenantId = tenantId, FirstName = member.FirstName, LastName = member.LastName,
                    BatchId = Id(scenario.Batch.Key), UniversityId = member.UniversityId, RollNumber = member.RollNumber,
                    ApplicantId = member.ApplicationNumber, AdmissionApplicantProfileId = applicantId, EnrollmentStatus = EnrollmentStatus.Active }, studentId);
            }
            await db.SaveChangesAsync(ct);

            foreach (var course in scenario.Courses)
            {
                await Add(new Course { Id = Id(course.Key), TenantId = tenantId, Title = course.Title, Code = course.Code, CreditHours = course.Credits,
                    DepartmentId = Id(scenario.Department.Key), PrerequisiteCourseId = course.PrerequisiteKey is null ? null : Id(course.PrerequisiteKey) }, Id(course.Key));
                if (course.FacultyAccountKey is not null)
                    await Add(new CourseAssignment { Id = Id($"assignment:{course.Key}"), TenantId = tenantId, CourseId = Id(course.Key), FacultyId = Id(course.FacultyAccountKey) }, Id($"assignment:{course.Key}"));
                await db.SaveChangesAsync(ct);
            }
            foreach (var member in cohort)
            {
                var completed = member.Number == focal.Number
                    ? scenario.AcademicHistory.FocalCompletedCourseCount
                    : Math.Min(scenario.Courses.Count - 1, scenario.AcademicHistory.CohortMinimumCompletedCourseCount + (member.Number % 24));
                for (var courseIndex = 0; courseIndex < scenario.Courses.Count; courseIndex++)
                {
                    var course = scenario.Courses[courseIndex];
                    var complete = courseIndex < completed;
                    var enrollmentKey = $"enrollment:{member.AccountKey}:{course.Key}";
                    await Add(new Enrollment { Id = Id(enrollmentKey), TenantId = tenantId, StudentId = Id(member.AccountKey), CourseId = Id(course.Key),
                        Grade = complete ? scenario.AcademicHistory.PassingGrade : null, GpaPoints = complete ? scenario.AcademicHistory.PassingGpaPoints : null,
                        EnrollmentStatus = EnrollmentStatus.Active }, Id(enrollmentKey));
                }
            }
            await db.SaveChangesAsync(ct);

            foreach (var requirement in scenario.GraduationRequirements)
                await Add(new DegreeRequirement { Id = Id(requirement.Key), TenantId = tenantId, ProgramId = Id(scenario.Program.Key), Name = requirement.Name,
                    Category = requirement.Category, RequirementType = requirement.RequirementType,
                    CourseId = requirement.CourseKey is null ? null : Id(requirement.CourseKey), MinCredits = requirement.MinimumCredits,
                    MinGradePoints = requirement.MinimumGradePoints, SortOrder = requirement.Order, IsRequired = true }, Id(requirement.Key));

            var facultyId = Id(scenario.Accounts.Single(a => a.Kind == DemoAccountKind.Faculty).Key);
            foreach (var (course, index) in scenario.Courses.Take(5).Select((course, index) => (course, index)))
                await Add(new Timetable { Id = Id($"timetable:{course.Key}"), TenantId = tenantId, BatchId = Id(scenario.Batch.Key), CourseId = Id(course.Key), FacultyId = facultyId,
                    DayOfWeek = (AegisCore.Domain.DayOfWeek)index, StartTime = TimeSpan.FromHours(9 + index), EndTime = TimeSpan.FromHours(10 + index), Room = $"CS-{201 + index}" }, Id($"timetable:{course.Key}"));

            var attendanceCourses = scenario.Courses.TakeLast(3).ToList();
            foreach (var member in cohort.Where(member => member.Number == focal.Number))
                foreach (var course in attendanceCourses)
                    for (var session = 0; session < scenario.AcademicHistory.AttendanceSessions; session++)
                    {
                        var key = $"attendance:{member.AccountKey}:{course.Key}:{session}";
                        await Add(new Attendance { Id = Id(key), TenantId = tenantId,
                            EnrollmentId = Id($"enrollment:{member.AccountKey}:{course.Key}"), CourseId = Id(course.Key), FacultyId = facultyId,
                            Date = new DateTime(2030, 1, 5, 0, 0, 0, DateTimeKind.Utc).AddDays(session * 7), Status = session == scenario.AcademicHistory.AttendanceSessions - 1 && member.Number % 5 == 0 ? AttendanceStatus.Absent : AttendanceStatus.Present }, Id(key));
                    }
            await db.SaveChangesAsync(ct);

            await Add(new FeeStructure { Id = Id(scenario.Fee.Key), TenantId = tenantId, AcademicYearId = Id(scenario.AcademicYear.Key), ProgramId = Id(scenario.Program.Key),
                Semester = scenario.Fee.SemesterNumber, FeeType = scenario.Fee.Type, TotalFee = scenario.Fee.Total }, Id(scenario.Fee.Key));
            foreach (var component in scenario.Fee.Components ?? [])
                await Add(new FeeComponent { Id = Id(component.Key), TenantId = tenantId, FeeStructureId = Id(scenario.Fee.Key), ComponentName = component.Name, Amount = component.Amount, OrderIndex = component.Order }, Id(component.Key));
            await db.SaveChangesAsync(ct);
            if (scenario.Fee.StudentDue > 0)
                foreach (var member in cohort)
                {
                    var isFocal = member.Number == focal.Number;
                    var dueId = Id($"due:{member.AccountKey}");
                    var dueDate = new DateTime(2026, 9, 30, 0, 0, 0, DateTimeKind.Utc);
                    await Add(new FeeDemand { Id = dueId, TenantId = tenantId, StudentId = Id(member.AccountKey), SemesterId = Id(scenario.Semester.Key),
                        FeeStructureId = Id(scenario.Fee.Key), DemandNumber = $"FD-DEMO-{member.Number:0000}", Currency = "PKR", TotalAmount = scenario.Fee.StudentDue,
                        PaidAmount = isFocal ? scenario.Fee.StudentDue : 0, OutstandingAmount = isFocal ? 0 : scenario.Fee.StudentDue,
                        Status = isFocal ? "PAID" : "OPEN", IssuedAt = new DateTime(2026, 8, 15, 0, 0, 0, DateTimeKind.Utc), DueDate = dueDate }, dueId);
                    await Add(new FeeInstallment { Id = Id($"installment:{member.AccountKey}"), TenantId = tenantId, FeeDemandId = dueId, Sequence = 1,
                        Amount = scenario.Fee.StudentDue, PaidAmount = isFocal ? scenario.Fee.StudentDue : 0, Status = isFocal ? "PAID" : "PENDING", DueDate = dueDate }, Id($"installment:{member.AccountKey}"));
                    if (isFocal)
                        await Add(new FinancePayment { Id = Id($"payment:{member.AccountKey}"), TenantId = tenantId, FeeDemandId = dueId, FeeInstallmentId = Id($"installment:{member.AccountKey}"),
                            IdempotencyKey = "demo:FNU-DEMO-PAID-0001", Method = "MANUAL", Amount = scenario.Fee.StudentDue, ExternalReference = "FNU-DEMO-PAID-0001",
                            Status = "SETTLED", CreatedAt = new DateTime(2026, 8, 20, 5, 0, 0, DateTimeKind.Utc), SettledAt = new DateTime(2026, 8, 20, 5, 5, 0, DateTimeKind.Utc) }, Id($"payment:{member.AccountKey}"));
                }
            await db.SaveChangesAsync(ct);

            var focalApplicantId = Id($"applicant:{focal.AccountKey}");
            foreach (var (kind, index) in new[] { AdmissionDocumentKind.Matric, AdmissionDocumentKind.Intermediate, AdmissionDocumentKind.NationalID, AdmissionDocumentKind.Photo }.Select((kind, index) => (kind, index)))
            {
                await Add(new AdmissionDocumentRequirement { Id = Id($"document-requirement:{kind}"), TenantId = tenantId, IntakeId = Id(scenario.Intake.Key),
                    ProgramId = Id(scenario.Program.Key), DocumentType = kind, IsRequired = true, AcceptedFormat = kind == AdmissionDocumentKind.Photo ? AdmissionDocumentFormat.Image : AdmissionDocumentFormat.Pdf }, Id($"document-requirement:{kind}"));
                await Add(new ApplicantDocument { Id = Id($"document:{focal.AccountKey}:{kind}"), TenantId = tenantId, ApplicantId = focalApplicantId,
                    DocumentType = kind, StoragePath = $"demo/admissions/{focal.ApplicationNumber}/{kind.ToString().ToLowerInvariant()}", FileSize = 180_000 + index * 20_000,
                    MimeType = kind == AdmissionDocumentKind.Photo ? "image/jpeg" : "application/pdf", ValidationStatus = AdmissionDocumentValidationStatus.Approved,
                    ValidationMethod = "manual", ReviewedBy = Id("account.administrator"), ReviewedAt = new DateTimeOffset(2026, 3, 5, 9, 0, 0, TimeSpan.Zero) }, Id($"document:{focal.AccountKey}:{kind}"));
            }
            var subjectId = Id("admission-subject.computing");
            var testProfileId = Id("admission-test-profile.bscs");
            await Add(new AdmissionSubject { Id = subjectId, TenantId = tenantId, Name = "Computing and Quantitative Reasoning", Code = "CQR", IsActive = true }, subjectId);
            await Add(new ProgramTestProfile { Id = testProfileId, TenantId = tenantId, IntakeId = Id(scenario.Intake.Key), ProgramId = Id(scenario.Program.Key), PassMark = 60, TimeLimitMinutes = 60 }, testProfileId);
            await Add(new ProgramTestSubject { Id = Id("admission-test-subject.cqr"), TenantId = tenantId, TestProfileId = testProfileId, SubjectId = subjectId, QuestionCount = 50, MarksPerQuestion = 2 }, Id("admission-test-subject.cqr"));
            await Add(new ApplicantTest { Id = Id($"applicant-test:{focal.AccountKey}"), TenantId = tenantId, ApplicantId = focalApplicantId, TestProfileId = testProfileId,
                PaperHash = new string('A', 64), PaperJson = "[]", ResponsesJson = "{}", Score = 84, Passed = true,
                StartedAt = new DateTimeOffset(2026, 3, 12, 5, 0, 0, TimeSpan.Zero), CompletedAt = new DateTimeOffset(2026, 3, 12, 5, 42, 0, TimeSpan.Zero),
                ExpiresAt = new DateTimeOffset(2026, 3, 12, 6, 0, 0, TimeSpan.Zero), Status = ApplicantTestStatus.Submitted }, Id($"applicant-test:{focal.AccountKey}"));
            if (!await db.EmailProvisioningConfigs.AnyAsync(c => c.TenantId == tenantId, ct))
                await Add(new EmailProvisioningConfig { Id = Id("email-provisioning"), TenantId = tenantId, Mode = EmailProvisioningMode.Manual, DomainName = scenario.Institution.Domain }, Id("email-provisioning"));
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        });
        logger.LogInformation("Scenario {Scenario} version {Version} seeded for dedicated tenant {TenantId}", scenarioName, scenario.Version, tenantId);
        return new(scenarioName, scenario.Version, tenantId);
    }
}
