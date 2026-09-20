using System.Security.Claims;
using System.Text.Json;
using AegisCore.Domain.Common;
using AegisCore.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AegisCore.Infrastructure.Security;

/// <summary>
/// Writes an <see cref="AuditLog"/> row for every insert, update or delete of a record in the
/// sensitive set below — who, what, when, which fields, old and new values — in the same
/// transaction as the change itself. One interceptor on the context rather than a call in each
/// handler, because a handler somebody forgets to instrument is exactly the write an auditor asks
/// about. (Compliance control AC-51; docs/compliance/04-nist-csf-gap-assessment.md.)
/// </summary>
public sealed class SensitiveChangeAuditInterceptor(IHttpContextAccessor? httpContextAccessor = null) : SaveChangesInterceptor
{
    /// <summary>Entity → module. Anything not listed here is not audited by this interceptor.</summary>
    public static readonly IReadOnlyDictionary<Type, string> AuditedEntities = new Dictionary<Type, string>
    {
        // identity and access
        [typeof(User)] = "identity", [typeof(UserRole)] = "identity", [typeof(RolePermission)] = "identity",
        [typeof(Role)] = "identity", [typeof(UserSecondFactor)] = "identity", [typeof(ApiKey)] = "identity",
        [typeof(IdentityProviderConfiguration)] = "identity",
        // academic record
        [typeof(CourseMark)] = "examination", [typeof(CourseGrade)] = "examination", [typeof(Result)] = "examination",
        [typeof(Transcript)] = "examination", [typeof(GradeAppeal)] = "examination",
        // student identity and welfare
        [typeof(Student)] = "sis", [typeof(StudentGuardian)] = "sis", [typeof(AdmissionApplication)] = "admissions",
        [typeof(StudentDiscipline)] = "welfare", [typeof(StudentCounseling)] = "welfare", [typeof(MentalHealthTicket)] = "welfare",
        [typeof(ForeignStudentRegistry)] = "sis", [typeof(StudentHealthRecord)] = "welfare",
        // staff and pay
        [typeof(Employee)] = "hr", [typeof(EmployeeContract)] = "hr", [typeof(PayrollRun)] = "payroll", [typeof(Payslip)] = "payroll",
        // privacy records
        [typeof(ConsentLedger)] = "privacy", [typeof(ErasureRequest)] = "privacy",
    };

    /// <summary>Values never written to the log, only the fact that they changed.</summary>
    private static readonly HashSet<string> RedactedProperties = new(StringComparer.Ordinal)
    {
        // secrets
        "PasswordHash", "SecretEncrypted", "BackupCodesHashCsv", "KeyHash", "EncryptedAccountReference", "IpAddressEncrypted",
        // free text about a person
        "ConsentText", "Notes", "ConcernDescription", "Description", "ReportContent",
        // identifiers: the log records that they changed, never the values, so an erasure that
        // anonymises a student does not leave the old name behind in the audit trail
        "FirstName", "LastName", "Name", "StudentName", "Email", "Phone", "Cnic", "DateOfBirth", "PassportNumber", "VisaNumber",
    };

    private static readonly HashSet<string> IgnoredProperties = new(StringComparer.Ordinal)
    {
        "UpdatedAt", "ModifiedAt", "LastLoginAt", "LastSeenAt", "RowVersion", "ConcurrencyStamp",
    };

    private const int DetailsLimit = 2000;

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Append(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Append(eventData.Context);
        return new ValueTask<InterceptionResult<int>>(result);
    }

    private void Append(DbContext? context)
    {
        if (context is not AegisDbContext db) return;

        var entries = db.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted
                        && AuditedEntities.ContainsKey(e.Metadata.ClrType))
            .ToList();
        if (entries.Count == 0) return;

        var http = httpContextAccessor?.HttpContext;
        var actor = Guid.TryParse(http?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsed) ? parsed : Guid.Empty;
        var actorEmail = http?.User.FindFirstValue(ClaimTypes.Email);
        var ip = http?.Connection.RemoteIpAddress?.ToString();
        var now = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            var changes = DescribeChanges(entry);
            if (entry.State == EntityState.Modified && changes.Count == 0) continue;

            var module = AuditedEntities[entry.Metadata.ClrType];
            var entityName = entry.Metadata.ClrType.Name;
            var verb = entry.State switch { EntityState.Added => "created", EntityState.Deleted => "deleted", _ => "updated" };
            var details = JsonSerializer.Serialize(new { actorEmail, changes });
            if (details.Length > DetailsLimit) details = details[..(DetailsLimit - 1)] + "…";

            db.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                TenantId = entry.Entity is ITenantScoped scoped && scoped.TenantId != Guid.Empty ? scoped.TenantId : db.CurrentTenantId,
                UserId = actor,
                Action = Truncate($"{module}.{entityName}.{verb}", 50),
                ResourceType = Truncate($"{module}:{entityName}", 100),
                ResourceId = PrimaryKey(entry),
                Details = details,
                Success = true,
                IpAddress = ip,
                Timestamp = now,
            });
        }
    }

    private static List<object> DescribeChanges(EntityEntry entry)
    {
        var changes = new List<object>();
        foreach (var property in entry.Properties)
        {
            var name = property.Metadata.Name;
            if (property.Metadata.IsPrimaryKey() || IgnoredProperties.Contains(name)) continue;
            if (entry.State == EntityState.Modified && !property.IsModified) continue;
            if (entry.State == EntityState.Modified && Equals(property.OriginalValue, property.CurrentValue)) continue;

            if (RedactedProperties.Contains(name))
            {
                changes.Add(new { name, redacted = true });
                continue;
            }

            changes.Add(entry.State switch
            {
                EntityState.Added => new { name, @new = Render(property.CurrentValue) },
                EntityState.Deleted => new { name, old = Render(property.OriginalValue) },
                _ => (object)new { name, old = Render(property.OriginalValue), @new = Render(property.CurrentValue) },
            });
        }
        return changes;
    }

    private static string? Render(object? value) => value switch
    {
        null => null,
        byte[] bytes => $"[{bytes.Length} bytes]",
        string s when s.Length > 120 => s[..117] + "...",
        _ => value.ToString(),
    };

    private static Guid PrimaryKey(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key is null || key.Properties.Count != 1) return Guid.Empty;
        return entry.Property(key.Properties[0].Name).CurrentValue is Guid id ? id : Guid.Empty;
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];
}
