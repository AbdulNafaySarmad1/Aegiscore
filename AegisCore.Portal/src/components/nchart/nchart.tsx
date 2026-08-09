"use client";

import React, { useState, useMemo, useCallback } from "react";
import { useTranslations } from "next-intl";
import {
  Bar,
  BarChart,
  Brush,
  CartesianGrid,
  Cell,
  Line,
  LineChart,
  Pie,
  PieChart,
  ResponsiveContainer,
  Scatter,
  ScatterChart,
  Tooltip,
  XAxis,
  YAxis,
  Area,
  AreaChart,
} from "recharts";

import type { NChartProps, NChartSeriesConfig, NChartDataPoint } from "./types";
import { DEFAULT_COLORS } from "./types";
import { NChartTooltip } from "./nchart-tooltip";
import { NChartZoomLayer } from "./nchart-zoom-layer";
import { NChartLegend } from "./nchart-legend";
import { NChartInsightChip } from "./nchart-insight-chip";
import { NChartDataTable } from "./nchart-data-table";
import { NChartGauge } from "./nchart-gauge";
import { NChartHeatmap } from "./nchart-heatmap";
import { BarChart3, Download, DownloadCloud, Table2 } from "lucide-react";

function inferSeries(data: NChartDataPoint[], config: NChartSeriesConfig[] | undefined): NChartSeriesConfig[] {
  if (config && config.length > 0) return config;
  if (data.length === 0) return [];
  const first = data[0];
  const keys = Object.keys(first).filter(
    (k) => typeof first[k] === "number" && !k.startsWith("_")
  );
  return keys.map((key) => ({ dataKey: key, name: key }));
}

function buildWaterfallData(
  data: NChartDataPoint[],
  config: { dataKey?: string; yKey?: string; xKey?: string; nameKey?: string }
): Array<{ label: string; value: number; fill: string; start: number; end: number }> {
  if (data.length === 0) return [];
  const key = config.dataKey ?? config.yKey ?? "value";
  const labelKey = config.nameKey ?? config.xKey ?? "label";
  let running = 0;
  return data.map((d, idx) => {
    const raw = Number(d[key]) || 0;
    const start = raw >= 0 ? running : running + raw;
    const end = start + raw;
    running = raw >= 0 ? end : start;
    return {
      label: String(d[labelKey] ?? idx),
      value: raw,
      fill: raw >= 0 ? "var(--green)" : "var(--red)",
      start,
      end,
    };
  });
}

export function NChart({
  type,
  data,
  config,
  title,
  subtitle,
  height = 320,
  loading = false,
  error = null,
  onRetry,
}: NChartProps) {
  const t = useTranslations(config.i18nDomain ?? "nchart");
  const [dataTableOpen, setDataTableOpen] = useState(false);
  const [hiddenSeries, setHiddenSeries] = useState<Set<string>>(new Set());
  const [exportMenuOpen, setExportMenuOpen] = useState(false);

  const series = useMemo(() => inferSeries(data, config.series), [data, config.series]);

  const filteredData = useMemo(() => {
    if (hiddenSeries.size === 0) return data;
    return data.map((d) => {
      const entry: Record<string, string | number | undefined> = { ...d };
      hiddenSeries.forEach((key) => {
        delete entry[key];
      });
      return entry as NChartDataPoint;
    });
  }, [data, hiddenSeries]);

  const handleToggleSeries = useCallback((key: string) => {
    setHiddenSeries((prev) => {
      const next = new Set(prev);
      if (next.has(key)) next.delete(key);
      else next.add(key);
      return next;
    });
  }, []);

  const exportAsPNG = useCallback(() => {
    const svg = document.querySelector(".nchart-svg-root");
    if (!svg) return; // no chart rendered yet — nothing to export
    const clone = svg.cloneNode(true) as SVGSVGElement;
    const data = new XMLSerializer().serializeToString(clone);
    const canvas = document.createElement("canvas");
    canvas.width = 800;
    canvas.height = 600;
    const ctx = canvas.getContext("2d");
    if (!ctx) return;
    const img = new Image();
    const blob = new Blob([data], { type: "image/svg+xml;charset=utf-8" });
    const url = URL.createObjectURL(blob);
    img.onload = () => {
      ctx.drawImage(img, 0, 0);
      URL.revokeObjectURL(url);
      const png = canvas.toDataURL("image/png");
      const link = document.createElement("a");
      link.href = png;
      link.download = `${title ?? "chart"}-${Date.now()}.png`;
      link.click();
      setExportMenuOpen(false);
    };
    img.src = url;
  }, [title, t]);

  const chartColors = config.colors ?? DEFAULT_COLORS;

  const renderChart = () => {
    if (type === "gauge") {
      return <NChartGauge data={data} config={config} height={height} />;
    }

    if (type === "heatmap") {
      return <NChartHeatmap data={data} config={config} height={height} />;
    }

    if (type === "waterfall") {
      const waterfallData = buildWaterfallData(data, config);
      return (
        <ResponsiveContainer width="100%" height={height}>
          <BarChart data={waterfallData} className="nchart-svg-root">
            {config.showGrid !== false && <CartesianGrid stroke="var(--line)" strokeDasharray="3 3" />}
            <XAxis
              dataKey="label"
              stroke="var(--muted)"
              tick={{ fontSize: "0.65rem" }}
              tickFormatter={config.formatX ? (v) => String(config.formatX!(v)) : undefined}
            />
            <YAxis stroke="var(--muted)" tick={{ fontSize: "0.65rem" }} />
            <Tooltip
              content={<NChartTooltip data={data} config={config} />}
            />
            <Bar dataKey="end" radius={[3, 3, 0, 0]}>
              {waterfallData.map((entry, idx) => (
                <Cell key={idx} fill={entry.fill} />
              ))}
            </Bar>
          </BarChart>
        </ResponsiveContainer>
      );
    }

    if (type === "gantt") {
      return (
        <div
          style={{
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            height,
            color: "var(--muted)",
            fontSize: "0.8rem",
          }}
        >
          {t("nchart.ganttPlaceholder")}
        </div>
      );
    }

    if (type === "bullet") {
      const valueKey = config.bullet?.dataKey ?? config.dataKey ?? config.yKey ?? "value";
      const targetKey = config.bullet?.targetKey ?? "target";
      const value = data.length > 0 ? Number(data[0][valueKey]) || 0 : 0;
      const target = data.length > 0 ? Number(data[0][targetKey]) || 0 : 0;
      const pct = target > 0 ? Math.min(100, (value / target) * 100) : 0;

      return (
        <div
          style={{
            display: "flex",
            flexDirection: "column",
            justifyContent: "center",
            height,
            gap: "0.75rem",
            padding: "1.5rem",
          }}
        >
          <div
            style={{
              display: "flex",
              justifyContent: "space-between",
              fontSize: "0.8rem",
              color: "var(--ink)",
            }}
          >
            <span>{data.length > 0 ? String(data[0][config.nameKey ?? "name"] ?? "") : ""}</span>
            <span style={{ fontFamily: "ui-monospace, monospace", fontWeight: 700 }}>
              {config.formatY ? config.formatY(value) : value.toLocaleString()}
              {" / "}
              {config.formatY ? config.formatY(target) : target.toLocaleString()}
            </span>
          </div>
          <div
            style={{
              height: "22px",
              borderRadius: "11px",
              background: "var(--surface-soft)",
              border: "1px solid var(--line)",
              overflow: "hidden",
              position: "relative",
            }}
          >
            <div
              style={{
                position: "absolute",
                height: "100%",
                width: `${pct}%`,
                borderRadius: "11px",
                background:
                  pct >= 80 ? "var(--green)" : pct >= 50 ? "var(--amber)" : "var(--red)",
                transition: "width 0.6s ease",
              }}
            />
            <div
              style={{
                position: "absolute",
                left: "66%",
                top: "-2px",
                bottom: "-2px",
                width: "3px",
                background: "var(--ink)",
                borderRadius: "2px",
              }}
            />
          </div>
          <div
            style={{
              display: "flex",
              justifyContent: "space-between",
              fontSize: "0.65rem",
              color: "var(--muted)",
            }}
          >
            <span>0</span>
            <span>{pct.toFixed(0)}%</span>
            <span>{target.toLocaleString()}</span>
          </div>
        </div>
      );
    }

    if (type === "donut") {
      const nameKey = config.nameKey ?? "name";
      const dataKey = config.dataKey ?? config.valueKey ?? "value";
      return (
        <ResponsiveContainer width="100%" height={height} className="nchart-svg-root">
          <PieChart>
            <Pie
              data={filteredData}
              dataKey={dataKey}
              nameKey={nameKey}
              cx="50%"
              cy="50%"
              innerRadius={config.innerRadius ?? height * 0.2}
              outerRadius={config.outerRadius ?? height * 0.35}
              label={
                config.showTooltip !== false
                  ? ({ name, percent }) => `${name} ${((percent ?? 0) * 100).toFixed(0)}%`
                  : undefined
              }
            >
              {filteredData.map((_, idx) => (
                <Cell
                  key={idx}
                  fill={chartColors[idx % chartColors.length]}
                  stroke="var(--surface)"
                />
              ))}
            </Pie>
            <Tooltip />
          </PieChart>
        </ResponsiveContainer>
      );
    }

    if (type === "treemap") {
      const nameKey = config.nameKey ?? "name";
      const valueKey = config.valueKey ?? config.dataKey ?? "value";
      const total = data.reduce((sum, d) => sum + (Number(d[valueKey]) || 0), 0);
      return (
        <div
          className="nchart-svg-root"
          style={{
            display: "grid",
            gridTemplateColumns: `repeat(auto-fill, minmax(${Math.max(60, height / 3)}px, 1fr))`,
            gap: "3px",
            height,
            overflow: "hidden",
            padding: "3px",
          }}
        >
          {data.map((d, idx) => {
            const val = Number(d[valueKey]) || 0;
            const pct = total > 0 ? (val / total) * 100 : 0;
            return (
              <div
                key={idx}
                style={{
                  background: chartColors[idx % chartColors.length],
                  color: "#fff",
                  display: "flex",
                  flexDirection: "column",
                  alignItems: "center",
                  justifyContent: "center",
                  borderRadius: "6px",
                  padding: "0.5rem",
                  fontSize: "0.65rem",
                  fontWeight: 600,
                  flexBasis: `${Math.max(5, pct)}%`,
                  flexGrow: 1,
                  minWidth: "40px",
                }}
                title={`${String(d[nameKey] ?? "")}: ${val}`}
              >
                <span style={{ fontSize: "0.6rem", opacity: 0.9 }}>
                  {String(d[nameKey] ?? "")}
                </span>
                <span style={{ fontSize: "0.75rem", fontWeight: 700 }}>
                  {config.formatY ? config.formatY(val) : val.toLocaleString()}
                </span>
              </div>
            );
          })}
        </div>
      );
    }

    if (type === "histogram") {
      const dataKey = config.dataKey ?? config.yKey ?? "value";
      const xKey = config.xKey ?? "label";
      return (
        <ResponsiveContainer width="100%" height={height} className="nchart-svg-root">
          <BarChart data={filteredData}>
            {config.showGrid !== false && <CartesianGrid stroke="var(--line)" strokeDasharray="3 3" />}
            <XAxis
              dataKey={xKey}
              stroke="var(--muted)"
              tick={{ fontSize: "0.65rem" }}
            />
            <YAxis stroke="var(--muted)" tick={{ fontSize: "0.65rem" }} />
            <Tooltip
              content={<NChartTooltip data={data} config={config} />}
            />
            <Bar dataKey={dataKey} fill={chartColors[0]} radius={[3, 3, 0, 0]}>
              {(data as Array<Record<string, unknown>>).map((_, idx) => (
                <Cell key={idx} fill={chartColors[0]} />
              ))}
            </Bar>
          </BarChart>
        </ResponsiveContainer>
      );
    }

    if (type === "scatter") {
      const xKey = config.xKey ?? "x";
      const yKey = config.yKey ?? "y";
      return (
        <ResponsiveContainer width="100%" height={height} className="nchart-svg-root">
          <ScatterChart>
            {config.showGrid !== false && <CartesianGrid stroke="var(--line)" strokeDasharray="3 3" />}
            <XAxis
              dataKey={xKey}
              stroke="var(--muted)"
              tick={{ fontSize: "0.65rem" }}
              name={xKey}
            />
            <YAxis
              dataKey={yKey}
              stroke="var(--muted)"
              tick={{ fontSize: "0.65rem" }}
              name={yKey}
            />
            <Tooltip
              content={<NChartTooltip data={data} config={config} />}
            />
            {series.length > 0 && series.map((s, idx) => (
              <Scatter
                key={s.dataKey}
                name={s.name ?? s.dataKey}
                data={data}
                fill={s.color ?? chartColors[idx % chartColors.length]}
              />
            ))}
            {series.length === 0 && (
              <Scatter
                data={data}
                fill={chartColors[0]}
              />
            )}
          </ScatterChart>
        </ResponsiveContainer>
      );
    }

    const useLine = type === "line";
    const useArea = type === "area";
    const xKey = config.xKey ?? "label";
    const layout = config.layout === "vertical" ? "vertical" : "horizontal";

    if (layout === "vertical") {
      return (
        <ResponsiveContainer width="100%" height={height} className="nchart-svg-root">
          <BarChart data={filteredData} layout="vertical">
            {config.showGrid !== false && <CartesianGrid stroke="var(--line)" strokeDasharray="3 3" />}
            <XAxis type="number" stroke="var(--muted)" tick={{ fontSize: "0.65rem" }} />
            <YAxis
              dataKey={xKey}
              type="category"
              stroke="var(--muted)"
              tick={{ fontSize: "0.65rem" }}
              width={140}
            />
            <Tooltip
              content={<NChartTooltip data={data} config={config} />}
            />
            {series.map((s, idx) => (
              <Bar
                key={s.dataKey}
                dataKey={s.dataKey}
                name={s.name ?? s.dataKey}
                fill={s.color ?? chartColors[idx % chartColors.length]}
                radius={[0, 3, 3, 0]}
                stackId={config.stacked ? s.stackId ?? "stack" : undefined}
              />
            ))}
          </BarChart>
        </ResponsiveContainer>
      );
    }

    const ChartComponent = useLine ? LineChart : useArea ? AreaChart : BarChart;

    return (
      <ResponsiveContainer width="100%" height={height} className="nchart-svg-root">
        <ChartComponent data={filteredData}>
          {config.showGrid !== false && (
            <CartesianGrid stroke="var(--line)" strokeDasharray="3 3" />
          )}
          <XAxis
            dataKey={xKey}
            stroke="var(--muted)"
            tick={{ fontSize: "0.65rem" }}
            tickFormatter={
              config.formatX
                ? (v: unknown) => String(config.formatX!(v))
                : undefined
            }
          />
          <YAxis
            stroke="var(--muted)"
            tick={{ fontSize: "0.65rem" }}
            tickFormatter={
              config.formatY
                ? (v: unknown) => String(config.formatY!(v))
                : undefined
            }
          />
          <Tooltip
            content={<NChartTooltip data={data} config={config} />}
          />
          {series.map((s, idx) => {
            const color = s.color ?? chartColors[idx % chartColors.length];
            const name = s.name ?? s.dataKey;

            if (useLine) {
              return (
                <Line
                  key={s.dataKey}
                  type="monotone"
                  dataKey={s.dataKey}
                  name={name}
                  stroke={color}
                  strokeWidth={1.5}
                  dot={data.length <= 50}
                  strokeDasharray={s.strokeDasharray}
                  isAnimationActive={config.animate !== false}
                />
              );
            }

            if (useArea) {
              return (
                <Area
                  key={s.dataKey}
                  type="monotone"
                  dataKey={s.dataKey}
                  name={name}
                  stroke={color}
                  fill={color}
                  fillOpacity={0.15}
                  strokeWidth={1.5}
                  dot={data.length <= 50}
                  strokeDasharray={s.strokeDasharray}
                  stackId={config.stacked ? s.stackId ?? "stack" : undefined}
                  isAnimationActive={config.animate !== false}
                />
              );
            }

            return (
              <Bar
                key={s.dataKey}
                dataKey={s.dataKey}
                name={name}
                fill={color}
                radius={[3, 3, 0, 0] as [number, number, number, number]}
                stackId={config.stacked ? s.stackId ?? "stack" : undefined}
                isAnimationActive={config.animate !== false}
              />
            );
          })}
          {config.showBrush && (
            <Brush dataKey={xKey} height={24} stroke="var(--blue-600)" fill="var(--surface)" />
          )}
        </ChartComponent>
      </ResponsiveContainer>
    );
  };

  if (loading) {
    return (
      <div
        style={{
          background: "var(--surface)",
          borderRadius: "12px",
          border: "1px solid var(--line)",
          overflow: "hidden",
          height: height + 80,
        }}
      >
        <div
          style={{
            padding: "1rem 1.25rem",
            borderBottom: "1px solid var(--line)",
            display: "flex",
            alignItems: "center",
            gap: "0.75rem",
          }}
        >
          <div
            style={{
              width: "120px",
              height: "14px",
              borderRadius: "4px",
              background: "var(--surface-soft)",
            }}
          />
          <div
            style={{
              width: "80px",
              height: "10px",
              borderRadius: "4px",
              background: "var(--surface-soft)",
            }}
          />
        </div>
        <div
          style={{
            height,
            background: `repeating-linear-gradient(90deg, var(--surface-soft) 0px, var(--surface-soft) 1px, transparent 1px, transparent ${height / 10}px)`,
          }}
        />
      </div>
    );
  }

  if (error) {
    return (
      <div
        style={{
          background: "var(--surface)",
          borderRadius: "12px",
          border: "1px solid var(--line)",
          overflow: "hidden",
          height: height + 120,
          display: "flex",
          flexDirection: "column",
          alignItems: "center",
          justifyContent: "center",
          gap: "0.75rem",
        }}
      >
        <div
          style={{
            width: "36px",
            height: "36px",
            borderRadius: "50%",
            background: "var(--red-soft)",
            color: "var(--red)",
            display: "grid",
            placeItems: "center",
          }}
        >
          !
        </div>
        <p style={{ color: "var(--muted)", fontSize: "0.85rem", margin: 0 }}>
          {t("nchart.errorMessage")}
        </p>
        <p style={{ color: "var(--red)", fontSize: "0.75rem", margin: 0, maxWidth: "320px", textAlign: "center" }}>
          {error}
        </p>
        {onRetry && (
          <button
            onClick={onRetry}
            style={{
              padding: "0.4rem 0.9rem",
              border: "1px solid var(--line)",
              borderRadius: "6px",
              background: "var(--surface)",
              color: "var(--blue-600)",
              cursor: "pointer",
              fontSize: "0.75rem",
              fontWeight: 600,
            }}
          >
            {t("nchart.retry")}
          </button>
        )}
      </div>
    );
  }

  if (!data || data.length === 0) {
    return (
      <div
        style={{
          background: "var(--surface)",
          borderRadius: "12px",
          border: "1px solid var(--line)",
          overflow: "hidden",
          height: height + 120,
          display: "flex",
          flexDirection: "column",
          alignItems: "center",
          justifyContent: "center",
          gap: "0.75rem",
        }}
      >
        <BarChart3 size={28} color="var(--subtle)" />
        <p style={{ color: "var(--muted)", fontSize: "0.85rem", margin: 0 }}>
          {t("nchart.emptyTitle")}
        </p>
        <p style={{ color: "var(--subtle)", fontSize: "0.72rem", margin: 0 }}>
          {t("nchart.emptySubtitle")}
        </p>
      </div>
    );
  }

  return (
    <div
      style={{
        background: "var(--surface)",
        borderRadius: "12px",
        border: "1px solid var(--line)",
        overflow: "hidden",
      }}
    >
      <div
        style={{
          padding: "1rem 1.25rem",
          borderBottom: "1px solid var(--line)",
          display: "flex",
          justifyContent: "space-between",
          alignItems: "flex-start",
          gap: "1rem",
        }}
      >
        <div style={{ flex: 1, minWidth: 0 }}>
          {title && (
            <h3
              style={{
                color: "var(--ink)",
                margin: 0,
                fontSize: "0.95rem",
                fontWeight: 600,
                lineHeight: 1.3,
              }}
            >
              {title}
            </h3>
          )}
          {subtitle && (
            <p
              style={{
                color: "var(--muted)",
                margin: "0.25rem 0 0 0",
                fontSize: "0.8rem",
              }}
            >
              {subtitle}
            </p>
          )}
          <div style={{ marginTop: "0.5rem" }}>
            <NChartInsightChip data={data} config={config} type={type} />
          </div>
        </div>

        <div style={{ display: "flex", gap: "0.375rem", flexShrink: 0 }}>
          <button
            onClick={() => setDataTableOpen((v) => !v)}
            style={actionBtnStyle}
            title={t("nchart.toggleDataTable")}
            aria-label={t("nchart.toggleDataTable")}
            aria-expanded={dataTableOpen}
          >
            <Table2 size={14} />
          </button>

          <div style={{ position: "relative" }}>
            <button
              onClick={() => setExportMenuOpen((v) => !v)}
              style={actionBtnStyle}
              title={t("nchart.export")}
              aria-label={t("nchart.export")}
              aria-haspopup="true"
              aria-expanded={exportMenuOpen}
            >
              <Download size={14} />
            </button>
            {exportMenuOpen && (
              <div
                style={{
                  position: "absolute",
                  top: "100%",
                  right: 0,
                  marginTop: "4px",
                  background: "var(--surface)",
                  border: "1px solid var(--line)",
                  borderRadius: "8px",
                  boxShadow: "var(--shadow-lg)",
                  padding: "0.3rem",
                  zIndex: 50,
                  minWidth: "140px",
                }}
              >
                <button
                  onClick={exportAsPNG}
                  style={menuBtnStyle}
                >
                  <DownloadCloud size={12} />
                  {t("nchart.exportPng")}
                </button>
                <button
                  onClick={() => {
                    setDataTableOpen(true);
                    setExportMenuOpen(false);
                  }}
                  style={menuBtnStyle}
                >
                  <Table2 size={12} />
                  {t("nchart.exportCsv")}
                </button>
              </div>
            )}
          </div>
        </div>
      </div>

      {config.showLegend && config.legendPosition === "top" && (
        <NChartLegend
          config={config}
          data={data}
          series={series}
          hiddenSeries={hiddenSeries}
          onToggleSeries={handleToggleSeries}
        />
      )}

      {config.legendPosition === "right" ? (
        <div style={{ display: "flex" }}>
          <div style={{ flex: 1, minWidth: 0 }}>
            <NChartZoomLayer preserveKey={title}>
              {renderChart()}
            </NChartZoomLayer>
          </div>
          <NChartLegend
            config={config}
            data={data}
            series={series}
            hiddenSeries={hiddenSeries}
            onToggleSeries={handleToggleSeries}
          />
        </div>
      ) : (
        <NChartZoomLayer preserveKey={title}>
          {renderChart()}
        </NChartZoomLayer>
      )}

      {config.showLegend && (
        config.legendPosition === undefined || config.legendPosition === "bottom"
      ) && (
        <NChartLegend
          config={config}
          data={data}
          series={series}
          hiddenSeries={hiddenSeries}
          onToggleSeries={handleToggleSeries}
        />
      )}

      <div
        style={{
          borderTop: "1px solid var(--line)",
          padding: "0.5rem 1rem",
          display: "flex",
          justifyContent: "flex-end",
        }}
      >
        <button
          onClick={() => setDataTableOpen((v) => !v)}
          style={{
            padding: "0.2rem 0.5rem",
            border: "1px solid var(--line)",
            borderRadius: "5px",
            background: "var(--surface)",
            color: "var(--muted)",
            cursor: "pointer",
            fontSize: "0.65rem",
            display: "flex",
            alignItems: "center",
            gap: "0.3rem",
          }}
          aria-expanded={dataTableOpen}
        >
          <Table2 size={11} />
          {t("nchart.dataTable")}
        </button>
      </div>

      <NChartDataTable
        open={dataTableOpen}
        data={data}
        config={config}
      />
    </div>
  );
}

const actionBtnStyle: React.CSSProperties = {
  padding: "0.25rem 0.5rem",
  borderRadius: "6px",
  border: "1px solid var(--line)",
  background: "var(--canvas)",
  color: "var(--ink)",
  cursor: "pointer",
  fontSize: "0.8rem",
  display: "flex",
  alignItems: "center",
  justifyContent: "center",
  width: "32px",
  height: "32px",
};

const menuBtnStyle: React.CSSProperties = {
  display: "flex",
  alignItems: "center",
  gap: "0.4rem",
  padding: "0.35rem 0.6rem",
  border: "none",
  borderRadius: "5px",
  background: "transparent",
  color: "var(--ink)",
  cursor: "pointer",
  fontSize: "0.7rem",
  width: "100%",
  textAlign: "left",
};
