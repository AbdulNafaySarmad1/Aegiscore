using AegisCore.Domain.Entities;

namespace AegisCore.Application.Governance;

public interface IGovernanceService
{
    int NormalizeReplicationLag(int? hours);
    string DetermineGradeApprovalThreshold(int originalMarks, int overrideMarks);
    decimal CalculateDynamicFee(int creditHours, decimal perCreditHour, decimal scholarshipDeduction, decimal overrideDeduction);
    bool IsSectionCapacityAllowed(int regularCount, int examCount, int regularCapacity, int examCapacity);
    Task<AnonymousFeedbackSubmission> SubmitAnonymousFeedbackAsync(AnonymousFeedbackRequest request, CancellationToken ct = default);
    Task<GradeOverrideRequest> RequestGradeOverrideAsync(GradeOverrideRequest request, CancellationToken ct = default);
}

public sealed record AnonymousFeedbackRequest(
    Guid UniversityId,
    Guid CourseId,
    Guid FacultyId,
    int Rating,
    decimal SentimentScore,
    string Comment,
    string[] Themes);
