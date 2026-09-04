using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

// GAP 09 — Campus Asset ERP

[Table("AssetCategories")] public class AssetCategory : ITenantScoped { [Key] public Guid Id { get; set; }
    [MaxLength(200)] public string Name { get; set; } = ""; [MaxLength(50)] public string? Code { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("Assets")] public class Asset : ITenantScoped { [Key] public Guid Id { get; set; }
    [MaxLength(200)] public string Name { get; set; } = ""; [MaxLength(100)] public string? SerialNumber { get; set; }
    public Guid AssetCategoryId { get; set; } public AssetStatus Status { get; set; }
    public Guid? LocationBuildingId { get; set; } public Guid? LocationRoomId { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal PurchaseCost { get; set; }
    public DateTime? PurchaseDate { get; set; } public DateTime? WarrantyExpiry { get; set; }
    public DepreciationMethod DepreciationMethod { get; set; } public int UsefulLifeYears { get; set; } = 5;
    [Column(TypeName = "decimal(18,2)")] public decimal CurrentValue { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; }
    [ForeignKey(nameof(AssetCategoryId))] public AssetCategory Category { get; set; } = null!; }

[Table("AssetTransfers")] public class AssetTransfer : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid AssetId { get; set; } public Guid FromLocationId { get; set; } public Guid ToLocationId { get; set; }
    public Guid TransferredByUserId { get; set; } public DateTime TransferredAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; } }

[Table("AssetMaintenances")] public class AssetMaintenance : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid AssetId { get; set; } public AssetMaintenanceStatus Status { get; set; }
    [MaxLength(500)] public string? Description { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal Cost { get; set; }
    public DateTime? ScheduledDate { get; set; } public DateTime? CompletedAt { get; set; } public Guid TenantId { get; set; } }

[Table("AssetDisposals")] public class AssetDisposal : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid AssetId { get; set; } public DisposalMethod Method { get; set; } public DisposalStatus Status { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal? SaleValue { get; set; }
    public DateTime DisposalDate { get; set; } public Guid TenantId { get; set; } }

[Table("AssetVerifications")] public class AssetVerification : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid AssetId { get; set; } public Guid VerificationCycleId { get; set; }
    public VerificationStatus Status { get; set; } public Guid VerifiedByUserId { get; set; }
    public DateTime? VerifiedAt { get; set; } public Guid TenantId { get; set; } }

[Table("VerificationCycles")] public class VerificationCycle : ITenantScoped { [Key] public Guid Id { get; set; }
    [MaxLength(200)] public string Name { get; set; } = ""; public VerificationCycleStatus Status { get; set; }
    public DateTime StartDate { get; set; } public DateTime EndDate { get; set; }
    public ICollection<AssetVerification> Verifications { get; set; } = []; public Guid TenantId { get; set; } }

// GAP 12 — Transportation Management

[Table("TransportRoutes")] public class TransportRoute : ITenantScoped { [Key] public Guid Id { get; set; }
    [MaxLength(200)] public string Name { get; set; } = ""; public RouteType RouteType { get; set; }
    [Column(TypeName = "decimal(18,4)")] public decimal DistanceKm { get; set; }
    public ICollection<RouteStop> Stops { get; set; } = []; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("RouteStops")] public class RouteStop : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid TransportRouteId { get; set; } [MaxLength(200)] public string StopName { get; set; } = "";
    public int Sequence { get; set; } [MaxLength(10)] public string? EstimatedTime { get; set; }
    [ForeignKey(nameof(TransportRouteId))] public TransportRoute Route { get; set; } = null!; public Guid TenantId { get; set; } }

[Table("TransportVehicles")] public class TransportVehicle : ITenantScoped { [Key] public Guid Id { get; set; }
    [MaxLength(100)] public string RegistrationNumber { get; set; } = ""; [MaxLength(100)] public string? Model { get; set; }
    public int Capacity { get; set; } public VehicleStatus Status { get; set; }
    public Guid? AssignedRouteId { get; set; } public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; }
    public ICollection<GPSLocation> GPSLocations { get; set; } = []; }

[Table("TransportDrivers")] public class TransportDriver : ITenantScoped { [Key] public Guid Id { get; set; }
    [MaxLength(200)] public string Name { get; set; } = ""; [MaxLength(50)] public string? LicenseNumber { get; set; }
    public DriverStatus Status { get; set; } public Guid? AssignedVehicleId { get; set; }
    public DateTime? LicenseExpiry { get; set; } public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; }
    public ICollection<DriverShift> Shifts { get; set; } = []; }

[Table("DriverShifts")] public class DriverShift : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid TransportDriverId { get; set; } public DateTime ShiftStart { get; set; } public DateTime ShiftEnd { get; set; }
    public Guid TenantId { get; set; }
    [ForeignKey(nameof(TransportDriverId))] public TransportDriver Driver { get; set; } = null!; }

[Table("DriverIncidents")] public class DriverIncident : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid TransportDriverId { get; set; } [MaxLength(1000)] public string? Description { get; set; }
    public DateTime IncidentDate { get; set; } public Guid TenantId { get; set; } }

[Table("BusAttendances")] public class BusAttendance : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; } public Guid TransportRouteId { get; set; }
    public DateTime AttendanceDate { get; set; } public bool IsPresent { get; set; } public Guid TenantId { get; set; } }

[Table("FuelLogs")] public class FuelLog : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid TransportVehicleId { get; set; } [Column(TypeName = "decimal(10,2)")] public decimal Litres { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal Cost { get; set; } public DateTime FuelDate { get; set; }
    public Guid TenantId { get; set; } }

[Table("GPSLocations")] public class GPSLocation : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid TransportVehicleId { get; set; } [Column(TypeName = "decimal(12,8)")] public decimal Latitude { get; set; }
    [Column(TypeName = "decimal(12,8)")] public decimal Longitude { get; set; } public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    [ForeignKey(nameof(TransportVehicleId))] public TransportVehicle TransportVehicle { get; set; } = null!; public Guid TenantId { get; set; } }

[Table("VehicleMaintenances")] public class VehicleMaintenance : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid TransportVehicleId { get; set; } public VehicleMaintenanceStatus Status { get; set; }
    [MaxLength(500)] public string? Description { get; set; } [Column(TypeName = "decimal(18,2)")] public decimal Cost { get; set; }
    public DateTime ScheduledDate { get; set; } public DateTime? CompletedAt { get; set; } public Guid TenantId { get; set; } }

// GAP 13 — Hostel ERP

[Table("HostelBlocks")] public class HostelBlock : ITenantScoped { [Key] public Guid Id { get; set; }
    [MaxLength(200)] public string Name { get; set; } = ""; public HostelBlockType BlockType { get; set; }
    public int TotalFloors { get; set; } public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; }
    public ICollection<HostelFloor> Floors { get; set; } = []; }

[Table("HostelFloors")] public class HostelFloor : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid HostelBlockId { get; set; } public int FloorNumber { get; set; }
    [MaxLength(100)] public string? Name { get; set; } public Guid TenantId { get; set; }
    [ForeignKey(nameof(HostelBlockId))] public HostelBlock Block { get; set; } = null!;
    public ICollection<HostelRoom> Rooms { get; set; } = []; }

[Table("HostelRooms")] public class HostelRoom : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid HostelFloorId { get; set; } [MaxLength(50)] public string RoomNumber { get; set; } = "";
    public RoomType RoomType { get; set; } public RoomStatus Status { get; set; } public int Capacity { get; set; }
    public Guid TenantId { get; set; }
    [ForeignKey(nameof(HostelFloorId))] public HostelFloor Floor { get; set; } = null!;
    public ICollection<HostelAllocation> Allocations { get; set; } = []; public ICollection<UtilityReading> UtilityReadings { get; set; } = []; }

[Table("HostelAllocations")] public class HostelAllocation : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid HostelRoomId { get; set; } public Guid StudentId { get; set; }
    public AllocationStatus Status { get; set; } public DateTime CheckInDate { get; set; } public DateTime? CheckOutDate { get; set; }
    public Guid TenantId { get; set; }
    [ForeignKey(nameof(HostelRoomId))] public HostelRoom Room { get; set; } = null!; }

[Table("RoomInspections")] public class RoomInspection : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid HostelRoomId { get; set; } public InspectionType InspectionType { get; set; }
    public DateTime InspectionDate { get; set; } [MaxLength(1000)] public string? Findings { get; set; }
    public Guid InspectedByUserId { get; set; } public Guid TenantId { get; set; }
    [ForeignKey(nameof(HostelRoomId))] public HostelRoom Room { get; set; } = null!;
    public ICollection<InspectionChecklistItem> ChecklistItems { get; set; } = []; }

[Table("InspectionChecklistItems")] public class InspectionChecklistItem : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid RoomInspectionId { get; set; } [MaxLength(500)] public string ItemDescription { get; set; } = "";
    public bool IsCompliant { get; set; } [MaxLength(500)] public string? Remarks { get; set; } public Guid TenantId { get; set; }
    [ForeignKey(nameof(RoomInspectionId))] public RoomInspection Inspection { get; set; } = null!; }

[Table("GateLogs")] public class GateLog : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; } public GateLogType Type { get; set; } public DateTime LogTime { get; set; }
    [MaxLength(500)] public string? Purpose { get; set; } public Guid TenantId { get; set; } }

[Table("VisitorLogs")] public class VisitorLog : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; } [MaxLength(200)] public string VisitorName { get; set; } = "";
    [MaxLength(50)] public string? VisitorCNIC { get; set; } public DateTime EntryTime { get; set; } public DateTime? ExitTime { get; set; }
    public Guid TenantId { get; set; } }

[Table("HostelDisciplines")] public class HostelDiscipline : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; } [MaxLength(1000)] public string? Description { get; set; }
    public HostelDisciplineStatus Status { get; set; } public DateTime IncidentDate { get; set; } public Guid TenantId { get; set; } }

[Table("UtilityReadings")] public class UtilityReading : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid HostelRoomId { get; set; } public UtilityType UtilityType { get; set; }
    [Column(TypeName = "decimal(12,4)")] public decimal Reading { get; set; } public DateTime ReadingDate { get; set; }
    public Guid TenantId { get; set; }
    [ForeignKey(nameof(HostelRoomId))] public HostelRoom HostelRoom { get; set; } = null!; }

[Table("MessMenus")] public class MessMenu : ITenantScoped { [Key] public Guid Id { get; set; }
    public MealType MealType { get; set; } [MaxLength(1000)] public string? Items { get; set; }
    public DateTime MenuDate { get; set; } public Guid TenantId { get; set; } }

[Table("MessAttendances")] public class MessAttendance : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; } public MealType MealType { get; set; } public DateTime AttendanceDate { get; set; }
    public Guid TenantId { get; set; } }

[Table("MessFeedbacks")] public class MessFeedback : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; } public int Rating { get; set; } [MaxLength(1000)] public string? Comments { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }

[Table("RoommatePreferences")] public class RoommatePreference : ITenantScoped { [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; } public Guid PreferredStudentId { get; set; }
    public bool IsMutual { get; set; } public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public Guid TenantId { get; set; } }
