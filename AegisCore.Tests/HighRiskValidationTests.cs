using System.Reflection;
using System.Text.Json;
using AegisCore.API.Controllers;
using AegisCore.API.Validation;
using AegisCore.Application.Examination;
using AegisCore.Application.Finance;
using AegisCore.Application.Identity;
using AegisCore.Application.Onboarding;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using AegisCore.Infrastructure;
using AegisCore.Infrastructure.Onboarding;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AegisCore.Tests;

public sealed class HighRiskValidationTests
{
    public static IEnumerable<object[]> InvalidEndpointIndexes()
        => Enumerable.Range(0, HighRiskEndpointRegistry.Endpoints.Count).Select(index => new object[] { index });

    [Theory]
    [MemberData(nameof(InvalidEndpointIndexes))]
    public async Task Each_high_risk_endpoint_rejects_bad_input(int index)
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var services = BuildServices(connection);
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AegisDbContext>().Database.EnsureCreatedAsync();

        var endpoint = HighRiskEndpointRegistry.Endpoints[index];
        var validator = (IValidator)scope.ServiceProvider.GetRequiredService(
            typeof(IValidator<>).MakeGenericType(endpoint.RequestType));
        var request = InvalidRequest(index);
        var context = (IValidationContext)Activator.CreateInstance(
            typeof(ValidationContext<>).MakeGenericType(endpoint.RequestType), request)!;

        var result = await validator.ValidateAsync(context);

        Assert.False(result.IsValid, $"{endpoint.Method} {endpoint.Route} accepted its invalid test request.");
    }

    [Fact]
    public void Registry_has_exactly_30_real_FromBody_endpoints_with_discoverable_validators()
    {
        Assert.Equal(30, HighRiskEndpointRegistry.Endpoints.Count);
        Assert.Equal(30, HighRiskEndpointRegistry.Endpoints.Select(item => item.ValidatorType).Distinct().Count());
        Assert.Equal(30, HighRiskEndpointRegistry.Endpoints.Select(item => $"{item.Method} {item.Route}").Distinct().Count());

        var bodyTypes = typeof(AuthController).Assembly.GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type))
            .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public))
            .SelectMany(method => method.GetParameters())
            .Where(parameter => parameter.GetCustomAttribute<FromBodyAttribute>() is not null)
            .Select(parameter => parameter.ParameterType)
            .ToHashSet();

        foreach (var endpoint in HighRiskEndpointRegistry.Endpoints)
        {
            Assert.Contains(endpoint.RequestType, bodyTypes);
            var contract = typeof(IValidator<>).MakeGenericType(endpoint.RequestType);
            Assert.True(contract.IsAssignableFrom(endpoint.ValidatorType),
                $"{endpoint.ValidatorType.Name} does not validate {endpoint.RequestType.Name}.");
            Assert.NotNull(typeof(LoginRequestValidator).Assembly.GetType(endpoint.ValidatorType.FullName!));
        }
    }

    [Fact]
    public async Task Login_rejects_empty_email_and_weak_password()
    {
        var result = await new LoginRequestValidator().ValidateAsync(new AuthController.LoginRequest("", "short"));
        Assert.Contains(result.Errors, error => error.PropertyName == "Email");
        Assert.Contains(result.Errors, error => error.PropertyName == "Password");
    }

    [Fact]
    public async Task Attendance_rejects_a_future_session()
    {
        await using var fixture = await ValidatorFixture.CreateAsync();
        var request = new MarkAttendanceRequest(Guid.Empty, Guid.Empty, DateTime.UtcNow.AddDays(2), []);
        var result = await new MarkAttendanceRequestValidator(fixture.Db).ValidateAsync(request);
        Assert.Contains(result.Errors, error => error.PropertyName == "SessionDate");
    }

    [Fact]
    public async Task Marks_rejects_a_score_above_the_component_total()
    {
        await using var fixture = await ValidatorFixture.CreateAsync();
        var result = await new MarksEntryRequestValidator(fixture.Db).ValidateAsync(
            new MarksEntryRequest(Guid.Empty, Guid.Empty, Guid.Empty, 101));

        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("exceed", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("SELECT id, name FROM students WHERE tenant_id = 1", true)]
    [InlineData("WITH enrolled AS (SELECT id FROM students) SELECT id FROM enrolled", true)]
    [InlineData("SELECT * INTO stolen_students FROM students", false)]
    [InlineData("SELECT * FROM students; DELETE FROM students", false)]
    [InlineData("UPDATE students SET name = 'x'", false)]
    public void Import_query_parser_allows_only_one_read_only_query(string query, bool expected)
        => Assert.Equal(expected, DataImportController.IsReadOnlySelect(query));

    private static ServiceCollection BuildServices(SqliteConnection connection)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AegisDbContext>(options => options.UseSqlite(connection));
        services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>(ServiceLifetime.Scoped);
        return services;
    }

    private static object InvalidRequest(int index) => index switch
    {
        0 => new AuthController.LoginRequest("", "short"),
        1 => new AuthController.RefreshRequest(""),
        2 => new WizardDatabaseRequest("", 0, "", "", ""),
        3 => new SsoConfigDto(""),
        4 => new AiConfigDto("", ""),
        5 => new DocumentTemplateWriteRequest("", ""),
        6 => new SaveWizardPermissionDefinitionsRequest([]),
        7 => new ValidateLicenseRequest(""),
        8 => new ChangeWizardModeRequest(Guid.Empty, "", null),
        9 => new SaveStepRequest(Guid.Empty, default),
        10 => new DemoImportRequest("", ""),
        11 => new CompleteWizardRequest(Guid.Empty, "", DateTimeOffset.MinValue),
        12 => new CreateUserRequest(),
        13 => new CreateRoleRequest(),
        14 => new AssignRoleRequest(),
        15 => new AegisCore.API.Controllers.S3ExaminationController.AssessmentSchemeInput(Guid.Empty, Guid.Empty, Guid.Empty, []),
        16 => new MarksEntryRequest(Guid.Empty, Guid.Empty, Guid.Empty, -1),
        17 => new ModerationSubmitRequest(Guid.Empty, Guid.Empty, Guid.Empty),
        18 => new ModerationReviewRequest("", Guid.Empty, null),
        19 => new GraceMarksRequest([], Guid.Empty),
        20 => new PublishResultsRequest(Guid.Empty, Guid.Empty),
        21 => new TranscriptRequest(Guid.Empty, ""),
        22 => new MarkAttendanceRequest(Guid.Empty, Guid.Empty, DateTime.UtcNow.AddDays(2), []),
        23 => new FeeStructure(),
        24 => new FeeDueRequest(Guid.Empty, Guid.Empty, Guid.Empty),
        25 => new PaymentRequest("", -1m, null),
        26 => new ScholarshipRequest(-1m, Guid.Empty),
        27 => new ProcurementRequisition(),
        28 => new ExternalDbConnection { Name = "", DbType = "invalid", ConnectionString = "" },
        29 => new DataImportController.StartImportRequest(),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };

    private sealed class ValidatorFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        public AegisDbContext Db { get; }

        private ValidatorFixture(SqliteConnection connection, AegisDbContext db)
        {
            _connection = connection;
            Db = db;
        }

        public static async Task<ValidatorFixture> CreateAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();
            var db = new AegisDbContext(new DbContextOptionsBuilder<AegisDbContext>().UseSqlite(connection).Options);
            await db.Database.EnsureCreatedAsync();
            return new ValidatorFixture(connection, db);
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
