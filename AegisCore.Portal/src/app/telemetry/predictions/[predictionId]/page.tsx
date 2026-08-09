"use client";

// ─── Prediction Explanation (ML / SHAP) ───
// Renders an immutable backend-recorded prediction snapshot: model
// metadata, baseline, and ranked SHAP contributions. Language discipline:
// SHAP describes the MODEL OUTPUT — never causality. Headings use
// "Factors influencing this model prediction"; the backend disclaimer and
// a fixed non-causal caption are always shown.

import { use, useState } from "react";
import Link from "next/link";
import { ArrowLeft, FileQuestion, ShieldAlert } from "lucide-react";
import { AppShell } from "@/components/layout/AppShell";
import { EmptyState, ErrorState, ForbiddenState, LoadingState } from "@/components/async/states";
import { isApiError } from "@/lib/api-client";
import {
  formatDateTime,
  formatValue,
  getPredictionExplanation,
  type PredictionExplanation,
} from "@/lib/analytics";
import { useAnalyticsFetch } from "@/components/telemetry/use-analytics";
import { ExplanationMetaPanel, StatItem } from "@/components/telemetry/explanation-shell";
import { ContributionChart } from "@/components/telemetry/contribution-chart";

const MODEL_KIND_LABELS: Record<string, string> = {
  GenuinePredictiveModel: "Trained predictive model",
  DeterministicRule: "Deterministic rule",
  Heuristic: "Heuristic",
  LlmEstimate: "LLM estimate",
};

function PredictionDetail({ data }: { data: PredictionExplanation }) {
  const increasing = data.rankedLocalContributions.filter(c => c.shapValue > 0);
  const decreasing = data.rankedLocalContributions.filter(c => c.shapValue < 0);

  return (
    <>
      <section className="card" aria-label="Prediction summary">
        <div className="card-header">
          <div>
            <h3 style={{ fontSize: 15 }}>{data.label}</h3>
            <p style={{ margin: "2px 0 0", fontSize: 12, color: "var(--muted)" }}>
              {data.subjectType} · recorded model output
            </p>
          </div>
          {data.requiresHumanReview && (
            <span className="status-chip status-yellow">
              <span aria-hidden="true" className="status-chip-symbol">▲</span>
              Requires human review
            </span>
          )}
        </div>
        <div className="card-body">
          <div className="stat-grid">
            <StatItem label="Model output" value={formatValue(data.rawPrediction, 3)} />
            {data.calibratedProbability !== null && (
              <StatItem
                label="Calibrated probability"
                value={`${formatValue(data.calibratedProbability * 100, 1)}%`}
                hint="Backend-calibrated probability for the predicted class"
              />
            )}
            <StatItem
              label="Baseline expected value"
              value={formatValue(data.baselineExpectedValue, 3)}
              hint="Average model output before feature contributions"
            />
          </div>
        </div>
      </section>

      <section className="card" aria-labelledby="shap-factors">
        <div className="card-header">
          <div>
            <h3 id="shap-factors" style={{ fontSize: 15 }}>Factors influencing this model prediction</h3>
            <p style={{ margin: "2px 0 0", fontSize: 12, color: "var(--muted)" }}>
              {data.explanationAlgorithm} {data.explanationAlgorithmVersion} contributions relative to the baseline.
            </p>
          </div>
        </div>
        <div className="card-body" style={{ display: "grid", gap: 20 }}>
          {data.rankedLocalContributions.length === 0 ? (
            <p className="availability-note" role="note">No feature contributions were recorded for this prediction.</p>
          ) : (
            <>
              {increasing.length > 0 && (
                <div>
                  <h4 style={{ fontSize: 13, margin: "0 0 8px", color: "var(--tone-red)" }}>
                    Factors increasing this model prediction
                  </h4>
                  <ContributionChart mode="local" contributions={increasing} />
                </div>
              )}
              {decreasing.length > 0 && (
                <div>
                  <h4 style={{ fontSize: 13, margin: "0 0 8px", color: "var(--tone-green)" }}>
                    Factors decreasing this model prediction
                  </h4>
                  <ContributionChart mode="local" contributions={decreasing} />
                </div>
              )}
            </>
          )}
          <p className="availability-note" role="note">
            These values explain the model&rsquo;s output, not real-world causality. A feature influencing the
            prediction does not mean it caused the outcome.
          </p>
          <p className="availability-note" role="note">{data.disclaimer}</p>
        </div>
      </section>

      <section className="card" aria-labelledby="model-metadata">
        <div className="card-header"><h3 id="model-metadata" style={{ fontSize: 15 }}>Model and snapshot metadata</h3></div>
        <div className="card-body">
          <div className="stat-grid">
            <StatItem label="Model" value={data.modelName} />
            <StatItem label="Model version" value={<span className="font-mono">{data.modelVersion}</span>} />
            <StatItem label="Model kind" value={MODEL_KIND_LABELS[data.modelKind] ?? data.modelKind} />
            <StatItem label="Feature schema" value={<span className="font-mono">{data.featureSchemaVersion}</span>} />
            <StatItem label="Explainer" value={`${data.explanationAlgorithm} ${data.explanationAlgorithmVersion}`} />
            <StatItem label="Prediction ID" value={<span className="font-mono">{data.predictionId}</span>} />
            <StatItem label="Predicted at" value={formatDateTime(data.predictionTimestamp)} />
            <StatItem label="Data cutoff" value={formatDateTime(data.dataCutoffTimestamp)} />
          </div>
        </div>
      </section>

      {Object.keys(data.featureValues).length > 0 && (
        <section className="card" aria-labelledby="feature-values">
          <div className="card-header"><h3 id="feature-values" style={{ fontSize: 15 }}>Input feature values at prediction time</h3></div>
          <div className="table-wrap" style={{ border: "none", borderRadius: 0 }}>
            <table>
              <thead><tr><th>Feature</th><th>Value</th></tr></thead>
              <tbody>
                {Object.entries(data.featureValues).map(([name, value]) => (
                  <tr key={name}>
                    <td>{name}</td>
                    <td className="font-mono">{formatValue(value, 3)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      )}
    </>
  );
}

export default function PredictionExplanationPage({ params }: { params: Promise<{ predictionId: string }> }) {
  const { predictionId } = use(params);
  const [attempt, setAttempt] = useState(0);
  const state = useAnalyticsFetch(`prediction-explanation:${predictionId}:${attempt}`, signal =>
    getPredictionExplanation(predictionId, signal)
  );

  const envelope = state.status === "ok" ? state.data : null;
  const data = envelope?.data ?? null;

  return (
    <AppShell>
      <div className="telemetry-page">
        <header className="page-header" style={{ padding: 0 }}>
          <div>
            <div className="page-header-breadcrumb">
              <Link href="/telemetry">Performance</Link><span>/</span><span>Model prediction</span>
            </div>
            <h1>Model prediction explanation</h1>
            <p>What influenced this recorded model output — and what did not decide anything.</p>
          </div>
          <div className="page-header-actions">
            <Link href="/telemetry" className="btn btn-secondary btn-sm"><ArrowLeft size={14} /> Telemetry</Link>
          </div>
        </header>

        {state.status === "loading" && <LoadingState label="Loading the stored explanation…" lines={5} />}

        {state.status === "error" && isApiError(state.error) && state.error.status === 403 && <ForbiddenState />}

        {state.status === "error" && isApiError(state.error) && state.error.status === 404 && (
          <EmptyState
            icon={FileQuestion}
            title="Prediction explanation not available"
            description="No recorded prediction snapshot exists for this identifier — or you are not authorized to view it."
          />
        )}

        {state.status === "error" && (!isApiError(state.error) || (state.error.status !== 403 && state.error.status !== 404)) && (
          <ErrorState error={state.error} title="The explanation could not be loaded" onRetry={() => setAttempt(a => a + 1)} />
        )}

        {envelope && !data && (
          <EmptyState
            icon={ShieldAlert}
            title="This prediction has no usable explanation"
            description={envelope.availability.reason ?? "The stored snapshot does not contain a valid explanation."}
          />
        )}

        {envelope && data && (
          <>
            <ExplanationMetaPanel envelope={envelope} />
            <PredictionDetail data={data} />
          </>
        )}
      </div>
    </AppShell>
  );
}
