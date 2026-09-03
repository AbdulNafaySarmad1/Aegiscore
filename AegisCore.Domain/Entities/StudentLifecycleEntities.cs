using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("AlumniProfiles")]
public class AlumniProfile : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public int GraduationYear { get; set; }
    [MaxLength(50)] public string AlumniId { get; set; } = string.Empty;
    [MaxLength(200)] public string? CurrentCompany { get; set; }
    [MaxLength(100)] public string? CurrentDesignation { get; set; }
    [MaxLength(200)] public string? CurrentLocation { get; set; }
    [MaxLength(4000)] public string? Bio { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("AlumniDonations")]
public class AlumniDonation : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid AlumniProfileId { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal DonationAmount { get; set; }
    public DateTime DonationDate { get; set; }
    [MaxLength(50)] public string DonationType { get; set; } = "one_time";
    [MaxLength(200)] public string Purpose { get; set; } = "scholarship";
    [MaxLength(50)] public string ReceiptNumber { get; set; } = string.Empty;
    [MaxLength(50)] public string? PaymentMethod { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("StudentSocieties")]
public class StudentSociety : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [MaxLength(200)] public string SocietyName { get; set; } = string.Empty;
    [MaxLength(4000)] public string? Description { get; set; }
    public Guid FacultyAdvisorId { get; set; }
    public Guid? PresidentId { get; set; }
    public int? FoundedYear { get; set; }
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("SocietyMemberships")]
public class SocietyMembership : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid SocietyId { get; set; }
    public Guid StudentId { get; set; }
    [MaxLength(50)] public string Role { get; set; } = "member";
    public DateTime JoinedDate { get; set; } = DateTime.UtcNow.Date;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("StudentActivities")]
public class StudentActivity : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    [MaxLength(200)] public string ActivityName { get; set; } = string.Empty;
    [MaxLength(50)] public string ActivityType { get; set; } = "volunteer";
    public Guid? OrganizingSocietyId { get; set; }
    public DateTime ScheduledDate { get; set; }
    [MaxLength(200)] public string? Location { get; set; }
    [MaxLength(4000)] public string? Description { get; set; }
    public Guid OrganizerId { get; set; }
    [MaxLength(20)] public string ApprovalStatus { get; set; } = "pending";
    public Guid? ApprovedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("ActivityParticipations")]
public class ActivityParticipation : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid ActivityId { get; set; }
    public Guid StudentId { get; set; }
    [MaxLength(50)] public string ParticipationRole { get; set; } = "participant";
    [MaxLength(20)] public string ParticipationStatus { get; set; } = "registered";
    public int PointsEarned { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("StudentHealthRecords")]
public class StudentHealthRecord : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    [MaxLength(5)] public string? BloodType { get; set; }
    [MaxLength(4000)] public string? AllergiesEncrypted { get; set; }
    [MaxLength(4000)] public string? ChronicConditionsEncrypted { get; set; }
    [MaxLength(100)] public string? EmergencyContactName { get; set; }
    [MaxLength(20)] public string? EmergencyContactPhone { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("MedicalVisits")]
public class MedicalVisit : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public DateTime VisitDate { get; set; } = DateTime.UtcNow;
    [MaxLength(4000)] public string? VisitReasonEncrypted { get; set; }
    public Guid MedicalOfficerId { get; set; }
    [MaxLength(4000)] public string? DiagnosisEncrypted { get; set; }
    [MaxLength(4000)] public string? TreatmentEncrypted { get; set; }
    public int RestDays { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
}

[Table("SickLeaveApplications")]
public class SickLeaveApplication : ITenantScoped
{
    [Key] public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public Guid? MedicalVisitId { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "pending";
    public Guid? ApprovedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }
}
