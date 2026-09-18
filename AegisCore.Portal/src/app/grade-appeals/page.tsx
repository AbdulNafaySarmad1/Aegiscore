"use client";

import { useState, type FormEvent } from "react";
import { apiGet, apiPost } from "@/lib/api-client";
import { useTenantFormatter } from "@/components/TenantPreferencesProvider";
import { Panel, Figures, Mark } from "@/components/register";
import { Ledger, toneOf, words } from "@/components/register/ledger";
import { ModulePage, useAction, useModuleData } from "@/components/register/module-page";

type Appeal = {
  id: string; studentId: string; studentName: string; courseCode: string; courseTitle: string; semester: string;
  submittedGrade: string; groundType: string; groundStrength: string; status: string; committeeDecision: string | null;
  adjustedGrade: string | null; submissionDate: string; deadline: string | null; resolvedAt: string | null; isEligible: boolean;
};

/** Grade appeals: what was appealed, on what ground, and what the committee decided. */
export default function GradeAppealsPage() {
  const { date } = useTenantFormatter();
  const { state, reload } = useModuleData<Appeal[]>(signal => apiGet<Appeal[]>("/api/grade-appeals", signal));
  const action = useAction();
  const [selected, setSelected] = useState("");
  const [decision, setDecision] = useState("upheld");
  const [adjusted, setAdjusted] = useState("");

  async function classify(e: FormEvent) {
    e.preventDefault();
    await action.run("classify", async () => { await apiPost(`/api/grade-appeals/${selected}/classify`); return "Appeal classified against the policy."; }, reload);
  }
  async function brief(e: FormEvent) {
    e.preventDefault();
    await action.run("brief", async () => { await apiPost(`/api/grade-appeals/${selected}/committee-brief`); return "Committee brief prepared."; }, reload);
  }
  async function decide(e: FormEvent) {
    e.preventDefault();
    await action.run("decide", async () => {
      await apiPost(`/api/grade-appeals/${selected}/decision?decision=${encodeURIComponent(decision)}${adjusted.trim() ? `&adjustedGrade=${encodeURIComponent(adjusted.trim())}` : ""}`);
      return `Decision recorded: ${decision}${adjusted.trim() ? `, grade adjusted to ${adjusted.trim()}` : ""}.`;
    }, reload);
  }

  return (
    <ModulePage
      title="Grade appeals"
      crumbs={[{ label: "Academic" }, { label: "Grade appeals" }]}
      meta={<span>Appeals against a recorded grade, their ground, and the committee&apos;s decision.</span>}
      ident={(d: Appeal[]) => <><strong>{d.filter(a => !a.resolvedAt).length}</strong><br />open appeals</>}
      state={state} reload={reload} notice={action.outcome}
    >
      {appeals => (
        <div className="record">
          <div className="record-body">
            <Figures aria-label="Appeal figures" items={[
              { label: "Appeals", value: appeals.length, note: appeals.length === 0 ? "none filed" : `latest ${date(appeals[0].submissionDate)}` },
              { label: "Open", value: appeals.filter(a => !a.resolvedAt).length, tone: "brass", note: "awaiting the committee" },
              { label: "Grades adjusted", value: appeals.filter(a => a.adjustedGrade).length, tone: "laurel", note: "appeals upheld" },
              { label: "Out of time", value: appeals.filter(a => !a.isEligible).length, note: "filed after the deadline" },
            ]} />

            <Panel title="Appeals" flush>
              <Ledger rows={appeals} keyOf={a => a.id} minWidth={900} status={a => toneOf(a.status)}
                empty={{ title: "No appeal filed", detail: "A student files an appeal against a recorded grade, citing a ground from the policy." }}
                columns={[
                  { key: "student", label: "Student", render: a => a.studentName },
                  { key: "id", label: "Student number", render: a => a.studentId, mono: true },
                  { key: "course", label: "Course", render: a => `${a.courseCode} ${a.courseTitle}` },
                  { key: "semester", label: "Semester", render: a => a.semester },
                  { key: "grade", label: "Grade", render: a => a.submittedGrade, mono: true },
                  { key: "ground", label: "Ground", render: a => <>{words(a.groundType)}<br /><span className="text-muted">{words(a.groundStrength)} evidence</span></> },
                  { key: "status", label: "Status", render: a => <Mark tone={toneOf(a.status)}>{words(a.status)}</Mark> },
                  { key: "decision", label: "Decision", render: a => a.committeeDecision ?? (a.isEligible ? "—" : "out of time") },
                  { key: "adjusted", label: "Adjusted to", render: a => a.adjustedGrade ?? "—", mono: true },
                  { key: "filed", label: "Filed", render: a => date(a.submissionDate), nowrap: true },
                ]} />
            </Panel>
          </div>

          <aside className="record-margin">
            <Panel title="Work an appeal">
              <form className="panel-form" onSubmit={classify}>
                <label><span>Appeal</span>
                  <select value={selected} onChange={e => setSelected(e.target.value)} required>
                    <option value="">Choose an appeal</option>
                    {appeals.filter(a => !a.resolvedAt).map(a => <option key={a.id} value={a.id}>{a.studentName} — {a.courseCode}</option>)}
                  </select>
                </label>
                <p className="field-hint">Classification tests the ground against the policy and records the clause it cites.</p>
                <button type="submit" className={`btn btn-secondary${action.busy === "classify" ? " btn-loading" : ""}`} disabled={action.busy !== null || !selected}>Classify</button>
              </form>
              <form className="panel-form" onSubmit={brief}>
                <p className="field-hint">The brief assembles the evidence for the committee to read.</p>
                <button type="submit" className={`btn btn-secondary${action.busy === "brief" ? " btn-loading" : ""}`} disabled={action.busy !== null || !selected}>Prepare committee brief</button>
              </form>
              <form className="panel-form" onSubmit={decide}>
                <label><span>Decision</span>
                  <select value={decision} onChange={e => setDecision(e.target.value)}>
                    <option value="upheld">Upheld</option>
                    <option value="rejected">Rejected</option>
                  </select>
                </label>
                <label><span>Adjusted grade</span><input value={adjusted} onChange={e => setAdjusted(e.target.value)} placeholder="only if upheld" /></label>
                <button type="submit" className={`btn btn-primary${action.busy === "decide" ? " btn-loading" : ""}`} disabled={action.busy !== null || !selected}>Record decision</button>
              </form>
            </Panel>
          </aside>
        </div>
      )}
    </ModulePage>
  );
}
