"use client";

import { useState } from "react";
import { CheckInButton } from "@/components/attendance/CheckInButton";
import { apiGet } from "@/lib/api-client";
import { loadSession } from "@/lib/session";
import { Panel, Notice } from "@/components/register";
import { ModulePage, useModuleData } from "@/components/register/module-page";

type Course = { id: string; code: string | null; title: string };
type Enrolled = { studentId: string; courses: Course[] };

/** A student marking themselves present for a class they are sitting in. */
export default function StudentAttendancePage() {
  const { state, reload } = useModuleData<Enrolled>(async signal => {
    const session = await loadSession();
    if (!session.studentId) return { studentId: "", courses: [] };
    const courses = await apiGet<Course[]>(`/api/attendance/student-courses/${encodeURIComponent(session.studentId)}`, signal);
    return { studentId: session.studentId, courses };
  });
  const [courseId, setCourseId] = useState("");

  return (
    <ModulePage
      title="Check in"
      crumbs={[{ label: "My record" }, { label: "Check in" }]}
      meta={<span>Marking yourself present for a class you are in now.</span>}
      state={state} reload={reload}
    >
      {({ studentId, courses }) => (
        <div className="page-narrow">
          {!studentId ? (
            <Notice tone="brass" title="This account is not a student record.">
              Checking in is for students. If you teach this class, the roll is on the attendance screen.
            </Notice>
          ) : courses.length === 0 ? (
            <Notice tone="brass" title="You are not enrolled on any course.">
              Ask the registrar if a course you are attending is missing.
            </Notice>
          ) : (
            <Panel title="Which class">
              <div className="panel-form">
                <label><span>Course</span>
                  <select value={courseId} onChange={e => setCourseId(e.target.value)}>
                    <option value="">Choose a course</option>
                    {courses.map(c => <option key={c.id} value={c.id}>{c.code ? `${c.code} — ` : ""}{c.title}</option>)}
                  </select>
                </label>
                {courseId && <CheckInButton studentId={studentId} courseId={courseId} />}
                <p className="field-hint">
                  Your device is asked where it is. The answer decides whether you are inside the
                  campus boundary, and is then stored encrypted against this check-in. Staff screens
                  show only whether it was inside or outside, never the coordinates.
                </p>
              </div>
            </Panel>
          )}
        </div>
      )}
    </ModulePage>
  );
}
