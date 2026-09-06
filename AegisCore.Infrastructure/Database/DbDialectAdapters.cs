using System.Data.Common;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using Npgsql;
using Npgsql.NameTranslation;
using Oracle.ManagedDataAccess.Client;

namespace AegisCore.Infrastructure.Database;

public static class DbDialects
{
    public const string Postgres = "postgres";
    public const string MySql = "mysql";
    public const string MsSql = "mssql";
    public const string Oracle = "oracle";

    public static readonly IReadOnlySet<string> Supported =
        new HashSet<string>([Postgres, MySql, MsSql, Oracle], StringComparer.OrdinalIgnoreCase);

    public static string Normalize(string? value)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        return normalized switch
        {
            null or "" or "postgres" or "postgresql" => Postgres,
            "mysql" or "mariadb" => MySql,
            "mssql" or "sqlserver" or "sql-server" => MsSql,
            "oracle" => Oracle,
            _ => throw new InvalidOperationException(
                $"Unsupported DB_DIALECT '{value}'. Expected one of: {string.Join(", ", Supported.Order())}.")
        };
    }
}

public interface IDbDialectAdapter
{
    string Dialect { get; }
    string MigrationsAssembly { get; }
    IServiceCollection AddDbProvider(IServiceCollection services, string connectionString);
    void ConfigureOptions(DbContextOptionsBuilder options, string connectionString);
    void ConfigureEnumStrategy(ModelBuilder modelBuilder, Type enumType);
    void ConfigureJsonColumn(PropertyBuilder propertyBuilder, string columnName);
    void NormalizeProviderModel(ModelBuilder modelBuilder);
    string NormalizeSql(string sql);
    string GetTenantFilterSql(string tenantId);
    string GetVersionQuery();
    DbConnection CreateConnection(string connectionString);
}

public static class DbDialectAdapterResolver
{
    public static IDbDialectAdapter FromConfiguration(IConfiguration configuration)
        => Create(configuration["DB_DIALECT"] ?? configuration["Database:Dialect"]);

    public static IDbDialectAdapter Create(string? dialect) => DbDialects.Normalize(dialect) switch
    {
        DbDialects.Postgres => new PostgresDialectAdapter(),
        DbDialects.MySql => new MySqlDialectAdapter(),
        DbDialects.MsSql => new MsSqlDialectAdapter(),
        DbDialects.Oracle => new OracleDialectAdapter(),
        _ => throw new UnreachableException()
    };
}

public static class DbConnectionStringFactory
{
    public static string Build(IConfiguration configuration, IDbDialectAdapter adapter)
    {
        var explicitConnection = configuration.GetConnectionString("Default")
            ?? configuration["ConnectionStrings:Default"]
            ?? configuration["Database:ConnectionString"]
            ?? configuration["AEGISCORE_DB_CONNECTION"]
            ?? configuration["POSTGRES_CONNECTION_STRING"];
        if (!string.IsNullOrWhiteSpace(explicitConnection))
            return explicitConnection;

        var host = configuration["DB_HOST"] ?? configuration["POSTGRES_HOST"] ?? "localhost";
        var port = ReadPositiveInt(configuration["DB_PORT"] ?? configuration["POSTGRES_PORT"], DefaultPort(adapter.Dialect));
        var database = configuration["DB_NAME"] ?? configuration["POSTGRES_DB"] ?? "aegiscore";
        var user = configuration["DB_USER"] ?? configuration["POSTGRES_USER"] ?? "aegiscore";
        var password = configuration["DB_PASS"] ?? configuration["POSTGRES_PASSWORD"] ?? "";
        var ssl = !string.Equals(configuration["DB_SSL"], "Disable", StringComparison.OrdinalIgnoreCase);
        var pool = configuration.GetValue<int?>("DB_POOL_MAX") ?? 200;
        var timeout = configuration.GetValue<int?>("DB_COMMAND_TIMEOUT") ?? 30;

        return adapter.Dialect switch
        {
            DbDialects.Postgres => new NpgsqlConnectionStringBuilder
            {
                Host = host,
                Port = port,
                Database = database,
                Username = user,
                Password = password,
                SslMode = ssl ? SslMode.Require : SslMode.Disable,
                MaxPoolSize = pool,
                CommandTimeout = timeout,
                Pooling = true,
                ConnectionIdleLifetime = 300,
                ConnectionPruningInterval = 10
            }.ConnectionString,
            DbDialects.MySql => new MySqlConnectionStringBuilder
            {
                Server = host,
                Port = checked((uint)port),
                Database = database,
                UserID = user,
                Password = password,
                SslMode = ssl ? MySqlSslMode.Required : MySqlSslMode.Disabled,
                AllowPublicKeyRetrieval = !ssl,
                MaximumPoolSize = checked((uint)pool),
                DefaultCommandTimeout = checked((uint)timeout),
                Pooling = true
            }.ConnectionString,
            DbDialects.MsSql => new SqlConnectionStringBuilder
            {
                DataSource = $"{host},{port}",
                InitialCatalog = database,
                UserID = user,
                Password = password,
                Encrypt = ssl,
                TrustServerCertificate = !ssl,
                MaxPoolSize = pool,
                CommandTimeout = timeout,
                Pooling = true
            }.ConnectionString,
            DbDialects.Oracle => new OracleConnectionStringBuilder
            {
                DataSource = $"{host}:{port}/{database}",
                UserID = user,
                Password = password,
                MaxPoolSize = pool,
                ConnectionTimeout = timeout,
                Pooling = true
            }.ConnectionString,
            _ => throw new UnreachableException()
        };
    }

    public static int DefaultPort(string dialect) => dialect switch
    {
        DbDialects.Postgres => 5432,
        DbDialects.MySql => 3306,
        DbDialects.MsSql => 1433,
        DbDialects.Oracle => 1521,
        _ => throw new InvalidOperationException($"Unsupported database dialect '{dialect}'.")
    };

    private static int ReadPositiveInt(string? value, int fallback)
        => int.TryParse(value, out var parsed) && parsed > 0 ? parsed : fallback;
}

public abstract class DbDialectAdapterBase : IDbDialectAdapter
{
    public abstract string Dialect { get; }
    public abstract string MigrationsAssembly { get; }

    public IServiceCollection AddDbProvider(IServiceCollection services, string connectionString)
    {
        services.AddScoped<TenantSessionInterceptor>();
        services.AddDbContext<AegisDbContext>((provider, options) =>
        {
            ConfigureOptions(options, connectionString);
            if (Dialect == DbDialects.Postgres)
                options.AddInterceptors(provider.GetRequiredService<TenantSessionInterceptor>());
        });
        return services;
    }

    public abstract void ConfigureOptions(DbContextOptionsBuilder options, string connectionString);

    public virtual void ConfigureEnumStrategy(ModelBuilder modelBuilder, Type enumType)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.ClrType is null) continue;
            foreach (var property in entityType.GetProperties()
                         .Where(property => Nullable.GetUnderlyingType(property.ClrType) == enumType
                             || property.ClrType == enumType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property(property.Name)
                    .HasConversion(typeof(string));
            }
        }
    }

    public abstract void ConfigureJsonColumn(PropertyBuilder propertyBuilder, string columnName);
    public virtual void NormalizeProviderModel(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (Dialect == DbDialects.Oracle
                    && property.GetColumnType() is null
                    && (property.ClrType == typeof(decimal)
                        || Nullable.GetUnderlyingType(property.ClrType) == typeof(decimal)))
                {
                    var precision = property.GetPrecision() ?? 18;
                    var scale = property.GetScale() ?? 2;
                    property.SetColumnType($"decimal({precision},{scale})");
                }

                property.SetColumnType(NormalizeColumnType(property.GetColumnType()));

                var computedSql = property.GetComputedColumnSql();
                if (!string.IsNullOrWhiteSpace(computedSql))
                    property.SetComputedColumnSql(NormalizeSql(computedSql));
            }

            foreach (var index in entityType.GetIndexes())
            {
                var filter = index.GetFilter();
                if (!string.IsNullOrWhiteSpace(filter))
                    index.SetFilter(Dialect is DbDialects.MySql or DbDialects.Oracle ? null : NormalizeSql(filter));
            }

            // SQL Server rejects otherwise valid schemas when two cascading paths
            // can reach the same table. Application/EF relationship handling stays
            // portable while database-side cascades become explicit NO ACTION.
            if (Dialect == DbDialects.MsSql)
                foreach (var foreignKey in entityType.GetForeignKeys()
                             .Where(foreignKey => foreignKey.DeleteBehavior == DeleteBehavior.Cascade))
                    foreignKey.DeleteBehavior = DeleteBehavior.NoAction;
        }
    }

    public virtual string NormalizeSql(string sql) => Dialect switch
    {
        DbDialects.MySql => ReplaceDelimitedIdentifiers(sql, '`', '`'),
        DbDialects.MsSql => ReplaceDelimitedIdentifiers(sql, '[', ']'),
        _ => sql
    };

    public abstract string GetTenantFilterSql(string tenantId);
    public abstract string GetVersionQuery();
    public abstract DbConnection CreateConnection(string connectionString);

    protected static string ValidatedTenantId(string tenantId) =>
        string.IsNullOrWhiteSpace(tenantId)
            ? string.Empty
            : Guid.Parse(tenantId).ToString("D");

    private string? NormalizeColumnType(string? columnType)
    {
        if (string.IsNullOrWhiteSpace(columnType)) return columnType;

        // Oracle serializes inferred DECIMAL types with a space after the comma,
        // then removes it while materializing a migration snapshot. Canonicalize
        // both inferred and explicitly annotated numeric types to keep snapshots stable.
        if (Dialect == DbDialects.Oracle
            && (columnType.StartsWith("decimal(", StringComparison.OrdinalIgnoreCase)
                || columnType.StartsWith("numeric(", StringComparison.OrdinalIgnoreCase)))
            return columnType.Replace(" ", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
        return columnType.ToLowerInvariant() switch
        {
            "jsonb" => Dialect switch
            {
                DbDialects.Postgres => "jsonb",
                DbDialects.MySql => "json",
                DbDialects.MsSql => "nvarchar(max)",
                DbDialects.Oracle => "CLOB",
                _ => columnType
            },
            "bytea" => Dialect switch
            {
                DbDialects.Postgres => "bytea",
                DbDialects.MySql => "longblob",
                DbDialects.MsSql => "varbinary(max)",
                DbDialects.Oracle => "BLOB",
                _ => columnType
            },
            "text" => Dialect switch
            {
                DbDialects.Postgres => "text",
                DbDialects.MySql => "longtext",
                DbDialects.MsSql => "nvarchar(max)",
                DbDialects.Oracle => "CLOB",
                _ => columnType
            },
            _ => columnType
        };
    }

    private static string ReplaceDelimitedIdentifiers(string sql, char opening, char closing)
    {
        var builder = new System.Text.StringBuilder(sql.Length);
        var inIdentifier = false;
        foreach (var character in sql)
        {
            if (character == '"')
            {
                builder.Append(inIdentifier ? closing : opening);
                inIdentifier = !inIdentifier;
            }
            else
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

}
public sealed class PostgresDialectAdapter : DbDialectAdapterBase
{
    private static readonly INpgsqlNameTranslator NameTranslator = new NpgsqlSnakeCaseNameTranslator();

    public override string Dialect => DbDialects.Postgres;
    public override string MigrationsAssembly => typeof(AegisDbContext).Assembly.GetName().Name!;

    public override void ConfigureOptions(DbContextOptionsBuilder options, string connectionString)
        => options.UseNpgsql(connectionString, provider =>
        {
            provider.MigrationsAssembly(MigrationsAssembly);
            provider.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
        });

    public override void ConfigureEnumStrategy(ModelBuilder modelBuilder, Type enumType)
        => modelBuilder.HasPostgresEnum(
            schema: null,
            name: NameTranslator.TranslateTypeName(enumType.Name),
            labels: Enum.GetNames(enumType).Select(NameTranslator.TranslateMemberName).ToArray());

    public override void ConfigureJsonColumn(PropertyBuilder propertyBuilder, string columnName)
        => propertyBuilder.HasColumnName(columnName).HasColumnType("jsonb");

    public override string GetTenantFilterSql(string tenantId)
        => $"SELECT set_config('app.tenant_id', '{ValidatedTenantId(tenantId)}', false)";

    public override string GetVersionQuery() => "SELECT version()";

    public override DbConnection CreateConnection(string connectionString)
        => new NpgsqlConnection(connectionString);
}

public sealed class MySqlDialectAdapter : DbDialectAdapterBase
{
    public override string Dialect => DbDialects.MySql;
    public override string MigrationsAssembly => "AegisCore.Migrations.MySql";

    public override void ConfigureOptions(DbContextOptionsBuilder options, string connectionString)
        => options.UseMySQL(connectionString, provider => provider.MigrationsAssembly(MigrationsAssembly));

    public override void ConfigureJsonColumn(PropertyBuilder propertyBuilder, string columnName)
        => propertyBuilder.HasColumnName(columnName).HasColumnType("json");

    public override string GetTenantFilterSql(string tenantId)
        => $"SELECT '{ValidatedTenantId(tenantId)}'";

    public override string GetVersionQuery() => "SELECT VERSION()";

    public override DbConnection CreateConnection(string connectionString)
        => new MySqlConnection(connectionString);
}

public sealed class MsSqlDialectAdapter : DbDialectAdapterBase
{
    public override string Dialect => DbDialects.MsSql;
    public override string MigrationsAssembly => "AegisCore.Migrations.MsSql";

    public override void ConfigureOptions(DbContextOptionsBuilder options, string connectionString)
        => options.UseSqlServer(connectionString, provider =>
        {
            provider.MigrationsAssembly(MigrationsAssembly);
            provider.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
        });

    public override void ConfigureJsonColumn(PropertyBuilder propertyBuilder, string columnName)
        => propertyBuilder.HasColumnName(columnName).HasColumnType("nvarchar(max)");

    public override string GetTenantFilterSql(string tenantId)
        => $"SELECT '{ValidatedTenantId(tenantId)}'";

    public override string GetVersionQuery() => "SELECT @@VERSION";

    public override DbConnection CreateConnection(string connectionString)
        => new SqlConnection(connectionString);
}

public sealed class OracleDialectAdapter : DbDialectAdapterBase
{
    public override string Dialect => DbDialects.Oracle;
    public override string MigrationsAssembly => "AegisCore.Migrations.Oracle";

    public override void ConfigureOptions(DbContextOptionsBuilder options, string connectionString)
        => options.UseOracle(connectionString, provider => provider.MigrationsAssembly(MigrationsAssembly));

    public override void ConfigureJsonColumn(PropertyBuilder propertyBuilder, string columnName)
        => propertyBuilder.HasColumnName(columnName).HasColumnType("CLOB");

    public override string GetTenantFilterSql(string tenantId)
        => $"SELECT '{ValidatedTenantId(tenantId)}' FROM DUAL";

    public override string GetVersionQuery() => "SELECT * FROM v$version";

    public override DbConnection CreateConnection(string connectionString)
        => new OracleConnection(connectionString);
}
