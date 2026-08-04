using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("FinanceFleetCostPostings")]
public sealed class FinanceFleetCostPosting : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid TransportVehicleId { get; set; }
    public Guid CostCenterId { get; set; }
    [Required, MaxLength(20)] public string CostType { get; set; } = "FUEL";
    [Column(TypeName = "numeric(19,4)")] public decimal Amount { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal Litres { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal Odometer { get; set; }
    [Required, MaxLength(500)] public string Description { get; set; } = string.Empty;
    public DateTime CostDate { get; set; }
    public Guid JournalEntryId { get; set; }
    public Guid TenantId { get; set; }
}

[Table("FinanceFleetTrips")]
public sealed class FinanceFleetTrip : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid TransportVehicleId { get; set; }
    public Guid? CostCenterId { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal DistanceKm { get; set; }
    [Required, MaxLength(500)] public string Purpose { get; set; } = string.Empty;
    [MaxLength(160)] public string? FromLocation { get; set; }
    [MaxLength(160)] public string? ToLocation { get; set; }
    public DateTime TripDate { get; set; }
    public Guid TenantId { get; set; }
}

[Table("FinanceFleetProfiles")]
public sealed class FinanceFleetProfile : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid TransportVehicleId { get; set; }
    public Guid AssetId { get; set; }
    public Guid CostCenterId { get; set; }
    public Guid? CustodianUserId { get; set; }
    [Required, MaxLength(80)] public string Make { get; set; } = string.Empty;
    public int ModelYear { get; set; }
    [Required, MaxLength(20)] public string FuelType { get; set; } = string.Empty;
    public int ServiceIntervalDays { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal ServiceIntervalKm { get; set; }
    [Column(TypeName = "numeric(19,4)")] public decimal CurrentOdometer { get; set; }
    public DateTime? LastServiceAt { get; set; }
    public DateTime? NextServiceAt { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "IN_SERVICE";
    public Guid TenantId { get; set; }
}
