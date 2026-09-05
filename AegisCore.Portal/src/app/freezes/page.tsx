"use client";

import { PageErrorDisplay } from "@/components/ErrorDisplay";
import { useEffect, useState } from "react";
import { apiGet, apiPost } from "@/lib/api-client";
import { usePersonalityTranslations } from "@/lib/personality-translator";
import { useTenantFormatter } from "@/components/TenantPreferencesProvider";
import {
  AlertCircle, Ban, CalendarClock, CheckCircle2, ClipboardList, Clock,
  FileText, RefreshCw, Send, Upload, Users, XCircle
} from "lucide-react";

type FreezeType = "Freeze" | "Deferral" | "Withdrawal";
type FreezeStatus = "Pending" | "Approved" | "Rejected" | "Active" | "Expired" | "Returned";
type ReasonCategory = "Medical" | "Financial" | "Personal" | "Academic" | "Other";

type Freeze = {
  id: string; studentId: string; studentName: string; program: string;
  type: FreezeType; reasonCategory: ReasonCategory; reason: string;
  status: FreezeStatus; appliedAt: string; approvedBy: string | null;
  effectiveFrom: string; effectiveTo: string;
  refundAmount: number; returnStatus: string | null;
};

const typeBadge = (t: FreezeType) => {
  const map: Record<FreezeType, { c: string; bg: string }> = {
    Freeze: { c: "var(--blue-600)", bg: "var(--blue-100)" },
    Deferral: { c: "var(--amber)", bg: "var(--amber-soft)" },
    Withdrawal: { c: "var(--red)", bg: "var(--red-soft)" }
  };
  const v = map[t];
  return <span style={{ display: "inline-flex", padding: "3px 8px", borderRadius: 99, fontSize: 10, fontWeight: 700, background: v.bg, color: v.c }}>{t}</span>;
};

const statusBadge = (s: FreezeStatus) => {
  const map: Record<FreezeStatus, { c: string; bg: string }> = {
    Pending: { c: "var(--amber)", bg: "var(--amber-soft)" },
    Approved: { c: "var(--green)", bg: "var(--green-soft)" },
    Rejected: { c: "var(--red)", bg: "var(--red-soft)" },
    Active: { c: "var(--blue-600)", bg: "var(--blue-100)" },
    Expired: { c: "var(--muted)", bg: "var(--surface-soft)" },
    Returned: { c: "var(--green)", bg: "var(--green-soft)" }
  };
  const v = map[s];
  return <span style={{ display: "inline-flex", padding: "3px 8px", borderRadius: 99, fontSize: 10, fontWeight: 700, background: v.bg, color: v.c }}>{s}</span>;
};

const inputS: React.CSSProperties = { minHeight: 42, padding: "0 11px", border: "1px solid var(--line)", borderRadius: 8, color: "var(--ink)", background: "var(--canvas)", fontSize: "0.85rem", width: "100%", boxSizing: "border-box" };
const labelS: React.CSSProperties = { display: "grid", gap: 5, color: "var(--muted)", fontSize: 10, fontWeight: 700 };

export default function FreezesPage() {
  const t = usePersonalityTranslations("academic");
  const { money } = useTenantFormatter();

  const [freezes, setFreezes] = useState<Freeze[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const [studentId, setStudentId] = useState("");
  const [freezeType, setFreezeType] = useState<FreezeType>("Freeze");
  const [reasonCategory, setReasonCategory] = useState<ReasonCategory>("Academic");
  const [description, setDescription] = useState("");
  const [file, setFile] = useState<File | null>(null);
  const [effectiveFrom, setEffectiveFrom] = useState("");
  const [effectiveTo, setEffectiveTo] = useState("");
  const [submitting, setSubmitting] = useState(false);

  const [approvalNotes, setApprovalNotes] = useState("");
  const [processingId, setProcessingId] = useState<string | null>(null);
  const [returnStudentId, setReturnStudentId] = useState("");
  const [returnDate, setReturnDate] = useState("");
  const [initiatingReturn, setInitiatingReturn] = useState(false);

  useEffect(() => {
    const ctrl = new AbortController();
    apiGet<Freeze[]>("/api/freezes", ctrl.signal)
      .then(d => { setFreezes(d); setError(null); })
      .catch(() => { setError("Freeze application records are temporarily unavailable."); })
      .finally(() => { setLoading(false); });
    return () => { ctrl.abort(); };
  }, []);

  const refresh = () => {
    apiGet<Freeze[]>("/api/freezes")
      .then(d => { setFreezes(d); setError(null); })
      .catch(() => setError("Failed to refresh"));
  };

  async function submitFreeze(e: React.FormEvent) {
    e.preventDefault();
    if (!studentId || !description || !effectiveFrom || !effectiveTo) return;
    setSubmitting(true); setMessage(null);
    try {
      const body: Record<string, unknown> = {
        studentId, type: freezeType, reasonCategory, reason: description, effectiveFrom, effectiveTo
      };
      if (file) {
        const form = new FormData();
        form.append("file", file);
        Object.entries(body).forEach(([k, v]) => form.append(k, String(v)));
        await apiPost("/api/freezes", form);
      } else {
        await apiPost("/api/freezes", body);
      }
      setMessage("success: Freeze application submitted");
      setStudentId(""); setDescription(""); setFile(null); setEffectiveFrom(""); setEffectiveTo("");
      refresh();
    } catch { setMessage("error: Submission failed"); }
    finally { setSubmitting(false); }
  }

  async function approveDryze(id: string, decision: "Approved" | "Rejected") {
    setProcessingId(id);
    try {
      await apiPost(`/api/freezes/${id}/decide`, { status: decision === "Approved" ? "Approved" : "Rejected", notes: approvalNotes });
      setMessage(`success: Freeze ${decision.toLowerCase()}`);
      setApprovalNotes("");
      refresh();
    } catch { setMessage("error: Decision failed"); }
    finally { setProcessingId(null); }
  }

  async function initiateReturn(e: React.FormEvent) {
    e.preventDefault();
    if (!returnStudentId || !returnDate) return;
    setInitiatingReturn(true); setMessage(null);
    try {
      await apiPost("/api/freezes/return", { studentId: returnStudentId, returnDate });
      setMessage("success: Return initiated");
      setReturnStudentId(""); setReturnDate("");
      refresh();
    } catch { setMessage("error: Return initiation failed"); }
    finally { setInitiatingReturn(false); }
  }

  const pending = freezes.filter(f => f.status === "Pending");
  const active = freezes.filter(f => f.status === "Active");
  const approved = freezes.filter(f => f.status === "Approved" || f.status === "Active" || f.status === "Returned");

  if (loading) return (
    <main style={{ minHeight: "100vh", display: "grid", placeItems: "center", gap: 16, color: "var(--muted)" }}>
      <div style={{ width: 40, height: 4, borderRadius: 99, overflow: "hidden", background: "var(--line)" }}>
        <div style={{ width: "50%", height: "100%", borderRadius: "inherit", background: "var(--blue-500)", animation: "launch 1s ease-in-out infinite alternate" }} />
      </div>
      Loading semester freeze data...
    </main>
  );

  if (error) return <PageErrorDisplay message={error} onRetry={() => void refresh()} />;

  return (
    <main style={{ minHeight: "100vh", maxWidth: 1480, margin: "0 auto", padding: "42px 34px 64px", background: "var(--canvas)" }}>
      <header style={{ display: "flex", alignItems: "end", justifyContent: "space-between", gap: 20, marginBottom: 28 }}>
        <div>
          <p style={{ margin: "0 0 7px", color: "var(--blue-600)", fontSize: 11, fontWeight: 800, letterSpacing: ".8px", textTransform: "uppercase" }}>Student Lifecycle</p>
          <h1 style={{ margin: 0, fontSize: "clamp(27px, 3vw, 36px)", lineHeight: 1.15, letterSpacing: "-1px" }}>{t("freezes") ?? "Semester Freeze & Deferral"}</h1>
          <span style={{ display: "block", marginTop: 9, color: "var(--muted)" }}>Manage freeze applications, deferrals, withdrawals, and return processing</span>
        </div>
      </header>

      {message && (
        <div style={{ display: "flex", alignItems: "center", gap: 9, padding: "11px 14px", marginBottom: 18, border: message.startsWith("error") ? "1px solid #ecc0c6" : "1px solid #aaddc9", borderRadius: 10, color: message.startsWith("error") ? "var(--red)" : "var(--green)", background: message.startsWith("error") ? "var(--red-soft)" : "var(--green-soft)", fontSize: 11 }}>
          {message.startsWith("error") ? <AlertCircle /> : <CheckCircle2 />}{message.replace(/^[^:]+: /, "")}
        </div>
      )}

      <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(180px, 1fr))", gap: 14, marginBottom: 18 }}>
        <article style={{ minHeight: 110, display: "grid", alignContent: "center", padding: 20, border: "1px solid var(--line)", borderRadius: 14, background: "var(--surface)", boxShadow: "var(--shadow)" }}>
          <span style={{ color: "var(--muted)", fontSize: 11, fontWeight: 700, textTransform: "uppercase" }}>Total Applications</span>
          <strong style={{ margin: "7px 0 3px", fontSize: 28 }}>{freezes.length}</strong>
          <small style={{ color: "var(--muted)" }}>{pending.length} pending</small>
        </article>
        <article style={{ minHeight: 110, display: "grid", alignContent: "center", padding: 20, border: "1px solid var(--line)", borderRadius: 14, background: "var(--surface)", boxShadow: "var(--shadow)" }}>
          <span style={{ color: "var(--muted)", fontSize: 11, fontWeight: 700, textTransform: "uppercase" }}>Active Freezes</span>
          <strong style={{ margin: "7px 0 3px", fontSize: 28, color: "var(--blue-600)" }}>{active.length}</strong>
          <small style={{ color: "var(--muted)" }}>currently frozen</small>
        </article>
        <article style={{ minHeight: 110, display: "grid", alignContent: "center", padding: 20, border: "1px solid var(--line)", borderRadius: 14, background: "var(--surface)", boxShadow: "var(--shadow)" }}>
          <span style={{ color: "var(--muted)", fontSize: 11, fontWeight: 700, textTransform: "uppercase" }}>Approved</span>
          <strong style={{ margin: "7px 0 3px", fontSize: 28, color: "var(--green)" }}>{approved.length}</strong>
          <small style={{ color: "var(--muted)" }}>{freezes.length > 0 ? Math.round((approved.length / freezes.length) * 100) : 0}% rate</small>
        </article>
        <article style={{ minHeight: 110, display: "grid", alignContent: "center", padding: 20, border: "1px solid var(--line)", borderRadius: 14, background: "var(--surface)", boxShadow: "var(--shadow)" }}>
          <span style={{ color: "var(--muted)", fontSize: 11, fontWeight: 700, textTransform: "uppercase" }}>Refunds Pending</span>
          <strong style={{ margin: "7px 0 3px", fontSize: 28, color: "var(--amber)" }}>
            {money(
              freezes.reduce((sum, f) => sum + (f.refundAmount || 0), 0)
            )}
          </strong>
          <small style={{ color: "var(--muted)" }}>across all applications</small>
        </article>
      </div>

      <section style={{ display: "grid", gridTemplateColumns: "minmax(0, 1.3fr) minmax(360px, 0.8fr)", gap: 18, marginBottom: 18 }}>
        <div className="surface" style={{ overflow: "hidden" }}>
          <div className="surface-heading"><div><h2>New Application</h2><p>Submit a freeze, deferral, or withdrawal request</p></div><Clock /></div>
          <form onSubmit={submitFreeze} style={{ display: "grid", gap: 12, padding: 18 }}>
            <div style={{ display: "grid", gridTemplateColumns: "repeat(2, minmax(0, 1fr))", gap: 12 }}>
              <label style={labelS}>
                Student ID <b style={{ color: "var(--red)" }}>*</b>
                <input placeholder="e.g. STU-2024-001" value={studentId} onChange={e => setStudentId(e.target.value)} style={inputS} required />
              </label>
              <label style={labelS}>
                Type <b style={{ color: "var(--red)" }}>*</b>
                <select value={freezeType} onChange={e => setFreezeType(e.target.value as FreezeType)} style={inputS}>
                  <option value="Freeze">Semester Freeze</option>
                  <option value="Deferral">Deferral</option>
                  <option value="Withdrawal">Withdrawal</option>
                </select>
              </label>
              <label style={labelS}>
                Reason Category <b style={{ color: "var(--red)" }}>*</b>
                <select value={reasonCategory} onChange={e => setReasonCategory(e.target.value as ReasonCategory)} style={inputS}>
                  <option value="Medical">Medical</option>
                  <option value="Financial">Financial</option>
                  <option value="Personal">Personal</option>
                  <option value="Academic">Academic</option>
                  <option value="Other">Other</option>
                </select>
              </label>
              <label style={labelS}>
                Effective From <b style={{ color: "var(--red)" }}>*</b>
                <input type="date" value={effectiveFrom} onChange={e => setEffectiveFrom(e.target.value)} style={inputS} required />
              </label>
            </div>
            <label style={labelS}>
              Effective To <b style={{ color: "var(--red)" }}>*</b>
              <input type="date" value={effectiveTo} onChange={e => setEffectiveTo(e.target.value)} style={{ ...inputS, maxWidth: 360 }} required />
            </label>
            <label style={labelS}>
              Description <b style={{ color: "var(--red)" }}>*</b>
              <textarea placeholder="Describe the reason in detail..." value={description} onChange={e => setDescription(e.target.value)} rows={3} style={{ ...inputS, minHeight: 80, padding: 9, resize: "vertical" }} required />
            </label>
            <label style={{ minHeight: 39, display: "grid", placeItems: "center", border: "1px dashed var(--line-strong)", borderRadius: 8, color: "var(--blue-600)", cursor: "pointer", fontSize: 10, fontWeight: 700, padding: 10, maxWidth: 360 }}>
              <Upload style={{ marginRight: 6 }} /> {file ? file.name : "Attach supporting documents"}
              <input type="file" accept=".pdf,.jpg,.png" style={{ display: "none" }} onChange={e => setFile(e.target.files?.[0] ?? null)} />
            </label>
            <button type="submit" disabled={submitting} style={{ padding: "0.7rem", background: "var(--blue-600)", color: "#fff", border: "none", borderRadius: 8, cursor: "pointer", fontWeight: 700, fontSize: 12, opacity: submitting ? 0.6 : 1, justifySelf: "start", minWidth: 180 }}>
              {submitting ? "Submitting..." : <><Send style={{ width: 14, verticalAlign: "middle", marginRight: 6 }} />Submit Application</>}
            </button>
          </form>
        </div>

        <div style={{ display: "grid", gap: 18, alignContent: "start" }}>
          <div className="surface" style={{ overflow: "hidden" }}>
            <div className="surface-heading"><div><h2>Return Initiation</h2><p>Process student return from freeze</p></div><RefreshCw /></div>
            <form onSubmit={initiateReturn} style={{ display: "grid", gap: 12, padding: 18 }}>
              <label style={labelS}>
                Student ID <b style={{ color: "var(--red)" }}>*</b>
                <input placeholder="e.g. STU-2024-001" value={returnStudentId} onChange={e => setReturnStudentId(e.target.value)} style={inputS} required />
              </label>
              <label style={labelS}>
                Return Date <b style={{ color: "var(--red)" }}>*</b>
                <input type="date" value={returnDate} onChange={e => setReturnDate(e.target.value)} style={inputS} required />
              </label>
              <button type="submit" disabled={initiatingReturn} style={{ padding: "0.7rem", background: "var(--green)", color: "#fff", border: "none", borderRadius: 8, cursor: "pointer", fontWeight: 700, fontSize: 12, opacity: initiatingReturn ? 0.6 : 1 }}>
                {initiatingReturn ? "Processing..." : <><RefreshCw style={{ width: 14, verticalAlign: "middle", marginRight: 6 }} />Initiate Return</>}
              </button>
            </form>
          </div>

          {pending.length > 0 && (
            <div className="surface" style={{ overflow: "hidden" }}>
              <div className="surface-heading"><div><h2>Approval Queue</h2><p>{pending.length} pending decisions</p></div><ClipboardList /></div>
              <div style={{ padding: 12, display: "grid", gap: 8 }}>
                <label style={labelS}>
                  Approval Notes
                  <textarea placeholder="Optional notes for the decision..." value={approvalNotes} onChange={e => setApprovalNotes(e.target.value)} rows={2} style={{ ...inputS, minHeight: 60, padding: 9, resize: "vertical" }} />
                </label>
                <div style={{ display: "grid", gap: 6 }}>
                  {pending.slice(0, 5).map(f => (
                    <div key={f.id} style={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: 8, padding: "8px 12px", border: "1px solid var(--line)", borderRadius: 8, background: "var(--surface-soft)" }}>
                      <div style={{ display: "grid" }}>
                        <strong style={{ fontSize: 11 }}>{f.studentName}</strong>
                        <small style={{ color: "var(--muted)", fontSize: 10 }}>{f.type} — {f.reasonCategory}</small>
                      </div>
                      <div style={{ display: "flex", gap: 6 }}>
                        <button onClick={() => approveDryze(f.id, "Approved")} disabled={processingId === f.id} style={{ padding: "4px 10px", border: "1px solid #aaddc9", borderRadius: 6, color: "var(--green)", background: "var(--green-soft)", cursor: "pointer", fontSize: 10, fontWeight: 700 }}>Approve</button>
                        <button onClick={() => approveDryze(f.id, "Rejected")} disabled={processingId === f.id} style={{ padding: "4px 10px", border: "1px solid #ecc0c6", borderRadius: 6, color: "var(--red)", background: "var(--red-soft)", cursor: "pointer", fontSize: 10, fontWeight: 700 }}>Reject</button>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          )}
        </div>
      </section>

      <section className="surface" style={{ overflow: "hidden" }}>
        <div className="surface-heading"><div><h2>Active Freezes & Deferrals</h2><p>{freezes.length} total records</p></div><Users /></div>
        {freezes.length === 0 ? (
          <div style={{ minHeight: 200, display: "grid", placeItems: "center", alignContent: "center", gap: 8, padding: 30, color: "var(--muted)", textAlign: "center" }}>
            <Ban style={{ width: 28 }} />
            <strong>No freeze applications</strong>
            <span>Submit the first application using the form above</span>
          </div>
        ) : (
          <div style={{ overflow: "auto" }}>
            <table style={{ width: "100%", minWidth: 900, borderCollapse: "collapse" }}>
              <thead>
                <tr style={{ background: "var(--surface-soft)" }}>
                  {["Student", "Type", "Category", "Status", "Effective", "Refund", "Return Status"].map(h => (
                    <th key={h} style={{ padding: "10px 14px", color: "var(--muted)", fontSize: 9, fontWeight: 800, textTransform: "uppercase", textAlign: "left", borderBottom: "1px solid var(--line)" }}>{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {freezes.map(f => (
                  <tr key={f.id} style={{ borderBottom: "1px solid var(--line)" }}>
                    <td style={{ padding: "10px 14px", fontSize: 11, fontWeight: 600 }}>{f.studentName}</td>
                    <td style={{ padding: "10px 14px" }}>{typeBadge(f.type)}</td>
                    <td style={{ padding: "10px 14px", fontSize: 11, color: "var(--muted)" }}>{f.reasonCategory}</td>
                    <td style={{ padding: "10px 14px" }}>{statusBadge(f.status)}</td>
                    <td style={{ padding: "10px 14px", fontSize: 10, color: "var(--muted)", whiteSpace: "nowrap" }}>
                      {new Date(f.effectiveFrom).toLocaleDateString()} → {new Date(f.effectiveTo).toLocaleDateString()}
                    </td>
                    <td style={{ padding: "10px 14px", fontSize: 11, fontWeight: 600 }}>
                      {f.refundAmount > 0 ? money(f.refundAmount) : "—"}
                    </td>
                    <td style={{ padding: "10px 14px", fontSize: 10, color: "var(--muted)" }}>{f.returnStatus || "In freeze"}</td>
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
