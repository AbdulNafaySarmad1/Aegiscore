using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("FleetVehicles")]
public class FleetVehicle : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(50)] public string VehicleNumber { get; set; } = string.Empty;
    [MaxLength(50)] public string VehicleType { get; set; } = "bus";
    public int SeatingCapacity { get; set; }
    [MaxLength(50)] public string? GpsDeviceId { get; set; }
    public DateTimeOffset RegistrationExpiry { get; set; }
    public DateTimeOffset? FitnessCertificateExpiry { get; set; }
    public DateTimeOffset? InsuranceExpiry { get; set; }
    [MaxLength(20)] public string? ConditionStatus { get; set; } = "operational";
    public bool IsGrounded { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("TransportRegistrations")]
public class TransportRegistration : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid RouteId { get; set; }
    public Guid RouteStopId { get; set; }
    public int? SeatNumber { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "active";
    [MaxLength(20)] public string PaymentStatus { get; set; } = "paid";
    public DateTimeOffset StartDate { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EndDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("BoardingLogs")]
public class BoardingLog : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid RouteId { get; set; }
    public Guid RouteStopId { get; set; }
    [MaxLength(20)] public string Direction { get; set; } = "pickup";
    public DateTimeOffset BoardedAt { get; set; } = DateTimeOffset.UtcNow;
    [MaxLength(20)] public string BoardingStatus { get; set; } = "boarded";
    public Guid TenantId { get; set; }
}

[Table("TransportIncidents")]
public class TransportIncident : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid? VehicleId { get; set; }
    public Guid? DriverId { get; set; }
    [MaxLength(50)] public string IncidentType { get; set; } = "accident";
    [MaxLength(2000)] public string? Description { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    [MaxLength(20)] public string Status { get; set; } = "under_investigation";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("TransportInspections")]
public class TransportInspection : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid VehicleId { get; set; }
    public Guid DriverId { get; set; }
    public bool BrakesOk { get; set; }
    public bool TiresOk { get; set; }
    public bool LightsOk { get; set; }
    public bool EmergencyExitsOk { get; set; }
    public bool FirstAidKitOk { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
    [MaxLength(20)] public string InspectionResult { get; set; } = "passed";
    public DateTimeOffset InspectedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid TenantId { get; set; }
}
