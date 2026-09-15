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
    public DemoCohort Cohort { get; init; } = new();
    public List<DemoCourse> Courses { get; init; } = [];
    public DemoAcademicHistory AcademicHistory { get; init; } = new();
    public List<DemoGraduationRequirement> GraduationRequirements { get; init; } = [];
    public DemoFee Fee { get; init; } = new();

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
        if (Cohort.Count is < 1 or > 500) throw new InvalidOperationException("Cohort count must be between 1 and 500.");
        if (Cohort.FocalStudentNumber is < 1 || Cohort.FocalStudentNumber > Cohort.Count) throw new InvalidOperationException("The focal student must belong to the cohort.");
        if ((Cohort.GivenNames?.Count ?? 0) == 0 || (Cohort.FamilyNames?.Count ?? 0) == 0 || Cohort.GivenNames!.Count * Cohort.FamilyNames!.Count < Cohort.Count)
            throw new InvalidOperationException("The configured name pools must produce a unique name for every cohort member.");
        Present(Cohort.AccountKeyPrefix, "cohort.accountKeyPrefix"); Present(Cohort.EmailDomain, "cohort.emailDomain");
        Present(Cohort.RollNumberPrefix, "cohort.rollNumberPrefix"); Present(Cohort.ApplicationNumberPrefix, "cohort.applicationNumberPrefix");
        if (!Cohort.EmailDomain.EndsWith(".invalid", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Demo cohort email domain must be reserved under .invalid.");
        var courses = Unique(Courses, x => x.Key, "course key");
        if (Courses.Any(c => c.Credits <= 0)) throw new InvalidOperationException("Course credits must be positive.");
        if (AcademicHistory.FocalCompletedCourseCount is < 1 || AcademicHistory.FocalCompletedCourseCount > Courses.Count)
            throw new InvalidOperationException("Focal completed course count must fit the curriculum.");
        if (AcademicHistory.AttendanceSessions is < 1 or > 100) throw new InvalidOperationException("Attendance session count is invalid.");
        foreach (var requirement in GraduationRequirements)
            if (requirement.CourseKey is not null && !courses.ContainsKey(requirement.CourseKey)) throw new InvalidOperationException($"Unknown graduation requirement course {requirement.CourseKey}.");
        if (Fee.Total <= 0 || (Fee.Components ?? []).Sum(x => x.Amount) != Fee.Total) throw new InvalidOperationException("Fee components must exactly equal the fee total.");
        Unique(Fee.Components ?? [], x => x.Order.ToString(System.Globalization.CultureInfo.InvariantCulture), "fee component order");
        if (Accounts.Any(a => !a.Email.EndsWith(".invalid", StringComparison.OrdinalIgnoreCase))) throw new InvalidOperationException("Demo accounts must use reserved .invalid domains.");
        if (accounts.Values.Any(a => a.Kind == DemoAccountKind.Student)) throw new InvalidOperationException("Student accounts must be generated by the cohort configuration.");
    }

    public IReadOnlyList<DemoCohortMember> BuildCohort()
    {
        var rows = new List<DemoCohortMember>(Cohort.Count);
        for (var offset = 0; offset < Cohort.Count; offset++)
        {
            var number = offset + 1;
            var first = Cohort.GivenNames![offset % Cohort.GivenNames.Count];
            var last = Cohort.FamilyNames![(offset / Cohort.GivenNames.Count) % Cohort.FamilyNames.Count];
            var slug = $"{first}.{last}".ToLowerInvariant().Replace(" ", "-");
            rows.Add(new DemoCohortMember(number, $"{Cohort.AccountKeyPrefix}{number:D3}", first, last,
                $"{slug}@{Cohort.EmailDomain}", $"{Cohort.UniversityIdPrefix}{number:D3}",
                $"{Cohort.RollNumberPrefix}{number:D3}", $"{Cohort.ApplicationNumberPrefix}{number:D4}"));
        }
        return rows;
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
public sealed record DemoCohort(int Count = 0, int FocalStudentNumber = 1, string AccountKeyPrefix = "", string EmailDomain = "",
    string UniversityIdPrefix = "", string RollNumberPrefix = "", string ApplicationNumberPrefix = "", List<string>? GivenNames = null,
    List<string>? FamilyNames = null);
public sealed record DemoCohortMember(int Number, string AccountKey, string FirstName, string LastName, string Email,
    string UniversityId, string RollNumber, string ApplicationNumber);
public sealed record DemoCourse(string Key = "", string Code = "", string Title = "", int Credits = 0, string? PrerequisiteKey = null, string? FacultyAccountKey = null);
public sealed record DemoAcademicHistory(int FocalCompletedCourseCount = 0, int CohortMinimumCompletedCourseCount = 4,
    int AttendanceSessions = 10, string PassingGrade = "B+", decimal PassingGpaPoints = 3.3m);
public sealed record DemoGraduationRequirement(string Key = "", string Name = "", DegreeRequirementCategory Category = DegreeRequirementCategory.Core,
    RequirementType RequirementType = RequirementType.CourseCompletion, string? CourseKey = null, int? MinimumCredits = null,
    decimal? MinimumGradePoints = null, int Order = 0);
public sealed record DemoFeeComponent(string Key = "", string Name = "", decimal Amount = 0, int Order = 0);
public sealed record DemoFee(string Key = "", int SemesterNumber = 1, string Type = "open_merit", decimal Total = 0, List<DemoFeeComponent>? Components = null, decimal StudentDue = 0);
