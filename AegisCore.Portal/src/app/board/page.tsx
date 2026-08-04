"use client";

import { useEffect, useState } from "react";
import { apiGet, apiPost } from "@/lib/api-client";
import { useTranslations } from "next-intl";
import {
  AlertCircle, BarChart3, Calendar, CheckCircle2, ClipboardList, Download,
  FileText, Gauge, LayoutDashboard, RefreshCw, Send, ShieldCheck, TrendingUp,
  Users, XCircle, Zap
} from "lucide-react";

type Grade = "A" | "B" | "C" | "D" | "F";

type MetricCard = {
  id: string; title: string; score: number; letterGrade: Grade;
  trend: "up" | "down" | "stable"; description: string;
};

type StrategicGoal = {
  id: string; title: string; description: string;
  percentAchieved: number; status: "OnTrack" | "AtRisk" | "OffTrack";
};

type BoardAlert = {
  id: string; title: string; description: string;
  category: "Financial" | "Academic" | "HR" | "Compliance" | "Reputational";
  severity: "Low" | "Medium" | "High" | "Critical"; date: string;
};

type BenchmarkFactor = {
  name: string; institutionValue: number; nationalAverage: number;
  peerAverage: number; unit: string;
};

const gradeColor = (g: Grade) => {
  const map: Record<Grade, string> = { A: "var(--green)", B: "var(--blue-600)", C: "var(--amber)", D: "var(--muted)", F: "var(--red)" };
  return map[g];
};

const gradeBg = (g: Grade) => {
  const map: Record<Grade, string> = { A: "var(--green-soft)", B: "var(--blue-100)", C: "var(--amber-soft)", D: "var(--surface-soft)", F: "var(--red-soft)" };
  return map[g];
};

const goalStatusBadge = (s: StrategicGoal["status"]) => {
  const map: Record<StrategicGoal["status"], { c: string; bg: string }> = {
    OnTrack: { c: "var(--green)", bg: "var(--green-soft)" },
    AtRisk: { c: "var(--amber)", bg: "var(--amber-soft)" },
    OffTrack: { c: "var(--red)", bg: "var(--red-soft)" }
  };
  const v = map[s];
  return <span style={{ display: "inline-flex", padding: "3px 8px", borderRadius: 99, fontSize: 10, fontWeight: 700, background: v.bg, color: v.c }}>{s}</span>;
};

const alertCategoryBadge = (c: BoardAlert["category"]) => {
  const map: Record<BoardAlert["category"], { c: string; bg: string }> = {
    Financial: { c: "var(--amber)", bg: "var(--amber-soft)" },
    Academic: { c: "var(--blue-600)", bg: "var(--blue-100)" },
    HR: { c: "var(--green)", bg: "var(--green-soft)" },
    Compliance: { c: "var(--red)", bg: "var(--red-soft)" },
    Reputational: { c: "var(--red)", bg: "var(--red-soft)" }
  };
  const v = map[c];
  return <span style={{ display: "inline-flex", padding: "3px 8px", borderRadius: 99, fontSize: 10, fontWeight: 700, background: v.bg, color: v.c }}>{c}</span>;
};

const inputS: React.CSSProperties = { minHeight: 42, padding: "0 11px", border: "1px solid var(--line)", borderRadius: 8, color: "var(--ink)", background: "var(--canvas)", fontSize: "0.85rem", width: "100%", boxSizing: "border-box" };
const labelS: React.CSSProperties = { display: "grid", gap: 5, color: "var(--muted)", fontSize: 10, fontWeight: 700 };

export default function BoardPage() {
  const t = useTranslations("academic");

  const [metrics, setMetrics] = useState<MetricCard[]>([]);
  const [goals, setGoals] = useState<StrategicGoal[]>([]);
  const [alerts, setAlerts] = useState<BoardAlert[]>([]);
  const [benchmarks, setBenchmarks] = useState<BenchmarkFactor[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const [generatingDash, setGeneratingDash] = useState(false);
  const [boardTitle, setBoardTitle] = useState("");
  const [boardDate, setBoardDate] = useState("");
  const [generatingPack, setGeneratingPack] = useState(false);

  useEffect(() => {
    const ctrl = new AbortController();
    const fetch = async () => {
      try {
        const [m, g, a, b] = await Promise.all([
          apiGet<MetricCard[]>("/api/board/metrics", ctrl.signal).catch(() => [] as MetricCard[]),
          apiGet<StrategicGoal[]>("/api/board/strategic-goals", ctrl.signal).catch(() => [] as StrategicGoal[]),
          apiGet<BoardAlert[]>("/api/board/alerts", ctrl.signal).catch(() => [] as BoardAlert[]),
          apiGet<BenchmarkFactor[]>("/api/board/benchmarks", ctrl.signal).catch(() => [] as BenchmarkFactor[])
        ]);
        setMetrics(m); setGoals(g); setAlerts(a); setBenchmarks(b);
      } catch { setError("Failed to load board data"); }
      finally { setLoading(false); }
    };
    fetch();
    return () => { ctrl.abort(); };
  }, []);

  async function generateDashboard() {
    setGeneratingDash(true); setMessage(null);
    try {
      await apiPost("/api/board/dashboard?userRole=chancellor&fiscalYear=2026");
      setMessage("success: Dashboard generated for FY2026");
      const ctrl = new AbortController();
      const [m, g, a, b] = await Promise.all([
        apiGet<MetricCard[]>("/api/board/metrics", ctrl.signal),
        apiGet<StrategicGoal[]>("/api/board/strategic-goals", ctrl.signal),
        apiGet<BoardAlert[]>("/api/board/alerts", ctrl.signal),
        apiGet<BenchmarkFactor[]>("/api/board/benchmarks", ctrl.signal)
      ]);
      setMetrics(m); setGoals(g); setAlerts(a); setBenchmarks(b);
    } catch { setMessage("error: Failed to generate dashboard"); }
    finally { setGeneratingDash(false); }
  }

  async function generateBoardPack(e: React.FormEvent) {
    e.preventDefault();
    if (!boardTitle || !boardDate) return;
    setGeneratingPack(true); setMessage(null);
    try {
      await apiPost("/api/board/board-pack", { title: boardTitle, meetingDate: boardDate });
      setMessage("success: Board pack generated successfully");
      setBoardTitle(""); setBoardDate("");
    } catch { setMessage("error: Failed to generate board pack"); }
    finally { setGeneratingPack(false); }
  }

  if (loading) return (
    <main style={{ minHeight: "100vh", display: "grid", placeItems: "center", gap: 16, color: "var(--muted)" }}>
      <div style={{ width: 40, height: 4, borderRadius: 99, overflow: "hidden", background: "var(--line)" }}>
        <div style={{ width: "50%", height: "100%", borderRadius: "inherit", background: "var(--blue-500)", animation: "launch 1s ease-in-out infinite alternate" }} />
      </div>
      Loading Board of Governors portal...
    </main>
  );

  if (error) return (
    <main style={{ minHeight: "100vh", display: "grid", placeItems: "center", alignContent: "center", gap: 12, color: "var(--red)" }}>
      <AlertCircle style={{ width: 32 }} />
      <strong>{error}</strong>
      <button onClick={() => window.location.reload()} style={{ minHeight: 40, padding: "0 18px", border: "1px solid var(--line)", borderRadius: 8, color: "var(--blue-600)", background: "var(--surface)", cursor: "pointer", fontWeight: 700 }}>Retry</button>
    </main>
  );

  return (
    <main style={{ minHeight: "100vh", maxWidth: 1480, margin: "0 auto", padding: "42px 34px 64px", background: "var(--canvas)" }}>
      <header style={{ display: "flex", alignItems: "end", justifyContent: "space-between", gap: 20, marginBottom: 28 }}>
        <div>
          <p style={{ margin: "0 0 7px", color: "var(--blue-600)", fontSize: 11, fontWeight: 800, letterSpacing: ".8px", textTransform: "uppercase" }}>Executive Governance</p>
          <h1 style={{ margin: 0, fontSize: "clamp(27px, 3vw, 36px)", lineHeight: 1.15, letterSpacing: "-1px" }}>{t("board") ?? "Board of Governors"}</h1>
          <span style={{ display: "block", marginTop: 9, color: "var(--muted)" }}>Strategic dashboards, board meeting packs, benchmark comparisons, and alert feeds</span>
        </div>
        <button onClick={generateDashboard} disabled={generatingDash}
          style={{ minHeight: 42, padding: "0 16px", border: "1px solid var(--blue-600)", borderRadius: 8, background: "var(--blue-600)", color: "#fff", cursor: "pointer", fontWeight: 700, fontSize: 11, display: "flex", alignItems: "center", gap: 7, opacity: generatingDash ? 0.6 : 1 }}>
          <LayoutDashboard style={{ width: 14 }} /> {generatingDash ? "Generating..." : "Generate Dashboard"}
        </button>
      </header>

      {message && (
        <div style={{ display: "flex", alignItems: "center", gap: 9, padding: "11px 14px", marginBottom: 18, border: message.startsWith("error") ? "1px solid #ecc0c6" : "1px solid #aaddc9", borderRadius: 10, color: message.startsWith("error") ? "var(--red)" : "var(--green)", background: message.startsWith("error") ? "var(--red-soft)" : "var(--green-soft)", fontSize: 11 }}>
          {message.startsWith("error") ? <AlertCircle /> : <CheckCircle2 />}{message.replace(/^[^:]+: /, "")}
        </div>
      )}

      <section style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(200px, 1fr))", gap: 14, marginBottom: 18 }}>
        {metrics.map(m => (
          <article key={m.id} style={{ minHeight: 150, display: "grid", alignContent: "center", padding: 20, border: "1px solid var(--line)", borderRadius: 14, background: "var(--surface)", boxShadow: "var(--shadow)" }}>
            <span style={{ color: "var(--muted)", fontSize: 11, fontWeight: 700, textTransform: "uppercase" }}>{m.title}</span>
            <div style={{ display: "flex", alignItems: "end", gap: 10, margin: "7px 0" }}>
              <strong style={{ fontSize: 28 }}>{m.score}%</strong>
              <span style={{ display: "inline-flex", width: 38, height: 38, alignItems: "center", justifyContent: "center", borderRadius: 10, fontSize: 16, fontWeight: 800, color: gradeColor(m.letterGrade), background: gradeBg(m.letterGrade) }}>{m.letterGrade}</span>
            </div>
            <small style={{ color: "var(--muted)", fontSize: 10, display: "flex", alignItems: "center", gap: 4 }}>
              {m.trend === "up" ? <TrendingUp style={{ width: 14, color: "var(--green)" }} /> : m.trend === "down" ? <TrendingUp style={{ width: 14, color: "var(--red)", transform: "rotate(180deg)" }} /> : <Gauge style={{ width: 14 }} />}
              {m.description}
            </small>
          </article>
        ))}
      </section>

      <div style={{ display: "grid", gridTemplateColumns: "minmax(0, 1.2fr) minmax(0, 0.9fr)", gap: 18, marginBottom: 18 }}>
        <div className="surface" style={{ overflow: "hidden" }}>
          <div className="surface-heading"><div><h2>Strategic Plan Goals</h2><p>{goals.length} objectives being tracked</p></div><ShieldCheck /></div>
          <div style={{ padding: 18, display: "grid", gap: 12 }}>
            {goals.map(g => (
              <div key={g.id} style={{ padding: 14, borderRadius: 10, border: "1px solid var(--line)", background: "var(--surface-soft)" }}>
                <div style={{ display: "flex", justifyContent: "space-between", alignItems: "start", gap: 8, marginBottom: 8 }}>
                  <div>
                    <strong style={{ fontSize: 12 }}>{g.title}</strong>
                    <small style={{ display: "block", marginTop: 3, color: "var(--muted)", fontSize: 10 }}>{g.description}</small>
                  </div>
                  {goalStatusBadge(g.status)}
                </div>
                <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
                  <div style={{ flex: 1, height: 6, borderRadius: 99, overflow: "hidden", background: "var(--line)" }}>
                    <div style={{ width: `${g.percentAchieved}%`, height: "100%", borderRadius: 99, background: g.status === "OffTrack" ? "var(--red)" : g.status === "AtRisk" ? "var(--amber)" : "var(--green)", transition: "width .4s" }} />
                  </div>
                  <span style={{ fontSize: 10, fontWeight: 700, minWidth: 38, textAlign: "right" }}>{g.percentAchieved}%</span>
                </div>
              </div>
            ))}
          </div>
        </div>

        <div className="surface" style={{ overflow: "hidden" }}>
          <div className="surface-heading"><div><h2>Board Meeting Pack</h2><p>Generate comprehensive board meeting documentation</p></div><FileText /></div>
          <form onSubmit={generateBoardPack} style={{ display: "grid", gap: 12, padding: 18 }}>
            <label style={labelS}>
              Meeting Title
              <input placeholder="e.g. Q3 Strategic Review" value={boardTitle} onChange={e => setBoardTitle(e.target.value)} style={inputS} required />
            </label>
            <label style={labelS}>
              Meeting Date
              <input type="date" value={boardDate} onChange={e => setBoardDate(e.target.value)} style={inputS} required />
            </label>
            <button type="submit" disabled={generatingPack}
              style={{ padding: "0.7rem", background: "var(--blue-600)", color: "#fff", border: "none", borderRadius: 8, cursor: "pointer", fontWeight: 700, fontSize: 12, display: "flex", alignItems: "center", justifyContent: "center", gap: 7, opacity: generatingPack ? 0.6 : 1 }}>
              <Download style={{ width: 14 }} /> {generatingPack ? "Generating Pack..." : "Generate Board Pack"}
            </button>
          </form>
        </div>
      </div>

      <div style={{ display: "grid", gridTemplateColumns: "minmax(0, 1fr) minmax(0, 1fr)", gap: 18, marginBottom: 18 }}>
        <div className="surface" style={{ overflow: "hidden" }}>
          <div className="surface-heading"><div><h2>Alerts Feed</h2><p>{alerts.length} active alerts</p></div><Zap /></div>
          <div style={{ padding: 12, maxHeight: 420, overflow: "auto" }}>
            {alerts.map(a => (
              <div key={a.id} style={{ display: "flex", alignItems: "start", gap: 10, padding: "12px 8px", borderBottom: "1px solid var(--line)" }}>
                <div style={{ minWidth: 8, height: 8, marginTop: 5, borderRadius: "50%", background: a.severity === "Critical" ? "var(--red)" : a.severity === "High" ? "var(--amber)" : a.severity === "Medium" ? "var(--blue-600)" : "var(--muted)" }} />
                <div style={{ flex: 1, minWidth: 0 }}>
                  <div style={{ display: "flex", alignItems: "center", gap: 6, marginBottom: 3 }}>
                    <strong style={{ fontSize: 11 }}>{a.title}</strong>
                    {alertCategoryBadge(a.category)}
                  </div>
                  <small style={{ display: "block", color: "var(--muted)", fontSize: 10 }}>{a.description}</small>
                  <small style={{ display: "block", marginTop: 3, color: "var(--subtle)", fontSize: 9 }}>{new Date(a.date).toLocaleDateString()}</small>
                </div>
              </div>
            ))}
          </div>
        </div>

        <div className="surface" style={{ overflow: "hidden" }}>
          <div className="surface-heading"><div><h2>Benchmark Comparison</h2><p>Institutional performance against peers</p></div><BarChart3 /></div>
          <div style={{ overflow: "auto" }}>
            <table style={{ width: "100%", minWidth: 500, borderCollapse: "collapse" }}>
              <thead>
                <tr style={{ background: "var(--surface-soft)" }}>
                  {["Factor", "Institution", "National Avg", "Peer Avg", "Status"].map(h => (
                    <th key={h} style={{ padding: "10px 14px", color: "var(--muted)", fontSize: 9, fontWeight: 800, textTransform: "uppercase", textAlign: "left", borderBottom: "1px solid var(--line)" }}>{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {benchmarks.map(b => {
                  const aboveNat = b.institutionValue >= b.nationalAverage;
                  const abovePeer = b.institutionValue >= b.peerAverage;
                  return (
                    <tr key={b.name} style={{ borderBottom: "1px solid var(--line)" }}>
                      <td style={{ padding: "10px 14px", fontSize: 11, fontWeight: 600 }}>{b.name}</td>
                      <td style={{ padding: "10px 14px", fontSize: 11 }}>{b.institutionValue}{b.unit}</td>
                      <td style={{ padding: "10px 14px", fontSize: 11, color: "var(--muted)" }}>{b.nationalAverage}{b.unit}</td>
                      <td style={{ padding: "10px 14px", fontSize: 11, color: "var(--muted)" }}>{b.peerAverage}{b.unit}</td>
                      <td style={{ padding: "10px 14px" }}>
                        <span style={{ display: "inline-flex", alignItems: "center", gap: 4, fontSize: 10, fontWeight: 700, color: abovePeer ? "var(--green)" : "var(--red)" }}>
                          {abovePeer ? <TrendingUp style={{ width: 12 }} /> : <TrendingUp style={{ width: 12, transform: "rotate(180deg)" }} />}
                          {abovePeer ? "Above" : "Below"}
                        </span>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </main>
  );
}
