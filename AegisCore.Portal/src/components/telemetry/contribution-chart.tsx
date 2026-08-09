"use client";

// ─── Contribution Chart ───
// Horizontal influence bars for backend-recorded SHAP snapshots (local or
// global mean-|SHAP|). Pure presentation of stored values — never a
// recomputation, never causal language.

import { formatDelta, formatValue, type ModelFeatureContribution } from "@/lib/analytics";

type LocalProps = {
  mode: "local";
  contributions: ModelFeatureContribution[];
  maxItems?: number;
};

type GlobalProps = {
  mode: "global";
  values: Record<string, number>;
  maxItems?: number;
};

type Props = LocalProps | GlobalProps;

/**
 * Reusable SHAP contribution visualization.
 * - local: signed contributions (positive/negative influence on the output)
 * - global: mean absolute contributions (magnitude only)
 * Includes an accessible text/table equivalent.
 */
export function ContributionChart(props: Props) {
  const rows =
    props.mode === "local"
      ? props.contributions
          .slice()
          .sort((a, b) => Math.abs(b.shapValue) - Math.abs(a.shapValue))
          .slice(0, props.maxItems ?? 12)
          .map(c => ({ name: c.featureName, value: c.shapValue, detail: `feature value ${formatValue(c.featureValue, 2)}` }))
      : Object.entries(props.values)
          .sort((a, b) => Math.abs(b[1]) - Math.abs(a[1]))
          .slice(0, props.maxItems ?? 12)
          .map(([name, value]) => ({ name, value, detail: "mean |SHAP|" }));

  if (rows.length === 0) {
    return <p className="availability-note" role="note">No contribution values were recorded for this explanation.</p>;
  }

  const maxAbs = Math.max(...rows.map(r => Math.abs(r.value)), 1e-9);

  return (
    <div className="contribution-chart">
      <ul className="contribution-list">
        {rows.map(row => {
          const positive = row.value >= 0;
          const width = (Math.abs(row.value) / maxAbs) * 100;
          return (
            <li key={row.name} className="contribution-row">
              <span className="contribution-name" title={row.detail}>{row.name}</span>
              <span
                className="contribution-track"
                role="img"
                aria-label={`${row.name}: ${positive ? "increases" : "decreases"} the model output by ${formatValue(Math.abs(row.value), 3)}`}
              >
                <span
                  className={`contribution-bar ${positive ? "contribution-pos" : "contribution-neg"}`}
                  style={{ width: `${width}%` }}
                />
              </span>
              <span className={`contribution-value ${positive ? "contribution-pos-text" : "contribution-neg-text"}`}>
                {formatDelta(row.value, 3)}
              </span>
            </li>
          );
        })}
      </ul>

      {/* Accessible text equivalent */}
      <details className="chart-table">
        <summary>View contribution data as a table</summary>
        <table>
          <thead>
            <tr><th>Feature</th><th>{props.mode === "local" ? "SHAP contribution" : "Mean |SHAP|"}</th></tr>
          </thead>
          <tbody>
            {rows.map(row => (
              <tr key={row.name}>
                <td>{row.name}</td>
                <td className="font-mono">{formatDelta(row.value, 4)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </details>
    </div>
  );
}
