"use client";

// ─── Grade Explanation ───
// "Why did I get this grade?" — the flagship deterministic-transparency
// surface. Renders the backend's exact reproduced arithmetic, the applied
// grade boundary, and a clearly separated statistical-context area.
// SHAP is never used for a deterministic grade; the provenance panel makes
// the decision source (Rule) and AI involvement (No) explicit.

import { use, useState } from "react";
import Link from "next/link";
import { ArrowLeft, FileQuestion, ShieldAlert } from "lucide-react";
import { AppShell } from "@/components/layout/AppShell";
import { EmptyState, ErrorState, LoadingState } from "@/components/async/states";
import { isApiError } from "@/lib/api-client";
import {
  formatDateTime,
  formatPercentile,
  formatValue,
  formatZScore,
  getMyGradeExplanation,
  type GradeExplanation,
} from "@/lib/analytics";
import { useAnalyticsFetch } from "@/components/telemetry/use-analytics";
import { ExplanationMetaPanel, StatItem } from "@/components/telemetry/explanation-shell";
import { AvailabilityNote } from "@/components/telemetry/status";
import { DistributionChart } from "@/components/telemetry/distribution-chart";

const n2 = (v: number | null | undefined) => formatValue(v, 2);

function GradeArithmetic({ data }: { data: GradeExplanation }) {
  return (
    <section className="card" aria-labelledby="grade-arithmetic">
      <div className="card-header">
        <h3 id="grade-arithmetic" style={{ fontSize: 15 }}>Why this grade — exact calculation</h3>
      </div>
      <div className="table-wrap" style={{ border: "none", borderRadius: 0 }}>
        <table>
          <thead>
            <tr>
              <th>Component</th>
              <th>Score</th>
              <th>Normalized</th>
              <th>Weight</th>
              <th>Weighted contribution</th>
            </tr>
          </thead>
          <tbody>
            {data.assessmentComponents.map(c => (
              <tr key={c.assessmentComponentId}>
                <td>{c.componentType}</td>
                <td className="font-mono">{n2(c.rawScore)} / {n2(c.maximumScore)}</td>
                <td className="font-mono">{n2(c.normalizedScore)}</td>
                <td className="font-mono">{n2(c.weight)}</td>
                <td className="font-mono">+{n2(c.weightedContribution)}</td>
              </tr>
            ))}
            <tr className="grade-total-row">
              <td>Total</td>
              <td colSpan={3} />
              <td className="font-mono">{n2(data.totalWeightedScore)} / 100.00</td>
            </tr>
          </tbody>
        </table>
      </div>
      <div className="card-body" style={{ display: "grid", gap: 12 }}>
        <div className="grade-boundary-rule" role="note">
          Applied rule: {n2(data.appliedGradeBoundary.minimumInclusive)} ≤ score ≤{" "}
          {n2(data.appliedGradeBoundary.maximumInclusive)} → {data.appliedGradeBoundary.grade}
          {` (${n2(data.appliedGradeBoundary.gpaPoints)} GPA points)`}
        </div>
        {data.nextGradeBoundary && data.distanceToNextGrade !== null && (
          <p style={{ margin: 0, fontSize: 14 }}>
            Next grade: <strong>{data.nextGradeBoundary.grade}</strong> begins at{" "}
            <span className="font-mono">{n2(data.nextGradeBoundary.minimumInclusive)}</span> — distance{" "}
            <span className="font-mono">{n2(data.distanceToNextGrade)}</span>.
          </p>
        )}
        <p style={{ margin: 0, fontSize: 13, color: "var(--muted)" }}>
          Rounding policy: {data.roundingPolicy}
          {data.moderationOrAdjustment ? ` · Moderation/adjustment: ${data.moderationOrAdjustment}` : ""}
        </p>
        <p style={{ margin: 0, fontSize: 12, color: "var(--subtle)" }}>
          Decision recorded {formatDateTime(data.decisionTimestamp)} · Snapshot {formatDateTime(data.snapshotTimestamp)} ·
          Grading policy version <span className="font-mono">{data.gradingPolicyVersion.slice(0, 12)}…</span>
        </p>
      </div>
    </section>
  );
}

function StatisticalContextCard({ data }: { data: GradeExplanation }) {
  const ctx = data.statisticalContext;
  const available = ctx.availability.status === "Available" || ctx.availability.status === "Partial";
  return (
    <section className="card" aria-labelledby="grade-context">
      <div className="card-header">
        <div>
          <h3 id="grade-context" style={{ fontSize: 15 }}>How you compare — statistical context</h3>
          <p style={{ margin: "2px 0 0", fontSize: 12, color: "var(--muted)" }}>
            Descriptive cohort statistics. This context did not determine your grade.
          </p>
        </div>
      </div>
      <div className="card-body" style={{ display: "grid", gap: 16 }}>
        {!available ? (
          <AvailabilityNote availability={ctx.availability} context="Cohort context" />
        ) : (
          <>
            <div className="stat-grid">
              <StatItem label="Cohort mean" value={n2(ctx.cohortMean)} />
              <StatItem label="Median" value={n2(ctx.cohortMedian)} />
              <StatItem label="Std deviation" value={n2(ctx.standardDeviation)} />
              <StatItem label="Percentile" value={formatPercentile(ctx.percentile)} hint="Your position relative to the comparison cohort" />
              <StatItem label="z-score" value={formatZScore(ctx.zScore)} hint="Standard deviations above or below the cohort mean" />
              <StatItem label="Cohort size" value={String(ctx.sampleSize)} />
            </div>
            {ctx.distribution.length > 0 && (
              <DistributionChart
                distribution={ctx.distribution}
                ownValue={data.finalScore}
                ownLabel="You"
                median={ctx.cohortMedian === null ? null : Number(ctx.cohortMedian)}
              />
            )}
            <AvailabilityNote availability={ctx.availability} context="Cohort context" />
          </>
        )}
      </div>
    </section>
  );
}

export default function GradeExplanationPage({ params }: { params: Promise<{ courseGradeId: string }> }) {
  const { courseGradeId } = use(params);
  const [attempt, setAttempt] = useState(0);
  const state = useAnalyticsFetch(`grade-explanation:${courseGradeId}:${attempt}`, signal =>
    getMyGradeExplanation(courseGradeId, signal)
  );

  const envelope = state.status === "ok" ? state.data : null;
  const data = envelope?.data ?? null;

  return (
    <AppShell>
      <div className="telemetry-page">
        <header className="page-header" style={{ padding: 0 }}>
          <div>
            <div className="page-header-breadcrumb">
              <Link href="/telemetry">Performance</Link><span>/</span><span>Grade explanation</span>
            </div>
            <h1>Why did I get this grade?</h1>
            <p>The exact recorded calculation behind your published grade.</p>
          </div>
          <div className="page-header-actions">
            <Link href="/telemetry" className="btn btn-secondary btn-sm"><ArrowLeft size={14} /> Telemetry</Link>
          </div>
        </header>

        {state.status === "loading" && <LoadingState label="Reconstructing the grade calculation…" lines={5} />}

        {state.status === "error" && isApiError(state.error) && state.error.status === 403 && (
          <EmptyState
            icon={ShieldAlert}
            title="Academic record not linked"
            description="Grade explanations appear here once the Registrar links your student record to this account."
          />
        )}

        {state.status === "error" && isApiError(state.error) && state.error.status === 404 && (
          <EmptyState
            icon={FileQuestion}
            title="Grade explanation not available"
            description="This grade is unpublished, does not exist, or is not part of your academic record."
          />
        )}

        {state.status === "error" && (!isApiError(state.error) || (state.error.status !== 403 && state.error.status !== 404)) && (
          <ErrorState error={state.error} title="The explanation could not be loaded" onRetry={() => setAttempt(a => a + 1)} />
        )}

        {envelope && !data && (
          <EmptyState
            icon={FileQuestion}
            title="This grade cannot be explained from current records"
            description={
              envelope.availability.reason ??
              "The recorded result cannot be exactly reproduced from current source records, so no explanation is shown rather than an approximate one."
            }
          />
        )}

        {envelope && data && (
          <>
            <section className="card" aria-label="Final result">
              <div className="card-body" style={{ display: "flex", alignItems: "baseline", gap: 16, flexWrap: "wrap" }}>
                <span style={{ fontSize: 13, color: "var(--muted)", textTransform: "uppercase", letterSpacing: ".04em" }}>
                  Final grade
                </span>
                <strong style={{ fontFamily: "var(--font-mono)", fontSize: 42, lineHeight: 1 }}>{data.finalGrade}</strong>
                <span className="font-mono" style={{ fontSize: 18, color: "var(--muted)" }}>
                  {n2(data.finalScore)} / 100.00
                </span>
              </div>
            </section>

            <ExplanationMetaPanel envelope={envelope} />
            <GradeArithmetic data={data} />
            <StatisticalContextCard data={data} />
          </>
        )}
      </div>
    </AppShell>
  );
}
