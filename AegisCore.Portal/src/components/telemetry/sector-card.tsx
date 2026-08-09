"use client";

// ─── Telemetry Sector Card ───
// F1-inspired "sector" presentation of one backend TelemetryMetric:
// value + absolute status + personal progress + trend, with cohort and
// personal context kept visibly separate. No status is ever recomputed
// here — the backend classifications are rendered as delivered.

import {
  formatDelta,
  formatPercentile,
  formatValue,
  formatZScore,
  humanizeDimension,
  type TelemetryMetric,
} from "@/lib/analytics";
import { performanceStatusMeta } from "@/lib/telemetry-semantics";
import { ImprovementChip, PerformanceChip, TrendIndicator } from "./status";

function Stat({ label, value, hint }: { label: string; value: string; hint?: string }) {
  return (
    <div className="sector-stat" title={hint}>
      <span className="sector-stat-label">{label}</span>
      <span className="sector-stat-value">{value}</span>
    </div>
  );
}

export function TelemetrySectorCard({ metric }: { metric: TelemetryMetric }) {
  const perf = performanceStatusMeta(metric.performanceStatus);
  const hasValue = metric.normalizedValue !== null || metric.rawValue !== null;
  const suppressed = metric.availability.status === "Suppressed";
  const unavailable = metric.availability.status === "Unavailable";
  const barWidth =
    metric.normalizedValue !== null ? Math.min(100, Math.max(0, metric.normalizedValue)) : 0;

  return (
    <article
      className="sector-card"
      data-tone={perf.tone}
      aria-label={`${humanizeDimension(metric.dimension)}: ${perf.label}`}
    >
      <header className="sector-card-head">
        <h3>{humanizeDimension(metric.dimension)}</h3>
        <TrendIndicator direction={metric.trendDirection} />
      </header>

      <div className="sector-card-value">
        {hasValue ? (
          <>
            <strong>{formatValue(metric.normalizedValue ?? metric.rawValue)}</strong>
            {metric.normalizedValue !== null && <span className="sector-card-unit">/ 100</span>}
          </>
        ) : (
          <strong className="sector-card-na">—</strong>
        )}
      </div>

      {/* Sector bar: visual only, labelled accessibly above. */}
      <div
        className="sector-bar"
        role="img"
        aria-label={
          hasValue
            ? `${humanizeDimension(metric.dimension)} score ${formatValue(metric.normalizedValue ?? metric.rawValue)} out of 100, status ${perf.label}`
            : `${humanizeDimension(metric.dimension)}: no value available`
        }
      >
        <div className={`sector-bar-fill sector-bar-${perf.tone}`} style={{ width: `${barWidth}%` }} />
      </div>

      <div className="sector-card-chips">
        <PerformanceChip status={metric.performanceStatus} />
        <ImprovementChip status={metric.improvementStatus} />
      </div>

      <dl className="sector-card-stats">
        <Stat
          label="Personal Δ"
          value={formatDelta(metric.personalDelta)}
          hint={
            metric.personalPreviousValue !== null
              ? `Previously ${formatValue(metric.personalPreviousValue)}`
              : "No previous personal value"
          }
        />
        <Stat
          label="Cohort Δ"
          value={suppressed || metric.cohortDelta === null ? "—" : formatDelta(metric.cohortDelta)}
          hint="Difference from the cohort mean"
        />
        <Stat
          label="Percentile"
          value={suppressed ? "—" : formatPercentile(metric.percentile)}
          hint="Your position relative to the comparison cohort"
        />
        <Stat
          label="z-score"
          value={suppressed ? "—" : formatZScore(metric.zScore)}
          hint="Standard deviations above or below the cohort mean"
        />
      </dl>

      <footer className="sector-card-foot">
        {suppressed && (
          <span className="sector-card-note">Cohort comparison hidden to protect privacy (small cohort).</span>
        )}
        {unavailable && (
          <span className="sector-card-note">
            {metric.availability.reason ?? "Insufficient data for this dimension."}
          </span>
        )}
        {!suppressed && !unavailable && (
          <span className="sector-card-note">
            n = {metric.sampleSize} · {metric.comparisonPopulation}
          </span>
        )}
      </footer>
    </article>
  );
}
