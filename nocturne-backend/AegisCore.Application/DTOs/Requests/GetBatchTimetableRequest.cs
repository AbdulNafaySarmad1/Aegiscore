namespace AegisCore.Application.DTOs.Requests;

/// <summary>
/// Request to retrieve the timetable for a specific batch.
/// </summary>
public record GetBatchTimetableRequest
{
    /// <summary>
    /// The batch identifier.
    /// </summary>
    public Guid BatchId { get; init; }
}
