using AegisCore.Domain.Entities;

namespace AegisCore.Application.DataImport;

public interface IDataImportService
{
    Task<ExternalDbConnection> AddConnectionAsync(ExternalDbConnection conn, CancellationToken ct);
    Task<bool> TestConnectionAsync(Guid connectionId, CancellationToken ct);
    Task<List<string>> DiscoverTablesAsync(Guid connectionId, string? schema, CancellationToken ct);
    Task<List<ColumnInfo>> DiscoverColumnsAsync(Guid connectionId, string tableName, string? schema, CancellationToken ct);
    Task<DataImportJob> StartImportAsync(Guid connectionId, string entityType, string sourceQuery, Dictionary<string, string> fieldMappings, Guid? userId, CancellationToken ct);
    Task<DataImportJob> GetJobStatusAsync(Guid jobId, CancellationToken ct);
    Task<List<DataImportJob>> GetJobHistoryAsync(CancellationToken ct);
    Task<List<FieldMappingTemplate>> GetMappingTemplatesAsync(string? entityType, CancellationToken ct);
    Task<FieldMappingTemplate> SaveMappingTemplateAsync(FieldMappingTemplate template, CancellationToken ct);
}

public class ColumnInfo
{
    public string Name { get; set; } = "";
    public string DataType { get; set; } = "";
    public bool IsNullable { get; set; }
    public int? MaxLength { get; set; }
}

public class DiscoveryResultDto
{
    public List<string> Tables { get; set; } = new();
    public string? Schema { get; set; }
}

public class ColumnDiscoveryDto
{
    public List<ColumnInfo> Columns { get; set; } = new();
    public string TableName { get; set; } = "";
}

public class ImportResultDto
{
    public Guid JobId { get; set; }
    public string Status { get; set; } = "";
    public int TotalRows { get; set; }
    public int SuccessRows { get; set; }
    public int ErrorRows { get; set; }
    public int SkippedRows { get; set; }
    public List<string> RecentErrors { get; set; } = new();
}
