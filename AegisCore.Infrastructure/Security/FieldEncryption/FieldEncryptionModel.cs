using System.Globalization;
using AegisCore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AegisCore.Infrastructure.Security.FieldEncryption;

/// <summary>
/// The list of columns encrypted at the application level and the EF Core wiring for them.
/// Every entry here is a row in docs/compliance/01-data-inventory.md whose "Enc. at rest" moved
/// from "disk" to "app". Adding a column: one line below plus an EF migration that widens the
/// column (ciphertext is longer than the value) — nothing else changes, queries included.
/// </summary>
public static class FieldEncryptionModel
{
    public sealed record EncryptedColumn(Type Entity, string Property, bool Deterministic, int MaxLength);

    private const int IdentifierLength = 400;   // a ≤ 100-char identifier as ciphertext, with headroom
    private const int TextLength = 8000;        // a ≤ 2000-char note as ciphertext

    public static readonly IReadOnlyList<EncryptedColumn> Columns =
    [
        // national identity numbers and travel documents — looked up by equality
        new(typeof(AdmissionApplication), nameof(AdmissionApplication.Cnic), Deterministic: true, IdentifierLength),
        new(typeof(VisitingFacultyProfile), nameof(VisitingFacultyProfile.Cnic), Deterministic: true, IdentifierLength),
        new(typeof(Visitor), nameof(Visitor.CNIC), Deterministic: true, IdentifierLength),
        new(typeof(VisitorLog), nameof(VisitorLog.VisitorCNIC), Deterministic: true, IdentifierLength),
        new(typeof(Vendor), nameof(Vendor.VendorCnic), Deterministic: true, IdentifierLength),
        new(typeof(ForeignStudentRegistry), nameof(ForeignStudentRegistry.PassportNumber), Deterministic: true, IdentifierLength),
        new(typeof(ForeignStudentRegistry), nameof(ForeignStudentRegistry.VisaNumber), Deterministic: true, IdentifierLength),
        // contact details of applicants and guardians
        new(typeof(AdmissionApplication), nameof(AdmissionApplication.Phone), Deterministic: false, IdentifierLength),
        new(typeof(ApplicantProfile), nameof(ApplicantProfile.Phone), Deterministic: false, IdentifierLength),
        new(typeof(ApplicantProfile), nameof(ApplicantProfile.GuardianPhone), Deterministic: false, IdentifierLength),
        new(typeof(ApplicantProfile), nameof(ApplicantProfile.GuardianEmail), Deterministic: false, IdentifierLength),
        new(typeof(StudentGuardian), nameof(StudentGuardian.Phone), Deterministic: false, IdentifierLength),
        new(typeof(StudentGuardian), nameof(StudentGuardian.Email), Deterministic: false, IdentifierLength),
        // welfare and conduct free text
        new(typeof(StudentCounseling), nameof(StudentCounseling.Notes), Deterministic: false, TextLength),
        new(typeof(StudentDiscipline), nameof(StudentDiscipline.Description), Deterministic: false, TextLength),
        new(typeof(MentalHealthTicket), nameof(MentalHealthTicket.ConcernDescription), Deterministic: false, TextLength),
        new(typeof(MandatoryReport), nameof(MandatoryReport.ReportContent), Deterministic: false, TextLength),
        // where a person signed in from — the IP is looked up by equality for anomaly detection
        new(typeof(LoginAuditRecord), nameof(LoginAuditRecord.IpAddress), Deterministic: true, IdentifierLength),
        new(typeof(LoginAuditRecord), nameof(LoginAuditRecord.IpCity), Deterministic: false, IdentifierLength),
        new(typeof(LoginAuditRecord), nameof(LoginAuditRecord.UserAgent), Deterministic: false, TextLength),
    ];

    /// <summary>GPS coordinates on login records: stored as encrypted text instead of doubles.</summary>
    public static readonly IReadOnlyList<(Type Entity, string Property)> EncryptedCoordinates =
    [
        (typeof(LoginAuditRecord), nameof(LoginAuditRecord.GpsLatitude)),
        (typeof(LoginAuditRecord), nameof(LoginAuditRecord.GpsLongitude)),
    ];

    public static void Apply(ModelBuilder modelBuilder)
    {
        foreach (var column in Columns)
        {
            var label = $"{column.Entity.Name}.{column.Property}";
            var converter = new ValueConverter<string?, string?>(
                v => FieldCipher.Protect(v, label, column.Deterministic),
                v => FieldCipher.Unprotect(v, label));
            modelBuilder.Entity(column.Entity).Property(column.Property)
                .HasConversion(converter)
                .HasMaxLength(column.MaxLength);
        }

        foreach (var (entity, property) in EncryptedCoordinates)
        {
            var label = $"{entity.Name}.{property}";
            var converter = new ValueConverter<double?, string?>(
                v => FieldCipher.Protect(v.HasValue ? v.Value.ToString("R", CultureInfo.InvariantCulture) : null, label, false),
                v => ParseCoordinate(FieldCipher.Unprotect(v, label)));
            modelBuilder.Entity(entity).Property(property)
                .HasConversion(converter)
                .HasMaxLength(IdentifierLength);
        }
    }

    private static double? ParseCoordinate(string? value) =>
        value is null ? null : double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
}
