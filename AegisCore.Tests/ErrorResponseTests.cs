using AegisCore.API.Middleware;
using AegisCore.Domain.Common;
using AegisCore.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Tests;

public sealed class ErrorResponseTests
{
    [Theory]
    [InlineData(typeof(AegisCore.Infrastructure.Licensing.LicenseReadOnlyException), 403, "license_read_only")]
    [InlineData(typeof(KeyNotFoundException), 404, "not_found")]
    [InlineData(typeof(ArgumentException), 400, "invalid_request")]
    [InlineData(typeof(NotSupportedException), 501, "not_available")]
    [InlineData(typeof(Microsoft.EntityFrameworkCore.Storage.RetryLimitExceededException), 503, "database_unavailable")]
    [InlineData(typeof(ArgumentOutOfRangeException), 400, "invalid_request")]
    [InlineData(typeof(FormatException), 400, "invalid_request")]
    [InlineData(typeof(DbUpdateConcurrencyException), 409, "concurrency_conflict")]
    [InlineData(typeof(DbUpdateException), 500, "database_write_failed")]
    [InlineData(typeof(Npgsql.NpgsqlException), 503, "database_unavailable")]
    [InlineData(typeof(StackExchange.Redis.RedisConnectionException), 503, "cache_unavailable")]
    [InlineData(typeof(System.Text.Json.JsonException), 500, "response_serialization_failed")]
    [InlineData(typeof(InvalidOperationException), 500, "internal_server_error")]
    [InlineData(typeof(NullReferenceException), 500, "internal_server_error")]
    public void Exceptions_Map_To_A_Status_A_Code_And_A_Sentence(Type exceptionType, int status, string code)
    {
        var exception = exceptionType == typeof(StackExchange.Redis.RedisConnectionException)
            ? new StackExchange.Redis.RedisConnectionException(StackExchange.Redis.ConnectionFailureType.UnableToConnect, "no redis")
            : exceptionType == typeof(Npgsql.NpgsqlException) ? new Npgsql.NpgsqlException("no database")
            : exceptionType == typeof(Microsoft.EntityFrameworkCore.Storage.RetryLimitExceededException) ? new Microsoft.EntityFrameworkCore.Storage.RetryLimitExceededException("retries exhausted", new Npgsql.NpgsqlException("no database"))
            : (Exception)Activator.CreateInstance(exceptionType, "boom")!;
        var (actualStatus, actualCode, message) = GlobalExceptionMiddleware.Describe(exception);
        Assert.Equal(status, actualStatus);
        Assert.Equal(code, actualCode);
        Assert.False(string.IsNullOrWhiteSpace(message));
        Assert.DoesNotContain("   at ", message); // never a stack trace
    }

    [Fact]
    public void Not_Found_And_Bad_Argument_Messages_Reach_The_Caller_Verbatim()
    {
        Assert.Equal("Campus not found.", GlobalExceptionMiddleware.Describe(new KeyNotFoundException("Campus not found.")).Message);
        Assert.Equal("Exam and student are required.", GlobalExceptionMiddleware.Describe(new ArgumentException("Exam and student are required.")).Message);
    }

    /// <summary>
    /// The tenant filter is applied to every ITenantScoped entity in the model, including the ones
    /// registered after the filter loop by explicit configuration.
    /// </summary>
    [Fact]
    public async Task Every_Tenant_Scoped_Entity_Has_The_Tenant_Query_Filter()
    {
        await using var database = await TestDatabase.CreateAsync(postgres: false);
        await using var db = database.CreateContext(Guid.NewGuid());
        var unfiltered = db.Model.GetEntityTypes()
            .Where(e => typeof(ITenantScoped).IsAssignableFrom(e.ClrType) && e.BaseType is null && e.GetQueryFilter() is null)
            .Select(e => e.ClrType.Name).OrderBy(n => n).ToList();
        Assert.True(unfiltered.Count == 0, "Tenant-scoped entities without a tenant filter: " + string.Join(", ", unfiltered));
    }
}
