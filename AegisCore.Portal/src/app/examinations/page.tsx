"use client";

import { useState } from "react";
import { apiGet, apiPut } from "@/lib/api-client";
import { Panel, Notice, Mark } from "@/components/register";
import { ModulePage, useAction, useModuleData } from "@/components/register/module-page";

type Course = { id: string; title: string; code: string };
type Component = { id: string; componentType: string; totalMarks: number; weightPercentage: number };
type Scheme = { id: string; components: Component[] };
type Student = { enrollmentId: string; studentId: string; name: string; universityId: string };

/**
 * Entering marks. A mark is recorded against the person signed in, and only the boxes actually
 * changed are sent, so opening the screen and closing it again writes nothing.
 */
export default function GradeEntryPage() {
  const { state, reload } = useModuleData<Course[]>(signal => apiGet<Course[]>("/api/courses/my", signal));
  const action = useAction();
  const [courseId, setCourseId] = useState("");
  const [marks, setMarks] = useState<Record<string, number>>({});   // "enrollmentId:componentId" → mark

  const sheet = useModuleData<{ scheme: Scheme | null; students: Student[] }>(
    async signal => {
      if (!courseId) return { scheme: null, students: [] };
      const [scheme, roster] = await Promise.all([
        apiGet<Scheme>(`/api/examination/schemes/by-course/${courseId}`, signal).catch(() => null),
        apiGet<{ students: Student[] }>(`/api/attendance/roster/${courseId}`, signal).catch(() => ({ students: [] })),
      ]);
      return { scheme, students: roster.students ?? [] };
    },
    courseId
  );

  async function submit(scheme: Scheme, students: Student[]) {
    const entries = Object.entries(marks);
    await action.run("save", async () => {
      let written = 0;
      for (const [key, value] of entries) {
        const [enrollmentId, componentId] = key.split(":");
        const student = students.find(s => s.enrollmentId === enrollmentId);
        if (!student) continue;
        await apiPut("/api/examination/marks", {
          courseId,
          studentId: student.studentId,
          assessmentComponentId: componentId,
          obtainedMarks: value,
        });
        written++;
      }
      setMarks({});
      return `${written} mark${written === 1 ? "" : "s"} recorded against your name. Nothing else on the sheet was touched.`;
    }, sheet.reload);
  }

  return (
    <ModulePage
      title="Marks"
      crumbs={[{ label: "Academic" }, { label: "Marks" }]}
      meta={<span>Marks for one of your courses, component by component. Only what you change is written.</span>}
      ident={(courses: Course[]) => <><strong>{courses.length}</strong><br />{courses.length === 1 ? "course" : "courses"}</>}
      state={state} reload={reload} notice={action.outcome}
    >
      {courses => {
        const data = sheet.state.kind === "ready" ? sheet.state.data : { scheme: null, students: [] };
        const { scheme, students } = data;
        const changed = Object.keys(marks).length;
        return (
          <>
            <Panel title="Which course">
              <div className="panel-form">
                <label><span>Course</span>
                  <select value={courseId} onChange={e => { setCourseId(e.target.value); setMarks({}); }}>
                    <option value="">Choose a course</option>
                    {courses.map(c => <option key={c.id} value={c.id}>{c.code} — {c.title}</option>)}
                  </select>
                </label>
                {courses.length === 0 && <p className="field-hint">No course is assigned to you, so there are no marks to enter.</p>}
              </div>
            </Panel>

            {courseId && sheet.state.kind === "loading" && <Panel><p>Reading the marks sheet…</p></Panel>}

            {courseId && sheet.state.kind === "ready" && !scheme && (
              <Notice tone="brass" title="This course has no assessment scheme.">
                Until someone sets out its components — what the quizzes, assignments and papers are
                worth — there is nothing to enter marks against.
              </Notice>
            )}

            {courseId && scheme && students.length === 0 && (
              <Notice tone="brass" title="Nobody is enrolled on this course.">
                There is no one to give a mark to yet.
              </Notice>
            )}

            {scheme && students.length > 0 && (
              <>
                <Panel title="How this course is assessed">
                  <ul className="scheme-list">
                    {scheme.components.map(c => (
                      <li key={c.id}>
                        <strong>{c.componentType}</strong>
                        <span>{c.totalMarks} marks</span>
                        <span>{c.weightPercentage}% of the course</span>
                      </li>
                    ))}
                  </ul>
                  {scheme.components.reduce((n, c) => n + c.weightPercentage, 0) !== 100 && (
                    <p className="field-hint">
                      These components add to {scheme.components.reduce((n, c) => n + c.weightPercentage, 0)}% of the course, not 100%.
                    </p>
                  )}
                </Panel>

                <Panel title="Marks" aside={changed > 0 ? <Mark tone="brass">{changed} unsaved</Mark> : <span className="text-muted">nothing changed</span>} flush>
                  <div className="ledger-wrap">
                    <table className="ledger" style={{ minWidth: 640 }}>
                      <thead>
                        <tr>
                          <th scope="col">Student</th>
                          <th scope="col">Number</th>
                          {scheme.components.map(c => <th key={c.id} scope="col" className="num">{c.componentType}<br /><span className="text-muted">out of {c.totalMarks}</span></th>)}
                        </tr>
                      </thead>
                      <tbody>
                        {students.map(s => (
                          <tr key={s.enrollmentId}>
                            <td>{s.name}</td>
                            <td className="mono">{s.universityId}</td>
                            {scheme.components.map(c => {
                              const key = `${s.enrollmentId}:${c.id}`;
                              return (
                                <td key={c.id} className="num">
                                  <input
                                    type="number" min={0} max={c.totalMarks} inputMode="numeric"
                                    aria-label={`${s.name}, ${c.componentType}, out of ${c.totalMarks}`}
                                    value={marks[key] ?? ""}
                                    placeholder="—"
                                    onChange={e => {
                                      const raw = e.target.value;
                                      if (raw === "") { const next = { ...marks }; delete next[key]; setMarks(next); return; }
                                      setMarks({ ...marks, [key]: Math.max(0, Math.min(Number(raw), c.totalMarks)) });
                                    }}
                                    className="mark-input"
                                  />
                                </td>
                              );
                            })}
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </Panel>

                <div className="btn-row">
                  <button type="button" className={`btn btn-primary${action.busy === "save" ? " btn-loading" : ""}`} disabled={action.busy !== null || changed === 0} onClick={() => submit(scheme, students)}>
                    Record {changed === 0 ? "marks" : changed === 1 ? "one mark" : `${changed} marks`}
                  </button>
                  {changed > 0 && <button type="button" className="btn btn-ghost" onClick={() => setMarks({})}>Discard what I typed</button>}
                </div>

                <Notice tone="ink">
                  A blank box is not a zero: it is left alone. Marks are locked once moderation has
                  been submitted for the course, and an attempt to change them then is refused.
                </Notice>
              </>
            )}
          </>
        );
      }}
    </ModulePage>
  );
}
