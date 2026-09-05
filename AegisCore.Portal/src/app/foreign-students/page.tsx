"use client";

import { PageErrorDisplay } from "@/components/ErrorDisplay";
import { useEffect, useState } from "react";
import { apiGet, apiPost } from "@/lib/api-client";
import { usePersonalityTranslations } from "@/lib/personality-translator";
import {
  AlertCircle, AlertTriangle, Building2, Calendar, CheckCircle2, Clock, FileText,
  Flag, Globe, RefreshCw, Send, Shield, UserCheck, UserX
} from "lucide-react";

type ForeignStudent = {
  id: string; studentId: string; studentName: string; nationality: string;
  passportNumber: string; passportExpiry: string; visaType: string;
  visaExpiry: string; program: string;
};

type VisaRenewal = {
  id: string; studentId: string; studentName: string; status: "Pending" | "Submitted" | "Approved" | "Rejected";
  applicationDeadline: string; documentsSubmitted: number; documentsRequired: number;
};

type ComplianceBreach = {
  id: string; studentId: string; studentName: string; description: string;
  severity: "Low" | "Medium" | "High" | "Critical"; reportedDate: string;
  resolved: boolean; resolutionNotes: string | null;
};

const expiryColor = (dateStr: string): { c: string; bg: string } => {
  const days = Math.ceil((new Date(dateStr).getTime() - Date.now()) / (1000 * 60 * 60 * 24));
  if (days <= 0) return { c: "var(--red)", bg: "var(--red-soft)" };
  if (days <= 7) return { c: "var(--red)", bg: "var(--red-soft)" };
  if (days <= 14) return { c: "var(--amber)", bg: "var(--amber-soft)" };
  if (days <= 30) return { c: "var(--amber)", bg: "var(--amber-soft)" };
  if (days <= 60) return { c: "var(--green)", bg: "var(--green-soft)" };
  return { c: "var(--blue-600)", bg: "var(--blue-100)" };
};

const daysRemaining = (dateStr: string): number => Math.ceil((new Date(dateStr).getTime() - Date.now()) / (1000 * 60 * 60 * 24));

const inputS: React.CSSProperties = { minHeight: 42, padding: "0 11px", border: "1px solid var(--line)", borderRadius: 8, color: "var(--ink)", background: "var(--canvas)", fontSize: "0.85rem", width: "100%", boxSizing: "border-box" };
const labelS: React.CSSProperties = { display: "grid", gap: 5, color: "var(--muted)", fontSize: 10, fontWeight: 700 };

export default function ForeignStudentsPage() {
  const t = usePersonalityTranslations("academic");

  const [students, setStudents] = useState<ForeignStudent[]>([]);
  const [renewals, setRenewals] = useState<VisaRenewal[]>([]);
  const [breaches, setBreaches] = useState<ComplianceBreach[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const [regStudentId, setRegStudentId] = useState("");
  const [regNationality, setRegNationality] = useState("");
  const [regPassportNo, setRegPassportNo] = useState("");
  const [regPassportExpiry, setRegPassportExpiry] = useState("");
  const [regVisaType, setRegVisaType] = useState("Student");
  const [regVisaExpiry, setRegVisaExpiry] = useState("");
  const [regProgram, setRegProgram] = useState("");
  const [registering, setRegistering] = useState(false);

  const [moraSemester, setMoraSemester] = useState("");
  const [moraCount, setMoraCount] = useState("");
  const [submittingMora, setSubmittingMora] = useState(false);

  const [renewalStudentId, setRenewalStudentId] = useState("");
  const [initiatingRenewal, setInitiatingRenewal] = useState(false);

  const [breachStudentId, setBreachStudentId] = useState("");
  const [breachDesc, setBreachDesc] = useState("");
  const [reportingBreach, setReportingBreach] = useState(false);

  useEffect(() => {
    const ctrl = new AbortController();
    const fetch = async () => {
      try {
        const [s, r, b] = await Promise.all([
          apiGet<ForeignStudent[]>("/api/foreign-students", ctrl.signal).catch(() => [] as ForeignStudent[]),
          apiGet<VisaRenewal[]>("/api/foreign-students/visa-renewals", ctrl.signal).catch(() => [] as VisaRenewal[]),
          apiGet<ComplianceBreach[]>("/api/foreign-students/compliance-breaches", ctrl.signal).catch(() => [] as ComplianceBreach[])
        ]);
        setStudents(s); setRenewals(r); setBreaches(b);
      } catch { setError("Foreign student records are temporarily unavailable."); }
      finally { setLoading(false); }
    };
    fetch();
    return () => { ctrl.abort(); };
  }, []);

  async function registerStudent(e: React.FormEvent) {
    e.preventDefault();
    setRegistering(true); setMessage(null);
    try {
      await apiPost("/api/foreign-students/register", {
        studentId: regStudentId, nationality: regNationality, passportNumber: regPassportNo,
        passportExpiry: regPassportExpiry, visaType: regVisaType,
        visaExpiry: regVisaExpiry, program: regProgram
      });
      setMessage("success: Foreign student registered");
      setRegStudentId(""); setRegNationality(""); setRegPassportNo("");
      setRegPassportExpiry(""); setRegVisaExpiry(""); setRegProgram("");
      const ctrl = new AbortController();
      const s = await apiGet<ForeignStudent[]>("/api/foreign-students", ctrl.signal);
      setStudents(s);
    } catch { setMessage("error: Registration failed"); }
    finally { setRegistering(false); }
  }

  async function submitMora(e: React.FormEvent) {
    e.preventDefault();
    setSubmittingMora(true); setMessage(null);
    try {
      await apiPost("/api/foreign-students/mora", { semester: moraSemester, studentCount: Number(moraCount) });
      setMessage("success: MORA submission completed");
      setMoraSemester(""); setMoraCount("");
    } catch { setMessage("error: MORA submission failed"); }
    finally { setSubmittingMora(false); }
  }

  async function initiateRenewal(e: React.FormEvent) {
    e.preventDefault();
    setInitiatingRenewal(true); setMessage(null);
    try {
      await apiPost("/api/foreign-students/visa-renewals/initiate", { studentId: renewalStudentId });
      setMessage("success: Renewal initiated");
      setRenewalStudentId("");
      const ctrl = new AbortController();
      const r = await apiGet<VisaRenewal[]>("/api/foreign-students/visa-renewals", ctrl.signal);
      setRenewals(r);
    } catch { setMessage("error: Renewal initiation failed"); }
    finally { setInitiatingRenewal(false); }
  }

  async function reportBreachAction(e: React.FormEvent) {
    e.preventDefault();
    setReportingBreach(true); setMessage(null);
    try {
      await apiPost("/api/foreign-students/compliance-breaches", { studentId: breachStudentId, description: breachDesc });
      setMessage("success: Breach reported");
      setBreachStudentId(""); setBreachDesc("");
      const ctrl = new AbortController();
      const b = await apiGet<ComplianceBreach[]>("/api/foreign-students/compliance-breaches", ctrl.signal);
      setBreaches(b);
    } catch { setMessage("error: Failed to report breach"); }
    finally { setReportingBreach(false); }
  }

  if (loading) return (
    <main style={{ minHeight: "100vh", display: "grid", placeItems: "center", gap: 16, color: "var(--muted)" }}>
      <div style={{ width: 40, height: 4, borderRadius: 99, overflow: "hidden", background: "var(--line)" }}>
        <div style={{ width: "50%", height: "100%", borderRadius: "inherit", background: "var(--blue-500)", animation: "launch 1s ease-in-out infinite alternate" }} />
      </div>
      Loading foreign student management...
    </main>
  );

  if (error) return <PageErrorDisplay message={error} />;

  const expiredStudents = students.filter(s => daysRemaining(s.visaExpiry) <= 7);

  return (
    <main style={{ minHeight: "100vh", maxWidth: 1480, margin: "0 auto", padding: "42px 34px 64px", background: "var(--canvas)" }}>
      <header style={{ display: "flex", alignItems: "end", justifyContent: "space-between", gap: 20, marginBottom: 28 }}>
        <div>
          <p style={{ margin: "0 0 7px", color: "var(--blue-600)", fontSize: 11, fontWeight: 800, letterSpacing: ".8px", textTransform: "uppercase" }}>International Office</p>
          <h1 style={{ margin: 0, fontSize: "clamp(27px, 3vw, 36px)", lineHeight: 1.15, letterSpacing: "-1px" }}>{t("foreignStudents") ?? "Foreign Student Management"}</h1>
          <span style={{ display: "block", marginTop: 9, color: "var(--muted)" }}>Visa tracking, MORA compliance, document expiry alerts, and renewal workflows</span>
        </div>
      </header>

      {message && (
        <div style={{ display: "flex", alignItems: "center", gap: 9, padding: "11px 14px", marginBottom: 18, border: message.startsWith("error") ? "1px solid #ecc0c6" : "1px solid #aaddc9", borderRadius: 10, color: message.startsWith("error") ? "var(--red)" : "var(--green)", background: message.startsWith("error") ? "var(--red-soft)" : "var(--green-soft)", fontSize: 11 }}>
          {message.startsWith("error") ? <AlertCircle /> : <CheckCircle2 />}{message.replace(/^[^:]+: /, "")}
        </div>
      )}

      {expiredStudents.length > 0 && (
        <div style={{ padding: "12px 16px", marginBottom: 18, border: "1px solid #f0c8cd", borderRadius: 10, background: "var(--red-soft)", color: "var(--red)", fontSize: 11, display: "flex", alignItems: "center", gap: 8 }}>
          <AlertTriangle style={{ width: 16 }} />
          <strong>{expiredStudents.length} student{expiredStudents.length > 1 ? "s" : ""} with expired or expiring visas (7 days or less)</strong>
        </div>
      )}

      <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(160px, 1fr))", gap: 14, marginBottom: 18 }}>
        <article style={{ minHeight: 110, display: "grid", alignContent: "center", padding: 20, border: "1px solid var(--line)", borderRadius: 14, background: "var(--surface)", boxShadow: "var(--shadow)" }}>
          <span style={{ color: "var(--muted)", fontSize: 11, fontWeight: 700, textTransform: "uppercase" }}>Registered</span>
          <strong style={{ margin: "7px 0 3px", fontSize: 28 }}>{students.length}</strong>
          <small style={{ color: "var(--muted)" }}>foreign students</small>
        </article>
        <article style={{ minHeight: 110, display: "grid", alignContent: "center", padding: 20, border: "1px solid var(--line)", borderRadius: 14, background: "var(--surface)", boxShadow: "var(--shadow)" }}>
          <span style={{ color: "var(--muted)", fontSize: 11, fontWeight: 700, textTransform: "uppercase" }}>Expiring Soon</span>
          <strong style={{ margin: "7px 0 3px", fontSize: 28, color: "var(--amber)" }}>{students.filter(s => { const d = daysRemaining(s.visaExpiry); return d > 0 && d <= 30; }).length}</strong>
          <small style={{ color: "var(--muted)" }}>within 30 days</small>
        </article>
        <article style={{ minHeight: 110, display: "grid", alignContent: "center", padding: 20, border: "1px solid var(--line)", borderRadius: 14, background: "var(--surface)", boxShadow: "var(--shadow)" }}>
          <span style={{ color: "var(--muted)", fontSize: 11, fontWeight: 700, textTransform: "uppercase" }}>Active Renewals</span>
          <strong style={{ margin: "7px 0 3px", fontSize: 28, color: "var(--blue-600)" }}>{renewals.filter(r => r.status !== "Approved" && r.status !== "Rejected").length}</strong>
          <small style={{ color: "var(--muted)" }}>in progress</small>
        </article>
        <article style={{ minHeight: 110, display: "grid", alignContent: "center", padding: 20, border: "1px solid var(--line)", borderRadius: 14, background: "var(--surface)", boxShadow: "var(--shadow)" }}>
          <span style={{ color: "var(--muted)", fontSize: 11, fontWeight: 700, textTransform: "uppercase" }}>Breaches</span>
          <strong style={{ margin: "7px 0 3px", fontSize: 28, color: "var(--red)" }}>{breaches.filter(b => !b.resolved).length}</strong>
          <small style={{ color: "var(--muted)" }}>unresolved</small>
        </article>
      </div>

      <section style={{ display: "grid", gridTemplateColumns: "minmax(0, 1fr) minmax(360px, 0.7fr)", gap: 18, marginBottom: 18 }}>
        <div className="surface" style={{ overflow: "hidden" }}>
          <div className="surface-heading"><div><h2>Registration</h2><p>Register a new foreign student</p></div><FileText /></div>
          <form onSubmit={registerStudent} style={{ display: "grid", gap: 12, padding: 18 }}>
            <div style={{ display: "grid", gridTemplateColumns: "repeat(2, minmax(0, 1fr))", gap: 12 }}>
              <label style={labelS}>Student ID <b style={{ color: "var(--red)" }}>*</b>
                <input placeholder="e.g. STU-2024-001" value={regStudentId} onChange={e => setRegStudentId(e.target.value)} style={inputS} required />
              </label>
              <label style={labelS}>Nationality <b style={{ color: "var(--red)" }}>*</b>
                <input placeholder="e.g. Saudi Arabian" value={regNationality} onChange={e => setRegNationality(e.target.value)} style={inputS} required />
              </label>
              <label style={labelS}>Passport Number <b style={{ color: "var(--red)" }}>*</b>
                <input placeholder="e.g. PK-1234567" value={regPassportNo} onChange={e => setRegPassportNo(e.target.value)} style={inputS} required />
              </label>
              <label style={labelS}>Passport Expiry <b style={{ color: "var(--red)" }}>*</b>
                <input type="date" value={regPassportExpiry} onChange={e => setRegPassportExpiry(e.target.value)} style={inputS} required />
              </label>
              <label style={labelS}>Visa Type <b style={{ color: "var(--red)" }}>*</b>
                <select value={regVisaType} onChange={e => setRegVisaType(e.target.value)} style={inputS}>
                  <option value="Student">Student</option>
                  <option value="Exchange">Exchange</option>
                  <option value="Research">Research</option>
                  <option value="Dependent">Dependent</option>
                </select>
              </label>
              <label style={labelS}>Visa Expiry <b style={{ color: "var(--red)" }}>*</b>
                <input type="date" value={regVisaExpiry} onChange={e => setRegVisaExpiry(e.target.value)} style={inputS} required />
              </label>
            </div>
            <label style={labelS}>Program <b style={{ color: "var(--red)" }}>*</b>
              <input placeholder="e.g. BS Computer Science" value={regProgram} onChange={e => setRegProgram(e.target.value)} style={{ ...inputS, maxWidth: 420 }} required />
            </label>
            <button type="submit" disabled={registering}
              style={{ padding: "0.7rem", background: "var(--blue-600)", color: "#fff", border: "none", borderRadius: 8, cursor: "pointer", fontWeight: 700, fontSize: 12, maxWidth: 220, opacity: registering ? 0.6 : 1 }}>
              {registering ? "Registering..." : <><UserCheck style={{ width: 14, marginRight: 6 }} />Register Student</>}
            </button>
          </form>
        </div>

        <div style={{ display: "grid", gap: 18, alignContent: "start" }}>
          <div className="surface" style={{ overflow: "hidden" }}>
            <div className="surface-heading"><div><h2>MORA Submission</h2><p>Submit foreign student data to MORA</p></div><Building2 /></div>
            <form onSubmit={submitMora} style={{ display: "grid", gap: 12, padding: 18 }}>
              <label style={labelS}>Semester <b style={{ color: "var(--red)" }}>*</b>
                <input placeholder="e.g. Fall 2026" value={moraSemester} onChange={e => setMoraSemester(e.target.value)} style={inputS} required />
              </label>
              <label style={labelS}>Student Count <b style={{ color: "var(--red)" }}>*</b>
                <input type="number" min={1} value={moraCount} onChange={e => setMoraCount(e.target.value)} style={inputS} required />
              </label>
              <button type="submit" disabled={submittingMora}
                style={{ padding: "0.7rem", background: "var(--green)", color: "#fff", border: "none", borderRadius: 8, cursor: "pointer", fontWeight: 700, fontSize: 12, opacity: submittingMora ? 0.6 : 1 }}>
                {submittingMora ? "Submitting..." : "Submit to MORA"}
              </button>
            </form>
          </div>

          <div className="surface" style={{ overflow: "hidden" }}>
            <div className="surface-heading"><div><h2>Visa Renewal</h2><p>Initiate visa renewal process</p></div><RefreshCw /></div>
            <form onSubmit={initiateRenewal} style={{ display: "grid", gap: 12, padding: 18 }}>
              <label style={labelS}>Student <b style={{ color: "var(--red)" }}>*</b>
                <select value={renewalStudentId} onChange={e => setRenewalStudentId(e.target.value)} style={inputS} required>
                  <option value="">Select student...</option>
                  {students.map(s => <option key={s.id} value={s.studentId}>{s.studentName} ({s.studentId})</option>)}
                </select>
              </label>
              <button type="submit" disabled={initiatingRenewal}
                style={{ padding: "0.7rem", background: "var(--blue-600)", color: "#fff", border: "none", borderRadius: 8, cursor: "pointer", fontWeight: 700, fontSize: 12, opacity: initiatingRenewal ? 0.6 : 1 }}>
                {initiatingRenewal ? "Initiating..." : "Initiate Renewal"}
              </button>
            </form>
            {renewals.length > 0 && (
              <div style={{ padding: "0 18px 14px" }}>
                <div style={{ display: "grid", gap: 4 }}>
                  {renewals.map(r => (
                    <div key={r.id} style={{ display: "flex", justifyContent: "space-between", alignItems: "center", padding: "8px 10px", border: "1px solid var(--line)", borderRadius: 6, background: "var(--surface-soft)" }}>
                      <div style={{ display: "grid" }}>
                        <strong style={{ fontSize: 11 }}>{r.studentName}</strong>
                        <small style={{ color: "var(--muted)", fontSize: 9 }}>Docs: {r.documentsSubmitted}/{r.documentsRequired} · Deadline: {new Date(r.applicationDeadline).toLocaleDateString()}</small>
                      </div>
                      <span style={{ display: "inline-flex", padding: "3px 8px", borderRadius: 99, fontSize: 10, fontWeight: 700, background: r.status === "Approved" ? "var(--green-soft)" : r.status === "Rejected" ? "var(--red-soft)" : "var(--amber-soft)", color: r.status === "Approved" ? "var(--green)" : r.status === "Rejected" ? "var(--red)" : "var(--amber)" }}>{r.status}</span>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>
        </div>
      </section>

      <div style={{ display: "grid", gridTemplateColumns: "minmax(0, 1.3fr) minmax(360px, 0.7fr)", gap: 18 }}>
        <div className="surface" style={{ overflow: "hidden" }}>
          <div className="surface-heading"><div><h2>Expiry Alert Panel</h2><p>{students.length} foreign students with tracked expiries</p></div><Calendar /></div>
          {students.length === 0 ? (
            <div style={{ minHeight: 180, display: "grid", placeItems: "center", alignContent: "center", gap: 8, padding: 30, color: "var(--muted)", textAlign: "center" }}>
              <Globe style={{ width: 28, opacity: 0.5 }} />
              <strong>No foreign students registered</strong>
              <span>Register students to track visa and passport expiries</span>
            </div>
          ) : (
            <div style={{ overflow: "auto" }}>
              <table style={{ width: "100%", minWidth: 800, borderCollapse: "collapse" }}>
                <thead>
                  <tr style={{ background: "var(--surface-soft)" }}>
                    {["Student", "Nationality", "Passport Expiry", "Visa Type", "Visa Expiry", "Status"].map(h => (
                      <th key={h} style={{ padding: "10px 14px", color: "var(--muted)", fontSize: 9, fontWeight: 800, textTransform: "uppercase", textAlign: "left", borderBottom: "1px solid var(--line)" }}>{h}</th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {students.map(s => {
                    const d = daysRemaining(s.visaExpiry);
                    const ec = expiryColor(s.visaExpiry);
                    return (
                      <tr key={s.id} style={{ borderBottom: "1px solid var(--line)" }}>
                        <td style={{ padding: "10px 14px", fontSize: 11, fontWeight: 600 }}>{s.studentName}</td>
                        <td style={{ padding: "10px 14px", fontSize: 11, color: "var(--muted)" }}>{s.nationality}</td>
                        <td style={{ padding: "10px 14px", fontSize: 10, color: "var(--muted)", whiteSpace: "nowrap" }}>{new Date(s.passportExpiry).toLocaleDateString()}</td>
                        <td style={{ padding: "10px 14px", fontSize: 11, color: "var(--muted)" }}>{s.visaType}</td>
                        <td style={{ padding: "10px 14px", fontSize: 10, whiteSpace: "nowrap" }}>{new Date(s.visaExpiry).toLocaleDateString()}</td>
                        <td style={{ padding: "10px 14px" }}>
                          <span style={{ display: "inline-flex", padding: "3px 8px", borderRadius: 99, fontSize: 10, fontWeight: 700, background: ec.bg, color: ec.c }}>{d <= 0 ? "Expired" : `${d} days`}</span>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}
        </div>

        <div style={{ display: "grid", gap: 18, alignContent: "start" }}>
          <div className="surface" style={{ overflow: "hidden" }}>
            <div className="surface-heading"><div><h2>Compliance Breaches</h2><p>{breaches.filter(b => !b.resolved).length} unresolved</p></div><Shield /></div>
            {breaches.length === 0 ? (
              <div style={{ minHeight: 140, display: "grid", placeItems: "center", alignContent: "center", gap: 8, padding: 20, color: "var(--muted)" }}>
                <CheckCircle2 style={{ width: 24, opacity: 0.5 }} />
                <span style={{ fontSize: 11 }}>All students compliant</span>
              </div>
            ) : (
              <div style={{ padding: 12, maxHeight: 320, overflow: "auto" }}>
                {breaches.map(b => (
                  <div key={b.id} style={{ padding: "10px 8px", borderBottom: "1px solid var(--line)" }}>
                    <div style={{ display: "flex", justifyContent: "space-between", alignItems: "start", gap: 8, marginBottom: 4 }}>
                      <strong style={{ fontSize: 11 }}>{b.studentName}</strong>
                      <span style={{ display: "inline-flex", padding: "2px 7px", borderRadius: 99, fontSize: 9, fontWeight: 700, background: b.severity === "Critical" || b.severity === "High" ? "var(--red-soft)" : "var(--amber-soft)", color: b.severity === "Critical" || b.severity === "High" ? "var(--red)" : "var(--amber)" }}>{b.severity}</span>
                    </div>
                    <small style={{ display: "block", color: "var(--muted)", fontSize: 10 }}>{b.description}</small>
                    <small style={{ display: "block", marginTop: 3, color: "var(--subtle)", fontSize: 9 }}>
                      {new Date(b.reportedDate).toLocaleDateString()}{" · "}{b.resolved ? <span style={{ color: "var(--green)" }}>Resolved: {b.resolutionNotes}</span> : <span style={{ color: "var(--red)" }}>Unresolved</span>}
                    </small>
                  </div>
                ))}
              </div>
            )}
          </div>

          <div className="surface" style={{ overflow: "hidden" }}>
            <div className="surface-heading"><div><h2>Report Breach</h2><p>Log a new compliance breach</p></div><AlertTriangle /></div>
            <form onSubmit={reportBreachAction} style={{ display: "grid", gap: 12, padding: 18 }}>
              <label style={labelS}>Student ID <b style={{ color: "var(--red)" }}>*</b>
                <input placeholder="e.g. STU-2024-001" value={breachStudentId} onChange={e => setBreachStudentId(e.target.value)} style={inputS} required />
              </label>
              <label style={labelS}>Description <b style={{ color: "var(--red)" }}>*</b>
                <textarea placeholder="Describe the compliance breach..." value={breachDesc} onChange={e => setBreachDesc(e.target.value)} rows={3} style={{ ...inputS, minHeight: 70, padding: 9, resize: "vertical" }} required />
              </label>
              <button type="submit" disabled={reportingBreach}
                style={{ padding: "0.7rem", background: "var(--red)", color: "#fff", border: "none", borderRadius: 8, cursor: "pointer", fontWeight: 700, fontSize: 12, opacity: reportingBreach ? 0.6 : 1 }}>
                {reportingBreach ? "Reporting..." : "Report Breach"}
              </button>
            </form>
          </div>
        </div>
      </div>
    </main>
  );
}
