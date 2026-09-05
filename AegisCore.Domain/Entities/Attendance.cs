using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("Attendances")]
public class Attendance : ITenantScoped
{
    public Guid TenantId { get; set; }
    [Key]
    public Guid Id { get; init; } = Guid.NewGuid();

    // FK
    public Guid EnrollmentId { get; init; }
    public Guid CourseId { get; init; }
    public Guid FacultyId { get; init; }

    public DateTime Date { get; init; }

    public AttendanceStatus Status { get; init; }

    [NotMapped]
    public decimal? Latitude { get; private set; }

    [NotMapped]
    public decimal? Longitude { get; private set; }

    [Column("Latitude")]
    public byte[]? LatitudeEncrypted { get; private set; }

    [Column("Longitude")]
    public byte[]? LongitudeEncrypted { get; private set; }

    public Guid? GeofenceId { get; set; }
    public LocationStatus LocationStatus { get; set; } = LocationStatus.Unverifiable;
    public bool IsManualOverride { get; private set; }
    [MaxLength(500)]
    public string? OverrideReason { get; private set; }

    // Calculated property (not mapped)
    [NotMapped]
    public double Percentage { get; set; } = 0.0;

    public void UpdatePercentage(int totalSessions, int presentSessions)
    {
        Percentage = totalSessions > 0 ? Math.Round((double)presentSessions / totalSessions * 100.0, 2) : 0.0;
    }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public void SetEncryptedLocation(decimal? latitude, decimal? longitude, byte[]? latitudeEncrypted, byte[]? longitudeEncrypted)
    {
        if (latitude is < -90 or > 90) throw new ArgumentOutOfRangeException(nameof(latitude));
        if (longitude is < -180 or > 180) throw new ArgumentOutOfRangeException(nameof(longitude));
        if ((latitude is null) != (longitude is null)) throw new ArgumentException("Latitude and longitude must both be supplied or both be null.");
        if ((latitudeEncrypted is null) != (longitudeEncrypted is null)) throw new ArgumentException("Encrypted latitude and longitude must both be supplied or both be null.");

        Latitude = latitude;
        Longitude = longitude;
        LatitudeEncrypted = latitudeEncrypted;
        LongitudeEncrypted = longitudeEncrypted;
    }

    public void ApplyManualOverride(string reason)
    {
        OverrideReason = string.IsNullOrWhiteSpace(reason) ? throw new ArgumentException("An override reason is required.", nameof(reason)) : reason.Trim();
        IsManualOverride = true;
    }


    // Navigation
    [ForeignKey(nameof(EnrollmentId))]
    public virtual Enrollment Enrollment { get; init; } = null!;

    [ForeignKey(nameof(CourseId))]
    public virtual Course Course { get; init; } = null!;

    [ForeignKey(nameof(FacultyId))]
    public virtual Faculty Faculty { get; init; } = null!;
    [ForeignKey(nameof(GeofenceId))]
    public virtual CampusPolygon? Geofence { get; set; }
}
