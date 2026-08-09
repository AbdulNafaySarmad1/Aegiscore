"use client";

// ─── Grade Entry ───
// Faculty mark-entry workflow. The faculty course-list feed and the
// mark-submission route are not yet exposed by the backend
// (docs/FRONTEND_BACKEND_BLOCKERS.md); the page degrades to deliberate
// states rather than a dead form.

import { useState, useEffect } from "react";
import { AppShell } from "@/components/layout/AppShell";
import { apiGet, apiPut, isApiError } from "@/lib/api-client";
import { useSession } from "@/components/session-provider";
import { EmptyState, ErrorState, LoadingState } from "@/components/async/states";
import { ClipboardCheck } from "lucide-react";

type Course = { id: string; title: string; code: string };
type Scheme = { id: string; components: { id: string; name: string; totalMarks: number; weightPercentage: number }[] };
type Student = { enrollmentId: string; studentId: string; name: string; universityId: string };

export default function GradeEntry() {
  const { session } = useSession();
  const [courses, setCourses] = useState<Course[]>([]);
  const [coursesState, setCoursesState] = useState<"loading" | "ready" | "unavailable">("loading");
  const [selectedCourse, setSelectedCourse] = useState("");
  const [scheme, setScheme] = useState<Scheme | null>(null);
  const [students, setStudents] = useState<Student[]>([]);
  const [marks, setMarks] = useState<Record<string, Record<string, number>>>({});
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<{ kind: "success" | "error"; text: string } | null>(null);

  useEffect(() => {
    const ctrl = new AbortController();
    apiGet<Course[]>("/api/courses/my", ctrl.signal)
      .then(c => { setCourses(Array.isArray(c) ? c : []); setCoursesState("ready"); })
      .catch(() => setCoursesState("unavailable"));
    return () => ctrl.abort();
  }, []);

  useEffect(() => {
    if (!selectedCourse) return;
    Promise.all([
      apiGet<Scheme>(`/api/examination/schemes/${selectedCourse}`).catch(() => null),
      apiGet<{ students: Student[] }>(`/api/attendance/roster/${selectedCourse}`).catch(() => null),
    ]).then(([s, r]) => {
      setScheme(s);
      setStudents(r?.students ?? []);
      const initial: Record<string, Record<string, number>> = {};
      for (const st of r?.students ?? []) {
        initial[st.enrollmentId] = {};
        for (const c of s?.components ?? []) {
          initial[st.enrollmentId]![c.id] = 0;
        }
      }
      setMarks(initial);
    });
  }, [selectedCourse]);

  const setMark = (enrollmentId: string, componentId: string, value: number) => {
    setMarks(prev => ({
      ...prev,
      [enrollmentId]: { ...prev[enrollmentId], [componentId]: Math.max(0, Math.min(value, scheme?.components.find(c => c.id === componentId)?.totalMarks ?? 100)) }
    }));
  };

  const submitAll = async () => {
    if (!selectedCourse || !session) return;
    setSaving(true); setMessage(null);
    try {
      for (const enrollmentId of Object.keys(marks)) {
        for (const componentId of Object.keys(marks[enrollmentId] ?? {})) {
          await apiPut("/api/examination/marks", {
            courseId: selectedCourse,
            studentId: students.find(s => s.enrollmentId === enrollmentId)?.studentId ?? "",
            assessmentComponentId: componentId,
            obtainedMarks: marks[enrollmentId]?.[componentId] ?? 0,
            enteredBy: session.userId,
          });
        }
      }
      setMessage({ kind: "success", text: "All marks submitted successfully." });
    } catch (e) {
      setMessage({ kind: "error", text: isApiError(e) ? e.message : "Failed to submit marks." });
    } finally { setSaving(false); }
  };

  return (
    <AppShell>
      <div className="content-page">
        <div className="page-header">
          <div>
            <div className="page-header-breadcrumb"><span>Academic</span><span>/</span><span>Grade Entry</span></div>
            <h1>Grade Entry</h1>
            <p>Enter marks for assignments, quizzes, and midterm.</p>
          </div>
        </div>

        {coursesState === "loading" && <LoadingState lines={3} label="Loading your courses" />}

        {coursesState === "unavailable" && (
          <>
            <ErrorState title="Course list unavailable" error={null} />
            <p style={{ fontSize: 13, color: "var(--muted)", marginTop: 8 }}>
              Grade entry requires the faculty course-feed endpoint, which is not yet exposed by the
              backend. This is tracked in <code>docs/FRONTEND_BACKEND_BLOCKERS.md</code>.
            </p>
          </>
        )}

        {coursesState === "ready" && courses.length === 0 && (
          <EmptyState
            icon={ClipboardCheck}
            title="No assigned courses"
            description="No course assignments were returned for your account. Contact your department if this looks wrong."
          />
        )}

        {coursesState === "ready" && courses.length > 0 && (
          <div className="card" style={{ marginBottom: 16 }}>
            <div className="card-body">
              <label className="field-label" style={{ display: "grid", gap: 6, fontSize: 13, fontWeight: 500 }}>
                Course
                <select value={selectedCourse} onChange={e => setSelectedCourse(e.target.value)}>
                  <option value="">Select a course…</option>
                  {courses.map(c => <option key={c.id} value={c.id}>{c.code} — {c.title}</option>)}
                </select>
              </label>
            </div>
          </div>
        )}

        {scheme && students.length > 0 && (
          <>
            <div className="card" style={{ marginBottom: 12 }}>
              <div className="card-header"><h3 style={{ fontSize: 14 }}>Assessment Breakdown</h3></div>
              <div className="card-body" style={{ display: "flex", gap: 16, flexWrap: "wrap" }}>
                {scheme.components.map(c => (
                  <div key={c.id} className="badge badge-primary" style={{ padding: "8px 12px" }}>
                    {c.name}: {c.totalMarks} marks ({c.weightPercentage}%)
                  </div>
                ))}
              </div>
            </div>

            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Student</th>
                    <th>ID</th>
                    {scheme.components.map(c => <th key={c.id}>{c.name}</th>)}
                  </tr>
                </thead>
                <tbody>
                  {students.map(s => (
                    <tr key={s.enrollmentId}>
                      <td style={{ fontWeight: 600 }}>{s.name}</td>
                      <td style={{ color: "var(--muted)", fontSize: 12 }}>{s.universityId}</td>
                      {scheme.components.map(c => (
                        <td key={c.id}>
                          <input
                            type="number"
                            min={0}
                            max={c.totalMarks}
                            aria-label={`${s.name} — ${c.name}`}
                            value={marks[s.enrollmentId]?.[c.id] ?? 0}
                            onChange={e => setMark(s.enrollmentId, c.id, parseInt(e.target.value) || 0)}
                            style={{ width: 70, textAlign: "center", minHeight: 36, borderRadius: 6, border: "1px solid var(--border)", padding: "2px 8px", fontSize: 13 }}
                          />
                          <small style={{ display: "block", color: "var(--muted)", fontSize: 10 }}>/ {c.totalMarks}</small>
                        </td>
                      ))}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <button className="btn btn-primary btn-lg" style={{ width: "100%", marginTop: 16 }} onClick={submitAll} disabled={saving}>
              {saving ? "Saving…" : "Submit All Marks"}
            </button>
          </>
        )}

        {message && (
          <div className="error-state" role={message.kind === "error" ? "alert" : "status"} style={message.kind === "success" ? { marginTop: 12, background: "var(--success-soft)", borderColor: "var(--success)", color: "var(--success)" } : { marginTop: 12 }}>
            <ClipboardCheck size={18} /><p>{message.text}</p>
          </div>
        )}
      </div>
    </AppShell>
  );
}
