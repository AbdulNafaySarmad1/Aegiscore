using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("FormDefinitions")]
public class FormDefinition
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    public FormStatus Status { get; set; } = FormStatus.Draft;

    [Required]
    [Column(TypeName = "jsonb")]
    public string FieldsJson { get; set; } = "[]";

    [Column(TypeName = "jsonb")]
    public string PagesJson { get; set; } = "[]";

    public bool AllowMultipleSubmissions { get; set; } = true;

    public bool AllowDraftSave { get; set; } = true;

    public DateTime? OpenAt { get; set; }

    public DateTime? CloseAt { get; set; }

    [MaxLength(500)]
    public string? AccessRoles { get; set; }

    public Guid? WorkflowDefinitionId { get; set; }

    public bool IsTemplate { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    public Guid CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Guid TenantId { get; set; }

    public ICollection<FormSubmission> Submissions { get; set; } = new List<FormSubmission>();
}
