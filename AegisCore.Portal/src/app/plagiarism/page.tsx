"use client";

import { useState } from "react";
import { apiPost } from "@/lib/api-client";
import { AlertCircle, FileSearch, FileText, Gauge, Layers, ScanEye, ShieldAlert, Zap } from "lucide-react";
import { usePersonalityTranslations } from "@/lib/personality-translator";

type MatchTab = "verbatim" | "paraphrase" | "structural" | "ai";
type MatchBreakdown = { type: MatchTab; label: string; percent: number; count: number };
type ScanResult = {
  documentId: string;
  overallSimilarity: number;
  breakdown: MatchBreakdown[];
  sources: { title: string; url: string; matchPercent: number }[];
  collusionGroup: { studentName: string; similarity: number }[] | null;
};

const tabLabels: Record<MatchTab, string> = { verbatim: "Verbatim", paraphrase: "Paraphrase", structural: "Structural", ai: "AI-Generated" };
const tabIcons: Record<MatchTab, typeof FileSearch> = { verbatim: FileText, paraphrase: Layers, structural: ScanEye, ai: Zap };

const inputS: React.CSSProperties = { padding: "0.6rem 0.75rem", borderRadius: 8, border: "1px solid var(--line)", background: "var(--canvas)", color: "var(--ink)", fontSize: "0.82rem", width: "100%", boxSizing: "border-box", resize: "vertical" };

export default function PlagiarismPage() {
  const t = usePersonalityTranslations("academic");
  const [text, setText] = useState("");
  const [scanning, setScanning] = useState(false);
  const [result, setResult] = useState<ScanResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<MatchTab>("verbatim");

  async function scan() {
    if (!text.trim()) return;
    setScanning(true); setError(null); setResult(null);
    try {
      const data = await apiPost<ScanResult>("/api/plagiarism/scan", { content: text.trim() });
      setResult(data);
    } catch { setError("Scan failed. Please try again."); }
    finally { setScanning(false); }
  }

  const gaugeAngle = result ? Math.min(360, (result.overallSimilarity / 100) * 270) : 0;
  const gaugeColor = result
    ? result.overallSimilarity <= 15 ? "var(--green)" : result.overallSimilarity <= 40 ? "var(--amber)" : "var(--red)"
    : "var(--line)";
  const gaugeLabel = result
    ? result.overallSimilarity <= 15 ? t("lowRisk") : result.overallSimilarity <= 40 ? t("moderate") : t("highRisk")
    : "\u2014";

  return (
    <main style={{ minHeight: "100vh", maxWidth: 1480, margin: "0 auto", padding: "42px 34px 64px", background: "var(--canvas)" }}>
      <header style={{ display: "flex", alignItems: "end", justifyContent: "space-between", gap: 20, marginBottom: 28 }}>
        <div>
          <p style={{ margin: "0 0 7px", color: "var(--blue-600)", fontSize: 11, fontWeight: 800, letterSpacing: ".8px", textTransform: "uppercase" }}>Academic Integrity</p>
          <h1 style={{ margin: 0, fontSize: "clamp(27px, 3vw, 36px)", lineHeight: 1.15, letterSpacing: "-1px" }}>{t("plagiarism")}</h1>
          <span style={{ display: "block", marginTop: 9, color: "var(--muted)" }}>{t("gradeAppealsDesc")}</span>
        </div>
      </header>

      <section style={{ display: "grid", gridTemplateColumns: "minmax(0, 1.5fr) minmax(340px, 0.8fr)", gap: 18, marginBottom: 18 }}>
        <div className="surface" style={{ overflow: "hidden" }}>
          <div className="surface-heading"><div><h2>{t("documentScanner")}</h2><p>Paste text to check for plagiarism across all dimensions</p></div><ScanEye /></div>
          <div style={{ padding: 18 }}>
            <textarea
              placeholder="Paste the assignment or document text here for analysis..."
              value={text}
              onChange={e => setText(e.target.value)}
              rows={10}
              style={inputS}
            />
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginTop: 10 }}>
              <span style={{ color: "var(--muted)", fontSize: 10 }}>{text.length} characters · ~{Math.round(text.split(/\s+/).filter(Boolean).length)} words</span>
              <button onClick={scan} disabled={scanning || !text.trim()} style={{ padding: "0.65rem 1.4rem", background: "var(--blue-600)", color: "#fff", border: "none", borderRadius: 8, cursor: "pointer", fontWeight: 700, fontSize: 12, display: "flex", alignItems: "center", gap: 6, opacity: !text.trim() ? 0.5 : 1 }}>
                <FileSearch style={{ width: 15 }} /> {scanning ? t("scanning") : t("scanDocument")}
              </button>
            </div>
            {error && <div style={{ marginTop: 10, padding: "10px 14px", borderRadius: 8, border: "1px solid #ecc0c6", color: "var(--red)", background: "var(--red-soft)", fontSize: 11, display: "flex", alignItems: "center", gap: 6 }}><AlertCircle style={{ width: 15 }} />{error}</div>}
          </div>
        </div>

        <div className="surface" style={{ overflow: "hidden", display: "grid", alignContent: "center", justifyItems: "center", padding: 36 }}>
          <h3 style={{ margin: "0 0 12px", fontSize: 13, color: "var(--ink)" }}>{t("similarityScore")}</h3>
          <svg viewBox="0 0 180 140" style={{ width: 180, height: 140 }}>
            <defs>
              <linearGradient id="gaugeGrad" x1="0" y1="0" x2="1" y2="0">
                <stop offset="0%" stopColor="var(--green)" />
                <stop offset="40%" stopColor="var(--amber)" />
                <stop offset="100%" stopColor="var(--red)" />
              </linearGradient>
            </defs>
            <g transform="translate(90, 100)">
              <path d={describeArc(0, 0, 64, 135, 405)} fill="none" stroke="var(--surface-soft)" strokeWidth={14} strokeLinecap="round" />
              <path d={describeArc(0, 0, 64, 135, 135 + gaugeAngle)} fill="none" stroke={gaugeColor} strokeWidth={14} strokeLinecap="round" style={{ transition: "stroke-dashoffset .6s ease, stroke .6s ease" }} />
              <text textAnchor="middle" y="8" style={{ fontSize: 30, fontWeight: 800, fill: "var(--ink)", letterSpacing: "-1px" }}>{result ? `${result.overallSimilarity}%` : "\u2014"}</text>
              <text textAnchor="middle" y="26" style={{ fontSize: 10, fill: gaugeColor, fontWeight: 700 }}>{gaugeLabel}</text>
            </g>
          </svg>
          <div style={{ display: "flex", justifyContent: "space-between", width: 200, marginTop: 4 }}>
            <span style={{ fontSize: 9, color: "var(--green)" }}>0%</span>
            <span style={{ fontSize: 9, color: "var(--amber)" }}>50%</span>
            <span style={{ fontSize: 9, color: "var(--red)" }}>100%</span>
          </div>
        </div>
      </section>

      {result && (
        <>
          <section style={{ display: "grid", gridTemplateColumns: "minmax(0, 1.5fr) minmax(340px, 0.8fr)", gap: 18, marginBottom: 18 }}>
            <div className="surface" style={{ overflow: "hidden" }}>
              <div style={{ display: "flex", borderBottom: "1px solid var(--line)" }}>
                {(Object.keys(tabLabels) as MatchTab[]).map(tab => {
                  const Icon = tabIcons[tab];
                  const bd = result.breakdown.find(b => b.type === tab);
                  return (
                    <button key={tab} onClick={() => setActiveTab(tab)}
                      style={{
                        flex: 1, minHeight: 52, display: "flex", alignItems: "center", justifyContent: "center", gap: 8,
                        border: "none", background: activeTab === tab ? "var(--surface-soft)" : "transparent",
                        color: activeTab === tab ? "var(--ink)" : "var(--muted)", cursor: "pointer",
                        borderBottom: activeTab === tab ? "2px solid var(--blue-600)" : "2px solid transparent",
                        fontWeight: 700, fontSize: 12, padding: "0 12px"
                      }}>
                      <Icon style={{ width: 15 }} /> {tabLabels[tab]}
                      {bd && <span style={{ padding: "2px 6px", borderRadius: 99, fontSize: 10, background: bd.percent > 40 ? "var(--red-soft)" : bd.percent > 15 ? "var(--amber-soft)" : "var(--green-soft)", color: bd.percent > 40 ? "var(--red)" : bd.percent > 15 ? "var(--amber)" : "var(--green)", fontWeight: 700 }}>{bd.percent}%</span>}
                    </button>
                  );
                })}
              </div>
              <div style={{ padding: 18, minHeight: 180 }}>
                {result.breakdown.filter(b => b.type === activeTab).map(b => (
                  <div key={b.type}>
                    <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 12 }}>
                      <span style={{ fontWeight: 600, fontSize: 14 }}>{b.label} Matches</span>
                      <span style={{ fontSize: 11, color: "var(--muted)" }}>{b.count} segments found</span>
                    </div>
                    <div style={{ height: 10, borderRadius: 99, background: "var(--surface-soft)", overflow: "hidden", marginBottom: 16 }}>
                      <div style={{ height: "100%", width: `${b.percent}%`, borderRadius: 99, background: b.percent > 40 ? "var(--red)" : b.percent > 15 ? "var(--amber)" : "var(--green)", transition: "width .4s ease" }} />
                    </div>
                    {activeTab === "ai" && b.percent > 10 && (
                      <div style={{ padding: "12px 14px", borderRadius: 8, border: "1px solid var(--line)", background: "var(--amber-soft)", display: "flex", alignItems: "center", gap: 8, fontSize: 11 }}>
                        <ShieldAlert style={{ width: 16, color: "var(--amber)" }} />
                        <span>{t("aiWarning")}</span>
                      </div>
                    )}
                    {activeTab === "verbatim" && result.sources.length > 0 && (
                      <div style={{ marginTop: 8 }}>
                        <h4 style={{ fontSize: 11, color: "var(--muted)", margin: "0 0 8px", textTransform: "uppercase", letterSpacing: ".5px" }}>{t("matchedSources")}</h4>
                        {result.sources.map((s, i) => (
                          <div key={i} style={{ display: "flex", justifyContent: "space-between", alignItems: "center", padding: "8px 0", borderTop: "1px solid var(--line)", fontSize: 11 }}>
                            <span style={{ maxWidth: 300, overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>{s.title}</span>
                            <span style={{ padding: "2px 7px", borderRadius: 99, fontSize: 10, fontWeight: 700, background: s.matchPercent > 40 ? "var(--red-soft)" : "var(--amber-soft)", color: s.matchPercent > 40 ? "var(--red)" : "var(--amber)" }}>{s.matchPercent}%</span>
                          </div>
                        ))}
                      </div>
                    )}
                  </div>
                ))}
              </div>
            </div>

            <div className="surface" style={{ overflow: "hidden" }}>
              <div className="surface-heading"><div><h2>{t("collusionGraph")}</h2><p>Student-to-student similarity mapping</p></div></div>
              {result.collusionGroup && result.collusionGroup.length > 0 ? (
                <div style={{ padding: 18 }}>
                  {result.collusionGroup.map((c, i) => (
                    <div key={i} style={{ display: "flex", justifyContent: "space-between", alignItems: "center", padding: "10px 0", borderBottom: "1px solid var(--line)" }}>
                      <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
                        <div style={{ width: 28, height: 28, borderRadius: "50%", background: "var(--blue-100)", color: "var(--blue-600)", display: "grid", placeItems: "center", fontSize: 11, fontWeight: 800 }}>{c.studentName.charAt(0)}</div>
                        <span style={{ fontSize: 12, fontWeight: 600 }}>{c.studentName}</span>
                      </div>
                      <span style={{ padding: "3px 8px", borderRadius: 99, fontSize: 10, fontWeight: 700, background: c.similarity > 60 ? "var(--red-soft)" : c.similarity > 30 ? "var(--amber-soft)" : "var(--green-soft)", color: c.similarity > 60 ? "var(--red)" : c.similarity > 30 ? "var(--amber)" : "var(--green)" }}>{c.similarity}% similarity</span>
                    </div>
                  ))}
                </div>
              ) : (
                <div style={{ minHeight: 160, display: "grid", placeItems: "center", alignContent: "center", gap: 8, padding: 30, color: "var(--muted)", textAlign: "center" }}>
                  <Gauge style={{ width: 28, opacity: 0.5 }} />
                  <span>{t("noCollusionPatterns")}</span>
                </div>
              )}
            </div>
          </section>
        </>
      )}
    </main>
  );
}

function polarToCartesian(cx: number, cy: number, r: number, degrees: number) {
  const rad = (degrees - 90) * (Math.PI / 180);
  return { x: cx + r * Math.cos(rad), y: cy + r * Math.sin(rad) };
}

function describeArc(cx: number, cy: number, r: number, startAngle: number, endAngle: number) {
  const start = polarToCartesian(cx, cy, r, endAngle);
  const end = polarToCartesian(cx, cy, r, startAngle);
  const largeArc = endAngle - startAngle <= 180 ? "0" : "1";
  return `M ${start.x} ${start.y} A ${r} ${r} 0 ${largeArc} 0 ${end.x} ${end.y}`;
}
