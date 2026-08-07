"use client";

import { useTranslations } from "next-intl";
import type { NChartSeriesConfig, NChartConfig, NChartDataPoint } from "./types";
import { DEFAULT_COLORS, formatValueCompact } from "./types";

interface NChartLegendProps {
  config: NChartConfig;
  data: NChartDataPoint[];
  series: NChartSeriesConfig[];
  hiddenSeries: Set<string>;
  onToggleSeries: (dataKey: string) => void;
}

export function NChartLegend({
  config,
  data,
  series,
  hiddenSeries,
  onToggleSeries,
}: NChartLegendProps) {
  const t = useTranslations(config.i18nDomain ?? "nchart");
  const showLegend = config.showLegend !== false;
  const position = config.legendPosition ?? "bottom";

  if (!showLegend || series.length === 0) return null;

  const positionStyle: React.CSSProperties =
    position === "right"
      ? {
          flexDirection: "column",
          gap: "0.5rem",
          maxWidth: "180px",
          padding: "0.5rem 0",
        }
      : {
          flexDirection: "row",
          flexWrap: "wrap",
          gap: "0.75rem",
          padding: "0.375rem 0",
        };

  const containerStyle: React.CSSProperties =
    position === "top"
      ? {
          order: -1,
          borderBottom: "1px solid var(--line)",
          padding: "0.625rem 1rem",
          display: "flex",
          flexWrap: "wrap",
          gap: "0.75rem",
          alignItems: "center",
        }
      : position === "right"
        ? {
            borderLeft: "1px solid var(--line)",
            padding: "0.75rem 1rem",
            display: "flex",
            flexDirection: "column",
            gap: "0.5rem",
            flexShrink: 0,
          }
        : {
            borderTop: "1px solid var(--line)",
            padding: "0.625rem 1rem",
            display: "flex",
            flexWrap: "wrap",
            gap: "0.75rem",
            alignItems: "center",
          };

  return (
    <div style={containerStyle} role="group" aria-label={t("nchart.legend")}>
      <div style={{ display: "flex", ...positionStyle } as React.CSSProperties}>
        {series.map((s, idx) => {
          const color = s.color ?? DEFAULT_COLORS[idx % DEFAULT_COLORS.length];
          const isHidden = hiddenSeries.has(s.dataKey);

          const total = formatValueCompact(
            data.reduce((sum, d) => sum + (Number(d[s.dataKey]) || 0), 0)
          );

          return (
            <button
              key={s.dataKey}
              onClick={() => onToggleSeries(s.dataKey)}
              onKeyDown={(e) => {
                if (e.key === "Enter" || e.key === " ") {
                  e.preventDefault();
                  onToggleSeries(s.dataKey);
                }
              }}
              role="switch"
              aria-checked={!isHidden}
              aria-label={`${t("nchart.toggleSeries")} ${s.name ?? s.dataKey}`}
              style={{
                display: "flex",
                alignItems: "center",
                gap: "0.4rem",
                padding: "0.15rem 0.4rem",
                border: "1px solid transparent",
                borderRadius: "5px",
                background: "transparent",
                cursor: "pointer",
                fontSize: "0.75rem",
                color: isHidden ? "var(--subtle)" : "var(--ink)",
                opacity: isHidden ? 0.55 : 1,
                textDecoration: isHidden ? "line-through" : "none",
                transition: "opacity 0.15s ease",
              }}
            >
              <span
                style={{
                  width: "8px",
                  height: "8px",
                  borderRadius: "50%",
                  background: color,
                  display: "inline-block",
                  flexShrink: 0,
                  opacity: isHidden ? 0.4 : 1,
                }}
              />
              <span style={{ lineHeight: 1.3 }}>
                {s.name ?? s.dataKey}
              </span>
              <span
                style={{
                  fontSize: "0.65rem",
                  color: "var(--muted)",
                  fontFamily: "ui-monospace, SFMono-Regular, Menlo, monospace",
                }}
              >
                {total}
              </span>
            </button>
          );
        })}
      </div>
    </div>
  );
}
