"use client";

import { useState, type FormEvent } from "react";
import { apiGet, apiPost } from "@/lib/api-client";
import { useTenantFormatter } from "@/components/TenantPreferencesProvider";
import { Panel, Figures, Mark } from "@/components/register";
import { Ledger, toneOf, words } from "@/components/register/ledger";
import { ModulePage, useAction, useModuleData } from "@/components/register/module-page";

type Conflict = {
  id: string; type: string; severity: string; sourceCourse: string | null; targetCourse: string | null;
  description: string | null; resolutionSuggestion: string | null; isResolved: boolean; detectedAt: string; resolvedAt: string | null;
};

/** Prerequisite conflicts: circular chains, broken sequences and the students they would block. */
export default function PrerequisiteConflictsPage() {
  const { date } = useTenantFormatter();
  const { state, reload } = useModuleData<Conflict[]>(signal => apiGet<Conflict[]>("/api/prerequisite-conflicts", signal));
  const action = useAction();
  const [courseId, setCourseId] = useState("");

  async function detect(e: FormEvent) {
    e.preventDefault();
    await action.run("detect", async () => {
      const found = await apiPost<Conflict[]>(`/api/prerequisite-conflicts/detect/${courseId.trim()}`);
      return found.length === 0 ? "No conflict found for that course." : `${found.length} conflict(s) found.`;
    }, reload);
  }
  async function simulate(e: FormEvent) {
    e.preventDefault();
    await action.run("simulate", async () => {
      const r = await apiPost<{ directlyAffectedCourses: number; indirectlyAffectedCourses: number; affectedStudents: number; invalidRegistrations: number }>(`/api/prerequisite-conflicts/simulate?eventType=PrerequisiteChanged&changedElementId=${courseId.trim()}`);
      return `Changing it would touch ${r.directlyAffectedCourses} course(s) directly and ${r.indirectlyAffectedCourses} indirectly, affecting ${r.affectedStudents} student(s) and invalidating ${r.invalidRegistrations} registration(s).`;
    });
  }

  return (
    <ModulePage
      title="Prerequisite conflicts"
      crumbs={[{ label: "Academic" }, { label: "Prerequisite conflicts" }]}
      meta={<span>Conflicts found in the prerequisite graph, and what changing a course would break.</span>}
      ident={(d: Conflict[]) => <><strong>{d.filter(c => !c.isResolved).length}</strong><br />open conflicts</>}
      state={state} reload={reload} notice={action.outcome}
    >
      {conflicts => (
        <div className="record">
          <div className="record-body">
            <Figures aria-label="Conflict figures" items={[
              { label: "Open conflicts", value: conflicts.filter(c => !c.isResolved).length, tone: conflicts.some(c => !c.isResolved) ? "brass" : "laurel", note: `${conflicts.length} found in all` },
              { label: "Critical", value: conflicts.filter(c => !c.isResolved && /critical|high/i.test(c.severity)).length, tone: "oxblood", note: "would block registration" },
              { label: "Circular chains", value: conflicts.filter(c => /circular/i.test(c.type)).length, note: "a course requiring itself, directly or through others" },
              { label: "Resolved", value: conflicts.filter(c => c.isResolved).length, tone: "laurel", note: "closed by the registrar" },
            ]} />

            <Panel title="Conflicts" flush>
              <Ledger rows={conflicts} keyOf={c => c.id} minWidth={840} status={c => c.isResolved ? "laurel" : toneOf(c.severity)}
                empty={{ title: "No conflicts found", detail: "Run detection against a course, or a circular check across the whole graph." }}
                columns={[
                  { key: "type", label: "Conflict", render: c => words(c.type) },
                  { key: "severity", label: "Severity", render: c => <Mark tone={toneOf(c.severity)}>{words(c.severity)}</Mark> },
                  { key: "courses", label: "Between", render: c => [c.sourceCourse, c.targetCourse].filter(Boolean).join(" → ") || "—", mono: true },
                  { key: "desc", label: "What was found", render: c => c.description ?? "—" },
                  { key: "suggestion", label: "Suggested fix", render: c => c.resolutionSuggestion ?? "—" },
                  { key: "detected", label: "Found", render: c => date(c.detectedAt), nowrap: true },
                  { key: "resolved", label: "Resolved", render: c => c.resolvedAt ? date(c.resolvedAt) : "open", nowrap: true },
                ]} />
            </Panel>
          </div>

          <aside className="record-margin">
            <Panel title="Check a course">
              <form className="panel-form" onSubmit={detect}>
                <label><span>Course id</span><input value={courseId} onChange={e => setCourseId(e.target.value)} placeholder="course GUID" required /></label>
                <p className="field-hint">Walks the prerequisite graph from that course and records what it finds.</p>
                <button type="submit" className={`btn btn-primary${action.busy === "detect" ? " btn-loading" : ""}`} disabled={action.busy !== null}>Detect conflicts</button>
              </form>
              <form className="panel-form" onSubmit={simulate}>
                <p className="field-hint">Simulation reports what changing that course would break, without changing anything.</p>
                <button type="submit" className={`btn btn-secondary${action.busy === "simulate" ? " btn-loading" : ""}`} disabled={action.busy !== null || !courseId.trim()}>Simulate a change</button>
              </form>
            </Panel>
          </aside>
        </div>
      )}
    </ModulePage>
  );
}
