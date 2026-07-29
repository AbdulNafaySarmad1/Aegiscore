namespace AegisCore.Application.ConfigCenter;

public class ConfigDto
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public object? Value { get; set; }
    public int CurrentVersion { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ConfigVersionDto
{
    public Guid Id { get; set; }
    public int VersionNumber { get; set; }
    public object? Value { get; set; }
    public string? ChangeDescription { get; set; }
    public string? ChangedByName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ConfigDiffDto
{
    public string Key { get; set; } = string.Empty;
    public int VersionA { get; set; }
    public int VersionB { get; set; }
    public object? ValueA { get; set; }
    public object? ValueB { get; set; }
    public List<string> Changes { get; set; } = new();
}

public class ConfigAuditLogDto
{
    public Guid Id { get; set; }
    public string ConfigKey { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public object? PreviousValue { get; set; }
    public object? NewValue { get; set; }
    public int? FromVersion { get; set; }
    public int? ToVersion { get; set; }
    public string? ChangedByName { get; set; }
    public DateTime ChangedAt { get; set; }
}

public class SetConfigRequest
{
    public string Key { get; set; } = string.Empty;
    public string ValueJson { get; set; } = "{}";
    public string Domain { get; set; } = "System";
    public string? Description { get; set; }
}

public class RollbackRequest
{
    public int TargetVersion { get; set; }
}
