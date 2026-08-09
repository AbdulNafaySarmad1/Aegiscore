"use client";

// ─── Telemetry Status Primitives ───
// Accessible chips/indicators for backend-owned statuses. Every status
// carries a text label AND a symbol — color is never the only cue.

import { ShieldAlert } from "lucide-react";
import type { Availability, ExplanationSource, ImprovementStatus, PerformanceStatus, TrendDirection } from "@/lib/analytics";
import {
  improvementStatusMeta,
  performanceStatusMeta,
  provenanceMeta,
  trendMeta,
  type StatusTone,
} from "@/lib/telemetry-semantics";

export function StatusChip({ meta, size = "md" }: { meta: { label: string; tone: StatusTone; symbol: string }; size?: "sm" | "md" }) {
  return (
    <span className={`status-chip status-${meta.tone} ${size === "sm" ? "status-chip-sm" : ""}`}>
      <span aria-hidden="true" className="status-chip-symbol">{meta.symbol}</span>
      <span>{meta.label}</span>
    </span>
  );
}

export function PerformanceChip({ status }: { status: PerformanceStatus }) {
  return <StatusChip meta={performanceStatusMeta(status)} />;
}

export function ImprovementChip({ status }: { status: ImprovementStatus }) {
  return <StatusChip meta={improvementStatusMeta(status)} />;
}

export function TrendIndicator({ direction }: { direction: TrendDirection }) {
  const meta = trendMeta(direction);
  return (
    <span className={`trend-indicator trend-${meta.tone}`} title={meta.label}>
      <span aria-hidden="true">{meta.arrow}</span>
      <span className="sr-only">{meta.label}</span>
    </span>
  );
}

/** "Decision source: Rule" + honest AI-involvement disclosure. */
export function ProvenanceBadge({ source }: { source: ExplanationSource }) {
  const meta = provenanceMeta(source);
  return (
    <span className={`provenance-badge provenance-${meta.aiInvolved}`}>
      <span className="provenance-source">Decision source: {meta.label}</span>
      <span className="provenance-ai">
        AI involved: {meta.aiInvolved === "yes" ? "Yes" : meta.aiInvolved === "partial" ? "Partially" : "No"}
      </span>
    </span>
  );
}

/**
 * Deliberate privacy/unavailability state. Renders nothing when the
 * payload is fully Available.
 */
export function AvailabilityNote({ availability, context }: { availability: Availability; context?: string }) {
  if (availability.status === "Available") return null;
  if (availability.status === "Suppressed") {
    return (
      <p className="availability-note availability-suppressed" role="note">
        <ShieldAlert size={14} aria-hidden="true" />
        Comparison unavailable because the cohort is too small to preserve privacy.
      </p>
    );
  }
  if (availability.status === "Partial") {
    return (
      <p className="availability-note" role="note">
        Some values are unavailable{availability.reason ? `: ${availability.reason}` : "."}
      </p>
    );
  }
  return (
    <p className="availability-note" role="note">
      {context ?? "This data"} is currently unavailable{availability.reason ? `: ${availability.reason}` : "."}
    </p>
  );
}
