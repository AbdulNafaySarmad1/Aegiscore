using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AegisCore.Domain.Common;

namespace AegisCore.Domain.Entities;

[Table("FormSubmissions")]
public class FormSubmission : ITenantScoped
{
    [Key]
    public Guid Id { get; set; }

    public Guid FormId { get; set; }

    public Guid? SubmittedByUserId { get; set; }

    [MaxLength(200)]
    public string? SubmittedByName { get; set; }

    [MaxLength(300)]
    public string? SubmittedByEmail { get; set; }

    public SubmissionStatus Status { get; set; } = SubmissionStatus.Draft;

    [Required]
    [Column(TypeName = "jsonb")]
    public string ValuesJson { get; set; } = "{}";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? SubmittedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }

    [MaxLength(500)]
    public string? ReviewNotes { get; set; }

    public Guid TenantId { get; set; }

    [ForeignKey(nameof(FormId))]
    public FormDefinition Form { get; set; } = null!;
}
