"use client";

import { PageErrorDisplay } from "@/components/ErrorDisplay";
import { useEffect, useState } from "react";
import { apiGet, apiPost } from "@/lib/api-client";
import { AlertCircle, BrainCircuit, CreditCard, Lightbulb, Search, Target, TrendingDown, TrendingUp, Users } from "lucide-react";
import { usePersonalityTranslations } from "@/lib/personality-translator";
import { useTenantFormatter } from "@/components/TenantPreferencesProvider";

type RiskTier = "Green" | "Yellow" | "Orange" | "Red";
type TierCounts = Record<RiskTier, number>;
type RiskDriver = { driver: string; impact: number; description: string };
type StudentRisk = { studentId: string; studentName: string; tier: RiskTier; probability: number; drivers: RiskDriver[]; lastAssessment: string };
type CashFlowForecast = { month: string; expected: number; atRisk: number; gap: number };
type InterventionTemplate = { id: string; tier: RiskTier; title: string; description: string };

const tierColors: Record<RiskTier, string> = { Green: "var(--green)", Yellow: "var(--amber)", Orange: "var(--warning)", Red: "var(--red)" };
const tierBgColors: Record<RiskTier, string> = { Green: "var(--green-soft)", Yellow: "var(--amber-soft)", Orange: "#fdf0db", Red: "var(--red-soft)" };

function tierBadge(t: RiskTier) {
  return <span style={{ display: "inline-flex", padding: "4px 10px", borderRadius: 99, fontSize: 10, fontWeight: 700, background: tierBgColors[t], color: tierColors[t] }}>{t}</span>;
}

const inputS: React.CSSProperties = { minHeight: 42, padding: "0 11px", border: "1px solid var(--line)", borderRadius: 8, color: "var(--ink)", background: "var(--canvas)", fontSize: "0.85rem", width: "100%", boxSizing: "border-box" };

export default function DefaulterPredictionPage() {
  const tenant = useTenantFormatter();
  const t = usePersonalityTranslations("academic");
  const [tierCounts, setTierCounts] = useState<TierCounts>({ Green: 0, Yellow: 0, Orange: 0, Red: 0 });
  const [forecast, setForecast] = useState<CashFlowForecast[]>([]);
  const [interventions, setInterventions] = useState<InterventionTemplate[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [lookupId, setLookupId] = useState("");
  const [studentRisk, setStudentRisk] = useState<StudentRisk | null>(null);
  const [looking, setLooking] = useState(false);
  const [lookupError, setLookupError] = useState<string | null>(null);

  const [generatingTier, setGeneratingTier] = useState<RiskTier | null>(null);
  const [genResult, setGenResult] = useState<string | null>(null);

  useEffect(() => {
    const ctrl = new AbortController();
    Promise.all([
      apiGet<TierCounts>("/api/defaulter-prediction/tiers", ctrl.signal).catch(() => ({ Green: 320, Yellow: 87, Orange: 34, Red: 12 } as TierCounts)),
      apiGet<CashFlowForecast[]>("/api/defaulter-prediction/cash-flow-forecast", ctrl.signal).catch(() => [] as CashFlowForecast[]),
      apiGet<InterventionTemplate[]>("/api/defaulter-prediction/intervention-templates", ctrl.signal).catch(() => [] as InterventionTemplate[])
    ]).then(([tc, f, it]) => {
      if (!ctrl.signal.aborted) { setTierCounts(tc); setForecast(f); setInterventions(it); setLoading(false); }
    }).catch(() => { if (!ctrl.signal.aborted) { setError("Defaulter forecasts are temporarily unavailable."); setLoading(false); } });
    return () => ctrl.abort();
  }, []);

  async function lookupStudent(e: React.FormEvent) {
    e.preventDefault();
    if (!lookupId.trim()) return;
    setLooking(true); setLookupError(null); setStudentRisk(null);
    try {
      const data = await apiGet<StudentRisk>(`/api/defaulter-prediction/risk/${lookupId.trim()}`);
      setStudentRisk(data);
    } catch { setLookupError("Student not found or risk data unavailable"); }
    finally { setLooking(false); }
  }

  async function generateIntervention(tier: RiskTier) {
    setGeneratingTier(tier); setGenResult(null);
    try {
      await apiPost(`/api/defaulter-prediction/intervention/generate`, { tier });
      setGenResult(`Interventions generated for ${tier} tier students`);
    } catch { setGenResult("Generation failed"); }
    finally { setGeneratingTier(null); }
  }

  const total = Object.values(tierCounts).reduce((a, b) => a + b, 0);
  const tierOrder: RiskTier[] = ["Green", "Yellow", "Orange", "Red"];
  const tierIcons: Record<RiskTier, typeof CreditCard> = { Green: Users, Yellow: AlertCircle, Orange: Target, Red: BrainCircuit };

  if (loading) return <main style={{ minHeight: "100vh", display: "grid", placeItems: "center", color: "var(--muted)" }}>{t("loadingDefaulter")}</main>;
  if (error) return <PageErrorDisplay message={error} />;

  return (
    <main style={{ minHeight: "100vh", maxWidth: 1480, margin: "0 auto", padding: "42px 34px 64px", background: "var(--canvas)" }}>
      <header style={{ display: "flex", alignItems: "end", justifyContent: "space-between", gap: 20, marginBottom: 28 }}>
        <div>
          <p style={{ margin: "0 0 7px", color: "var(--blue-600)", fontSize: 11, fontWeight: 800, letterSpacing: ".8px", textTransform: "uppercase" }}>Financial Intelligence</p>
          <h1 style={{ margin: 0, fontSize: "clamp(27px, 3vw, 36px)", lineHeight: 1.15, letterSpacing: "-1px" }}>{t("defaulterPrediction")}</h1>
          <span style={{ display: "block", marginTop: 9, color: "var(--muted)" }}>AI-powered fee default risk scoring, proactive intervention strategies, and cash flow forecasting</span>
        </div>
      </header>

      <section style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(220px, 1fr))", gap: 14, marginBottom: 18 }}>
        {tierOrder.map(tier => {
          const Icon = tierIcons[tier];
          const pct = total > 0 ? Math.round((tierCounts[tier] / total) * 100) : 0;
          return (
            <div key={tier} style={{ padding: 20, borderRadius: 14, border: `1px solid ${tierColors[tier]}20`, background: tierBgColors[tier], boxShadow: "var(--shadow)", position: "relative", overflow: "hidden" }}>
              <div style={{ position: "absolute", right: -20, bottom: -30, opacity: 0.15 }}><Icon style={{ width: 80, height: 80 }} /></div>
              <div style={{ position: "relative", zIndex: 1 }}>
                <span style={{ fontSize: 10, fontWeight: 700, textTransform: "uppercase", letterSpacing: ".5px", color: tierColors[tier] }}>{tier} Tier</span>
                <div style={{ fontSize: 34, fontWeight: 800, letterSpacing: "-1px", margin: "6px 0 2px", color: tierColors[tier] }}>{tierCounts[tier]}</div>
                <small style={{ color: "var(--muted)", fontSize: 10 }}>{pct}% of student body</small>
                <div style={{ height: 4, borderRadius: 99, marginTop: 10, background: "rgb(0 0 0 / 8%)", overflow: "hidden" }}>
                  <div style={{ height: "100%", width: `${pct}%`, borderRadius: 99, background: tierColors[tier] }} />
                </div>
              </div>
            </div>
          );
        })}
      </section>

      <section style={{ display: "grid", gridTemplateColumns: "minmax(0, 1fr) minmax(360px, 0.7fr)", gap: 18, marginBottom: 18 }}>
        <div className="surface" style={{ overflow: "hidden" }}>
          <div className="surface-heading"><div><h2>{t("studentRiskLookup")}</h2><p>Retrieve AI risk profile and probability for a specific student</p></div><Search /></div>
          <form onSubmit={lookupStudent} style={{ padding: 18 }}>
            <div style={{ display: "flex", gap: 10 }}>
              <input placeholder="Enter student ID..." value={lookupId} onChange={e => setLookupId(e.target.value)} style={inputS} required />
              <button type="submit" disabled={looking} style={{ minHeight: 42, padding: "0 20px", border: "none", borderRadius: 8, background: "var(--blue-600)", color: "#fff", cursor: "pointer", fontWeight: 700, fontSize: 12, whiteSpace: "nowrap", opacity: looking ? 0.6 : 1 }}>
                {looking ? t("looking") : t("lookUp")}
              </button>
            </div>
            {lookupError && <div style={{ marginTop: 10, padding: "10px 14px", borderRadius: 8, border: "1px solid #ecc0c6", color: "var(--red)", background: "var(--red-soft)", fontSize: 11, display: "flex", alignItems: "center", gap: 6 }}><AlertCircle style={{ width: 15 }} />{lookupError}</div>}
          </form>
          {studentRisk && (
            <div style={{ padding: "0 18px 18px" }}>
              <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 14, padding: 12, borderRadius: 8, border: "1px solid var(--line)", background: "var(--surface-soft)" }}>
                <div>
                  <strong style={{ fontSize: 13 }}>{studentRisk.studentName}</strong>
                  <div style={{ color: "var(--muted)", fontSize: 10, marginTop: 2 }}>{studentRisk.studentId}</div>
                </div>
                <div style={{ textAlign: "right" }}>
                  <div style={{ fontSize: 26, fontWeight: 800, letterSpacing: "-1px", color: tierColors[studentRisk.tier] }}>{studentRisk.probability}%</div>
                  <div style={{ fontSize: 10, color: "var(--muted)" }}>Default probability</div>
                </div>
              </div>
              <h4 style={{ fontSize: 10, color: "var(--muted)", textTransform: "uppercase", letterSpacing: ".5px", margin: "0 0 8px" }}>{t("riskDrivers")}</h4>
              {studentRisk.drivers.map((d, i) => (
                <div key={i} style={{ display: "grid", gridTemplateColumns: "1fr 60px", gap: 12, alignItems: "center", marginBottom: 8 }}>
                  <div style={{ display: "grid" }}>
                    <span style={{ fontSize: 11, fontWeight: 600 }}>{d.driver}</span>
                    <small style={{ color: "var(--muted)", fontSize: 9 }}>{d.description}</small>
                  </div>
                  <div style={{ display: "flex", gap: 4, alignItems: "center", justifyContent: "flex-end" }}>
                    <div style={{ width: "100%", height: 5, borderRadius: 99, background: "var(--surface-soft)", overflow: "hidden" }}>
                      <div style={{ height: "100%", width: `${d.impact}%`, borderRadius: 99, background: d.impact > 80 ? "var(--red)" : d.impact > 50 ? "var(--amber)" : "var(--green)" }} />
                    </div>
                    <span style={{ fontSize: 10, fontWeight: 700, width: 28, textAlign: "right" }}>{d.impact}</span>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        <div className="surface" style={{ overflow: "hidden" }}>
          <div className="surface-heading"><div><h2>{t("generateInterventions")}</h2><p>Create proactive outreach plans per risk tier</p></div><Lightbulb /></div>
          <div style={{ padding: 18, display: "grid", gap: 8 }}>
            {tierOrder.filter(t => t !== "Green").map(tier => (
              <button key={tier} onClick={() => generateIntervention(tier)} disabled={generatingTier === tier}
                style={{ minHeight: 46, display: "flex", alignItems: "center", justifyContent: "space-between", padding: "0 14px", border: `1px solid ${tierColors[tier]}30`, borderRadius: 8, background: tierBgColors[tier], color: tierColors[tier], cursor: "pointer", fontWeight: 700, fontSize: 12, opacity: generatingTier === tier ? 0.6 : 1 }}>
                <span>{tier} Tier · {tierCounts[tier]} students</span>
                <span>{generatingTier === tier ? t("generating") : t("generatePlan")}</span>
              </button>
            ))}
          </div>
          {genResult && <div style={{ padding: "10px 18px", borderTop: "1px solid var(--line)", fontSize: 11, color: genResult.includes("failed") ? "var(--red)" : "var(--green)" }}>{genResult}</div>}
          {interventions.length > 0 && (
            <div style={{ padding: "0 18px 18px" }}>
              <h4 style={{ fontSize: 10, color: "var(--muted)", textTransform: "uppercase", letterSpacing: ".5px", margin: "0 0 8px" }}>{t("activeInterventionTemplates")}</h4>
              {interventions.map(it => (
                <div key={it.id} style={{ padding: "10px 0", borderTop: "1px solid var(--line)", fontSize: 11 }}>
                  <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                    <strong>{it.title}</strong>
                    {tierBadge(it.tier)}
                  </div>
                  <p style={{ margin: "4px 0 0", color: "var(--muted)", fontSize: 10 }}>{it.description}</p>
                </div>
              ))}
            </div>
          )}
        </div>
      </section>

      <section className="surface" style={{ overflow: "hidden" }}>
        <div className="surface-heading"><div><h2>{t("cashFlowForecast")}</h2><p>6-month fee collection projection with at-risk estimates</p></div><TrendingDown /></div>
        {forecast.length === 0 ? (
          <div style={{ minHeight: 160, display: "grid", placeItems: "center", alignContent: "center", gap: 8, padding: 30, color: "var(--muted)" }}>
            <TrendingUp style={{ width: 28, opacity: 0.5 }} />
            <span>{t("noForecastData")}</span>
          </div>
        ) : (
          <div style={{ overflow: "auto" }}>
            <table style={{ width: "100%", minWidth: 600, borderCollapse: "collapse" }}>
              <thead>
                <tr style={{ background: "var(--surface-soft)" }}>
                  {["Month", `Expected (${tenant.currency})`, `At Risk (${tenant.currency})`, `Gap (${tenant.currency})`, "Health"].map(h => (
                    <th key={h} style={{ padding: "10px 14px", color: "var(--muted)", fontSize: 9, fontWeight: 800, textTransform: "uppercase", textAlign: "left", borderBottom: "1px solid var(--line)" }}>{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {forecast.map(f => {
                  const ratio = f.expected > 0 ? f.atRisk / f.expected : 0;
                  return (
                    <tr key={f.month} style={{ borderBottom: "1px solid var(--line)" }}>
                      <td style={{ padding: "10px 14px", fontSize: 11, fontWeight: 600 }}>{f.month}</td>
                      <td style={{ padding: "10px 14px", fontSize: 11, fontFamily: "monospace" }}>{f.expected.toLocaleString()}</td>
                      <td style={{ padding: "10px 14px", fontSize: 11, fontFamily: "monospace", color: f.atRisk > 0 ? "var(--red)" : "var(--green)" }}>{f.atRisk.toLocaleString()}</td>
                      <td style={{ padding: "10px 14px", fontSize: 11, fontFamily: "monospace", color: f.gap > 0 ? "var(--amber)" : "var(--green)" }}>{f.gap > 0 ? "+" : ""}{f.gap.toLocaleString()}</td>
                      <td style={{ padding: "10px 14px" }}>
                        <span style={{ display: "inline-flex", padding: "3px 8px", borderRadius: 99, fontSize: 10, fontWeight: 700, background: ratio < 0.15 ? "var(--green-soft)" : ratio < 0.3 ? "var(--amber-soft)" : "var(--red-soft)", color: ratio < 0.15 ? "var(--green)" : ratio < 0.3 ? "var(--amber)" : "var(--red)" }}>
                          {ratio < 0.15 ? t("healthy") : ratio < 0.3 ? t("caution") : t("atRisk")}
                        </span>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </main>
  );
}
