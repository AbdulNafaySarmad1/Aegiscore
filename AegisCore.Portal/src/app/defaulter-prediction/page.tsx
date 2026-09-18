"use client";

import { useState, type FormEvent } from "react";
import { apiGet, apiPost } from "@/lib/api-client";
import { useTenantFormatter } from "@/components/TenantPreferencesProvider";
import { Panel, Figures, Mark } from "@/components/register";
import { Ledger, words } from "@/components/register/ledger";
import { ModulePage, useAction, useModuleData } from "@/components/register/module-page";

type Score = { id: string; studentId: string; studentName: string; semester: string; probability: number; tier: string; confidence: number; predictedAt: string };
type Intervention = { id: string; studentName: string; type: string; triggeredByTier: string; initiatedAt: string; completedAt: string | null; outcome: string | null; isSuccessful: boolean };
type Data = { scores: Score[]; tiers: { tier: string; count: number }[]; interventions: Intervention[] };

const tierTone = (tier: string) => /red/i.test(tier) ? "oxblood" : /orange|yellow/i.test(tier) ? "brass" : "laurel";

/** Fee-default risk: each assessed student's standing and the interventions raised from it. */
export default function DefaulterPredictionPage() {
  const { date } = useTenantFormatter();
  const { state, reload } = useModuleData<Data>(async signal => {
    const [tiers, interventions] = await Promise.all([
      apiGet<{ scores: Score[]; tiers: { tier: string; count: number }[] }>("/api/defaulter-prediction/tiers", signal),
      apiGet<Intervention[]>("/api/defaulter-prediction/interventions", signal),
    ]);
    return { scores: tiers.scores, tiers: tiers.tiers, interventions };
  });
  const action = useAction();
  const [studentId, setStudentId] = useState("");
  const [semester, setSemester] = useState("");

  async function assess(e: FormEvent) {
    e.preventDefault();
    await action.run("assess", async () => {
      const s = await apiPost<{ defaultProbabilityScore?: number; riskTier?: string }>(`/api/defaulter-prediction/predict/${studentId.trim()}?semester=${encodeURIComponent(semester.trim())}`);
      return `Assessed: ${s?.riskTier ?? "recorded"}${s?.defaultProbabilityScore !== undefined ? ` at ${(s.defaultProbabilityScore * 100).toFixed(0)}%` : ""}.`;
    }, reload);
  }

  return (
    <ModulePage
      title="Fee-default risk"
      crumbs={[{ label: "Finance" }, { label: "Fee-default risk" }]}
      meta={<span>Computed from each student&apos;s outstanding balance, how late it is, and their scholarship cover.</span>}
      ident={(d: Data) => <><strong>{d.scores.length}</strong><br />students assessed</>}
      state={state} reload={reload} notice={action.outcome}
    >
      {({ scores, tiers, interventions }) => (
        <div className="record">
          <div className="record-body">
            <Figures aria-label="Risk figures" items={[
              { label: "Students assessed", value: scores.length, note: scores.length === 0 ? "none yet" : `latest ${date(scores[0].predictedAt)}` },
              { label: "High risk", value: scores.filter(s => /red|orange/i.test(s.tier)).length, tone: "oxblood", note: "red or orange tier" },
              { label: "Interventions", value: interventions.length, note: `${interventions.filter(i => i.isSuccessful).length} recorded as successful` },
              { label: "Mean probability", value: scores.length === 0 ? "—" : `${(scores.reduce((s, x) => s + x.probability, 0) / scores.length * 100).toFixed(0)}%`, note: "of those assessed" },
            ]} />

            <Panel title="Risk by tier" flush>
              <Ledger rows={tiers} keyOf={t => t.tier} minWidth={300} status={t => tierTone(t.tier)}
                empty={{ title: "Nothing assessed", detail: "Assess a student for a semester to record their standing." }}
                columns={[
                  { key: "tier", label: "Tier", render: t => <Mark tone={tierTone(t.tier)}>{words(t.tier)}</Mark> },
                  { key: "count", label: "Students", render: t => t.count, num: true },
                ]} />
            </Panel>

            <Panel title="Assessed students" aside={<span className="text-muted">highest risk first</span>} flush>
              <Ledger rows={scores} keyOf={s => s.id} minWidth={720} status={s => tierTone(s.tier)}
                empty={{ title: "No student assessed", detail: "Each assessment reads the student's fee demands and scholarships for the semester." }}
                columns={[
                  { key: "student", label: "Student", render: s => s.studentName },
                  { key: "id", label: "Student number", render: s => s.studentId, mono: true },
                  { key: "semester", label: "Semester", render: s => s.semester },
                  { key: "probability", label: "Probability", render: s => `${(s.probability * 100).toFixed(0)}%`, num: true },
                  { key: "tier", label: "Tier", render: s => <Mark tone={tierTone(s.tier)}>{words(s.tier)}</Mark> },
                  { key: "when", label: "Assessed", render: s => date(s.predictedAt), nowrap: true },
                ]} />
            </Panel>

            <Panel title="Interventions" aside={<span className="text-muted">{interventions.length} raised</span>} flush>
              <Ledger rows={interventions} keyOf={i => i.id} minWidth={680} status={i => i.isSuccessful ? "laurel" : i.completedAt ? "ink" : "brass"}
                empty={{ title: "No intervention raised", detail: "An intervention — a reminder, an outreach call, an escalation — is raised from a student's tier." }}
                columns={[
                  { key: "student", label: "Student", render: i => i.studentName },
                  { key: "type", label: "Intervention", render: i => words(i.type) },
                  { key: "tier", label: "Raised by", render: i => <Mark tone={tierTone(i.triggeredByTier)}>{words(i.triggeredByTier)}</Mark> },
                  { key: "started", label: "Started", render: i => date(i.initiatedAt), nowrap: true },
                  { key: "outcome", label: "Outcome", render: i => i.outcome ? words(i.outcome) : i.completedAt ? "closed" : "open" },
                ]} />
            </Panel>
          </div>

          <aside className="record-margin">
            <Panel title="Assess a student">
              <form className="panel-form" onSubmit={assess}>
                <label><span>Student id</span><input value={studentId} onChange={e => setStudentId(e.target.value)} placeholder="student GUID" required /></label>
                <label><span>Semester</span><input value={semester} onChange={e => setSemester(e.target.value)} placeholder="e.g. Fall 2026" required /></label>
                <p className="field-hint">Reads the student&apos;s fee demands, how overdue they are and their scholarship cover for that semester.</p>
                <button type="submit" className={`btn btn-primary${action.busy === "assess" ? " btn-loading" : ""}`} disabled={action.busy !== null}>Assess</button>
              </form>
            </Panel>
          </aside>
        </div>
      )}
    </ModulePage>
  );
}
