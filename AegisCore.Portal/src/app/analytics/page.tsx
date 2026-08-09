"use client";

// ─── Model Analytics (authorized oversight) ───
// Management/academic oversight surface for model-quality snapshots and
// global explanation snapshots. Backend roles: Admin, SuperAdmin,
// Registrar, ExamController, VC, HOD, Dean (enforced server-side).
// Null metrics are never rendered as zero; no snapshot → deliberate
// empty state. Nothing here claims a model is "bias free".

import { useState } from "react";
import { FileQuestion, Search } from "lucide-react";
import { AppShell } from "@/components/layout/AppShell";
import { EmptyState, ErrorState, LoadingState } from "@/components/async/states";
import { isApiError } from "@/lib/api-client";
import {
  formatDateTime,
  formatValue,
  getGlobalModelExplanation,
  getModelQuality,
  type GlobalModelExplanation,
  type ModelQuality,
} from "@/lib/analytics";
import { StatItem } from "@/components/telemetry/explanation-shell";
import { ContributionChart } from "@/components/telemetry/contribution-chart";

const n3 = (v: number | null | undefined) => (v === null || v === undefined ? null : formatValue(v, 3));

function QualityPanel({ quality }: { quality: ModelQuality }) {
  const metrics: { label: string; value: string | null; hint?: string }[] = [
    { label: "AUROC", value: n3(quality.auRoc), hint: "Area under the ROC curve" },
    { label: "Precision", value: n3(quality.precision) },
    { label: "Recall", value: n3(quality.recall) },
    { label: "Specificity", value: n3(quality.specificity) },
    { label: "F1", value: n3(quality.f1) },
    { label: "Threshold", value: n3(quality.threshold) },
    { label: "Calibration error", value: n3(quality.calibrationError) },
    { label: "Drift indicator", value: n3(quality.driftIndicator) },
  ].filter(m => m.value !== null);

  const confusion: [string, number | null][] = [
    ["True positives", quality.truePositive],
    ["True negatives", quality.trueNegative],
    ["False positives", quality.falsePositive],
    ["False negatives", quality.falseNegative],
  ];
  const hasConfusion = confusion.some(([, v]) => v !== null);

  return (
    <section className="card" aria-labelledby="model-quality">
      <div className="card-header">
        <div>
          <h3 id="model-quality" style={{ fontSize: 15 }}>
            Model quality — {quality.modelName} <span className="font-mono" style={{ fontSize: 12 }}>{quality.modelVersion}</span>
          </h3>
          <p style={{ margin: "2px 0 0", fontSize: 12, color: "var(--muted)" }}>
            Evaluated {formatDateTime(quality.evaluationTimestamp)}
            {quality.trainingPeriodStart && quality.trainingPeriodEnd
              ? ` · trained on ${formatDateTime(quality.trainingPeriodStart)} – ${formatDateTime(quality.trainingPeriodEnd)}`
              : ""}
            {quality.trainingSampleSize !== null ? ` · training n = ${quality.trainingSampleSize}` : ""}
          </p>
        </div>
      </div>
      <div className="card-body" style={{ display: "grid", gap: 20 }}>
        {metrics.length > 0 && (
          <div className="stat-grid">
            {metrics.map(m => <StatItem key={m.label} label={m.label} value={m.value} hint={m.hint} />)}
          </div>
        )}

        {hasConfusion && (
          <div>
            <h4 style={{ fontSize: 13, margin: "0 0 8px" }}>Confusion counts</h4>
            <div className="stat-grid">
              {confusion.filter(([, v]) => v !== null).map(([label, v]) => (
                <StatItem key={label} label={label} value={String(v)} />
              ))}
            </div>
          </div>
        )}

        {Object.keys(quality.fairnessMonitoringMetrics).length > 0 && (
          <div>
            <h4 style={{ fontSize: 13, margin: "0 0 8px" }}>Fairness monitoring metrics</h4>
            <div className="stat-grid">
              {Object.entries(quality.fairnessMonitoringMetrics).map(([name, v]) => (
                <StatItem key={name} label={name} value={formatValue(v, 3)} />
              ))}
            </div>
          </div>
        )}

        {quality.sensitiveOrProxyFeatureReview.length > 0 && (
          <div>
            <h4 style={{ fontSize: 13, margin: "0 0 8px" }}>Sensitive / proxy feature review</h4>
            <ul className="limitation-list">
              {quality.sensitiveOrProxyFeatureReview.map((s, i) => <li key={i}>{s}</li>)}
            </ul>
          </div>
        )}

        <p style={{ margin: 0, fontSize: 13, color: "var(--muted)" }}>{quality.evidenceStatement}</p>
      </div>
    </section>
  );
}

function GlobalExplanationPanel({ explanation }: { explanation: GlobalModelExplanation }) {
  return (
    <section className="card" aria-labelledby="global-explanation">
      <div className="card-header">
        <div>
          <h3 id="global-explanation" style={{ fontSize: 15 }}>Top model drivers — global explanation</h3>
          <p style={{ margin: "2px 0 0", fontSize: 12, color: "var(--muted)" }}>
            {explanation.authorizedPopulation} · {formatDateTime(explanation.populationStart)} –{" "}
            {formatDateTime(explanation.populationEnd)} · n = {explanation.sampleSize} ·{" "}
            {explanation.explanationAlgorithm} {explanation.explanationAlgorithmVersion} · calculated{" "}
            {formatDateTime(explanation.calculatedAt)}
          </p>
        </div>
      </div>
      <div className="card-body" style={{ display: "grid", gap: 16 }}>
        {Object.keys(explanation.meanAbsoluteShapValues).length === 0 ? (
          <p className="availability-note" role="note">No global contribution values were recorded in this snapshot.</p>
        ) : (
          <ContributionChart mode="global" values={explanation.meanAbsoluteShapValues} />
        )}
        <p className="availability-note" role="note">
          Aggregate model behavior across the authorized population. This describes how the model behaves on
          average — it contains no individual student data and does not establish causality.
        </p>
        <p className="availability-note" role="note">{explanation.disclaimer}</p>
      </div>
    </section>
  );
}

type LookupState =
  | { status: "idle" }
  | { status: "loading" }
  | { status: "error"; error: unknown }
  | { status: "ok"; quality: ModelQuality | null; global: GlobalModelExplanation | null };

export default function AnalyticsPage() {
  const [modelName, setModelName] = useState("");
  const [modelVersion, setModelVersion] = useState("");
  const [state, setState] = useState<LookupState>({ status: "idle" });

  const lookup = async (e: React.FormEvent) => {
    e.preventDefault();
    const name = modelName.trim();
    if (!name) return;
    const version = modelVersion.trim() || undefined;
    setState({ status: "loading" });
    // Both snapshots are independent: one may exist without the other.
    const [quality, global] = await Promise.all([
      getModelQuality(name, version).catch(err => {
        if (isApiError(err) && err.status === 404) return null;
        throw err;
      }),
      getGlobalModelExplanation(name, version).catch(err => {
        if (isApiError(err) && err.status === 404) return null;
        throw err;
      }),
    ]).catch((error: unknown) => {
      setState({ status: "error", error });
      return [undefined, undefined];
    });
    if (quality === undefined && global === undefined) return; // error state already set
    setState({ status: "ok", quality: quality ?? null, global: global ?? null });
  };

  return (
    <AppShell>
      <div className="telemetry-page">
        <header className="page-header" style={{ padding: 0 }}>
          <div>
            <div className="page-header-breadcrumb"><span>Oversight</span><span>/</span><span>Model analytics</span></div>
            <h1>Model Analytics</h1>
            <p>Recorded model-quality and global-explanation snapshots for governed predictive models.</p>
          </div>
        </header>

        <section className="card" aria-label="Model lookup">
          <div className="card-body">
            <form onSubmit={lookup} style={{ display: "flex", gap: 12, flexWrap: "wrap", alignItems: "flex-end" }}>
              <label style={{ display: "grid", gap: 4, fontSize: 12, color: "var(--muted)", flex: 1, minWidth: 220 }}>
                Model name
                <input
                  value={modelName}
                  onChange={e => setModelName(e.target.value)}
                  placeholder="e.g. academic-risk"
                  required
                  maxLength={160}
                />
              </label>
              <label style={{ display: "grid", gap: 4, fontSize: 12, color: "var(--muted)", minWidth: 160 }}>
                Version (optional)
                <input
                  value={modelVersion}
                  onChange={e => setModelVersion(e.target.value)}
                  placeholder="latest"
                  maxLength={80}
                />
              </label>
              <button type="submit" className="btn btn-primary" disabled={state.status === "loading"}>
                <Search size={16} /> {state.status === "loading" ? "Loading…" : "Load snapshots"}
              </button>
            </form>
          </div>
        </section>

        {state.status === "idle" && (
          <EmptyState
            icon={Search}
            title="Look up a governed model"
            description="Quality and global-explanation snapshots are precomputed server-side. Enter a model name to load its latest recorded snapshots."
          />
        )}

        {state.status === "loading" && <LoadingState label="Loading recorded snapshots…" lines={4} />}

        {state.status === "error" && <ErrorState error={state.error} title="Snapshots could not be loaded" />}

        {state.status === "ok" && state.quality === null && state.global === null && (
          <EmptyState
            icon={FileQuestion}
            title="No snapshots recorded for this model"
            description="No quality or global-explanation snapshot has been recorded for this model and version. Governed model snapshots appear here once a versioned predictive-model pipeline publishes them."
          />
        )}

        {state.status === "ok" && (
          <>
            {state.quality ? (
              <QualityPanel quality={state.quality} />
            ) : (
              <p className="availability-note" role="note">No quality snapshot recorded for this model and version.</p>
            )}
            {state.global ? (
              <GlobalExplanationPanel explanation={state.global} />
            ) : (
              <p className="availability-note" role="note">No global explanation snapshot recorded for this model and version.</p>
            )}
          </>
        )}
      </div>
    </AppShell>
  );
}
