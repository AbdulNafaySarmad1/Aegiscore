using System.Collections.Concurrent;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using AegisCore.Application.Onboarding;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace AegisCore.Infrastructure.Onboarding;

public sealed class EnhancedSetupService
{
    private const int DefaultMaxRows = 50_000;
    private static readonly Regex EmailPattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    private static readonly Regex PhonePattern = new(@"^03\d{9}$", RegexOptions.Compiled);
    private static readonly Regex StudentIdPattern = new(@"^STU-\d{3,}-\d{4}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex FacultyIdPattern = new(@"^FAC-\d{3,}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex CourseCodePattern = new(@"^[A-Z]{2,10}-\d{3,4}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private readonly string _statePath;
    private readonly int _maxRows;
    private readonly object _gate = new();
    private EnhancedSetupState _state;

    public EnhancedSetupService(IHostEnvironment environment, IConfiguration? configuration = null)
    {
        var directory = Path.Combine(environment.ContentRootPath, "config");
        Directory.CreateDirectory(directory);
        _statePath = Path.Combine(directory, "enhanced-setup.json");
        _maxRows = Math.Clamp(configuration?.GetValue<int?>("TenantConfiguration:ImportMaxRows") ?? DefaultMaxRows, 1, 1_000_000);
        _state = Load();
    }

    public static IReadOnlyDictionary<string, SetupTemplateDefinition> Templates { get; } =
        new Dictionary<string, SetupTemplateDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["students"] = new("students", "Students", ["Student ID", "Name", "Email", "Phone", "Department", "Program", "Admission Date"],
                [["STU-001-2024", "Student Name", "student@example.invalid", "03001234567", "Example Department", "Example Program", "2024-01-15"]],
                ["Student ID: STU-XXX-YYYY", "Email must be unique", "Phone must be 11 digits and start with 03", "Admission Date: YYYY-MM-DD"]),
            ["faculty"] = new("faculty", "Faculty", ["Faculty ID", "Name", "Email", "Phone", "Department", "Designation"],
                [["FAC-001", "Faculty Name", "faculty@example.invalid", "03001234568", "Example Department", "Faculty Member"]],
                ["Faculty ID: FAC-XXX", "Email must be unique", "Use a supported academic designation"]),
            ["employees"] = new("employees", "Employees", ["Employee ID", "Name", "Email", "Department", "Designation"],
                [["EMPLOYEE-001", "Employee Name", "employee@example.invalid", "Example Department", "Administrator"]],
                ["Employee ID must be unique", "Email must be valid", "Department must exist"]),
            ["programs"] = new("programs", "Programs", ["Program Code", "Program Name", "Department", "Degree Type", "Duration Years"],
                [["BS-CS", "BS Computer Science", "Computer Science", "Bachelor", "4"]],
                ["Program code must be unique", "Department must exist"]),
            ["courses"] = new("courses", "Courses", ["Course Code", "Course Name", "Department", "Credits", "Credit Hours", "Lab Hours"],
                [["CS-101", "Introduction to Programming", "Computer Science", "3", "3", "0"]],
                ["Course Code: DEPT-XXX", "Credits must be between 1 and 6", "Department must exist"]),
            ["departments"] = new("departments", "Departments", ["Department Code", "Department Name", "HOD Name"],
                [["EX", "Example Department", "Department Head"]],
                ["Department code and name must be unique", "HOD may be left blank during initial setup"]),
            ["attendance"] = new("attendance", "Attendance", ["Date", "Course Code", "Student ID", "Status", "Notes"],
                [["2024-01-15", "CS-101", "STU-001-2024", "Present", ""]],
                ["Date: YYYY-MM-DD", "Status: Present, Absent, Late, or Excused"]),
            ["marks"] = new("marks", "Marks", ["Course Code", "Student ID", "Quiz 1", "Quiz 2", "Midterm", "Final", "Project"],
                [["CS-101", "STU-001-2024", "8", "9", "75", "78", "90"]],
                ["Quiz values: 0-10", "Midterm, Final, and Project values: 0-100", "Blank marks are allowed"]),
            ["enrollments"] = new("enrollments", "Enrollments", ["Student ID", "Course Code", "Status", "Grade"],
                [["STU-001-2024", "CS-101", "Active", ""]],
                ["Student and course must exist", "Duplicate enrollment is rejected"]),
            ["examinations"] = new("examinations", "Examinations", ["Exam Code", "Course Code", "Exam Date", "Type", "Total Marks"],
                [["MID-CS101-2024", "CS-101", "2024-03-01", "Midterm", "100"]],
                ["Course must exist", "Exam date: YYYY-MM-DD", "Total marks must be positive"]),
            ["financial"] = new("financial", "Financial", ["Student ID", "Date", "Type", "Amount", "Description"],
                [["STU-001-2024", "2024-01-15", "Tuition", "150000", "Semester tuition fee"]],
                ["Date: YYYY-MM-DD", "Amount uses the tenant's configured currency", "Negative amounts are discounts or refunds"])
        };

    public IReadOnlyCollection<SetupTemplateDefinition> GetTemplates() => Templates.Values.ToArray();

    public byte[] GenerateTemplate(string key)
    {
        if (!Templates.TryGetValue(key, out var template))
            throw new KeyNotFoundException($"Unknown setup template '{key}'.");

        return SimpleXlsx.Write(template.DisplayName, template.Columns, template.ExampleRows);
    }

    public byte[] GenerateAllTemplates()
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var template in Templates.Values)
            {
                var entry = archive.CreateEntry($"{template.Key}-template.xlsx", CompressionLevel.SmallestSize);
                using var stream = entry.Open();
                var bytes = GenerateTemplate(template.Key);
                stream.Write(bytes);
            }
        }
        return output.ToArray();
    }

    public SetupHierarchy SaveHierarchy(SaveHierarchyRequest request)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(request.ViceChancellor))
            errors.Add("Vice Chancellor name is required.");
        if (request.Departments.Count == 0)
            errors.Add("Add at least one department.");
        if (request.Departments.Any(x => string.IsNullOrWhiteSpace(x.Name) || string.IsNullOrWhiteSpace(x.Code)))
            errors.Add("Every department needs a name and code.");
        if (request.Departments.GroupBy(x => x.Name.Trim(), StringComparer.OrdinalIgnoreCase).Any(x => x.Count() > 1))
            errors.Add("Department names must be unique.");
        if (request.Departments.GroupBy(x => x.Code.Trim(), StringComparer.OrdinalIgnoreCase).Any(x => x.Count() > 1))
            errors.Add("Department codes must be unique.");
        if (errors.Count > 0)
            throw new ArgumentException(string.Join(" ", errors));

        var hierarchy = new SetupHierarchy(
            request.ViceChancellor.Trim(),
            request.Departments.Select(x => x with
            {
                Id = x.Id == Guid.Empty ? Guid.NewGuid() : x.Id,
                Name = x.Name.Trim(),
                Code = x.Code.Trim().ToUpperInvariant(),
                HodName = string.IsNullOrWhiteSpace(x.HodName) ? null : x.HodName.Trim()
            }).ToArray());

        lock (_gate)
        {
            _state.Hierarchy = hierarchy;
            Audit("hierarchy.saved", $"{hierarchy.Departments.Count} departments");
            Persist();
        }
        return hierarchy;
    }

    public IReadOnlyList<SetupPermission> SavePermissions(SavePermissionsRequest request)
    {
        if (request.Permissions.Count == 0)
            throw new ArgumentException("At least one permission is required.");
        if (request.Permissions.Any(x => string.IsNullOrWhiteSpace(x.Role) || x.Role.Trim().Length > 64 || string.IsNullOrWhiteSpace(x.Feature)))
            throw new ArgumentException("Role names must contain 1 to 64 characters and every permission needs a feature.");

        var permissions = request.Permissions
            .GroupBy(x => $"{x.Role.Trim().ToUpperInvariant()}::{x.Feature.Trim()}", StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Last() with { Role = x.Last().Role.Trim(), Feature = x.Last().Feature.Trim() })
            .OrderBy(x => x.Feature)
            .ThenBy(x => x.Role)
            .ToList();

        lock (_gate)
        {
            _state.Permissions = permissions;
            Audit("permissions.saved", $"{permissions.Count} matrix entries");
            Persist();
        }
        return permissions;
    }

    public IReadOnlyList<SetupPermission> GetPermissions()
    {
        lock (_gate)
        {
            if (_state.Permissions.Count > 0) return _state.Permissions.ToArray();
        }
        return DefaultPermissions();
    }

    public SetupHierarchy? GetHierarchy()
    {
        lock (_gate) return _state.Hierarchy;
    }

    public async Task<SetupFileValidation> ValidateAsync(string fileType, IFormFile file, CancellationToken ct)
    {
        if (!Templates.TryGetValue(fileType, out var template))
            return Invalid(fileType, $"Unknown file type '{fileType}'.");
        if (file.Length == 0)
            return Invalid(fileType, "The file is empty.");
        if (file.Length > 25 * 1024 * 1024)
            return Invalid(fileType, "The file exceeds the 25 MB limit.");

        IReadOnlyList<string[]> rows;
        try
        {
            await using var input = file.OpenReadStream();
            rows = file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
                ? SimpleXlsx.Read(input)
                : await ReadCsvAsync(input, ct);
        }
        catch (Exception ex) when (ex is InvalidDataException or FormatException)
        {
            return Invalid(fileType, ex.Message);
        }

        if (rows.Count == 0) return Invalid(fileType, "The file has no header row.");
        var headers = rows[0].Select(x => x.Trim()).ToArray();
        var missing = template.Columns.Where(x => !headers.Contains(x, StringComparer.OrdinalIgnoreCase)).ToArray();
        if (missing.Length > 0)
            return Invalid(fileType, $"Missing required columns: {string.Join(", ", missing)}.");

        var dataRows = rows.Skip(1).Where(x => x.Any(v => !string.IsNullOrWhiteSpace(v))).ToArray();
        var errors = new List<string>();
        var warnings = new List<string>();
        if (dataRows.Length > _maxRows) errors.Add($"Maximum {_maxRows:N0} data rows are allowed.");

        var index = headers.Select((name, i) => (name, i)).ToDictionary(x => x.name, x => x.i, StringComparer.OrdinalIgnoreCase);
        var seenPrimary = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < Math.Min(dataRows.Length, _maxRows); i++)
        {
            var rowNumber = i + 2;
            string Cell(string name) => index.TryGetValue(name, out var position) && position < dataRows[i].Length ? dataRows[i][position].Trim() : "";
            ValidateRow(fileType, rowNumber, Cell, seenPrimary, seenEmails, errors, warnings);
            if (errors.Count >= 200)
            {
                errors.Add("Validation stopped after 200 errors. Fix these errors and upload again.");
                break;
            }
        }

        var result = new SetupFileValidation(fileType, errors.Count == 0, dataRows.Length, errors, warnings);
        lock (_gate)
        {
            _state.Uploads[fileType] = result;
            _state.Rows[fileType] = rows.Select(x => x.ToArray()).ToList();
            Audit("upload.validated", $"{fileType}: {dataRows.Length} rows, {errors.Count} errors");
            Persist();
        }
        return result;
    }

    public IReadOnlyDictionary<string, IReadOnlyList<string[]>> GetValidatedRows()
    {
        lock (_gate)
            return _state.Rows.ToDictionary(x => x.Key, x => (IReadOnlyList<string[]>)x.Value.ToArray(), StringComparer.OrdinalIgnoreCase);
    }

    public SetupReview Review(WizardStateDto wizard)
    {
        SetupHierarchy? hierarchy;
        Dictionary<string, SetupFileValidation> uploads;
        SetupPermission[] permissions;
        lock (_gate)
        {
            hierarchy = _state.Hierarchy;
            uploads = new Dictionary<string, SetupFileValidation>(_state.Uploads, StringComparer.OrdinalIgnoreCase);
            permissions = (_state.Permissions.Count > 0 ? _state.Permissions : DefaultPermissions()).ToArray();
        }

        var checks = new List<GoLiveCheck>
        {
            new("University configured", !string.IsNullOrWhiteSpace(wizard.UniversityFullName), wizard.UniversityFullName),
            new("Database configured", !string.IsNullOrWhiteSpace(wizard.DbHost), $"{wizard.DbHost}:{wizard.DbPort}"),
            new("Redis configured", !string.IsNullOrWhiteSpace(wizard.RedisHost), $"{wizard.RedisHost}:{wizard.RedisPort}"),
            new("Hierarchy configured", hierarchy is { Departments.Count: > 0 }, $"{hierarchy?.Departments.Count ?? 0} departments"),
            new("Required master data valid",
                new[] { "departments", "students", "faculty", "courses" }.All(x => uploads.TryGetValue(x, out var value) && value.IsValid),
                $"{uploads.Count(x => x.Value.IsValid)}/{Templates.Count} files valid"),
            new("RBAC configured", permissions.Length > 0, $"{permissions.Count(x => x.Allowed)} permissions enabled")
        };
        return new SetupReview(checks.All(x => x.Passed), checks, hierarchy, uploads, permissions);
    }

    private void ValidateRow(
        string type,
        int row,
        Func<string, string> cell,
        HashSet<string> seenPrimary,
        HashSet<string> seenEmails,
        List<string> errors,
        List<string> warnings)
    {
        void Required(string column)
        {
            if (string.IsNullOrWhiteSpace(cell(column))) errors.Add($"Row {row}: {column} is required.");
        }
        void Unique(string column, HashSet<string> seen)
        {
            var value = cell(column);
            if (!string.IsNullOrWhiteSpace(value) && !seen.Add(value)) errors.Add($"Row {row}: duplicate {column} '{value}'.");
        }
        void Date(string column)
        {
            if (!DateOnly.TryParseExact(cell(column), "yyyy-MM-dd", out _)) errors.Add($"Row {row}: {column} must use YYYY-MM-DD.");
        }
        void Number(string column, decimal min, decimal max, bool optional = false)
        {
            var value = cell(column);
            if (optional && string.IsNullOrWhiteSpace(value)) return;
            if (!decimal.TryParse(value, out var parsed) || parsed < min || parsed > max)
                errors.Add($"Row {row}: {column} must be between {min} and {max}.");
        }

        switch (type.ToLowerInvariant())
        {
            case "students":
                Required("Student ID"); Required("Name"); Required("Email"); Required("Phone"); Required("Department"); Required("Program");
                if (!StudentIdPattern.IsMatch(cell("Student ID"))) errors.Add($"Row {row}: invalid Student ID '{cell("Student ID")}'.");
                if (!EmailPattern.IsMatch(cell("Email"))) errors.Add($"Row {row}: invalid email '{cell("Email")}'.");
                if (!PhonePattern.IsMatch(cell("Phone"))) errors.Add($"Row {row}: phone must be 11 digits starting with 03.");
                Unique("Student ID", seenPrimary); Unique("Email", seenEmails); Date("Admission Date");
                break;
            case "faculty":
                Required("Faculty ID"); Required("Name"); Required("Email"); Required("Phone"); Required("Department"); Required("Designation");
                if (!FacultyIdPattern.IsMatch(cell("Faculty ID"))) errors.Add($"Row {row}: invalid Faculty ID '{cell("Faculty ID")}'.");
                if (!EmailPattern.IsMatch(cell("Email"))) errors.Add($"Row {row}: invalid email '{cell("Email")}'.");
                if (!PhonePattern.IsMatch(cell("Phone"))) errors.Add($"Row {row}: phone must be 11 digits starting with 03.");
                Unique("Faculty ID", seenPrimary); Unique("Email", seenEmails);
                break;
            case "employees":
                Required("Employee ID"); Required("Name"); Required("Email"); Required("Department"); Required("Designation");
                Unique("Employee ID", seenPrimary); if (!EmailPattern.IsMatch(cell("Email"))) errors.Add($"Row {row}: invalid email '{cell("Email")}'.");
                break;
            case "programs":
                Required("Program Code"); Required("Program Name"); Required("Department"); Unique("Program Code", seenPrimary);
                Number("Duration Years", 1, 10);
                break;
            case "courses":
                Required("Course Code"); Required("Course Name"); Required("Department");
                if (!CourseCodePattern.IsMatch(cell("Course Code"))) errors.Add($"Row {row}: invalid Course Code '{cell("Course Code")}'.");
                Unique("Course Code", seenPrimary); Number("Credits", 1, 6); Number("Credit Hours", 0, 6); Number("Lab Hours", 0, 6);
                break;
            case "departments":
                Required("Department Code"); Required("Department Name"); Unique("Department Code", seenPrimary);
                break;
            case "attendance":
                Date("Date"); Required("Course Code"); Required("Student ID");
                if (!new[] { "Present", "Absent", "Late", "Excused" }.Contains(cell("Status"), StringComparer.OrdinalIgnoreCase))
                    errors.Add($"Row {row}: Status must be Present, Absent, Late, or Excused.");
                break;
            case "marks":
                Required("Course Code"); Required("Student ID");
                Number("Quiz 1", 0, 10, true); Number("Quiz 2", 0, 10, true);
                Number("Midterm", 0, 100, true); Number("Final", 0, 100, true); Number("Project", 0, 100, true);
                break;
            case "enrollments":
                Required("Student ID"); Required("Course Code");
                break;
            case "examinations":
                Required("Exam Code"); Required("Course Code"); Date("Exam Date"); Required("Type"); Number("Total Marks", 1, 1000);
                Unique("Exam Code", seenPrimary);
                break;
            case "financial":
                Required("Student ID"); Date("Date"); Required("Type");
                if (!decimal.TryParse(cell("Amount"), out _)) errors.Add($"Row {row}: Amount must be numeric.");
                if (string.IsNullOrWhiteSpace(cell("Description"))) warnings.Add($"Row {row}: Description is blank.");
                break;
        }
    }

    private static async Task<IReadOnlyList<string[]>> ReadCsvAsync(Stream stream, CancellationToken ct)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var rows = new List<string[]>();
        while (await reader.ReadLineAsync(ct) is { } line)
            rows.Add(ParseCsvLine(line));
        return rows;
    }

    private static string[] ParseCsvLine(string line)
    {
        var values = new List<string>();
        var value = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"' && quoted && i + 1 < line.Length && line[i + 1] == '"') { value.Append('"'); i++; }
            else if (c == '"') quoted = !quoted;
            else if (c == ',' && !quoted) { values.Add(value.ToString()); value.Clear(); }
            else value.Append(c);
        }
        if (quoted) throw new FormatException("CSV contains an unterminated quoted value.");
        values.Add(value.ToString());
        return values.ToArray();
    }

    private static SetupFileValidation Invalid(string type, string message) =>
        new(type, false, 0, [message], []);

    private static List<SetupPermission> DefaultPermissions()
    {
        var roles = new[] { "Admin", "Faculty", "Student" };
        var features = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Students.Read"] = ["Admin", "Faculty"],
            ["Grades.Read"] = ["Admin", "Faculty", "Student"],
            ["Grades.Write"] = ["Admin", "Faculty"],
            ["Attendance.Read"] = ["Admin", "Faculty", "Student"],
            ["Attendance.Write"] = ["Admin", "Faculty", "Student"],
            ["Attendance.Approve"] = ["Admin", "Faculty"],
            ["CampusPolygon.Read"] = ["Admin"],
            ["CampusPolygon.Write"] = ["Admin"],
            ["System.Write"] = ["Admin"],
            ["System.Admin"] = ["Admin"],
            ["Staff.Read"] = ["Admin"],
            ["Staff.Write"] = ["Admin"],
            ["Payroll.Read"] = ["Admin"],
            ["Payroll.Export"] = ["Admin"],
            ["Welfare.Read"] = ["Admin"],
            ["Welfare.Write"] = ["Admin"]
        };
        return features.SelectMany(feature => roles.Select(role =>
            new SetupPermission(role, feature.Key, feature.Value.Contains(role)))).ToList();
    }

    private EnhancedSetupState Load()
    {
        try
        {
            return File.Exists(_statePath)
                ? JsonSerializer.Deserialize<EnhancedSetupState>(File.ReadAllText(_statePath)) ?? new()
                : new();
        }
        catch (JsonException) { return new(); }
        catch (IOException) { return new(); }
    }

    private void Persist()
    {
        var temp = _statePath + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(_state, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temp, _statePath, true);
    }

    private void Audit(string action, string detail) =>
        _state.Audit.Add(new SetupAudit(DateTimeOffset.UtcNow, action, detail));

    private sealed class EnhancedSetupState
    {
        public SetupHierarchy? Hierarchy { get; set; }
        public List<SetupPermission> Permissions { get; set; } = [];
        public Dictionary<string, SetupFileValidation> Uploads { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<string[]>> Rows { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public List<SetupAudit> Audit { get; set; } = [];
    }

    private sealed record SetupAudit(DateTimeOffset At, string Action, string Detail);

    private static class SimpleXlsx
    {
        private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        public static byte[] Write(string sheetName, string[] headers, string[][] rows)
        {
            using var output = new MemoryStream();
            using (var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
            {
                WriteEntry(zip, "[Content_Types].xml", """
                    <?xml version="1.0" encoding="UTF-8"?>
                    <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                      <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
                      <Default Extension="xml" ContentType="application/xml"/>
                      <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
                      <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
                    </Types>
                    """);
                WriteEntry(zip, "_rels/.rels", """
                    <?xml version="1.0" encoding="UTF-8"?>
                    <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                      <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
                    </Relationships>
                    """);
                WriteEntry(zip, "xl/workbook.xml", $"""
                    <?xml version="1.0" encoding="UTF-8"?>
                    <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
                      <sheets><sheet name="{Escape(sheetName)}" sheetId="1" r:id="rId1"/></sheets>
                    </workbook>
                    """);
                WriteEntry(zip, "xl/_rels/workbook.xml.rels", """
                    <?xml version="1.0" encoding="UTF-8"?>
                    <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                      <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
                    </Relationships>
                    """);
                var sheetRows = new[] { headers }.Concat(rows).Select((row, r) =>
                    $"<row r=\"{r + 1}\">{string.Join("", row.Select((value, c) => $"<c r=\"{Column(c + 1)}{r + 1}\" t=\"inlineStr\"><is><t>{Escape(value)}</t></is></c>"))}</row>");
                WriteEntry(zip, "xl/worksheets/sheet1.xml",
                    $"<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"{Main}\"><sheetData>{string.Join("", sheetRows)}</sheetData></worksheet>");
            }
            return output.ToArray();
        }

        public static IReadOnlyList<string[]> Read(Stream input)
        {
            using var zip = new ZipArchive(input, ZipArchiveMode.Read, leaveOpen: true);
            var sheet = zip.GetEntry("xl/worksheets/sheet1.xml") ?? throw new InvalidDataException("Excel workbook has no first worksheet.");
            var shared = ReadSharedStrings(zip);
            using var sheetStream = sheet.Open();
            var document = XDocument.Load(sheetStream);
            return document.Descendants(Main + "row").Select(row =>
                row.Elements(Main + "c").Select(cell =>
                {
                    var type = (string?)cell.Attribute("t");
                    var value = cell.Descendants(Main + "t").FirstOrDefault()?.Value ?? cell.Element(Main + "v")?.Value ?? "";
                    return type == "s" && int.TryParse(value, out var index) && index >= 0 && index < shared.Count ? shared[index] : value;
                }).ToArray()).ToArray();
        }

        private static List<string> ReadSharedStrings(ZipArchive zip)
        {
            var entry = zip.GetEntry("xl/sharedStrings.xml");
            if (entry is null) return [];
            using var stream = entry.Open();
            var document = XDocument.Load(stream);
            return document.Descendants(Main + "si")
                .Select(x => string.Concat(x.Descendants(Main + "t").Select(t => t.Value))).ToList();
        }

        private static void WriteEntry(ZipArchive zip, string path, string content)
        {
            var entry = zip.CreateEntry(path, CompressionLevel.SmallestSize);
            using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
            writer.Write(content.Trim());
        }

        private static string Column(int number)
        {
            var result = "";
            while (number > 0) { number--; result = (char)('A' + number % 26) + result; number /= 26; }
            return result;
        }

        private static string Escape(string value) =>
            System.Security.SecurityElement.Escape(value) ?? "";
    }
}
