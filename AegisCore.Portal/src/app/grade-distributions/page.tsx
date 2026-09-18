"use client";

import { useState, type FormEvent } from "react";
import { apiGet, apiPost } from "@/lib/api-client";
import { useTenantFormatter } from "@/components/TenantPreferencesProvider";
import { Panel, Figures, Mark } from "@/components/register";
import { Ledger, toneOf, words } from "@/components/register/ledger";
import { ModulePage, useAction, useModuleData } from "@/components/register/module-page";

type Distribution = { id: string; courseId: string; courseCode: string; courseTitle: string; semester: string; mean: number; median: number; stdDev: number; passRate: number; distinctionRate: number; failureRate: number; enrollmentCount: number; curveShape: string; letterDistributionJson: string; generatedAt: string };
type Anomaly = { id: string; courseCode: string; courseTitle: string; semester: string; anomalyType: string; description: string; recommendedAction: string | null; isResolved: boolean; createdAt: string };
type Data = { distributions: Distribution[]; anomalies: Anomaly[] };

const percent = (v: number) => `${(v * 100).toFixed(1)}%`;

/** Grade distributions: the statistics of the marks recorded for each course, and what looks wrong. */
export default function GradeDistributionsPage() {
  const { date } = useTenantFormatter();
  const { state, reload } = useModuleData<Data>(async signal => {
    const [distributions, anomalies] = await Promise.all([
      apiGet<Distribution[]>("/api/grade-distributions/courses", signal),
      apiGet<Anomaly[]>("/api/grade-distributions/anomalies", signal),
    ]);
    return { distributions, anomalies };
  });
  const action = useAction();
  const [courseId, setCourseId] = useState("");
  const [semester, setSemester] = useState("");

  async function analyse(e: FormEvent) {
    e.preventDefault();
    await action.run("analyse", async () => {
      const d = await apiPost<Distribution>(`/api/grade-distributions/analyze?courseId=${encodeURIComponent(courseId.trim())}&semester=${encodeURIComponent(semester.trim())}`);
      return `Analysed: mean ${d.mean.toFixed(1)}%, ${d.enrollmentCount} students, ${percent(d.passRate)} passed.`;
    }, reload);
  }

  return (
    <ModulePage
      title="Grade distributions"
      crumbs={[{ label: "Academic" }, { label: "Grade distributions" }]}
      meta={<span>Computed from the marks recorded for each course; a course with no results has no distribution.</span>}
      ident={(d: Data) => <><strong>{d.distributions.length}</strong><br />courses analysed</>}
      state={state} reload={reload} notice={action.outcome}
    >
      {({ distributions, anomalies }) => (
        <>
          <Figures aria-label="Distribution figures" items={[
            { label: "Courses analysed", value: distributions.length, note: distributions.length === 0 ? "none yet" : `latest ${date(distributions[0].generatedAt)}` },
            { label: "Mean across courses", value: distributions.length === 0 ? "—" : `${(distributions.reduce((s, d) => s + d.mean, 0) / distributions.length).toFixed(1)}%`, note: "of the analysed courses" },
            { label: "Pass rate", value: distributions.length === 0 ? "—" : percent(distributions.reduce((s, d) => s + d.passRate, 0) / distributions.length), note: "mean of the analysed courses" },
            { label: "Anomalies", value: anomalies.filter(a => !a.isResolved).length, tone: anomalies.some(a => !a.isResolved) ? "brass" : "ink", note: `${anomalies.length} raised` },
          ]} />

          <div className="record">
            <div className="record-body">
              <Panel title="Distributions" flush>
                <Ledger rows={distributions} keyOf={d => d.id} minWidth={860}
                  empty={{ title: "No course analysed yet", detail: "Analyse a course to compute the mean, median, spread and pass rate from its recorded marks." }}
                  columns={[
                    { key: "course", label: "Course", render: d => <><strong>{d.courseCode}</strong> {d.courseTitle}</> },
                    { key: "semester", label: "Semester", render: d => d.semester },
                    { key: "n", label: "Students", render: d => d.enrollmentCount, num: true },
                    { key: "mean", label: "Mean", render: d => d.mean.toFixed(1), num: true },
                    { key: "median", label: "Median", render: d => d.median.toFixed(1), num: true },
                    { key: "sd", label: "SD", render: d => d.stdDev.toFixed(1), num: true },
                    { key: "pass", label: "Passed", render: d => percent(d.passRate), num: true },
                    { key: "fail", label: "Failed", render: d => percent(d.failureRate), num: true },
                    { key: "shape", label: "Curve", render: d => words(d.curveShape) },
                  ]} />
              </Panel>

              <Panel title="Anomalies" aside={<span className="text-muted">{anomalies.filter(a => !a.isResolved).length} open</span>} flush>
                <Ledger rows={anomalies} keyOf={a => a.id} minWidth={720} status={a => a.isResolved ? "laurel" : "brass"}
                  empty={{ title: "No anomalies raised", detail: "An anomaly is raised when a distribution falls outside the expected shape — an unusual failure rate, for instance." }}
                  columns={[
                    { key: "course", label: "Course", render: a => `${a.courseCode} ${a.courseTitle}` },
                    { key: "semester", label: "Semester", render: a => a.semester },
                    { key: "type", label: "Anomaly", render: a => <Mark tone={a.isResolved ? "laurel" : "brass"}>{words(a.anomalyType)}</Mark> },
                    { key: "desc", label: "What was seen", render: a => a.description },
                    { key: "action", label: "Recommended", render: a => a.recommendedAction ?? "—" },
                    { key: "when", label: "Raised", render: a => date(a.createdAt), nowrap: true },
                  ]} />
              </Panel>
            </div>

            <aside className="record-margin">
              <Panel title="Analyse a course">
                <form className="panel-form" onSubmit={analyse}>
                  <label><span>Course id</span><input value={courseId} onChange={e => setCourseId(e.target.value)} placeholder="course GUID" required /></label>
                  <label><span>Semester</span><input value={semester} onChange={e => setSemester(e.target.value)} placeholder="e.g. Fall 2026" required /></label>
                  <p className="field-hint">Computes the statistics from the marks recorded for that course. A course with no marks is refused, not estimated.</p>
                  <button type="submit" className={`btn btn-primary${action.busy === "analyse" ? " btn-loading" : ""}`} disabled={action.busy !== null}>Analyse</button>
                </form>
              </Panel>
            </aside>
          </div>
        </>
      )}
    </ModulePage>
  );
}
