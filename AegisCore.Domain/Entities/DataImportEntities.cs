using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AegisCore.Domain.Entities;

[Table("ExternalDbConnections")]
public class ExternalDbConnection
{
    [Key]
    public Guid Id { get; set; }

    [MaxLength(200)]
    public string Name { get; set; } = "";

    [MaxLength(50)]
    public string DbType { get; set; } = "PostgreSQL";

    [MaxLength(2000)]
    public string ConnectionString { get; set; } = "";

    [MaxLength(200)]
    public string? Schema { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? LastTestedAt { get; set; }

    public bool LastTestSuccessful { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid TenantId { get; set; }
}

[Table("DataImportJobs")]
public class DataImportJob
{
    [Key]
    public Guid Id { get; set; }

    public Guid ConnectionId { get; set; }

    [MaxLength(200)]
    public string EntityType { get; set; } = "";

    [MaxLength(50)]
    public string Status { get; set; } = "Pending";

    [Column(TypeName = "jsonb")]
    public string SourceQueryJson { get; set; } = "{}";

    [Column(TypeName = "jsonb")]
    public string FieldMappingsJson { get; set; } = "{}";

    public int TotalRows { get; set; }

    public int SuccessRows { get; set; }

    public int ErrorRows { get; set; }

    public int SkippedRows { get; set; }

    [Column(TypeName = "jsonb")]
    public string ErrorLogJson { get; set; } = "[]";

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    public Guid? TriggeredByUserId { get; set; }

    public Guid TenantId { get; set; }
}

[Table("FieldMappingTemplates")]
public class FieldMappingTemplate
{
    [Key]
    public Guid Id { get; set; }

    [MaxLength(200)]
    public string Name { get; set; } = "";

    [MaxLength(200)]
    public string EntityType { get; set; } = "";

    [MaxLength(50)]
    public string SourceDbType { get; set; } = "PostgreSQL";

    [Column(TypeName = "jsonb")]
    public string MappingJson { get; set; } = "{}";

    public bool IsBuiltIn { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid TenantId { get; set; }
}
