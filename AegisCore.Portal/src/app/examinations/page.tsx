"use client";

import { useState, useEffect } from "react";
import { motion } from "framer-motion";
import { AppShell } from "@/components/layout/AppShell";
import { apiGet, apiPost, apiPut } from "@/lib/api-client";
import { ClipboardCheck, FileText } from "lucide-react";

type Course = { id: string; title: string; code: string };
type Scheme = { id: string; components: { id: string; name: string; totalMarks: number; weightPercentage: number }[] };
type Student = { enrollmentId: string; studentId: string; name: string; universityId: string };
type MarksEntry = { courseId: string; studentId: string; assessmentComponentId: string; obtainedMarks: number; enteredBy: string };

export default function GradeEntry() {
  const [courses, setCourses] = useState<Course[]>([]);
  const [selectedCourse, setSelectedCourse] = useState("");
  const [scheme, setScheme] = useState<Scheme | null>(null);
  const [students, setStudents] = useState<Student[]>([]);
  const [marks, setMarks] = useState<Record<string, Record<string, number>>>({});
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState("");
  const [loading, setLoading] = useState(true);

  useEffect(() => { apiGet<Course[]>("/api/courses/my").then(setCourses).catch(()=>{}).finally(()=>setLoading(false)); }, []);

  useEffect(() => {
    if (!selectedCourse) return;
    Promise.all([
      apiGet<Scheme>(`/api/examination/schemes/${selectedCourse}`).catch(() => null),
      apiGet<{students: Student[]}>(`/api/attendance/roster/${selectedCourse}`).catch(() => null),
    ]).then(([s, r]) => {
      setScheme(s);
      setStudents(r?.students ?? []);
      const initial: Record<string, Record<string, number>> = {};
      for (const st of r?.students ?? []) {
        initial[st.enrollmentId] = {};
        for (const c of s?.components ?? []) {
          initial[st.enrollmentId][c.id] = 0;
        }
      }
      setMarks(initial);
    });
  }, [selectedCourse]);

  const setMark = (enrollmentId: string, componentId: string, value: number) => {
    setMarks(prev => ({
      ...prev,
      [enrollmentId]: { ...prev[enrollmentId], [componentId]: Math.max(0, Math.min(value, scheme?.components.find(c=>c.id===componentId)?.totalMarks ?? 100)) }
    }));
  };

  const submitAll = async () => {
    if (!selectedCourse) return;
    setSaving(true); setMessage("");
    try {
      for (const enrollmentId of Object.keys(marks)) {
        for (const componentId of Object.keys(marks[enrollmentId] ?? {})) {
          await apiPut("/api/examination/marks", {
            courseId: selectedCourse,
            studentId: students.find(s => s.enrollmentId === enrollmentId)?.studentId ?? "",
            assessmentComponentId: componentId,
            obtainedMarks: marks[enrollmentId]?.[componentId] ?? 0,
            enteredBy: "00000000-0000-0000-0000-000000000000",
          });
        }
      }
      setMessage("All marks submitted successfully.");
    } catch (e) {
      setMessage(e instanceof Error ? e.message : "Failed to submit marks.");
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

        <div className="card" style={{ marginBottom: 16 }}>
          <div className="card-body">
            {loading ? <div className="skeleton" style={{ height: 40 }} /> : (
              <select value={selectedCourse} onChange={e => setSelectedCourse(e.target.value)} style={{ width: "100%", minHeight: 44, borderRadius: 10, border: "1px solid var(--border)", padding: "0 12px", fontSize: 14 }}>
                <option value="">Select a course...</option>
                {courses.map(c => <option key={c.id} value={c.id}>{c.code} — {c.title}</option>)}
              </select>
            )}
          </div>
        </div>

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
              {saving ? <span className="btn-loading">Saving...</span> : "Submit All Marks"}
            </button>
          </>
        )}

        {message && (
          <div className={message.includes("success") ? "error-state" : "error-state"} style={{ marginTop: 12, background: message.includes("success") ? "var(--success-soft)" : "var(--danger-soft)", border: message.includes("success") ? "1px solid var(--success)" : "1px solid var(--danger)", color: message.includes("success") ? "var(--success)" : "var(--danger)" }}>
            <ClipboardCheck size={20} /><p>{message}</p>
          </div>
        )}
      </div>
    </AppShell>
  );
}
