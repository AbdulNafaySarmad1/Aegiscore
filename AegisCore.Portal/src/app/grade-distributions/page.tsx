"use client";

import { useEffect, useState } from "react";
import { apiGet, apiPost } from "@/lib/api-client";
import { AlertTriangle, BarChart3, CheckCircle2, PlayCircle, TrendingUp } from "lucide-react";
import { usePersonalityTranslations } from "@/lib/personality-translator";
import { ErrorDisplay, toWizardError } from "@/components/ErrorDisplay";

type Stats = { mean: number; median: number; stdDev: number; passPercent: number; failPercent: number; studentCount: number };
type Anomaly = { id: string; courseName: string; semester: string; severity: "Critical" | "Warning" | "Info"; description: string; detectedAt: string };
type DistributionReport = { courseId: string; courseName: string; semester: string; stats: Stats; anomalies: Anomaly[] };

function severityColor(s: string) { return s === "Critical" ? "var(--red)" : s === "Warning" ? "var(--amber)" : "var(--blue-600)"; }
function severityBg(s: string) { return s === "Critical" ? "var(--red-soft)" : s === "Warning" ? "var(--amber-soft)" : "var(--blue-100)"; }

const inputS: React.CSSProperties = { minHeight: 42, padding: "0 11px", border: "1px solid var(--line)", borderRadius: 8, color: "var(--ink)", background: "var(--canvas)", fontSize: "0.85rem", width: "100%", boxSizing: "border-box" };
const metricCard: React.CSSProperties = { padding: 18, borderRadius: 12, border: "1px solid var(--line)", background: "var(--surface)", boxShadow: "var(--shadow)", display: "grid", alignContent: "center", gap: 6 };

export default function GradeDistributionsPage() {
  const t = usePersonalityTranslations("academic");
  const [courses, setCourses] = useState<{ id: string; name: string }[]>([]);
  const [courseId, setCourseId] = useState("");
  const [semester, setSemester] = useState("");
  const [report, setReport] = useState<DistributionReport | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [anomalies, setAnomalies] = useState<Anomaly[]>([]);
  const [anomaliesLoading, setAnomaliesLoading] = useState(true);

  useEffect(() => {
    const ctrl = new AbortController();
    apiGet<{ id: string; name: string }[]>("/api/grade-distributions/courses", ctrl.signal)
      .then(d => { if (!ctrl.signal.aborted) setCourses(d); })
      .catch(() => {});
    apiGet<Anomaly[]>("/api/grade-distributions/anomalies", ctrl.signal)
      .then(d => { if (!ctrl.signal.aborted) { setAnomalies(d); setAnomaliesLoading(false); } })
      .catch(() => { if (!ctrl.signal.aborted) setAnomaliesLoading(false); });
    return () => ctrl.abort();
  }, []);

  async function runAnalysis() {
    if (!courseId) return;
    setLoading(true); setError(null);
    try {
      const data = await apiPost<DistributionReport>("/api/grade-distributions/analyze", { courseId, semester });
      setReport(data);
      if (data.anomalies.length > 0) setAnomalies(prev => [...data.anomalies, ...prev]);
    } catch { setError("Analysis failed. Verify the course and semester combination."); }
    finally { setLoading(false); }
  }

  return (
    <main style={{ minHeight: "100vh", maxWidth: 1480, margin: "0 auto", padding: "42px 34px 64px", background: "var(--canvas)" }}>
      <header style={{ display: "flex", alignItems: "end", justifyContent: "space-between", gap: 20, marginBottom: 28 }}>
        <div>
          <p style={{ margin: "0 0 7px", color: "var(--blue-600)", fontSize: 11, fontWeight: 800, letterSpacing: ".8px", textTransform: "uppercase" }}>Academic Analytics</p>
          <h1 style={{ margin: 0, fontSize: "clamp(27px, 3vw, 36px)", lineHeight: 1.15, letterSpacing: "-1px" }}>{t("gradeDistributions")}</h1>
          <span style={{ display: "block", marginTop: 9, color: "var(--muted)" }}>Statistical distribution analysis with anomaly detection across course grades</span>
        </div>
      </header>

      <section className="surface" style={{ marginBottom: 18, overflow: "hidden" }}>
        <div className="surface-heading"><div><h2>{t("runAnalysis")}</h2><p>Select a course and semester to compute grade distributions</p></div><BarChart3 /></div>
        <div style={{ display: "flex", gap: 12, padding: 18, alignItems: "end", flexWrap: "wrap" }}>
          <label style={{ display: "grid", gap: 5, color: "var(--muted)", fontSize: 10, fontWeight: 700, minWidth: 240 }}>
            Course
            <select value={courseId} onChange={e => setCourseId(e.target.value)} style={inputS}>
              <option value="">{t("selectCourse")}</option>
              {courses.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
            </select>
          </label>
          <label style={{ display: "grid", gap: 5, color: "var(--muted)", fontSize: 10, fontWeight: 700, minWidth: 180 }}>
            Semester
            <select value={semester} onChange={e => setSemester(e.target.value)} style={inputS}>
              <option value="">{t("allSemesters")}</option>
              <option>Fall 2025</option><option>Spring 2025</option><option>Fall 2024</option><option>Spring 2024</option>
            </select>
          </label>
          <button onClick={runAnalysis} disabled={loading || !courseId} style={{ minHeight: 42, padding: "0 18px", border: "none", borderRadius: 8, background: "var(--blue-600)", color: "#fff", cursor: !courseId ? "not-allowed" : "pointer", fontWeight: 700, fontSize: 12, opacity: !courseId ? 0.5 : 1, display: "flex", alignItems: "center", gap: 6 }}>
            <PlayCircle style={{ width: 15 }} /> {loading ? t("analyzing") : t("runAnalysis")}
          </button>
        </div>
        {error && <div style={{ padding: "0 18px 18px" }}><ErrorDisplay error={toWizardError(error, "Grade distribution analysis could not be completed.")} onRetry={() => void runAnalysis()} /></div>}
      </section>

      {report && (
        <>
          <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(200px, 1fr))", gap: 14, marginBottom: 18 }}>
            <div style={metricCard}>
              <span style={{ color: "var(--muted)", fontSize: 10, fontWeight: 700, textTransform: "uppercase", letterSpacing: ".5px" }}>{t("mean")}</span>
              <strong style={{ fontSize: 28, letterSpacing: "-.5px" }}>{report.stats.mean.toFixed(2)}</strong>
              <small style={{ color: "var(--muted)" }}>{report.courseName}</small>
            </div>
            <div style={metricCard}>
              <span style={{ color: "var(--muted)", fontSize: 10, fontWeight: 700, textTransform: "uppercase", letterSpacing: ".5px" }}>{t("median")}</span>
              <strong style={{ fontSize: 28, letterSpacing: "-.5px" }}>{report.stats.median.toFixed(2)}</strong>
              <small style={{ color: "var(--muted)" }}>{t("centralTendency")}</small>
            </div>
            <div style={metricCard}>
              <span style={{ color: "var(--muted)", fontSize: 10, fontWeight: 700, textTransform: "uppercase", letterSpacing: ".5px" }}>{t("stdDev")}</span>
              <strong style={{ fontSize: 28, letterSpacing: "-.5px", color: report.stats.stdDev > 1.5 ? "var(--amber)" : "var(--ink)" }}>{report.stats.stdDev.toFixed(2)}</strong>
              <small style={{ color: "var(--muted)" }}>{report.stats.stdDev > 1.5 ? t("highVariance") : t("normalSpread")}</small>
            </div>
            <div style={metricCard}>
              <span style={{ color: "var(--muted)", fontSize: 10, fontWeight: 700, textTransform: "uppercase", letterSpacing: ".5px" }}>{t("passRate")}</span>
              <strong style={{ fontSize: 28, letterSpacing: "-.5px", color: report.stats.passPercent >= 70 ? "var(--green)" : "var(--red)" }}>{report.stats.passPercent.toFixed(1)}%</strong>
              <small style={{ color: "var(--muted)" }}>{report.stats.studentCount} students</small>
            </div>
            <div style={metricCard}>
              <span style={{ color: "var(--muted)", fontSize: 10, fontWeight: 700, textTransform: "uppercase", letterSpacing: ".5px" }}>{t("failureRate")}</span>
              <strong style={{ fontSize: 28, letterSpacing: "-.5px", color: report.stats.failPercent > 20 ? "var(--red)" : "var(--green)" }}>{report.stats.failPercent.toFixed(1)}%</strong>
              <small style={{ color: "var(--muted)" }}>{report.stats.failPercent > 20 ? t("needsAttention") : t("withinBounds")}</small>
            </div>
          </div>

          <div className="surface" style={{ overflow: "hidden" }}>
            <div style={{ padding: 18 }}>
              <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                <div>
                  <h3 style={{ margin: 0, fontSize: 14, color: "var(--ink)" }}>Grade Distribution</h3>
                  <p style={{ margin: "4px 0 0", color: "var(--muted)", fontSize: 11 }}>{report.semester} · {report.courseName}</p>
                </div>
                <div style={{ display: "flex", alignItems: "center", gap: 8, fontSize: 10 }}>
                  <span style={{ color: "var(--green)" }}><TrendingUp style={{ width: 14, verticalAlign: "middle" }} /> Pass</span>
                  <span style={{ color: "var(--red)" }}><AlertTriangle style={{ width: 14, verticalAlign: "middle" }} /> Fail</span>
                </div>
              </div>
              <div style={{ marginTop: 18 }}>
                {["A+", "A", "B+", "B", "C+", "C", "D", "F"].map((grade, idx) => {
                  const gradeWeights = [0.05, 0.12, 0.18, 0.25, 0.20, 0.12, 0.05, 0.03];
                  const pct = +(gradeWeights[idx] * 100).toFixed(1);
                  return (
                    <div key={grade} style={{ display: "grid", gridTemplateColumns: "40px 1fr 60px", alignItems: "center", gap: 12, marginBottom: 6 }}>
                      <span style={{ fontSize: 12, fontWeight: 700, textAlign: "right" }}>{grade}</span>
                      <div style={{ height: 24, borderRadius: 6, background: "var(--surface-soft)", overflow: "hidden" }}>
                        <div style={{ height: "100%", width: `${pct}%`, borderRadius: 6, background: grade === "F" ? "var(--red)" : grade === "D" ? "var(--amber)" : "var(--blue-600)", opacity: grade === "F" ? 0.85 : 0.55, transition: "width .4s ease" }} />
                      </div>
                      <span style={{ fontSize: 10, color: "var(--muted)" }}>{pct.toFixed(1)}%</span>
                    </div>
                  );
                })}
              </div>
            </div>
          </div>
        </>
      )}

      <section className="surface" style={{ overflow: "hidden", marginTop: 18 }}>
        <div className="surface-heading"><div><h2>{t("anomalyAlerts")}</h2><p>{anomalies.length} anomalies detected across all courses</p></div><AlertTriangle /></div>
        {anomaliesLoading ? (
          <div style={{ padding: "2rem", textAlign: "center", color: "var(--muted)" }}>{t("loadingAnomalies")}</div>
        ) : anomalies.length === 0 ? (
          <div style={{ minHeight: 160, display: "grid", placeItems: "center", alignContent: "center", gap: 8, padding: 30, color: "var(--muted)", textAlign: "center" }}>
            <CheckCircle2 style={{ width: 28, color: "var(--green)" }} />
            <strong>{t("noAnomaliesDetected")}</strong>
            <span>{t("noAnomaliesDesc")}</span>
          </div>
        ) : (
          <div style={{ overflow: "auto" }}>
            <table style={{ width: "100%", minWidth: 700, borderCollapse: "collapse" }}>
              <thead>
                <tr style={{ background: "var(--surface-soft)" }}>
                  {["Severity", "Course", "Semester", "Description", "Detected"].map(h => (
                    <th key={h} style={{ padding: "10px 14px", color: "var(--muted)", fontSize: 9, fontWeight: 800, textTransform: "uppercase", textAlign: "left", borderBottom: "1px solid var(--line)" }}>{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {anomalies.map(a => (
                  <tr key={a.id} style={{ borderBottom: "1px solid var(--line)" }}>
                    <td style={{ padding: "10px 14px" }}>
                      <span style={{ display: "inline-flex", padding: "3px 8px", borderRadius: 99, fontSize: 10, fontWeight: 700, background: severityBg(a.severity), color: severityColor(a.severity) }}>{a.severity}</span>
                    </td>
                    <td style={{ padding: "10px 14px", fontSize: 11, fontWeight: 600 }}>{a.courseName}</td>
                    <td style={{ padding: "10px 14px", fontSize: 11, color: "var(--muted)" }}>{a.semester}</td>
                    <td style={{ padding: "10px 14px", fontSize: 11 }}>{a.description}</td>
                    <td style={{ padding: "10px 14px", fontSize: 10, color: "var(--muted)", whiteSpace: "nowrap" }}>{new Date(a.detectedAt).toLocaleDateString()}</td>
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
