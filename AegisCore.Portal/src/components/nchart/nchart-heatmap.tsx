"use client";

import { useMemo, useState, useCallback } from "react";
import { useTranslations } from "next-intl";
import type { NChartDataPoint, NChartConfig } from "./types";

interface NChartHeatmapProps {
  data: NChartDataPoint[];
  config: NChartConfig;
  height: number;
}

interface Cell {
  date: string;
  value: number;
  day: number;
  week: number;
  month: number;
  label: string;
}

function parseDate(str: string): Date | null {
  const d = new Date(str);
  return isNaN(d.getTime()) ? null : d;
}

function getMonthLabel(monthIdx: number): string {
  const labels = [
    "Jan", "Feb", "Mar", "Apr", "May", "Jun",
    "Jul", "Aug", "Sep", "Oct", "Nov", "Dec",
  ];
  return labels[monthIdx] ?? "";
}

function getDayLabel(dayIdx: number): string {
  const labels = ["Mon", "", "Wed", "", "Fri", "", ""];
  return labels[dayIdx] ?? "";
}

const INTENSITY_STEPS = 5;

export function NChartHeatmap({ data, config, height }: NChartHeatmapProps) {
  const t = useTranslations(config.i18nDomain ?? "nchart");
  const [hoveredCell, setHoveredCell] = useState<Cell | null>(null);

  const cells = useMemo(() => {
    if (data.length === 0) return [] as Cell[];
    const dateKey = config.xKey ?? "date";
    const valueKey = config.yKey ?? config.valueKey ?? "value";

    const parsed = data
      .map((d) => {
        const dateStr = String(d[dateKey] ?? "");
        const date = parseDate(dateStr);
        if (!date) return null;
        const value = Number(d[valueKey]) || 0;
        const dayOfWeek = date.getDay();
        const adjustedDay = dayOfWeek === 0 ? 6 : dayOfWeek - 1;
        return {
          date: dateStr,
          value,
          day: adjustedDay,
          week: -1,
          month: date.getMonth(),
          label: date.toLocaleDateString(),
        };
      })
      .filter(Boolean) as Cell[];

    if (parsed.length === 0) return [];

    parsed.sort((a, b) => a.date.localeCompare(b.date));

    const firstDate = parseDate(parsed[0].date);
    if (!firstDate) return parsed;

    const firstDay = firstDate.getDay();
    const adjustedFirstDay = firstDay === 0 ? 6 : firstDay - 1;

    const startOfWeek = new Date(firstDate);
    startOfWeek.setDate(startOfWeek.getDate() - adjustedFirstDay);

    parsed.forEach((cell) => {
      const cellDate = parseDate(cell.date);
      if (!cellDate) return;
      const diffMs = cellDate.getTime() - startOfWeek.getTime();
      cell.week = Math.floor(diffMs / (7 * 24 * 60 * 60 * 1000));
    });

    return parsed;
  }, [data, config]);

  const monthLabels = useMemo(() => {
    if (cells.length === 0) return [] as Array<{ label: string; week: number }>;
    const seen = new Set<number>();
    const labels: Array<{ label: string; week: number }> = [];
    cells.forEach((cell) => {
      if (!seen.has(cell.month)) {
        seen.add(cell.month);
        labels.push({ label: getMonthLabel(cell.month), week: cell.week });
      }
    });
    return labels;
  }, [cells]);

  const maxWeeks = useMemo(() => {
    if (cells.length === 0) return 0;
    return Math.max(...cells.map((c) => c.week)) + 1;
  }, [cells]);

  const values = cells.map((c) => c.value);
  const maxValue = Math.max(1, ...values);
  const minValue = Math.min(...values);

  const getIntensity = useCallback(
    (value: number) => {
      if (maxValue === minValue) return INTENSITY_STEPS;
      const ratio = (value - minValue) / (maxValue - minValue);
      return Math.ceil(ratio * INTENSITY_STEPS);
    },
    [maxValue, minValue]
  );

  const cellMap = useMemo(() => {
    const map = new Map<string, Cell>();
    cells.forEach((c) => map.set(`${c.week}:${c.day}`, c));
    return map;
  }, [cells]);

  if (cells.length === 0) return null;

  const cellSize = Math.min(
    Math.max(10, (height - 30) / 8),
    22
  );

  return (
    <div
      role="img"
      aria-label={t("nchart.heatmap")}
      style={{
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        height,
        gap: "0.5rem",
        padding: "0.5rem",
      }}
    >
      <div style={{ position: "relative" }}>
        <div
          style={{
            display: "grid",
            gridTemplateColumns: `20px repeat(${maxWeeks}, ${cellSize + 3}px)`,
            gap: 0,
            marginBottom: "4px",
            paddingLeft: "24px",
          }}
        >
          <div />
          {monthLabels.map((ml, idx) => (
            <div
              key={`${ml.label}-${idx}`}
              style={{
                gridColumn: `${ml.week + 2}`,
                fontSize: "0.55rem",
                color: "var(--muted)",
                fontWeight: 600,
                lineHeight: 1,
              }}
            >
              {ml.label}
            </div>
          ))}
        </div>

        <div
          style={{
            display: "grid",
            gridTemplateColumns: `20px repeat(${maxWeeks}, ${cellSize + 3}px)`,
            gap: 0,
          }}
        >
          {Array.from({ length: 7 }).map((_, day) => (
            <div key={`day-${day}`}>
              <div
                style={{
                  height: cellSize + 3,
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "flex-end",
                  fontSize: "0.5rem",
                  color: "var(--muted)",
                  paddingRight: "4px",
                }}
              >
                {day % 2 === 0 ? getDayLabel(day) : ""}
              </div>
              {Array.from({ length: maxWeeks }).map((_, week) => {
                const cell = cellMap.get(`${week}:${day}`);
                const intensity = cell ? getIntensity(cell.value) : 0;

                return (
                  <div
                    key={`${week}-${day}`}
                    onMouseEnter={() => cell && setHoveredCell(cell)}
                    onMouseLeave={() => setHoveredCell(null)}
                    style={{
                      width: cellSize,
                      height: cellSize,
                      borderRadius: "3px",
                      margin: "1.5px",
                      background: cell
                        ? `color-mix(in srgb, var(--green) ${intensity * 20}%, var(--surface-soft))`
                        : "var(--surface-soft)",
                      border: cell ? "1px solid color-mix(in srgb, var(--green) 40%, transparent)" : "1px solid transparent",
                      transition: "transform 0.1s ease",
                      cursor: cell ? "pointer" : "default",
                    }}
                  >
                    {cell && <title>{`${cell.label}: ${cell.value}`}</title>}
                  </div>
                );
              })}
            </div>
          ))}
        </div>
      </div>

      {hoveredCell && (
        <div
          style={{
            background: "var(--surface)",
            border: "1px solid var(--line)",
            borderRadius: "6px",
            padding: "0.4rem 0.6rem",
            fontSize: "0.7rem",
            color: "var(--ink)",
            boxShadow: "var(--shadow)",
            whiteSpace: "nowrap",
          }}
        >
          <strong>{hoveredCell.label}</strong>
          <span style={{ marginLeft: "0.5rem", fontFamily: "ui-monospace, monospace" }}>
            {hoveredCell.value.toLocaleString()}
          </span>
        </div>
      )}

      <div
        style={{
          display: "flex",
          flexDirection: "column",
          alignItems: "center",
          gap: "2px",
          marginLeft: "0.5rem",
        }}
        aria-hidden="true"
      >
        <span style={{ fontSize: "0.5rem", color: "var(--muted)" }}>{t("nchart.heatmapMore")}</span>
        {Array.from({ length: INTENSITY_STEPS }).map((_, i) => (
          <div
            key={i}
            style={{
              width: "10px",
              height: "10px",
              borderRadius: "2px",
              background: `color-mix(in srgb, var(--green) ${(i + 1) * 20}%, var(--surface-soft))`,
            }}
          />
        ))}
        <span style={{ fontSize: "0.5rem", color: "var(--muted)" }}>{t("nchart.heatmapLess")}</span>
      </div>
    </div>
  );
}
