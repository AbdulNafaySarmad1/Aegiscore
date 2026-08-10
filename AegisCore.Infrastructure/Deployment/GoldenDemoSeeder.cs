using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using AegisCore.Application.Analytics;
using AegisCore.Application.Common.Interfaces;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Deployment;

public sealed class GoldenDemoSeeder(
    AegisDbContext db,
    IPasswordHashingService passwords,
    IStatisticalAnalyticsEngine statistics,
    ITelemetryPolicyEngine telemetryPolicy,
    IConfiguration configuration,
    ILogger<GoldenDemoSeeder> logger)
{
    public const int ExpectedGoldenUsers = 12;
    public const int ExpectedCohortSize = 36;
    private static readonly DateTime DemoInstant = new(2026, 8, 10, 9, 0, 0, DateTimeKind.Utc);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly (string Key, string First, string Last, string Email, string Role, string Purpose)[] Personas =
    [
        ("student", "Ali", "Hassan", "student@iobm.demo", "Student", "Golden Student"),
        ("teacher", "Imran", "Siddiqui", "faculty@iobm.demo", "Faculty", "Golden Teacher"),
        ("admissions", "Sana", "Mirza", "admissions@iobm.demo", "AdmissionsHead", "Admissions Officer"),
        ("registrar", "Hira", "Nawaz", "registrar@iobm.demo", "Registrar", "Registrar"),
        ("exams", "Ayesha", "Qureshi", "exams@iobm.demo", "ExamController", "Examination Officer"),
        ("finance", "Khalid", "Hussain", "finance@iobm.demo", "Finance", "Finance Officer"),
        ("hod", "Farhan", "Ahmed", "hod@iobm.demo", "HOD", "HOD / Program Head"),
        ("dean", "Nadia", "Rahman", "dean@iobm.demo", "Dean", "Dean"),
        ("qa", "Maham", "Iqbal", "qa@iobm.demo", "OBECoordinator", "OBE / QA Officer"),
        ("hr", "Nadia", "Shah", "hr@iobm.demo", "HR", "HR Officer"),
        ("auditor", "Usman", "Tariq", "auditor@iobm.demo", "Auditor", "Auditor / Compliance Officer"),
        ("admin", "Bilal", "Raza", "it@iobm.demo", "Admin", "System Administrator")
    ];

    private static readonly (string First, string Last)[] SupportingNames =
    [
        ("Areeba", "Khan"), ("Hamza", "Malik"), ("Fatima", "Sheikh"), ("Saad", "Ahmed"), ("Noor", "Raza"),
        ("Muneeb", "Ali"), ("Laiba", "Siddiqui"), ("Zain", "Qureshi"), ("Eman", "Nawaz"), ("Daniyal", "Hussain"),
        ("Hania", "Iqbal"), ("Talha", "Farooq"), ("Mehwish", "Akram"), ("Rafay", "Khalid"), ("Anaya", "Javed"),
        ("Shayan", "Mirza"), ("Mariam", "Tariq"), ("Ahsan", "Butt"), ("Rida", "Aslam"), ("Hassan", "Baig"),
        ("Iqra", "Saleem"), ("Abdullah", "Rashid"), ("Mahnoor", "Yousaf"), ("Huzaifa", "Ansari"), ("Kinza", "Arif"),
        ("Umer", "Shah"), ("Aiman", "Zafar"), ("Mikaeel", "Latif"), ("Saba", "Rehman"), ("Waleed", "Aziz"),
        ("Rameen", "Saeed"), ("Mustafa", "Khawaja"), ("Alina", "Hashmi"), ("Ibrahim", "Nadeem"), ("Zoya", "Masood")
    ];

    public async Task SeedAsync(Guid tenantId, CancellationToken ct = default)
    {
        if (!configuration.GetValue<bool>("Runtime:DemoMode"))
            throw new InvalidOperationException("Golden demo seeding requires Runtime:DemoMode=true.");
        var environment = configuration["ASPNETCORE_ENVIRONMENT"] ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (!string.Equals(environment, "Development", StringComparison.Ordinal))
            throw new InvalidOperationException("Golden demo data may be seeded only in the Development environment.");
        if (db.Database.IsNpgsql()
            && !string.Equals(db.Database.GetDbConnection().Database, "aegiscore_golden_demo", StringComparison.Ordinal))
            throw new InvalidOperationException("Golden demo seeding requires the isolated aegiscore_golden_demo database.");
        var password = configuration["AEGISCORE_DEMO_PASSWORD"] ?? configuration["Demo:Password"];
        if (string.IsNullOrWhiteSpace(password) || password.Length < 12)
            throw new InvalidOperationException("AEGISCORE_DEMO_PASSWORD must contain at least 12 characters.");

        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            if (db.Database.IsNpgsql())
                await db.Database.ExecuteSqlInterpolatedAsync($"SELECT set_config('app.tenant_id', {tenantId.ToString()}, true)", ct);

            await AddIfMissingAsync(tenantId, () => new Tenant
            {
                Id = tenantId, Name = "AegisCore Golden University", ShortName = "ACGU", LicenseMode = "demo",
                IsActive = true, CreatedAt = DemoInstant, ActivatedAt = DemoInstant
            }, ct);

            var users = await SeedUsersAsync(tenantId, password, ct);
            var ids = new DemoIds(tenantId);
            await SeedAcademicStructureAsync(ids, ct);
            users["hod"].StaffDepartmentId = ids.Department;
            users["dean"].StaffDepartmentId = ids.Department;
            await db.SaveChangesAsync(ct);
            var students = await SeedPeopleAndCoursesAsync(ids, users, ct);
            await SeedAdmissionsAsync(ids, users, ct);
            await SeedAcademicEvidenceAsync(ids, users, students, ct);
            await SeedSupportingWorkflowsAsync(ids, users, ct);
            await SeedTelemetryAsync(ids, students, ct);
            await transaction.CommitAsync(ct);
        });

        logger.LogInformation(
            "Golden demo seeded for tenant {TenantId}: {UserCount} personas, {StudentCount} showcase students, no ML/SHAP snapshots.",
            tenantId, ExpectedGoldenUsers, ExpectedCohortSize);
    }

    private async Task<Dictionary<string, User>> SeedUsersAsync(Guid tenantId, string password, CancellationToken ct)
    {
        var users = new Dictionary<string, User>(StringComparer.Ordinal);
        foreach (var persona in Personas)
        {
            var role = await db.Roles.IgnoreQueryFilters().SingleOrDefaultAsync(item => item.Name == persona.Role, ct);
            if (role is null)
            {
                role = new Role
                {
                    Id = StableId(tenantId, $"role:{persona.Role}"), TenantId = tenantId, Name = persona.Role,
                    Description = $"Golden demo role: {persona.Purpose}", IsSystem = false, CreatedAt = DemoInstant
                };
                db.Roles.Add(role);
                await db.SaveChangesAsync(ct);
            }

            var user = await db.Users.IgnoreQueryFilters().SingleOrDefaultAsync(item => item.TenantId == tenantId && item.Email == persona.Email, ct);
            if (user is null)
            {
                user = new User
                {
                    Id = StableId(tenantId, $"user:{persona.Key}"), TenantId = tenantId, Username = persona.Email,
                    Email = persona.Email, FirstName = persona.First, LastName = persona.Last, Status = UserStatus.Active,
                    AuthProvider = AuthProvider.Local, CreatedAt = DemoInstant,
                    PasswordHash = passwords.HashPassword(password)
                };
                db.Users.Add(user);
            }
            if (string.IsNullOrWhiteSpace(user.PasswordHash) || !passwords.VerifyPassword(password, user.PasswordHash))
                user.PasswordHash = passwords.HashPassword(password);
            await db.SaveChangesAsync(ct);

            if (!await db.UserRoles.IgnoreQueryFilters().AnyAsync(item => item.UserId == user.Id && item.RoleId == role.Id, ct))
            {
                db.UserRoles.Add(new UserRole
                {
                    Id = StableId(tenantId, $"user-role:{persona.Key}:{persona.Role}"), TenantId = tenantId,
                    UserId = user.Id, RoleId = role.Id, AssignedByUserId = user.Id, AssignedAt = DemoInstant
                });
                await db.SaveChangesAsync(ct);
            }
            users[persona.Key] = user;
        }
        return users;
    }

    private async Task SeedAcademicStructureAsync(DemoIds ids, CancellationToken ct)
    {
        await AddIfMissingAsync(ids.Department, () => new Department
        {
            Id = ids.Department, TenantId = ids.Tenant, Name = "Department of Computer Science", Code = "CS", CreatedAt = DemoInstant
        }, ct);
        await AddIfMissingAsync(ids.Batch, () => new Batch
        {
            Id = ids.Batch, Name = "BSCS 2022 Golden Cohort", StartYear = 2022, EndYear = 2026,
            DepartmentId = ids.Department, CreatedAt = DemoInstant.AddYears(-4)
        }, ct);
        await AddIfMissingAsync(ids.Program, () => new DegreeProgram
        {
            Id = ids.Program, TenantId = ids.Tenant, DepartmentId = ids.Department, Name = "BS Computer Science",
            Code = "BSCS", DegreeType = "Bachelor", DurationYears = 4, MinCreditHours = 130, MinCGPA = 2m,
            CurriculumVersion = CurriculumVersion.V2025, IsActive = true, CreatedAt = DemoInstant.AddYears(-4)
        }, ct);
        await AddIfMissingAsync(ids.AcademicYear, () => new AcademicYear
        {
            Id = ids.AcademicYear, Name = "2025-2026", StartDate = new DateOnly(2025, 8, 18),
            EndDate = new DateOnly(2026, 6, 30), IsCurrent = false, CreatedAt = DemoInstant.AddYears(-1)
        }, ct);
        await AddIfMissingAsync(ids.AdmissionAcademicYear, () => new AcademicYear
        {
            Id = ids.AdmissionAcademicYear, Name = "2022-2023", StartDate = new DateOnly(2022, 8, 15),
            EndDate = new DateOnly(2023, 6, 30), IsCurrent = false, CreatedAt = DemoInstant.AddYears(-4)
        }, ct);
        await AddIfMissingAsync(ids.AdmissionSemester, () => new Semester
        {
            Id = ids.AdmissionSemester, AcademicYearId = ids.AdmissionAcademicYear, Name = "Fall 2022", Type = "fall",
            StartDate = new DateOnly(2022, 8, 15), EndDate = new DateOnly(2022, 12, 23),
            ExamStartDate = new DateOnly(2022, 12, 12), ExamEndDate = new DateOnly(2022, 12, 23),
            ResultDeclarationDate = new DateOnly(2023, 1, 6), Status = "completed", CreatedAt = DemoInstant.AddYears(-4)
        }, ct);
        await AddIfMissingAsync(ids.Semester, () => new Semester
        {
            Id = ids.Semester, AcademicYearId = ids.AcademicYear, Name = "Spring 2026", Type = "spring",
            StartDate = new DateOnly(2026, 1, 19), EndDate = new DateOnly(2026, 6, 12),
            ExamStartDate = new DateOnly(2026, 5, 25), ExamEndDate = new DateOnly(2026, 6, 5),
            ResultDeclarationDate = new DateOnly(2026, 6, 20), Status = "completed", CreatedAt = DemoInstant.AddMonths(-8)
        }, ct);
    }

    private async Task<IReadOnlyList<Student>> SeedPeopleAndCoursesAsync(
        DemoIds ids,
        IReadOnlyDictionary<string, User> users,
        CancellationToken ct)
    {
        await AddIfMissingAsync(ids.Teacher, () => new Faculty
        {
            Id = ids.Teacher, FirstName = "Imran", LastName = "Siddiqui", EmployeeCode = "ACGU-CS-014",
            Designation = Designation.AssistantProfessor, DepartmentId = ids.Department, CreatedAt = DemoInstant.AddYears(-7)
        }, ct);
        for (var index = 1; index <= 3; index++)
        {
            var facultyId = ids.Id($"faculty:support:{index}");
            await AddIfMissingAsync(facultyId, () => new Faculty
            {
                Id = facultyId, FirstName = $"Supporting{index}", LastName = "Faculty", EmployeeCode = $"ACGU-CS-{20 + index}",
                Designation = Designation.Lecturer, DepartmentId = ids.Department, CreatedAt = DemoInstant.AddYears(-3)
            }, ct);
        }

        var courses = new[]
        {
            (ids.ShowcaseCourse, "CS-201", "Data Structures & Algorithms", 4),
            (ids.Id("course:db"), "CS-220", "Database Systems", 3),
            (ids.Id("course:se"), "CS-310", "Software Engineering", 3),
            (ids.Id("course:net"), "CS-330", "Computer Networks", 3)
        };
        foreach (var course in courses)
            await AddIfMissingAsync(course.Item1, () => new Course
            {
                Id = course.Item1, Code = course.Item2, Title = course.Item3, CreditHours = course.Item4,
                DepartmentId = ids.Department, CreatedAt = DemoInstant.AddYears(-3)
            }, ct);

        await AddIfMissingAsync(ids.Id("course-assignment:dsa"), () => new CourseAssignment
        {
            Id = ids.Id("course-assignment:dsa"), CourseId = ids.ShowcaseCourse, FacultyId = ids.Teacher,
            CreatedAt = DemoInstant.AddMonths(-8)
        }, ct);
        await AddIfMissingAsync(ids.Id("timetable:dsa"), () => new Timetable
        {
            Id = ids.Id("timetable:dsa"), BatchId = ids.Batch, CourseId = ids.ShowcaseCourse, FacultyId = ids.Teacher,
            DayOfWeek = AegisCore.Domain.DayOfWeek.Monday, StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(11, 30, 0),
            Room = "CS Lab 2", CreatedAt = DemoInstant.AddMonths(-8)
        }, ct);

        var students = new List<Student>(ExpectedCohortSize);
        var golden = await db.Students.IgnoreQueryFilters().SingleOrDefaultAsync(item => item.Id == ids.GoldenStudent, ct);
        if (golden is null)
        {
            golden = new Student
            {
                Id = ids.GoldenStudent, FirstName = "Ali", LastName = "Hassan", UniversityId = "ACGU-22K-1042",
                EnrollmentStatus = EnrollmentStatus.Graduated, BatchId = ids.Batch, CreatedAt = DemoInstant.AddYears(-4)
            };
            db.Students.Add(golden);
            await db.SaveChangesAsync(ct);
        }
        students.Add(golden);
        for (var index = 0; index < SupportingNames.Length; index++)
        {
            var studentId = ids.Id($"student:support:{index + 1}");
            var student = await db.Students.IgnoreQueryFilters().SingleOrDefaultAsync(item => item.Id == studentId, ct);
            if (student is null)
            {
                student = new Student
                {
                    Id = studentId, FirstName = SupportingNames[index].First, LastName = SupportingNames[index].Last,
                    UniversityId = $"ACGU-22K-{1100 + index}", EnrollmentStatus = EnrollmentStatus.Graduated,
                    BatchId = ids.Batch, CreatedAt = DemoInstant.AddYears(-4)
                };
                db.Students.Add(student);
                await db.SaveChangesAsync(ct);
            }
            students.Add(student);
        }

        var studentUser = users["student"];
        studentUser.StudentId = ids.GoldenStudent;
        await db.SaveChangesAsync(ct);
        await AddIfMissingAsync(ids.Id("student-profile:golden"), () => new StudentProfile
        {
            Id = ids.Id("student-profile:golden"), StudentId = ids.GoldenStudent, TenantId = ids.Tenant,
            ProfileJson = JsonSerializer.Serialize(new { programId = ids.Program, cohort = "BSCS 2022", lifecycle = "graduated-alumni" }),
            UpdatedAt = DemoInstant
        }, ct);
        return students;
    }

    private async Task SeedAdmissionsAsync(DemoIds ids, IReadOnlyDictionary<string, User> users, CancellationToken ct)
    {
        await AddIfMissingAsync(ids.Application, () => new AdmissionApplication
        {
            Id = ids.Application, ApplicantUserId = users["student"].Id, StudentName = "Ali Hassan",
            Email = users["student"].Email, Phone = "+92-300-5550104", Cnic = "42101-0000104-7",
            DateOfBirth = new DateOnly(2004, 3, 18), ProgramId = ids.Program, QuotaType = "open_merit",
            MatricPercentage = 88m, FscPercentage = 84m, EntryTestScore = 82,
            ApplicationStatus = "offer_accepted", SubmittedAt = DemoInstant.AddYears(-4).AddMonths(-2),
            CreatedAt = DemoInstant.AddYears(-4).AddMonths(-2), UpdatedAt = DemoInstant.AddYears(-4)
        }, ct);
        await AddIfMissingAsync(ids.MeritList, () => new MeritList
        {
            Id = ids.MeritList, ProgramId = ids.Program, SemesterId = ids.AdmissionSemester, QuotaType = "open_merit",
            RoundNumber = 1, TotalSeats = 40, GeneratedBy = users["admissions"].Id, Status = "published",
            GeneratedAt = DemoInstant.AddYears(-4).AddMonths(-1), PublishedAt = DemoInstant.AddYears(-4).AddMonths(-1)
        }, ct);
        await AddIfMissingAsync(ids.Id("merit-entry:golden"), () => new MeritListEntry
        {
            Id = ids.Id("merit-entry:golden"), MeritListId = ids.MeritList, ApplicationId = ids.Application,
            MeritRank = 7, MeritScore = 84.40m, MatricContribution = 26.40m, FscContribution = 25.20m,
            EntryTestContribution = 32.80m, SeatAllocated = true, AllocationStatus = "allocated", CreatedAt = DemoInstant.AddYears(-4)
        }, ct);
        await AddIfMissingAsync(ids.Id("seat-allocation:golden"), () => new SeatAllocation
        {
            Id = ids.Id("seat-allocation:golden"), MeritListEntryId = ids.Id("merit-entry:golden"),
            ApplicationId = ids.Application, SeatType = "regular", AllocatedAt = DemoInstant.AddYears(-4)
        }, ct);
    }

    private async Task SeedAcademicEvidenceAsync(
        DemoIds ids,
        IReadOnlyDictionary<string, User> users,
        IReadOnlyList<Student> students,
        CancellationToken ct)
    {
        for (var studentIndex = 0; studentIndex < students.Count; studentIndex++)
        {
            var student = students[studentIndex];
            var enrollmentId = ids.Id($"enrollment:dsa:{student.Id:N}");
            await AddIfMissingAsync(enrollmentId, () => new Enrollment
            {
                Id = enrollmentId, StudentId = student.Id, CourseId = ids.ShowcaseCourse,
                EnrollmentStatus = EnrollmentStatus.Graduated, CreatedAt = DemoInstant.AddMonths(-8)
            }, ct);
            for (var session = 0; session < 12; session++)
            {
                var present = student.Id == ids.GoldenStudent ? session != 7 : (session + studentIndex) % 7 != 0;
                var attendanceId = ids.Id($"attendance:{student.Id:N}:{session}");
                await AddIfMissingAsync(attendanceId, () => new Attendance
                {
                    Id = attendanceId, EnrollmentId = enrollmentId, CourseId = ids.ShowcaseCourse, FacultyId = ids.Teacher,
                    Date = DemoInstant.AddMonths(-7).AddDays(session * 7), Status = present ? AttendanceStatus.Present : AttendanceStatus.Absent,
                    CreatedAt = DemoInstant.AddMonths(-7).AddDays(session * 7)
                }, ct);
            }
        }

        await AddIfMissingAsync(ids.AssessmentScheme, () => new AssessmentScheme
        {
            Id = ids.AssessmentScheme, CourseId = ids.ShowcaseCourse, SemesterId = ids.Semester,
            ExamControllerId = users["exams"].Id, CreatedAt = DemoInstant.AddMonths(-8)
        }, ct);
        var components = new[]
        {
            ("assignment", "Assignments", 15, 20), ("quiz", "Quizzes", 10, 10), ("lab", "Labs", 15, 20),
            ("midterm", "Midterm", 25, 30), ("final", "Final", 35, 40)
        };
        for (var index = 0; index < components.Length; index++)
        {
            var component = components[index];
            var componentId = ids.Id($"assessment-component:{component.Item1}");
            await AddIfMissingAsync(componentId, () => new AssessmentComponent
            {
                Id = componentId, SchemeId = ids.AssessmentScheme, ComponentType = component.Item2,
                WeightPercentage = component.Item3, TotalMarks = component.Item4, PassingMarks = component.Item4 / 2,
                IsMandatory = true, OrderIndex = index + 1, CreatedAt = DemoInstant.AddMonths(-8)
            }, ct);
        }

        await AddIfMissingAsync(ids.GradeScale, () => new GradeScale
        {
            Id = ids.GradeScale, UniversityId = ids.Tenant, Name = "ACGU Undergraduate Scale 2025", CreatedAt = DemoInstant.AddYears(-1)
        }, ct);
        var bands = new[]
        {
            ("F", 0, 49, 0m), ("D", 50, 59, 1m), ("C", 60, 69, 2m), ("B", 70, 76, 3m),
            ("B+", 77, 79, 3.5m), ("A", 80, 100, 4m)
        };
        foreach (var band in bands)
        {
            var bandId = ids.Id($"grade-band:{band.Item1}");
            await AddIfMissingAsync(bandId, () => new GradeBand
            {
                Id = bandId, GradeScaleId = ids.GradeScale, LetterGrade = band.Item1, PercentageFrom = band.Item2,
                PercentageTo = band.Item3, GpaPoints = band.Item4, Description = "ACGU published undergraduate boundary"
            }, ct);
        }

        for (var index = 0; index < students.Count; index++)
        {
            var student = students[index];
            var marks = student.Id == ids.GoldenStudent
                ? new[] { 18, 8, 16, 23, 30 }
                : new[] { 11 + index % 9, 5 + index % 5, 10 + index % 10, 15 + index % 14, 20 + index % 19 };
            decimal score = 0;
            for (var componentIndex = 0; componentIndex < components.Length; componentIndex++)
            {
                var component = components[componentIndex];
                score += Math.Round((decimal)marks[componentIndex] / component.Item4 * component.Item3, 4);
                var markId = ids.Id($"course-mark:{student.Id:N}:{component.Item1}");
                await AddIfMissingAsync(markId, () => new CourseMark
                {
                    Id = markId, CourseId = ids.ShowcaseCourse, StudentId = student.Id,
                    AssessmentComponentId = ids.Id($"assessment-component:{component.Item1}"), ObtainedMarks = marks[componentIndex],
                    EnteredBy = users["teacher"].Id, EntryDate = DemoInstant.AddMonths(-3 + componentIndex)
                }, ct);
            }
            score = Math.Round(score, 2, MidpointRounding.AwayFromZero);
            var band = bands.Last(item => score >= item.Item2);
            var gradeId = student.Id == ids.GoldenStudent ? ids.GoldenGrade : ids.Id($"course-grade:{student.Id:N}");
            await AddIfMissingAsync(gradeId, () => new CourseGrade
            {
                Id = gradeId, CourseId = ids.ShowcaseCourse, SemesterId = ids.Semester, StudentId = student.Id,
                TotalMarksObtained = marks.Sum(), PercentageObtained = score, LetterGrade = band.Item1, GradePoints = band.Item4,
                IsPassing = score >= 50, ResultStatus = "published", CalculatedAt = DemoInstant.AddMonths(-2),
                PublishedAt = DemoInstant.AddMonths(-1)
            }, ct);
        }

        await AddIfMissingAsync(ids.Id("exam:midterm"), () => new ExamSchedule
        {
            Id = ids.Id("exam:midterm"), CourseId = ids.ShowcaseCourse, ExamType = ExamType.Midterm,
            ScheduledDateTime = DemoInstant.AddMonths(-5), Venue = "Main Block 204", CreatedAt = DemoInstant.AddMonths(-8)
        }, ct);
        await AddIfMissingAsync(ids.Id("exam:final"), () => new ExamSchedule
        {
            Id = ids.Id("exam:final"), CourseId = ids.ShowcaseCourse, ExamType = ExamType.Final,
            ScheduledDateTime = DemoInstant.AddMonths(-2), Venue = "Examination Hall A", CreatedAt = DemoInstant.AddMonths(-8)
        }, ct);

        await AddIfMissingAsync(ids.Id("po:computing"), () => new ProgramOutcome
        {
            Id = ids.Id("po:computing"), Code = "PO-CS-2", Description = "Analyze computing problems and design suitable algorithms.",
            ProgramId = ids.Program, SortOrder = 2, CreatedAt = DemoInstant.AddYears(-2), TenantId = ids.Tenant
        }, ct);
        await AddIfMissingAsync(ids.Id("clo:dsa"), () => new CourseLearningOutcome
        {
            Id = ids.Id("clo:dsa"), Code = "CS201-CLO-2", Description = "Implement and evaluate core data structures and algorithms.",
            CourseId = ids.ShowcaseCourse, BloomsLevel = BloomsLevel.Analyze, SortOrder = 2,
            CreatedAt = DemoInstant.AddYears(-2), TenantId = ids.Tenant
        }, ct);
        await AddIfMissingAsync(ids.Id("clo-po:dsa"), () => new CLOPOMapping
        {
            Id = ids.Id("clo-po:dsa"), CourseLearningOutcomeId = ids.Id("clo:dsa"), ProgramOutcomeId = ids.Id("po:computing"),
            Strength = MappingStrength.High, CreatedAt = DemoInstant.AddYears(-2), TenantId = ids.Tenant
        }, ct);
    }

    private async Task SeedSupportingWorkflowsAsync(DemoIds ids, IReadOnlyDictionary<string, User> users, CancellationToken ct)
    {
        await AddIfMissingAsync(ids.Id("employee:teacher"), () => new Employee
        {
            Id = ids.Id("employee:teacher"), FirstName = "Imran", LastName = "Siddiqui", EmployeeCode = "ACGU-CS-014",
            Email = users["teacher"].Email, EmployeeType = EmployeeType.Permanent, Status = EmployeeStatus.Active,
            Designation = Designation.AssistantProfessor, DepartmentId = ids.Department, DateOfJoining = DemoInstant.AddYears(-7),
            Salary = 235000m, CreatedAt = DemoInstant.AddYears(-7), TenantId = ids.Tenant
        }, ct);
        await AddIfMissingAsync(ids.Id("workload:teacher:dsa"), () => new FacultyWorkload
        {
            Id = ids.Id("workload:teacher:dsa"), FacultyId = ids.Teacher, CourseId = ids.ShowcaseCourse,
            UnitType = WorkloadUnitType.Lecture, HoursPerWeek = 4, StudentCount = ExpectedCohortSize,
            Schedule = "Monday/Wednesday 10:00-11:30, CS Lab 2", EffectiveFrom = DemoInstant.AddMonths(-8),
            EffectiveTo = DemoInstant.AddMonths(-1), TenantId = ids.Tenant
        }, ct);
        await AddIfMissingAsync(ids.Id("grade-appeal:golden"), () => new GradeAppeal
        {
            Id = ids.Id("grade-appeal:golden"), StudentId = ids.GoldenStudent, CourseId = ids.ShowcaseCourse,
            Semester = "Spring 2026", SubmittedGrade = "B+", AppealGrounds = "Requested verification of final-exam totaling.",
            GroundType = AppealGroundType.ClericalError, GroundStrength = EvidenceStrength.Moderate,
            Status = AppealStatus.Dismissed, FacultyRespondentId = ids.Teacher,
            CommitteeDecision = "Marks re-totaled and verified; published B+ remains unchanged.",
            SubmissionDate = DemoInstant.AddDays(-45), ResolvedAt = DemoInstant.AddDays(-35),
            PolicyClauseCitation = "EXAM-APPEAL-4.2", TenantId = ids.Tenant
        }, ct);

        await AddIfMissingAsync(ids.Id("fee-demand:golden"), () => new FeeDemand
        {
            Id = ids.Id("fee-demand:golden"), StudentId = ids.GoldenStudent, DemandNumber = "ACGU-SP26-22K1042",
            Currency = "PKR", TotalAmount = 145000m, PaidAmount = 145000m, OutstandingAmount = 0m,
            Status = "PAID", IssuedAt = DemoInstant.AddMonths(-9), DueDate = DemoInstant.AddMonths(-8), TenantId = ids.Tenant
        }, ct);
        await AddIfMissingAsync(ids.Id("finance-payment:golden"), () => new FinancePayment
        {
            Id = ids.Id("finance-payment:golden"), FeeDemandId = ids.Id("fee-demand:golden"),
            IdempotencyKey = "golden-demo-sp26-22k1042", Method = "BANK_TRANSFER", ExternalReference = "DEMO-RAAST-1042",
            Amount = 145000m, Status = "SETTLED", CreatedAt = DemoInstant.AddMonths(-8),
            SettledAt = DemoInstant.AddMonths(-8).AddMinutes(3), TenantId = ids.Tenant
        }, ct);

        var requirementId = ids.Id("degree-requirement:showcase");
        await AddIfMissingAsync(requirementId, () => new DegreeRequirement
        {
            Id = requirementId, ProgramId = ids.Program, Name = "Golden lifecycle completion evidence",
            Category = DegreeRequirementCategory.Core, RequirementType = RequirementType.CourseCompletion,
            CourseId = ids.ShowcaseCourse, CourseCount = 1, MinGradePoints = 2m,
            AllowedCourseIdsJson = JsonSerializer.Serialize(new[] { ids.ShowcaseCourse }), SortOrder = 1,
            IsRequired = true, CreatedAt = DemoInstant.AddYears(-4), TenantId = ids.Tenant
        }, ct);
        await AddIfMissingAsync(ids.Id("degree-audit:golden"), () => new AegisCore.Domain.Entities.DegreeAudit
        {
            Id = ids.Id("degree-audit:golden"), StudentId = ids.GoldenStudent, ProgramId = ids.Program,
            RequirementId = requirementId, Status = AuditStatus.Satisfied, EarnedValue = 4m, RequiredValue = 2m,
            CompletedCount = 1, RequiredCount = 1, Notes = "Showcase requirement satisfied; full 130-credit history is outside the legacy attempt model.",
            LastEvaluatedAt = DemoInstant.AddDays(-20), TenantId = ids.Tenant
        }, ct);
        await AddIfMissingAsync(ids.Id("alumni:golden"), () => new AlumniProfile
        {
            Id = ids.Id("alumni:golden"), UserId = users["student"].Id, GraduationYear = 2026,
            AlumniId = "ACGU-AL-2026-1042", CurrentLocation = "Karachi, Pakistan",
            Bio = "Synthetic golden-demo alumni profile linked to the BSCS 2022 cohort.",
            CreatedAt = DemoInstant.AddDays(-10), TenantId = ids.Tenant
        }, ct);

        var provenance = new[]
        {
            ("admissions", users["admissions"].Id, "PUBLISH", "MeritList", ids.MeritList),
            ("registrar", users["registrar"].Id, "LINK_STUDENT", "Student", ids.GoldenStudent),
            ("finance", users["finance"].Id, "RECONCILE", "FeeDemand", ids.Id("fee-demand:golden")),
            ("exams", users["exams"].Id, "PUBLISH", "CourseGrade", ids.GoldenGrade),
            ("qa", users["qa"].Id, "VERIFY", "CLOPOMapping", ids.Id("clo-po:dsa")),
            ("hr", users["hr"].Id, "VERIFY", "Employee", ids.Id("employee:teacher")),
            ("auditor", users["auditor"].Id, "READ_AUDIT", "CourseGrade", ids.GoldenGrade),
            ("admin", users["admin"].Id, "ASSIGN_ROLE", "User", users["student"].Id)
        };
        foreach (var item in provenance)
        {
            var auditId = ids.Id($"audit:{item.Item1}");
            await AddIfMissingAsync(auditId, () => new AuditLog
            {
                Id = auditId, TenantId = ids.Tenant, UserId = item.Item2, Action = item.Item3,
                ResourceType = item.Item4, ResourceId = item.Item5, Details = "Golden demo provenance record",
                Success = true, Timestamp = DemoInstant.AddDays(-30)
            }, ct);
        }
    }

    private async Task SeedTelemetryAsync(DemoIds ids, IReadOnlyList<Student> students, CancellationToken ct)
    {
        var cohortGrades = await db.CourseGrades.IgnoreQueryFilters().AsNoTracking()
            .Where(item => item.CourseId == ids.ShowcaseCourse && item.SemesterId == ids.Semester)
            .Select(item => (double?)item.PercentageObtained).ToListAsync(ct);
        var grade = await db.CourseGrades.IgnoreQueryFilters().AsNoTracking().SingleAsync(item => item.Id == ids.GoldenGrade, ct);
        var attendance = await db.Attendances.IgnoreQueryFilters().AsNoTracking()
            .Where(item => item.Enrollment.StudentId == ids.GoldenStudent && item.CourseId == ids.ShowcaseCourse)
            .Select(item => item.Status).ToListAsync(ct);
        var attendanceValue = Math.Round(attendance.Count(item => item == AttendanceStatus.Present) * 100m / attendance.Count, 2);

        var values = new Dictionary<string, (decimal Current, decimal Previous, IReadOnlyList<double?> Cohort)>
        {
            ["CoursePerformance"] = (grade.PercentageObtained, 72m, cohortGrades),
            ["Assignments"] = (90m, 82m, Enumerable.Range(0, students.Count).Select(index => (double?)(60 + index % 31)).ToArray()),
            ["Quizzes"] = (80m, 80m, Enumerable.Range(0, students.Count).Select(index => (double?)(55 + index % 36)).ToArray()),
            ["Midterms"] = (76.67m, 69m, Enumerable.Range(0, students.Count).Select(index => (double?)(50 + index % 38)).ToArray()),
            ["Finals"] = (75m, 82m, Enumerable.Range(0, students.Count).Select(index => (double?)(48 + index % 43)).ToArray()),
            ["Labs"] = (80m, 74m, Enumerable.Range(0, students.Count).Select(index => (double?)(58 + index % 34)).ToArray()),
            ["Attendance"] = (attendanceValue, 83m, Enumerable.Range(0, students.Count).Select(index => (double?)(70 + index % 28)).ToArray()),
            ["CLOAttainment"] = (68m, 66m, Enumerable.Range(0, students.Count).Select(index => (double?)(52 + index % 34)).ToArray()),
            ["PLOAttainment"] = (63m, 65m, Enumerable.Range(0, students.Count).Select(index => (double?)(50 + index % 32)).ToArray()),
            ["Consistency"] = (58m, 52m, Enumerable.Range(0, students.Count).Select(index => (double?)(45 + index % 36)).ToArray())
        };
        var freshness = new DateTimeOffset(DemoInstant);
        var metrics = values.Select(item => BuildMetric(item.Key, item.Value.Current, item.Value.Previous, item.Value.Cohort, freshness)).ToArray();
        var courseTelemetry = new StudentTelemetry(ids.GoldenStudent, ids.ShowcaseCourse, "Spring 2026", "BSCS 2022 course cohort",
            10, metrics, freshness.AddDays(-1), freshness);
        var overallTelemetry = courseTelemetry with { CourseId = null, ComparisonPopulation = "BSCS 2022 graduating cohort" };
        await AddTelemetrySnapshotAsync(ids.Id("telemetry:dsa:spring-2026"), courseTelemetry);
        await AddTelemetrySnapshotAsync(ids.Id("telemetry:overall:spring-2026"), overallTelemetry);

        async Task AddTelemetrySnapshotAsync(Guid snapshotId, StudentTelemetry telemetry)
        {
            var payload = JsonSerializer.Serialize(telemetry, JsonOptions);
            await AddIfMissingAsync(snapshotId, () => new AnalyticsStudentTelemetrySnapshot
            {
                Id = snapshotId, TenantId = ids.Tenant, StudentId = ids.GoldenStudent, CourseId = telemetry.CourseId,
                Period = telemetry.Period, ComparisonPopulation = telemetry.ComparisonPopulation,
                InputHash = Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(payload))), PayloadJson = payload,
                DataCutoffAt = telemetry.DataCutoffTimestamp, FreshnessAt = telemetry.FreshnessTimestamp
            }, ct);
        }
    }

    private TelemetryMetric BuildMetric(
        string dimension,
        decimal current,
        decimal previous,
        IReadOnlyList<double?> cohort,
        DateTimeOffset freshness)
    {
        var comparison = statistics.CompareToCohort((double)current, cohort, 10);
        var classification = telemetryPolicy.Classify((double)current, (double)previous, [(double)previous - 4, (double)previous], new TelemetryClassificationPolicy());
        return new TelemetryMetric(
            dimension, current, current, Decimal(comparison.PercentileRank), Decimal(comparison.CohortMean),
            Decimal(comparison.CohortMedian), Decimal(comparison.StandardDeviation), Decimal(comparison.ZScore), previous,
            current - previous, comparison.CohortMean is null ? null : current - (decimal)comparison.CohortMean.Value,
            classification.TrendDirection, classification.AbsolutePerformance, classification.PersonalProgress,
            comparison.SampleSize, "Spring 2026", "BSCS 2022 course cohort", freshness,
            new Availability(comparison.Suppressed ? AvailabilityStatus.Suppressed : AvailabilityStatus.Available, comparison.SuppressionReason));
    }

    private async Task AddIfMissingAsync<T>(Guid id, Func<T> factory, CancellationToken ct) where T : class
    {
        if (await db.Set<T>().IgnoreQueryFilters().AnyAsync(item => EF.Property<Guid>(item, "Id") == id, ct)) return;
        db.Set<T>().Add(factory());
        await db.SaveChangesAsync(ct);
    }

    private static decimal? Decimal(double? value) => value is null ? null : Math.Round((decimal)value.Value, 4);

    public static Guid StableId(Guid tenantId, string key)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"{tenantId:N}:{key}"));
        Span<byte> guidBytes = stackalloc byte[16];
        bytes.AsSpan(0, 16).CopyTo(guidBytes);
        guidBytes[7] = (byte)((guidBytes[7] & 0x0F) | 0x50);
        guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);
        return new Guid(guidBytes);
    }

    private sealed class DemoIds(Guid tenant)
    {
        public Guid Tenant { get; } = tenant;
        public Guid Department { get; } = StableId(tenant, "department:cs");
        public Guid Batch { get; } = StableId(tenant, "batch:bscs-2022");
        public Guid Program { get; } = StableId(tenant, "program:bscs");
        public Guid AdmissionAcademicYear { get; } = StableId(tenant, "academic-year:2022-2023");
        public Guid AdmissionSemester { get; } = StableId(tenant, "semester:fall-2022");
        public Guid AcademicYear { get; } = StableId(tenant, "academic-year:2025-2026");
        public Guid Semester { get; } = StableId(tenant, "semester:spring-2026");
        public Guid GoldenStudent { get; } = StableId(tenant, "student:golden");
        public Guid Teacher { get; } = StableId(tenant, "faculty:golden-teacher");
        public Guid ShowcaseCourse { get; } = StableId(tenant, "course:dsa");
        public Guid Application { get; } = StableId(tenant, "application:golden");
        public Guid MeritList { get; } = StableId(tenant, "merit-list:bscs-2022");
        public Guid AssessmentScheme { get; } = StableId(tenant, "assessment-scheme:dsa:spring-2026");
        public Guid GradeScale { get; } = StableId(tenant, "grade-scale:ug-2025");
        public Guid GoldenGrade { get; } = StableId(tenant, "course-grade:golden:dsa");
        public Guid Id(string key) => StableId(Tenant, key);
    }
}
