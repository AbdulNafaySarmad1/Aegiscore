"use client";

import { useState, useEffect } from "react";
import { motion } from "framer-motion";
import { AppShell } from "@/components/layout/AppShell";
import { apiGet, apiPost } from "@/lib/api-client";
import { ClipboardCheck, CheckCircle, XCircle } from "lucide-react";

type Course = { id: string; title: string; code: string };
type Student = { enrollmentId: string; name: string; universityId: string };
type Roster = { students: Student[] };

export default function AttendancePage() {
  const [courses, setCourses] = useState<Course[]>([]);
  const [selectedCourse, setSelectedCourse] = useState<string>("");
  const [roster, setRoster] = useState<Student[]>([]);
  const [statuses, setStatuses] = useState<Record<string, "present" | "absent">>({});
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState("");
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    apiGet<Course[]>("/api/courses/my").then(c => { setCourses(c); setLoading(false); }).catch(() => setLoading(false));
  }, []);

  useEffect(() => {
    if (!selectedCourse) { setRoster([]); return; }
    apiGet<Roster>(`/api/attendance/roster/${selectedCourse}`).then(r => {
      setRoster(r.students ?? []);
      const initial: Record<string, "present" | "absent"> = {};
      for (const s of r.students ?? []) initial[s.enrollmentId] = "present";
      setStatuses(initial);
    }).catch(() => {});
  }, [selectedCourse]);

  const toggle = (id: string) => {
    setStatuses(prev => ({ ...prev, [id]: prev[id] === "present" ? "absent" : "present" }));
  };

  const submit = async () => {
    if (!selectedCourse) return;
    setSaving(true); setMessage("");
    try {
      await apiPost("/api/attendance/mark", {
        courseId: selectedCourse,
        markedByFacultyId: "00000000-0000-0000-0000-000000000000",
        sessionDate: new Date().toISOString(),
        entries: Object.entries(statuses).map(([enrollmentId, status]) => ({
          enrollmentId, status: status === "present" ? 0 : 1,
        })),
      });
      setMessage("Attendance recorded successfully.");
    } catch (e) {
      setMessage(e instanceof Error ? e.message : "Failed to record attendance.");
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
            <p>Mark present/absent for today's session.</p>
          </div>
        </div>

        <div className="card" style={{ marginBottom: 16 }}>
          <div className="card-body">
            {loading ? <div className="skeleton" style={{ height: 40 }} /> : (
              <select value={selectedCourse} onChange={e => setSelectedCourse(e.target.value)} style={{ width: "100%", minHeight: 44, borderRadius: 10, border: "1px solid var(--border)", padding: "0 12px", fontSize: 14, background: "var(--surface)", color: "var(--ink)" }}>
                <option value="">Select a course...</option>
                {courses.map(c => <option key={c.id} value={c.id}>{c.code} — {c.title}</option>)}
              </select>
            )}
          </div>
        </div>

        {roster.length > 0 && (
          <>
            <div style={{ marginBottom: 12, fontSize: 13, color: "var(--muted)" }}>
              {present} of {total} present ({total > 0 ? Math.round(present / total * 100) : 0}%)
            </div>
            <div className="card" style={{ marginBottom: 16 }}>
              <div className="card-body" style={{ display: "grid", gap: 4, padding: 0 }}>
                {roster.map(s => (
                  <button key={s.enrollmentId} onClick={() => toggle(s.enrollmentId)} style={{
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
              {saving ? <span className="btn-loading">Saving...</span> : "Submit Attendance"}
            </button>
          </>
        )}

        {message && (
          <div className={message.includes("success") ? "error-state" : "error-state"} style={{ marginTop: 12, background: message.includes("success") ? "var(--success-soft)" : "var(--danger-soft)", border: message.includes("success") ? "1px solid var(--success)" : "1px solid var(--danger)", color: message.includes("success") ? "var(--success)" : "var(--danger)" }}>
            <ClipboardCheck size={20} />
            <p>{message}</p>
          </div>
        )}
      </div>
    </AppShell>
  );
}
