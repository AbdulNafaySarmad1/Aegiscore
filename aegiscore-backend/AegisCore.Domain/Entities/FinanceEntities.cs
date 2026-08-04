using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("FeeStructures")]
public class FeeStructure
{
    [Key] public Guid Id { get; set; }
    public Guid AcademicYearId { get; set; }
    public Guid ProgramId { get; set; }
    public int Semester { get; set; }
    [Required, MaxLength(50)] public string FeeType { get; set; } = "open_merit";
    [Column(TypeName = "decimal(12,2)")] public decimal TotalFee { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<FeeComponent> Components { get; set; } = [];
}

[Table("FeeComponents")]
public class FeeComponent
{
    [Key] public Guid Id { get; set; }
    public Guid FeeStructureId { get; set; }
    [Required, MaxLength(100)] public string ComponentName { get; set; } = string.Empty;
    [Column(TypeName = "decimal(12,2)")] public decimal Amount { get; set; }
    public bool IsMandatory { get; set; } = true;
    public int OrderIndex { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public FeeStructure FeeStructure { get; set; } = null!;
}

[Table("StudentFeeDues")]
public class StudentFeeDue
{
    [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid SemesterId { get; set; }
    public Guid FeeStructureId { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal TotalDue { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal PaidAmount { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal ScholarshipDeduction { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal OutstandingAmount { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "pending";
    public Guid? WaivedBy { get; set; }
    public string? WaivedReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

[Table("StudentFeePayments")]
public class StudentFeePayment
{
    [Key] public Guid Id { get; set; }
    public Guid FeeDueId { get; set; }
    [Required, MaxLength(50)] public string PaymentMethod { get; set; } = string.Empty;
    [Column(TypeName = "decimal(12,2)")] public decimal Amount { get; set; }
    [MaxLength(100)] public string? ReferenceNumber { get; set; }
    public DateTimeOffset PaymentDate { get; set; } = DateTimeOffset.UtcNow;
    public Guid? VerifiedBy { get; set; }
    public DateTimeOffset? VerificationDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

[Table("CostCenters")]
public class CostCenter
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(20)] public string Code { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    [Column(TypeName = "decimal(15,2)")] public decimal BudgetAllocation { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

[Table("ProcurementRequisitions")]
public class ProcurementRequisition
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(50)] public string RequisitionNumber { get; set; } = string.Empty;
    public Guid RequestingDepartmentId { get; set; }
    public Guid RequestedBy { get; set; }
    public DateOnly RequisitionDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? RequiredByDate { get; set; }
    [Column(TypeName = "decimal(15,2)")] public decimal TotalAmount { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "draft";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<RequisitionLineItem> LineItems { get; set; } = [];
}

[Table("RequisitionLineItems")]
public class RequisitionLineItem
{
    [Key] public Guid Id { get; set; }
    public Guid RequisitionId { get; set; }
    [MaxLength(100)] public string? ItemCode { get; set; }
    [Required] public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal UnitPrice { get; set; }
    [Column(TypeName = "decimal(15,2)")] public decimal TotalPrice { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ProcurementRequisition Requisition { get; set; } = null!;
}

[Table("Vendors")]
public class Vendor
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(200)] public string VendorName { get; set; } = string.Empty;
    [MaxLength(15)] public string? VendorCnic { get; set; }
    [MaxLength(50)] public string? BankAccount { get; set; }
    [MaxLength(100)] public string? ContactPerson { get; set; }
    [MaxLength(200)] public string? Email { get; set; }
    [MaxLength(20)] public string? Phone { get; set; }
    public string? Address { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "active";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

[Table("PurchaseOrders")]
public class PurchaseOrder
{
    [Key] public Guid Id { get; set; }
    [Required, MaxLength(50)] public string PoNumber { get; set; } = string.Empty;
    public Guid RequisitionId { get; set; }
    public Guid VendorId { get; set; }
    public DateOnly PoDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? DeliveryDate { get; set; }
    [Column(TypeName = "decimal(15,2)")] public decimal TotalAmount { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "issued";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
