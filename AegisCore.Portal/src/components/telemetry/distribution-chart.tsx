"use client";

// ─── Distribution Chart ───
// Backend-supplied histogram with the student's own position and the cohort
// median marked. Axis scaling starts at zero (count axis) — no misleading
// truncation. Includes an accessible table equivalent.

import { formatValue, type HistogramBucket } from "@/lib/analytics";

type Props = {
  distribution: HistogramBucket[];
  /** The student's own value (e.g. final score) — marked on the chart. */
  ownValue?: number | null;
  ownLabel?: string;
  median?: number | null;
};

export function DistributionChart({ distribution, ownValue, ownLabel = "You", median }: Props) {
  if (!distribution || distribution.length === 0) {
    return <p className="availability-note" role="note">No cohort distribution was recorded.</p>;
  }

  const maxCount = Math.max(...distribution.map(b => b.count), 1);
  const minBound = Math.min(...distribution.map(b => b.lowerBound));
  const maxBound = Math.max(...distribution.map(b => b.upperBound));
  const span = maxBound - minBound || 1;
  const toPct = (v: number) => `${Math.min(100, Math.max(0, ((v - minBound) / span) * 100))}%`;

  return (
    <div className="distribution-chart">
      <div
        className="distribution-plot"
        role="img"
        aria-label={`Cohort distribution across ${distribution.length} buckets from ${formatValue(minBound)} to ${formatValue(maxBound)}`}
      >
        {distribution.map((b, i) => (
          <div key={i} className="distribution-col" title={`${formatValue(b.lowerBound)}–${formatValue(b.upperBound)}: ${b.count}`}>
            <div className="distribution-bar" style={{ height: `${(b.count / maxCount) * 100}%` }} />
          </div>
        ))}
        {ownValue !== null && ownValue !== undefined && ownValue >= minBound && ownValue <= maxBound && (
          <div className="distribution-marker distribution-marker-own" style={{ insetInlineStart: toPct(ownValue) }}>
            <span>{ownLabel}</span>
          </div>
        )}
        {median !== null && median !== undefined && median >= minBound && median <= maxBound && (
          <div className="distribution-marker distribution-marker-median" style={{ insetInlineStart: toPct(median) }}>
            <span>Median</span>
          </div>
        )}
      </div>
      <div className="distribution-axis">
        <span>{formatValue(minBound)}</span>
        <span>{formatValue(maxBound)}</span>
      </div>

      <details className="chart-table">
        <summary>View distribution data as a table</summary>
        <table>
          <thead>
            <tr><th>Range</th><th>Students</th></tr>
          </thead>
          <tbody>
            {distribution.map((b, i) => (
              <tr key={i}>
                <td className="font-mono">
                  {formatValue(b.lowerBound)} – {formatValue(b.upperBound)}
                </td>
                <td className="font-mono">{b.count}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </details>
    </div>
  );
}
