namespace AegisCore.Application.DTOs.Requests;

/// <summary>
/// Request to submit a result for a student.
/// </summary>
public record SubmitResultRequest
{
    /// <summary>
    /// The enrollment identifier.
    /// </summary>
    public Guid EnrollmentId { get; init; }

    /// <summary>
    /// The course identifier.
    /// </summary>
    public Guid CourseId { get; init; }

    /// <summary>
    /// The total marks for the assessment.
    /// </summary>
    public decimal TotalMarks { get; init; }

    /// <summary>
    /// The marks obtained by the student.
    /// </summary>
    public decimal ObtainedMarks { get; init; }
}
