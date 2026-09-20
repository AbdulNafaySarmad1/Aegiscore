using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Database;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AegisCore.Tests.Infrastructure;

/// <summary>
/// A throwaway database for a test: SQLite in memory by default, or a freshly created PostgreSQL
/// database on the pilot server when <c>PILOT_POSTGRES_CONNECTION</c> is set. Tests that build
/// on this can run under both providers, which is the only way to catch a query that SQLite
/// translates and PostgreSQL rejects.
/// </summary>
public sealed class TestDatabase : IAsyncDisposable
{
    public const string PostgresVariable = "PILOT_POSTGRES_CONNECTION";
    public static bool PostgresConfigured => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(PostgresVariable));

    // xUnit runs test classes in parallel; concurrent CREATE DATABASE calls collide on template1
    // ("source database is being accessed by other users"). Creation and drop are serialised; the
    // tests themselves still run in parallel against their own databases.
    private static readonly SemaphoreSlim ServerGate = new(1, 1);

    private readonly SqliteConnection? _sqlite;
    private readonly string? _postgresConnection;
    private readonly string? _postgresDatabase;
    private readonly string? _postgresMaintenance;

    public IDbDialectAdapter Adapter { get; }
    public string Provider { get; }

    private TestDatabase(SqliteConnection sqlite)
    {
        _sqlite = sqlite;
        // No SQLite adapter exists; existing SQLite tests run the model under the Postgres adapter too.
        Adapter = new PostgresDialectAdapter();
        Provider = "sqlite";
    }

    private TestDatabase(string connection, string database, string maintenance)
    {
        _postgresConnection = connection;
        _postgresDatabase = database;
        _postgresMaintenance = maintenance;
        Adapter = new PostgresDialectAdapter();
        Provider = "postgres";
    }

    public static async Task<TestDatabase> CreateAsync(bool postgres)
    {
        if (!postgres)
        {
            var sqlite = new SqliteConnection("DataSource=:memory:");
            await sqlite.OpenAsync();
            return new TestDatabase(sqlite);
        }

        var maintenance = Environment.GetEnvironmentVariable(PostgresVariable)
            ?? throw new InvalidOperationException($"{PostgresVariable} is not set.");
        var database = $"aegiscore_test_{Guid.NewGuid():N}"[..40];
        await ServerGate.WaitAsync();
        try
        {
            await using var admin = new NpgsqlConnection(maintenance);
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{database}\"", admin);
            await create.ExecuteNonQueryAsync();
        }
        finally { ServerGate.Release(); }
        var builder = new NpgsqlConnectionStringBuilder(maintenance) { Database = database, Pooling = false };
        return new TestDatabase(builder.ConnectionString, database, maintenance);
    }

    public void Configure(DbContextOptionsBuilder options)
    {
        if (_sqlite is not null) options.UseSqlite(_sqlite);
        else Adapter.ConfigureOptions(options, _postgresConnection!);
    }

    /// <summary>A context bound to <paramref name="tenantId"/> so the global tenant filter applies as it does in production.</summary>
    public AegisDbContext CreateContext(Guid tenantId)
    {
        var http = new DefaultHttpContext();
        http.Items["TenantId"] = tenantId;
        return CreateContext(http);
    }

    /// <summary>A context bound to a caller-owned HttpContext, for tests that change tenant or user mid-way.</summary>
    public AegisDbContext CreateContext(HttpContext http)
    {
        var options = new DbContextOptionsBuilder<AegisDbContext>();
        Configure(options);
        // A plain property, not HttpContextAccessor: that one keeps the context in an AsyncLocal, which does
        // not flow out of an async helper, so a context created inside one silently lost its tenant.
        var accessor = new FixedHttpContextAccessor(http);
        // The production context carries this interceptor, so tests see the same audit rows a deployment writes.
        options.AddInterceptors(new AegisCore.Infrastructure.Security.SensitiveChangeAuditInterceptor(accessor));
        return new AegisDbContext(options.Options, accessor, Adapter);
    }

    // Building the 600-table schema takes thousands of locks; a dozen in parallel exhausts the
    // server's max_locks_per_transaction ("53200: out of shared memory"). One schema build at a time.
    private static readonly SemaphoreSlim SchemaGate = new(1, 1);

    public async Task EnsureSchemaAsync(Guid tenantId)
    {
        await using var db = CreateContext(tenantId);
        await SchemaGate.WaitAsync();
        try { await db.Database.EnsureCreatedAsync(); }
        finally { SchemaGate.Release(); }
    }

    /// <summary>Applies the migration chain (the production path) under the same schema gate.</summary>
    public async Task MigrateAsync(AegisDbContext db)
    {
        await SchemaGate.WaitAsync();
        try { await db.Database.MigrateAsync(); }
        finally { SchemaGate.Release(); }
    }

    public async ValueTask DisposeAsync()
    {
        if (_sqlite is not null) { await _sqlite.DisposeAsync(); return; }
        NpgsqlConnection.ClearAllPools();
        await ServerGate.WaitAsync();
        try
        {
            await using var admin = new NpgsqlConnection(_postgresMaintenance);
            await admin.OpenAsync();
            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_postgresDatabase}\" WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
        finally { ServerGate.Release(); }
    }

    private sealed class FixedHttpContextAccessor(HttpContext context) : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; } = context;
    }
}

/// <summary>Runs the test on SQLite always, and additionally on PostgreSQL when the pilot server is configured.</summary>
public sealed class DatabaseProvidersAttribute : Xunit.Sdk.DataAttribute
{
    public override IEnumerable<object[]> GetData(System.Reflection.MethodInfo testMethod)
    {
        yield return new object[] { false };
        if (TestDatabase.PostgresConfigured) yield return new object[] { true };
    }
}
