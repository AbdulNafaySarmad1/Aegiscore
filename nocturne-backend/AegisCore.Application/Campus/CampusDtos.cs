namespace AegisCore.Application.Campus;

public class CampusDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CampusCode { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string Status { get; set; } = string.Empty;
    public string CalendarType { get; set; } = string.Empty;
    public string GradingScheme { get; set; } = string.Empty;
    public bool IsMain { get; set; }
    public int BuildingCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateCampusRequest
{
    public string Name { get; set; } = string.Empty;
    public string CampusCode { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string CalendarType { get; set; } = "Semester";
    public string GradingScheme { get; set; } = "GPA4";
    public bool IsMain { get; set; }
}

public class UpdateCampusRequest
{
    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? CalendarType { get; set; }
    public string? GradingScheme { get; set; }
}

public class TransferRequestDto
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string SourceCampusCode { get; set; } = string.Empty;
    public string DestCampusCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class InitiateTransferRequest
{
    public Guid StudentId { get; set; }
    public string SourceCampusCode { get; set; } = string.Empty;
    public string DestCampusCode { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public class ApproveTransferRequest
{
    public Guid TransferId { get; set; }
    public string ApproverName { get; set; } = string.Empty;
    public bool IsSourceApprover { get; set; }
}

public class UniversityReportDto
{
    public int TotalCampuses { get; set; }
    public int ActiveCampuses { get; set; }
    public long TotalStudents { get; set; }
    public long TotalFaculty { get; set; }
    public long TotalCourses { get; set; }
    public List<CampusSummaryDto> Campuses { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

public class CampusSummaryDto
{
    public string Name { get; set; } = string.Empty;
    public string CampusCode { get; set; } = string.Empty;
    public long StudentCount { get; set; }
    public long FacultyCount { get; set; }
    public long CourseCount { get; set; }
    public int BuildingCount { get; set; }
}

public class CampusComparisonDto
{
    public List<string> Metrics { get; set; } = new() { "Students", "Faculty", "Courses", "Buildings", "Revenue" };
    public List<CampusMetricDto> Data { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

public class CampusMetricDto
{
    public string CampusName { get; set; } = string.Empty;
    public string CampusCode { get; set; } = string.Empty;
    public long StudentCount { get; set; }
    public long FacultyCount { get; set; }
    public long CourseCount { get; set; }
    public int BuildingCount { get; set; }
    public decimal EstimatedRevenue { get; set; }
}
