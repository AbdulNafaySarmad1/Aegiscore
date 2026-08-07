"use client";

import { useMemo } from "react";
import { useTranslations } from "next-intl";
import type { NChartDataPoint, NChartConfig } from "./types";
import {
  extractNumericValues,
  computeTrend,
  computeDominantSegment,
  computeRecovery,
  formatPercentage,
} from "./types";

interface NChartInsightChipProps {
  data: NChartDataPoint[];
  config: NChartConfig;
  type: string;
}

const TREND_KEYS: Record<string, string> = {
  up: "nchart.insightTrendUp",
  down: "nchart.insightTrendDown",
  flat: "nchart.insightTrendFlat",
};

const RECOVERY_KEYS: Record<string, string> = {
  recovered: "nchart.insightRecoveryYes",
  notRecovered: "nchart.insightRecoveryNo",
};

export function NChartInsightChip({ data, config, type }: NChartInsightChipProps) {
  const t = useTranslations(config.i18nDomain ?? "nchart");

  const insight = useMemo(() => {
    if (!data || data.length === 0) return null;

    const numericValues = extractNumericValues(data, config);
    if (numericValues.length < 2) return null;

    const trend = computeTrend(numericValues);
    if (trend.direction !== "flat") {
      const key = TREND_KEYS[trend.direction] ?? "nchart.insightTrendGeneric";
      return {
        text: t(key, {
          percentage: formatPercentage(trend.percentage),
          measure: t("nchart.measureValue"),
        }),
        tone: trend.direction === "up" ? "green" : trend.direction === "down" ? "red" : "muted",
      };
    }

    const recovery = computeRecovery(numericValues);
    if (recovery) {
      const key = recovery.recovered ? RECOVERY_KEYS.recovered : RECOVERY_KEYS.notRecovered;
      return {
        text: t(key, { percentage: recovery.percentage.toFixed(0) }),
        tone: recovery.recovered ? "green" : "amber" as const,
      };
    }

    return null;
  }, [data, config, t]);

  const dominantSegment = useMemo(() => {
    if (!data || data.length === 0) return null;
    const key = config.nameKey ?? "name";
    const valueKey = config.valueKey ?? config.yKey ?? config.dataKey;
    if (!valueKey) return null;
    const segments = data.map((d) => ({
      name: String(d[key] ?? ""),
      value: Number(d[valueKey]) || 0,
    }));
    const dominant = computeDominantSegment(segments);
    if (!dominant || dominant.share < 20) return null;
    return t("nchart.insightDominant", {
      segment: dominant.name,
      share: dominant.share.toFixed(0),
    });
  }, [data, config, t]);

  if (!insight && !dominantSegment) return null;

  const toneColor: Record<string, string> = {
    green: "var(--green)",
    red: "var(--red)",
    amber: "var(--amber)",
    muted: "var(--muted)",
  };

  const toneBg: Record<string, string> = {
    green: "var(--green-soft)",
    red: "var(--red-soft)",
    amber: "var(--amber-soft)",
    muted: "var(--surface-soft)",
  };

  return (
    <div style={{ display: "flex", gap: "0.5rem", flexWrap: "wrap" }}>
      {insight && (
        <span
          style={{
            padding: "0.25rem 0.625rem",
            borderRadius: "var(--radius, 4px)",
            background: toneBg[insight.tone] ?? "var(--blue-100)",
            color: toneColor[insight.tone] ?? "var(--blue-600)",
            fontSize: "0.75rem",
            lineHeight: 1.5,
            fontWeight: 500,
          }}
        >
          {insight.text}
        </span>
      )}
      {dominantSegment && (
        <span
          style={{
            padding: "0.25rem 0.625rem",
            borderRadius: "var(--radius, 4px)",
            background: "var(--blue-100)",
            color: "var(--blue-600)",
            fontSize: "0.75rem",
            lineHeight: 1.5,
            fontWeight: 500,
          }}
        >
          {dominantSegment}
        </span>
      )}
    </div>
  );
}
