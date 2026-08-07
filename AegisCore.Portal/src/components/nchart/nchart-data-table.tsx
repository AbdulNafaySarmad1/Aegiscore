"use client";

import { useState, useMemo, useCallback } from "react";
import { useTranslations } from "next-intl";
import type { NChartDataPoint, NChartConfig } from "./types";

interface NChartDataTableProps {
  open: boolean;
  data: NChartDataPoint[];
  config: NChartConfig;
}

const SORT_ICONS: Record<string, string> = {
  none: "\u2195",
  asc: "\u2191",
  desc: "\u2193",
};

export function NChartDataTable({ open, data, config }: NChartDataTableProps) {
  const t = useTranslations(config.i18nDomain ?? "nchart");
  const [sortKey, setSortKey] = useState<string | null>(null);
  const [sortDir, setSortDir] = useState<"asc" | "desc">("asc");

  const columns = useMemo(() => {
    if (data.length === 0) return [];
    return Object.keys(data[0]).filter(
      (k) => typeof data[0][k] !== "object" && !k.startsWith("_")
    );
  }, [data]);

  const sortedData = useMemo(() => {
    if (!sortKey) return data;
    return [...data].sort((a, b) => {
      const aVal = a[sortKey];
      const bVal = b[sortKey];
      if (typeof aVal === "number" && typeof bVal === "number") {
        return sortDir === "asc" ? aVal - bVal : bVal - aVal;
      }
      const cmp = String(aVal).localeCompare(String(bVal));
      return sortDir === "asc" ? cmp : -cmp;
    });
  }, [data, sortKey, sortDir]);

  const handleSort = useCallback(
    (key: string) => {
      if (sortKey === key) {
        if (sortDir === "asc") setSortDir("desc");
        else if (sortDir === "desc") {
          setSortKey(null);
          setSortDir("asc");
        }
      } else {
        setSortKey(key);
        setSortDir("asc");
      }
    },
    [sortKey, sortDir]
  );

  const exportCsv = useCallback(() => {
    const header = columns.join(",");
    const rows = sortedData.map((row) =>
      columns.map((col) => {
        const val = row[col];
        const str = String(val ?? "");
        return str.includes(",") ? `"${str}"` : str;
      }).join(",")
    );
    const csv = [header, ...rows].join("\n");
    const blob = new Blob([csv], { type: "text/csv;charset=utf-8" });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = `chart-data-${Date.now()}.csv`;
    link.click();
    URL.revokeObjectURL(url);
  }, [columns, sortedData]);

  if (!open) return null;
  if (data.length === 0) return null;

  return (
    <div style={{ borderTop: "1px solid var(--line)" }}>
      <div
        style={{
          display: "flex",
          justifyContent: "flex-end",
          padding: "0.5rem 1rem",
        }}
      >
        <button
          onClick={exportCsv}
          style={{
            padding: "0.25rem 0.6rem",
            border: "1px solid var(--line)",
            borderRadius: "5px",
            background: "var(--surface)",
            color: "var(--blue-600)",
            fontSize: "0.7rem",
            fontWeight: 600,
            cursor: "pointer",
          }}
          aria-label={t("nchart.exportCsv")}
        >
          {t("nchart.exportCsv")}
        </button>
      </div>
      <div
        style={{
          overflow: "auto",
          maxHeight: "320px",
          borderTop: "1px solid var(--line)",
        }}
        role="table"
        aria-label={t("nchart.dataTable")}
      >
        <div
          role="rowgroup"
          style={{
            position: "sticky",
            top: 0,
            zIndex: 2,
            background: "var(--surface-soft)",
            borderBottom: "1px solid var(--line)",
          }}
        >
          <div role="row" style={{ display: "flex", minWidth: `${columns.length * 140}px` }}>
            {columns.map((col) => (
              <button
                key={col}
                role="columnheader"
                aria-sort={sortKey === col ? (sortDir === "asc" ? "ascending" : "descending") : "none"}
                aria-label={`${t("nchart.sortBy")} ${col}`}
                onClick={() => handleSort(col)}
                style={{
                  flex: `1 1 ${Math.max(120, 100 / columns.length)}%`,
                  minWidth: "100px",
                  padding: "0.5rem 0.75rem",
                  border: "none",
                  background: "transparent",
                  color: "var(--muted)",
                  fontSize: "0.63rem",
                  fontWeight: 800,
                  textTransform: "uppercase",
                  letterSpacing: "0.4px",
                  textAlign: "left",
                  cursor: "pointer",
                  display: "flex",
                  gap: "0.3rem",
                  alignItems: "center",
                }}
              >
                {col}
                <span style={{ fontSize: "0.55rem" }}>
                  {sortKey === col ? SORT_ICONS[sortDir] : SORT_ICONS.none}
                </span>
              </button>
            ))}
          </div>
        </div>
        <div role="rowgroup">
          {sortedData.map((row, rowIdx) => (
            <div
              key={rowIdx}
              role="row"
              aria-rowindex={rowIdx + 2}
              style={{
                display: "flex",
                minWidth: `${columns.length * 140}px`,
                borderBottom:
                  rowIdx < sortedData.length - 1 ? "1px solid var(--line)" : "none",
              }}
            >
              {columns.map((col, colIdx) => (
                <div
                  key={col}
                  role="cell"
                  aria-colindex={colIdx + 1}
                  style={{
                    flex: `1 1 ${Math.max(120, 100 / columns.length)}%`,
                    minWidth: "100px",
                    padding: "0.45rem 0.75rem",
                    fontSize: "0.7rem",
                    color: "var(--ink)",
                    overflow: "hidden",
                    textOverflow: "ellipsis",
                    whiteSpace: "nowrap",
                    fontFamily:
                      typeof row[col] === "number"
                        ? "ui-monospace, SFMono-Regular, Menlo, monospace"
                        : "inherit",
                  }}
                >
                  {typeof row[col] === "number"
                    ? row[col].toLocaleString()
                    : String(row[col] ?? "")}
                </div>
              ))}
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
