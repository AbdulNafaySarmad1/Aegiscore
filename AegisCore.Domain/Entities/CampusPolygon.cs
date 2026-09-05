using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace AegisCore.Domain.Entities;

[Table("CampusPolygons")]
public sealed class CampusPolygon : ITenantScoped
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }

    [Required, MaxLength(160)]
    public string Name { get; set; } = string.Empty;

    [Column("Vertices", TypeName = "jsonb")]
    public string VerticesJson { get; private set; } = "[]";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public IReadOnlyList<CampusVertex> GetVertices() =>
        JsonSerializer.Deserialize<List<CampusVertex>>(VerticesJson, JsonOptions) ?? [];

    public void SetVertices(IEnumerable<CampusVertex> vertices)
    {
        var values = vertices.ToArray();
        if (values.Length < 3) throw new ArgumentException("A campus polygon requires at least three vertices.", nameof(vertices));
        if (values.Any(vertex => vertex.Lat is < -90 or > 90 || vertex.Lon is < -180 or > 180))
            throw new ArgumentOutOfRangeException(nameof(vertices), "Campus polygon coordinates are outside valid latitude/longitude bounds.");
        VerticesJson = JsonSerializer.Serialize(values, JsonOptions);
    }
}

public sealed record CampusVertex(decimal Lat, decimal Lon);
