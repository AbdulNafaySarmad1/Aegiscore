using AegisCore.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql;

namespace AegisCore.Tests;

/// <summary>
/// A container that ships to a client starts against an empty PostgreSQL database and runs every
/// migration from zero. These tests do exactly that on a throwaway database and then check the
/// result against the EF model table by table and column by column, so a table that lives in the
/// model snapshot but in no migration (43 of them, before 20260916132033_AddMissingModuleTables)
/// is caught here rather than by a 500 in production.
/// </summary>
public sealed class MigrationSchemaTests
{
    [PilotPostgresFact]
    public async Task Migrating_An_Empty_Database_Produces_Every_Table_And_Column_In_The_Model()
    {
        await using var database = await TestDatabase.CreateAsync(postgres: true);
        await using var db = database.CreateContext(Guid.NewGuid());
        await db.Database.MigrateAsync();
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.False(db.Database.HasPendingModelChanges(), "The EF model differs from the migration snapshot: add a migration.");

        var columns = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        await using (var connection = new NpgsqlConnection(db.Database.GetConnectionString()))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand("select table_name, column_name from information_schema.columns where table_schema = 'public'", connection);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var table = reader.GetString(0);
                if (!columns.TryGetValue(table, out var set)) columns[table] = set = new HashSet<string>(StringComparer.Ordinal);
                set.Add(reader.GetString(1));
            }
        }

        var missing = new List<string>();
        foreach (var entity in db.Model.GetEntityTypes())
        {
            var table = entity.GetTableName();
            if (table is null) continue; // owned/TPH children map onto their parent's table
            var identifier = StoreObjectIdentifier.Table(table, entity.GetSchema());
            if (!columns.TryGetValue(table, out var actual)) { missing.Add($"{table} (whole table)"); continue; }
            foreach (var property in entity.GetProperties())
            {
                var column = property.GetColumnName(identifier);
                if (column is not null && !actual.Contains(column)) missing.Add($"{table}.{column}");
            }
        }
        Assert.True(missing.Count == 0, "Model objects absent after migrating from zero:\n  " + string.Join("\n  ", missing));
    }
}
