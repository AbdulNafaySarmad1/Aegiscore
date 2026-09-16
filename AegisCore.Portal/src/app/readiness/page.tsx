"use client";

import { useCallback, useEffect, useState } from "react";
import { apiGet, apiPost, ApiError } from "@/lib/api-client";
import { useTenantFormatter } from "@/components/TenantPreferencesProvider";
import { RegisterHead, Panel, Figures, Facts, Mark, Notice, ErrorNotice, Empty, RuledLoading, type Tone } from "@/components/register";
import type { ReadinessCurrent } from "@/lib/kpi-sources";

type Gap = { id: string; criterionId: string; gapDescription?: string | null; evidenceNeeded?: string | null; effort: string; impactOnScore: number; priorityRank: number };
type Alert = { id: string; title: string; message?: string | null; category: string; isAcknowledged: boolean; triggeredAt: string };
type Framework = ReadinessCurrent["frameworks"][number] & {
  nextAuditDate?: string | null; daysUntilAudit: number; complianceVelocity: number;
  categoryScores?: Record<string, number> | null; gaps: Gap[]; alerts: Alert[];
};
type Current = Omit<ReadinessCurrent, "frameworks"> & { frameworks: Framework[] };

type State = { kind: "loading" } | { kind: "ready"; data: Current } | { kind: "forbidden" } | { kind: "error"; message: string };

/** Criterion rows are written by the scoring engine as free JSON; read the common field names and ignore the rest. */
type Criterion = { id: string; name: string; category?: string; score?: number; maxScore?: number; status?: string };
function criteriaOf(raw: unknown): Criterion[] {
  if (!Array.isArray(raw)) return [];
  return raw.flatMap((item, index) => {
    if (!item || typeof item !== "object") return [];
    const r = item as Record<string, unknown>;
    const text = (...keys: string[]) => keys.map(k => r[k]).find(v => typeof v === "string") as string | undefined;
    const num = (...keys: string[]) => keys.map(k => r[k]).find(v => typeof v === "number") as number | undefined;
    return [{
      id: text("id", "criterionId", "code") ?? String(index),
      name: text("name", "title", "criterion") ?? text("id", "criterionId") ?? `Criterion ${index + 1}`,
      category: text("category", "standard"),
      score: num("score", "achieved"),
      maxScore: num("maxScore", "max", "weight"),
      status: text("status"),
    }];
  });
}

const statusTone: Record<string, Tone> = { Excellent: "laurel", Good: "laurel", Satisfactory: "ink", NeedsImprovement: "brass", CriticalRisk: "oxblood" };
const statusLabel: Record<string, string> = { Excellent: "Excellent", Good: "Good", Satisfactory: "Satisfactory", NeedsImprovement: "Needs improvement", CriticalRisk: "Critical risk" };
const effortLabel: Record<string, string> = { QuickWin: "Quick win", ShortTerm: "Short term", MediumTerm: "Medium term", LongTerm: "Long term" };
const alertTone = (category: string): Tone => category === "CRITICAL" ? "oxblood" : category === "WARNING" ? "brass" : "ink";

export default function ReadinessPage() {
  const { date, dateTime } = useTenantFormatter();
  const [state, setState] = useState<State>({ kind: "loading" });
  const [checking, setChecking] = useState(false);
  const [checkNote, setCheckNote] = useState<string | null>(null);

  const load = useCallback((signal?: AbortSignal) =>
    apiGet<Current>("/api/readiness/current", signal)
      .then(data => ({ kind: "ready", data } as const))
      .catch(error => {
        if (signal?.aborted) return null;
        if (error instanceof ApiError && error.status === 403) return { kind: "forbidden" } as const;
        return { kind: "error", message: "The readiness record could not be loaded." } as const;
      }), []);

  const [retry, setRetry] = useState(0);
  useEffect(() => {
    const abort = new AbortController();
    // eslint-disable-next-line react-hooks/set-state-in-effect -- reset on retry.
    setState({ kind: "loading" });
    load(abort.signal).then(next => { if (next && !abort.signal.aborted) setState(next); });
    return () => abort.abort();
  }, [load, retry]);

  async function checkFreshness() {
    setChecking(true); setCheckNote(null);
    try {
      await apiPost("/api/readiness/check-freshness");
      const next = await load();
      if (next) setState(next);
      setCheckNote("Evidence freshness checked.");
    } catch (error) {
      setCheckNote(error instanceof ApiError && error.status === 403 ? "Your role cannot run a freshness check." : "The freshness check did not complete.");
    } finally { setChecking(false); }
  }

  const crumbs = [{ label: "Compliance" }, { label: "Accreditation" }];

  if (state.kind === "loading") return <div className="page"><RuledLoading label="Opening the readiness record" rows={5} /></div>;
  if (state.kind === "forbidden") return (
    <div className="page">
      <RegisterHead title="Accreditation readiness" crumbs={crumbs} reveal={false} />
      <Notice tone="brass" title="Not available to your role.">Readiness scores are visible to the offices that hold accreditation responsibility.</Notice>
    </div>
  );
  if (state.kind === "error") return (
    <div className="page">
      <RegisterHead title="Accreditation readiness" crumbs={crumbs} reveal={false} />
      <ErrorNotice message={state.message} hint="Check your connection and try again." onRetry={() => setRetry(n => n + 1)} />
    </div>
  );

  const { data } = state;
  const frameworks = data.frameworks;
  const evidenced = frameworks.filter(f => criteriaOf(f.criteria).length > 0);
  const openGaps = frameworks.reduce((n, f) => n + f.gaps.length, 0);
  const nearestAudit = frameworks.filter(f => f.nextAuditDate).sort((a, b) => a.daysUntilAudit - b.daysUntilAudit)[0];

  const checkButton = (
    <button type="button" className={`btn btn-secondary${checking ? " btn-loading" : ""}`} disabled={checking} onClick={checkFreshness} aria-describedby={checkNote ? "freshness-note" : undefined}>
      {checking ? "Checking evidence" : "Check evidence freshness"}
    </button>
  );

  return (
    <div className="page">
      <RegisterHead
        title="Accreditation readiness"
        crumbs={crumbs}
        ident={data.overall && evidenced.length === frameworks.length
          ? <><strong>{data.overall.score}%</strong><br />{data.overall.frameworkCount === 1 ? frameworks[0].frameworkName : `${data.overall.frameworkCount} frameworks`}</>
          : data.overall
            ? <><strong>Unevidenced</strong><br />Score lacks criteria</>
            : <><strong>Not assessed</strong><br />No score recorded</>}
        meta={data.overall ? <span>Latest assessment {date(data.overall.generatedAt)}. Scores are recorded by framework; the overall figure is their plain mean.</span> : undefined}
        actions={<>{checkButton}{checkNote && <span id="freshness-note" role="status" className="field-hint">{checkNote}</span>}</>}
      />

      {frameworks.length === 0 ? (
        <Empty title="No readiness score has been recorded.">
          A score appears here once an accreditation framework assessment has been run for this institution. Nothing is estimated in its place.
        </Empty>
      ) : (
        <>
          <Figures aria-label="Readiness figures" items={[
            evidenced.length === frameworks.length
              ? { label: "Overall score", value: `${data.overall!.score}%`, note: `across ${frameworks.length === 1 ? "one framework" : `${frameworks.length} frameworks`}`, tone: "laurel" }
              : { label: "Overall score", value: "—", note: evidenced.length === 0 ? "recorded without criterion evidence" : `${frameworks.length - evidenced.length} of ${frameworks.length} frameworks lack criterion evidence`, tone: "brass" },
            nearestAudit
              ? { label: "Days until audit", value: nearestAudit.daysUntilAudit, note: `${nearestAudit.frameworkName}, ${date(nearestAudit.nextAuditDate!)}`, tone: nearestAudit.daysUntilAudit <= 30 ? "oxblood" : nearestAudit.daysUntilAudit <= 90 ? "brass" : undefined }
              : { label: "Days until audit", value: "—", note: "no audit date set" },
            { label: "Open gaps", value: openGaps, note: openGaps === 0 ? "none outstanding" : "across all frameworks", tone: openGaps > 0 ? "brass" : "laurel" },
          ]} />

          {frameworks.map(framework => {
            const criteria = criteriaOf(framework.criteria);
            return (
              <Panel key={framework.scoreId} title={framework.frameworkName} aside={<Mark tone={statusTone[framework.status] ?? "ink"}>{statusLabel[framework.status] ?? framework.status}</Mark>} tone={statusTone[framework.status]}>
                <Facts inline aria-label={`${framework.frameworkName} assessment`} items={[
                  { label: "Score", value: `${framework.overallScore}%`, mono: true },
                  { label: "Assessed", value: dateTime(framework.generatedAt) },
                  { label: "Compliance velocity", value: `${framework.complianceVelocity > 0 ? "+" : ""}${framework.complianceVelocity}% per month`, mono: true },
                  { label: "Next audit", value: framework.nextAuditDate ? date(framework.nextAuditDate) : "Not scheduled" },
                ]} />

                {criteria.length === 0 ? (
                  <Notice tone="brass" title="Recorded without criterion evidence.">
                    This score was stored with no per-criterion breakdown, so it cannot be traced to evidence. It is shown here for the record but is not reported as the institution&rsquo;s accreditation score.
                  </Notice>
                ) : (
                  <div className="ledger-wrap">
                    <table className="ledger" style={{ minWidth: 640 }} aria-label={`${framework.frameworkName} criteria`}>
                      <thead><tr><th scope="col">Criterion</th><th scope="col">Category</th><th scope="col" className="num">Score</th><th scope="col">Status</th></tr></thead>
                      <tbody>
                        {criteria.map(c => (
                          <tr key={c.id}>
                            <td>{c.name}</td>
                            <td>{c.category ?? <span className="text-muted">—</span>}</td>
                            <td className="num mono">{c.score !== undefined ? (c.maxScore !== undefined ? `${c.score} of ${c.maxScore}` : c.score) : "—"}</td>
                            <td>{c.status ? <Mark tone={/not/i.test(c.status) ? "oxblood" : /partial/i.test(c.status) ? "brass" : "laurel"}>{c.status}</Mark> : <span className="text-muted">—</span>}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}

                <h3 className="panel-subtitle">Open gaps</h3>
                {framework.gaps.length === 0 ? (
                  <p className="text-muted">No open gaps are recorded against this framework.</p>
                ) : (
                  <div className="ledger-wrap">
                    <table className="ledger" style={{ minWidth: 760 }} aria-label={`${framework.frameworkName} open gaps`}>
                      <thead><tr><th scope="col" className="num">Priority</th><th scope="col">Criterion</th><th scope="col">Gap</th><th scope="col">Evidence needed</th><th scope="col">Effort</th><th scope="col" className="num">Impact</th></tr></thead>
                      <tbody>
                        {framework.gaps.map(g => (
                          <tr key={g.id} data-status={g.priorityRank === 1 ? "oxblood" : undefined}>
                            <td className="num mono">{g.priorityRank}</td>
                            <td className="mono">{g.criterionId}</td>
                            <td>{g.gapDescription ?? <span className="text-muted">—</span>}</td>
                            <td>{g.evidenceNeeded ?? <span className="text-muted">Not specified</span>}</td>
                            <td className="nowrap">{effortLabel[g.effort] ?? g.effort}</td>
                            <td className="num mono nowrap">{g.impactOnScore > 0 ? `+${g.impactOnScore}` : g.impactOnScore} pts</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}

                {framework.alerts.length > 0 && (
                  <>
                    <h3 className="panel-subtitle">Alerts</h3>
                    <ul className="notice-list">
                      {framework.alerts.map(a => (
                        <li key={a.id}>
                          <Notice tone={alertTone(a.category)} title={a.title} actions={a.isAcknowledged ? <Mark tone="ink">Acknowledged</Mark> : undefined}>
                            {a.message}{a.message ? " " : ""}<span className="text-muted">{dateTime(a.triggeredAt)}</span>
                          </Notice>
                        </li>
                      ))}
                    </ul>
                  </>
                )}
              </Panel>
            );
          })}
        </>
      )}
    </div>
  );
}
