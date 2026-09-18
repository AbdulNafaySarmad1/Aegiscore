"use client";

import { useState, type FormEvent } from "react";
import { apiGet, apiPost } from "@/lib/api-client";
import { useTenantFormatter } from "@/components/TenantPreferencesProvider";
import { Panel, Figures, Mark, Notice, ProgressRule } from "@/components/register";
import { Ledger, toneOf, words } from "@/components/register/ledger";
import { ModulePage, useAction, useModuleData } from "@/components/register/module-page";

type Metrics = {
  id: string; fiscalYear: string; academicExcellenceScore: number; financialStabilityScore: number; accreditationScore: number;
  studentSuccessScore: number; facultyQualityScore: number; compositeRankingScore: number; generatedAt: string;
  kpis: { enrolment?: number; faculty?: number; researchPublications?: number; facultyToStudentRatio?: number | null; feeCollectionRate?: number | null; averageMarksPercent?: number | null; passRate?: number | null; readinessScore?: number | null; measured?: string[]; unmeasured?: string[]; note?: string };
} | null;
type Goal = { id: string; title: string; description: string | null; currentValue: number; targetValue: number; percentAchieved: number; targetDate: string; status: string };
type Alert = { id: string; severity: string; category: string; message: string | null; recommendedAction: string | null; responsibleOfficer: string | null; isAcknowledged: boolean; createdAt?: string };
type Benchmark = { factorName: string; institutionValue: number; nationalAverage: number; peerAverage: number; peerInstitutionName: string | null; isAboveAverage: boolean; academicYear: string };
type Data = { metrics: Metrics; goals: Goal[]; alerts: Alert[]; benchmarks: Benchmark[] };

const index = (label: string, value: number, measured: boolean) => ({
  label, value: measured ? `${value.toFixed(1)}%` : "Not measured",
  note: measured ? undefined : "no data source in this installation",
  tone: (measured ? (value >= 75 ? "laurel" : value >= 50 ? "brass" : "oxblood") : "ink") as "laurel" | "brass" | "oxblood" | "ink",
});

/** The board's view: indices computed from the registers, strategic goals, alerts and benchmarks. */
export default function BoardPage() {
  const { date } = useTenantFormatter();
  const { state, reload } = useModuleData<Data>(async signal => {
    const [metrics, goals, alerts, benchmarks] = await Promise.all([
      apiGet<Metrics>("/api/board/metrics", signal),
      apiGet<{ total: number; goals: Goal[] } | Goal[]>("/api/board/strategic-goals", signal),
      apiGet<{ total: number; alerts: Alert[] } | Alert[]>("/api/board/alerts", signal),
      apiGet<{ total: number; benchmarks: Benchmark[] } | Benchmark[]>("/api/board/benchmarks?academicYear=" + new Date().getFullYear(), signal),
    ]);
    return {
      metrics,
      goals: Array.isArray(goals) ? goals : goals.goals ?? [],
      alerts: Array.isArray(alerts) ? alerts : alerts.alerts ?? [],
      benchmarks: Array.isArray(benchmarks) ? benchmarks : benchmarks.benchmarks ?? [],
    };
  });
  const action = useAction();
  const [packTitle, setPackTitle] = useState("");
  const [packDate, setPackDate] = useState("");

  async function refreshMetrics(e: FormEvent) {
    e.preventDefault();
    await action.run("metrics", async () => {
      await apiPost(`/api/board/dashboard?userRole=chancellor&fiscalYear=${new Date().getFullYear()}`);
      return "Indices recomputed from the registers.";
    }, reload);
  }
  async function generatePack(e: FormEvent) {
    e.preventDefault();
    await action.run("pack", async () => {
      const pack = await apiPost<{ executiveSummary?: string }>(`/api/board/board-pack?title=${encodeURIComponent(packTitle.trim())}&meetingDate=${encodeURIComponent(packDate)}`);
      return pack?.executiveSummary ? `Board pack prepared. ${pack.executiveSummary}` : "Board pack prepared.";
    }, reload);
  }

  return (
    <ModulePage
      title="Board"
      crumbs={[{ label: "Governance" }, { label: "Board" }]}
      meta={<span>Every index is computed from this installation&apos;s registers; one with no data source says so rather than being estimated.</span>}
      ident={(d: Data) => d.metrics ? <><strong>{d.metrics.compositeRankingScore.toFixed(1)}</strong><br />composite index</> : <><strong>Not computed</strong><br />no indices yet</>}
      state={state} reload={reload} notice={action.outcome}
    >
      {({ metrics, goals, alerts, benchmarks }) => {
        const unmeasured = metrics?.kpis?.unmeasured ?? [];
        const isMeasured = (key: string) => metrics !== null && !unmeasured.includes(key);
        return (
          <div className="record">
            <div className="record-body">
              {metrics === null ? (
                <Notice tone="brass" title="No indices computed yet.">Recompute them from the registers using the panel on the right.</Notice>
              ) : (
                <>
                  <Figures aria-label="Institution indices" items={[
                    index("Academic excellence", metrics.academicExcellenceScore, isMeasured("academicExcellence")),
                    index("Financial stability", metrics.financialStabilityScore, isMeasured("financialStability")),
                    index("Accreditation", metrics.accreditationScore, isMeasured("accreditation")),
                    index("Student success", metrics.studentSuccessScore, isMeasured("studentSuccess")),
                    index("Faculty quality", metrics.facultyQualityScore, isMeasured("facultyQuality")),
                  ]} />
                  {unmeasured.length > 0 && (
                    <Notice tone="ink" role="status">
                      {unmeasured.length === 1 ? "One index has" : `${unmeasured.length} indices have`} no data source in this installation — {unmeasured.map(words).join(", ").toLowerCase()} — and {unmeasured.length === 1 ? "is" : "are"} reported as not measured rather than estimated. Computed {date(metrics.generatedAt)}.
                    </Notice>
                  )}
                  <Panel title="What the indices were computed from" flush>
                    <Ledger rows={[
                      { key: "enrolment", label: "Students on the register", value: metrics.kpis?.enrolment ?? 0 },
                      { key: "faculty", label: "Faculty on the register", value: metrics.kpis?.faculty ?? 0 },
                      { key: "ratio", label: "Faculty to student ratio", value: metrics.kpis?.facultyToStudentRatio == null ? "—" : `1 : ${Math.round(1 / metrics.kpis.facultyToStudentRatio)}` },
                      { key: "fees", label: "Fee collection", value: metrics.kpis?.feeCollectionRate == null ? "no fees billed" : `${(metrics.kpis.feeCollectionRate * 100).toFixed(1)}%` },
                      { key: "marks", label: "Average marks", value: metrics.kpis?.averageMarksPercent == null ? "no results recorded" : `${metrics.kpis.averageMarksPercent.toFixed(1)}%` },
                      { key: "pass", label: "Pass rate", value: metrics.kpis?.passRate == null ? "no results recorded" : `${(metrics.kpis.passRate * 100).toFixed(1)}%` },
                      { key: "readiness", label: "Accreditation readiness", value: metrics.kpis?.readinessScore == null ? "not assessed" : `${metrics.kpis.readinessScore.toFixed(1)}%` },
                      { key: "research", label: "Research publications", value: metrics.kpis?.researchPublications ?? 0 },
                    ]} keyOf={r => r.key} minWidth={420}
                      empty={{ title: "Nothing computed" }}
                      columns={[
                        { key: "label", label: "Figure", render: r => r.label },
                        { key: "value", label: "As recorded", render: r => r.value, num: true },
                      ]} />
                  </Panel>
                </>
              )}

              <Panel title="Strategic goals" aside={<span className="text-muted">{goals.length} tracked</span>}>
                {goals.length === 0
                  ? <p className="text-muted">No strategic goal is recorded. Goals are entered with a target value and date, and their progress is read from the registers.</p>
                  : goals.map(g => (
                    <div key={g.id} className="goal-row">
                      <div>
                        <strong>{g.title}</strong>
                        {g.description && <p className="text-muted">{g.description}</p>}
                      </div>
                      <ProgressRule value={g.currentValue} max={g.targetValue} label={`${g.percentAchieved.toFixed(0)}% of target by ${date(g.targetDate)}`} tone={toneOf(g.status)} />
                    </div>
                  ))}
              </Panel>

              <Panel title="Alerts" aside={<span className="text-muted">{alerts.filter(a => !a.isAcknowledged).length} unacknowledged</span>} flush>
                <Ledger rows={alerts} keyOf={a => a.id} minWidth={680} status={a => toneOf(a.severity)}
                  empty={{ title: "No alerts", detail: "Alerts are raised against the board when a register crosses a threshold." }}
                  columns={[
                    { key: "severity", label: "Severity", render: a => <Mark tone={toneOf(a.severity)}>{words(a.severity)}</Mark> },
                    { key: "category", label: "Category", render: a => words(a.category) },
                    { key: "message", label: "What was seen", render: a => a.message ?? "—" },
                    { key: "action", label: "Recommended", render: a => a.recommendedAction ?? "—" },
                    { key: "officer", label: "Responsible", render: a => a.responsibleOfficer ?? "—" },
                  ]} />
              </Panel>

              <Panel title="Benchmarks" aside={<span className="text-muted">against national and peer averages</span>} flush>
                <Ledger rows={benchmarks} keyOf={b => b.factorName} minWidth={640} status={b => b.isAboveAverage ? "laurel" : "brass"}
                  empty={{ title: "No benchmarks recorded", detail: "Benchmarks are entered per academic year with the national and peer figures to compare against." }}
                  columns={[
                    { key: "factor", label: "Factor", render: b => b.factorName },
                    { key: "institution", label: "This institution", render: b => b.institutionValue.toFixed(1), num: true },
                    { key: "national", label: "National", render: b => b.nationalAverage.toFixed(1), num: true },
                    { key: "peer", label: "Peer", render: b => `${b.peerAverage.toFixed(1)}${b.peerInstitutionName ? ` (${b.peerInstitutionName})` : ""}`, num: true },
                    { key: "standing", label: "Standing", render: b => <Mark tone={b.isAboveAverage ? "laurel" : "brass"}>{b.isAboveAverage ? "Above average" : "Below average"}</Mark> },
                  ]} />
              </Panel>
            </div>

            <aside className="record-margin">
              <Panel title="Recompute indices">
                <form className="panel-form" onSubmit={refreshMetrics}>
                  <p className="field-hint">Reads the student, faculty, results, fee and readiness registers and records the indices with today&apos;s figures.</p>
                  <button type="submit" className={`btn btn-primary${action.busy === "metrics" ? " btn-loading" : ""}`} disabled={action.busy !== null}>Recompute</button>
                </form>
              </Panel>
              <Panel title="Prepare a board pack">
                <form className="panel-form" onSubmit={generatePack}>
                  <label><span>Meeting title</span><input value={packTitle} onChange={e => setPackTitle(e.target.value)} placeholder="e.g. Q3 board meeting" required /></label>
                  <label><span>Meeting date</span><input type="date" value={packDate} onChange={e => setPackDate(e.target.value)} required /></label>
                  <p className="field-hint">The pack&apos;s summary states the figures as they stand; nothing is rounded up for the room.</p>
                  <button type="submit" className={`btn btn-secondary${action.busy === "pack" ? " btn-loading" : ""}`} disabled={action.busy !== null}>Prepare</button>
                </form>
              </Panel>
            </aside>
          </div>
        );
      }}
    </ModulePage>
  );
}
