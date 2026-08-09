// ─── Telemetry Semantics ─── src/lib/telemetry-semantics.ts ───
// Pure presentation mapping from backend-owned semantic statuses to
// labels, tones, and icon names. The backend classifies; we only render.
// Color is NEVER the only carrier of meaning — every mapping includes a
// text label and a symbol.

import type { ImprovementStatus, PerformanceStatus, TrendDirection } from "@/lib/analytics";

export type StatusTone = "purple" | "green" | "yellow" | "red" | "gray";

export type StatusMeta = {
  label: string;
  tone: StatusTone;
  /** Short symbol rendered next to the label so color is not the only cue. */
  symbol: string;
};

// ─── Absolute performance (backend: PerformanceStatus) ───

const PERFORMANCE_META: Record<PerformanceStatus, StatusMeta> = {
  Exceptional: { label: "Exceptional", tone: "purple", symbol: "◆" },
  Strong: { label: "Strong", tone: "green", symbol: "●" },
  Developing: { label: "Developing", tone: "yellow", symbol: "▲" },
  AtRisk: { label: "At risk", tone: "red", symbol: "▼" },
  Unavailable: { label: "Unavailable", tone: "gray", symbol: "—" },
};

export function performanceStatusMeta(status: PerformanceStatus): StatusMeta {
  return PERFORMANCE_META[status] ?? PERFORMANCE_META.Unavailable;
}

// ─── Personal progress (backend: ImprovementStatus) ───
// Deliberately independent from absolute performance: a 64% score can be a
// Personal Best and an 88% score can be Declining.

const IMPROVEMENT_META: Record<ImprovementStatus, StatusMeta> = {
  PersonalBest: { label: "Personal best", tone: "purple", symbol: "★" },
  Improving: { label: "Improving", tone: "green", symbol: "↗" },
  Stable: { label: "Stable", tone: "gray", symbol: "→" },
  Declining: { label: "Declining", tone: "red", symbol: "↘" },
  Unavailable: { label: "No comparison", tone: "gray", symbol: "—" },
};

export function improvementStatusMeta(status: ImprovementStatus): StatusMeta {
  return IMPROVEMENT_META[status] ?? IMPROVEMENT_META.Unavailable;
}

// ─── Trend direction (backend: TrendDirection) ───

export type TrendMeta = {
  label: string;
  tone: StatusTone;
  arrow: string;
};

const TREND_META: Record<TrendDirection, TrendMeta> = {
  Up: { label: "Trending up", tone: "green", arrow: "↗" },
  Flat: { label: "Flat", tone: "gray", arrow: "→" },
  Down: { label: "Trending down", tone: "red", arrow: "↘" },
  Unavailable: { label: "No trend data", tone: "gray", arrow: "—" },
};

export function trendMeta(direction: TrendDirection): TrendMeta {
  return TREND_META[direction] ?? TREND_META.Unavailable;
}

// ─── Explanation provenance (backend: ExplanationSource / ExplanationKind) ───

export type ProvenanceMeta = {
  label: string;
  /** Honest answer to "Was AI involved?" derived ONLY from the backend source. */
  aiInvolved: "yes" | "no" | "partial";
  aiLabel: string;
};

export function provenanceMeta(source: import("@/lib/analytics").ExplanationSource): ProvenanceMeta {
  switch (source) {
    case "Model":
      return { label: "Model", aiInvolved: "yes", aiLabel: "Yes — a predictive model produced this output" };
    case "Mixed":
      return { label: "Mixed", aiInvolved: "partial", aiLabel: "Partially — combines model output with rules or human input" };
    case "Human":
      return { label: "Human", aiInvolved: "no", aiLabel: "No — recorded human decision" };
    case "Statistic":
      return { label: "Statistic", aiInvolved: "no", aiLabel: "No — descriptive statistics, not a decision rule" };
    case "Rule":
    default:
      return { label: "Rule", aiInvolved: "no", aiLabel: "No — deterministic rule or policy calculation" };
  }
}
