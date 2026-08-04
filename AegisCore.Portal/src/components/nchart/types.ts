"use client";

export type NChartType =
  | "line"
  | "area"
  | "bar"
  | "donut"
  | "treemap"
  | "scatter"
  | "histogram"
  | "heatmap"
  | "waterfall"
  | "gauge"
  | "gantt"
  | "bullet";

export type NChartDataPoint = Record<string, string | number>;

export interface NChartSeriesConfig {
  dataKey: string;
  name?: string;
  color?: string;
  strokeDasharray?: string;
  yAxisId?: string;
  stackId?: string;
  hidden?: boolean;
}

export interface NChartGaugeConfig {
  zones?: Array<{ color: string; from: number; to: number; label?: string }>;
  target?: number;
  label?: string;
  format?: (value: number) => string;
  max?: number;
}

export interface NChartHeatmapConfig {
  from?: string;
  to?: string;
}

export interface NChartWaterfallConfig {
  totalKey?: string;
  showTotal?: boolean;
  positiveTotalLabel?: string;
  negativeTotalLabel?: string;
}

export interface NChartGanttConfig {
  startKey: string;
  endKey: string;
  labelKey?: string;
  groupKey?: string;
  showToday?: boolean;
}

export interface NChartBulletConfig {
  dataKey: string;
  targetKey?: string;
  rangeColors?: string[];
}

export interface NChartConfig {
  xKey?: string;
  yKey?: string;
  nameKey?: string;
  valueKey?: string;
  dataKey?: string;
  sizeKey?: string;
  categoryKey?: string;

  series?: NChartSeriesConfig[];
  colors?: string[];
  showLegend?: boolean;
  legendPosition?: "top" | "bottom" | "right" | "none";
  showTooltip?: boolean;
  showGrid?: boolean;
  showBrush?: boolean;
  showZoom?: boolean;
  animate?: boolean;
  stacked?: boolean;
  innerRadius?: number;
  outerRadius?: number;
  layout?: "horizontal" | "vertical";

  gauge?: NChartGaugeConfig;
  heatmap?: NChartHeatmapConfig;
  waterfall?: NChartWaterfallConfig;
  gantt?: NChartGanttConfig;
  bullet?: NChartBulletConfig;

  formatX?: (value: unknown) => string;
  formatY?: (value: unknown) => string;
  i18nDomain?: string;
}

export interface NChartProps {
  type: NChartType;
  data: NChartDataPoint[];
  config: NChartConfig;
  title?: string;
  subtitle?: string;
  height?: number;
  loading?: boolean;
  error?: string | null;
  onRetry?: () => void;
}

export interface NChartTooltipPayload {
  name: string;
  value: number | string;
  color: string;
  dataKey: string;
  unit?: string;
}

export interface NChartTooltipContext {
  active: boolean;
  payload: NChartTooltipPayload[];
  label: string | number;
  x: number;
  y: number;
  viewport: { width: number; height: number };
}

export interface NChartZoomState {
  scale: number;
  translateX: number;
  translateY: number;
  brushStart: number | null;
  brushEnd: number | null;
}

export const DEFAULT_COLORS = [
  "var(--blue-600)",
  "var(--green)",
  "var(--amber)",
  "var(--red)",
  "var(--violet)",
  "#0891b2",
  "#d946ef",
  "#f97316",
  "#84cc16",
  "#06b6d4",
];

export const CHART_ZOOM_SESSION_KEY = "nchart:zoom";

export function computeMean(values: number[]): number {
  if (values.length === 0) return 0;
  return values.reduce((a, b) => a + b, 0) / values.length;
}

export function computeStdDev(values: number[], mean: number): number {
  if (values.length < 2) return 0;
  const variance =
    values.reduce((sum, v) => sum + (v - mean) ** 2, 0) / values.length;
  return Math.sqrt(variance);
}

export function detectAnomalies(
  values: number[],
  threshold = 2
): Set<number> {
  const mean = computeMean(values);
  const stdDev = computeStdDev(values, mean);
  if (stdDev === 0) return new Set();
  const anomalies = new Set<number>();
  values.forEach((v, i) => {
    if (Math.abs(v - mean) > threshold * stdDev) anomalies.add(i);
  });
  return anomalies;
}

export function detectZeroWhenNonNull(
  values: number[],
  previousValues: number[]
): Set<number> {
  const flags = new Set<number>();
  for (let i = 0; i < values.length; i++) {
    if (values[i] === 0 && (previousValues[i] ?? 0) !== 0) {
      flags.add(i);
    }
  }
  return flags;
}

export function detectReversals(values: number[]): Set<number> {
  const flags = new Set<number>();
  for (let i = 1; i < values.length - 1; i++) {
    const prev = values[i - 1];
    const curr = values[i];
    const next = values[i + 1];
    if (
      (prev < curr && curr > next && next < prev) ||
      (prev > curr && curr < next && next > prev)
    ) {
      flags.add(i);
    }
  }
  return flags;
}

export function computeTrend(
  values: number[]
): { direction: "up" | "down" | "flat"; percentage: number } {
  if (values.length < 2) return { direction: "flat", percentage: 0 };
  const first = values[0];
  const last = values[values.length - 1];
  if (first === 0)
    return { direction: last > 0 ? "up" : last < 0 ? "down" : "flat", percentage: 0 };
  const change = ((last - first) / Math.abs(first)) * 100;
  return {
    direction: change > 0 ? "up" : change < 0 ? "down" : "flat",
    percentage: Math.abs(change),
  };
}

export function computeDominantSegment(
  data: Array<{ name: string; value: number }>
): { name: string; share: number } | null {
  if (data.length === 0) return null;
  const total = data.reduce((s, d) => s + d.value, 0);
  if (total === 0) return null;
  const best = data.reduce((a, b) => (a.value > b.value ? a : b));
  return { name: best.name, share: (best.value / total) * 100 };
}

export function computeRecovery(
  values: number[]
): { recovered: boolean; percentage: number } | null {
  if (values.length < 2) return null;
  const drop = values.reduce((acc, v, i) => {
    if (i === 0) return acc;
    return v < values[i - 1] ? acc + 1 : acc;
  }, 0);
  if (drop === 0) return null;
  const trough = Math.min(...values);
  const maxPoint = Math.max(...values);
  const recovery =
    maxPoint === trough ? 100 : ((values[values.length - 1] - trough) / (maxPoint - trough)) * 100;
  return {
    recovered: recovery >= 50,
    percentage: Math.max(0, Math.min(100, recovery)),
  };
}

export function extractNumericValues(
  data: NChartDataPoint[],
  config: NChartConfig
): number[] {
  const key = config.yKey ?? config.dataKey ?? config.valueKey;
  if (!key) return [];
  return data.map((d) => Number(d[key])).filter((v) => !isNaN(v));
}

export function formatValueCompact(value: number): string {
  if (Math.abs(value) >= 1_000_000) return `${(value / 1_000_000).toFixed(1)}M`;
  if (Math.abs(value) >= 1_000) return `${(value / 1_000).toFixed(1)}K`;
  return value.toFixed(value % 1 === 0 ? 0 : 1);
}

export function formatPercentage(value: number): string {
  return `${value >= 0 ? "+" : ""}${value.toFixed(1)}%`;
}
