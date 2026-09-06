"use client";

import { useState, useEffect } from "react";
import { motion } from "framer-motion";
import { AppShell } from "@/components/layout/AppShell";
import { FocusTrapDialog } from "@/components/FocusTrapDialog";
import { apiGet, apiPost } from "@/lib/api-client";
import { ClipboardCheck, CheckCircle, XCircle } from "lucide-react";

type Course = { id: string; title: string; code: string };
type Student = { enrollmentId: string; name: string; universityId: string; attendanceId: string | null; locationStatus: "ON_CAMPUS" | "OFF_CAMPUS" | "UNVERIFIABLE" | null; isManualOverride: boolean };
type Roster = { students: Student[] };

export default function AttendancePage() {
  const [courses, setCourses] = useState<Course[]>([]);
  const [selectedCourse, setSelectedCourse] = useState<string>("");
  const [roster, setRoster] = useState<Student[]>([]);
  const [statuses, setStatuses] = useState<Record<string, "present" | "absent">>({});
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState("");
  const [loading, setLoading] = useState(true);
  const [offCampusOnly, setOffCampusOnly] = useState(false);
  const [overrideTarget, setOverrideTarget] = useState<Student | null>(null);
  const [overrideReason, setOverrideReason] = useState("");

  useEffect(() => {
    apiGet<Course[]>("/api/courses/my").then(c => { setCourses(c); setLoading(false); }).catch(() => setLoading(false));
  }, []);

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect -- clearing dependent data is intentional.
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
  const submitOverride = async () => {
    if (!overrideTarget?.attendanceId || !overrideReason.trim()) return;
    setSaving(true);
    try {
      await apiPost(`/api/attendance/${overrideTarget.attendanceId}/override`, { reason: overrideReason.trim() });
      setRoster(current => current.map(student => student.enrollmentId === overrideTarget.enrollmentId
        ? { ...student, isManualOverride: true }
        : student));
      setMessage(`Location review approved for ${overrideTarget.name}.`);
      setOverrideTarget(null);
      setOverrideReason("");
    } catch (reason) {
      setMessage(reason instanceof Error ? reason.message : "The location override could not be saved.");
    } finally {
      setSaving(false);
    }
  };


  const present = Object.values(statuses).filter(s => s === "present").length;
  const visibleRoster = offCampusOnly ? roster.filter(student => student.locationStatus === "OFF_CAMPUS" && !student.isManualOverride) : roster;
  const total = Object.keys(statuses).length;

  return (
    <AppShell>
      <div className="content-page">
        <div className="page-header">
          <div>
            <div className="page-header-breadcrumb"><span>Academic</span><span>/</span><span>Attendance</span></div>
            <h1>Attendance Marking</h1>
            <p>Mark present/absent for today&apos;s session.</p>
          </div>
        </div>

        <div className="card" style={{ marginBottom: 16 }}>
          <div className="card-body">
            {loading ? <div className="skeleton" style={{ height: 40 }} /> : (
              <select aria-label="Course" value={selectedCourse} onChange={e => setSelectedCourse(e.target.value)} style={{ width: "100%", minHeight: 44, borderRadius: 10, border: "1px solid var(--border)", padding: "0 12px", fontSize: 14, background: "var(--surface)", color: "var(--ink)" }}>
                <option value="">Select a course...</option>
                {courses.map(c => <option key={c.id} value={c.id}>{c.code} — {c.title}</option>)}
              </select>
            )}
          </div>
        </div>

        {roster.length > 0 && (
          <>
            <div style={{ marginBottom: 12, display: "flex", alignItems: "center", justifyContent: "space-between", gap: 12, fontSize: 13, color: "var(--muted)" }}>
              <span>{present} of {total} present ({total > 0 ? Math.round(present / total * 100) : 0}%)</span>
              <label style={{ display: "flex", alignItems: "center", gap: 7 }}>
                <input type="checkbox" checked={offCampusOnly} onChange={event => setOffCampusOnly(event.target.checked)} />
                Show only off-campus
              </label>
            </div>
            <div className="card" style={{ marginBottom: 16 }}>
              <div className="card-body" style={{ display: "grid", gap: 4, padding: 0 }}>
                {visibleRoster.length === 0 ? <p style={{ padding: 16 }}>No off-campus check-ins need review.</p> : visibleRoster.map(s => (
                  <div key={s.enrollmentId} style={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: 12, padding: "12px 16px", borderBottom: "1px solid var(--border)" }}>
                    <button type="button" aria-label={`Mark ${s.name} ${statuses[s.enrollmentId] === "present" ? "absent" : "present"}`} aria-pressed={statuses[s.enrollmentId] === "present"} onClick={() => toggle(s.enrollmentId)} style={{ display: "flex", alignItems: "center", justifyContent: "space-between", flex: 1, border: 0, background: "transparent", cursor: "pointer", textAlign: "left", fontSize: 13, minHeight: 48 }}>
                      <span><strong>{s.name}</strong><small style={{ display: "block", color: "var(--muted)", fontSize: 11 }}>{s.universityId}</small></span>
                      <span className={`badge ${statuses[s.enrollmentId] === "present" ? "badge-success" : "badge-danger"}`}>
                        {statuses[s.enrollmentId] === "present" ? <><CheckCircle size={14} />Present</> : <><XCircle size={14} />Absent</>}
                      </span>
                    </button>
                    <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
                      <span className={`badge ${s.isManualOverride || s.locationStatus === "ON_CAMPUS" ? "badge-success" : s.locationStatus === "OFF_CAMPUS" ? "badge-danger" : "badge-warning"}`}>
                        {s.isManualOverride ? "MANUAL OVERRIDE" : s.locationStatus?.replaceAll("_", " ") ?? "NOT CHECKED IN"}
                      </span>
                      {s.locationStatus === "OFF_CAMPUS" && !s.isManualOverride && s.attendanceId && (
                        <button type="button" className="btn btn-secondary btn-sm" onClick={() => { setOverrideTarget(s); setOverrideReason(""); }}>Override</button>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </div>
            <button className="btn btn-primary btn-lg" style={{ width: "100%" }} onClick={submit} disabled={saving}>
              {saving ? <span className="btn-loading">Saving...</span> : "Submit Attendance"}
            </button>
          </>
        )}
        {overrideTarget && (
          <FocusTrapDialog labelledBy="override-title" onClose={() => setOverrideTarget(null)}>
            <div className="wizard-card">
              <h2 id="override-title">Override off-campus check-in</h2>
              <p>Approve {overrideTarget.name}&apos;s attendance without viewing or exposing their coordinates.</p>
              <label className="wizard-field">
                <span>Reason *</span>
                <textarea value={overrideReason} maxLength={500} rows={4} onChange={event => setOverrideReason(event.target.value)} placeholder="Explain why this check-in is being approved" />
              </label>
              <div className="wizard-footer">
                <button type="button" className="wizard-secondary" onClick={() => setOverrideTarget(null)}>Cancel</button>
                <button type="button" className="wizard-primary" disabled={saving || !overrideReason.trim()} onClick={() => void submitOverride()}>{saving ? "Saving override…" : "Approve attendance"}</button>
              </div>
            </div>
          </FocusTrapDialog>
        )}


        {message && (
          <div role={message.includes("success") ? "status" : "alert"} className="error-state" style={{ marginTop: 12, background: message.includes("success") ? "var(--success-soft)" : "var(--danger-soft)", border: message.includes("success") ? "1px solid var(--success)" : "1px solid var(--danger)", color: message.includes("success") ? "var(--success)" : "var(--danger)" }}>
            <ClipboardCheck size={20} />
            <p>{message}</p>
          </div>
        )}
      </div>
    </AppShell>
  );
}
