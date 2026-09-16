using System.Data;
using System.Data.Common;
using System.Text.Json;
using AegisCore.Application.DataImport;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace AegisCore.Infrastructure.DataImport;

public class DataImportService : IDataImportService
{
    private readonly AegisDbContext _db; private readonly ILogger<DataImportService> _logger;
    private static readonly object _templateLock = new();
    private static bool _templatesSeeded;

    public DataImportService(AegisDbContext db, ILogger<DataImportService> logger) { _db = db; _logger = logger; }

    public async Task<ExternalDbConnection> AddConnectionAsync(ExternalDbConnection conn, CancellationToken ct)
    {
        conn.Id = Guid.NewGuid(); conn.CreatedAt = DateTime.UtcNow;
        await _db.ExternalDbConnections.AddAsync(conn, ct);
        await _db.SaveChangesAsync(ct);
        return conn;
    }

    public async Task<bool> TestConnectionAsync(Guid connectionId, CancellationToken ct)
    {
        var conn = await _db.ExternalDbConnections.FindAsync([connectionId], ct) ?? throw new KeyNotFoundException("Connection not found.");
        conn.LastTestedAt = DateTime.UtcNow;
        try
        {
            using var dbConn = CreateDbConnection(conn);
            await dbConn.OpenAsync(ct);
            using var cmd = dbConn.CreateCommand(); cmd.CommandText = "SELECT 1";
            await cmd.ExecuteScalarAsync(ct);
            conn.LastTestSuccessful = true;
        }
        catch (Exception ex)
        {
            conn.LastTestSuccessful = false;
            _logger.LogWarning(ex, "Connection test failed for {Name} ({DbType})", conn.Name, conn.DbType);
        }
        await _db.SaveChangesAsync(ct);
        return conn.LastTestSuccessful;
    }

    public async Task<List<string>> DiscoverTablesAsync(Guid connectionId, string? schema, CancellationToken ct)
    {
        var conn = await _db.ExternalDbConnections.FindAsync([connectionId], ct) ?? throw new KeyNotFoundException("Connection not found.");
        if (conn.DbType != "PostgreSQL")
        {
            _logger.LogWarning("DiscoverTablesAsync: {DbType} discovery simulated; real column metadata unavailable", conn.DbType);
            return ["table1", "table2"];
        }
        using var dbConn = CreateDbConnection(conn); await dbConn.OpenAsync(ct);
        using var cmd = dbConn.CreateCommand();
        cmd.CommandText = "SELECT table_name FROM information_schema.tables WHERE table_schema = COALESCE(@schema, 'public') AND table_type = 'BASE TABLE' ORDER BY table_name";
        cmd.Parameters.Add(new NpgsqlParameter("schema", schema ?? conn.Schema ?? "public"));
        var tables = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) tables.Add(reader.GetString(0));
        return tables;
    }

    public async Task<List<ColumnInfo>> DiscoverColumnsAsync(Guid connectionId, string tableName, string? schema, CancellationToken ct)
    {
        var conn = await _db.ExternalDbConnections.FindAsync([connectionId], ct) ?? throw new KeyNotFoundException("Connection not found.");
        if (conn.DbType != "PostgreSQL")
        {
            _logger.LogWarning("DiscoverColumnsAsync: {DbType} column discovery simulated", conn.DbType);
            return [new ColumnInfo { Name = "id", DataType = "integer", IsNullable = false }, new ColumnInfo { Name = "name", DataType = "varchar", IsNullable = true, MaxLength = 255 }];
        }
        using var dbConn = CreateDbConnection(conn); await dbConn.OpenAsync(ct);
        using var cmd = dbConn.CreateCommand();
        cmd.CommandText = "SELECT column_name, data_type, is_nullable, character_maximum_length FROM information_schema.columns WHERE table_schema = COALESCE(@schema, 'public') AND table_name = @table ORDER BY ordinal_position";
        cmd.Parameters.Add(new NpgsqlParameter("schema", schema ?? conn.Schema ?? "public"));
        cmd.Parameters.Add(new NpgsqlParameter("table", tableName));
        var columns = new List<ColumnInfo>();
        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            columns.Add(new ColumnInfo
            {
                Name = reader.GetString(0),
                DataType = reader.GetString(1),
                IsNullable = reader.GetString(2) == "YES",
                MaxLength = reader.IsDBNull(3) ? null : reader.GetInt32(3)
            });
        return columns;
    }

    public async Task<DataImportJob> StartImportAsync(Guid connectionId, string entityType, string sourceQuery, Dictionary<string, string> fieldMappings, Guid? userId, CancellationToken ct)
    {
        var conn = await _db.ExternalDbConnections.FindAsync([connectionId], ct) ?? throw new KeyNotFoundException("Connection not found.");
        var job = new DataImportJob { Id = Guid.NewGuid(), ConnectionId = connectionId, EntityType = entityType, Status = "Running", SourceQueryJson = sourceQuery, FieldMappingsJson = JsonSerializer.Serialize(fieldMappings), TriggeredByUserId = userId, StartedAt = DateTime.UtcNow };
        await _db.DataImportJobs.AddAsync(job, ct); await _db.SaveChangesAsync(ct);

        var errors = new List<string>();
        try
        {
            if (conn.DbType != "PostgreSQL") { job.Status = "Failed"; job.ErrorLogJson = JsonSerializer.Serialize(new[] { $"Unsupported source DB type: {conn.DbType}. Currently only PostgreSQL sources are supported for import." }); await _db.SaveChangesAsync(ct); return job; }

            if (!IsQuerySafe(sourceQuery)) { job.Status = "Failed"; job.ErrorLogJson = JsonSerializer.Serialize(new[] { "Source query contains prohibited SQL keywords." }); await _db.SaveChangesAsync(ct); return job; }

            using var dbConn = CreateDbConnection(conn); await dbConn.OpenAsync(ct);
            using var cmd = dbConn.CreateCommand(); cmd.CommandText = sourceQuery; cmd.CommandTimeout = 30;
            using var reader = await cmd.ExecuteReaderAsync(ct);
            var rowIndex = 0; var batchRows = 0;
            while (await reader.ReadAsync(ct))
            {
                rowIndex++;
                try
                {
                    var entity = MapRowToEntity(reader, entityType, fieldMappings, rowIndex, errors);
                    if (entity != null) { _db.Add(entity); batchRows++; job.SuccessRows++; }
                    else job.SkippedRows++;
                }
                catch (Exception ex) { job.ErrorRows++; errors.Add($"Row {rowIndex}: {ex.Message}"); }
                job.TotalRows = rowIndex;
                if (batchRows >= 100) { await _db.SaveChangesAsync(ct); _db.ChangeTracker.Clear(); batchRows = 0; }
            }
            if (batchRows > 0) await _db.SaveChangesAsync(ct);
            job.Status = errors.Count > 0 ? "CompletedWithErrors" : "Completed";
        }
        catch (Exception ex) { job.Status = "Failed"; errors.Add($"Fatal: {ex.Message}"); _logger.LogError(ex, "Import job {JobId} failed", job.Id); }
        job.ErrorLogJson = JsonSerializer.Serialize(errors); job.CompletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return job;
    }

    public async Task<DataImportJob> GetJobStatusAsync(Guid jobId, CancellationToken ct)
    {
        var job = await _db.DataImportJobs.FindAsync([jobId], ct) ?? throw new KeyNotFoundException("Job not found.");
        return job;
    }

    public async Task<List<DataImportJob>> GetJobHistoryAsync(CancellationToken ct) => await _db.DataImportJobs.OrderByDescending(j => j.StartedAt).Take(50).ToListAsync(ct);

    public async Task<List<FieldMappingTemplate>> GetMappingTemplatesAsync(string? entityType, CancellationToken ct)
    {
        await SeedBuiltInTemplates(ct);
        var q = _db.FieldMappingTemplates.AsQueryable();
        if (!string.IsNullOrWhiteSpace(entityType)) q = q.Where(t => t.EntityType == entityType);
        return await q.OrderBy(t => t.IsBuiltIn).ThenBy(t => t.Name).ToListAsync(ct);
    }

    public async Task<FieldMappingTemplate> SaveMappingTemplateAsync(FieldMappingTemplate template, CancellationToken ct)
    {
        template.Id = Guid.NewGuid(); template.CreatedAt = DateTime.UtcNow; template.IsBuiltIn = false;
        await _db.FieldMappingTemplates.AddAsync(template, ct);
        await _db.SaveChangesAsync(ct);
        return template;
    }

    private static DbConnection CreateDbConnection(ExternalDbConnection conn) => conn.DbType switch
    {
        "PostgreSQL" => new NpgsqlConnection(conn.ConnectionString),
        _ => throw new NotSupportedException($"Database type '{conn.DbType}' is not supported for actual connections. Install the required NuGet package.")
    };

    private object? MapRowToEntity(DbDataReader reader, string entityType, Dictionary<string, string> mappings, int rowIndex, List<string> errors)
    {
        switch (entityType)
        {
            case "Student":
                var student = new Student { Id = Guid.NewGuid(), FirstName = null!, LastName = null! };
                foreach (var kvp in mappings)
                {
                    var ord = reader.GetOrdinal(kvp.Key);
                    var val = reader.IsDBNull(ord) ? null : reader.GetValue(ord);
                    SetProperty(student, kvp.Value, val, rowIndex, kvp.Key, errors);
                }
                return student;
            case "Faculty":
                var faculty = new Faculty { Id = Guid.NewGuid(), FirstName = null!, LastName = null! };
                foreach (var kvp in mappings)
                {
                    var ord = reader.GetOrdinal(kvp.Key);
                    var val = reader.IsDBNull(ord) ? null : reader.GetValue(ord);
                    SetProperty(faculty, kvp.Value, val, rowIndex, kvp.Key, errors);
                }
                return faculty;
            case "Course":
                var course = new Course { Id = Guid.NewGuid(), Title = null! };
                foreach (var kvp in mappings)
                {
                    var ord = reader.GetOrdinal(kvp.Key);
                    var val = reader.IsDBNull(ord) ? null : reader.GetValue(ord);
                    SetProperty(course, kvp.Value, val, rowIndex, kvp.Key, errors);
                }
                return course;
            default:
                _logger.LogWarning("Entity type '{EntityType}' is not yet supported for automatic import mapping. Row {Row}", entityType, rowIndex);
                return null;
        }
    }

    private static void SetProperty(object obj, string propertyName, object? value, int rowIndex, string sourceField, List<string> errors)
    {
        try
        {
            var prop = obj.GetType().GetProperty(propertyName);
            if (prop == null) { errors.Add($"Row {rowIndex}: Target property '{propertyName}' not found (source field: '{sourceField}')"); return; }
            if (value == null || value == DBNull.Value) return;
            var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            var converted = Convert.ChangeType(value, targetType);
            prop.SetValue(obj, converted);
        }
        catch (Exception ex) { errors.Add($"Row {rowIndex}: Failed mapping source field '{sourceField}' → '{propertyName}': {ex.Message}"); }
    }

    private async Task SeedBuiltInTemplates(CancellationToken ct)
    {
        if (_templatesSeeded) return;
        lock (_templateLock)
        {
            if (_templatesSeeded) return;
            try
            {
                var existing = _db.FieldMappingTemplates.Where(t => t.IsBuiltIn).Any();
                if (!existing)
                {
                    _db.FieldMappingTemplates.AddRange(
                        new FieldMappingTemplate { Id = Guid.NewGuid(), Name = "PeopleSoft Campus → AegisCore Students", EntityType = "Student", SourceDbType = "PostgreSQL", IsBuiltIn = true, MappingJson = JsonSerializer.Serialize(new { student_id = "StudentId", first_name = "FirstName", last_name = "LastName", email_address = "Email", date_of_birth = "DateOfBirth" }) },
                        new FieldMappingTemplate { Id = Guid.NewGuid(), Name = "Moodle → AegisCore Courses", EntityType = "Course", SourceDbType = "MySQL", IsBuiltIn = true, MappingJson = JsonSerializer.Serialize(new { fullname = "Name", shortname = "Code", summary = "Description", startdate = "StartDate" }) },
                        new FieldMappingTemplate { Id = Guid.NewGuid(), Name = "QuickBooks → AegisCore Finance", EntityType = "ChartOfAccount", SourceDbType = "MSSQL", IsBuiltIn = true, MappingJson = JsonSerializer.Serialize(new { AccountNumber = "Code", AccountName = "Name", AccountType = "Type", Balance = "OpeningBalance" }) },
                        new FieldMappingTemplate { Id = Guid.NewGuid(), Name = "Generic CSV → AegisCore Students", EntityType = "Student", SourceDbType = "PostgreSQL", IsBuiltIn = true, MappingJson = JsonSerializer.Serialize(new { full_name = "FullName", reg_number = "RegistrationNumber", email = "Email", phone = "PhoneNumber", department = "DepartmentName" }) }
                    );
                    _db.SaveChangesAsync(ct).GetAwaiter().GetResult();
                }
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to seed built-in field mapping templates"); }
            _templatesSeeded = true;
        }
    }

    private static bool IsQuerySafe(string query) => ReadOnlySqlQueryValidator.IsValid(query);
}
