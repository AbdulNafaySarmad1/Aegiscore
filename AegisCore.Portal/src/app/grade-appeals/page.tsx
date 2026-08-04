"use client";

import { useEffect, useState } from "react";
import { apiGet, apiPost } from "@/lib/api-client";
import { AlertCircle, CheckCircle2, FileSearch, FileText, FolderOpen, Scale, Send, Upload } from "lucide-react";

type AppealStatus = "Pending" | "UnderReview" | "Resolved" | "Rejected";
type Appeal = {
  id: string;
  studentId: string;
  studentName: string;
  courseId: string;
  courseName: string;
  semester: string;
  submittedGrade: string;
  appealGrounds: string;
  status: AppealStatus;
  filedAt: string;
  committeeNotes: string | null;
};
type CommitteeBrief = {
  appealId: string;
  classification: string;
  recommendation: string;
  evidenceStrength: "Strong" | "Moderate" | "Weak";
  generatedAt: string;
};

function statusBadge(status: AppealStatus) {
  const color =
    status === "Resolved" ? "var(--green)" :
    status === "Rejected" ? "var(--red)" :
    status === "UnderReview" ? "var(--amber)" : "var(--muted)";
  const bg =
    status === "Resolved" ? "var(--green-soft)" :
    status === "Rejected" ? "var(--red-soft)" :
    status === "UnderReview" ? "var(--amber-soft)" : "var(--surface-soft)";
  return <span style={{ display: "inline-flex", padding: "3px 8px", borderRadius: 99, background: bg, color, fontSize: 10, fontWeight: 700 }}>{status}</span>;
}

const inputS: React.CSSProperties = { padding: "0.55rem 0.7rem", borderRadius: 8, border: "1px solid var(--line)", background: "var(--canvas)", color: "var(--ink)", fontSize: "0.82rem", width: "100%", boxSizing: "border-box" };

export default function GradeAppealsPage() {
  const [appeals, setAppeals] = useState<Appeal[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const [studentId, setStudentId] = useState("");
  const [courseId, setCourseId] = useState("");
  const [semester, setSemester] = useState("");
  const [grade, setGrade] = useState("");
  const [grounds, setGrounds] = useState("");
  const [file, setFile] = useState<File | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const [brief, setBrief] = useState<CommitteeBrief | null>(null);
  const [generatingBrief, setGeneratingBrief] = useState(false);
  const [processingId, setProcessingId] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    apiGet<Appeal[]>("/api/grade-appeals")
      .then(d => { if (!cancelled) { setAppeals(d); setError(null); } })
      .catch(() => { if (!cancelled) setError("Failed to load appeals"); })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, []);

  const refreshAppeals = () => {
    apiGet<Appeal[]>("/api/grade-appeals")
      .then(d => { setAppeals(d); setError(null); })
      .catch(() => setError("Failed to load appeals"));
  };

  async function submitAppeal(e: React.FormEvent) {
    e.preventDefault();
    setSubmitting(true); setMessage(null);
    try {
      const body: Record<string, unknown> = { studentId, courseId, semester, submittedGrade: grade, appealGrounds: grounds, facultyRespondentId: "00000000-0000-0000-0000-000000000000" };
      if (file) {
        const form = new FormData();
        form.append("file", file);
        Object.entries(body).forEach(([k, v]) => form.append(k, String(v)));
        await apiPost("/api/grade-appeals", form);
      } else {
        await apiPost("/api/grade-appeals", body);
      }
      setMessage("success: Appeal submitted");
      setStudentId(""); setCourseId(""); setSemester(""); setGrade(""); setGrounds(""); setFile(null);
      refreshAppeals();
    } catch { setMessage("error: Submission failed"); }
    finally { setSubmitting(false); }
  }

  async function classifyGrounds(id: string) {
    setProcessingId(id);
    try { await apiPost(`/api/grade-appeals/${id}/classify`); refreshAppeals(); } catch { /* ignore */ }
    finally { setProcessingId(null); }
  }

  async function generateBrief(id: string) {
    setGeneratingBrief(true); setBrief(null);
    try {
      const result = await apiPost<CommitteeBrief>(`/api/grade-appeals/${id}/brief`);
      setBrief(result);
    } catch { setBrief(null); }
    finally { setGeneratingBrief(false); }
  }

  async function processDecision(id: string) {
    setProcessingId(id);
    try { await apiPost(`/api/grade-appeals/${id}/process`); refreshAppeals(); } catch { /* ignore */ }
    finally { setProcessingId(null); }
  }

  const resolved = appeals.filter(a => a.status === "Resolved").length;
  const rejectionRate = appeals.length > 0 ? Math.round((appeals.filter(a => a.status === "Rejected").length / appeals.length) * 100) : 0;

  return (
    <main style={{ minHeight: "100vh", maxWidth: 1480, margin: "0 auto", padding: "42px 34px 64px", background: "var(--canvas)" }}>
      <header style={{ display: "flex", alignItems: "end", justifyContent: "space-between", gap: 20, marginBottom: 28 }}>
        <div>
          <p style={{ margin: "0 0 7px", color: "var(--blue-600)", fontSize: 11, fontWeight: 800, letterSpacing: ".8px", textTransform: "uppercase" }}>Academic Governance</p>
          <h1 style={{ margin: 0, fontSize: "clamp(27px, 3vw, 36px)", lineHeight: 1.15, letterSpacing: "-1px" }}>Grade Appeals</h1>
          <span style={{ display: "block", marginTop: 9, color: "var(--muted)" }}>Submit, classify, and adjudicate grade appeals with committee brief generation</span>
        </div>
      </header>

      {message && (
        <div style={{ display: "flex", alignItems: "center", gap: 9, padding: "11px 14px", marginBottom: 18, border: message.startsWith("error") ? "1px solid #ecc0c6" : "1px solid #aaddc9", borderRadius: 10, color: message.startsWith("error") ? "var(--red)" : "var(--green)", background: message.startsWith("error") ? "var(--red-soft)" : "var(--green-soft)", fontSize: 11 }}>
          {message.startsWith("error") ? <AlertCircle /> : <CheckCircle2 />}{message.replace(/^[^:]+: /, "")}
        </div>
      )}

      <section style={{ display: "grid", gridTemplateColumns: "minmax(0, 1.5fr) minmax(380px, 0.8fr)", gap: 18, marginBottom: 18 }}>
        <div className="surface" style={{ overflow: "hidden" }}>
          <div className="surface-heading"><div><h2>Appeal Submissions</h2><p>File a new grade appeal with supporting documentation</p></div><Scale /></div>
          <form onSubmit={submitAppeal} style={{ display: "grid", gap: 12, padding: 18 }}>
            <div style={{ display: "grid", gridTemplateColumns: "repeat(2, minmax(0, 1fr))", gap: 12 }}>
              <label style={{ display: "grid", gap: 5, color: "var(--muted)", fontSize: 10, fontWeight: 700 }}>
                Student ID
                <input placeholder="e.g. STU-2024-001" value={studentId} onChange={e => setStudentId(e.target.value)} style={inputS} required />
              </label>
              <label style={{ display: "grid", gap: 5, color: "var(--muted)", fontSize: 10, fontWeight: 700 }}>
                Course ID
                <input placeholder="e.g. CS-401" value={courseId} onChange={e => setCourseId(e.target.value)} style={inputS} required />
              </label>
              <label style={{ display: "grid", gap: 5, color: "var(--muted)", fontSize: 10, fontWeight: 700 }}>
                Semester
                <input placeholder="e.g. Fall 2025" value={semester} onChange={e => setSemester(e.target.value)} style={inputS} required />
              </label>
              <label style={{ display: "grid", gap: 5, color: "var(--muted)", fontSize: 10, fontWeight: 700 }}>
                Submitted Grade
                <input placeholder="e.g. C+" value={grade} onChange={e => setGrade(e.target.value)} style={inputS} required />
              </label>
            </div>
            <label style={{ display: "grid", gap: 5, color: "var(--muted)", fontSize: 10, fontWeight: 700 }}>
              Appeal Grounds
              <textarea placeholder="Explain the basis for this appeal in detail..." value={grounds} onChange={e => setGrounds(e.target.value)} rows={3} style={{ ...inputS, resize: "vertical" }} required />
            </label>
            <label style={{ minHeight: 39, display: "grid", placeItems: "center", border: "1px dashed var(--line-strong)", borderRadius: 8, color: "var(--blue-600)", cursor: "pointer", fontSize: 10, fontWeight: 700, padding: "10px" }}>
              <Upload style={{ marginRight: 6 }} /> {file ? file.name : "Attach supporting document (PDF, max 10MB)"}
              <input type="file" accept=".pdf,.doc,.docx" style={{ display: "none" }} onChange={e => setFile(e.target.files?.[0] ?? null)} />
            </label>
            <button type="submit" disabled={submitting} style={{ padding: "0.7rem", background: "var(--blue-600)", color: "#fff", border: "none", borderRadius: 8, cursor: "pointer", fontWeight: 700, fontSize: 12, opacity: submitting ? 0.6 : 1 }}>
              {submitting ? "Submitting..." : <><Send style={{ width: 14, verticalAlign: "middle", marginRight: 6 }} />Submit Appeal</>}
            </button>
          </form>
        </div>

        <div style={{ display: "grid", gap: 18 }}>
          <div className="surface" style={{ padding: 20 }}>
            <h3 style={{ margin: "0 0 16px", fontSize: 13, color: "var(--ink)" }}>Appeal Pipeline</h3>
            <div style={{ display: "grid", gridTemplateColumns: "repeat(2, minmax(0, 1fr))", gap: 10 }}>
              <div style={{ padding: 14, borderRadius: 10, border: "1px solid var(--line)", background: "var(--surface-soft)", textAlign: "center" }}>
                <div style={{ fontSize: 28, fontWeight: 800, color: "var(--green)" }}>{resolved}</div>
                <small style={{ color: "var(--muted)", fontSize: 10 }}>Resolved</small>
              </div>
              <div style={{ padding: 14, borderRadius: 10, border: "1px solid var(--line)", background: "var(--surface-soft)", textAlign: "center" }}>
                <div style={{ fontSize: 28, fontWeight: 800, color: rejectionRate > 30 ? "var(--red)" : "var(--amber)" }}>{rejectionRate}%</div>
                <small style={{ color: "var(--muted)", fontSize: 10 }}>Rejection Rate</small>
              </div>
            </div>
          </div>

          {brief && (
            <div className="surface" style={{ padding: 18 }}>
              <h3 style={{ margin: "0 0 10px", fontSize: 13, color: "var(--ink)", display: "flex", alignItems: "center", gap: 8 }}><FileSearch style={{ width: 16 }} /> Committee Brief</h3>
              <div style={{ display: "grid", gap: 8, fontSize: 11 }}>
                <div style={{ display: "flex", justifyContent: "space-between" }}><span style={{ color: "var(--muted)" }}>Classification</span><strong>{brief.classification}</strong></div>
                <div style={{ display: "flex", justifyContent: "space-between" }}><span style={{ color: "var(--muted)" }}>Recommendation</span><strong style={{ color: "var(--green)" }}>{brief.recommendation}</strong></div>
                <div style={{ display: "flex", justifyContent: "space-between" }}>
                  <span style={{ color: "var(--muted)" }}>Evidence</span>
                  <span style={{ padding: "2px 7px", borderRadius: 99, fontSize: 10, fontWeight: 700, background: brief.evidenceStrength === "Strong" ? "var(--green-soft)" : brief.evidenceStrength === "Moderate" ? "var(--amber-soft)" : "var(--red-soft)", color: brief.evidenceStrength === "Strong" ? "var(--green)" : brief.evidenceStrength === "Moderate" ? "var(--amber)" : "var(--red)" }}>{brief.evidenceStrength}</span>
                </div>
                <div style={{ display: "flex", justifyContent: "space-between" }}><span style={{ color: "var(--muted)" }}>Generated</span><span>{new Date(brief.generatedAt).toLocaleString()}</span></div>
              </div>
            </div>
          )}
        </div>
      </section>

      <section className="surface" style={{ overflow: "hidden" }}>
        <div className="surface-heading"><div><h2>Appeals Registry</h2><p>{appeals.length} records</p></div><FileText /></div>
        {loading ? (
          <div style={{ padding: "3rem", textAlign: "center", color: "var(--muted)" }}>Loading appeals...</div>
        ) : error ? (
          <div style={{ padding: "2rem", textAlign: "center", color: "var(--red)" }}>{error}</div>
        ) : appeals.length === 0 ? (
          <div style={{ minHeight: 200, display: "grid", placeItems: "center", alignContent: "center", gap: 8, padding: 30, color: "var(--muted)", textAlign: "center" }}>
            <FolderOpen style={{ width: 28, color: "var(--green)" }} />
            <strong>No appeals filed</strong>
            <span>Submit the first grade appeal using the form above</span>
          </div>
        ) : (
          <div style={{ overflow: "auto" }}>
            <table style={{ width: "100%", minWidth: 800, borderCollapse: "collapse" }}>
              <thead>
                <tr style={{ background: "var(--surface-soft)" }}>
                  {["ID", "Student", "Course", "Semester", "Grade", "Status", "Grounds", "Date", "Actions"].map(h => (
                    <th key={h} style={{ padding: "10px 14px", color: "var(--muted)", fontSize: 9, fontWeight: 800, textTransform: "uppercase", textAlign: "left", borderBottom: "1px solid var(--line)" }}>{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {appeals.map(a => (
                  <tr key={a.id} style={{ borderBottom: "1px solid var(--line)" }}>
                    <td style={{ padding: "10px 14px", fontSize: 10, fontFamily: "monospace", color: "var(--muted)" }}>{a.id.slice(0, 8)}...</td>
                    <td style={{ padding: "10px 14px", fontSize: 11, fontWeight: 600 }}>{a.studentName}</td>
                    <td style={{ padding: "10px 14px", fontSize: 11 }}>{a.courseName}</td>
                    <td style={{ padding: "10px 14px", fontSize: 11, color: "var(--muted)" }}>{a.semester}</td>
                    <td style={{ padding: "10px 14px", fontSize: 12, fontWeight: 700 }}>{a.submittedGrade}</td>
                    <td style={{ padding: "10px 14px" }}>{statusBadge(a.status)}</td>
                    <td style={{ padding: "10px 14px", fontSize: 10, color: "var(--muted)", maxWidth: 200, overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>{a.appealGrounds}</td>
                    <td style={{ padding: "10px 14px", fontSize: 10, color: "var(--muted)", whiteSpace: "nowrap" }}>{new Date(a.filedAt).toLocaleDateString()}</td>
                    <td style={{ padding: "10px 14px" }}>
                      <div style={{ display: "flex", gap: 6 }}>
                        <button onClick={() => classifyGrounds(a.id)} disabled={processingId === a.id} style={actionBtnS} title="Classify Grounds"><FileSearch style={{ width: 13 }} /></button>
                        <button onClick={() => generateBrief(a.id)} disabled={generatingBrief} style={actionBtnS} title="Generate Brief"><FileText style={{ width: 13 }} /></button>
                        <button onClick={() => processDecision(a.id)} disabled={processingId === a.id || a.status === "Resolved"} style={{ ...actionBtnS, color: a.status === "Resolved" ? "var(--muted)" : "var(--green)" }} title="Process Decision"><CheckCircle2 style={{ width: 13 }} /></button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
            {generatingBrief && <div style={{ padding: "10px 14px", color: "var(--muted)", fontSize: 11 }}>Generating committee brief...</div>}
          </div>
        )}
      </section>
    </main>
  );
}

const actionBtnS: React.CSSProperties = {
  width: 30, height: 30, display: "grid", placeItems: "center", border: "1px solid var(--line)", borderRadius: 6,
  background: "var(--surface)", color: "var(--blue-600)", cursor: "pointer"
};
