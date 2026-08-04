using System.Data.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace AegisCore.Infrastructure;

public sealed class TenantSessionInterceptor(IHttpContextAccessor httpContextAccessor, IConfiguration configuration) : DbConnectionInterceptor
{
    public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        var itemTenant = httpContextAccessor.HttpContext?.Items["TenantId"] as Guid?;
        var value = itemTenant is { } tenantFromContext && tenantFromContext != Guid.Empty
            ? tenantFromContext.ToString()
            : httpContextAccessor.HttpContext?.User.FindFirst("tenant_id")?.Value
            ?? httpContextAccessor.HttpContext?.User.FindFirst("tid")?.Value
            ?? configuration["TENANT_ID"];
        var tenant = Guid.TryParse(value, out var tenantId) && tenantId != Guid.Empty ? tenantId.ToString() : string.Empty;

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT set_config('app.tenant_id', @tenant_id, false)";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "tenant_id";
        parameter.Value = tenant;
        command.Parameters.Add(parameter);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
