"use client";

// ─── Admissions Decision Explanation ───
// Decision transparency for authorized admissions/oversight roles
// (Admin, SuperAdmin, Registrar, AdmissionsHead — enforced by the backend).
// Shows the deterministic merit arithmetic, eligibility outcomes, and the
// recorded closing-merit comparison. The closing-merit distance is
// historical and non-promissory — it is never rendered as a guarantee.

import { use, useState } from "react";
import { ArrowLeft, CheckCircle2, FileQuestion, HelpCircle, XCircle } from "lucide-react";
import Link from "next/link";
import { AppShell } from "@/components/layout/AppShell";
import { EmptyState, ErrorState, ForbiddenState, LoadingState } from "@/components/async/states";
import { isApiError } from "@/lib/api-client";
import {
  formatDateTime,
  formatDelta,
  formatValue,
  getAdmissionDecisionExplanation,
  type AdmissionDecisionExplanation,
} from "@/lib/analytics";
import { useAnalyticsFetch } from "@/components/telemetry/use-analytics";
import { ExplanationMetaPanel, StatItem } from "@/components/telemetry/explanation-shell";

const n2 = (v: number | null | undefined) => formatValue(v, 2);

function RequirementIcon({ passed }: { passed: boolean | null }) {
  if (passed === true) return <CheckCircle2 className="req-pass" aria-label="Passed" />;
  if (passed === false) return <XCircle className="req-fail" aria-label="Failed" />;
  return <HelpCircle className="req-unknown" aria-label="Not recorded" />;
}

function AdmissionDetail({ data }: { data: AdmissionDecisionExplanation }) {
  return (
    <>
      <section className="card" aria-label="Decision summary">
        <div className="card-header"><h3 style={{ fontSize: 15 }}>Decision</h3></div>
        <div className="card-body" style={{ display: "grid", gap: 16 }}>
          <div className="stat-grid">
            <StatItem label="Outcome" value={<strong>{data.decision}</strong>} />
            <StatItem label="Aggregate score" value={n2(data.aggregateScore)} />
            <StatItem label="Rank" value={String(data.rank)} />
            <StatItem label="Seats available" value={String(data.seatsAvailable)} />
            <StatItem label="Closing merit" value={data.closingMerit === null ? "Not recorded" : n2(data.closingMerit)} />
            <StatItem
              label="Distance from closing merit"
              value={data.distanceFromClosingMerit === null ? "—" : formatDelta(data.distanceFromClosingMerit, 2)}
            />
            {data.waitlistPosition !== null && <StatItem label="Waitlist position" value={String(data.waitlistPosition)} />}
          </div>

          {data.distanceFromClosingMerit !== null && data.closingMerit !== null && (
            <p style={{ margin: 0, fontSize: 14 }}>
              {data.distanceFromClosingMerit < 0
                ? `Against the recorded closing merit for this intake, the aggregate was ${n2(Math.abs(data.distanceFromClosingMerit))} points lower.`
                : `The aggregate met or exceeded the recorded closing merit for this intake by ${n2(data.distanceFromClosingMerit)} points.`}
            </p>
          )}
          <p className="availability-note" role="note">
            This comparison is historical and non-promissory: it describes the recorded cutoff for this intake and does
            not imply that changing any single component would have changed the outcome.
          </p>

          {data.failedPrerequisiteOrReason && (
            <p style={{ margin: 0, fontSize: 14 }}>
              <strong>Recorded reason:</strong> {data.failedPrerequisiteOrReason}
            </p>
          )}
          <p style={{ margin: 0, fontSize: 13, color: "var(--muted)" }}>
            Policy: {data.policyVersion ?? "Not recorded for this decision"} · Decided {formatDateTime(data.decisionTimestamp)}
          </p>
        </div>
      </section>

      <section className="card" aria-labelledby="eligibility">
        <div className="card-header"><h3 id="eligibility" style={{ fontSize: 15 }}>Eligibility requirements</h3></div>
        <div className="card-body">
          {data.eligibilityRequirements.length === 0 ? (
            <p className="availability-note" role="note">
              Structured eligibility outcomes were not recorded for this decision.
            </p>
          ) : (
            <ul className="requirement-list">
              {data.eligibilityRequirements.map((r, i) => (
                <li key={i}>
                  <RequirementIcon passed={r.passed} />
                  <div>
                    <strong>{r.requirement}</strong>
                    {r.passed === true && <span className="req-pass"> — Passed</span>}
                    {r.passed === false && <span className="req-fail"> — Failed</span>}
                    {r.passed === null && <span className="req-unknown"> — Not recorded</span>}
                    {r.evidence && <div style={{ fontSize: 12, color: "var(--muted)" }}>Evidence: {r.evidence}</div>}
                    {r.failureReason && <div style={{ fontSize: 12, color: "var(--tone-red)" }}>{r.failureReason}</div>}
                  </div>
                </li>
              ))}
            </ul>
          )}
        </div>
      </section>

      <section className="card" aria-labelledby="merit-components">
        <div className="card-header"><h3 id="merit-components" style={{ fontSize: 15 }}>Merit components</h3></div>
        {data.meritComponents.length === 0 ? (
          <div className="card-body">
            <p className="availability-note" role="note">No merit component breakdown was recorded for this decision.</p>
          </div>
        ) : (
          <div className="table-wrap" style={{ border: "none", borderRadius: 0 }}>
            <table>
              <thead>
                <tr><th>Component</th><th>Raw value</th><th>Normalized</th><th>Weight</th><th>Contribution</th></tr>
              </thead>
              <tbody>
                {data.meritComponents.map((c, i) => (
                  <tr key={i}>
                    <td>{c.name}</td>
                    <td className="font-mono">{n2(c.rawValue)}</td>
                    <td className="font-mono">{n2(c.normalizedValue)}</td>
                    <td className="font-mono">{c.weight === null ? "—" : n2(c.weight)}</td>
                    <td className="font-mono">+{n2(c.contribution)}</td>
                  </tr>
                ))}
                <tr className="grade-total-row">
                  <td>Aggregate</td>
                  <td colSpan={3} />
                  <td className="font-mono">{n2(data.aggregateScore)}</td>
                </tr>
              </tbody>
            </table>
          </div>
        )}
      </section>

      {data.limitations.length > 0 && (
        <section className="card" aria-labelledby="limitations">
          <div className="card-header"><h3 id="limitations" style={{ fontSize: 15 }}>Recorded limitations</h3></div>
          <div className="card-body">
            <ul className="limitation-list">
              {data.limitations.map((l, i) => <li key={i}>{l}</li>)}
            </ul>
          </div>
        </section>
      )}
    </>
  );
}

export default function AdmissionExplanationPage({ params }: { params: Promise<{ applicationId: string }> }) {
  const { applicationId } = use(params);
  const [attempt, setAttempt] = useState(0);
  const state = useAnalyticsFetch(`admission-explanation:${applicationId}:${attempt}`, signal =>
    getAdmissionDecisionExplanation(applicationId, signal)
  );

  const envelope = state.status === "ok" ? state.data : null;
  const data = envelope?.data ?? null;

  return (
    <AppShell>
      <div className="telemetry-page">
        <header className="page-header" style={{ padding: 0 }}>
          <div>
            <div className="page-header-breadcrumb">
              <Link href="/analytics">Analytics</Link><span>/</span><span>Admissions decision</span>
            </div>
            <h1>Admissions decision explanation</h1>
            <p>The recorded merit arithmetic and provenance behind this decision.</p>
          </div>
          <div className="page-header-actions">
            <Link href="/analytics" className="btn btn-secondary btn-sm"><ArrowLeft size={14} /> Analytics</Link>
          </div>
        </header>

        {state.status === "loading" && <LoadingState label="Loading the decision explanation…" lines={5} />}

        {state.status === "error" && isApiError(state.error) && state.error.status === 403 && <ForbiddenState />}

        {state.status === "error" && isApiError(state.error) && state.error.status === 404 && (
          <EmptyState
            icon={FileQuestion}
            title="Decision explanation not available"
            description="No published, reproducible decision record exists for this application — or you are not authorized to view it."
          />
        )}

        {state.status === "error" && (!isApiError(state.error) || (state.error.status !== 403 && state.error.status !== 404)) && (
          <ErrorState error={state.error} title="The explanation could not be loaded" onRetry={() => setAttempt(a => a + 1)} />
        )}

        {envelope && !data && (
          <EmptyState
            icon={FileQuestion}
            title="This decision cannot be explained from current records"
            description={envelope.availability.reason ?? "The recorded decision cannot be exactly reproduced from current source records."}
          />
        )}

        {envelope && data && (
          <>
            <ExplanationMetaPanel envelope={envelope} />
            <AdmissionDetail data={data} />
          </>
        )}
      </div>
    </AppShell>
  );
}
