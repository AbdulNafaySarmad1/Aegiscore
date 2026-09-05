"use client";

import { PageErrorDisplay } from "@/components/ErrorDisplay";
import { useEffect, useState } from "react";
import { apiGet, apiPost } from "@/lib/api-client";
import { usePersonalityTranslations } from "@/lib/personality-translator";
import {
  AlertCircle, Award, CheckCircle2, Crown, Medal, Search,
  Send, Shield, Swords, Target, Trophy, Users
} from "lucide-react";

type Badge = {
  id: string; name: string; category: string; tier: "Bronze" | "Silver" | "Gold" | "Platinum";
  criteria: string; imageUrl: string;
};

type Achievement = {
  studentId: string; studentName: string; badgeId: string; badgeName: string;
  earnedAt: string; triggerEvent: string;
};

type DeansEntry = {
  studentId: string; studentName: string; gpa: number; semester: string;
  entryType: "DeansList" | "HonorsRoll"; processedAt: string;
};

type LeaderboardEntry = {
  rank: number; studentId: string; studentName: string; totalScore: number;
  badgeCount: number; topBadge: string;
};

const tierColors: Record<string, { c: string; bg: string; border: string }> = {
  Bronze: { c: "#a77d4a", bg: "#faf3e8", border: "#cd9a5b" },
  Silver: { c: "#7e8da0", bg: "#f0f2f5", border: "#b8c4d0" },
  Gold: { c: "#b8960e", bg: "#fef9e3", border: "#e2c835" },
  Platinum: { c: "#4a6fa5", bg: "#eaf1fc", border: "#8ab4f5" }
};

const tierBadge = (tier: Badge["tier"]) => {
  const v = tierColors[tier];
  return <span style={{ display: "inline-flex", padding: "3px 8px", borderRadius: 99, fontSize: 10, fontWeight: 700, background: v.bg, color: v.c, border: `1px solid ${v.border}` }}>{tier}</span>;
};

const inputS: React.CSSProperties = { minHeight: 42, padding: "0 11px", border: "1px solid var(--line)", borderRadius: 8, color: "var(--ink)", background: "var(--canvas)", fontSize: "0.85rem", width: "100%", boxSizing: "border-box" };
const labelS: React.CSSProperties = { display: "grid", gap: 5, color: "var(--muted)", fontSize: 10, fontWeight: 700 };

export default function AchievementsPage() {
  const t = usePersonalityTranslations("academic");

  const [badges, setBadges] = useState<Badge[]>([]);
  const [achievements, setAchievements] = useState<Achievement[]>([]);
  const [deansEntries, setDeansEntries] = useState<DeansEntry[]>([]);
  const [leaderboard, setLeaderboard] = useState<LeaderboardEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const [detectStudentId, setDetectStudentId] = useState("");
  const [detecting, setDetecting] = useState(false);
  const [detectResult, setDetectResult] = useState<string | null>(null);

  const [deansSemester, setDeansSemester] = useState("");
  const [processingDeans, setProcessingDeans] = useState(false);

  const [leaderboardScope, setLeaderboardScope] = useState("all");

  useEffect(() => {
    const ctrl = new AbortController();
    Promise.all([
      apiGet<Badge[]>("/api/achievements/badges", ctrl.signal).catch(() => [] as Badge[]),
      apiGet<Achievement[]>("/api/achievements/recent", ctrl.signal).catch(() => [] as Achievement[]),
      apiGet<DeansEntry[]>("/api/achievements/deans-list", ctrl.signal).catch(() => [] as DeansEntry[]),
      apiGet<LeaderboardEntry[]>("/api/achievements/leaderboard", ctrl.signal).catch(() => [] as LeaderboardEntry[])
    ]).then(([bd, ac, dl, lb]) => {
      setBadges(bd); setAchievements(ac); setDeansEntries(dl); setLeaderboard(lb);
      setError(null); setLoading(false);
    }).catch(() => { setError("Achievement records are temporarily unavailable."); setLoading(false); });
    return () => { ctrl.abort(); };
  }, []);

  const refresh = () => {
    Promise.all([
      apiGet<Badge[]>("/api/achievements/badges").catch(() => [] as Badge[]),
      apiGet<Achievement[]>("/api/achievements/recent").catch(() => [] as Achievement[]),
      apiGet<DeansEntry[]>("/api/achievements/deans-list").catch(() => [] as DeansEntry[]),
      apiGet<LeaderboardEntry[]>(`/api/achievements/leaderboard?scope=${leaderboardScope}`).catch(() => [] as LeaderboardEntry[])
    ]).then(([bd, ac, dl, lb]) => { setBadges(bd); setAchievements(ac); setDeansEntries(dl); setLeaderboard(lb); });
  };

  async function triggerDetection(e: React.FormEvent) {
    e.preventDefault();
    if (!detectStudentId) return;
    setDetecting(true); setDetectResult(null);
    try {
      const res = await apiPost<{ newBadges: string[] }>("/api/achievements/detect", { studentId: detectStudentId });
      setDetectResult(res.newBadges.length > 0
        ? `Detected ${res.newBadges.length} new achievements: ${res.newBadges.join(", ")}`
        : "No new achievements detected");
      refresh();
    } catch { setDetectResult("error: Detection failed"); }
    finally { setDetecting(false); }
  }

  async function processDeansList(e: React.FormEvent) {
    e.preventDefault();
    if (!deansSemester) return;
    setProcessingDeans(true); setMessage(null);
    try {
      await apiPost("/api/achievements/deans-list/process", { semester: deansSemester });
      setMessage("success: Dean's List processed");
      setDeansSemester("");
      refresh();
    } catch { setMessage("error: Processing failed"); }
    finally { setProcessingDeans(false); }
  }

  if (loading) return (
    <main style={{ minHeight: "100vh", display: "grid", placeItems: "center", gap: 16, color: "var(--muted)" }}>
      <div style={{ width: 40, height: 4, borderRadius: 99, overflow: "hidden", background: "var(--line)" }}>
        <div style={{ width: "50%", height: "100%", borderRadius: "inherit", background: "var(--blue-500)", animation: "launch 1s ease-in-out infinite alternate" }} />
      </div>
      Loading achievements dashboard...
    </main>
  );

  if (error) return <PageErrorDisplay message={error} onRetry={() => void refresh()} />;

  return (
    <main style={{ minHeight: "100vh", maxWidth: 1480, margin: "0 auto", padding: "42px 34px 64px", background: "var(--canvas)" }}>
      <header style={{ display: "flex", alignItems: "end", justifyContent: "space-between", gap: 20, marginBottom: 28 }}>
        <div>
          <p style={{ margin: "0 0 7px", color: "var(--blue-600)", fontSize: 11, fontWeight: 800, letterSpacing: ".8px", textTransform: "uppercase" }}>Student Engagement</p>
          <h1 style={{ margin: 0, fontSize: "clamp(27px, 3vw, 36px)", lineHeight: 1.15, letterSpacing: "-1px" }}>{t("achievements") ?? "Student Achievement Gamification"}</h1>
          <span style={{ display: "block", marginTop: 9, color: "var(--muted)" }}>Badge catalog, achievement detection, Dean&apos;s List, leaderboards, and roadmap</span>
        </div>
      </header>

      {message && (
        <div style={{ display: "flex", alignItems: "center", gap: 9, padding: "11px 14px", marginBottom: 18, border: message.startsWith("error") ? "1px solid #ecc0c6" : "1px solid #aaddc9", borderRadius: 10, color: message.startsWith("error") ? "var(--red)" : "var(--green)", background: message.startsWith("error") ? "var(--red-soft)" : "var(--green-soft)", fontSize: 11 }}>
          {message.startsWith("error") ? <AlertCircle /> : <CheckCircle2 />}{message.replace(/^[^:]+: /, "")}
        </div>
      )}

      <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(180px, 1fr))", gap: 14, marginBottom: 18 }}>
        <article style={{ minHeight: 110, display: "grid", alignContent: "center", padding: 20, border: "1px solid var(--line)", borderRadius: 14, background: "var(--surface)", boxShadow: "var(--shadow)" }}>
          <span style={{ color: "var(--muted)", fontSize: 11, fontWeight: 700, textTransform: "uppercase" }}>Total Badges</span>
          <strong style={{ margin: "7px 0 3px", fontSize: 28 }}>{badges.length}</strong>
          <small style={{ color: "var(--muted)" }}>across {new Set(badges.map(b => b.category)).size} categories</small>
        </article>
        <article style={{ minHeight: 110, display: "grid", alignContent: "center", padding: 20, border: "1px solid var(--line)", borderRadius: 14, background: "var(--surface)", boxShadow: "var(--shadow)" }}>
          <span style={{ color: "var(--muted)", fontSize: 11, fontWeight: 700, textTransform: "uppercase" }}>Achievements Earned</span>
          <strong style={{ margin: "7px 0 3px", fontSize: 28, color: "var(--green)" }}>{achievements.length}</strong>
          <small style={{ color: "var(--muted)" }}>across all students</small>
        </article>
        <article style={{ minHeight: 110, display: "grid", alignContent: "center", padding: 20, border: "1px solid var(--line)", borderRadius: 14, background: "var(--surface)", boxShadow: "var(--shadow)" }}>
          <span style={{ color: "var(--muted)", fontSize: 11, fontWeight: 700, textTransform: "uppercase" }}>Dean&apos;s List</span>
          <strong style={{ margin: "7px 0 3px", fontSize: 28, color: "var(--amber)" }}>{deansEntries.length}</strong>
          <small style={{ color: "var(--muted)" }}>honored students</small>
        </article>
        <article style={{ minHeight: 110, display: "grid", alignContent: "center", padding: 20, border: "1px solid var(--line)", borderRadius: 14, background: "var(--surface)", boxShadow: "var(--shadow)" }}>
          <span style={{ color: "var(--muted)", fontSize: 11, fontWeight: 700, textTransform: "uppercase" }}>Top Scorer</span>
          <strong style={{ margin: "7px 0 3px", fontSize: 28, color: "var(--blue-600)" }}>
            {leaderboard.length > 0 ? leaderboard[0].totalScore.toLocaleString() : "—"}
          </strong>
          <small style={{ color: "var(--muted)" }}>pts • {leaderboard[0]?.studentName || "N/A"}</small>
        </article>
      </div>

      <section style={{ display: "grid", gridTemplateColumns: "minmax(0, 1fr) minmax(360px, 0.8fr)", gap: 18, marginBottom: 18 }}>
        <div className="surface" style={{ overflow: "hidden" }}>
          <div className="surface-heading"><div><h2>Achievement Detection</h2><p>Trigger detection rules for a student</p></div><Search /></div>
          <form onSubmit={triggerDetection} style={{ display: "grid", gap: 12, padding: 18 }}>
            <div style={{ display: "grid", gridTemplateColumns: "1fr auto", gap: 12, alignItems: "end" }}>
              <label style={labelS}>
                Student ID <b style={{ color: "var(--red)" }}>*</b>
                <input placeholder="e.g. STU-2024-001" value={detectStudentId} onChange={e => setDetectStudentId(e.target.value)} style={inputS} required />
              </label>
              <button type="submit" disabled={detecting} style={{ padding: "0.7rem 1.2rem", background: "var(--blue-600)", color: "#fff", border: "none", borderRadius: 8, cursor: "pointer", fontWeight: 700, fontSize: 12, opacity: detecting ? 0.6 : 1, whiteSpace: "nowrap" }}>
                {detecting ? "Detecting..." : <><Search style={{ width: 14, verticalAlign: "middle", marginRight: 6 }} />Detect</>}
              </button>
            </div>
            {detectResult && (
              <div style={{ padding: "10px 14px", borderRadius: 8, border: detectResult.startsWith("error") ? "1px solid #ecc0c6" : "1px solid #aaddc9", color: detectResult.startsWith("error") ? "var(--red)" : "var(--green)", background: detectResult.startsWith("error") ? "var(--red-soft)" : "var(--green-soft)", fontSize: 11 }}>
                {detectResult.replace("error: ", "")}
              </div>
            )}
          </form>
        </div>

        <div className="surface" style={{ overflow: "hidden" }}>
          <div className="surface-heading"><div><h2>Dean&apos;s List Processing</h2><p>Process end-of-semester honors</p></div><Crown /></div>
          <form onSubmit={processDeansList} style={{ display: "grid", gap: 12, padding: 18 }}>
            <label style={labelS}>
              Semester <b style={{ color: "var(--red)" }}>*</b>
              <input placeholder="e.g. Fall 2025" value={deansSemester} onChange={e => setDeansSemester(e.target.value)} style={inputS} required />
            </label>
            <button type="submit" disabled={processingDeans} style={{ padding: "0.7rem", background: "var(--amber)", color: "#fff", border: "none", borderRadius: 8, cursor: "pointer", fontWeight: 700, fontSize: 12, opacity: processingDeans ? 0.6 : 1 }}>
              {processingDeans ? "Processing..." : <><Crown style={{ width: 14, verticalAlign: "middle", marginRight: 6 }} />Process Dean&apos;s List</>}
            </button>
          </form>
        </div>
      </section>

      <section className="surface" style={{ overflow: "hidden", marginBottom: 18 }}>
        <div className="surface-heading"><div><h2>Badge Catalog</h2><p>{badges.length} badges available</p></div><Award /></div>
        {badges.length === 0 ? (
          <div style={{ minHeight: 200, display: "grid", placeItems: "center", alignContent: "center", gap: 8, padding: 30, color: "var(--muted)", textAlign: "center" }}>
            <Medal style={{ width: 28 }} />
            <strong>No badges configured</strong>
            <span>Create achievement badges to motivate students</span>
          </div>
        ) : (
          <div style={{ padding: 18, display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(240px, 1fr))", gap: 14 }}>
            {badges.map(b => {
              const tc = tierColors[b.tier];
              return (
                <div key={b.id} style={{ padding: 16, border: `1px solid ${tc.border}`, borderRadius: 12, background: tc.bg, display: "grid", gap: 8 }}>
                  <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
                    <div style={{ width: 42, height: 42, borderRadius: 10, display: "grid", placeItems: "center", background: "var(--surface)", fontSize: 20 }}>
                      {b.imageUrl ? <img src={b.imageUrl} alt={b.name} style={{ width: 28, height: 28 }} /> : <Award style={{ width: 20, color: tc.c }} />}
                    </div>
                    <div>
                      <strong style={{ fontSize: 12 }}>{b.name}</strong>
                      <div style={{ display: "flex", gap: 6, marginTop: 3 }}>
                        {tierBadge(b.tier)}
                        <span style={{ fontSize: 9, color: "var(--muted)", padding: "2px 6px", borderRadius: 99, background: "var(--surface)" }}>{b.category}</span>
                      </div>
                    </div>
                  </div>
                  <p style={{ margin: 0, fontSize: 10, color: "var(--muted)", lineHeight: 1.5 }}>{b.criteria}</p>
                </div>
              );
            })}
          </div>
        )}
      </section>

      <section style={{ display: "grid", gridTemplateColumns: "minmax(0, 1fr) minmax(380px, 0.9fr)", gap: 18, marginBottom: 18 }}>
        <div className="surface" style={{ overflow: "hidden" }}>
          <div className="surface-heading">
            <div><h2>Leaderboard</h2><p>{leaderboard.length} ranked students</p></div>
            <select value={leaderboardScope} onChange={e => { setLeaderboardScope(e.target.value); refresh(); }} style={{ minHeight: 33, padding: "0 9px", border: "1px solid var(--line)", borderRadius: 7, color: "var(--ink)", background: "var(--surface)", fontSize: 11 }}>
              <option value="all">All Students</option>
              <option value="department">Department</option>
              <option value="batch">Batch</option>
              <option value="semester">Current Semester</option>
            </select>
          </div>
          {leaderboard.length === 0 ? (
            <div style={{ minHeight: 200, display: "grid", placeItems: "center", alignContent: "center", gap: 8, padding: 30, color: "var(--muted)", textAlign: "center" }}>
              <Trophy style={{ width: 28 }} />
              <strong>Leaderboard is empty</strong>
              <span>Achievements will appear as students earn badges</span>
            </div>
          ) : (
            <div style={{ overflow: "auto" }}>
              <table style={{ width: "100%", minWidth: 550, borderCollapse: "collapse" }}>
                <thead>
                  <tr style={{ background: "var(--surface-soft)" }}>
                    {["Rank", "Student", "Score", "Badges", "Top Badge"].map(h => (
                      <th key={h} style={{ padding: "10px 14px", color: "var(--muted)", fontSize: 9, fontWeight: 800, textTransform: "uppercase", textAlign: "left", borderBottom: "1px solid var(--line)" }}>{h}</th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {leaderboard.map(e => (
                    <tr key={e.studentId} style={{ borderBottom: "1px solid var(--line)", background: e.rank <= 3 ? "var(--amber-soft)" : undefined }}>
                      <td style={{ padding: "10px 14px", fontSize: 13, fontWeight: 800, color: e.rank === 1 ? "#b8960e" : e.rank === 2 ? "#7e8da0" : e.rank === 3 ? "#a77d4a" : "var(--muted)" }}>
                        #{e.rank}
                      </td>
                      <td style={{ padding: "10px 14px", fontSize: 11, fontWeight: 600 }}>{e.studentName}</td>
                      <td style={{ padding: "10px 14px", fontSize: 13, fontWeight: 800, color: "var(--blue-600)" }}>{e.totalScore.toLocaleString()}</td>
                      <td style={{ padding: "10px 14px", fontSize: 11 }}>{e.badgeCount}</td>
                      <td style={{ padding: "10px 14px", fontSize: 11, color: "var(--muted)" }}>{e.topBadge}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>

        <div className="surface" style={{ overflow: "hidden" }}>
          <div className="surface-heading"><div><h2>Recent Achievements</h2><p>{achievements.length} earned</p></div><Shield /></div>
          {achievements.length === 0 ? (
            <div style={{ minHeight: 200, display: "grid", placeItems: "center", alignContent: "center", gap: 8, padding: 30, color: "var(--muted)", textAlign: "center" }}>
              <Target style={{ width: 28 }} />
              <strong>No achievements earned yet</strong>
              <span>Run detection to award badges</span>
            </div>
          ) : (
            <div style={{ overflow: "auto", maxHeight: 400 }}>
              {achievements.map(a => (
                <div key={`${a.studentId}-${a.badgeId}`} style={{ display: "flex", alignItems: "center", gap: 10, padding: "10px 18px", borderBottom: "1px solid var(--line)" }}>
                  <div style={{ width: 32, height: 32, borderRadius: 8, display: "grid", placeItems: "center", background: "var(--amber-soft)", color: "var(--amber)", flex: "0 0 auto" }}>
                    <Trophy style={{ width: 16 }} />
                  </div>
                  <div style={{ display: "grid", flex: 1 }}>
                    <strong style={{ fontSize: 11 }}>{a.studentName}</strong>
                    <small style={{ color: "var(--muted)", fontSize: 10 }}>Earned {a.badgeName} — {new Date(a.earnedAt).toLocaleDateString()}</small>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </section>

      <section className="surface" style={{ overflow: "hidden" }}>
        <div className="surface-heading"><div><h2>Dean&apos;s List & Honors Roll</h2><p>{deansEntries.length} entries</p></div><Crown /></div>
        {deansEntries.length === 0 ? (
          <div style={{ minHeight: 200, display: "grid", placeItems: "center", alignContent: "center", gap: 8, padding: 30, color: "var(--muted)", textAlign: "center" }}>
            <Crown style={{ width: 28 }} />
            <strong>No Dean&apos;s List entries</strong>
            <span>Process a semester to generate the Dean&apos;s List</span>
          </div>
        ) : (
          <div style={{ overflow: "auto" }}>
            <table style={{ width: "100%", minWidth: 600, borderCollapse: "collapse" }}>
              <thead>
                <tr style={{ background: "var(--surface-soft)" }}>
                  {["Student", "GPA", "Semester", "Type", "Processed"].map(h => (
                    <th key={h} style={{ padding: "10px 14px", color: "var(--muted)", fontSize: 9, fontWeight: 800, textTransform: "uppercase", textAlign: "left", borderBottom: "1px solid var(--line)" }}>{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {deansEntries.map(d => (
                  <tr key={d.studentId} style={{ borderBottom: "1px solid var(--line)" }}>
                    <td style={{ padding: "10px 14px", fontSize: 11, fontWeight: 600 }}>{d.studentName}</td>
                    <td style={{ padding: "10px 14px", fontSize: 12, fontWeight: 800, color: d.gpa >= 3.8 ? "var(--green)" : "var(--blue-600)" }}>{d.gpa.toFixed(2)}</td>
                    <td style={{ padding: "10px 14px", fontSize: 11, color: "var(--muted)" }}>{d.semester}</td>
                    <td style={{ padding: "10px 14px" }}>
                      <span style={{ display: "inline-flex", padding: "3px 8px", borderRadius: 99, fontSize: 10, fontWeight: 700, background: d.entryType === "DeansList" ? "var(--amber-soft)" : "var(--green-soft)", color: d.entryType === "DeansList" ? "var(--amber)" : "var(--green)" }}>
                        {d.entryType === "DeansList" ? "Dean's List" : "Honors Roll"}
                      </span>
                    </td>
                    <td style={{ padding: "10px 14px", fontSize: 10, color: "var(--muted)" }}>{new Date(d.processedAt).toLocaleDateString()}</td>
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
