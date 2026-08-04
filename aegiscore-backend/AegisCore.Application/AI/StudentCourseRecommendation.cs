namespace AegisCore.Application.AI;

public class StudentCourseRecommendation
{
    public Guid StudentId { get; set; }
    public List<CourseSuggestion> Suggestions { get; set; } = new();
    public string Rationale { get; set; } = string.Empty;
}

public class CourseSuggestion
{
    public Guid CourseId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public double RelevanceScore { get; set; }
    public string Reason { get; set; } = string.Empty;
}
