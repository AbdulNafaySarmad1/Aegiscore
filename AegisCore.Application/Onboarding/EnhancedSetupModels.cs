namespace AegisCore.Application.Onboarding;

public sealed record SetupTemplateDefinition(
    string Key,
    string DisplayName,
    string[] Columns,
    string[][] ExampleRows,
    string[] Rules);

public sealed record SetupDepartment(
    Guid Id,
    string Name,
    string Code,
    string? HodName,
    int FacultyCount = 0);

public sealed record SetupHierarchy(
    string ViceChancellor,
    IReadOnlyList<SetupDepartment> Departments);

public sealed record SetupPermission(string Role, string Feature, bool Allowed);

public sealed record SetupFileValidation(
    string FileType,
    bool IsValid,
    int RowCount,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings);

public sealed record SetupReview(
    bool Ready,
    IReadOnlyList<GoLiveCheck> Checks,
    SetupHierarchy? Hierarchy,
    IReadOnlyDictionary<string, SetupFileValidation> Uploads,
    IReadOnlyList<SetupPermission> Permissions);

public sealed record SaveHierarchyRequest(
    string ViceChancellor,
    IReadOnlyList<SetupDepartment> Departments);

public sealed record SavePermissionsRequest(IReadOnlyList<SetupPermission> Permissions);

