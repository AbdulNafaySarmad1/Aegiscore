namespace AegisCore.Application.DegreeAudit;

public class DegreeProgramDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string DegreeType { get; set; } = string.Empty;
    public int DurationYears { get; set; }
    public int MinCreditHours { get; set; }
    public decimal MinCGPA { get; set; }
    public string CurriculumVersion { get; set; } = string.Empty;
    public int RequirementCount { get; set; }
}

public class RequirementDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string RequirementType { get; set; } = string.Empty;
    public int? MinCredits { get; set; }
    public int? CourseCount { get; set; }
    public int SortOrder { get; set; }
    public bool IsRequired { get; set; }
}

public class StudentProgressDto
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string ProgramName { get; set; } = string.Empty;
    public decimal CurrentCGPA { get; set; }
    public decimal RequiredCGPA { get; set; }
    public int CreditsEarned { get; set; }
    public int CreditsRequired { get; set; }
    public int CreditsEnrolled { get; set; }
    public int TotalRequirements { get; set; }
    public int SatisfiedRequirements { get; set; }
    public int InProgressRequirements { get; set; }
    public int RemainingRequirements { get; set; }
    public double CompletionPercentage { get; set; }
    public List<RequirementProgressDto> Requirements { get; set; } = new();
    public bool IsEligibleForGraduation { get; set; }
}

public class RequirementProgressDto
{
    public Guid RequirementId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal? EarnedValue { get; set; }
    public decimal? RequiredValue { get; set; }
    public string ProgressText { get; set; } = string.Empty;
}

public class WhatIfResultDto
{
    public List<Guid> ProposedCourses { get; set; } = new();
    public int AdditionalCreditsEarned { get; set; }
    public int RequirementsCleared { get; set; }
    public List<string> ClearedRequirements { get; set; } = new();
    public int RemainingRequirementsAfterAdd { get; set; }
    public int RemainingCreditsAfterAdd { get; set; }
    public double NewCompletionPercentage { get; set; }
}

public class CreditTransferDto
{
    public Guid Id { get; set; }
    public string ExternalCourseName { get; set; } = string.Empty;
    public string ExternalInstitution { get; set; } = string.Empty;
    public int CreditHours { get; set; }
    public string? InternalCourseCode { get; set; }
    public bool IsApproved { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateProgramRequest
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string DegreeType { get; set; } = "BS";
    public int DurationYears { get; set; } = 4;
    public int MinCreditHours { get; set; } = 120;
    public decimal MinCGPA { get; set; } = 2.0m;
    public string CurriculumVersion { get; set; } = "V2025";
    public Guid DepartmentId { get; set; }
}

public class AddRequirementRequest
{
    public Guid ProgramId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = "Core";
    public string RequirementType { get; set; } = "CourseCompletion";
    public Guid? CourseId { get; set; }
    public int? MinCredits { get; set; }
    public int? CourseCount { get; set; }
    public List<Guid>? AllowedCourseIds { get; set; }
    public int SortOrder { get; set; }
    public bool IsRequired { get; set; } = true;
}

public class WhatIfRequest
{
    public List<Guid> ProposedCourseIds { get; set; } = new();
}
