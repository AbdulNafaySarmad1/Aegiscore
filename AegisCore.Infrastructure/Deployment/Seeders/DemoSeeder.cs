using AegisCore.Application.Common.Interfaces;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Deployment.Seeders;

public sealed class DemoSeeder
{
    private readonly AegisDbContext _db;
    private readonly IPasswordHashingService _passwords;
    private readonly ILogger<DemoSeeder> _logger;

    private static readonly Guid GoldenStudentUserId = Guid.Parse("a0000000-0000-0000-0000-000000000008");
    private static readonly Guid Student2UserId      = Guid.Parse("a0000000-0000-0000-0000-000000000009");
    private static readonly Guid FacultyUserId       = Guid.Parse("f0000000-0000-0000-0000-000000000001");
    private static readonly Guid VcUserId            = Guid.Parse("a0000000-0000-0000-0000-000000000001");
    private static readonly Guid RegistrarUserId     = Guid.Parse("a0000000-0000-0000-0000-000000000002");
    private static readonly Guid FinanceUserId       = Guid.Parse("a0000000-0000-0000-0000-000000000003");
    private static readonly Guid ExamControllerUserId = Guid.Parse("a0000000-0000-0000-0000-000000000004");
    private static readonly Guid ItHeadUserId        = Guid.Parse("a0000000-0000-0000-0000-000000000005");
    private static readonly Guid HrUserId            = Guid.Parse("a0000000-0000-0000-0000-000000000006");

    private static readonly (Guid UserId, string FirstName, string LastName, string Email, string Password, string Role, string? Program, int Semester, decimal CGPA, string Description)[] DemoAccounts =
    [
        (VcUserId,             "Prof. Dr. Waseem", "Ahmed",    "vc@iobm.demo",         "Lambo$Aventador1", "SuperAdmin",    null,                   0, 0m,    "Vice Chancellor"),
        (RegistrarUserId,      "Dr. Sana",         "Mirza",    "registrar@iobm.demo",  "Ferrari$Roma22",  "Registrar",     null,                   0, 0m,    "Registrar"),
        (FinanceUserId,        "Mr. Khalid",       "Hussain",  "finance@iobm.demo",    "Porsche$GT3RS",   "Finance",       null,                   0, 0m,    "Finance Head"),
        (ExamControllerUserId, "Ms. Ayesha",       "Qureshi",  "exams@iobm.demo",      "McLaren$720S!",  "ExamController", null,                   0, 0m,    "Exam Controller"),
        (ItHeadUserId,         "Mr. Bilal",         "Raza",     "it@iobm.demo",         "Bugatti$Chiron9", "ITHead",        null,                   0, 0m,    "IT Head"),
        (HrUserId,             "Ms. Nadia",         "Shah",     "hr@iobm.demo",         "Aston$Martin77",  "HR",            null,                   0, 0m,    "HR Head"),
        (FacultyUserId,        "Dr. Imran",         "Siddiqui", "faculty@iobm.demo",    "Pagani$Huayra3",  "Faculty",       null,                   0, 0m,    "Faculty Member"),
        (GoldenStudentUserId,  "Ali",               "Hassan",   "student@iobm.demo",    "Koenigsegg$One1", "Student",       "BS Computer Science",  5, 3.67m, "Student"),
        (Student2UserId,       "Fatima",            "Malik",    "student2@iobm.demo",   "Rimac$Nevera88",  "Student",       "BBA",                  3, 3.42m, "Student 2"),
    ];

    public DemoSeeder(AegisDbContext db, IPasswordHashingService passwords, ILogger<DemoSeeder> logger)
    {
        _db = db;
        _passwords = passwords;
        _logger = logger;
    }

    public async Task SeedAsync(Guid tenantId, CancellationToken ct = default)
    {
        var seedStarted = !await _db.Users.IgnoreQueryFilters().AnyAsync(u => u.TenantId == tenantId, ct);

        _logger.LogInformation("Seeding {Count} demo accounts for tenant {TenantId}", DemoAccounts.Length, tenantId);

        foreach (var (userId, firstName, lastName, email, password, roleName, _, _, _, desc) in DemoAccounts)
        {
            if (await _db.Users.IgnoreQueryFilters().AnyAsync(u => u.TenantId == tenantId && u.Email == email, ct))
            {
                _logger.LogInformation("  {Email} already exists — skipping", email);
                continue;
            }

            var user = new User
            {
                Id = userId,
                TenantId = tenantId,
                FirstName = firstName,
                LastName = lastName,
                Username = email,
                Email = email,
                PasswordHash = _passwords.HashPassword(password),
                Status = UserStatus.Active,
                AuthProvider = AuthProvider.Local,
                CreatedAt = DateTime.UtcNow,
            };
            _db.Users.Add(user);

            var role = _db.Roles.Local
                .FirstOrDefault(r => r.TenantId == tenantId && r.Name == roleName);

            if (role is null)
            {
                role = await _db.Roles.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Name == roleName, ct);
            }

            if (role is null)
            {
                role = new Role
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Name = roleName,
                    IsSystem = false,
                    Description = $"Demo role: {desc}"
                };
                _db.Roles.Add(role);
            }

            _db.UserRoles.Add(new UserRole { Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId, RoleId = role.Id });
            _logger.LogInformation("  CREATED {Email} ({roleName}) — {desc}", email, roleName, desc);
        }

        await SeedAcademicResourcesAsync(tenantId, ct);
        await SeedFinanceResourcesAsync(tenantId, ct);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Demo seed complete for tenant {TenantId}", tenantId);
    }

    private async Task SeedAcademicResourcesAsync(Guid tenantId, CancellationToken ct)
    {
        var csDeptId = Guid.Parse("dddd0000-0000-0000-0000-000000000001");
        var baDeptId = Guid.Parse("dddd0000-0000-0000-0000-000000000002");

        if (!await _db.Departments.IgnoreQueryFilters().AnyAsync(d => d.TenantId == tenantId && d.Id == csDeptId, ct))
        {
            _db.Departments.Add(new Department { Id = csDeptId, Name = "Computer Science", Code = "CS", TenantId = tenantId });
            _db.Departments.Add(new Department { Id = baDeptId, Name = "Business Administration", Code = "BA", TenantId = tenantId });
            _logger.LogInformation("Seeded departments");
        }

        var bsCsId = Guid.Parse("acac0000-0000-0000-0000-000000000001");
        var bbaId  = Guid.Parse("acac0000-0000-0000-0000-000000000002");

        if (!await _db.DegreePrograms.IgnoreQueryFilters().AnyAsync(p => p.TenantId == tenantId && p.Id == bsCsId, ct))
        {
            _db.DegreePrograms.Add(new DegreeProgram { Id = bsCsId, Name = "BS Computer Science", Code = "BS-CS", DegreeType = "BS", DurationYears = 4, MinCreditHours = 130, MinCGPA = 2.0m, DepartmentId = csDeptId, TenantId = tenantId });
            _db.DegreePrograms.Add(new DegreeProgram { Id = bbaId,  Name = "BBA", Code = "BBA", DegreeType = "BBA", DurationYears = 4, MinCreditHours = 130, MinCGPA = 2.0m, DepartmentId = baDeptId, TenantId = tenantId });
            _logger.LogInformation("Seeded degree programs");
        }

        var batchCsId = Guid.Parse("bbbb0000-0000-0000-0000-000000000001");
        var batchBaId = Guid.Parse("bbbb0000-0000-0000-0000-000000000002");

        if (!await _db.Batches.IgnoreQueryFilters().AnyAsync(b => b.Id == batchCsId, ct))
        {
            _db.Batches.Add(new Batch { Id = batchCsId, Name = "BS-CS 2024-2028", StartYear = 2024, EndYear = 2028, DepartmentId = csDeptId });
            _db.Batches.Add(new Batch { Id = batchBaId, Name = "BBA 2024-2028", StartYear = 2024, EndYear = 2028, DepartmentId = baDeptId });
            _logger.LogInformation("Seeded batches");
        }

        if (!await _db.Students.IgnoreQueryFilters().AnyAsync(s => s.Id == GoldenStudentUserId, ct))
        {
            _db.Students.Add(new Student { Id = GoldenStudentUserId, FirstName = "Ali", LastName = "Hassan", UniversityId = "IoBM-2024-0178", EnrollmentStatus = EnrollmentStatus.Active, BatchId = batchCsId });
            _db.Students.Add(new Student { Id = Student2UserId, FirstName = "Fatima", LastName = "Malik", UniversityId = "IoBM-2024-0200", EnrollmentStatus = EnrollmentStatus.Active, BatchId = batchBaId });
            _logger.LogInformation("Seeded student records");
        }

        if (!await _db.StudentProfiles.IgnoreQueryFilters().AnyAsync(sp => sp.TenantId == tenantId && sp.StudentId == GoldenStudentUserId, ct))
        {
            _db.StudentProfiles.Add(new StudentProfile
            {
                Id = Guid.NewGuid(), TenantId = tenantId, StudentId = GoldenStudentUserId,
                ProfileJson = """{"program":"BS Computer Science","currentSemester":"5","cgpa":"3.67","creditsCompleted":"87","totalCredits":"130","enrollmentStatus":"Active","universityId":"IoBM-2024-0178","phone":"+92 300 1234567","address":"A-12, Block 3, Gulshan-e-Iqbal, Karachi","guardianName":"Mr. Hassan Ali","guardianPhone":"+92 300 1234567","paymentAlias":"03211234567","batchId":"bbbb0000-0000-0000-0000-000000000001"}"""
            });
            _db.StudentProfiles.Add(new StudentProfile
            {
                Id = Guid.NewGuid(), TenantId = tenantId, StudentId = Student2UserId,
                ProfileJson = """{"program":"BBA","currentSemester":"3","cgpa":"3.42","creditsCompleted":"45","totalCredits":"130","enrollmentStatus":"Active","universityId":"IoBM-2024-0200","phone":"+92 300 7654321","address":"B-5, Block 5, Clifton, Karachi","guardianName":"Mr. Amir Malik","guardianPhone":"+92 300 7654321","paymentAlias":"03331234567","batchId":"bbbb0000-0000-0000-0000-000000000002"}"""
            });
            _logger.LogInformation("Seeded student profiles");
        }

        if (!await _db.Faculties.IgnoreQueryFilters().AnyAsync(f => f.Id == FacultyUserId, ct))
        {
            _db.Faculties.Add(new Faculty { Id = FacultyUserId, FirstName = "Dr. Imran", LastName = "Siddiqui", DepartmentId = csDeptId, Designation = Designation.Professor });
            _logger.LogInformation("Seeded faculty record");
        }

        var courseIds = new[]
        {
            Guid.Parse("cccc0000-0000-0000-0000-000000000001"),
            Guid.Parse("cccc0000-0000-0000-0000-000000000002"),
            Guid.Parse("cccc0000-0000-0000-0000-000000000003"),
            Guid.Parse("cccc0000-0000-0000-0000-000000000004"),
            Guid.Parse("cccc0000-0000-0000-0000-000000000005"),
            Guid.Parse("cccc0000-0000-0000-0000-000000000006"),
        };

        var courseDefinitions = new (Guid Id, string Title, string Code, int Credits)[]
        {
            (courseIds[0], "Database Systems",       "CS301", 3),
            (courseIds[1], "Artificial Intelligence", "CS401", 3),
            (courseIds[2], "Operating Systems",       "CS302", 3),
            (courseIds[3], "Software Engineering",    "CS303", 3),
            (courseIds[4], "Machine Learning",        "CS402", 3),
            (courseIds[5], "Cyber Security",          "CS403", 3),
        };

        if (!await _db.Courses.IgnoreQueryFilters().AnyAsync(c => c.Id == courseIds[0], ct))
        {
            foreach (var (id, title, code, credits) in courseDefinitions)
                _db.Courses.Add(new Course { Id = id, Title = title, Code = code, CreditHours = credits, DepartmentId = csDeptId });
            _logger.LogInformation("Seeded {Count} courses", courseDefinitions.Length);
        }

        if (!await _db.Enrollments.IgnoreQueryFilters().AnyAsync(e => e.StudentId == GoldenStudentUserId, ct))
        {
            for (int i = 0; i < courseIds.Length; i++)
            {
                _db.Enrollments.Add(new Enrollment
                {
                    Id = Guid.Parse($"eeee0000-0000-0000-0000-00000000000{i + 1}"),
                    StudentId = GoldenStudentUserId,
                    CourseId = courseIds[i],
                    Grade = i < 2 ? "A" : i < 4 ? "B+" : "A-",
                    GpaPoints = i < 2 ? 4.0m : i < 4 ? 3.33m : 3.67m,
                    EnrollmentStatus = EnrollmentStatus.Active,
                });
            }
            _logger.LogInformation("Seeded {Count} enrollments for golden student", courseIds.Length);
        }

        if (!await _db.Timetables.IgnoreQueryFilters().AnyAsync(t => t.BatchId == batchCsId, ct))
        {
            var slots = new (Guid, AegisCore.Domain.DayOfWeek, TimeSpan, TimeSpan, string)[]
            {
                (courseIds[0], AegisCore.Domain.DayOfWeek.Monday,    new TimeSpan(9, 0, 0),  new TimeSpan(11, 0, 0), "CS-101"),
                (courseIds[1], AegisCore.Domain.DayOfWeek.Tuesday,   new TimeSpan(11, 0, 0), new TimeSpan(13, 0, 0), "CS-102"),
                (courseIds[2], AegisCore.Domain.DayOfWeek.Wednesday,  new TimeSpan(9, 0, 0),  new TimeSpan(11, 0, 0), "CS-103"),
                (courseIds[3], AegisCore.Domain.DayOfWeek.Thursday,  new TimeSpan(11, 0, 0), new TimeSpan(13, 0, 0), "CS-104"),
                (courseIds[4], AegisCore.Domain.DayOfWeek.Friday,     new TimeSpan(9, 0, 0),  new TimeSpan(11, 0, 0), "CS-105"),
                (courseIds[5], AegisCore.Domain.DayOfWeek.Monday,    new TimeSpan(14, 0, 0), new TimeSpan(16, 0, 0), "CS-106"),
            };
            foreach (var (courseId, day, start, end, room) in slots)
            {
                _db.Timetables.Add(new Timetable
                {
                    Id = Guid.NewGuid(), BatchId = batchCsId, CourseId = courseId, FacultyId = FacultyUserId,
                    DayOfWeek = day, StartTime = start, EndTime = end, Room = room,
                });
            }
            _logger.LogInformation("Seeded timetable entries");
        }

        if (!await _db.Attendances.IgnoreQueryFilters().AnyAsync(a => a.Enrollment.StudentId == GoldenStudentUserId, ct))
        {
            var today = DateTime.UtcNow.Date;
            var random = new Random(42);
            foreach (var enrollmentId in Enumerable.Range(1, 6).Select(i => Guid.Parse($"eeee0000-0000-0000-0000-00000000000{i}")))
            {
                for (int dayOffset = 0; dayOffset < 30; dayOffset++)
                {
                    var date = today.AddDays(-dayOffset);
                    if (date.DayOfWeek == System.DayOfWeek.Saturday || date.DayOfWeek == System.DayOfWeek.Sunday) continue;
                    _db.Attendances.Add(new Attendance
                    {
                        Id = Guid.NewGuid(), EnrollmentId = enrollmentId, CourseId = courseIds[(int)(enrollmentId.ToByteArray()[0] % 6)],
                        FacultyId = FacultyUserId, Date = date, Status = random.NextDouble() > 0.12 ? AttendanceStatus.Present : AttendanceStatus.Absent,
                    });
                }
            }
            _logger.LogInformation("Seeded attendance records");
        }

        if (!await _db.ExamSchedules.IgnoreQueryFilters().AnyAsync(e => e.CourseId == courseIds[0], ct))
        {
            var baseDate = DateTime.UtcNow.Date.AddDays(14).AddHours(10);
            foreach (var (courseId, i) in courseIds.Select((c, i) => (c, i)))
            {
                _db.ExamSchedules.Add(new ExamSchedule
                {
                    Id = Guid.NewGuid(), CourseId = courseId, ExamType = ExamType.Midterm,
                    ScheduledDateTime = baseDate.AddDays(i * 3), Venue = "Examination Hall A",
                });
            }
            _logger.LogInformation("Seeded exam schedules");
        }

        if (!await _db.CourseGrades.IgnoreQueryFilters().AnyAsync(g => g.StudentId == GoldenStudentUserId, ct))
        {
            var gradeData = new[] { ("A", 4.0m, 88m), ("A-", 3.67m, 85m), ("B+", 3.33m, 80m), ("B+", 3.33m, 78m), ("A-", 3.67m, 84m), ("A", 4.0m, 90m) };
            var semesterId = Guid.Parse("33330000-0000-0000-0000-000000000001");
            for (int i = 0; i < courseIds.Length; i++)
            {
                _db.CourseGrades.Add(new CourseGrade
                {
                    Id = Guid.NewGuid(), CourseId = courseIds[i], SemesterId = semesterId, StudentId = GoldenStudentUserId,
                    TotalMarksObtained = gradeData[i].Item3, PercentageObtained = gradeData[i].Item3,
                    LetterGrade = gradeData[i].Item1, GradePoints = gradeData[i].Item2,
                    IsPassing = true, ResultStatus = "published", CalculatedAt = DateTimeOffset.UtcNow.AddDays(-30), PublishedAt = DateTimeOffset.UtcNow.AddDays(-28),
                });
            }
            _logger.LogInformation("Seeded course grades");
        }
    }

    private async Task SeedFinanceResourcesAsync(Guid tenantId, CancellationToken ct)
    {
        if (!await _db.FeeDemands.IgnoreQueryFilters().AnyAsync(f => f.TenantId == tenantId && f.StudentId == GoldenStudentUserId, ct))
        {
            var demandId = Guid.Parse("fddd0000-0000-0000-0000-000000000001");
            var demand = new FeeDemand
            {
                Id = demandId, TenantId = tenantId, StudentId = GoldenStudentUserId,
                DemandNumber = "DEM-FALL25-001", Currency = "PKR", TotalAmount = 48500m,
                PaidAmount = 0m, OutstandingAmount = 48500m, Status = "OPEN",
                IssuedAt = DateTime.UtcNow.AddDays(-60), DueDate = DateTime.UtcNow.AddDays(30),
            };
            _db.FeeDemands.Add(demand);

            _db.FeeInstallments.Add(new FeeInstallment
            {
                Id = Guid.NewGuid(), TenantId = tenantId, FeeDemandId = demandId,
                Sequence = 1, Amount = 24250m, PaidAmount = 0m, PenaltyAmount = 0m, RecognizedAmount = 0m,
                DueDate = DateTime.UtcNow.AddDays(-10), Status = "OVERDUE",
            });
            _db.FeeInstallments.Add(new FeeInstallment
            {
                Id = Guid.NewGuid(), TenantId = tenantId, FeeDemandId = demandId,
                Sequence = 2, Amount = 24250m, PaidAmount = 0m, PenaltyAmount = 0m, RecognizedAmount = 0m,
                DueDate = DateTime.UtcNow.AddDays(30), Status = "PENDING",
            });

            _logger.LogInformation("Seeded fee demand and installments");
        }

        if (!await _db.PaymentGatewayConfigurations.IgnoreQueryFilters().AnyAsync(g => g.TenantId == tenantId, ct))
        {
            _db.PaymentGatewayConfigurations.Add(new PaymentGatewayConfiguration
            {
                Id = Guid.NewGuid(), TenantId = tenantId, GatewaySlug = "raast", Environment = "sandbox",
                SecretNamespace = $"tenants/{tenantId}/gateways/raast-sandbox", IsActive = true,
                HealthStatus = "ok", ConfiguredByUserId = Guid.Empty, ConfiguredAt = DateTime.UtcNow,
            });
            _logger.LogInformation("Seeded sandbox payment gateway configuration");
        }

        if (!await _db.Licenses.IgnoreQueryFilters().AnyAsync(l => l.TenantId == tenantId, ct))
        {
            _db.Licenses.Add(new License
            {
                Id = Guid.NewGuid(), TenantId = tenantId, CustomerName = "IoBM Demo", CustomerEmail = "admin@iobm.demo",
                LicenseKey = "DEMO-IoBM-2025-0001", Edition = LicenseEdition.Professional, Status = LicenseStatus.Active,
                MaxUsers = 5000, MaxCampuses = 3, FeatureEntitlementsJson = """{"finance":true,"examinations":true,"attendance":true,"transcripts":true,"pqc":true,"raast":true}""",
                IssuedAt = DateTime.UtcNow.AddMonths(-6), ExpiresAt = DateTime.UtcNow.AddYears(10),
            });
            _logger.LogInformation("Seeded license");
        }
    }
}
