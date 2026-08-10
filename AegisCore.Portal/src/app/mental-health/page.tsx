"use client";

import { useEffect, useState } from "react";
import { apiGet, apiPost } from "@/lib/api-client";
import { AlertCircle, AlertTriangle, Ban, CheckCircle2, Clock, EyeOff, HeartPulse, LifeBuoy, Loader2, Send, ShieldOff, ThermometerSun } from "lucide-react";
import { usePersonalityTranslations } from "@/lib/personality-translator";
import { useRoles, isStudentOnly } from "@/lib/use-roles";
import { AppShell } from "@/components/layout/AppShell";

type Severity = "Low" | "Moderate" | "High" | "Critical";
type ConcernType = "Academic Stress" | "Personal" | "Financial" | "Bullying" | "Other";
type Ticket = {
  id: string;
  concernType: ConcernType;
  severity: Severity;
  description: string;
  isCrisis: boolean;
  needsMandatoryReport: boolean;
  submittedAt: string;
  status: "Open" | "Assigned" | "InProgress" | "Resolved";
};

function severityBadge(s: Severity) {
  const map: Record<Severity, { c: string; bg: string }> = {
    Low: { c: "var(--green)", bg: "var(--green-soft)" },
    Moderate: { c: "var(--blue-600)", bg: "var(--blue-100)" },
    High: { c: "var(--amber)", bg: "var(--amber-soft)" },
    Critical: { c: "var(--red)", bg: "var(--red-soft)" }
  };
  const v = map[s];
  return <span style={{ display: "inline-flex", padding: "3px 8px", borderRadius: 99, fontSize: 10, fontWeight: 700, background: v.bg, color: v.c }}>{s}</span>;
}

function timeElapsed(ts: string) {
  const diff = Date.now() - new Date(ts).getTime();
  const min = Math.floor(diff / 60000);
  if (min < 60) return `${min}m ago`;
  const h = Math.floor(min / 60);
  if (h < 24) return `${h}h ago`;
  return `${Math.floor(h / 24)}d ago`;
}

const inputS: React.CSSProperties = { padding: "0.55rem 0.7rem", borderRadius: 8, border: "1px solid var(--line)", background: "var(--canvas)", color: "var(--ink)", fontSize: "0.82rem", width: "100%", boxSizing: "border-box" };

export default function MentalHealthPage() {
  const t = usePersonalityTranslations("academic");
  const roles = useRoles();
  const studentOnly = isStudentOnly(roles);

  const [queue, setQueue] = useState<Ticket[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [concernType, setConcernType] = useState<ConcernType>("Academic Stress");
  const [severity, setSeverity] = useState<Severity>("Moderate");
  const [description, setDescription] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [submitMsg, setSubmitMsg] = useState<string | null>(null);

  const [triggeringId, setTriggeringId] = useState<string | null>(null);
  const [triggerResult, setTriggerResult] = useState<string | null>(null);

  useEffect(() => {
    if (roles.length === 0) return;
    if (studentOnly) { setLoading(false); setError(null); return; }
    const ctrl = new AbortController();
    apiGet<Ticket[]>("/api/mental-health/counselor/queue", ctrl.signal)
      .then(d => { if (!ctrl.signal.aborted) { setQueue(d); setError(null); } })
      .catch(() => { if (!ctrl.signal.aborted) setError("Failed to load counselor queue"); })
      .finally(() => { if (!ctrl.signal.aborted) setLoading(false); });
    return () => ctrl.abort();
  }, [studentOnly]);

  const refreshQueue = () => {
    if (studentOnly) return;
    apiGet<Ticket[]>("/api/mental-health/counselor/queue")
      .then(d => { setQueue(d); setError(null); })
      .catch(() => setError("Failed to load counselor queue"));
  };

  async function submitTicket(e: React.FormEvent) {
    e.preventDefault();
    setSubmitting(true); setSubmitMsg(null);
    try {
      await apiPost("/api/student/v2/mental-health/tickets", { concernType, severity, description });
      setSubmitMsg("success: Ticket submitted anonymously");
      setDescription("");
      refreshQueue();
    } catch { setSubmitMsg("error: Submission failed"); }
    finally { setSubmitting(false); }
  }

  async function triggerMandatoryReport(ticketId: string) {
    setTriggeringId(ticketId); setTriggerResult(null);
    try {
      await apiPost(`/api/mental-health/report/${ticketId}/mandatory`);
      setTriggerResult("Mandatory report filed");
    } catch { setTriggerResult("Failed to file report"); }
    finally { setTriggeringId(null); }
  }

  const concernTypes: ConcernType[] = ["Academic Stress", "Personal", "Financial", "Bullying", "Other"];
  const severityOptions: Severity[] = ["Low", "Moderate", "High", "Critical"];
  const crisisCount = queue.filter(t => t.isCrisis).length;
  const openCount = queue.filter(t => t.status === "Open").length;

  if (loading) return <AppShell><main style={{ minHeight: "100vh", display: "grid", placeItems: "center", color: "var(--muted)" }}>{t("loadingMentalHealth")}</main></AppShell>;
  if (error) return <AppShell><main style={{ minHeight: "100vh", display: "grid", placeItems: "center", color: "var(--red)" }}>{error}</main></AppShell>;

  return (
    <AppShell>
    <main style={{ minHeight: "100vh", maxWidth: 1480, margin: "0 auto", padding: "42px 34px 64px", background: "var(--canvas)" }}>
      <header style={{ display: "flex", alignItems: "end", justifyContent: "space-between", gap: 20, marginBottom: 28 }}>
        <div>
          <p style={{ margin: "0 0 7px", color: "var(--blue-600)", fontSize: 11, fontWeight: 800, letterSpacing: ".8px", textTransform: "uppercase" }}>Student Wellness</p>
          <h1 style={{ margin: 0, fontSize: "clamp(27px, 3vw, 36px)", lineHeight: 1.15, letterSpacing: "-1px" }}>{t("mentalHealth")}</h1>
          <span style={{ display: "block", marginTop: 9, color: "var(--muted)" }}>Anonymous ticketing, counselor routing, crisis protocol management, and mandatory reporting</span>
        </div>
      </header>

      {!studentOnly && (
      <div style={{ display: "grid", gridTemplateColumns: "repeat(4, minmax(0, 1fr))", gap: 14, marginBottom: 18 }}>
        <div style={{ padding: 20, borderRadius: 14, border: "1px solid var(--line)", background: "var(--surface)", boxShadow: "var(--shadow)" }}>
          <span style={{ color: "var(--muted)", fontSize: 10, fontWeight: 700, textTransform: "uppercase" }}>Active Queue</span>
          <div style={{ fontSize: 28, fontWeight: 800, letterSpacing: "-.5px", margin: "8px 0 3px" }}>{openCount}</div>
          <small style={{ color: "var(--muted)" }}>{queue.filter(t => t.status === "InProgress").length} in progress</small>
        </div>
        <div style={{ padding: 20, borderRadius: 14, border: "1px solid var(--line)", background: crisisCount > 0 ? "var(--red-soft)" : "var(--surface)", boxShadow: "var(--shadow)" }}>
          <span style={{ color: "var(--muted)", fontSize: 10, fontWeight: 700, textTransform: "uppercase" }}>{t("crisisTickets")}</span>
          <div style={{ fontSize: 28, fontWeight: 800, letterSpacing: "-.5px", margin: "8px 0 3px", color: crisisCount > 0 ? "var(--red)" : "var(--green)" }}>{crisisCount}</div>
          <small style={{ color: crisisCount > 0 ? "var(--red)" : "var(--muted)" }}>{crisisCount > 0 ? t("immediateAttention") : t("noActiveCrises")}</small>
        </div>
        <div style={{ padding: 20, borderRadius: 14, border: "1px solid var(--line)", background: "var(--surface)", boxShadow: "var(--shadow)" }}>
          <span style={{ color: "var(--muted)", fontSize: 10, fontWeight: 700, textTransform: "uppercase" }}>Resolved (24h)</span>
          <div style={{ fontSize: 28, fontWeight: 800, letterSpacing: "-.5px", margin: "8px 0 3px", color: "var(--green)" }}>{queue.filter(t => t.status === "Resolved").length}</div>
          <small style={{ color: "var(--muted)" }}>Cases closed</small>
        </div>
        <div style={{ padding: 20, borderRadius: 14, border: "1px solid var(--line)", background: "var(--surface)", boxShadow: "var(--shadow)" }}>
          <span style={{ color: "var(--muted)", fontSize: 10, fontWeight: 700, textTransform: "uppercase" }}>Mandatory Reports</span>
          <div style={{ fontSize: 28, fontWeight: 800, letterSpacing: "-.5px", margin: "8px 0 3px", color: queue.filter(t => t.needsMandatoryReport).length > 0 ? "var(--amber)" : "var(--green)" }}>{queue.filter(t => t.needsMandatoryReport).length}</div>
          <small style={{ color: "var(--muted)" }}>Requiring escalation</small>
        </div>
      </div>
      )}

      <section style={{ display: "grid", gridTemplateColumns: studentOnly ? "1fr" : "minmax(0, 1.2fr) minmax(360px, 0.7fr)", gap: 18, marginBottom: 18 }}>
        <div className="surface" style={{ overflow: "hidden" }}>
          <div className="surface-heading"><div><h2>{t("anonymousTicket")}</h2><p>Students can submit confidential mental health concerns</p></div><EyeOff /></div>
          <form onSubmit={submitTicket} style={{ display: "grid", gap: 12, padding: 18 }}>
            {submitMsg && (
              <div style={{ padding: "10px 14px", borderRadius: 8, border: submitMsg.startsWith("error") ? "1px solid #ecc0c6" : "1px solid #aaddc9", color: submitMsg.startsWith("error") ? "var(--red)" : "var(--green)", background: submitMsg.startsWith("error") ? "var(--red-soft)" : "var(--green-soft)", fontSize: 11, display: "flex", alignItems: "center", gap: 6 }}>
                {submitMsg.startsWith("error") ? <AlertCircle style={{ width: 15 }} /> : <CheckCircle2 style={{ width: 15 }} />}{submitMsg.replace(/^[^:]+: /, "")}
              </div>
            )}
            <div style={{ display: "grid", gridTemplateColumns: "repeat(2, minmax(0, 1fr))", gap: 12 }}>
              <label style={{ display: "grid", gap: 5, color: "var(--muted)", fontSize: 10, fontWeight: 700 }}>
                Concern Type
                <select value={concernType} onChange={e => setConcernType(e.target.value as ConcernType)} style={inputS}>
                  {concernTypes.map(c => <option key={c} value={c}>{c}</option>)}
                </select>
              </label>
              <label style={{ display: "grid", gap: 5, color: "var(--muted)", fontSize: 10, fontWeight: 700 }}>
                Severity
                <div style={{ display: "grid", gridTemplateColumns: "repeat(4, minmax(0, 1fr))", gap: 6 }}>
                  {severityOptions.map(s => (
                    <button key={s} type="button" onClick={() => setSeverity(s)}
                      style={{
                        minHeight: 36, border: severity === s ? `2px solid ${s === "Critical" ? "var(--red)" : s === "High" ? "var(--amber)" : s === "Moderate" ? "var(--blue-600)" : "var(--green)"}` : "1px solid var(--line)",
                        borderRadius: 6, background: severity === s ? "var(--surface-soft)" : "var(--canvas)", color: "var(--ink)", cursor: "pointer", fontSize: 10, fontWeight: 700
                      }}>{s}</button>
                  ))}
                </div>
              </label>
            </div>
            <label style={{ display: "grid", gap: 5, color: "var(--muted)", fontSize: 10, fontWeight: 700 }}>
              Description
              <textarea placeholder="Describe your concerns confidentially..." value={description} onChange={e => setDescription(e.target.value)} rows={4} style={{ ...inputS, resize: "vertical" }} required />
            </label>
            <button type="submit" disabled={submitting} style={{ padding: "0.7rem", background: "var(--blue-600)", color: "#fff", border: "none", borderRadius: 8, cursor: "pointer", fontWeight: 700, fontSize: 12, display: "flex", alignItems: "center", justifyContent: "center", gap: 6, opacity: submitting ? 0.6 : 1 }}>
              <Send style={{ width: 14 }} /> {submitting ? t("submitting") : t("submitAnonymousTicket")}
            </button>
          </form>
        </div>

        {!studentOnly && (
        <div className="surface" style={{ overflow: "hidden" }}>
          <div className="surface-heading"><div><h2>{t("mandatoryReportPanel")}</h2><p>Trigger mandatory reports for crisis tickets</p></div><ShieldOff /></div>
          <div style={{ padding: 18, maxHeight: 280, overflow: "auto" }}>
            {queue.filter(t => t.isCrisis || t.needsMandatoryReport).length === 0 ? (
              <div style={{ minHeight: 120, display: "grid", placeItems: "center", alignContent: "center", gap: 8, padding: 20, color: "var(--muted)", textAlign: "center" }}>
                <CheckCircle2 style={{ width: 24, color: "var(--green)" }} />
                <span style={{ fontSize: 11 }}>{t("noTicketsRequireReport")}</span>
              </div>
            ) : (
              queue.filter(tck => tck.isCrisis || tck.needsMandatoryReport).map(tck => (
                <div key={tck.id} style={{ padding: "10px 0", borderBottom: "1px solid var(--line)" }}>
                  <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 4 }}>
                    <div style={{ display: "flex", alignItems: "center", gap: 6 }}>
                      {tck.isCrisis && <AlertTriangle style={{ width: 14, color: "var(--red)" }} />}
                      <span style={{ fontSize: 11, fontWeight: 600 }}>{tck.concernType}</span>
                      {severityBadge(tck.severity)}
                    </div>
                    <span style={{ fontSize: 9, color: "var(--muted)" }}>{timeElapsed(tck.submittedAt)}</span>
                  </div>
                  <p style={{ margin: "4px 0 6px", fontSize: 10, color: "var(--muted)", lineHeight: 1.4 }}>{tck.description.slice(0, 100)}{tck.description.length > 100 ? "..." : ""}</p>
                  <button onClick={() => triggerMandatoryReport(tck.id)} disabled={triggeringId === tck.id}
                    style={{ padding: "4px 10px", border: "1px solid var(--red)", borderRadius: 6, background: "var(--red-soft)", color: "var(--red)", cursor: "pointer", fontSize: 10, fontWeight: 700, display: "flex", alignItems: "center", gap: 4 }}>
                    {triggeringId === tck.id ? <Loader2 style={{ width: 12 }} className="spin" /> : <Ban style={{ width: 12 }} />}
                    {t("fileMandatoryReport")}
                  </button>
                </div>
              ))
            )}
          </div>
          {triggerResult && <div style={{ padding: "10px 18px", borderTop: "1px solid var(--line)", fontSize: 11, color: triggerResult.includes("Failed") ? "var(--red)" : "var(--green)" }}>{triggerResult}</div>}
        </div>
        )}
      </section>

      {!studentOnly && (
      <section className="surface" style={{ overflow: "hidden" }}>
        <div className="surface-heading"><div><h2>{t("counselorQueue")}</h2><p>{queue.length} tickets — {crisisCount} require crisis intervention</p></div><HeartPulse /></div>
        {queue.length === 0 ? (
          <div style={{ minHeight: 200, display: "grid", placeItems: "center", alignContent: "center", gap: 8, padding: 30, color: "var(--muted)", textAlign: "center" }}>
            <ThermometerSun style={{ width: 28, color: "var(--green)" }} />
            <strong>{t("queueIsClear")}</strong>
            <span>{t("noPendingTickets")}</span>
          </div>
        ) : (
          <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(320px, 1fr))", gap: 14, padding: 18 }}>
            {queue.map(tck => (
              <div key={tck.id} style={{ padding: 16, borderRadius: 12, border: tck.isCrisis ? "1px solid var(--red)" : tck.needsMandatoryReport ? "1px solid var(--amber)" : "1px solid var(--line)", background: tck.isCrisis ? "var(--red-soft)" : "var(--surface)", boxShadow: tck.isCrisis ? "none" : "0 4px 12px rgb(0 0 0 / 4%)" }}>
                <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", marginBottom: 8 }}>
                  <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
                    <div style={{ width: 32, height: 32, borderRadius: 8, display: "grid", placeItems: "center", background: tck.isCrisis ? "var(--red)" : "var(--blue-100)", color: tck.isCrisis ? "#fff" : "var(--blue-600)", fontSize: 13, fontWeight: 800 }}>
                      {tck.isCrisis ? <AlertTriangle style={{ width: 16 }} /> : tck.concernType.charAt(0)}
                    </div>
                    <div>
                      <strong style={{ fontSize: 12 }}>{tck.concernType}</strong>
                      <div style={{ fontSize: 9, color: "var(--muted)", marginTop: 1 }}>{tck.id.slice(0, 8)}</div>
                    </div>
                  </div>
                  <div style={{ display: "flex", alignItems: "center", gap: 6 }}>
                    {tck.needsMandatoryReport && <span style={{ padding: "2px 6px", borderRadius: 99, fontSize: 9, fontWeight: 700, background: "var(--amber-soft)", color: "var(--amber)" }}>Report</span>}
                    <span style={{ display: "flex", alignItems: "center", gap: 3, fontSize: 9, color: "var(--muted)" }}><Clock style={{ width: 11 }} />{timeElapsed(tck.submittedAt)}</span>
                  </div>
                </div>
                <p style={{ margin: "0 0 10px", fontSize: 10, color: "var(--muted)", lineHeight: 1.5 }}>{tck.description}</p>
                <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                  <div style={{ display: "flex", gap: 6, alignItems: "center" }}>
                    {severityBadge(tck.severity)}
                    <span style={{ display: "inline-flex", padding: "3px 7px", borderRadius: 99, fontSize: 9, fontWeight: 700, background: tck.status === "Open" ? "var(--amber-soft)" : tck.status === "InProgress" ? "var(--blue-100)" : "var(--green-soft)", color: tck.status === "Open" ? "var(--amber)" : tck.status === "InProgress" ? "var(--blue-600)" : "var(--green)" }}>{tck.status}</span>
                  </div>
                  {tck.isCrisis && (
                    <div style={{ display: "flex", alignItems: "center", gap: 4, color: "var(--red)", fontSize: 10, fontWeight: 800 }}>
                      <LifeBuoy style={{ width: 14 }} /> CRISIS
                    </div>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}
      </section>
      )}
    </main>
    </AppShell>
  );
}
