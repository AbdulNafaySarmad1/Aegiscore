namespace AegisCore.Application.StudentLifecycle;

public interface IStudentLifecycleService
{
    bool IsPlacementEligible(decimal cgpa, int completedCredits, int requiredCredits, int failedCourses, decimal minCgpa = 2.5m);
    string CreateReceiptNumber(DateTime donationDate, Guid donationId);
    int CalculateActivityPoints(string role, bool completed);
}
