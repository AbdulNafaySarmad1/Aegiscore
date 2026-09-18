using AegisCore.Infrastructure.Database;
using AegisCore.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace AegisCore.Tests;

/// <summary>
/// A container that ships to a client starts against an empty PostgreSQL database and runs every
/// migration from zero. These tests do exactly that on a throwaway database and then check the
/// result against the EF model table by table and column by column, so a table that lives in the
/// model snapshot but in no migration (43 of them, before 20260916132605_AddMissingModuleTables)
/// is caught here rather than by a 500 in production.
/// </summary>
public sealed class MigrationSchemaTests
{
    [PilotPostgresFact]
    public async Task Migrating_An_Empty_Database_Produces_Every_Table_And_Column_In_The_Model()
    {
        await using var database = await TestDatabase.CreateAsync(postgres: true);
        await using var db = database.CreateContext(Guid.NewGuid());
        await database.MigrateAsync(db);
        Assert.False(db.Database.HasPendingModelChanges(), "The EF model differs from the migration snapshot: add a migration.");

        var verification = await SchemaVerifier.VerifyAsync(db);
        Assert.True(verification.IsCurrent, verification.Describe() + "\n  " + string.Join("\n  ", verification.MissingTables.Concat(verification.MissingColumns).Concat(verification.MistypedColumns)));
        Assert.True(verification.ModelTables > 600, $"Only {verification.ModelTables} tables in the model.");
        Assert.Null(verification.Note);
    }

    /// <summary>The verifier names what an un-migrated database lacks, so an operator reads names, not a 500.</summary>
    [PilotPostgresFact]
    public async Task Verifier_Names_The_Pending_Migrations_And_Missing_Objects_Of_A_Stale_Database()
    {
        await using var database = await TestDatabase.CreateAsync(postgres: true);
        await using var db = database.CreateContext(Guid.NewGuid());
        var migrator = db.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>();
        await migrator.MigrateAsync("20260908002929_AddAdmissionMigration"); // the last migration before the module tables were added
        var verification = await SchemaVerifier.VerifyAsync(db);
        Assert.False(verification.IsCurrent);
        Assert.Contains(verification.PendingMigrations, m => m.EndsWith("AddMissingModuleTables", StringComparison.Ordinal));
        Assert.Contains("AccreditationFrameworks", verification.MissingTables);
        Assert.Contains("CashFlowForecasts.WeeklyExpectedInflow", verification.MissingColumns);
        Assert.Contains("not current", verification.Describe());
    }

    /// <summary>The pilot database itself, after the migrations were applied, has no drift.</summary>
    [PilotPostgresFact]
    public async Task The_Pilot_Database_Has_Zero_Drift()
    {
        var connection = Environment.GetEnvironmentVariable("PILOT_POSTGRES_CONNECTION")!;
        var options = new DbContextOptionsBuilder<AegisCore.Infrastructure.AegisDbContext>().UseNpgsql(connection).Options;
        await using var db = new AegisCore.Infrastructure.AegisDbContext(options);
        var verification = await SchemaVerifier.VerifyAsync(db);
        Assert.True(verification.IsCurrent, verification.Describe());
    }
}
