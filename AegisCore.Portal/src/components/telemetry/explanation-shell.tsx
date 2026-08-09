"use client";

// ─── Explanation Shell ───
// Shared presentation pattern for "Why did this happen?" surfaces:
// provenance (Rule / Statistic / Model / Human / Mixed), AI disclosure,
// explanation version, audit reference and timestamps — straight from the
// AnalyticsEnvelope, never inferred.

import { formatDateTime, type AnalyticsEnvelope } from "@/lib/analytics";
import { ProvenanceBadge } from "./status";

const KIND_LABELS: Record<string, string> = {
  DeterministicDecision: "Deterministic decision",
  StatisticalObservation: "Statistical observation",
  MlPrediction: "Model prediction",
  HumanDecision: "Human decision",
};

export function ExplanationMetaPanel({ envelope }: { envelope: AnalyticsEnvelope<unknown> }) {
  return (
    <section className="explanation-meta" aria-label="Explanation provenance">
      <ProvenanceBadge source={envelope.decisionSource} />
      <dl className="explanation-meta-grid">
        <div>
          <dt>Explanation type</dt>
          <dd>{KIND_LABELS[envelope.explanationType] ?? envelope.explanationType}</dd>
        </div>
        <div>
          <dt>Explanation version</dt>
          <dd className="font-mono">{envelope.explanationVersion}</dd>
        </div>
        <div>
          <dt>Generated</dt>
          <dd>{formatDateTime(envelope.generatedAt)}</dd>
        </div>
        <div>
          <dt>Audit reference</dt>
          <dd className="font-mono">{envelope.auditId ?? "—"}</dd>
        </div>
      </dl>
    </section>
  );
}

/** Small labeled statistic used across telemetry/explanation surfaces. */
export function StatItem({ label, value, hint }: { label: string; value: React.ReactNode; hint?: string }) {
  return (
    <div className="stat-item" title={hint}>
      <span className="stat-item-label">{label}</span>
      <span className="stat-item-value">{value}</span>
    </div>
  );
}
