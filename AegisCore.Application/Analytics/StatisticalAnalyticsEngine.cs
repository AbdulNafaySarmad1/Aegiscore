namespace AegisCore.Application.Analytics;

public sealed class StatisticalAnalyticsEngine : IStatisticalAnalyticsEngine
{
    private static readonly double[] DefaultPercentiles = [5, 10, 25, 50, 75, 90, 95];

    public StatisticalSummary Summarize(
        IReadOnlyList<double?> values,
        IReadOnlyList<double>? percentiles = null,
        int histogramBins = 10,
        int minimumValidCount = 2,
        double confidenceLevel = 0.95)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (histogramBins is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(histogramBins));
        if (minimumValidCount < 1) throw new ArgumentOutOfRangeException(nameof(minimumValidCount));
        if (confidenceLevel is <= 0 or >= 1) throw new ArgumentOutOfRangeException(nameof(confidenceLevel));

        var valid = values
            .Select((value, index) => (value, index))
            .Where(item => item.value is { } value && double.IsFinite(value))
            .Select(item => (Value: item.value!.Value, item.index))
            .OrderBy(item => item.Value)
            .ToArray();
        var sorted = valid.Select(item => item.Value).ToArray();
        var sufficient = sorted.Length >= minimumValidCount;

        if (sorted.Length == 0)
        {
            return new StatisticalSummary(values.Count, 0, values.Count, false, "No finite values are available.",
                null, null, null, null, null, [], null, null, null, null, null, [], null, [], [], null);
        }

        var minimum = sorted[0];
        var maximum = sorted[^1];
        var mean = sorted.Average();
        var median = Quantile(sorted, 0.5);
        var q1 = Quantile(sorted, 0.25);
        var q3 = Quantile(sorted, 0.75);
        var iqr = q3 - q1;
        var variance = sorted.Length >= 2 ? sorted.Sum(value => Math.Pow(value - mean, 2)) / (sorted.Length - 1) : (double?)null;
        double? standardDeviation = variance is { } sampleVariance ? Math.Sqrt(sampleVariance) : null;
        var requestedPercentiles = (percentiles ?? DefaultPercentiles)
            .Distinct()
            .Order()
            .Select(percentile =>
            {
                if (percentile is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(percentiles));
                return new PercentileValue(percentile, Quantile(sorted, percentile / 100));
            })
            .ToArray();
        var modes = sorted.GroupBy(value => value).Select(group => new { Value = group.Key, Count = group.Count() }).ToArray();
        var maximumFrequency = modes.Max(item => item.Count);
        var meaningfulModes = maximumFrequency > 1
            ? modes.Where(item => item.Count == maximumFrequency).Select(item => item.Value).ToArray()
            : [];
        var lowerFence = q1 - 1.5 * iqr;
        var upperFence = q3 + 1.5 * iqr;
        var outliers = valid
            .Where(item => item.Value < lowerFence || item.Value > upperFence)
            .Select(item => new OutlierIndicator(item.index, item.Value, item.Value < lowerFence ? "Lower" : "Upper"))
            .ToArray();

        return new StatisticalSummary(
            values.Count,
            sorted.Length,
            values.Count - sorted.Length,
            sufficient,
            sufficient ? null : $"At least {minimumValidCount} valid values are required.",
            minimum,
            maximum,
            maximum - minimum,
            mean,
            median,
            meaningfulModes,
            variance,
            standardDeviation,
            q1,
            q3,
            iqr,
            requestedPercentiles,
            AdjustedSkewness(sorted, mean, standardDeviation),
            BuildHistogram(sorted, histogramBins),
            outliers,
            BuildMeanConfidenceInterval(mean, standardDeviation, sorted.Length, confidenceLevel));
    }

    public double? PercentileRank(double value, IReadOnlyList<double?> population)
    {
        ArgumentNullException.ThrowIfNull(population);
        var valid = Finite(population);
        if (valid.Length == 0 || !double.IsFinite(value)) return null;
        var below = valid.Count(item => item < value);
        var equal = valid.Count(item => item == value);
        return (below + 0.5 * equal) / valid.Length * 100;
    }

    public double? ZScore(double value, IReadOnlyList<double?> population)
    {
        var summary = Summarize(population, minimumValidCount: 2);
        return summary.SampleStandardDeviation is > 0 && summary.Mean is { } mean
            ? (value - mean) / summary.SampleStandardDeviation.Value
            : null;
    }

    public CohortComparison CompareToCohort(double value, IReadOnlyList<double?> cohort, int minimumCohortSize)
    {
        if (minimumCohortSize < 2) throw new ArgumentOutOfRangeException(nameof(minimumCohortSize));
        var summary = Summarize(cohort, minimumValidCount: minimumCohortSize);
        if (summary.ValidCount < minimumCohortSize)
        {
            return new CohortComparison(true, $"Comparative statistics require at least {minimumCohortSize} valid members.", summary.ValidCount, null, null, null, null, null);
        }

        return new CohortComparison(false, null, summary.ValidCount, summary.Mean, summary.Median,
            summary.SampleStandardDeviation, PercentileRank(value, cohort), ZScore(value, cohort));
    }

    public PeriodComparison ComparePeriods(double? current, double? previous, double flatTolerance = 0.001)
    {
        if (flatTolerance < 0) throw new ArgumentOutOfRangeException(nameof(flatTolerance));
        if (current is null || previous is null || !double.IsFinite(current.Value) || !double.IsFinite(previous.Value))
            return new PeriodComparison(current, previous, null, null, TrendDirection.Unavailable);

        var delta = current.Value - previous.Value;
        double? percentageDelta = previous.Value == 0 ? null : delta / Math.Abs(previous.Value) * 100;
        var direction = Math.Abs(delta) <= flatTolerance ? TrendDirection.Flat : delta > 0 ? TrendDirection.Up : TrendDirection.Down;
        return new PeriodComparison(current, previous, delta, percentageDelta, direction);
    }

    public IReadOnlyList<double?> RollingMean(IReadOnlyList<double?> values, int windowSize, int minimumValidValues = 1)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (windowSize < 1) throw new ArgumentOutOfRangeException(nameof(windowSize));
        if (minimumValidValues < 1 || minimumValidValues > windowSize) throw new ArgumentOutOfRangeException(nameof(minimumValidValues));

        var result = new double?[values.Count];
        for (var index = 0; index < values.Count; index++)
        {
            var window = values.Skip(Math.Max(0, index - windowSize + 1)).Take(Math.Min(windowSize, index + 1));
            var valid = window.Where(value => value is { } item && double.IsFinite(item)).Select(value => value!.Value).ToArray();
            result[index] = valid.Length >= minimumValidValues ? valid.Average() : null;
        }
        return result;
    }

    public CorrelationResult Correlate(IReadOnlyList<double?> left, IReadOnlyList<double?> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        if (left.Count != right.Count) throw new ArgumentException("Correlation inputs must have equal lengths.");
        var pairs = left.Zip(right)
            .Where(pair => pair.First is { } x && pair.Second is { } y && double.IsFinite(x) && double.IsFinite(y))
            .Select(pair => (X: pair.First!.Value, Y: pair.Second!.Value))
            .ToArray();
        if (pairs.Length < 3) return new CorrelationResult(pairs.Length, false, null, "At least three valid pairs are required.");
        var xMean = pairs.Average(pair => pair.X);
        var yMean = pairs.Average(pair => pair.Y);
        var covariance = pairs.Sum(pair => (pair.X - xMean) * (pair.Y - yMean));
        var xSumSquares = pairs.Sum(pair => Math.Pow(pair.X - xMean, 2));
        var ySumSquares = pairs.Sum(pair => Math.Pow(pair.Y - yMean, 2));
        if (xSumSquares == 0 || ySumSquares == 0)
            return new CorrelationResult(pairs.Length, false, null, "Correlation is undefined when either variable has zero variance.");
        return new CorrelationResult(pairs.Length, true, covariance / Math.Sqrt(xSumSquares * ySumSquares), null);
    }

    public AnovaResult OneWayAnova(IReadOnlyDictionary<string, IReadOnlyList<double?>> groups)
    {
        ArgumentNullException.ThrowIfNull(groups);
        var validGroups = groups
            .Select(group => (group.Key, Values: Finite(group.Value)))
            .Where(group => group.Values.Length > 0)
            .ToArray();
        var sampleSize = validGroups.Sum(group => group.Values.Length);
        if (validGroups.Length < 2 || validGroups.Any(group => group.Values.Length < 2) || sampleSize <= validGroups.Length)
            return new AnovaResult(validGroups.Length, sampleSize, false, null, null, null, null, "At least two groups with two valid observations each are required.");

        var grandMean = validGroups.SelectMany(group => group.Values).Average();
        var between = validGroups.Sum(group => group.Values.Length * Math.Pow(group.Values.Average() - grandMean, 2));
        var within = validGroups.Sum(group =>
        {
            var mean = group.Values.Average();
            return group.Values.Sum(value => Math.Pow(value - mean, 2));
        });
        var dfBetween = validGroups.Length - 1;
        var dfWithin = sampleSize - validGroups.Length;
        var f = within == 0 ? (between == 0 ? (double?)null : double.PositiveInfinity) : (between / dfBetween) / (within / dfWithin);
        var total = between + within;
        return new AnovaResult(validGroups.Length, sampleSize, f is not null, f, dfBetween, dfWithin, total == 0 ? null : between / total,
            f is null ? "ANOVA is undefined when all observations are identical." : null);
    }

    private static double[] Finite(IEnumerable<double?> values) => values
        .Where(value => value is { } item && double.IsFinite(item))
        .Select(value => value!.Value)
        .ToArray();

    private static double Quantile(IReadOnlyList<double> sorted, double probability)
    {
        if (sorted.Count == 1) return sorted[0];
        var position = (sorted.Count - 1) * probability;
        var lower = (int)Math.Floor(position);
        var upper = (int)Math.Ceiling(position);
        return lower == upper ? sorted[lower] : sorted[lower] + (position - lower) * (sorted[upper] - sorted[lower]);
    }

    private static double? AdjustedSkewness(IReadOnlyList<double> values, double mean, double? standardDeviation)
    {
        if (values.Count < 3 || standardDeviation is null or 0) return null;
        var n = values.Count;
        var sumCubed = values.Sum(value => Math.Pow((value - mean) / standardDeviation.Value, 3));
        return n / ((double)(n - 1) * (n - 2)) * sumCubed;
    }

    private static IReadOnlyList<HistogramBucket> BuildHistogram(IReadOnlyList<double> sorted, int requestedBins)
    {
        if (sorted.Count == 0) return [];
        if (sorted[0] == sorted[^1]) return [new HistogramBucket(sorted[0], sorted[0], sorted.Count, true)];
        var bins = Math.Min(requestedBins, sorted.Count);
        var width = (sorted[^1] - sorted[0]) / bins;
        var counts = new int[bins];
        foreach (var value in sorted)
        {
            var index = value == sorted[^1] ? bins - 1 : Math.Min((int)((value - sorted[0]) / width), bins - 1);
            counts[index]++;
        }
        return Enumerable.Range(0, bins)
            .Select(index => new HistogramBucket(sorted[0] + index * width, sorted[0] + (index + 1) * width, counts[index], index == bins - 1))
            .ToArray();
    }

    private static ConfidenceInterval? BuildMeanConfidenceInterval(double mean, double? standardDeviation, int count, double level)
    {
        if (count < 2 || standardDeviation is null) return null;
        if (standardDeviation == 0) return new ConfidenceInterval(level, mean, mean, "StudentT");
        if (count < 30) return null;
        var probability = 1 - (1 - level) / 2;
        var z = InverseStandardNormal(probability);
        var degreesOfFreedom = count - 1d;
        var t = z + (Math.Pow(z, 3) + z) / (4 * degreesOfFreedom)
            + (5 * Math.Pow(z, 5) + 16 * Math.Pow(z, 3) + 3 * z) / (96 * Math.Pow(degreesOfFreedom, 2));
        var margin = t * standardDeviation.Value / Math.Sqrt(count);
        return new ConfidenceInterval(level, mean - margin, mean + margin, "StudentTApproximation");
    }

    // Peter J. Acklam's rational approximation.
    private static double InverseStandardNormal(double probability)
    {
        const double low = 0.02425;
        const double high = 1 - low;
        double[] a = [-39.6968302866538, 220.946098424521, -275.928510446969, 138.357751867269, -30.6647980661472, 2.50662827745924];
        double[] b = [-54.4760987982241, 161.585836858041, -155.698979859887, 66.8013118877197, -13.2806815528857];
        double[] c = [-0.00778489400243029, -0.322396458041136, -2.40075827716184, -2.54973253934373, 4.37466414146497, 2.93816398269878];
        double[] d = [0.00778469570904146, 0.32246712907004, 2.445134137143, 3.75440866190742];
        if (probability < low)
        {
            var q = Math.Sqrt(-2 * Math.Log(probability));
            return (((((c[0] * q + c[1]) * q + c[2]) * q + c[3]) * q + c[4]) * q + c[5])
                / ((((d[0] * q + d[1]) * q + d[2]) * q + d[3]) * q + 1);
        }
        if (probability <= high)
        {
            var q = probability - 0.5;
            var r = q * q;
            return (((((a[0] * r + a[1]) * r + a[2]) * r + a[3]) * r + a[4]) * r + a[5]) * q
                / (((((b[0] * r + b[1]) * r + b[2]) * r + b[3]) * r + b[4]) * r + 1);
        }
        var upperQ = Math.Sqrt(-2 * Math.Log(1 - probability));
        return -(((((c[0] * upperQ + c[1]) * upperQ + c[2]) * upperQ + c[3]) * upperQ + c[4]) * upperQ + c[5])
            / ((((d[0] * upperQ + d[1]) * upperQ + d[2]) * upperQ + d[3]) * upperQ + 1);
    }
}

public sealed class TelemetryPolicyEngine : ITelemetryPolicyEngine
{
    public TelemetryClassification Classify(
        double? normalizedValue,
        double? previousValue,
        IReadOnlyList<double?> personalHistory,
        TelemetryClassificationPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(personalHistory);
        ArgumentNullException.ThrowIfNull(policy);
        if (normalizedValue is null || !double.IsFinite(normalizedValue.Value))
            return new TelemetryClassification(PerformanceStatus.Unavailable, ImprovementStatus.Unavailable, TrendDirection.Unavailable);

        var current = normalizedValue.Value;
        var performance = current >= policy.ExceptionalMinimum ? PerformanceStatus.Exceptional
            : current >= policy.StrongMinimum ? PerformanceStatus.Strong
            : current >= policy.DevelopingMinimum ? PerformanceStatus.Developing
            : PerformanceStatus.AtRisk;
        var validHistory = personalHistory
            .Where(value => value is { } item && double.IsFinite(item))
            .Select(value => value!.Value)
            .ToArray();
        var delta = previousValue is { } previous && double.IsFinite(previous) ? current - previous : (double?)null;
        var progress = validHistory.Length > 0 && current > validHistory.Max() + policy.StableTolerance
            ? ImprovementStatus.PersonalBest
            : delta >= policy.ImprovementMinimumDelta ? ImprovementStatus.Improving
            : delta <= -policy.DeclineMinimumDelta ? ImprovementStatus.Declining
            : delta is not null ? ImprovementStatus.Stable
            : ImprovementStatus.Unavailable;
        var trend = delta is null ? TrendDirection.Unavailable
            : Math.Abs(delta.Value) <= policy.StableTolerance ? TrendDirection.Flat
            : delta > 0 ? TrendDirection.Up
            : TrendDirection.Down;
        return new TelemetryClassification(performance, progress, trend);
    }
}
