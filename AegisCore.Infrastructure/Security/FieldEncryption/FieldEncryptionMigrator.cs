using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Security.FieldEncryption;

/// <summary>
/// Rewrites rows written before column encryption existed so that every value in an encrypted
/// column is ciphertext. Operator command: <c>dotnet AegisCore.API.dll encrypt-fields</c>.
/// Idempotent: a value that is already ciphertext is stored unchanged, so the command can be
/// re-run after an interruption. Runs in batches inside the licence guard like any other write.
/// </summary>
public static class FieldEncryptionMigrator
{
    private const int BatchSize = 500;

    public static async Task<int> RewriteAsync(AegisDbContext db, ILogger logger, CancellationToken ct = default)
    {
        if (!FieldCipher.IsConfigured)
            throw new InvalidOperationException("AEGISCORE_PQC_KEY_BASE64 is not configured; nothing can be encrypted.");

        var total = 0;
        var entities = FieldEncryptionModel.Columns.Select(c => c.Entity)
            .Concat(FieldEncryptionModel.EncryptedCoordinates.Select(c => c.Entity))
            .Distinct();
        foreach (var entity in entities)
        {
            var properties = FieldEncryptionModel.Columns.Where(c => c.Entity == entity).Select(c => c.Property)
                .Concat(FieldEncryptionModel.EncryptedCoordinates.Where(c => c.Entity == entity).Select(c => c.Property))
                .ToArray();
            var method = typeof(FieldEncryptionMigrator).GetMethod(nameof(RewriteEntityAsync), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(entity);
            var count = await (Task<int>)method.Invoke(null, [db, properties, ct])!;
            logger.LogInformation("encrypt-fields: {Entity} — {Count} rows rewritten ({Columns})", entity.Name, count, string.Join(", ", properties));
            total += count;
        }
        return total;
    }

    private static async Task<int> RewriteEntityAsync<TEntity>(AegisDbContext db, string[] properties, CancellationToken ct) where TEntity : class
    {
        var keyName = db.Model.FindEntityType(typeof(TEntity))!.FindPrimaryKey()!.Properties[0].Name;
        var rewritten = 0;
        var offset = 0;
        while (true)
        {
            var batch = await db.Set<TEntity>().IgnoreQueryFilters().AsTracking()
                .OrderBy(e => EF.Property<Guid>(e, keyName))
                .Skip(offset).Take(BatchSize)
                .ToListAsync(ct);
            if (batch.Count == 0) break;

            foreach (var row in batch)
            {
                var entry = db.Entry(row);
                foreach (var property in properties)
                {
                    // Marking the property modified makes EF write it back through the converter,
                    // which turns a legacy plaintext into ciphertext and leaves ciphertext alone.
                    if (entry.Property(property).CurrentValue is not null) entry.Property(property).IsModified = true;
                }
            }
            rewritten += await db.SaveChangesAsync(ct);
            db.ChangeTracker.Clear();
            offset += batch.Count;
        }
        return rewritten;
    }
}
