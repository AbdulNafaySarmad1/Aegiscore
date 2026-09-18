using AegisCore.Domain;

namespace AegisCore.Infrastructure.Platform;

/// <summary>Descriptive statistics of a set of percentage marks, for the grade distribution report.</summary>
public sealed class DistributionStatistics
{
    private readonly List<double> _sorted;
    public double Mean { get; }
    public double Median { get; }
    public double Mode { get; }
    public double Variance { get; }
    public double StdDev { get; }
    public double Skewness { get; }
    public double Kurtosis { get; }
    public GradeCurveShape Shape { get; }

    private DistributionStatistics(List<double> values)
    {
        _sorted = values.OrderBy(v => v).ToList();
        var n = _sorted.Count;
        Mean = _sorted.Average();
        Median = n % 2 == 1 ? _sorted[n / 2] : (_sorted[n / 2 - 1] + _sorted[n / 2]) / 2;
        Mode = _sorted.GroupBy(v => Math.Round(v)).OrderByDescending(g => g.Count()).ThenBy(g => g.Key).First().Key;
        Variance = _sorted.Sum(v => (v - Mean) * (v - Mean)) / n;
        StdDev = Math.Sqrt(Variance);
        // Fisher moments; a flat set (single value) has no shape.
        Skewness = StdDev == 0 ? 0 : _sorted.Sum(v => Math.Pow((v - Mean) / StdDev, 3)) / n;
        Kurtosis = StdDev == 0 ? 0 : _sorted.Sum(v => Math.Pow((v - Mean) / StdDev, 4)) / n - 3;
        Shape = StdDev == 0 ? GradeCurveShape.Uniform
            : Skewness < -0.5 ? GradeCurveShape.LeftSkewed
            : Skewness > 0.5 ? GradeCurveShape.RightSkewed
            : Kurtosis > 1 ? GradeCurveShape.Leptokurtic
            : Kurtosis < -1 ? GradeCurveShape.Platykurtic
            : GradeCurveShape.Normal;
    }

    public static DistributionStatistics From(List<double> values)
        => values.Count == 0 ? throw new ArgumentException("At least one mark is required.", nameof(values)) : new DistributionStatistics(values);

    public double Share(Func<double, bool> predicate) => (double)_sorted.Count(predicate) / _sorted.Count;

    /// <summary>Letter grades on the same thresholds as <see cref="Domain.Entities.Result"/>.</summary>
    public Dictionary<string, int> Letters() => new()
    {
        ["A+"] = _sorted.Count(p => p >= 90), ["A"] = _sorted.Count(p => p is >= 80 and < 90), ["B"] = _sorted.Count(p => p is >= 70 and < 80),
        ["C"] = _sorted.Count(p => p is >= 60 and < 70), ["D"] = _sorted.Count(p => p is >= 50 and < 60), ["F"] = _sorted.Count(p => p < 50)
    };

    public Dictionary<string, double> Percentiles() => new()
    {
        ["p10"] = Percentile(0.10), ["p25"] = Percentile(0.25), ["p50"] = Percentile(0.50), ["p75"] = Percentile(0.75), ["p90"] = Percentile(0.90)
    };

    private double Percentile(double p)
    {
        if (_sorted.Count == 1) return _sorted[0];
        var position = p * (_sorted.Count - 1);
        var lower = (int)Math.Floor(position);
        var fraction = position - lower;
        return lower + 1 < _sorted.Count ? _sorted[lower] + fraction * (_sorted[lower + 1] - _sorted[lower]) : _sorted[lower];
    }
}
