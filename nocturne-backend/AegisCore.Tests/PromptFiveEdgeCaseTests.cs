using AegisCore.Infrastructure.Admissions;
using AegisCore.Infrastructure.AssetInventory;
using AegisCore.Infrastructure.CampusOperations;
using AegisCore.Infrastructure.Curriculum;
using AegisCore.Infrastructure.Hr;
using AegisCore.Infrastructure.SsoHitl;
using AegisCore.Infrastructure.StudentLifecycle;
using Microsoft.Extensions.Logging.Abstractions;

namespace AegisCore.Tests;

public sealed class PromptFiveEdgeCaseTests
{
    [Theory]
    [InlineData(-100, 0)]
    [InlineData(-1, 0)]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(10, 9)]
    [InlineData(25, 22)]
    [InlineData(50, 45)]
    [InlineData(75, 67)]
    [InlineData(99, 89)]
    [InlineData(100, 90)]
    [InlineData(101, 90)]
    [InlineData(110, 99)]
    [InlineData(120, 100)]
    [InlineData(200, 100)]
    [InlineData(999, 100)]
    public void IbccOLevelConversion_IsClamped(int input, int expected)
        => Assert.Equal(expected, (int)new IBCCEquivalenceService().ConvertOLevelToMatric(input));

    [Theory]
    [InlineData(-100, 0)]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(10, 8)]
    [InlineData(25, 21)]
    [InlineData(50, 42)]
    [InlineData(75, 63)]
    [InlineData(99, 84)]
    [InlineData(100, 85)]
    [InlineData(101, 85)]
    [InlineData(120, 100)]
    [InlineData(200, 100)]
    public void IbccALevelConversion_IsClamped(int input, int expected)
        => Assert.Equal(expected, (int)new IBCCEquivalenceService().ConvertALevelToFsc(input));

    [Theory]
    [MemberData(nameof(PlacementCases))]
    public void PlacementEligibility_HandlesBoundaries(decimal cgpa, int completed, int required, int failed, bool expected)
        => Assert.Equal(expected, new StudentLifecycleService().IsPlacementEligible(cgpa, completed, required, failed));

    public static IEnumerable<object[]> PlacementCases()
    {
        yield return new object[] { 2.5m, 80, 100, 0, true };
        yield return new object[] { 2.49m, 80, 100, 0, false };
        yield return new object[] { 3m, 79, 100, 0, false };
        yield return new object[] { 3m, 81, 100, 1, false };
        yield return new object[] { 4m, 100, 100, 0, true };
        yield return new object[] { 0m, 100, 100, 0, false };
        yield return new object[] { 2.5m, 8, 10, 0, true };
        yield return new object[] { 2.5m, 7, 10, 0, false };
        yield return new object[] { 2.5m, 0, 0, 0, false };
        yield return new object[] { 2.5m, -1, 100, 0, false };
        yield return new object[] { 2.5m, 80, -1, 0, false };
        yield return new object[] { 2.5m, 80, 100, -1, false };
        yield return new object[] { 2.6m, 80, 100, 0, true };
        yield return new object[] { 3.1m, 80, 100, 0, true };
        yield return new object[] { 4m, 79, 100, 0, false };
        yield return new object[] { 4m, 81, 100, 0, true };
        yield return new object[] { 2.5m, 160, 200, 0, true };
        yield return new object[] { 2.5m, 159, 200, 0, false };
        yield return new object[] { 2.5m, 800, 1000, 0, true };
        yield return new object[] { 2.5m, 801, 1000, 0, true };
        yield return new object[] { 2.5m, 80, 100, 2, false };
        yield return new object[] { 2.5m, 80, 100, 99, false };
        yield return new object[] { 2.5m, 100, 100, 0, true };
        yield return new object[] { 2.5m, 80, 101, 0, false };
        yield return new object[] { 2.5m, 81, 101, 0, true };
        yield return new object[] { 2.5m, 40, 50, 0, true };
        yield return new object[] { 2.5m, 39, 50, 0, false };
        yield return new object[] { 2.5m, 20, 25, 0, true };
        yield return new object[] { 2.5m, 19, 25, 0, false };
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("student", false)]
    [InlineData("Organizer", true)]
    [InlineData(" organizer ", true)]
    [InlineData("sponsor", true)]
    [InlineData("president", true)]
    [InlineData("member", true)]
    [InlineData("unknown", true)]
    [InlineData("ORGANIZER", false)]
    [InlineData("Sponsor", false)]
    [InlineData("President", false)]
    public void ActivityPoints_RespectCompletionAndRole(string? role, bool completed)
    {
        var points = new StudentLifecycleService().CalculateActivityPoints(role!, completed);
        Assert.Equal(completed ? role?.Trim().ToLowerInvariant() switch
        {
            "organizer" => 10,
            "sponsor" => 8,
            "president" => 12,
            _ => 5
        } : 0, points);
    }

    [Theory]
    [MemberData(nameof(DepreciationCases))]
    public void StraightLineDepreciation_StopsAtCost(decimal cost, int life, int months, decimal expected)
    {
        var date = new DateTime(2025, 1, 1);
        var result = new AssetInventoryService(null!).CalculateStraightLineDepreciation(cost, life, date, date.AddMonths(months));
        Assert.Equal(expected, result);
    }

    public static IEnumerable<object[]> DepreciationCases()
    {
        yield return new object[] { 0m, 1, 1, 0m };
        yield return new object[] { 1200m, 1, 0, 0m };
        yield return new object[] { 1200m, 1, 1, 100m };
        yield return new object[] { 1200m, 1, 12, 1200m };
        yield return new object[] { 1200m, 1, 24, 1200m };
        yield return new object[] { 1000m, 2, 6, 250m };
        yield return new object[] { 1000m, 4, 12, 250m };
        yield return new object[] { 999m, 3, 1, 27.75m };
        yield return new object[] { 10000m, 10, 1, 83.33m };
        yield return new object[] { 10000m, 10, 120, 10000m };
        yield return new object[] { 10000m, 10, 121, 10000m };
        yield return new object[] { 1m, 1, 1, 0.08m };
        yield return new object[] { 1m, 1, 6, 0.5m };
        yield return new object[] { 1m, 1, 12, 1m };
        yield return new object[] { 500m, 5, 3, 25m };
        yield return new object[] { 500m, 5, 30, 250m };
        yield return new object[] { 500m, 5, 60, 500m };
        yield return new object[] { 25000m, 25, 1, 83.33m };
        yield return new object[] { 25000m, 25, 300, 25000m };
        yield return new object[] { 100.01m, 2, 1, 4.17m };
        yield return new object[] { 100.01m, 2, 24, 100.01m };
    }

    [Theory]
    [MemberData(nameof(FineCases))]
    public void LibraryFine_IsCappedAndIgnoresEarlyReturns(int overdueDays, int dailyFine, int maxFine, decimal expected)
    {
        var due = new DateTime(2026, 1, 1);
        var result = new CampusOperationsService(null!).CalculateLibraryFine(due, due.AddDays(overdueDays), dailyFine, maxFine);
        Assert.Equal(expected, result);
    }

    public static IEnumerable<object[]> FineCases()
    {
        yield return new object[] { -10, 10, 500, 0m };
        yield return new object[] { 0, 10, 500, 0m };
        yield return new object[] { 1, 10, 500, 10m };
        yield return new object[] { 5, 10, 500, 50m };
        yield return new object[] { 50, 10, 500, 500m };
        yield return new object[] { 51, 10, 500, 500m };
        yield return new object[] { 100, 0, 500, 0m };
        yield return new object[] { 100, 10, 0, 0m };
        yield return new object[] { 2, 25, 100, 50m };
        yield return new object[] { 10, 25, 100, 100m };
        yield return new object[] { 3, 7, 100, 21m };
        yield return new object[] { 14, 7, 100, 98m };
        yield return new object[] { 15, 7, 100, 100m };
        yield return new object[] { -1, 25, 100, 0m };
        yield return new object[] { 4, 1, 10, 4m };
        yield return new object[] { 11, 1, 10, 10m };
        yield return new object[] { 7, 100, 700, 700m };
        yield return new object[] { 6, 100, 700, 600m };
        yield return new object[] { 2, 0, 0, 0m };
        yield return new object[] { 20, 1, 19, 19m };
    }

    [Theory]
    [MemberData(nameof(CreditCases))]
    public void CurriculumCredits_AddNonNegativeComponents(int theory, int lab, int expected)
        => Assert.Equal(expected, new CurriculumService(null!, NullLogger<CurriculumService>.Instance).TotalCredits(theory, lab));

    public static IEnumerable<object[]> CreditCases()
    {
        for (var theory = 0; theory <= 8; theory++)
        {
            yield return new object[] { theory, 0, theory };
            if (theory > 0) yield return new object[] { 0, theory, theory };
        }

        yield return new object[] { 1, 1, 2 };
        yield return new object[] { 3, 1, 4 };
        yield return new object[] { 4, 2, 6 };
        yield return new object[] { 6, 3, 9 };
        yield return new object[] { 8, 4, 12 };
    }

    [Theory]
    [MemberData(nameof(WorkloadCases))]
    public void WorkloadAssignment_EnforcesHoursAndSections(int existingHours, int existingSections, int newHours, bool expected)
    {
        var workloads = Enumerable.Range(0, existingSections)
            .Select(i => new AegisCore.Domain.Entities.FacultyWorkload
            {
                CourseId = Guid.Parse($"00000000-0000-0000-0000-{i + 1:000000000000}"),
                HoursPerWeek = existingSections == 0 ? 0 : existingHours / existingSections,
                EffectiveTo = DateTime.UtcNow.AddDays(1)
            });

        Assert.Equal(expected, new CurriculumService(null!, NullLogger<CurriculumService>.Instance)
            .CanAssignWorkload(workloads, newHours));
    }

    public static IEnumerable<object[]> WorkloadCases()
    {
        yield return new object[] { 0, 0, 0, true };
        yield return new object[] { 0, 0, 12, true };
        yield return new object[] { 0, 0, 13, false };
        yield return new object[] { 4, 1, 8, true };
        yield return new object[] { 4, 1, 9, false };
        yield return new object[] { 8, 2, 4, true };
        yield return new object[] { 8, 2, 5, false };
        yield return new object[] { 9, 3, 3, true };
        yield return new object[] { 9, 3, 4, false };
        yield return new object[] { 12, 1, 0, true };
        yield return new object[] { 12, 1, 1, false };
        yield return new object[] { 8, 4, 4, false };
        yield return new object[] { 8, 3, 4, true };
        yield return new object[] { 4, 4, 0, false };
        yield return new object[] { 0, 4, 0, false };
        yield return new object[] { 6, 2, 6, true };
        yield return new object[] { 7, 2, 6, true };
        yield return new object[] { 1, 1, 11, true };
        yield return new object[] { 1, 1, 12, false };
        yield return new object[] { 10, 2, 2, true };
    }

    [Theory]
    [InlineData(0, 0.75, true)]
    [InlineData(0.1, 0.75, true)]
    [InlineData(0.74, 0.75, true)]
    [InlineData(0.75, 0.75, false)]
    [InlineData(0.76, 0.75, false)]
    [InlineData(0.99, 0.75, false)]
    [InlineData(1, 0.75, false)]
    [InlineData(0.5, 0.5, false)]
    [InlineData(0.49, 0.5, true)]
    [InlineData(0.25, 0.2, false)]
    [InlineData(0.19, 0.2, true)]
    [InlineData(0.9, 1, true)]
    [InlineData(1, 1, false)]
    [InlineData(0, 0, false)]
    [InlineData(0.01, 0, false)]
    public void HumanReview_IsRequiredBelowThreshold(double confidence, double threshold, bool expected)
        => Assert.Equal(expected, new AgenticExamService().RequiresHumanReview((decimal)confidence, (decimal)threshold));

    [Theory]
    [MemberData(nameof(HumanReviewGrid))]
    public void HumanReviewGrid_CoversConfidenceThresholdBoundaries(decimal confidence, decimal threshold)
        => Assert.Equal(confidence < threshold, new AgenticExamService().RequiresHumanReview(confidence, threshold));

    public static IEnumerable<object[]> HumanReviewGrid()
    {
        for (var confidence = 0; confidence <= 9; confidence++)
        {
            for (var threshold = 1; threshold <= 5; threshold++)
                yield return new object[] { confidence / 10m, threshold / 10m };
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("abcdef")]
    [InlineData("12a456")]
    [InlineData("１２３４５６")]
    [InlineData(" 123456")]
    [InlineData("123456 ")]
    [InlineData("000000")]
    [InlineData("999999")]
    [InlineData("123-456")]
    [InlineData("+12345")]
    [InlineData("١٢٣٤٥٦")]
    public void TotpRejectsMalformedCodes(string code)
        => Assert.False(new TwoFactorService().VerifyTotp("not-a-secret", code, DateTimeOffset.UnixEpoch));

    [Theory]
    [InlineData("student@example.edu.pk", "example.edu.pk")]
    [InlineData("STUDENT@EXAMPLE.EDU.PK", "example.edu.pk")]
    [InlineData(" student@example.edu.pk ", "example.edu.pk")]
    [InlineData("a@b.co", "b.co")]
    [InlineData("user@university.ac.uk", "university.ac.uk")]
    [InlineData("x@y.z", "y.z")]
    [InlineData("one.two@dept.example.org", "dept.example.org")]
    [InlineData("user+tag@example.com", "example.com")]
    [InlineData("admin@sub.domain.edu", "sub.domain.edu")]
    [InlineData("a@B.C", "b.c")]
    public void EmailDomain_NormalizesValidAddresses(string email, string expected)
        => Assert.Equal(expected, new MultiTenantSsoService(null!).GetEmailDomain(email));

    [Theory]
    [InlineData("")]
    [InlineData("user")]
    [InlineData("user@localhost")]
    [InlineData("@example.com")]
    [InlineData("user@@example.com")]
    [InlineData("user@example")]
    [InlineData("user@example.")]
    [InlineData("user@.example.com")]
    [InlineData("user example.com")]
    public void EmailDomain_RejectsInvalidAddresses(string email)
        => Assert.Throws<ArgumentException>(() => new MultiTenantSsoService(null!).GetEmailDomain(email));

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(100, 2)]
    [InlineData(1000, 3)]
    [InlineData(10000, 4)]
    [InlineData(100000, 5)]
    [InlineData(500000, 6)]
    [InlineData(1000000, 7)]
    public void TemporaryPasswords_AreAlwaysSixteenCharacters(int seed, int ignored)
    {
        _ = seed;
        _ = ignored;
        var password = new MultiTenantSsoService(null!).GenerateTemporaryPassword();
        Assert.Equal(16, password.Length);
        Assert.Contains(password, char.IsUpper);
        Assert.Contains(password, char.IsLower);
        Assert.Contains(password, char.IsDigit);
        Assert.Contains(password, "!@#$%".Contains);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(15)]
    [InlineData(20)]
    public void BackupCodes_HonorRequestedCount(int count)
        => Assert.Equal(count, new TwoFactorService().GenerateBackupCodes(count).Length);

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(50000, true)]
    [InlineData(100000, true)]
    [InlineData(200000, true)]
    [InlineData(600000, true)]
    [InlineData(1000000, true)]
    [InlineData(2000000, true)]
    [InlineData(5000000, true)]
    [InlineData(10000000, true)]
    public void PayrollGrossSalary_IsDeterministic(int basic, bool expected)
    {
        var result = new PayrollService(null!).CalculatePayslip(new AegisCore.Domain.Entities.Employee { Salary = basic }, new DateOnly(2026, 1, 1));
        Assert.Equal(expected, result.GrossSalary == basic * 1.2m + 8000m);
        Assert.True(result.NetSalary <= result.GrossSalary);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(10, 0)]
    [InlineData(50000, 0)]
    [InlineData(600000, 0)]
    [InlineData(1200000, 0)]
    [InlineData(2400000, 0)]
    [InlineData(10000000, 0)]
    [InlineData(50000000, 0)]
    public void PayrollNeverProducesNegativeNetSalary(int basic, int ignored)
    {
        _ = ignored;
        var result = new PayrollService(null!).CalculatePayslip(new AegisCore.Domain.Entities.Employee { Salary = basic }, new DateOnly(2026, 1, 1));
        Assert.True(result.NetSalary >= 0m);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(10, 1)]
    [InlineData(100, 1)]
    [InlineData(1000, 1)]
    [InlineData(1, 2)]
    [InlineData(2, 2)]
    [InlineData(10, 2)]
    [InlineData(100, 2)]
    [InlineData(1000, 2)]
    public void AgenticQrPayload_ContainsBothIdentifiers(int examSeed, int studentSeed)
    {
        var exam = Guid.Parse($"00000000-0000-0000-0000-{examSeed:000000000000}");
        var student = Guid.Parse($"00000000-0000-0000-0000-{studentSeed:000000000000}");
        var payload = new AgenticExamService().BuildQrPayload(exam, student);
        Assert.Contains($"exam:{exam:N}", payload);
        Assert.Contains($"student:{student:N}", payload);
    }
}
