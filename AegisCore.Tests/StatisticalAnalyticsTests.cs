using AegisCore.Application.Analytics;

namespace AegisCore.Tests;

public sealed class StatisticalAnalyticsTests
{
    private readonly StatisticalAnalyticsEngine _engine = new();

    [Fact]
    public void Summary_UsesSampleStatisticsAndIgnoresMissingValues()
    {
        var summary = _engine.Summarize([1, 2, null, double.NaN, 3, 4, 5], [0, 25, 50, 75, 100], 5);

        Assert.Equal(7, summary.Count);
        Assert.Equal(5, summary.ValidCount);
        Assert.Equal(2, summary.MissingCount);
        Assert.Equal(3, summary.Mean);
        Assert.Equal(3, summary.Median);
        Assert.Equal(2.5, summary.SampleVariance);
        Assert.Equal(Math.Sqrt(2.5), summary.SampleStandardDeviation);
        Assert.Equal(2, summary.FirstQuartile);
        Assert.Equal(4, summary.ThirdQuartile);
        Assert.Equal(2, summary.InterquartileRange);
        Assert.Equal([1, 2, 3, 4, 5], summary.Percentiles.Select(item => item.Value));
        Assert.Empty(summary.Modes);
        Assert.Equal(5, summary.Distribution.Sum(bucket => bucket.Count));
    }

    [Fact]
    public void ZeroVariance_HasNoZScoreOrSkewnessAndDegenerateConfidenceInterval()
    {
        var summary = _engine.Summarize([7, 7, 7, 7]);

        Assert.Equal(0, summary.SampleVariance);
        Assert.Equal(0, summary.SampleStandardDeviation);
        Assert.Null(summary.Skewness);
        Assert.Null(_engine.ZScore(7, [7, 7, 7]));
        Assert.Equal(7, summary.MeanConfidenceInterval!.LowerBound);
        Assert.Equal(7, summary.MeanConfidenceInterval.UpperBound);
    }

    [Fact]
    public void PercentileRank_UsesMidrankAtPopulationBoundaries()
    {
        double?[] population = [10, 20, 30, 40];

        Assert.Equal(12.5, _engine.PercentileRank(10, population));
        Assert.Equal(87.5, _engine.PercentileRank(40, population));
        Assert.Equal(50, _engine.PercentileRank(25, population));
    }

    [Fact]
    public void TinyCohort_IsSuppressedWithoutLeakingComparativeValues()
    {
        var comparison = _engine.CompareToCohort(75, [70, 75, 80], 5);

        Assert.True(comparison.Suppressed);
        Assert.Equal(3, comparison.SampleSize);
        Assert.Null(comparison.CohortMean);
        Assert.Null(comparison.CohortMedian);
        Assert.Null(comparison.PercentileRank);
        Assert.Null(comparison.ZScore);
    }

    [Fact]
    public void RollingCorrelationAndAnova_HandleMissingAndGroupedData()
    {
        Assert.Equal([1d, 1d, 3d, 4d], _engine.RollingMean([1, null, 3, 5], 2));

        var correlation = _engine.Correlate([1, 2, null, 3, 4], [2, 4, 99, 6, 8]);
        Assert.True(correlation.IsSufficient);
        Assert.Equal(1, correlation.PearsonCorrelation!.Value, 10);

        var anova = _engine.OneWayAnova(new Dictionary<string, IReadOnlyList<double?>>
        {
            ["A"] = [1, 2, 3],
            ["B"] = [7, 8, 9]
        });
        Assert.True(anova.IsSufficient);
        Assert.True(anova.FStatistic > 0);
        Assert.True(anova.EtaSquared > 0.8);
    }

    [Fact]
    public void TelemetryClassification_SeparatesLowAbsoluteStatusFromPersonalBest()
    {
        var classification = new TelemetryPolicyEngine().Classify(58, 54, [42, 50, 54], new TelemetryClassificationPolicy());

        Assert.Equal(PerformanceStatus.AtRisk, classification.AbsolutePerformance);
        Assert.Equal(ImprovementStatus.PersonalBest, classification.PersonalProgress);
        Assert.Equal(TrendDirection.Up, classification.TrendDirection);
    }
}
