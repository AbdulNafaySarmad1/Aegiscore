using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("CampusConfigurations")]
public class CampusConfiguration : ITenantScoped
{
    [Key]
    public Guid Id { get; set; }

    public Guid CampusId { get; set; }

    [Column(TypeName = "jsonb")]
    public string AcademicCalendarJson { get; set; } = "{}";

    [Column(TypeName = "jsonb")]
    public string GradingPolicyJson { get; set; } = "{}";

    [Column(TypeName = "jsonb")]
    public string AdmissionRulesJson { get; set; } = "{}";

    [Column(TypeName = "jsonb")]
    public string FeeStructureJson { get; set; } = "{}";

    [Column(TypeName = "jsonb")]
    public string PoliciesJson { get; set; } = "{}";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Guid TenantId { get; set; }

    [ForeignKey(nameof(CampusId))]
    public Campus Campus { get; set; } = null!;
}
