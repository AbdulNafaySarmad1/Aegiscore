namespace AegisCore.Application.Compliance;

public class ComplianceFrameworkDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int ControlCount { get; set; }
    public int CompliantCount { get; set; }
    public int NonCompliantCount { get; set; }
    public double ComplianceScore { get; set; }
}

public class ControlDto
{
    public Guid Id { get; set; }
    public Guid FrameworkId { get; set; }
    public string ControlCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? OwnerName { get; set; }
    public string ReviewFrequency { get; set; } = string.Empty;
    public DateTime? NextReviewDate { get; set; }
    public int EvidenceCount { get; set; }
    public int FindingCount { get; set; }
}

public class FindingDto
{
    public Guid Id { get; set; }
    public Guid ControlId { get; set; }
    public string ControlCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? IdentifiedByName { get; set; }
    public DateTime IdentifiedAt { get; set; }
    public DateTime? TargetResolutionDate { get; set; }
    public int ActionPlanCount { get; set; }
    public int CompletedCAPCount { get; set; }
}

public class CAPDto
{
    public Guid Id { get; set; }
    public Guid FindingId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? AssignedToName { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CompletionNotes { get; set; }
}

public class ComplianceDashboardDto
{
    public int TotalFrameworks { get; set; }
    public int TotalControls { get; set; }
    public int CompliantControls { get; set; }
    public int NonCompliantControls { get; set; }
    public int InProgressControls { get; set; }
    public int OpenFindings { get; set; }
    public int CriticalFindings { get; set; }
    public int OverdueCAPs { get; set; }
    public double OverallComplianceScore { get; set; }
    public List<FrameworkScoreDto> FrameworkScores { get; set; } = new();
    public List<FindingDto> RecentFindings { get; set; } = new();
}

public class FrameworkScoreDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int TotalControls { get; set; }
    public int CompliantControls { get; set; }
    public double Score { get; set; }
}

public class RegisterFrameworkRequest
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0";
}

public class AddControlRequest
{
    public Guid FrameworkId { get; set; }
    public string ControlCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid OwnerUserId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public string ReviewFrequency { get; set; } = "Annually";
}

public class UpdateControlStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class AttachEvidenceRequest
{
    public Guid ControlId { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class ReportFindingRequest
{
    public Guid ControlId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = "Observation";
}

public class CreateCAPRequest
{
    public Guid FindingId { get; set; }
    public string Description { get; set; } = string.Empty;
    public Guid AssigneeId { get; set; }
    public string AssigneeName { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
}

public class CompleteCAPRequest
{
    public string CompletionNotes { get; set; } = string.Empty;
}
