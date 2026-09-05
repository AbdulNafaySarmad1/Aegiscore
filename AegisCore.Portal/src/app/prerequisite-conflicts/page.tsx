"use client";

import { PageErrorDisplay } from "@/components/ErrorDisplay";
import { useEffect, useState } from "react";
import { apiGet, apiPost } from "@/lib/api-client";
import { usePersonalityTranslations } from "@/lib/personality-translator";
import {
  AlertCircle, AlertTriangle, ArrowRight, CheckCircle2, Clock, GitBranch,
  GitMerge, Network, RefreshCw, Search, Send, Shield, XCircle
} from "lucide-react";

type ConflictType = "Circular" | "Sequence" | "CreditLoad" | "Orphaned" | "Bottleneck" | "NotMet";

type PrerequisiteConflict = {
  id: string; courseId: string; courseName: string; type: ConflictType;
  description: string; severity: "Low" | "Medium" | "High" | "Critical";
  affectedStudents: number; resolutionSuggestion: string; resolved: boolean;
};

type SimulationResult = {
  affectedCourses: string[]; affectedStudents: number;
  invalidRegistrations: number; cascadingConflicts: string[];
};

const conflictTypeBadge = (c: ConflictType) => {
  const map: Record<ConflictType, { c: string; bg: string }> = {
    Circular: { c: "var(--red)", bg: "var(--red-soft)" },
    Sequence: { c: "var(--amber)", bg: "var(--amber-soft)" },
    CreditLoad: { c: "var(--amber)", bg: "var(--amber-soft)" },
    Orphaned: { c: "var(--red)", bg: "var(--red-soft)" },
    Bottleneck: { c: "var(--red)", bg: "var(--red-soft)" },
    NotMet: { c: "var(--red)", bg: "var(--red-soft)" }
  };
  const v = map[c];
  return <span style={{ display: "inline-flex", padding: "3px 8px", borderRadius: 99, fontSize: 10, fontWeight: 700, background: v.bg, color: v.c }}>{c}</span>;
};

const severityIcon = (s: PrerequisiteConflict["severity"]) => {
  const map: Record<PrerequisiteConflict["severity"], { c: string; Icon: typeof AlertCircle }> = {
    Low: { c: "var(--green)", Icon: CheckCircle2 },
    Medium: { c: "var(--amber)", Icon: AlertTriangle },
    High: { c: "var(--amber)", Icon: AlertTriangle },
    Critical: { c: "var(--red)", Icon: XCircle }
  };
  const v = map[s];
  return <v.Icon style={{ width: 16, color: v.c }} />;
};

const inputS: React.CSSProperties = { minHeight: 42, padding: "0 11px", border: "1px solid var(--line)", borderRadius: 8, color: "var(--ink)", background: "var(--canvas)", fontSize: "0.85rem", width: "100%", boxSizing: "border-box" };
const labelS: React.CSSProperties = { display: "grid", gap: 5, color: "var(--muted)", fontSize: 10, fontWeight: 700 };

export default function PrerequisiteConflictsPage() {
  const t = usePersonalityTranslations("academic");

  const [conflicts, setConflicts] = useState<PrerequisiteConflict[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const [detectCourseId, setDetectCourseId] = useState("");
  const [detecting, setDetecting] = useState(false);

  const [scanningCircular, setScanningCircular] = useState(false);

  const [simEventType, setSimEventType] = useState("courseDeletion");
  const [simElementId, setSimElementId] = useState("");
  const [simulating, setSimulating] = useState(false);
  const [simResult, setSimResult] = useState<SimulationResult | null>(null);

  const [resolvingId, setResolvingId] = useState<string | null>(null);

  useEffect(() => {
    const ctrl = new AbortController();
    apiGet<PrerequisiteConflict[]>("/api/prerequisite-conflicts", ctrl.signal)
      .then(d => { setConflicts(d); setError(null); })
      .catch(() => setError("Prerequisite conflict records are temporarily unavailable."))
      .finally(() => setLoading(false));
    return () => { ctrl.abort(); };
  }, []);

  async function detectConflicts(e: React.FormEvent) {
    e.preventDefault();
    if (!detectCourseId) return;
    setDetecting(true); setMessage(null);
    try {
      const detected = await apiPost<PrerequisiteConflict[]>(`/api/prerequisite-conflicts/detect/${detectCourseId}`);
      setConflicts(prev => [...detected, ...prev]);
      setMessage(`success: Found ${detected.length} conflict(s)`);
      setDetectCourseId("");
    } catch { setMessage("error: Detection failed"); }
    finally { setDetecting(false); }
  }

  async function detectCircular() {
    setScanningCircular(true); setMessage(null);
    try {
      const detected = await apiPost<PrerequisiteConflict[]>("/api/prerequisite-conflicts/detect-circular");
      setConflicts(prev => [...detected, ...prev]);
      setMessage(`success: Found ${detected.length} circular conflict(s)`);
    } catch { setMessage("error: Circular scan failed"); }
    finally { setScanningCircular(false); }
  }

  async function simulateImpact(e: React.FormEvent) {
    e.preventDefault();
    if (!simElementId) return;
    setSimulating(true); setMessage(null);
    try {
      const result = await apiPost<SimulationResult>("/api/prerequisite-conflicts/simulate", {
        eventType: simEventType, changedElementId: simElementId
      });
      setSimResult(result);
    } catch { setMessage("error: Simulation failed"); }
    finally { setSimulating(false); }
  }

  async function toggleResolve(conflictId: string, current: boolean) {
    setResolvingId(conflictId);
    try {
      await apiPost(`/api/prerequisite-conflicts/${conflictId}/resolve`, { resolved: !current });
      setConflicts(prev => prev.map(c => c.id === conflictId ? { ...c, resolved: !current } : c));
      setMessage("success: Conflict status updated");
    } catch { setMessage("error: Failed to update conflict"); }
    finally { setResolvingId(null); }
  }

  const unresolved = conflicts.filter(c => !c.resolved);

  if (loading) return (
    <main style={{ minHeight: "100vh", display: "grid", placeItems: "center", gap: 16, color: "var(--muted)" }}>
      <div style={{ width: 40, height: 4, borderRadius: 99, overflow: "hidden", background: "var(--line)" }}>
        <div style={{ width: "50%", height: "100%", borderRadius: "inherit", background: "var(--blue-500)", animation: "launch 1s ease-in-out infinite alternate" }} />
      </div>
      Loading prerequisite conflict analysis...
    </main>
  );

  if (error) return <PageErrorDisplay message={error} />;

  return (
    <main style={{ minHeight: "100vh", maxWidth: 1480, margin: "0 auto", padding: "42px 34px 64px", background: "var(--canvas)" }}>
      <header style={{ display: "flex", alignItems: "end", justifyContent: "space-between", gap: 20, marginBottom: 28 }}>
        <div>
          <p style={{ margin: "0 0 7px", color: "var(--blue-600)", fontSize: 11, fontWeight: 800, letterSpacing: ".8px", textTransform: "uppercase" }}>Academic Integrity</p>
          <h1 style={{ margin: 0, fontSize: "clamp(27px, 3vw, 36px)", lineHeight: 1.15, letterSpacing: "-1px" }}>{t("prerequisites") ?? "Prerequisite Conflicts"}</h1>
          <span style={{ display: "block", marginTop: 9, color: "var(--muted)" }}>Detect circular prerequisites, offering sequence conflicts, and graduation bottlenecks</span>
        </div>
        {unresolved.length > 0 && (
          <span style={{ display: "inline-flex", alignItems: "center", gap: 6, padding: "8px 14px", borderRadius: 99, background: "var(--red-soft)", color: "var(--red)", fontSize: 12, fontWeight: 800 }}>
            <AlertTriangle style={{ width: 14 }} /> {unresolved.length} Unresolved
          </span>
        )}
      </header>

      {message && (
        <div style={{ display: "flex", alignItems: "center", gap: 9, padding: "11px 14px", marginBottom: 18, border: message.startsWith("error") ? "1px solid #ecc0c6" : "1px solid #aaddc9", borderRadius: 10, color: message.startsWith("error") ? "var(--red)" : "var(--green)", background: message.startsWith("error") ? "var(--red-soft)" : "var(--green-soft)", fontSize: 11 }}>
          {message.startsWith("error") ? <AlertCircle /> : <CheckCircle2 />}{message.replace(/^[^:]+: /, "")}
        </div>
      )}

      <section style={{ display: "grid", gridTemplateColumns: "minmax(0, 0.55fr) minmax(0, 0.45fr)", gap: 18, marginBottom: 18 }}>
        <div className="surface" style={{ overflow: "hidden" }}>
          <div className="surface-heading"><div><h2>Detect Conflicts</h2><p>Scan course prerequisites for issues</p></div><Search /></div>
          <div style={{ padding: 18, display: "grid", gap: 12 }}>
            <form onSubmit={detectConflicts} style={{ display: "flex", gap: 10 }}>
              <input placeholder="Enter course ID (e.g. CS-401)" value={detectCourseId} onChange={e => setDetectCourseId(e.target.value)} style={{ ...inputS, flex: 1 }} />
              <button type="submit" disabled={detecting || !detectCourseId}
                style={{ minHeight: 42, padding: "0 18px", background: "var(--blue-600)", color: "#fff", border: "none", borderRadius: 8, cursor: "pointer", fontWeight: 700, fontSize: 12, display: "flex", alignItems: "center", gap: 6, opacity: detecting || !detectCourseId ? 0.6 : 1, whiteSpace: "nowrap" }}>
                <Search style={{ width: 14 }} /> {detecting ? "Detecting..." : "Detect Conflicts"}
              </button>
            </form>
            <button onClick={detectCircular} disabled={scanningCircular}
              style={{ padding: "0.7rem", background: "var(--red)", color: "#fff", border: "none", borderRadius: 8, cursor: "pointer", fontWeight: 700, fontSize: 12, display: "flex", alignItems: "center", justifyContent: "center", gap: 7, opacity: scanningCircular ? 0.6 : 1 }}>
              <GitBranch style={{ width: 14 }} /> {scanningCircular ? "Scanning..." : "Detect Circular Prerequisites"}
            </button>
          </div>
        </div>

        <div className="surface" style={{ overflow: "hidden" }}>
          <div className="surface-heading"><div><h2>Change Impact Simulation</h2><p>Predict ripple effects of curriculum changes</p></div><Network /></div>
          <form onSubmit={simulateImpact} style={{ display: "grid", gap: 12, padding: 18 }}>
            <label style={labelS}>
              Event Type <b style={{ color: "var(--red)" }}>*</b>
              <select value={simEventType} onChange={e => setSimEventType(e.target.value)} style={inputS}>
                <option value="courseDeletion">Course Deletion</option>
                <option value="courseAddition">Course Addition</option>
                <option value="prerequisiteChange">Prerequisite Change</option>
                <option value="semesterShift">Semester Shift</option>
              </select>
            </label>
            <label style={labelS}>
              Course / Element ID <b style={{ color: "var(--red)" }}>*</b>
              <input placeholder="e.g. CS-301" value={simElementId} onChange={e => setSimElementId(e.target.value)} style={inputS} required />
            </label>
            <button type="submit" disabled={simulating}
              style={{ padding: "0.7rem", background: "var(--blue-600)", color: "#fff", border: "none", borderRadius: 8, cursor: "pointer", fontWeight: 700, fontSize: 12, opacity: simulating ? 0.6 : 1 }}>
              {simulating ? "Simulating..." : "Simulate Impact"}
            </button>
          </form>
          {simResult && (
            <div style={{ padding: "0 18px 18px", display: "grid", gap: 8 }}>
              <div style={{ display: "grid", gridTemplateColumns: "repeat(3, minmax(0, 1fr))", gap: 8 }}>
                <div style={{ padding: 10, borderRadius: 6, background: "var(--red-soft)", textAlign: "center" }}>
                  <strong style={{ display: "block", fontSize: 18, color: "var(--red)" }}>{simResult.affectedCourses.length}</strong>
                  <small style={{ color: "var(--red)", fontSize: 9 }}>Affected Courses</small>
                </div>
                <div style={{ padding: 10, borderRadius: 6, background: "var(--amber-soft)", textAlign: "center" }}>
                  <strong style={{ display: "block", fontSize: 18, color: "var(--amber)" }}>{simResult.affectedStudents}</strong>
                  <small style={{ color: "var(--amber)", fontSize: 9 }}>Affected Students</small>
                </div>
                <div style={{ padding: 10, borderRadius: 6, background: "var(--red-soft)", textAlign: "center" }}>
                  <strong style={{ display: "block", fontSize: 18, color: "var(--red)" }}>{simResult.invalidRegistrations}</strong>
                  <small style={{ color: "var(--red)", fontSize: 9 }}>Invalid Registrations</small>
                </div>
              </div>
              {simResult.cascadingConflicts.length > 0 && (
                <div style={{ padding: 10, borderRadius: 6, border: "1px solid var(--line)", background: "var(--surface-soft)" }}>
                  <small style={{ color: "var(--muted)", fontSize: 9, fontWeight: 700, display: "block", marginBottom: 4 }}>CASCADING CONFLICTS</small>
                  <div style={{ display: "flex", flexWrap: "wrap", gap: 4 }}>
                    {simResult.cascadingConflicts.map((c, i) => (
                      <span key={i} style={{ display: "inline-flex", padding: "2px 8px", borderRadius: 99, fontSize: 9, fontWeight: 700, background: "var(--red-soft)", color: "var(--red)" }}>{c}</span>
                    ))}
                  </div>
                </div>
              )}
            </div>
          )}
        </div>
      </section>

      <section className="surface" style={{ overflow: "hidden" }}>
        <div className="surface-heading"><div><h2>Conflicts</h2><p>{conflicts.length} total · {unresolved.length} unresolved</p></div><GitMerge /></div>
        {conflicts.length === 0 ? (
          <div style={{ minHeight: 220, display: "grid", placeItems: "center", alignContent: "center", gap: 8, padding: 30, color: "var(--muted)", textAlign: "center" }}>
            <Shield style={{ width: 28, opacity: 0.5 }} />
            <strong>No prerequisite conflicts detected</strong>
            <span>All prerequisite chains are valid. Run a detection to check for issues.</span>
          </div>
        ) : (
          <div style={{ overflow: "auto" }}>
            <table style={{ width: "100%", minWidth: 900, borderCollapse: "collapse" }}>
              <thead>
                <tr style={{ background: "var(--surface-soft)" }}>
                  {["", "Type", "Course", "Description", "Resolution", "Students", "Resolved"].map(h => (
                    <th key={h} style={{ padding: "10px 14px", color: "var(--muted)", fontSize: 9, fontWeight: 800, textTransform: "uppercase", textAlign: "left", borderBottom: "1px solid var(--line)" }}>{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {conflicts.map(c => (
                  <tr key={c.id} style={{ borderBottom: "1px solid var(--line)", opacity: c.resolved ? 0.55 : 1 }}>
                    <td style={{ padding: "10px 14px" }}>{severityIcon(c.severity)}</td>
                    <td style={{ padding: "10px 14px" }}>{conflictTypeBadge(c.type)}</td>
                    <td style={{ padding: "10px 14px", fontSize: 11, fontWeight: 600 }}>{c.courseName}</td>
                    <td style={{ padding: "10px 14px", fontSize: 10, color: "var(--muted)", maxWidth: 260 }}>{c.description}</td>
                    <td style={{ padding: "10px 14px", fontSize: 10, color: "var(--blue-600)", maxWidth: 220 }}>{c.resolutionSuggestion}</td>
                    <td style={{ padding: "10px 14px", fontSize: 11, fontWeight: 700 }}>{c.affectedStudents}</td>
                    <td style={{ padding: "10px 14px" }}>
                      <button onClick={() => toggleResolve(c.id, c.resolved)} disabled={resolvingId === c.id}
                        style={{ padding: "4px 10px", border: c.resolved ? "1px solid var(--line)" : "1px solid #aaddc9", borderRadius: 6, color: c.resolved ? "var(--muted)" : "var(--green)", background: c.resolved ? "var(--surface-soft)" : "var(--green-soft)", cursor: "pointer", fontSize: 10, fontWeight: 700 }}>
                        {c.resolved ? "Reopen" : "Resolve"}
                      </button>
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
