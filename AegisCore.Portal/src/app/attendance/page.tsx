"use client";

import { useState } from "react";
import { CheckInButton } from "@/components/attendance/CheckInButton";
import { apiGet, apiPost } from "@/lib/api-client";
import { Panel, Mark, Notice } from "@/components/register";
import { ModulePage, useAction, useModuleData } from "@/components/register/module-page";

type Course = { id: string; title: string; code: string };
type Student = {
  enrollmentId: string; name: string; universityId: string; attendanceId: string | null;
  locationStatus: "ON_CAMPUS" | "OFF_CAMPUS" | "UNVERIFIABLE" | null; isManualOverride: boolean;
};
type Roster = { students: Student[] };

const PLACE: Record<string, { label: string; tone: "laurel" | "oxblood" | "brass" }> = {
  ON_CAMPUS: { label: "On campus", tone: "laurel" },
  OFF_CAMPUS: { label: "Off campus", tone: "oxblood" },
  UNVERIFIABLE: { label: "Location unknown", tone: "brass" },
};

/**
 * Marking the roll. Everyone starts as present — the convention of a roll call — and the teacher
 * marks the exceptions, so the screen says so plainly rather than letting a silent default decide.
 */
export default function AttendancePage() {
  const { state, reload } = useModuleData<Course[]>(signal => apiGet<Course[]>("/api/courses/my", signal));
  const action = useAction();
  const [courseId, setCourseId] = useState("");
  const [absent, setAbsent] = useState<Set<string>>(new Set());
  const [offCampusOnly, setOffCampusOnly] = useState(false);
  const [checkInStudent, setCheckInStudent] = useState("");
  const [override, setOverride] = useState<{ student: Student; reason: string } | null>(null);

  const roster = useModuleData<Roster>(
    signal => courseId ? apiGet<Roster>(`/api/attendance/roster/${courseId}`, signal) : Promise.resolve({ students: [] }),
    courseId
  );

  async function submit(students: Student[]) {
    await action.run("mark", async () => {
      const result = await apiPost<{ entriesCreated: number; entriesAlreadyMarked: number; markedByName: string | null }>("/api/attendance/mark", {
        courseId,
        sessionDate: new Date().toISOString(),
        entries: students.map(s => ({ enrollmentId: s.enrollmentId, status: absent.has(s.enrollmentId) ? 1 : 0 })),
      });
      const who = result.markedByName ? ` against ${result.markedByName}` : "";
      return result.entriesAlreadyMarked > 0
        ? `${result.entriesCreated} recorded${who}; ${result.entriesAlreadyMarked} were already marked for today and were left as they were.`
        : `Today's roll is recorded${who}: ${students.length - absent.size} present, ${absent.size} absent.`;
    }, roster.reload);
  }

  async function approve() {
    const { student, reason } = override!;
    setOverride(null);
    await action.run("override", async () => {
      await apiPost(`/api/attendance/${student.attendanceId}/override`, { reason: reason.trim() });
      return `${student.name}'s attendance stands, with your reason on the record.`;
    }, roster.reload);
  }

  return (
    <ModulePage
      title="Attendance"
      crumbs={[{ label: "Academic" }, { label: "Attendance" }]}
      meta={<span>Today&apos;s roll for one of your courses. It is recorded against the faculty member the course is assigned to.</span>}
      ident={(courses: Course[]) => <><strong>{courses.length}</strong><br />{courses.length === 1 ? "course" : "courses"}</>}
      state={state} reload={reload} notice={action.outcome}
    >
      {courses => {
        const students = roster.state.kind === "ready" ? roster.state.data.students : [];
        const shown = offCampusOnly ? students.filter(s => s.locationStatus === "OFF_CAMPUS" && !s.isManualOverride) : students;
        const present = students.length - students.filter(s => absent.has(s.enrollmentId)).length;
        return (
          <div className="record">
            <div className="record-body">
              <Panel title="Which course">
                <div className="panel-form">
                  <label><span>Course</span>
                    <select value={courseId} onChange={e => { setCourseId(e.target.value); setAbsent(new Set()); setCheckInStudent(""); }}>
                      <option value="">Choose a course</option>
                      {courses.map(c => <option key={c.id} value={c.id}>{c.code} — {c.title}</option>)}
                    </select>
                  </label>
                  {courses.length === 0 && <p className="field-hint">No course is assigned to you, so there is no roll to mark.</p>}
                </div>
              </Panel>

              {override && (
                <Panel tone="brass" title={`Let ${override.student.name}'s attendance stand?`}>
                  <div className="panel-form">
                    <p>
                      This student checked in from outside the campus boundary. Approving keeps the
                      attendance and records your reason against it. Their coordinates are not shown
                      to you here.
                    </p>
                    <label><span>Why</span><textarea value={override.reason} maxLength={500} rows={3} onChange={e => setOverride({ ...override, reason: e.target.value })} placeholder="e.g. teaching practice at the partner school today" /></label>
                    <div className="btn-row">
                      <button type="button" className="btn btn-primary" disabled={!override.reason.trim()} onClick={approve}>Approve and record the reason</button>
                      <button type="button" className="btn btn-ghost" onClick={() => setOverride(null)}>Cancel</button>
                    </div>
                  </div>
                </Panel>
              )}

              {courseId && (
                <Panel
                  title="The roll"
                  aside={<>
                    <span>{present} of {students.length} present</span>
                    <label className="check-row"><input type="checkbox" checked={offCampusOnly} onChange={e => setOffCampusOnly(e.target.checked)} /><span>only those off campus</span></label>
                  </>}
                  flush
                >
                  {roster.state.kind === "loading" ? (
                    <p className="field-hint" style={{ padding: "1rem 1.5rem" }}>Reading the roster…</p>
                  ) : roster.state.kind === "error" ? (
                    <p className="field-hint" style={{ padding: "1rem 1.5rem" }}>{roster.state.message}</p>
                  ) : shown.length === 0 ? (
                    <p className="field-hint" style={{ padding: "1rem 1.5rem" }}>
                      {students.length === 0 ? "Nobody is enrolled on this course." : "Nobody checked in from off campus."}
                    </p>
                  ) : (
                    <ul className="roll">
                      {shown.map(s => {
                        const isAbsent = absent.has(s.enrollmentId);
                        const place = s.isManualOverride ? { label: "Approved by you", tone: "laurel" as const } : s.locationStatus ? PLACE[s.locationStatus] : null;
                        return (
                          <li key={s.enrollmentId}>
                            <button
                              type="button"
                              className="roll-name"
                              aria-pressed={!isAbsent}
                              onClick={() => setAbsent(a => { const n = new Set(a); if (n.has(s.enrollmentId)) n.delete(s.enrollmentId); else n.add(s.enrollmentId); return n; })}
                            >
                              <span><strong>{s.name}</strong><small>{s.universityId}</small></span>
                              <Mark tone={isAbsent ? "oxblood" : "laurel"}>{isAbsent ? "Absent" : "Present"}</Mark>
                            </button>
                            <span className="roll-place">
                              {place ? <Mark tone={place.tone}>{place.label}</Mark> : <span className="text-muted">did not check in</span>}
                              {s.locationStatus === "OFF_CAMPUS" && !s.isManualOverride && s.attendanceId && (
                                <button type="button" className="btn btn-ghost btn-sm" onClick={() => setOverride({ student: s, reason: "" })}>Review</button>
                              )}
                            </span>
                          </li>
                        );
                      })}
                    </ul>
                  )}
                </Panel>
              )}

              {courseId && students.length > 0 && (
                <div className="btn-row">
                  <button type="button" className={`btn btn-primary${action.busy === "mark" ? " btn-loading" : ""}`} disabled={action.busy !== null} onClick={() => submit(students)}>
                    Record today&apos;s roll
                  </button>
                  <span className="text-muted">{present} present, {absent.size} absent</span>
                </div>
              )}
            </div>

            <aside className="record-margin">
              {courseId && students.length > 0 && (
                <Panel title="Check a student in">
                  <div className="panel-form">
                    <label><span>Student</span>
                      <select value={checkInStudent} onChange={e => setCheckInStudent(e.target.value)}>
                        <option value="">Choose a student</option>
                        {students.map(s => <option key={s.enrollmentId} value={s.universityId}>{s.name}</option>)}
                      </select>
                    </label>
                    {checkInStudent && <CheckInButton key={`${courseId}:${checkInStudent}`} courseId={courseId} studentId={checkInStudent} />}
                    <p className="field-hint">
                      This uses the location of the device you are holding. The coordinates are stored
                      encrypted against the check-in; this screen shows only whether they fell inside
                      the campus boundary.
                    </p>
                  </div>
                </Panel>
              )}

              <Notice tone="ink">
                Everyone is present until you mark them absent, which is how a roll call is read aloud.
                A student already marked today is left alone rather than overwritten.
              </Notice>
            </aside>
          </div>
        );
      }}
    </ModulePage>
  );
}
