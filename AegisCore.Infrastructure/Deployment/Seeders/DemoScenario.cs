using System.Text.Json;
using System.Text.Json.Serialization;
using AegisCore.Domain;

namespace AegisCore.Infrastructure.Deployment.Seeders;

public sealed class DemoScenario
{
    public string Version { get; init; } = string.Empty;
    public DemoInstitution Institution { get; init; } = new();
    public DemoIntegrations Integrations { get; init; } = new();
    public DemoDepartment Department { get; init; } = new();
    public DemoBatch Batch { get; init; } = new();
    public DemoProgram Program { get; init; } = new();
    public DemoAcademicYear AcademicYear { get; init; } = new();
    public DemoSemester Semester { get; init; } = new();
    public DemoIntake Intake { get; init; } = new();
    public List<DemoRole> Roles { get; init; } = [];
    public List<DemoAccount> Accounts { get; init; } = [];
    public DemoStudent Student { get; init; } = new();
    public List<DemoCourse> Courses { get; init; } = [];
    public List<DemoEnrollment> Enrollments { get; init; } = [];
    public DemoFee Fee { get; init; } = new();
    public DemoApplicant Applicant { get; init; } = new();

    public static DemoScenario Load(string root, string name)
    {
        if (string.IsNullOrWhiteSpace(root)) throw new InvalidOperationException("DemoSeed:ScenarioRoot is required.");
        if (string.IsNullOrWhiteSpace(name) || name.Any(c => !(char.IsAsciiLetterOrDigit(c) || c == '-')))
            throw new InvalidOperationException("DemoSeed:Scenario must contain only letters, digits and hyphens.");
        var safeRoot = Path.GetFullPath(root);
        var path = Path.GetFullPath(Path.Combine(safeRoot, name, "scenario.json"));
        if (!path.StartsWith(safeRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Scenario path is outside the configured root.");
        if (!File.Exists(path)) throw new FileNotFoundException("Configured demo scenario was not found.", path);
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web) {
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
        };
        options.Converters.Add(new JsonStringEnumConverter());
        var scenario = JsonSerializer.Deserialize<DemoScenario>(File.ReadAllText(path), options)
            ?? throw new InvalidOperationException("Scenario JSON is empty.");
        scenario.Validate();
        return scenario;
    }

    public void Validate()
    {
        static void Present(string value, string field) { if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException($"Scenario field {field} is required."); }
        static Dictionary<string, T> Unique<T>(IEnumerable<T> rows, Func<T, string> key, string field)
        {
            var result = new Dictionary<string, T>(StringComparer.Ordinal);
            foreach (var row in rows) if (!result.TryAdd(key(row), row)) throw new InvalidOperationException($"Duplicate scenario {field}: {key(row)}");
            return result;
        }
        Present(Version, "version"); Present(Institution.Name, "institution.name"); Present(Institution.Domain, "institution.domain");
        Present(Department.Key, "department.key"); Present(Batch.Key, "batch.key"); Present(Program.Key, "program.key");
        if (Batch.StartYear >= Batch.EndYear) throw new InvalidOperationException("Batch end year must be after its start year.");
        if (Program.MinimumCredits <= 0 || Program.MinimumCgpa is < 0 or > 4) throw new InvalidOperationException("Program graduation thresholds are invalid.");
        if (AcademicYear.Start >= AcademicYear.End || Semester.Start >= Semester.End) throw new InvalidOperationException("Academic date ranges are invalid.");
        var roles = Unique(Roles, x => x.Key, "role key");
        var accounts = Unique(Accounts, x => x.Key, "account key");
        foreach (var account in Accounts) { Present(account.Email, $"accounts[{account.Key}].email"); if (!roles.ContainsKey(account.RoleKey)) throw new InvalidOperationException($"Unknown role {account.RoleKey}."); }
        if (!accounts.ContainsKey(Student.AccountKey) || Accounts.Single(x => x.Key == Student.AccountKey).Kind != DemoAccountKind.Student)
            throw new InvalidOperationException("Student must reference a student account.");
        var courses = Unique(Courses, x => x.Key, "course key");
        if (Courses.Any(c => c.Credits <= 0)) throw new InvalidOperationException("Course credits must be positive.");
        foreach (var enrollment in Enrollments) if (!courses.ContainsKey(enrollment.CourseKey)) throw new InvalidOperationException($"Unknown enrollment course {enrollment.CourseKey}.");
        if (Fee.Total <= 0 || (Fee.Components ?? []).Sum(x => x.Amount) != Fee.Total) throw new InvalidOperationException("Fee components must exactly equal the fee total.");
        Unique(Fee.Components ?? [], x => x.Order.ToString(System.Globalization.CultureInfo.InvariantCulture), "fee component order");
        if (Applicant.Email.EndsWith(".invalid", StringComparison.OrdinalIgnoreCase) is false) throw new InvalidOperationException("Demo applicant email must use a reserved .invalid domain.");
        if (Accounts.Any(a => !a.Email.EndsWith(".invalid", StringComparison.OrdinalIgnoreCase))) throw new InvalidOperationException("Demo accounts must use reserved .invalid domains.");
    }
}

public enum DemoAccountKind { Administrator, Faculty, Student }
public sealed record DemoInstitution(string Name = "", string ShortName = "", string Timezone = "", string Currency = "", string Domain = "");
public sealed record DemoIntegrations(string SsoProvider = "local", string AiProvider = "disabled", string EmailProvisioning = "manual", string PaymentMode = "manual", bool DatabaseTls = false);
public sealed record DemoDepartment(string Key = "", string Name = "", string Code = "");
public sealed record DemoBatch(string Key = "", string Name = "", int StartYear = 0, int EndYear = 0);
public sealed record DemoProgram(string Key = "", string Name = "", string Code = "", string DegreeType = "", int DurationYears = 4, int MinimumCredits = 0, decimal MinimumCgpa = 0);
public sealed record DemoAcademicYear(string Key = "", string Name = "", DateOnly Start = default, DateOnly End = default);
public sealed record DemoSemester(string Key = "", string Name = "", DateOnly Start = default, DateOnly End = default, string Status = "active");
public sealed record DemoIntake(string Key = "", string Name = "", int Year = 0, IntakeSemester Semester = IntakeSemester.Fall);
public sealed record DemoRole(string Key = "", string Name = "", string Description = "", List<string>? Permissions = null);
public sealed record DemoAccount(string Key = "", DemoAccountKind Kind = DemoAccountKind.Student, string RoleKey = "", string Email = "", string FirstName = "", string LastName = "");
public sealed record DemoStudent(string AccountKey = "", string UniversityId = "", string RollNumber = "");
public sealed record DemoCourse(string Key = "", string Code = "", string Title = "", int Credits = 0, string? PrerequisiteKey = null, string? FacultyAccountKey = null);
public sealed record DemoEnrollment(string Key = "", string CourseKey = "", string Grade = "", decimal GpaPoints = 0, EnrollmentStatus Status = EnrollmentStatus.Active);
public sealed record DemoFeeComponent(string Key = "", string Name = "", decimal Amount = 0, int Order = 0);
public sealed record DemoFee(string Key = "", int SemesterNumber = 1, string Type = "open_merit", decimal Total = 0, List<DemoFeeComponent>? Components = null, decimal StudentDue = 0);
public sealed record DemoApplicant(string Key = "", string ApplicationNumber = "", string Email = "", string FullName = "");
