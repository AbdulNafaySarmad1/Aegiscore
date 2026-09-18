using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace AegisCore.Infrastructure.Database;

/// <summary>
/// Compares the live database with what the code expects: every migration applied, and every
/// table and column the EF model maps present in the catalogue. A migration that never reached a
/// deployed database (43 tables and 95 columns lived in the model snapshot alone for weeks) shows
/// up here as a list of names, not as a 500 on the first request that touches them.
/// </summary>
public sealed record SchemaVerification(
    IReadOnlyList<string> PendingMigrations,
    IReadOnlyList<string> MissingTables,
    IReadOnlyList<string> MissingColumns,
    IReadOnlyList<string> MistypedColumns,
    int ModelTables,
    int ModelColumns,
    string? Note)
{
    public bool IsCurrent => PendingMigrations.Count == 0 && MissingTables.Count == 0 && MissingColumns.Count == 0 && MistypedColumns.Count == 0;

    /// <summary>One paragraph an operator can act on.</summary>
    public string Describe()
    {
        if (IsCurrent) return $"Schema is current: {ModelTables} tables and {ModelColumns} columns present, no pending migrations.";
        var parts = new List<string>();
        if (PendingMigrations.Count > 0) parts.Add($"{PendingMigrations.Count} migration(s) not applied ({string.Join(", ", PendingMigrations.Take(5))}{(PendingMigrations.Count > 5 ? ", …" : "")})");
        if (MissingTables.Count > 0) parts.Add($"{MissingTables.Count} table(s) missing ({string.Join(", ", MissingTables.Take(5))}{(MissingTables.Count > 5 ? ", …" : "")})");
        if (MissingColumns.Count > 0) parts.Add($"{MissingColumns.Count} column(s) missing ({string.Join(", ", MissingColumns.Take(5))}{(MissingColumns.Count > 5 ? ", …" : "")})");
        if (MistypedColumns.Count > 0) parts.Add($"{MistypedColumns.Count} column(s) of the wrong type ({string.Join(", ", MistypedColumns.Take(5))}{(MistypedColumns.Count > 5 ? ", …" : "")})");
        return "Schema is not current: " + string.Join("; ", parts) + (PendingMigrations.Count > 0
            ? ". Apply the migrations (Database__AutoMigrate=true for one start, or `dotnet AegisCore.API.dll migrate`) before serving requests."
            : ". No migration is pending: the difference was made outside the migration history.");
    }
}

public static class SchemaVerifier
{
    public static async Task<SchemaVerification> VerifyAsync(DbContext db, CancellationToken ct = default)
    {
        var pending = (await db.Database.GetPendingMigrationsAsync(ct)).ToList();
        var model = db.Model.GetEntityTypes().Where(e => e.GetTableName() is not null && e.BaseType is null).ToList();
        var modelColumns = 0;
        foreach (var entity in model)
        {
            var identifier = StoreObjectIdentifier.Table(entity.GetTableName()!, entity.GetSchema());
            modelColumns += entity.GetProperties().Count(p => p.GetColumnName(identifier) is not null);
        }

        var live = await LiveColumnsAsync(db, ct);
        if (live is null)
            return new SchemaVerification(pending, [], [], [], model.Count, modelColumns, "Column-level verification is not available for this database provider; only pending migrations were checked.");

        var missingTables = new List<string>();
        var missingColumns = new List<string>();
        var mistyped = new List<string>();
        var postgres = (db.Database.ProviderName ?? string.Empty).Contains("Npgsql", StringComparison.OrdinalIgnoreCase);
        foreach (var entity in model)
        {
            var table = entity.GetTableName()!;
            var identifier = StoreObjectIdentifier.Table(table, entity.GetSchema());
            if (!live.TryGetValue(table, out var columns)) { missingTables.Add(table); continue; }
            foreach (var property in entity.GetProperties())
            {
                var column = property.GetColumnName(identifier);
                if (column is null) continue;
                if (!columns.TryGetValue(column, out var liveType)) { missingColumns.Add($"{table}.{column}"); continue; }
                // Types are compared on PostgreSQL only, where the catalogue spells them the way EF does.
                if (!postgres) continue;
                var expected = Normalize(property.GetColumnType(identifier));
                var actual = Normalize(liveType);
                if (expected is not null && actual is not null && expected != actual)
                    mistyped.Add($"{table}.{column} is {liveType}, the model expects {property.GetColumnType(identifier)}");
            }
        }
        missingTables.Sort(StringComparer.Ordinal);
        missingColumns.Sort(StringComparer.Ordinal);
        mistyped.Sort(StringComparer.Ordinal);
        return new SchemaVerification(pending, missingTables, missingColumns, mistyped, model.Count, modelColumns, null);
    }

    /// <summary>
    /// PostgreSQL spells one type several ways (timestamptz / timestamp with time zone, varchar(20) /
    /// character varying(20), numeric(18,2) / numeric); this reduces both sides to one spelling and
    /// drops the length and precision, which the catalogue reports differently from the model. The
    /// base type is what breaks a query (integer = character varying), so the base type is compared.
    /// </summary>
    private static string? Normalize(string? type)
    {
        if (string.IsNullOrWhiteSpace(type)) return null;
        var t = type.Trim().ToLowerInvariant();
        t = t.Replace("timestamp with time zone", "timestamptz").Replace("timestamp without time zone", "timestamp")
             .Replace("character varying", "varchar").Replace("double precision", "float8").Replace("boolean", "bool")
             .Replace("bigint", "int8").Replace("integer", "int4").Replace("smallint", "int2").Replace("real", "float4");
        var paren = t.IndexOf('(');
        if (paren > 0) t = t[..paren];
        if (t == "timestamptz" || t == "timestamp") return "timestamp";
        return t == "text" ? "varchar" : t;
    }

    /// <summary>Table → column → catalogue type, or null when the provider has no information_schema we read.</summary>
    private static async Task<Dictionary<string, Dictionary<string, string>>?> LiveColumnsAsync(DbContext db, CancellationToken ct)
    {
        var provider = db.Database.ProviderName ?? string.Empty;
        string sql;
        if (provider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
            sql = "select table_name, column_name, case when data_type = 'ARRAY' then udt_name else data_type end from information_schema.columns where table_schema = current_schema()";
        else if (provider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
            sql = "select table_name, column_name, data_type from information_schema.columns where table_schema = schema_name()";
        else if (provider.Contains("MySql", StringComparison.OrdinalIgnoreCase))
            sql = "select table_name, column_name, data_type from information_schema.columns where table_schema = database()";
        else if (provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            sql = "select m.name, p.name, p.type from sqlite_master m join pragma_table_info(m.name) p where m.type = 'table'";
        else return null;

        var connection = db.Database.GetDbConnection();
        var opened = false;
        if (connection.State != System.Data.ConnectionState.Open) { await connection.OpenAsync(ct); opened = true; }
        try
        {
            var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var table = reader.GetString(0);
                if (!result.TryGetValue(table, out var set)) result[table] = set = new Dictionary<string, string>(StringComparer.Ordinal);
                set[reader.GetString(1)] = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
            }
            return result;
        }
        finally { if (opened) await connection.CloseAsync(); }
    }
}
