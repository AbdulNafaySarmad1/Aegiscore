"use client";

import { useEffect, useState } from "react";
import { apiGet, apiPost } from "@/lib/api-client";
import { AlertCircle, Camera, CheckCircle2, Clock, Eye, Lock, Monitor, PlayCircle, ShieldAlert, ShieldCheck, Upload, UserCheck } from "lucide-react";
import { usePersonalityTranslations } from "@/lib/personality-translator";

type IntegrityStatus = "Clean" | "Review" | "Referred";
type StudentIntegrity = { studentId: string; studentName: string; status: IntegrityStatus; warnings: number };
type ProctorEvent = { id: string; timestamp: string; severity: "Low" | "Medium" | "High" | "Critical"; type: string; description: string };
type SessionData = { sessionId: string; courseId: string; courseName: string; startTime: string; durationMinutes: number; activeStudents: number };

function integrityBadge(s: IntegrityStatus) {
  const m: Record<IntegrityStatus, { c: string; bg: string; icon: typeof CheckCircle2 }> = {
    Clean: { c: "var(--green)", bg: "var(--green-soft)", icon: CheckCircle2 },
    Review: { c: "var(--amber)", bg: "var(--amber-soft)", icon: Eye },
    Referred: { c: "var(--red)", bg: "var(--red-soft)", icon: ShieldAlert }
  };
  const v = m[s];
  const Icon = v.icon;
  return <span style={{ display: "inline-flex", alignItems: "center", gap: 5, padding: "4px 9px", borderRadius: 99, background: v.bg, color: v.c, fontSize: 10, fontWeight: 700 }}><Icon style={{ width: 12 }} />{s}</span>;
}

function severityPill(s: string) {
  const map: Record<string, string> = { Low: "var(--muted)", Medium: "var(--amber)", High: "var(--red)", Critical: "var(--red)" };
  return <span style={{ display: "inline-flex", padding: "2px 7px", borderRadius: 99, fontSize: 10, fontWeight: 700, background: s === "Critical" || s === "High" ? "var(--red-soft)" : s === "Medium" ? "var(--amber-soft)" : "var(--surface-soft)", color: map[s] ?? "var(--muted)" }}>{s}</span>;
}

const inputS: React.CSSProperties = { minHeight: 42, padding: "0 11px", border: "1px solid var(--line)", borderRadius: 8, color: "var(--ink)", background: "var(--canvas)", fontSize: "0.85rem", width: "100%", boxSizing: "border-box" };

export default function ProctoringPage() {
  const t = usePersonalityTranslations("academic");
  const [students, setStudents] = useState<StudentIntegrity[]>([]);
  const [events, setEvents] = useState<ProctorEvent[]>([]);
  const [sessions, setSessions] = useState<SessionData[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [courseId, setCourseId] = useState("");
  const [duration, setDuration] = useState(120);
  const [creating, setCreating] = useState(false);
  const [sessionMsg, setSessionMsg] = useState<string | null>(null);

  const [file, setFile] = useState<File | null>(null);
  const [uploading, setUploading] = useState(false);

  useEffect(() => {
    const ctrl = new AbortController();
    Promise.all([
      apiGet<StudentIntegrity[]>("/api/proctoring/integrity", ctrl.signal).catch(() => [] as StudentIntegrity[]),
      apiGet<ProctorEvent[]>("/api/proctoring/events", ctrl.signal).catch(() => [] as ProctorEvent[]),
      apiGet<SessionData[]>("/api/proctoring/sessions", ctrl.signal).catch(() => [] as SessionData[])
    ]).then(([s, ev, se]) => {
      if (!ctrl.signal.aborted) { setStudents(s); setEvents(ev); setSessions(se); setLoading(false); }
    }).catch(() => { if (!ctrl.signal.aborted) { setError("Failed to load proctoring data"); setLoading(false); } });
    return () => ctrl.abort();
  }, []);

  async function createSession(e: React.FormEvent) {
    e.preventDefault();
    setCreating(true); setSessionMsg(null);
    try {
      const data = await apiPost<SessionData>("/api/proctoring/sessions", { courseId, durationMinutes: duration });
      setSessions(prev => [data, ...prev]);
      setSessionMsg("success: Session created");
    } catch { setSessionMsg("error: Failed to create session"); }
    finally { setCreating(false); }
  }

  async function uploadFace() {
    if (!file) return;
    setUploading(true);
    try {
      const form = new FormData();
      form.append("face", file);
      await apiPost("/api/proctoring/face-analysis", form);
      setFile(null);
    } catch { /* ignore */ }
    finally { setUploading(false); }
  }

  async function validateLockdown() {
    try { await apiPost("/api/proctoring/lockdown/validate"); } catch { /* ignore */ }
  }

  const clean = students.filter(s => s.status === "Clean").length;
  const review = students.filter(s => s.status === "Review").length;
  const referred = students.filter(s => s.status === "Referred").length;

  if (loading) return <main style={{ minHeight: "100vh", display: "grid", placeItems: "center", color: "var(--muted)" }}>{t("loadingProctoring")}</main>;
  if (error) return <main style={{ minHeight: "100vh", display: "grid", placeItems: "center", color: "var(--red)" }}>{error}</main>;

  return (
    <main style={{ minHeight: "100vh", maxWidth: 1480, margin: "0 auto", padding: "42px 34px 64px", background: "var(--canvas)" }}>
      <header style={{ display: "flex", alignItems: "end", justifyContent: "space-between", gap: 20, marginBottom: 28 }}>
        <div>
          <p style={{ margin: "0 0 7px", color: "var(--blue-600)", fontSize: 11, fontWeight: 800, letterSpacing: ".8px", textTransform: "uppercase" }}>Examination Security</p>
          <h1 style={{ margin: 0, fontSize: "clamp(27px, 3vw, 36px)", lineHeight: 1.15, letterSpacing: "-1px" }}>{t("proctoring")}</h1>
          <span style={{ display: "block", marginTop: 9, color: "var(--muted)" }}>Remote exam integrity monitoring, face analysis, browser lockdown, and forensic event tracking</span>
        </div>
      </header>

      <div style={{ display: "grid", gridTemplateColumns: "repeat(4, minmax(0, 1fr))", gap: 14, marginBottom: 18 }}>
        <div style={{ padding: 20, borderRadius: 14, border: "1px solid var(--line)", background: "var(--surface)", boxShadow: "var(--shadow)" }}>
          <span style={{ color: "var(--muted)", fontSize: 10, fontWeight: 700, textTransform: "uppercase" }}>Sessions Active</span>
          <div style={{ fontSize: 28, fontWeight: 800, letterSpacing: "-.5px", margin: "8px 0 3px" }}>{sessions.length}</div>
          <small style={{ color: "var(--muted)" }}>{sessions.reduce((sum, s) => sum + s.activeStudents, 0)} proctored students</small>
        </div>
        <div style={{ padding: 20, borderRadius: 14, border: "1px solid var(--line)", background: "var(--surface)", boxShadow: "var(--shadow)" }}>
          <span style={{ color: "var(--muted)", fontSize: 10, fontWeight: 700, textTransform: "uppercase" }}>Clean Status</span>
          <div style={{ fontSize: 28, fontWeight: 800, letterSpacing: "-.5px", margin: "8px 0 3px", color: "var(--green)" }}>{clean}</div>
          <small style={{ color: "var(--muted)" }}>No integrity flags</small>
        </div>
        <div style={{ padding: 20, borderRadius: 14, border: "1px solid var(--line)", background: "var(--surface)", boxShadow: "var(--shadow)" }}>
          <span style={{ color: "var(--muted)", fontSize: 10, fontWeight: 700, textTransform: "uppercase" }}>Under Review</span>
          <div style={{ fontSize: 28, fontWeight: 800, letterSpacing: "-.5px", margin: "8px 0 3px", color: "var(--amber)" }}>{review}</div>
          <small style={{ color: "var(--muted)" }}>Needs proctor attention</small>
        </div>
        <div style={{ padding: 20, borderRadius: 14, border: "1px solid var(--line)", background: "var(--surface)", boxShadow: "var(--shadow)" }}>
          <span style={{ color: "var(--muted)", fontSize: 10, fontWeight: 700, textTransform: "uppercase" }}>Referred</span>
          <div style={{ fontSize: 28, fontWeight: 800, letterSpacing: "-.5px", margin: "8px 0 3px", color: "var(--red)" }}>{referred}</div>
          <small style={{ color: "var(--muted)" }}>Escalated to committee</small>
        </div>
      </div>

      <section style={{ display: "grid", gridTemplateColumns: "minmax(0, 1.2fr) minmax(360px, 0.7fr)", gap: 18, marginBottom: 18 }}>
        <div className="surface" style={{ overflow: "hidden" }}>
          <div className="surface-heading"><div><h2>{t("createSession")}</h2><p>Launch a new remote exam monitoring session</p></div><Monitor /></div>
          <form onSubmit={createSession} style={{ display: "grid", gap: 12, padding: 18 }}>
            {sessionMsg && (
              <div style={{ padding: "10px 14px", borderRadius: 8, border: sessionMsg.startsWith("error") ? "1px solid #ecc0c6" : "1px solid #aaddc9", color: sessionMsg.startsWith("error") ? "var(--red)" : "var(--green)", background: sessionMsg.startsWith("error") ? "var(--red-soft)" : "var(--green-soft)", fontSize: 11, display: "flex", alignItems: "center", gap: 6 }}>
                {sessionMsg.startsWith("error") ? <AlertCircle style={{ width: 15 }} /> : <CheckCircle2 style={{ width: 15 }} />}{sessionMsg.replace(/^[^:]+: /, "")}
              </div>
            )}
            <div style={{ display: "grid", gridTemplateColumns: "repeat(2, minmax(0, 1fr))", gap: 12 }}>
              <label style={{ display: "grid", gap: 5, color: "var(--muted)", fontSize: 10, fontWeight: 700 }}>
                Course ID
                <input placeholder="e.g. CS-301" value={courseId} onChange={e => setCourseId(e.target.value)} style={inputS} required />
              </label>
              <label style={{ display: "grid", gap: 5, color: "var(--muted)", fontSize: 10, fontWeight: 700 }}>
                Duration (minutes)
                <input type="number" min={30} max={300} value={duration} onChange={e => setDuration(Number(e.target.value))} style={inputS} required />
              </label>
            </div>
            <button type="submit" disabled={creating} style={{ padding: "0.7rem", background: "var(--blue-600)", color: "#fff", border: "none", borderRadius: 8, cursor: "pointer", fontWeight: 700, fontSize: 12, display: "flex", alignItems: "center", justifyContent: "center", gap: 6, opacity: creating ? 0.6 : 1 }}>
              <PlayCircle style={{ width: 15 }} /> {creating ? "Creating..." : t("startSession")}
            </button>
          </form>
        </div>

        <div style={{ display: "grid", gap: 18 }}>
          <div className="surface" style={{ overflow: "hidden" }}>
            <div className="surface-heading"><div><h2>{t("faceAnalysis")}</h2><p>Upload a frame for biometric verification</p></div><Camera /></div>
            <div style={{ padding: 18 }}>
              <label style={{ minHeight: 39, display: "grid", placeItems: "center", border: "1px dashed var(--line-strong)", borderRadius: 8, color: "var(--blue-600)", cursor: "pointer", fontSize: 10, fontWeight: 700, padding: 10, marginBottom: 10 }}>
                <Upload style={{ marginRight: 6 }} /> {file ? file.name : "Upload face capture (JPEG/PNG)"}
                <input type="file" accept="image/jpeg,image/png" style={{ display: "none" }} onChange={e => setFile(e.target.files?.[0] ?? null)} />
              </label>
              <button onClick={uploadFace} disabled={!file || uploading} style={{ width: "100%", padding: "0.6rem", border: "1px solid var(--line)", borderRadius: 8, background: "var(--surface)", color: "var(--blue-600)", cursor: "pointer", fontWeight: 700, fontSize: 11, opacity: !file ? 0.5 : 1 }}>
                {uploading ? "Analyzing..." : t("analyzeFace")}
              </button>
            </div>
          </div>
          <div className="surface" style={{ overflow: "hidden" }}>
            <div className="surface-heading"><div><h2>{t("lockdownValidation")}</h2><p>Verify browser lockdown compliance</p></div><Lock /></div>
            <div style={{ padding: 18 }}>
              <button onClick={validateLockdown} style={{ width: "100%", padding: "0.6rem", border: "1px solid var(--line)", borderRadius: 8, background: "var(--surface)", color: "var(--blue-600)", cursor: "pointer", fontWeight: 700, fontSize: 11, display: "flex", alignItems: "center", justifyContent: "center", gap: 8 }}>
                <ShieldCheck style={{ width: 15 }} /> {t("validateLockdownStatus")}
              </button>
            </div>
          </div>
        </div>
      </section>

      <section className="surface" style={{ overflow: "hidden", marginBottom: 18 }}>
        <div className="surface-heading"><div><h2>{t("studentIntegrityGrid")}</h2><p>{students.length} students under monitoring</p></div><Eye /></div>
        {students.length === 0 ? (
          <div style={{ minHeight: 160, display: "grid", placeItems: "center", alignContent: "center", gap: 8, padding: 30, color: "var(--muted)", textAlign: "center" }}>
            <UserCheck style={{ width: 28, color: "var(--green)" }} />
            <span>{t("noStudentsInSessions")}</span>
          </div>
        ) : (
          <div style={{ overflow: "auto" }}>
            <table style={{ width: "100%", minWidth: 600, borderCollapse: "collapse" }}>
              <thead>
                <tr style={{ background: "var(--surface-soft)" }}>
                  {["Student", "ID", "Status", "Warnings"].map(h => (
                    <th key={h} style={{ padding: "10px 14px", color: "var(--muted)", fontSize: 9, fontWeight: 800, textTransform: "uppercase", textAlign: "left", borderBottom: "1px solid var(--line)" }}>{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {students.map(s => (
                  <tr key={s.studentId} style={{ borderBottom: "1px solid var(--line)" }}>
                    <td style={{ padding: "10px 14px", fontSize: 11, fontWeight: 600 }}>{s.studentName}</td>
                    <td style={{ padding: "10px 14px", fontSize: 10, fontFamily: "monospace", color: "var(--muted)" }}>{s.studentId}</td>
                    <td style={{ padding: "10px 14px" }}>{integrityBadge(s.status)}</td>
                    <td style={{ padding: "10px 14px" }}>
                      <span style={{ padding: "3px 7px", borderRadius: 99, fontSize: 10, fontWeight: 700, background: s.warnings > 2 ? "var(--red-soft)" : s.warnings > 0 ? "var(--amber-soft)" : "var(--green-soft)", color: s.warnings > 2 ? "var(--red)" : s.warnings > 0 ? "var(--amber)" : "var(--green)" }}>{s.warnings}</span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <section className="surface" style={{ overflow: "hidden" }}>
        <div className="surface-heading"><div><h2>{t("proctorEventLog")}</h2><p>{events.length} events recorded</p></div><Clock /></div>
        {events.length === 0 ? (
          <div style={{ minHeight: 160, display: "grid", placeItems: "center", alignContent: "center", gap: 8, padding: 30, color: "var(--muted)", textAlign: "center" }}>
            <CheckCircle2 style={{ width: 28, color: "var(--green)" }} />
            <strong>{t("noEventsRecorded")}</strong>
            <span>{t("noEventsDesc")}</span>
          </div>
        ) : (
          <div style={{ overflow: "auto" }}>
            <table style={{ width: "100%", minWidth: 750, borderCollapse: "collapse" }}>
              <thead>
                <tr style={{ background: "var(--surface-soft)" }}>
                  {["Timestamp", "Severity", "Type", "Description"].map(h => (
                    <th key={h} style={{ padding: "10px 14px", color: "var(--muted)", fontSize: 9, fontWeight: 800, textTransform: "uppercase", textAlign: "left", borderBottom: "1px solid var(--line)" }}>{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {events.map(e => (
                  <tr key={e.id} style={{ borderBottom: "1px solid var(--line)" }}>
                    <td style={{ padding: "10px 14px", fontSize: 10, color: "var(--muted)", whiteSpace: "nowrap" }}>{new Date(e.timestamp).toLocaleString()}</td>
                    <td style={{ padding: "10px 14px" }}>{severityPill(e.severity)}</td>
                    <td style={{ padding: "10px 14px", fontSize: 11, fontWeight: 600 }}>{e.type}</td>
                    <td style={{ padding: "10px 14px", fontSize: 11 }}>{e.description}</td>
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
