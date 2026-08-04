namespace AegisCore.Application.AI;

public class GradePredictionResult
{
    public Guid StudentId { get; set; }
    public Guid CourseId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string PredictedGrade { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public List<FactorContribution> ContributingFactors { get; set; } = new();
    public double CurrentScore { get; set; }
    public double RequiredFinalScore { get; set; }
}

public class FactorContribution
{
    public string FactorName { get; set; } = string.Empty;
    public double Weight { get; set; }
    public string Impact { get; set; } = string.Empty;
}
