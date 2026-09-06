using System.Data.Common;
using AegisCore.Infrastructure.Database;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace AegisCore.Infrastructure;

public sealed class TenantSessionInterceptor(
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration,
    IDbDialectAdapter dialectAdapter) : DbConnectionInterceptor
{
    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        var itemTenant = httpContextAccessor.HttpContext?.Items["TenantId"] as Guid?;
        var value = itemTenant is { } tenantFromContext && tenantFromContext != Guid.Empty
            ? tenantFromContext.ToString()
            : httpContextAccessor.HttpContext?.User.FindFirst("tenant_id")?.Value
                ?? httpContextAccessor.HttpContext?.User.FindFirst("tid")?.Value
                ?? httpContextAccessor.HttpContext?.User.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value
                ?? configuration["TENANT_ID"];

        var tenant = Guid.TryParse(value, out var tenantId) && tenantId != Guid.Empty
            ? tenantId.ToString("D")
            : string.Empty;

        await using var command = connection.CreateCommand();
        command.CommandText = dialectAdapter.GetTenantFilterSql(tenant);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
