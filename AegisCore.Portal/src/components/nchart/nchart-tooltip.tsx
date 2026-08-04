"use client";

import { useMemo } from "react";
import { useTranslations } from "next-intl";
import {
  computeMean,
  detectAnomalies,
} from "./types";
import type { NChartDataPoint, NChartConfig, NChartTooltipPayload } from "./types";

interface NChartTooltipProps {
  active?: boolean;
  payload?: NChartTooltipPayload[];
  label?: string | number;
  data: NChartDataPoint[];
  config: NChartConfig;
}

function anomalyLabel(
  index: number,
  values: number[],
  config: NChartConfig,
  t: ReturnType<typeof useTranslations>
): string | null {
  const anomalies = detectAnomalies(values, 2);
  if (anomalies.has(index)) {
    const mean = computeMean(values);
    const value = values[index];
    return value > mean
      ? t("nchart.tooltipAboveMean", { sigma: "2σ" })
      : t("nchart.tooltipBelowMean", { sigma: "2σ" });
  }
  return null;
}

export function NChartTooltip({ active, payload, label, data: fullData, config }: NChartTooltipProps) {
  const t = useTranslations(config.i18nDomain ?? "nchart");

  const anomalyMessage = useMemo(() => {
    if (!active || !payload || payload.length === 0) return null;
    const key = config.yKey ?? config.dataKey ?? config.valueKey;
    if (!key) return null;
    const values = fullData.map((d) => Number(d[key])).filter((v) => !isNaN(v));
    if (values.length < 3) return null;

    const xKey = config.xKey ?? "label";
    const labelStr = String(label ?? "");
    const dataIndex = fullData.findIndex((d) => String(d[xKey]) === labelStr);
    if (dataIndex < 0) return null;

    return anomalyLabel(dataIndex, values, config, t);
  }, [active, payload, label, fullData, config, t]);

  if (!active || !payload || payload.length === 0) return null;

  return (
    <div
      role="tooltip"
      style={{
        background: "color-mix(in srgb, var(--surface) 97%, transparent)",
        backdropFilter: "blur(12px)",
        border: "1px solid var(--line)",
        borderRadius: "10px",
        padding: "0.75rem 0.875rem",
        minWidth: "180px",
        maxWidth: "280px",
        boxShadow: "var(--shadow-lg)",
        zIndex: 100,
      }}
    >
      {label !== undefined && label !== null && (
        <div
          style={{
            color: "var(--muted)",
            fontSize: "0.7rem",
            fontWeight: 700,
            textTransform: "uppercase",
            letterSpacing: "0.5px",
            marginBottom: "0.5rem",
            borderBottom: "1px solid var(--line)",
            paddingBottom: "0.4rem",
          }}
        >
          {label}
        </div>
      )}

      {payload.map((entry, idx) => (
        <div
          key={`${entry.dataKey}-${idx}`}
          style={{
            display: "flex",
            justifyContent: "space-between",
            alignItems: "center",
            gap: "1rem",
            padding: "0.25rem 0",
          }}
        >
          <span
            style={{
              display: "flex",
              alignItems: "center",
              gap: "0.4rem",
              fontSize: "0.75rem",
              color: "var(--ink)",
            }}
          >
            <span
              style={{
                width: "8px",
                height: "8px",
                borderRadius: "50%",
                background: entry.color ?? "var(--blue-600)",
                flexShrink: 0,
              }}
            />
            {entry.name}
          </span>
          <span
            style={{
              fontFamily: "ui-monospace, SFMono-Regular, Menlo, monospace",
              fontSize: "0.85rem",
              fontWeight: 700,
              color: "var(--ink)",
            }}
          >
            {config.formatY
              ? config.formatY(entry.value)
              : typeof entry.value === "number"
                ? entry.value.toLocaleString()
                : String(entry.value)}
          </span>
        </div>
      ))}

      {anomalyMessage && (
        <div
          style={{
            marginTop: "0.5rem",
            padding: "0.3rem 0.5rem",
            borderRadius: "4px",
            background: "var(--amber-soft)",
            color: "var(--amber)",
            fontSize: "0.65rem",
            fontWeight: 600,
            lineHeight: 1.4,
          }}
        >
          {anomalyMessage}
        </div>
      )}
    </div>
  );
}
