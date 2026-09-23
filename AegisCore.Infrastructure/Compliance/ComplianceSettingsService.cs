using System.Text.Json;
using AegisCore.Application.Compliance;
using AegisCore.Domain;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisCore.Infrastructure.Compliance;

/// <summary>
/// Reads and writes the <c>compliance.*</c> tenant settings. Values are stored as JSON in
/// <see cref="ConfigurationEntry"/>, the same rows the retention worker reads, so a change made on
/// the compliance screen takes effect on the next nightly run with no restart and no redeploy.
/// Every write is picked up by the sensitive-change audit like any other configuration change.
/// </summary>
public sealed class ComplianceSettingsService(AegisDbContext db, ILogger<ComplianceSettingsService> logger) : IComplianceSettingsService
{
    public async Task<ComplianceSettings> GetAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var stored = await LoadAsync(tenantId, cancellationToken);

        var contact = new DataProtectionContact(
            Text(stored, DataProtectionContact.NameKey),
            Text(stored, DataProtectionContact.RoleKey),
            Text(stored, DataProtectionContact.EmailKey),
            Text(stored, DataProtectionContact.PhoneKey),
            Text(stored, DataProtectionContact.PostalAddressKey),
            Text(stored, DataProtectionContact.DeputyNameKey),
            Text(stored, DataProtectionContact.DeputyEmailKey));

        var residency = new DataResidency(
            Text(stored, DataResidency.DeploymentModelKey),
            Text(stored, DataResidency.DatabaseLocationKey),
            Text(stored, DataResidency.FileStorageLocationKey),
            Text(stored, DataResidency.BackupLocationKey),
            Text(stored, DataResidency.ReplicaLocationKey),
            Flag(stored, DataResidency.ApprovedForProductionKey),
            Text(stored, DataResidency.NotesKey));

        var fallback = new RetentionSettings();
        var retention = new RetentionSettings(
            Number(stored, RetentionSettings.AuditYearsKey, fallback.AuditRetentionYears),
            Number(stored, RetentionSettings.WriteAuditYearsKey, fallback.WriteAuditRetentionYears),
            Number(stored, RetentionSettings.LoginAuditDaysKey, fallback.LoginAuditRetentionDays),
            Number(stored, RetentionSettings.VisitorDaysKey, fallback.VisitorRetentionDays),
            Number(stored, RetentionSettings.ApplicantYearsKey, fallback.ApplicantRetentionYears),
            Holds(stored),
            Number(stored, RetentionSettings.StudentRecordYearsKey, fallback.StudentRecordRetentionYears),
            Number(stored, RetentionSettings.WelfareYearsKey, fallback.WelfareRetentionYears),
            Number(stored, RetentionSettings.StaffYearsKey, fallback.StaffRetentionYears),
            Number(stored, RetentionSettings.FinancialYearsKey, fallback.FinancialRetentionYears),
            Number(stored, RetentionSettings.RecruitmentYearsKey, fallback.RecruitmentRetentionYears));

        return new ComplianceSettings(contact, residency, retention);
    }

    public async Task<ComplianceSettings> SaveAsync(Guid tenantId, ComplianceSettingsUpdate update, Guid actorId, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty) throw new InvalidOperationException("Tenant context is required to change compliance settings.");
        var writes = new Dictionary<string, string>(StringComparer.Ordinal);

        if (update.Contact is { } contact)
        {
            Put(writes, DataProtectionContact.NameKey, contact.Name);
            Put(writes, DataProtectionContact.RoleKey, contact.Role);
            Put(writes, DataProtectionContact.EmailKey, contact.Email);
            Put(writes, DataProtectionContact.PhoneKey, contact.Phone);
            Put(writes, DataProtectionContact.PostalAddressKey, contact.PostalAddress);
            Put(writes, DataProtectionContact.DeputyNameKey, contact.DeputyName);
            Put(writes, DataProtectionContact.DeputyEmailKey, contact.DeputyEmail);
        }

        if (update.Residency is { } residency)
        {
            Put(writes, DataResidency.DeploymentModelKey, residency.DeploymentModel);
            Put(writes, DataResidency.DatabaseLocationKey, residency.DatabaseLocation);
            Put(writes, DataResidency.FileStorageLocationKey, residency.FileStorageLocation);
            Put(writes, DataResidency.BackupLocationKey, residency.BackupLocation);
            Put(writes, DataResidency.ReplicaLocationKey, residency.ReplicaLocation);
            writes[DataResidency.ApprovedForProductionKey] = JsonSerializer.Serialize(residency.ApprovedForProduction);
            Put(writes, DataResidency.NotesKey, residency.Notes);
        }

        if (update.Retention is { } retention)
        {
            // A period of zero would mean "delete immediately", which is never what anybody means;
            // each is clamped to a sane band so a typo cannot erase a register overnight.
            PutNumber(writes, RetentionSettings.AuditYearsKey, retention.AuditRetentionYears, 1, 100);
            PutNumber(writes, RetentionSettings.WriteAuditYearsKey, retention.WriteAuditRetentionYears, 1, 100);
            PutNumber(writes, RetentionSettings.LoginAuditDaysKey, retention.LoginAuditRetentionDays, 30, 3650);
            PutNumber(writes, RetentionSettings.VisitorDaysKey, retention.VisitorRetentionDays, 7, 3650);
            PutNumber(writes, RetentionSettings.ApplicantYearsKey, retention.ApplicantRetentionYears, 1, 20);
            PutNumber(writes, RetentionSettings.StudentRecordYearsKey, retention.StudentRecordRetentionYears, 1, 100);
            PutNumber(writes, RetentionSettings.WelfareYearsKey, retention.WelfareRetentionYears, 1, 100);
            PutNumber(writes, RetentionSettings.StaffYearsKey, retention.StaffRetentionYears, 1, 100);
            PutNumber(writes, RetentionSettings.FinancialYearsKey, retention.FinancialRetentionYears, 1, 100);
            PutNumber(writes, RetentionSettings.RecruitmentYearsKey, retention.RecruitmentRetentionYears, 1, 20);
            if (retention.LegalHoldIds is not null)
                writes[RetentionSettings.LegalHoldsKey] = JsonSerializer.Serialize(retention.LegalHoldIds.Distinct());
        }

        if (writes.Count > 0)
        {
            var existing = await db.ConfigurationEntries.IgnoreQueryFilters()
                .Where(entry => entry.TenantId == tenantId && writes.Keys.Contains(entry.Key))
                .ToListAsync(cancellationToken);

            foreach (var (key, value) in writes)
            {
                var entry = existing.FirstOrDefault(item => item.Key == key);
                if (entry is null)
                {
                    db.ConfigurationEntries.Add(new ConfigurationEntry
                    {
                        Id = Guid.NewGuid(), TenantId = tenantId, Key = key, Domain = ConfigurationDomain.System,
                        ValueJson = value, Description = "Compliance setting (docs/compliance).", UpdatedAt = DateTime.UtcNow,
                    });
                }
                else if (!string.Equals(entry.ValueJson, value, StringComparison.Ordinal))
                {
                    entry.ValueJson = value;
                    entry.UpdatedAt = DateTime.UtcNow;
                }
            }
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Compliance settings changed by {ActorId} for tenant {TenantId}: {Keys}", actorId, tenantId, string.Join(", ", writes.Keys));
        }

        return await GetAsync(tenantId, cancellationToken);
    }

    public IReadOnlyList<ComplianceSettingGap> Gaps(ComplianceSettings settings)
    {
        var gaps = new List<ComplianceSettingGap>();

        if (string.IsNullOrWhiteSpace(settings.Contact.Name) || string.IsNullOrWhiteSpace(settings.Contact.Email))
            gaps.Add(new ComplianceSettingGap("Data protection contact",
                "Nobody is named for data-subject requests, breach notices or a regulator's enquiry. A privacy notice cannot be published without one.",
                "docs/compliance/05-governance.md §1"));

        if (string.IsNullOrWhiteSpace(settings.Residency.DeploymentModel) || string.IsNullOrWhiteSpace(settings.Residency.DatabaseLocation))
            gaps.Add(new ComplianceSettingGap("Data residency",
                "Where the database physically sits is unrecorded, so the cross-border transfer question in the draft PDPA and the data-sovereignty expectation of the National Data Governance Policy cannot be answered.",
                "docs/compliance/03-data-residency.md"));

        if (!settings.Residency.ApprovedForProduction && !string.IsNullOrWhiteSpace(settings.Residency.DeploymentModel))
            gaps.Add(new ComplianceSettingGap("Residency approval",
                "The recorded location has not been approved for production personal data by the institution.",
                "docs/compliance/03-data-residency.md"));

        return gaps;
    }

    private async Task<Dictionary<string, string>> LoadAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var keys = AllKeys;
        var rows = await db.ConfigurationEntries.IgnoreQueryFilters().AsNoTracking()
            .Where(entry => entry.TenantId == tenantId && keys.Contains(entry.Key))
            .Select(entry => new { entry.Key, entry.ValueJson })
            .ToListAsync(cancellationToken);
        return rows.GroupBy(row => row.Key).ToDictionary(group => group.Key, group => group.First().ValueJson, StringComparer.Ordinal);
    }

    private static readonly string[] AllKeys =
    [
        DataProtectionContact.NameKey, DataProtectionContact.RoleKey, DataProtectionContact.EmailKey,
        DataProtectionContact.PhoneKey, DataProtectionContact.PostalAddressKey,
        DataProtectionContact.DeputyNameKey, DataProtectionContact.DeputyEmailKey,
        DataResidency.DeploymentModelKey, DataResidency.DatabaseLocationKey, DataResidency.FileStorageLocationKey,
        DataResidency.BackupLocationKey, DataResidency.ReplicaLocationKey, DataResidency.ApprovedForProductionKey,
        DataResidency.NotesKey,
        .. RetentionSettings.Keys,
    ];

    private static void Put(IDictionary<string, string> writes, string key, string? value) =>
        writes[key] = JsonSerializer.Serialize(string.IsNullOrWhiteSpace(value) ? null : value.Trim());

    private static void PutNumber(IDictionary<string, string> writes, string key, int? value, int min, int max)
    {
        if (value is not { } number) return;
        writes[key] = JsonSerializer.Serialize(Math.Clamp(number, min, max));
    }

    /// <summary>Values may have been written as bare JSON or as a quoted string; read both.</summary>
    private static string? Text(IReadOnlyDictionary<string, string> stored, string key)
    {
        if (!stored.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw)) return null;
        try
        {
            using var document = JsonDocument.Parse(raw);
            var value = document.RootElement.ValueKind switch
            {
                JsonValueKind.String => document.RootElement.GetString(),
                JsonValueKind.Null => null,
                _ => document.RootElement.ToString(),
            };
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
        catch (JsonException) { return raw.Trim('"'); }
    }

    private static int Number(IReadOnlyDictionary<string, string> stored, string key, int fallback) =>
        int.TryParse(Text(stored, key), out var value) ? value : fallback;

    private static bool Flag(IReadOnlyDictionary<string, string> stored, string key) =>
        bool.TryParse(Text(stored, key), out var value) && value;

    private static IReadOnlySet<Guid> Holds(IReadOnlyDictionary<string, string> stored) =>
        (Text(stored, RetentionSettings.LegalHoldsKey) ?? string.Empty)
            .Split([',', ';', ' ', '[', ']', '"'], StringSplitOptions.RemoveEmptyEntries)
            .Select(text => Guid.TryParse(text, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToHashSet();
}
