namespace AegisCore.Application.DTOs.Requests;

/// <summary>
/// Request to enroll a student in a course.
/// </summary>
public record EnrollStudentRequest
{
    /// <summary>
    /// The student identifier.
    /// </summary>
    public Guid StudentId { get; init; }

    /// <summary>
    /// The course identifier.
    /// </summary>
    public Guid CourseId { get; init; }
}
