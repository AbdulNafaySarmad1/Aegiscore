"use client";

import { useEffect, useState } from "react";
import { apiGet } from "@/lib/api-client";
import { AlertCircle, Building2, FileCheck2, Globe, GraduationCap, TrendingUp, UserPlus } from "lucide-react";
import { usePersonalityTranslations } from "@/lib/personality-translator";

type PlacementStatus = "Placed" | "OfferAccepted" | "Interviewing" | "Searching" | "NotParticipating";
type Placement = {
  id: string;
  studentId: string;
  studentName: string;
  company: string;
  role: string;
  stipend: number;
  status: PlacementStatus;
  startDate: string;
  durationMonths: number;
};
type ReadinessResult = {
  studentId: string;
  studentName: string;
  overallScore: number;
  skills: { skill: string; score: number }[];
  recommendations: string[];
};
type Analytics = {
  placementRate: number;
  avgStipend: number;
  avgTimeToPlace: number;
  activeStudents: number;
  partnerCompanies: number;
  offersMade: number;
};
type Partner = { id: string; name: string; industry: string; activeEngagements: number; contactPerson: string };

function statusBadge(s: PlacementStatus) {
  const map: Record<PlacementStatus, { c: string; bg: string }> = {
    Placed: { c: "var(--green)", bg: "var(--green-soft)" },
    OfferAccepted: { c: "var(--green)", bg: "var(--green-soft)" },
    Interviewing: { c: "var(--amber)", bg: "var(--amber-soft)" },
    Searching: { c: "var(--muted)", bg: "var(--surface-soft)" },
    NotParticipating: { c: "var(--red)", bg: "var(--red-soft)" }
  };
  const v = map[s];
  return <span style={{ display: "inline-flex", padding: "3px 8px", borderRadius: 99, fontSize: 10, fontWeight: 700, background: v.bg, color: v.c }}>{s}</span>;
}

const inputS: React.CSSProperties = { minHeight: 42, padding: "0 11px", border: "1px solid var(--line)", borderRadius: 8, color: "var(--ink)", background: "var(--canvas)", fontSize: "0.85rem", width: "100%", boxSizing: "border-box" };

function money(val: number) { return new Intl.NumberFormat("en-PK", { style: "currency", currency: "PKR", maximumFractionDigits: 0 }).format(val); }

export default function PlacementPage() {
  const t = usePersonalityTranslations("academic");
  const [placements, setPlacements] = useState<Placement[]>([]);
  const [analytics, setAnalytics] = useState<Analytics | null>(null);
  const [partners, setPartners] = useState<Partner[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [readinessId, setReadinessId] = useState("");
  const [readiness, setReadiness] = useState<ReadinessResult | null>(null);
  const [checking, setChecking] = useState(false);
  const [checkError, setCheckError] = useState<string | null>(null);

  useEffect(() => {
    const ctrl = new AbortController();
    Promise.all([
      apiGet<Placement[]>("/api/placement/active", ctrl.signal).catch(() => [] as Placement[]),
      apiGet<Analytics>("/api/placement/analytics", ctrl.signal).catch(() => null as Analytics | null),
      apiGet<Partner[]>("/api/placement/partners", ctrl.signal).catch(() => [] as Partner[])
    ]).then(([pl, an, pa]) => {
      if (!ctrl.signal.aborted) { setPlacements(pl); setAnalytics(an); setPartners(pa); setLoading(false); }
    }).catch(() => { if (!ctrl.signal.aborted) { setError("Failed to load placement data"); setLoading(false); } });
    return () => ctrl.abort();
  }, []);

  async function checkReadiness(e: React.FormEvent) {
    e.preventDefault();
    setChecking(true); setCheckError(null); setReadiness(null);
    try {
      const data = await apiGet<ReadinessResult>(`/api/placement/readiness/${readinessId.trim()}`);
      setReadiness(data);
    } catch { setCheckError("Student not found or readiness data unavailable"); }
    finally { setChecking(false); }
  }

  if (loading) return <main style={{ minHeight: "100vh", display: "grid", placeItems: "center", color: "var(--muted)" }}>{t("loadingPlacement")}</main>;
  if (error) return <main style={{ minHeight: "100vh", display: "grid", placeItems: "center", color: "var(--red)" }}>{error}</main>;

  const placed = placements.filter(p => p.status === "Placed" || p.status === "OfferAccepted").length;
  const interviewing = placements.filter(p => p.status === "Interviewing").length;

  return (
    <main style={{ minHeight: "100vh", maxWidth: 1480, margin: "0 auto", padding: "42px 34px 64px", background: "var(--canvas)" }}>
      <header style={{ display: "flex", alignItems: "end", justifyContent: "space-between", gap: 20, marginBottom: 28 }}>
        <div>
          <p style={{ margin: "0 0 7px", color: "var(--blue-600)", fontSize: 11, fontWeight: 800, letterSpacing: ".8px", textTransform: "uppercase" }}>Career Services</p>
          <h1 style={{ margin: 0, fontSize: "clamp(27px, 3vw, 36px)", lineHeight: 1.15, letterSpacing: "-1px" }}>{t("placement")}</h1>
          <span style={{ display: "block", marginTop: 9, color: "var(--muted)" }}>Student readiness assessment, active placement tracking, analytics, and partner management</span>
        </div>
      </header>

      <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(180px, 1fr))", gap: 14, marginBottom: 18 }}>
        <div style={{ padding: 20, borderRadius: 14, border: "1px solid var(--line)", background: "var(--surface)", boxShadow: "var(--shadow)" }}>
          <span style={{ color: "var(--muted)", fontSize: 10, fontWeight: 700, textTransform: "uppercase", letterSpacing: ".5px" }}>{t("placementRate")}</span>
          <div style={{ fontSize: 28, fontWeight: 800, letterSpacing: "-.5px", margin: "8px 0 3px", color: analytics && analytics.placementRate >= 80 ? "var(--green)" : analytics && analytics.placementRate >= 50 ? "var(--amber)" : "var(--red)" }}>
            {analytics ? `${analytics.placementRate}%` : "\u2014"}
          </div>
          <small style={{ color: "var(--muted)" }}>{placed} {t("studentsPlaced")}</small>
        </div>
        <div style={{ padding: 20, borderRadius: 14, border: "1px solid var(--line)", background: "var(--surface)", boxShadow: "var(--shadow)" }}>
          <span style={{ color: "var(--muted)", fontSize: 10, fontWeight: 700, textTransform: "uppercase", letterSpacing: ".5px" }}>{t("avgStipend")}</span>
          <div style={{ fontSize: 28, fontWeight: 800, letterSpacing: "-.5px", margin: "8px 0 3px" }}>{analytics ? money(analytics.avgStipend) : "\u2014"}</div>
          <small style={{ color: "var(--muted)" }}>{t("perMonth")}</small>
        </div>
        <div style={{ padding: 20, borderRadius: 14, border: "1px solid var(--line)", background: "var(--surface)", boxShadow: "var(--shadow)" }}>
          <span style={{ color: "var(--muted)", fontSize: 10, fontWeight: 700, textTransform: "uppercase", letterSpacing: ".5px" }}>{t("timeToPlace")}</span>
          <div style={{ fontSize: 28, fontWeight: 800, letterSpacing: "-.5px", margin: "8px 0 3px" }}>{analytics ? `${analytics.avgTimeToPlace}d` : "\u2014"}</div>
          <small style={{ color: "var(--muted)" }}>{t("averageFromSearch")}</small>
        </div>
        <div style={{ padding: 20, borderRadius: 14, border: "1px solid var(--line)", background: "var(--surface)", boxShadow: "var(--shadow)" }}>
          <span style={{ color: "var(--muted)", fontSize: 10, fontWeight: 700, textTransform: "uppercase", letterSpacing: ".5px" }}>{t("partnerCompanies")}</span>
          <div style={{ fontSize: 28, fontWeight: 800, letterSpacing: "-.5px", margin: "8px 0 3px", color: "var(--blue-600)" }}>{analytics?.partnerCompanies ?? "\u2014"}</div>
          <small style={{ color: "var(--muted)" }}>{t("activeEngagements")}</small>
        </div>
        <div style={{ padding: 20, borderRadius: 14, border: "1px solid var(--line)", background: "var(--surface)", boxShadow: "var(--shadow)" }}>
          <span style={{ color: "var(--muted)", fontSize: 10, fontWeight: 700, textTransform: "uppercase", letterSpacing: ".5px" }}>{t("offersMade")}</span>
          <div style={{ fontSize: 28, fontWeight: 800, letterSpacing: "-.5px", margin: "8px 0 3px", color: analytics && analytics.offersMade > 0 ? "var(--green)" : "var(--muted)" }}>{analytics?.offersMade ?? "\u2014"}</div>
          <small style={{ color: "var(--muted)" }}>{interviewing} {t("inPipeline")}</small>
        </div>
      </div>

      <section style={{ display: "grid", gridTemplateColumns: "minmax(0, 1fr) minmax(380px, 0.75fr)", gap: 18, marginBottom: 18 }}>
        <div className="surface" style={{ overflow: "hidden" }}>
          <div className="surface-heading"><div><h2>{t("studentReadiness")}</h2><p>Evaluate employability skills and generate improvement recommendations</p></div><FileCheck2 /></div>
          <form onSubmit={checkReadiness} style={{ padding: 18 }}>
            <div style={{ display: "flex", gap: 10 }}>
              <input placeholder="Enter student ID..." value={readinessId} onChange={e => setReadinessId(e.target.value)} style={inputS} required />
              <button type="submit" disabled={checking} style={{ minHeight: 42, padding: "0 20px", border: "none", borderRadius: 8, background: "var(--blue-600)", color: "#fff", cursor: "pointer", fontWeight: 700, fontSize: 12, whiteSpace: "nowrap", opacity: checking ? 0.6 : 1 }}>
                {checking ? "Analyzing..." : t("checkReadiness")}
              </button>
            </div>
            {checkError && <div style={{ marginTop: 10, padding: "10px 14px", borderRadius: 8, border: "1px solid #ecc0c6", color: "var(--red)", background: "var(--red-soft)", fontSize: 11, display: "flex", alignItems: "center", gap: 6 }}><AlertCircle style={{ width: 15 }} />{checkError}</div>}
          </form>
          {readiness && (
            <div style={{ padding: "0 18px 18px" }}>
              <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", padding: 12, borderRadius: 8, border: "1px solid var(--line)", background: "var(--surface-soft)", marginBottom: 14 }}>
                <div>
                  <strong style={{ fontSize: 13 }}>{readiness.studentName}</strong>
                  <div style={{ color: "var(--muted)", fontSize: 10 }}>{readiness.studentId}</div>
                </div>
                <div style={{ textAlign: "right" }}>
                  <div style={{ fontSize: 26, fontWeight: 800, letterSpacing: "-1px", color: readiness.overallScore >= 80 ? "var(--green)" : readiness.overallScore >= 50 ? "var(--amber)" : "var(--red)" }}>{readiness.overallScore}%</div>
                  <div style={{ fontSize: 10, color: "var(--muted)" }}>{t("readinessScore")}</div>
                </div>
              </div>
              <h4 style={{ fontSize: 10, color: "var(--muted)", textTransform: "uppercase", letterSpacing: ".5px", margin: "0 0 8px" }}>{t("skillAssessment")}</h4>
              {readiness.skills.map((s, i) => (
                <div key={i} style={{ display: "grid", gridTemplateColumns: "120px 1fr 40px", gap: 10, alignItems: "center", marginBottom: 6 }}>
                  <span style={{ fontSize: 11, fontWeight: 600 }}>{s.skill}</span>
                  <div style={{ height: 6, borderRadius: 99, background: "var(--surface-soft)", overflow: "hidden" }}>
                    <div style={{ height: "100%", width: `${s.score}%`, borderRadius: 99, background: s.score >= 80 ? "var(--green)" : s.score >= 50 ? "var(--amber)" : "var(--red)" }} />
                  </div>
                  <span style={{ fontSize: 10, fontWeight: 700, textAlign: "right" }}>{s.score}</span>
                </div>
              ))}
              {readiness.recommendations.length > 0 && (
                <div style={{ marginTop: 12 }}>
                  <h4 style={{ fontSize: 10, color: "var(--muted)", textTransform: "uppercase", letterSpacing: ".5px", margin: "0 0 6px" }}>{t("recommendations")}</h4>
                  {readiness.recommendations.map((r, i) => (
                    <div key={i} style={{ fontSize: 11, padding: "6px 0", borderTop: "1px solid var(--line)", display: "flex", alignItems: "center", gap: 6 }}>
                      <TrendingUp style={{ width: 14, color: "var(--blue-600)" }} /> {r}
                    </div>
                  ))}
                </div>
              )}
            </div>
          )}
        </div>

        <div className="surface" style={{ overflow: "hidden" }}>
          <div className="surface-heading"><div><h2>{t("partnerManagement")}</h2><p>{partners.length} industry partners</p></div><Building2 /></div>
          <div style={{ padding: 18, maxHeight: 400, overflow: "auto" }}>
            {partners.length === 0 ? (
              <div style={{ minHeight: 120, display: "grid", placeItems: "center", alignContent: "center", gap: 8, color: "var(--muted)" }}>
                <Globe style={{ width: 24, opacity: 0.5 }} />
                <span style={{ fontSize: 11 }}>{t("noPartnerCompanies")}</span>
              </div>
            ) : (
              partners.map(p => (
                <div key={p.id} style={{ padding: "12px 0", borderBottom: "1px solid var(--line)", display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                  <div style={{ display: "flex", gap: 10, alignItems: "center" }}>
                    <div style={{ width: 34, height: 34, borderRadius: 8, display: "grid", placeItems: "center", background: "var(--blue-100)", color: "var(--blue-600)", fontSize: 13, fontWeight: 700 }}>{p.name.charAt(0)}</div>
                    <div style={{ display: "grid" }}>
                      <strong style={{ fontSize: 12 }}>{p.name}</strong>
                      <small style={{ color: "var(--muted)", fontSize: 9 }}>{p.industry} · {p.contactPerson}</small>
                    </div>
                  </div>
                  <span style={{ padding: "3px 8px", borderRadius: 99, fontSize: 10, fontWeight: 700, background: p.activeEngagements > 0 ? "var(--green-soft)" : "var(--surface-soft)", color: p.activeEngagements > 0 ? "var(--green)" : "var(--muted)" }}>
                    {p.activeEngagements} active
                  </span>
                </div>
              ))
            )}
          </div>
        </div>
      </section>

      <section className="surface" style={{ overflow: "hidden" }}>
        <div className="surface-heading"><div><h2>{t("activePlacements")}</h2><p>{placements.length} student placements tracked</p></div><GraduationCap /></div>
        {placements.length === 0 ? (
          <div style={{ minHeight: 200, display: "grid", placeItems: "center", alignContent: "center", gap: 8, padding: 30, color: "var(--muted)", textAlign: "center" }}>
            <UserPlus style={{ width: 28, color: "var(--green)" }} />
            <strong>{t("noActivePlacements")}</strong>
            <span>{t("noActivePlacementsDesc")}</span>
          </div>
        ) : (
          <div style={{ overflow: "auto" }}>
            <table style={{ width: "100%", minWidth: 850, borderCollapse: "collapse" }}>
              <thead>
                <tr style={{ background: "var(--surface-soft)" }}>
                  {["Student", "Company", "Role", "Stipend", "Status", "Start Date", "Duration"].map(h => (
                    <th key={h} style={{ padding: "10px 14px", color: "var(--muted)", fontSize: 9, fontWeight: 800, textTransform: "uppercase", textAlign: "left", borderBottom: "1px solid var(--line)" }}>{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {placements.map(p => (
                  <tr key={p.id} style={{ borderBottom: "1px solid var(--line)" }}>
                    <td style={{ padding: "10px 14px", fontSize: 11, fontWeight: 600 }}>{p.studentName}</td>
                    <td style={{ padding: "10px 14px", fontSize: 11 }}>{p.company}</td>
                    <td style={{ padding: "10px 14px", fontSize: 11, color: "var(--muted)" }}>{p.role}</td>
                    <td style={{ padding: "10px 14px", fontSize: 11, fontFamily: "monospace" }}>{money(p.stipend)}</td>
                    <td style={{ padding: "10px 14px" }}>{statusBadge(p.status)}</td>
                    <td style={{ padding: "10px 14px", fontSize: 10, color: "var(--muted)", whiteSpace: "nowrap" }}>{new Date(p.startDate).toLocaleDateString()}</td>
                    <td style={{ padding: "10px 14px", fontSize: 11, textAlign: "center" }}>{p.durationMonths}mo</td>
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
