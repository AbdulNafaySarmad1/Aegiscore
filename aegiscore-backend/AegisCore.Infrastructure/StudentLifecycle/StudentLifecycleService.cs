using AegisCore.Application.StudentLifecycle;

namespace AegisCore.Infrastructure.StudentLifecycle;

public class StudentLifecycleService : IStudentLifecycleService
{
    public bool IsPlacementEligible(decimal cgpa, int completedCredits, int requiredCredits, int failedCourses, decimal minCgpa = 2.5m)
    {
        if (requiredCredits <= 0 || completedCredits < 0 || failedCourses < 0 || minCgpa < 0) return false;
        var completionPercent = completedCredits * 100m / requiredCredits;
        return cgpa >= minCgpa && completionPercent >= 80m && failedCourses == 0;
    }

    public string CreateReceiptNumber(DateTime donationDate, Guid donationId)
        => $"DON-{donationDate:yyyyMMdd}-{donationId.ToString("N")[..8].ToUpperInvariant()}";

    public int CalculateActivityPoints(string role, bool completed)
    {
        if (!completed) return 0;
        return (role ?? string.Empty).Trim().ToLowerInvariant() switch { "organizer" => 10, "sponsor" => 8, "president" => 12, _ => 5 };
    }
}
