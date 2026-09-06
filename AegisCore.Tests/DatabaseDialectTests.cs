using AegisCore.Application.Onboarding;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Database;
using AegisCore.Infrastructure.Deployment;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Tests;

public sealed class DatabaseDialectTests
{
    public static TheoryData<string, Type, string, string> AdapterCases => new()
    {
        { "postgres", typeof(PostgresDialectAdapter), "SELECT version()", "NpgsqlConnection" },
        { "mysql", typeof(MySqlDialectAdapter), "SELECT VERSION()", "MySqlConnection" },
        { "mssql", typeof(MsSqlDialectAdapter), "SELECT @@VERSION", "SqlConnection" },
        { "oracle", typeof(OracleDialectAdapter), "SELECT * FROM v$version", "OracleConnection" }
    };

    public static TheoryData<string, string, string, string> StoreTypeCases => new()
    {
        { "postgres", "jsonb", "bytea", "text" },
        { "mysql", "json", "longblob", "longtext" },
        { "mssql", "nvarchar(max)", "varbinary(max)", "nvarchar(max)" },
        { "oracle", "CLOB", "BLOB", "CLOB" }
    };

    [Theory]
    [MemberData(nameof(AdapterCases))]
    public void Resolver_ProvidesDialectSpecificConnectionAndVersionQuery(
        string dialect,
        Type adapterType,
        string versionQuery,
        string connectionType)
    {
        var adapter = DbDialectAdapterResolver.Create(dialect);
        using var connection = adapter.CreateConnection(ConnectionString(dialect));

        Assert.IsType(adapterType, adapter);
        Assert.Equal(versionQuery, adapter.GetVersionQuery());
        Assert.Equal(connectionType, connection.GetType().Name);
        var tenantSql = adapter.GetTenantFilterSql(Guid.NewGuid().ToString());
        Assert.Equal(dialect == "postgres", tenantSql.Contains("set_config", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(";", tenantSql, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(StoreTypeCases))]
    public void Model_UsesDialectSpecificJsonBinaryAndTextTypes(
        string dialect,
        string jsonType,
        string binaryType,
        string textType)
    {
        var adapter = DbDialectAdapterResolver.Create(dialect);
        var options = new DbContextOptionsBuilder<AegisDbContext>();
        adapter.ConfigureOptions(options, ConnectionString(dialect));

        using var context = new AegisDbContext(options.Options, dialectAdapter: adapter);
        var compliance = context.Model.FindEntityType(typeof(ComplianceControl))!;
        var attendance = context.Model.FindEntityType(typeof(Attendance))!;
        var template = context.Model.FindEntityType(typeof(DocumentTemplate))!;

        Assert.Equal(jsonType, compliance.FindProperty(nameof(ComplianceControl.AutomatedCheckConfigJson))!.GetColumnType());
        Assert.Equal(binaryType, attendance.FindProperty(nameof(Attendance.LatitudeEncrypted))!.GetColumnType());
        Assert.Equal(textType, template.FindProperty(nameof(DocumentTemplate.RawHtml))!.GetColumnType());

        if (dialect == DbDialects.MsSql)
            Assert.DoesNotContain(
                context.Model.GetEntityTypes().SelectMany(entity => entity.GetForeignKeys()),
                foreignKey => foreignKey.DeleteBehavior == DeleteBehavior.Cascade);

        if (dialect == DbDialects.Oracle)
        {
            var threshold = context.Model.FindEntityType(typeof(AttainmentThreshold))!
                .FindProperty(nameof(AttainmentThreshold.Threshold))!;
            Assert.Equal("decimal(18,2)", threshold.GetColumnType());
        }
    }

    [Theory]
    [InlineData("postgres", 5432, "Host=db;Port=5432;Database=aegis;Username=user;Password=secret;")]
    [InlineData("mysql", 3306, "Server=db;Port=3306;Database=aegis;User ID=user;Password=secret;")]
    [InlineData("mssql", 1433, "Server=db,1433;Database=aegis;User ID=user;Password=secret;")]
    [InlineData("oracle", 1521, "User Id=user;Password=secret;Data Source=db:1521/aegis")]
    public void WizardState_BuildsConnectionStringForEveryDialect(
        string dialect,
        int port,
        string expectedFragment)
    {
        var state = new WizardStateDto
        {
            DbDialect = dialect,
            DbHost = "db",
            DbPort = port,
            DbName = "aegis",
            DbUsername = "user",
            DbPassword = "secret",
            DbSslEnabled = false
        };

        Assert.Contains(expectedFragment, state.DbConnectionString, StringComparison.Ordinal);
    }

    [Fact]
    public void EnvironmentRegistry_DeclaresAllDatabaseDialects()
    {
        var definition = Assert.Single(EnvironmentValidator.Registry, item => item.Name == "DB_DIALECT");

        Assert.False(definition.IsRequired);
        Assert.NotNull(definition.AllowedValues);
        Assert.Equal<string>(
            new[] { "mssql", "mysql", "oracle", "postgres" },
            definition.AllowedValues!.Order(StringComparer.Ordinal).ToArray());
        Assert.Equal(DbDialects.Postgres, DbDialects.Normalize(null));
    }

    [Fact]
    public void MySqlConnectionFactory_AllowsPublicKeyRetrievalOnlyWhenTlsIsDisabled()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DB_HOST"] = "db",
                ["DB_NAME"] = "aegis",
                ["DB_USER"] = "user",
                ["DB_PASS"] = "secret",
                ["DB_SSL"] = "Disable"
            })
            .Build();

        var connectionString = DbConnectionStringFactory.Build(
            configuration,
            new MySqlDialectAdapter());

        Assert.Contains("AllowPublicKeyRetrieval=True", connectionString, StringComparison.OrdinalIgnoreCase);
    }


    [Theory]
    [InlineData("postgresql", "postgres")]
    [InlineData("mariadb", "mysql")]
    [InlineData("sqlserver", "mssql")]
    public void Resolver_NormalizesSupportedAliases(string value, string expected)
        => Assert.Equal(expected, DbDialectAdapterResolver.Create(value).Dialect);

    private static string ConnectionString(string dialect) => dialect switch
    {
        "postgres" => "Host=localhost;Database=aegis;Username=user;Password=secret;SSL Mode=Disable",
        "mysql" => "Server=localhost;Database=aegis;User ID=user;Password=secret;SslMode=Disabled",
        "mssql" => "Server=localhost;Database=aegis;User ID=user;Password=Secret_123!;Encrypt=False;TrustServerCertificate=True",
        "oracle" => "User Id=user;Password=secret;Data Source=localhost:1521/FREEPDB1",
        _ => throw new ArgumentOutOfRangeException(nameof(dialect))
    };
}
