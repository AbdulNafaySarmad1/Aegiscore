"use client";

import { useEffect, useState, useMemo } from "react";
import { apiGet, apiPost } from "@/lib/api-client";
import { AlertCircle, CheckCircle2, Clock, Goal, RefreshCw, ShieldCheck, Timer, TrendingDown, XCircle } from "lucide-react";
import { usePersonalityTranslations } from "@/lib/personality-translator";

type CriterionStatus = "Fully Met" | "Substantially Met" | "Partially Met" | "Not Met";
type Criterion = { id: string; name: string; category: string; status: CriterionStatus; score: number; maxScore: number };
type Gap = { id: string; criterion: string; description: string; effort: "Low" | "Medium" | "High"; priority: "Critical" | "High" | "Medium" | "Low"; estimatedDays: number };
type Alert = { id: string; message: string; severity: "Critical" | "Warning" | "Info"; timestamp: string };
type ReadinessData = {
  overallScore: number;
  daysUntilAudit: number;
  complianceVelocity: number;
  criteria: Criterion[];
  gaps: Gap[];
  alerts: Alert[];
};

function statusBadge(s: CriterionStatus) {
  const map: Record<CriterionStatus, { c: string; bg: string }> = {
    "Fully Met": { c: "var(--green)", bg: "var(--green-soft)" },
    "Substantially Met": { c: "var(--blue-600)", bg: "var(--blue-100)" },
    "Partially Met": { c: "var(--amber)", bg: "var(--amber-soft)" },
    "Not Met": { c: "var(--red)", bg: "var(--red-soft)" }
  };
  const v = map[s];
  return <span style={{ display: "inline-flex", padding: "3px 8px", borderRadius: 99, fontSize: 10, fontWeight: 700, background: v.bg, color: v.c }}>{s}</span>;
}

function effortBadge(e: string) {
  const map: Record<string, string> = { Low: "var(--green)", Medium: "var(--amber)", High: "var(--red)" };
  return <span style={{ display: "inline-flex", padding: "3px 7px", borderRadius: 99, fontSize: 10, fontWeight: 700, background: `${map[e]}20`, color: map[e] }}>{e}</span>;
}

export default function ReadinessPage() {
  const t = usePersonalityTranslations("academic");
  const [data, setData] = useState<ReadinessData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [checking, setChecking] = useState(false);

  useEffect(() => {
    const ctrl = new AbortController();
    apiGet<ReadinessData>("/api/readiness/compute", ctrl.signal)
      .then(d => { if (!ctrl.signal.aborted) { setData(d); setError(null); } })
      .catch(() => { if (!ctrl.signal.aborted) setError("Failed to compute readiness score"); })
      .finally(() => { if (!ctrl.signal.aborted) setLoading(false); });
    return () => ctrl.abort();
  }, []);

  function refreshData() {
    apiGet<ReadinessData>("/api/readiness/compute")
      .then(d => { setData(d); setError(null); })
      .catch(() => setError("Failed to compute readiness score"));
  }

  async function checkFreshness() {
    setChecking(true);
    try { await apiPost("/api/readiness/check-evidence-freshness"); refreshData(); } catch { /* ignore */ }
    finally { setChecking(false); }
  }

  const urgencyColor = useMemo(() => {
    if (!data) return "var(--muted)";
    const d = data.daysUntilAudit;
    return d <= 30 ? "var(--red)" : d <= 90 ? "var(--amber)" : "var(--blue-600)";
  }, [data]);

  const gaugeColor = useMemo(() => {
    if (!data) return "var(--line)";
    const s = data.overallScore;
    return s >= 80 ? "var(--green)" : s >= 50 ? "var(--amber)" : "var(--red)";
  }, [data]);

  if (loading) return <main style={{ minHeight: "100vh", display: "grid", placeItems: "center", color: "var(--muted)" }}>{t("computingReadiness")}</main>;
  if (error) return <main style={{ minHeight: "100vh", display: "grid", placeItems: "center", color: "var(--red)" }}>{error}</main>;
  if (!data) return null;

  return (
    <main style={{ minHeight: "100vh", maxWidth: 1480, margin: "0 auto", padding: "42px 34px 64px", background: "var(--canvas)" }}>
      <header style={{ display: "flex", alignItems: "end", justifyContent: "space-between", gap: 20, marginBottom: 28 }}>
        <div>
          <p style={{ margin: "0 0 7px", color: "var(--blue-600)", fontSize: 11, fontWeight: 800, letterSpacing: ".8px", textTransform: "uppercase" }}>Accreditation Engineering</p>
          <h1 style={{ margin: 0, fontSize: "clamp(27px, 3vw, 36px)", lineHeight: 1.15, letterSpacing: "-1px" }}>{t("readiness")}</h1>
          <span style={{ display: "block", marginTop: 9, color: "var(--muted)" }}>Live HEC/PEC compliance scoring, gap analysis, and predictive readiness forecasting</span>
        </div>
        <div style={{ display: "flex", gap: 8 }}>
          <button onClick={checkFreshness} disabled={checking} style={{ minHeight: 42, padding: "0 16px", border: "1px solid var(--line)", borderRadius: 8, background: "var(--surface)", color: "var(--blue-600)", cursor: "pointer", fontWeight: 700, fontSize: 11, display: "flex", alignItems: "center", gap: 6 }}>
            <RefreshCw style={{ width: 14 }} /> {checking ? t("checkingFreshness") : t("checkFreshness")}
          </button>
        </div>
      </header>

      <section style={{ display: "grid", gridTemplateColumns: "minmax(0, 1fr) minmax(340px, 0.7fr)", gap: 18, marginBottom: 18 }}>
        <div className="surface" style={{ display: "flex", alignItems: "center", justifyContent: "center", padding: 36, gap: 40, flexWrap: "wrap" }}>
          <div style={{ textAlign: "center" }}>
            <CircularGauge score={data.overallScore} color={gaugeColor} />
            <div style={{ marginTop: 12 }}>
              <div style={{ fontSize: 10, fontWeight: 700, textTransform: "uppercase", letterSpacing: ".5px", color: "var(--muted)" }}>{t("overallScore")}</div>
              <div style={{ fontSize: 38, fontWeight: 800, letterSpacing: "-1.5px", color: gaugeColor }}>{data.overallScore}%</div>
            </div>
          </div>
          <div style={{ display: "grid", gap: 14 }}>
            <div style={{ display: "flex", alignItems: "center", gap: 12, padding: 12, borderRadius: 10, border: "1px solid var(--line)", background: "var(--surface-soft)" }}>
              <div style={{ width: 44, height: 44, borderRadius: 10, display: "grid", placeItems: "center", background: urgencyColor === "var(--red)" ? "var(--red-soft)" : urgencyColor === "var(--amber)" ? "var(--amber-soft)" : "var(--blue-100)", color: urgencyColor }}>
                <Timer style={{ width: 20 }} />
              </div>
              <div style={{ display: "grid" }}>
                <strong style={{ fontSize: 22, letterSpacing: "-.5px", color: urgencyColor }}>{data.daysUntilAudit}</strong>
                <small style={{ color: "var(--muted)", fontSize: 10 }}>{t("daysUntilAudit")}</small>
              </div>
            </div>
            <div style={{ display: "flex", alignItems: "center", gap: 12, padding: 12, borderRadius: 10, border: "1px solid var(--line)", background: "var(--surface-soft)" }}>
              <div style={{ width: 44, height: 44, borderRadius: 10, display: "grid", placeItems: "center", background: "var(--blue-100)", color: "var(--blue-600)" }}>
                <TrendingDown style={{ width: 20 }} />
              </div>
              <div style={{ display: "grid" }}>
                <strong style={{ fontSize: 22, letterSpacing: "-.5px", color: data.complianceVelocity >= 0 ? "var(--green)" : "var(--red)" }}>{data.complianceVelocity > 0 ? "+" : ""}{data.complianceVelocity}%</strong>
                <small style={{ color: "var(--muted)", fontSize: 10 }}>{t("complianceVelocity")}</small>
              </div>
            </div>
            <div style={{ display: "flex", alignItems: "center", gap: 12, padding: 12, borderRadius: 10, border: "1px solid var(--line)", background: "var(--surface-soft)" }}>
              <div style={{ width: 44, height: 44, borderRadius: 10, display: "grid", placeItems: "center", background: "var(--green-soft)", color: "var(--green)" }}>
                <CheckCircle2 style={{ width: 20 }} />
              </div>
              <div style={{ display: "grid" }}>
                <strong style={{ fontSize: 22, letterSpacing: "-.5px", color: "var(--green)" }}>{data.criteria.filter(c => c.status === "Fully Met").length}/{data.criteria.length}</strong>
                <small style={{ color: "var(--muted)", fontSize: 10 }}>{t("criteriaFullyMet")}</small>
              </div>
            </div>
          </div>
        </div>

        <div className="surface" style={{ overflow: "hidden" }}>
          <div className="surface-heading"><div><h2>{t("alertsFeed")}</h2><p>{data.alerts.length} updates</p></div><AlertCircle /></div>
          <div style={{ maxHeight: 360, overflow: "auto" }}>
            {data.alerts.length === 0 ? (
              <div style={{ minHeight: 160, display: "grid", placeItems: "center", alignContent: "center", gap: 8, padding: 30, color: "var(--muted)" }}>
                <ShieldCheck style={{ width: 28, color: "var(--green)" }} />
                <span>{t("allClear")}</span>
              </div>
            ) : (
              data.alerts.map(a => (
                <div key={a.id} style={{ padding: "12px 16px", borderBottom: "1px solid var(--line)", display: "flex", gap: 10, alignItems: "flex-start" }}>
                  {a.severity === "Critical" ? <XCircle style={{ width: 16, color: "var(--red)", marginTop: 1 }} /> :
                   a.severity === "Warning" ? <AlertCircle style={{ width: 16, color: "var(--amber)", marginTop: 1 }} /> :
                   <CheckCircle2 style={{ width: 16, color: "var(--blue-600)", marginTop: 1 }} />}
                  <div style={{ minWidth: 0 }}>
                    <p style={{ margin: 0, fontSize: 11, lineHeight: 1.4 }}>{a.message}</p>
                    <small style={{ color: "var(--muted)", fontSize: 9, display: "block", marginTop: 4 }}>{new Date(a.timestamp).toLocaleString()}</small>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>
      </section>

      <section className="surface" style={{ overflow: "hidden", marginBottom: 18 }}>
        <div className="surface-heading"><div><h2>{t("criteriaBreakdown")}</h2><p>{data.criteria.length} criteria evaluated across all standards</p></div><Goal /></div>
        <div style={{ overflow: "auto" }}>
          <table style={{ width: "100%", minWidth: 750, borderCollapse: "collapse" }}>
            <thead>
              <tr style={{ background: "var(--surface-soft)" }}>
                {["Criterion", "Category", "Score", "Status"].map(h => (
                  <th key={h} style={{ padding: "10px 14px", color: "var(--muted)", fontSize: 9, fontWeight: 800, textTransform: "uppercase", textAlign: "left", borderBottom: "1px solid var(--line)" }}>{h}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {data.criteria.map(c => (
                <tr key={c.id} style={{ borderBottom: "1px solid var(--line)" }}>
                  <td style={{ padding: "10px 14px", fontSize: 11, fontWeight: 600 }}>{c.name}</td>
                  <td style={{ padding: "10px 14px", fontSize: 10, color: "var(--muted)" }}>{c.category}</td>
                  <td style={{ padding: "10px 14px" }}>
                    <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
                      <div style={{ width: 80, height: 6, borderRadius: 99, background: "var(--surface-soft)", overflow: "hidden" }}>
                        <div style={{ height: "100%", width: `${(c.score / c.maxScore) * 100}%`, borderRadius: 99, background: (c.score / c.maxScore) >= 0.8 ? "var(--green)" : (c.score / c.maxScore) >= 0.5 ? "var(--amber)" : "var(--red)" }} />
                      </div>
                      <span style={{ fontSize: 10, fontWeight: 700, minWidth: 40 }}>{c.score}/{c.maxScore}</span>
                    </div>
                  </td>
                  <td style={{ padding: "10px 14px" }}>{statusBadge(c.status)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>

      <section className="surface" style={{ overflow: "hidden" }}>
        <div className="surface-heading"><div><h2>{t("gapAnalysis")}</h2><p>{data.gaps.length} gaps identified with effort estimates and priorities</p></div><Clock /></div>
        {data.gaps.length === 0 ? (
          <div style={{ minHeight: 160, display: "grid", placeItems: "center", alignContent: "center", gap: 8, padding: 30, color: "var(--muted)" }}>
            <CheckCircle2 style={{ width: 28, color: "var(--green)" }} />
            <strong>{t("noGapsIdentified")}</strong>
            <span>{t("noGapsDesc")}</span>
          </div>
        ) : (
          <div style={{ overflow: "auto" }}>
            <table style={{ width: "100%", minWidth: 750, borderCollapse: "collapse" }}>
              <thead>
                <tr style={{ background: "var(--surface-soft)" }}>
                  {["Criterion", "Gap Description", "Effort", "Priority", "Est. Days"].map(h => (
                    <th key={h} style={{ padding: "10px 14px", color: "var(--muted)", fontSize: 9, fontWeight: 800, textTransform: "uppercase", textAlign: "left", borderBottom: "1px solid var(--line)" }}>{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {data.gaps.map(g => (
                  <tr key={g.id} style={{ borderBottom: "1px solid var(--line)" }}>
                    <td style={{ padding: "10px 14px", fontSize: 11, fontWeight: 600 }}>{g.criterion}</td>
                    <td style={{ padding: "10px 14px", fontSize: 11, maxWidth: 300 }}>{g.description}</td>
                    <td style={{ padding: "10px 14px" }}>{effortBadge(g.effort)}</td>
                    <td style={{ padding: "10px 14px" }}>
                      <span style={{ display: "inline-flex", padding: "3px 8px", borderRadius: 99, fontSize: 10, fontWeight: 700, background: g.priority === "Critical" ? "var(--red-soft)" : g.priority === "High" ? "var(--amber-soft)" : "var(--surface-soft)", color: g.priority === "Critical" ? "var(--red)" : g.priority === "High" ? "var(--amber)" : "var(--muted)" }}>{g.priority}</span>
                    </td>
                    <td style={{ padding: "10px 14px", fontSize: 11, fontWeight: 700, fontFamily: "monospace" }}>{g.estimatedDays}d</td>
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

function CircularGauge({ score, color }: { score: number; color: string }) {
  const r = 74;
  const circumference = 2 * Math.PI * r;
  const arcLength = (270 / 360) * circumference;
  const offset = arcLength * (1 - Math.min(100, score) / 100);
  return (
    <svg viewBox="0 0 200 180" style={{ width: 200, height: 180 }}>
      <g transform="translate(100, 115)">
        <circle cx="0" cy="0" r={r} fill="none" stroke="var(--surface-soft)" strokeWidth={16} strokeDasharray={`${arcLength} ${circumference - arcLength}`} strokeDashoffset={circumference * 0.125} transform="rotate(135)" />
        <circle cx="0" cy="0" r={r} fill="none" stroke={color} strokeWidth={16} strokeLinecap="round" strokeDasharray={`${arcLength} ${circumference}`} strokeDashoffset={offset + circumference * 0.125} transform="rotate(135)" style={{ transition: "stroke-dashoffset .8s ease, stroke .8s ease" }} />
      </g>
    </svg>
  );
}
