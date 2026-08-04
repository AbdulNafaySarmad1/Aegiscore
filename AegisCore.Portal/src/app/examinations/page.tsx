"use client";

import { useEffect, useState } from "react";
import { apiGet, apiPost } from "@/lib/api-client";
import { AlertCircle, BarChart3, BookOpen, CheckCircle2, ClipboardList, FileText, GraduationCap, PenLine, Send } from "lucide-react";
import Link from "next/link";

type AssessmentScheme = { id: string; name: string; courseId: string; courseName: string; components: { label: string; weight: number }[] };

type PublicationSlot = { id: string; courseId: string; courseName: string; semester: string; status: "Draft" | "Ready" | "Published"; students: number };

function pubBadge(s: string) {
  const map: Record<string, { c: string; bg: string }> = {
    Draft: { c: "var(--muted)", bg: "var(--surface-soft)" },
    Ready: { c: "var(--amber)", bg: "var(--amber-soft)" },
    Published: { c: "var(--green)", bg: "var(--green-soft)" }
  };
  const v = map[s] ?? { c: "var(--muted)", bg: "var(--surface-soft)" };
  return <span style={{ display: "inline-flex", padding: "3px 8px", borderRadius: 99, fontSize: 10, fontWeight: 700, background: v.bg, color: v.c }}>{s}</span>;
}

const inputS: React.CSSProperties = { minHeight: 42, padding: "0 11px", border: "1px solid var(--line)", borderRadius: 8, color: "var(--ink)", background: "var(--canvas)", fontSize: "0.85rem", width: "100%", boxSizing: "border-box" };

export default function ExaminationsPage() {
  const [schemes, setSchemes] = useState<AssessmentScheme[]>([]);
  const [slots, setSlots] = useState<PublicationSlot[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [studentId, setStudentId] = useState("");
  const [courseId, setCourseId] = useState("");
  const [component, setComponent] = useState("");
  const [marksObtained, setMarksObtained] = useState("");
  const [maxMarks, setMaxMarks] = useState("100");
  const [submitting, setSubmitting] = useState(false);
  const [entryMsg, setEntryMsg] = useState<string | null>(null);

  const [publishingId, setPublishingId] = useState<string | null>(null);
  const [pubResult, setPubResult] = useState<string | null>(null);

  useEffect(() => {
    Promise.all([
      apiGet<AssessmentScheme[]>("/api/examinations/schemes").catch(() => [] as AssessmentScheme[]),
      apiGet<PublicationSlot[]>("/api/examinations/publication-slots").catch(() => [] as PublicationSlot[])
    ]).then(([sc, sl]) => {
      setSchemes(sc); setSlots(sl); setLoading(false);
    }).catch(() => { setError("Failed to load examination data"); setLoading(false); });
  }, []);

  async function submitMarks(e: React.FormEvent) {
    e.preventDefault();
    setSubmitting(true); setEntryMsg(null);
    try {
      await apiPost("/api/examinations/marks", { studentId, courseId, component, marksObtained: Number(marksObtained), maxMarks: Number(maxMarks) });
      setEntryMsg("success: Marks recorded");
      setStudentId(""); setComponent(""); setMarksObtained("");
    } catch { setEntryMsg("error: Failed to record marks"); }
    finally { setSubmitting(false); }
  }

  async function publishResults(slotId: string) {
    setPublishingId(slotId); setPubResult(null);
    try {
      await apiPost(`/api/examinations/publish/${slotId}`);
      setPubResult("Results published successfully");
      setSlots(prev => prev.map(s => s.id === slotId ? { ...s, status: "Published" as const } : s));
    } catch { setPubResult("Publication failed"); }
    finally { setPublishingId(null); }
  }

  if (loading) return <main style={{ minHeight: "100vh", display: "grid", placeItems: "center", color: "var(--muted)" }}>Loading examination dashboard...</main>;
  if (error) return <main style={{ minHeight: "100vh", display: "grid", placeItems: "center", color: "var(--red)" }}>{error}</main>;

  return (
    <main style={{ minHeight: "100vh", maxWidth: 1480, margin: "0 auto", padding: "42px 34px 64px", background: "var(--canvas)" }}>
      <header style={{ display: "flex", alignItems: "end", justifyContent: "space-between", gap: 20, marginBottom: 28 }}>
        <div>
          <p style={{ margin: "0 0 7px", color: "var(--blue-600)", fontSize: 11, fontWeight: 800, letterSpacing: ".8px", textTransform: "uppercase" }}>Examination Management</p>
          <h1 style={{ margin: 0, fontSize: "clamp(27px, 3vw, 36px)", lineHeight: 1.15, letterSpacing: "-1px" }}>Examinations</h1>
          <span style={{ display: "block", marginTop: 9, color: "var(--muted)" }}>Assessment schemes, marks entry, result publication, and grade distribution analysis</span>
        </div>
        <Link href="/grade-distributions" style={{ minHeight: 42, padding: "0 16px", border: "1px solid var(--line)", borderRadius: 8, background: "var(--surface)", color: "var(--blue-600)", cursor: "pointer", fontWeight: 700, fontSize: 11, display: "inline-flex", alignItems: "center", gap: 6, textDecoration: "none" }}>
          <BarChart3 style={{ width: 14 }} /> Grade Distributions
        </Link>
      </header>

      <section style={{ display: "grid", gridTemplateColumns: "minmax(0, 1.2fr) minmax(360px, 0.7fr)", gap: 18, marginBottom: 18 }}>
        <div className="surface" style={{ overflow: "hidden" }}>
          <div className="surface-heading"><div><h2>Marks Entry</h2><p>Record student marks for course assessment components</p></div><PenLine /></div>
          <form onSubmit={submitMarks} style={{ display: "grid", gap: 12, padding: 18 }}>
            {entryMsg && (
              <div style={{ padding: "10px 14px", borderRadius: 8, border: entryMsg.startsWith("error") ? "1px solid #ecc0c6" : "1px solid #aaddc9", color: entryMsg.startsWith("error") ? "var(--red)" : "var(--green)", background: entryMsg.startsWith("error") ? "var(--red-soft)" : "var(--green-soft)", fontSize: 11, display: "flex", alignItems: "center", gap: 6 }}>
                {entryMsg.startsWith("error") ? <AlertCircle style={{ width: 15 }} /> : <CheckCircle2 style={{ width: 15 }} />}{entryMsg.replace(/^[^:]+: /, "")}
              </div>
            )}
            <div style={{ display: "grid", gridTemplateColumns: "repeat(2, minmax(0, 1fr))", gap: 12 }}>
              <label style={{ display: "grid", gap: 5, color: "var(--muted)", fontSize: 10, fontWeight: 700 }}>
                Student ID
                <input placeholder="e.g. STU-2024-001" value={studentId} onChange={e => setStudentId(e.target.value)} style={inputS} required />
              </label>
              <label style={{ display: "grid", gap: 5, color: "var(--muted)", fontSize: 10, fontWeight: 700 }}>
                Course ID
                <input placeholder="e.g. CS-301" value={courseId} onChange={e => setCourseId(e.target.value)} style={inputS} required />
              </label>
              <label style={{ display: "grid", gap: 5, color: "var(--muted)", fontSize: 10, fontWeight: 700 }}>
                Component
                <input placeholder="e.g. Midterm, Final, Assignment 1" value={component} onChange={e => setComponent(e.target.value)} style={inputS} required />
              </label>
              <div style={{ display: "grid", gridTemplateColumns: "repeat(2, minmax(0, 1fr))", gap: 8 }}>
                <label style={{ display: "grid", gap: 5, color: "var(--muted)", fontSize: 10, fontWeight: 700 }}>
                  Marks Obtained
                  <input type="number" min={0} value={marksObtained} onChange={e => setMarksObtained(e.target.value)} style={inputS} required />
                </label>
                <label style={{ display: "grid", gap: 5, color: "var(--muted)", fontSize: 10, fontWeight: 700 }}>
                  Max Marks
                  <input type="number" min={1} value={maxMarks} onChange={e => setMaxMarks(e.target.value)} style={inputS} required />
                </label>
              </div>
            </div>
            <button type="submit" disabled={submitting} style={{ padding: "0.7rem", background: "var(--blue-600)", color: "#fff", border: "none", borderRadius: 8, cursor: "pointer", fontWeight: 700, fontSize: 12, display: "flex", alignItems: "center", justifyContent: "center", gap: 6, opacity: submitting ? 0.6 : 1 }}>
              <Send style={{ width: 14 }} /> {submitting ? "Recording..." : "Record Marks"}
            </button>
          </form>
        </div>

        <div className="surface" style={{ overflow: "hidden" }}>
          <div className="surface-heading"><div><h2>Result Publication</h2><p>Publish finalized results to student portals</p></div><FileText /></div>
          <div style={{ padding: 18 }}>
            {pubResult && <div style={{ padding: "10px 14px", borderRadius: 8, border: pubResult.includes("failed") ? "1px solid #ecc0c6" : "1px solid #aaddc9", color: pubResult.includes("failed") ? "var(--red)" : "var(--green)", background: pubResult.includes("failed") ? "var(--red-soft)" : "var(--green-soft)", fontSize: 11, display: "flex", alignItems: "center", gap: 6, marginBottom: 12 }}>{pubResult.includes("failed") ? <AlertCircle style={{ width: 15 }} /> : <CheckCircle2 style={{ width: 15 }} />}{pubResult}</div>}
            {slots.length === 0 ? (
              <div style={{ minHeight: 120, display: "grid", placeItems: "center", alignContent: "center", gap: 8, padding: 20, color: "var(--muted)" }}>
                <ClipboardList style={{ width: 24, opacity: 0.5 }} />
                <span style={{ fontSize: 11 }}>No publication slots configured</span>
              </div>
            ) : (
              <div style={{ display: "grid", gap: 10 }}>
                {slots.map(s => (
                  <div key={s.id} style={{ padding: 14, borderRadius: 10, border: "1px solid var(--line)", background: "var(--surface-soft)", display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                    <div style={{ display: "grid", gap: 2 }}>
                      <strong style={{ fontSize: 12 }}>{s.courseName}</strong>
                      <div style={{ display: "flex", gap: 10, alignItems: "center", fontSize: 10, color: "var(--muted)" }}>
                        <span>{s.semester}</span>
                        <span>{s.students} students</span>
                        {pubBadge(s.status)}
                      </div>
                    </div>
                    <button onClick={() => publishResults(s.id)} disabled={publishingId === s.id || s.status === "Published"}
                      style={{ padding: "6px 14px", border: "none", borderRadius: 6, background: s.status === "Published" ? "var(--surface-soft)" : "var(--blue-600)", color: s.status === "Published" ? "var(--muted)" : "#fff", cursor: s.status === "Published" ? "default" : "pointer", fontWeight: 700, fontSize: 11, whiteSpace: "nowrap", opacity: publishingId === s.id ? 0.6 : 1 }}>
                      {publishingId === s.id ? "Publishing..." : s.status === "Published" ? "Published" : "Publish"}
                    </button>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      </section>

      <section className="surface" style={{ overflow: "hidden" }}>
        <div className="surface-heading"><div><h2>Assessment Schemes</h2><p>{schemes.length} schemes defined across courses</p></div><BookOpen /></div>
        {schemes.length === 0 ? (
          <div style={{ minHeight: 180, display: "grid", placeItems: "center", alignContent: "center", gap: 8, padding: 30, color: "var(--muted)", textAlign: "center" }}>
            <GraduationCap style={{ width: 28, opacity: 0.5 }} />
            <strong>No assessment schemes defined</strong>
            <span>Configure assessment components for your courses</span>
          </div>
        ) : (
          <div style={{ overflow: "auto" }}>
            <table style={{ width: "100%", minWidth: 700, borderCollapse: "collapse" }}>
              <thead>
                <tr style={{ background: "var(--surface-soft)" }}>
                  {["Scheme", "Course", "Components"].map(h => (
                    <th key={h} style={{ padding: "10px 14px", color: "var(--muted)", fontSize: 9, fontWeight: 800, textTransform: "uppercase", textAlign: "left", borderBottom: "1px solid var(--line)" }}>{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {schemes.map(s => (
                  <tr key={s.id} style={{ borderBottom: "1px solid var(--line)" }}>
                    <td style={{ padding: "10px 14px", fontSize: 11, fontWeight: 600 }}>{s.name}</td>
                    <td style={{ padding: "10px 14px", fontSize: 11, color: "var(--muted)" }}>{s.courseName}</td>
                    <td style={{ padding: "10px 14px" }}>
                      <div style={{ display: "flex", flexWrap: "wrap", gap: 6 }}>
                        {s.components.map((c, i) => (
                          <span key={i} style={{ display: "inline-flex", alignItems: "center", gap: 4, padding: "4px 9px", borderRadius: 99, fontSize: 10, fontWeight: 600, background: "var(--blue-100)", color: "var(--blue-600)" }}>
                            {c.label} <span style={{ opacity: 0.7 }}>({c.weight}%)</span>
                          </span>
                        ))}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </main>
  );
}
