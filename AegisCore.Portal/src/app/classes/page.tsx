"use client";

import { PageErrorDisplay } from "@/components/ErrorDisplay";
import { useEffect, useState } from "react";
import { apiGet, apiPost } from "@/lib/api-client";
import { usePersonalityTranslations } from "@/lib/personality-translator";
import {
  AlertCircle, BookOpen, CheckCircle2, Clock, LayoutGrid, RefreshCw,
  Send, Server, Settings, Users, XCircle
} from "lucide-react";

type GenerationJob = {
  id: string; academicYear: string; semester: string;
  totalOfferings: number; sectionsGenerated: number;
  studentsDistributed: number; facultyAssigned: number;
  status: "Pending" | "Running" | "Completed" | "Failed";
};

type Offering = {
  id: string; name: string; courseCode: string; courseName: string;
  targetSections: number; generatedSections: number; totalCapacity: number;
};

type ClassSection = {
  id: string; offeringId: string; offeringName: string; sectionName: string;
  capacity: number; enrolled: number; lmsProvider: string;
  lmsStatus: "Pending" | "Provisioned" | "Failed" | "Synced";
};

type SyncLog = { timestamp: string; membersAdded: number; membersRemoved: number; conflicts: number; status: "Success" | "Partial" | "Failed" };

const lmsStatusBadge = (s: ClassSection["lmsStatus"]) => {
  const map: Record<ClassSection["lmsStatus"], { c: string; bg: string }> = {
    Pending: { c: "var(--muted)", bg: "var(--surface-soft)" },
    Provisioned: { c: "var(--green)", bg: "var(--green-soft)" },
    Failed: { c: "var(--red)", bg: "var(--red-soft)" },
    Synced: { c: "var(--blue-600)", bg: "var(--blue-100)" }
  };
  const v = map[s];
  return <span style={{ display: "inline-flex", padding: "3px 8px", borderRadius: 99, fontSize: 10, fontWeight: 700, background: v.bg, color: v.c }}>{s}</span>;
};

const inputS: React.CSSProperties = { minHeight: 42, padding: "0 11px", border: "1px solid var(--line)", borderRadius: 8, color: "var(--ink)", background: "var(--canvas)", fontSize: "0.85rem", width: "100%", boxSizing: "border-box" };
const labelS: React.CSSProperties = { display: "grid", gap: 5, color: "var(--muted)", fontSize: 10, fontWeight: 700 };

export default function ClassesPage() {
  const t = usePersonalityTranslations("academic");

  const [offerings, setOfferings] = useState<Offering[]>([]);
  const [sections, setSections] = useState<ClassSection[]>([]);
  const [syncLogs, setSyncLogs] = useState<SyncLog[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const [academicYear, setAcademicYear] = useState("2026-2027");
  const [semester, setSemester] = useState("Fall");
  const [generating, setGenerating] = useState(false);

  const [lmsProvider, setLmsProvider] = useState("GoogleClassroom");
  const [retryAttempts, setRetryAttempts] = useState("3");
  const [retryDelay, setRetryDelay] = useState("30");
  const [syncSchedule, setSyncSchedule] = useState("daily");
  const [conflictResolution, setConflictResolution] = useState("manual");
  const [provisioningId, setProvisioningId] = useState<string | null>(null);
  const [syncingId, setSyncingId] = useState<string | null>(null);

  useEffect(() => {
    const ctrl = new AbortController();
    const fetch = async () => {
      try {
        const [o, s, l] = await Promise.all([
          apiGet<Offering[]>("/api/classes/offerings", ctrl.signal).catch(() => [] as Offering[]),
          apiGet<ClassSection[]>("/api/classes/sections", ctrl.signal).catch(() => [] as ClassSection[]),
          apiGet<SyncLog[]>("/api/classes/sync-logs", ctrl.signal).catch(() => [] as SyncLog[])
        ]);
        setOfferings(o); setSections(s); setSyncLogs(l);
      } catch { setError("Class records are temporarily unavailable."); }
      finally { setLoading(false); }
    };
    fetch();
    return () => { ctrl.abort(); };
  }, []);

  async function generateClasses(e: React.FormEvent) {
    e.preventDefault();
    setGenerating(true); setMessage(null);
    try {
      await apiPost("/api/classes/generate", { academicYear, semester });
      setMessage("success: Class generation initiated");
      const ctrl = new AbortController();
      const [o, s] = await Promise.all([
        apiGet<Offering[]>("/api/classes/offerings", ctrl.signal),
        apiGet<ClassSection[]>("/api/classes/sections", ctrl.signal)
      ]);
      setOfferings(o); setSections(s);
    } catch { setMessage("error: Generation failed"); }
    finally { setGenerating(false); }
  }

  async function provisionLms(sectionId: string) {
    setProvisioningId(sectionId);
    try {
      await apiPost(`/api/classes/lms/provision`, { sectionId, provider: lmsProvider });
      setMessage("success: LMS provisioned");
      setSections(prev => prev.map(s => s.id === sectionId ? { ...s, lmsStatus: "Provisioned" as const, lmsProvider } : s));
    } catch { setMessage("error: Provisioning failed"); }
    finally { setProvisioningId(null); }
  }

  async function syncRoster(sectionId: string) {
    setSyncingId(sectionId);
    try {
      await apiPost(`/api/classes/lms/sync-roster`, { sectionId });
      setMessage("success: Roster synced");
      const ctrl = new AbortController();
      const [s, l] = await Promise.all([
        apiGet<ClassSection[]>("/api/classes/sections", ctrl.signal),
        apiGet<SyncLog[]>("/api/classes/sync-logs", ctrl.signal)
      ]);
      setSections(s); setSyncLogs(l);
    } catch { setMessage("error: Sync failed"); }
    finally { setSyncingId(null); }
  }

  if (loading) return (
    <main style={{ minHeight: "100vh", display: "grid", placeItems: "center", gap: 16, color: "var(--muted)" }}>
      <div style={{ width: 40, height: 4, borderRadius: 99, overflow: "hidden", background: "var(--line)" }}>
        <div style={{ width: "50%", height: "100%", borderRadius: "inherit", background: "var(--blue-500)", animation: "launch 1s ease-in-out infinite alternate" }} />
      </div>
      Loading class management...
    </main>
  );

  if (error) return <PageErrorDisplay message={error} />;

  return (
    <main style={{ minHeight: "100vh", maxWidth: 1480, margin: "0 auto", padding: "42px 34px 64px", background: "var(--canvas)" }}>
      <header style={{ display: "flex", alignItems: "end", justifyContent: "space-between", gap: 20, marginBottom: 28 }}>
        <div>
          <p style={{ margin: "0 0 7px", color: "var(--blue-600)", fontSize: 11, fontWeight: 800, letterSpacing: ".8px", textTransform: "uppercase" }}>Academic Operations</p>
          <h1 style={{ margin: 0, fontSize: "clamp(27px, 3vw, 36px)", lineHeight: 1.15, letterSpacing: "-1px" }}>{t("classes") ?? "Class Generation & LMS"}</h1>
          <span style={{ display: "block", marginTop: 9, color: "var(--muted)" }}>Auto-generate classes, distribute students into sections, and provision LMS spaces</span>
        </div>
      </header>

      {message && (
        <div style={{ display: "flex", alignItems: "center", gap: 9, padding: "11px 14px", marginBottom: 18, border: message.startsWith("error") ? "1px solid #ecc0c6" : "1px solid #aaddc9", borderRadius: 10, color: message.startsWith("error") ? "var(--red)" : "var(--green)", background: message.startsWith("error") ? "var(--red-soft)" : "var(--green-soft)", fontSize: 11 }}>
          {message.startsWith("error") ? <AlertCircle /> : <CheckCircle2 />}{message.replace(/^[^:]+: /, "")}
        </div>
      )}

      <section style={{ display: "grid", gridTemplateColumns: "minmax(0, 1.3fr) minmax(360px, 0.8fr)", gap: 18, marginBottom: 18 }}>
        <div className="surface" style={{ overflow: "hidden" }}>
          <div className="surface-heading"><div><h2>Class Generation</h2><p>Generate classes for academic year and semester</p></div><LayoutGrid /></div>
          <form onSubmit={generateClasses} style={{ display: "grid", gap: 12, padding: 18 }}>
            <div style={{ display: "grid", gridTemplateColumns: "repeat(2, minmax(0, 1fr))", gap: 12 }}>
              <label style={labelS}>
                Academic Year
                <select value={academicYear} onChange={e => setAcademicYear(e.target.value)} style={inputS}>
                  <option value="2025-2026">2025-2026</option>
                  <option value="2026-2027">2026-2027</option>
                  <option value="2027-2028">2027-2028</option>
                </select>
              </label>
              <label style={labelS}>
                Semester
                <select value={semester} onChange={e => setSemester(e.target.value)} style={inputS}>
                  <option value="Fall">Fall</option>
                  <option value="Spring">Spring</option>
                  <option value="Summer">Summer</option>
                </select>
              </label>
            </div>
            <button type="submit" disabled={generating} style={{ padding: "0.7rem", background: "var(--blue-600)", color: "#fff", border: "none", borderRadius: 8, cursor: "pointer", fontWeight: 700, fontSize: 12, display: "flex", alignItems: "center", justifyContent: "center", gap: 7, opacity: generating ? 0.6 : 1 }}>
              <Send style={{ width: 14 }} /> {generating ? "Generating..." : "Generate Classes"}
            </button>
          </form>
          {offerings.length > 0 && (
            <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(140px, 1fr))", gap: 10, padding: "0 18px 18px" }}>
              <div style={{ padding: 12, borderRadius: 8, background: "var(--surface-soft)", textAlign: "center" }}>
                <strong style={{ display: "block", fontSize: 22, color: "var(--blue-600)" }}>{offerings.length}</strong>
                <small style={{ color: "var(--muted)", fontSize: 10 }}>Total Offerings</small>
              </div>
              <div style={{ padding: 12, borderRadius: 8, background: "var(--green-soft)", textAlign: "center" }}>
                <strong style={{ display: "block", fontSize: 22, color: "var(--green)" }}>{sections.length}</strong>
                <small style={{ color: "var(--green)", fontSize: 10 }}>Sections Generated</small>
              </div>
              <div style={{ padding: 12, borderRadius: 8, background: "var(--blue-100)", textAlign: "center" }}>
                <strong style={{ display: "block", fontSize: 22, color: "var(--blue-600)" }}>{sections.reduce((sum, s) => sum + s.enrolled, 0)}</strong>
                <small style={{ color: "var(--muted)", fontSize: 10 }}>Students Distributed</small>
              </div>
              <div style={{ padding: 12, borderRadius: 8, background: "var(--amber-soft)", textAlign: "center" }}>
                <strong style={{ display: "block", fontSize: 22, color: "var(--amber)" }}>—</strong>
                <small style={{ color: "var(--amber)", fontSize: 10 }}>Faculty Assigned</small>
              </div>
            </div>
          )}
        </div>

        <div className="surface" style={{ overflow: "hidden" }}>
          <div className="surface-heading"><div><h2>LMS Configuration</h2><p>Configure LMS provisioning settings</p></div><Settings /></div>
          <div style={{ padding: 18, display: "grid", gap: 12 }}>
            <label style={labelS}>
              Provider
              <select value={lmsProvider} onChange={e => setLmsProvider(e.target.value)} style={inputS}>
                <option value="GoogleClassroom">Google Classroom</option>
                <option value="Teams">Microsoft Teams</option>
                <option value="Moodle">Moodle</option>
              </select>
            </label>
            <div style={{ display: "grid", gridTemplateColumns: "repeat(2, minmax(0, 1fr))", gap: 10 }}>
              <label style={labelS}>
                Retry Attempts
                <input type="number" min={1} value={retryAttempts} onChange={e => setRetryAttempts(e.target.value)} style={inputS} />
              </label>
              <label style={labelS}>
                Retry Delay (s)
                <input type="number" min={5} value={retryDelay} onChange={e => setRetryDelay(e.target.value)} style={inputS} />
              </label>
            </div>
            <div style={{ display: "grid", gridTemplateColumns: "repeat(2, minmax(0, 1fr))", gap: 10 }}>
              <label style={labelS}>
                Sync Schedule
                <select value={syncSchedule} onChange={e => setSyncSchedule(e.target.value)} style={inputS}>
                  <option value="manual">Manual</option>
                  <option value="hourly">Hourly</option>
                  <option value="daily">Daily</option>
                  <option value="weekly">Weekly</option>
                </select>
              </label>
              <label style={labelS}>
                Conflict Resolution
                <select value={conflictResolution} onChange={e => setConflictResolution(e.target.value)} style={inputS}>
                  <option value="manual">Manual</option>
                  <option value="last-write-wins">Last Write Wins</option>
                  <option value="first-write-wins">First Write Wins</option>
                </select>
              </label>
            </div>
          </div>
        </div>
      </section>

      <section className="surface" style={{ overflow: "hidden", marginBottom: 18 }}>
        <div className="surface-heading"><div><h2>Section Distribution</h2><p>{offerings.length} offerings with {sections.length} sections</p></div><BookOpen /></div>
        {offerings.length === 0 ? (
          <div style={{ minHeight: 180, display: "grid", placeItems: "center", alignContent: "center", gap: 8, padding: 30, color: "var(--muted)", textAlign: "center" }}>
            <BookOpen style={{ width: 28, opacity: 0.5 }} />
            <strong>No class offerings</strong>
            <span>Generate classes for the semester to see section distribution</span>
          </div>
        ) : (
          <div style={{ overflow: "auto" }}>
            <table style={{ width: "100%", minWidth: 800, borderCollapse: "collapse" }}>
              <thead>
                <tr style={{ background: "var(--surface-soft)" }}>
                  {["Offering", "Target Sections", "Generated", "Capacity", "LMS Provider", "LMS Status", "Actions"].map(h => (
                    <th key={h} style={{ padding: "10px 14px", color: "var(--muted)", fontSize: 9, fontWeight: 800, textTransform: "uppercase", textAlign: "left", borderBottom: "1px solid var(--line)" }}>{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {sections.map(s => (
                  <tr key={s.id} style={{ borderBottom: "1px solid var(--line)" }}>
                    <td style={{ padding: "10px 14px", fontSize: 11, fontWeight: 600 }}>{s.offeringName}</td>
                    <td style={{ padding: "10px 14px", fontSize: 11, color: "var(--muted)" }}>{s.capacity}</td>
                    <td style={{ padding: "10px 14px", fontSize: 11 }}>{s.sectionName}</td>
                    <td style={{ padding: "10px 14px" }}>
                      <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
                        <div style={{ flex: 1, height: 5, borderRadius: 99, overflow: "hidden", background: "var(--line)", minWidth: 60 }}>
                          <div style={{ width: `${Math.min((s.enrolled / s.capacity) * 100, 100)}%`, height: "100%", borderRadius: 99, background: s.enrolled / s.capacity > 0.9 ? "var(--red)" : s.enrolled / s.capacity > 0.7 ? "var(--amber)" : "var(--green)" }} />
                        </div>
                        <small style={{ fontSize: 9, color: "var(--muted)", whiteSpace: "nowrap" }}>{s.enrolled}/{s.capacity}</small>
                      </div>
                    </td>
                    <td style={{ padding: "10px 14px", fontSize: 10, color: "var(--muted)" }}>{s.lmsProvider || "—"}</td>
                    <td style={{ padding: "10px 14px" }}>{lmsStatusBadge(s.lmsStatus)}</td>
                    <td style={{ padding: "10px 14px" }}>
                      <div style={{ display: "flex", gap: 6 }}>
                        <button onClick={() => provisionLms(s.id)} disabled={provisioningId === s.id || s.lmsStatus !== "Pending"}
                          style={{ padding: "4px 10px", border: "1px solid var(--line)", borderRadius: 6, color: "var(--blue-600)", background: "var(--surface)", cursor: s.lmsStatus !== "Pending" ? "default" : "pointer", fontSize: 10, fontWeight: 700, opacity: s.lmsStatus !== "Pending" ? 0.4 : 1 }}>
                          {provisioningId === s.id ? "..." : "Provision"}
                        </button>
                        <button onClick={() => syncRoster(s.id)} disabled={syncingId === s.id || s.lmsStatus === "Pending" || s.lmsStatus === "Failed"}
                          style={{ padding: "4px 10px", border: "1px solid var(--line)", borderRadius: 6, color: "var(--green)", background: "var(--surface)", cursor: s.lmsStatus === "Pending" || s.lmsStatus === "Failed" ? "default" : "pointer", fontSize: 10, fontWeight: 700, opacity: s.lmsStatus === "Pending" || s.lmsStatus === "Failed" ? 0.4 : 1 }}>
                          {syncingId === s.id ? "..." : "Sync Roster"}
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <section className="surface" style={{ overflow: "hidden" }}>
        <div className="surface-heading"><div><h2>Sync Log</h2><p>{syncLogs.length} recent roster sync operations</p></div><RefreshCw /></div>
        {syncLogs.length === 0 ? (
          <div style={{ minHeight: 140, display: "grid", placeItems: "center", alignContent: "center", gap: 8, padding: 30, color: "var(--muted)" }}>
            <Clock style={{ width: 24, opacity: 0.5 }} />
            <span style={{ fontSize: 11 }}>No sync operations recorded</span>
          </div>
        ) : (
          <div style={{ overflow: "auto" }}>
            <table style={{ width: "100%", minWidth: 600, borderCollapse: "collapse" }}>
              <thead>
                <tr style={{ background: "var(--surface-soft)" }}>
                  {["Timestamp", "Members Added", "Removed", "Conflicts", "Status"].map(h => (
                    <th key={h} style={{ padding: "10px 14px", color: "var(--muted)", fontSize: 9, fontWeight: 800, textTransform: "uppercase", textAlign: "left", borderBottom: "1px solid var(--line)" }}>{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {syncLogs.map((l, i) => (
                  <tr key={i} style={{ borderBottom: "1px solid var(--line)" }}>
                    <td style={{ padding: "10px 14px", fontSize: 10, color: "var(--muted)", whiteSpace: "nowrap" }}>{new Date(l.timestamp).toLocaleString()}</td>
                    <td style={{ padding: "10px 14px", fontSize: 11, color: "var(--green)", fontWeight: 700 }}>+{l.membersAdded}</td>
                    <td style={{ padding: "10px 14px", fontSize: 11, color: "var(--red)" }}>-{l.membersRemoved}</td>
                    <td style={{ padding: "10px 14px", fontSize: 11, color: l.conflicts > 0 ? "var(--amber)" : "var(--muted)" }}>{l.conflicts}</td>
                    <td style={{ padding: "10px 14px" }}>
                      <span style={{ display: "inline-flex", padding: "3px 8px", borderRadius: 99, fontSize: 10, fontWeight: 700, background: l.status === "Success" ? "var(--green-soft)" : l.status === "Partial" ? "var(--amber-soft)" : "var(--red-soft)", color: l.status === "Success" ? "var(--green)" : l.status === "Partial" ? "var(--amber)" : "var(--red)" }}>{l.status}</span>
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
