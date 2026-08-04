namespace AegisCore.Application.DTOs.Requests;

/// <summary>
/// Request to mark attendance for a student.
/// </summary>
public record MarkAttendanceRequest
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
    /// The faculty identifier.
    /// </summary>
    public Guid FacultyId { get; init; }

    /// <summary>
    /// The date of attendance.
    /// </summary>
    public DateTime Date { get; init; }

    /// <summary>
    /// The attendance status.
    /// </summary>
    public Domain.AttendanceStatus Status { get; init; }
}
