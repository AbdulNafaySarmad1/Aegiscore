"use client";

// ─── Attendance Marking ───
// Faculty daily workflow. Requires a faculty course-list and roster feed;
// those backend contracts are not yet available (see
// docs/FRONTEND_BACKEND_BLOCKERS.md), so the page degrades to a deliberate
// unavailable state instead of a dead selector.

import { useState, useEffect } from "react";
import { AppShell } from "@/components/layout/AppShell";
import { apiGet, apiPost, isApiError } from "@/lib/api-client";
import { useSession } from "@/components/session-provider";
import { EmptyState, ErrorState, LoadingState } from "@/components/async/states";
import { ClipboardCheck, CheckCircle, XCircle } from "lucide-react";

type Course = { id: string; title: string; code: string };
type Student = { enrollmentId: string; name: string; universityId: string };
type Roster = { students: Student[] };

export default function AttendancePage() {
  const { session } = useSession();
  const [courses, setCourses] = useState<Course[]>([]);
  const [coursesState, setCoursesState] = useState<"loading" | "ready" | "unavailable">("loading");
  const [selectedCourse, setSelectedCourse] = useState<string>("");
  // Roster is keyed by course so a stale roster is never shown for a new selection.
  const [rosterState, setRosterState] = useState<{ course: string; students: Student[]; unavailable: boolean }>({ course: "", students: [], unavailable: false });
  const [statuses, setStatuses] = useState<Record<string, "present" | "absent">>({});
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<{ kind: "success" | "error"; text: string } | null>(null);

  const roster = rosterState.course === selectedCourse ? rosterState.students : [];
  const rosterUnavailable = rosterState.course === selectedCourse && rosterState.unavailable;

  const [attempt, setAttempt] = useState(0);
  useEffect(() => {
    const ctrl = new AbortController();
    apiGet<Course[]>("/api/courses/my", ctrl.signal)
      .then(c => { setCourses(Array.isArray(c) ? c : []); setCoursesState("ready"); })
      .catch(() => { if (!ctrl.signal.aborted) setCoursesState("unavailable"); });
    return () => ctrl.abort();
  }, [attempt]);

  useEffect(() => {
    if (!selectedCourse) return;
    const ctrl = new AbortController();
    apiGet<Roster>(`/api/attendance/roster/${selectedCourse}`, ctrl.signal)
      .then(r => {
        const students = r.students ?? [];
        setRosterState({ course: selectedCourse, students, unavailable: false });
        const initial: Record<string, "present" | "absent"> = {};
        for (const s of students) initial[s.enrollmentId] = "present";
        setStatuses(initial);
      })
      .catch(() => { if (!ctrl.signal.aborted) setRosterState({ course: selectedCourse, students: [], unavailable: true }); });
    return () => ctrl.abort();
  }, [selectedCourse]);

  const toggle = (id: string) => {
    setStatuses(prev => ({ ...prev, [id]: prev[id] === "present" ? "absent" : "present" }));
  };

  const submit = async () => {
    if (!selectedCourse || !session) return;
    setSaving(true); setMessage(null);
    try {
      await apiPost("/api/attendance/mark", {
        courseId: selectedCourse,
        markedByFacultyId: session.userId,
        sessionDate: new Date().toISOString(),
        entries: Object.entries(statuses).map(([enrollmentId, status]) => ({
          enrollmentId, status: status === "present" ? 0 : 1,
        })),
      });
      setMessage({ kind: "success", text: "Attendance recorded successfully." });
    } catch (e) {
      setMessage({ kind: "error", text: isApiError(e) ? e.message : "Failed to record attendance." });
    } finally {
      setSaving(false);
    }
  };

  const present = Object.values(statuses).filter(s => s === "present").length;
  const total = Object.keys(statuses).length;

  return (
    <AppShell>
      <div className="content-page">
        <div className="page-header">
          <div>
            <div className="page-header-breadcrumb"><span>Academic</span><span>/</span><span>Attendance</span></div>
            <h1>Attendance Marking</h1>
            <p>Mark present/absent for today&rsquo;s session.</p>
          </div>
        </div>

        {coursesState === "loading" && <LoadingState lines={3} label="Loading your courses" />}

        {coursesState === "unavailable" && (
          <ErrorState
            title="Course list unavailable"
            error={null}
            onRetry={() => { setCoursesState("loading"); setAttempt(a => a + 1); }}
          />
        )}

        {coursesState === "unavailable" && (
          <p style={{ fontSize: 13, color: "var(--muted)", marginTop: 8 }}>
            Attendance marking requires the faculty course-feed endpoint, which is not yet exposed by the
            backend. This is tracked in <code>docs/FRONTEND_BACKEND_BLOCKERS.md</code>.
          </p>
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

        {rosterUnavailable && (
          <ErrorState title="Roster unavailable" error={null} />
        )}

        {roster.length > 0 && (
          <>
            <div style={{ marginBottom: 12, fontSize: 13, color: "var(--muted)" }}>
              {present} of {total} present ({total > 0 ? Math.round(present / total * 100) : 0}%)
            </div>
            <div className="card" style={{ marginBottom: 16 }}>
              <div className="card-body" style={{ display: "grid", gap: 0, padding: 0 }}>
                {roster.map(s => (
                  <button key={s.enrollmentId} onClick={() => toggle(s.enrollmentId)} aria-pressed={statuses[s.enrollmentId] === "present"} style={{
                    display: "flex", alignItems: "center", justifyContent: "space-between", padding: "12px 16px",
                    border: "none", borderBottom: "1px solid var(--border)", background: "transparent", cursor: "pointer",
                    width: "100%", textAlign: "left", fontSize: 13, minHeight: 48,
                  }}>
                    <span><strong>{s.name}</strong><small style={{ display: "block", color: "var(--muted)", fontSize: 11 }}>{s.universityId}</small></span>
                    <span className={`badge ${statuses[s.enrollmentId] === "present" ? "badge-success" : "badge-danger"}`}>
                      {statuses[s.enrollmentId] === "present" ? <><CheckCircle size={14} />Present</> : <><XCircle size={14} />Absent</>}
                    </span>
                  </button>
                ))}
              </div>
            </div>
            <button className="btn btn-primary btn-lg" style={{ width: "100%" }} onClick={submit} disabled={saving}>
              {saving ? "Saving…" : "Submit Attendance"}
            </button>
          </>
        )}

        {message && (
          <div className={message.kind === "success" ? "form-notice info" : "error-state"} style={message.kind === "success" ? { marginTop: 12, borderColor: "var(--success)", background: "var(--success-soft)", color: "var(--success)" } : { marginTop: 12 }} role={message.kind === "error" ? "alert" : "status"}>
            <ClipboardCheck size={18} />
            <p>{message.text}</p>
          </div>
        )}
      </div>
    </AppShell>
  );
}
