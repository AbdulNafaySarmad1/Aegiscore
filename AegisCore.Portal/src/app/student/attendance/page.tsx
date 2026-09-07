"use client";

import { useEffect, useState } from "react";
import { AppShell } from "@/components/layout/AppShell";
import { CheckInButton } from "@/components/attendance/CheckInButton";
import { apiGet } from "@/lib/api-client";
import { ClipboardCheck } from "lucide-react";
import { loadSession } from "@/lib/session";

type Course = { id: string; code: string | null; title: string };

export default function StudentAttendancePage() {
  const [studentId, setStudentId] = useState("");
  const [courses, setCourses] = useState<Course[]>([]);
  const [courseId, setCourseId] = useState("");
  const [loading, setLoading] = useState(true);
  useEffect(() => {
    loadSession().then(session => setStudentId(session.studentId ?? "")).catch(() => setLoading(false));
  }, []);


  useEffect(() => {
    if (!studentId) { Promise.resolve().then(() => setLoading(false)); return; }
    apiGet<Course[]>(`/api/attendance/student-courses/${encodeURIComponent(studentId)}`)
      .then(setCourses)
      .finally(() => setLoading(false));
  }, [studentId]);

  return (
    <AppShell>
      <div className="content-page">
        <header className="page-header">
          <div>
            <div className="page-header-breadcrumb"><span>Academic</span><span>/</span><span>Attendance</span></div>
            <h1>Mark Attendance</h1>
            <p>Select today&apos;s course. Location is used only to classify the check-in and is encrypted at rest.</p>
          </div>
          <ClipboardCheck aria-hidden="true" />
        </header>
        <section className="card">
          <div className="card-body" style={{ display: "grid", gap: 16 }}>
            {loading ? <p>Loading your enrolled courses…</p> : courses.length === 0 ? (
              <div className="empty-state"><strong>No enrolled courses are available.</strong><p>Contact the registrar if an active course is missing.</p></div>
            ) : (
              <>
                <label style={{ display: "grid", gap: 6 }}>
                  <span>Course</span>
                  <select value={courseId} onChange={event => setCourseId(event.target.value)}>
                    <option value="">Select a course…</option>
                    {courses.map(course => <option key={course.id} value={course.id}>{course.code ? `${course.code} — ` : ""}{course.title}</option>)}
                  </select>
                </label>
                {courseId && <CheckInButton studentId={studentId} courseId={courseId} />}
              </>
            )}
          </div>
        </section>
      </div>
    </AppShell>
  );
}
