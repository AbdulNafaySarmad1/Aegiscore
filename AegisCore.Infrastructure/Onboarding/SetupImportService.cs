using System.Text.Json;
using AegisCore.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AegisCore.Infrastructure.Onboarding;

public sealed record SetupImportResult(bool Success, int ImportedRows, IReadOnlyList<string> RejectedRows, IReadOnlyDictionary<string, int> Counts);

public sealed class SetupImportService
{
    private readonly AegisDbContext _db;
    private readonly IPasswordHashingService _passwords;

    public SetupImportService(AegisDbContext db, IPasswordHashingService passwords)
    {
        _db = db;
        _passwords = passwords;
    }

    public async Task<SetupImportResult> CommitAsync(Guid tenantId, string adminName, string adminEmail, string adminPassword, IReadOnlyDictionary<string, IReadOnlyList<string[]>> rows, CancellationToken ct)
    {
        if (tenantId == Guid.Empty || string.IsNullOrWhiteSpace(adminEmail) || adminPassword.Length < 12)
            return new(false, 0, ["A tenant, administrator email, and 12-character password are required."], new Dictionary<string, int>());

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        await _db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "SetupImportRows" ("Id" uuid PRIMARY KEY, "TenantId" uuid NOT NULL, "FileType" varchar(50) NOT NULL, "RowNumber" integer NOT NULL, "Payload" jsonb NOT NULL, "CreatedAt" timestamptz NOT NULL);
            CREATE TABLE IF NOT EXISTS "SetupAdministrators" ("Id" uuid PRIMARY KEY, "TenantId" uuid NOT NULL UNIQUE, "FullName" varchar(200) NOT NULL, "Email" varchar(300) NOT NULL, "PasswordHash" text NOT NULL, "EmailVerified" boolean NOT NULL, "CreatedAt" timestamptz NOT NULL);
            """, ct);

        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var imported = 0;
        foreach (var (fileType, sourceRows) in rows)
        {
            var count = 0;
            if (sourceRows.Count > 0)
            {
                var headers = sourceRows[0];
                for (var i = 1; i < sourceRows.Count; i++)
                {
                    var payload = headers.Select((header, index) => new { header, value = index < sourceRows[i].Length ? sourceRows[i][index] : string.Empty }).ToDictionary(x => x.header, x => x.value, StringComparer.OrdinalIgnoreCase);
                    await _db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"SetupImportRows\" (\"Id\", \"TenantId\", \"FileType\", \"RowNumber\", \"Payload\", \"CreatedAt\") VALUES ({Guid.NewGuid()}, {tenantId}, {fileType}, {i + 1}, {JsonSerializer.Serialize(payload)}, {DateTime.UtcNow})", ct);
                    imported++;
                    count++;
                }
            }
            counts[fileType] = count;
        }

        await _db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "SetupAdministrators" ("Id", "TenantId", "FullName", "Email", "PasswordHash", "EmailVerified", "CreatedAt")
            VALUES ({Guid.NewGuid()}, {tenantId}, {adminName}, {adminEmail.ToLowerInvariant()}, {_passwords.HashPassword(adminPassword)}, {false}, {DateTime.UtcNow})
            ON CONFLICT ("TenantId") DO UPDATE SET "FullName" = EXCLUDED."FullName", "Email" = EXCLUDED."Email", "PasswordHash" = EXCLUDED."PasswordHash", "EmailVerified" = false
            """, ct);

        await transaction.CommitAsync(ct);
        return new(true, imported, [], counts);
    }
}
