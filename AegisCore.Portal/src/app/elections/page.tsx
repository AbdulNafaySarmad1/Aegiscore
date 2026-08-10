"use client";

import { useEffect, useState } from "react";
import { apiGet, apiPost } from "@/lib/api-client";
import { usePersonalityTranslations } from "@/lib/personality-translator";
import { useRoles, isStudentOnly } from "@/lib/use-roles";
import { AppShell } from "@/components/layout/AppShell";
import {
  AlertCircle, Award, Calendar, CheckCircle2, ClipboardList, Clock, FileText,
  Key, Megaphone, RefreshCw, Send, Shield, Trophy, UserPlus, Users, Vote
} from "lucide-react";

type ElectionType = "StudentUnion" | "ClassRep" | "SocietyHead" | "SportsCaptain";
type ElectionStatus = "Setup" | "NominationsOpen" | "Campaigning" | "VotingOpen" | "ResultsAnnounced";

type Election = {
  id: string; title: string; type: ElectionType; status: ElectionStatus;
  nominationStart: string; campaigningStart: string; votingStart: string;
  resultsDate: string;
};

type Candidate = {
  id: string; electionId: string; studentId: string; studentName: string;
  position: string; votes: number;
};

type ElectionResult = {
  position: string; winnerName: string; winnerId: string;
  voteCount: number; percentage: number; certified: boolean;
};

const statusOrder: ElectionStatus[] = ["Setup", "NominationsOpen", "Campaigning", "VotingOpen", "ResultsAnnounced"];

const inputS: React.CSSProperties = { minHeight: 42, padding: "0 11px", border: "1px solid var(--line)", borderRadius: 8, color: "var(--ink)", background: "var(--canvas)", fontSize: "0.85rem", width: "100%", boxSizing: "border-box" };
const labelS: React.CSSProperties = { display: "grid", gap: 5, color: "var(--muted)", fontSize: 10, fontWeight: 700 };

export default function ElectionsPage() {
  const t = usePersonalityTranslations("academic");
  const roles = useRoles();
  const studentOnly = isStudentOnly(roles);

  const [elections, setElections] = useState<Election[]>([]);
  const [candidates, setCandidates] = useState<Candidate[]>([]);
  const [results, setResults] = useState<ElectionResult[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const [elecTitle, setElecTitle] = useState("");
  const [elecType, setElecType] = useState<ElectionType>("StudentUnion");
  const [nomStart, setNomStart] = useState("");
  const [campStart, setCampStart] = useState("");
  const [voteStart, setVoteStart] = useState("");
  const [resultsDate, setResultsDate] = useState("");
  const [creating, setCreating] = useState(false);

  const [nomElectionId, setNomElectionId] = useState("");
  const [nomStudentId, setNomStudentId] = useState("");
  const [nomPosition, setNomPosition] = useState("");
  const [nominating, setNominating] = useState(false);

  const [tokenStudentId, setTokenStudentId] = useState("");
  const [issuedToken, setIssuedToken] = useState<string | null>(null);
  const [issuing, setIssuing] = useState(false);

  const [voteElectionId, setVoteElectionId] = useState("");
  const [voteToken, setVoteToken] = useState("");
  const [voteData, setVoteData] = useState("");
  const [casting, setCasting] = useState(false);

  const [tallyElectionId, setTallyElectionId] = useState("");
  const [tallying, setTallying] = useState(false);
  const [certifyingId, setCertifyingId] = useState<string | null>(null);

  useEffect(() => {
    if (studentOnly) { setLoading(false); return; }
    const ctrl = new AbortController();
    const fetch = async () => {
      try {
        const [e, c, r] = await Promise.all([
          apiGet<Election[]>("/api/elections", ctrl.signal).catch(() => [] as Election[]),
          apiGet<Candidate[]>("/api/elections/candidates", ctrl.signal).catch(() => [] as Candidate[]),
          apiGet<ElectionResult[]>("/api/elections/results", ctrl.signal).catch(() => [] as ElectionResult[])
        ]);
        setElections(e); setCandidates(c); setResults(r);
      } catch { setError("Failed to load election data"); }
      finally { setLoading(false); }
    };
    fetch();
    return () => { ctrl.abort(); };
  }, [studentOnly]);

  async function createElection(e: React.FormEvent) {
    e.preventDefault();
    setCreating(true); setMessage(null);
    try {
      await apiPost("/api/elections", { title: elecTitle, type: elecType, nominationStart: nomStart, campaigningStart: campStart, votingStart: voteStart, resultsDate });
      setMessage("success: Election created");
      setElecTitle(""); setNomStart(""); setCampStart(""); setVoteStart(""); setResultsDate("");
      const ctrl = new AbortController();
      const el = await apiGet<Election[]>("/api/elections", ctrl.signal);
      setElections(el);
    } catch { setMessage("error: Failed to create election"); }
    finally { setCreating(false); }
  }

  async function nominateCandidate(e: React.FormEvent) {
    e.preventDefault();
    setNominating(true); setMessage(null);
    try {
      await apiPost("/api/elections/nominate", { electionId: nomElectionId, studentId: nomStudentId, position: nomPosition });
      setMessage("success: Candidate nominated");
      setNomStudentId(""); setNomPosition("");
      const ctrl = new AbortController();
      const c = await apiGet<Candidate[]>("/api/elections/candidates", ctrl.signal);
      setCandidates(c);
    } catch { setMessage("error: Nomination failed"); }
    finally { setNominating(false); }
  }

  async function issueToken(e: React.FormEvent) {
    e.preventDefault();
    setIssuing(true); setMessage(null); setIssuedToken(null);
    try {
      const resp = await apiPost<{ token: string }>("/api/elections/issue-token", { studentId: tokenStudentId });
      setIssuedToken(resp.token);
      setMessage("success: Token issued successfully");
    } catch { setMessage("error: Token issuance failed"); }
    finally { setIssuing(false); }
  }

  async function castVote(e: React.FormEvent) {
    e.preventDefault();
    setCasting(true); setMessage(null);
    try {
      await apiPost("/api/elections/cast-vote", { electionId: voteElectionId, token: voteToken, voteData });
      setMessage("success: Vote cast successfully");
      setVoteToken(""); setVoteData("");
    } catch { setMessage("error: Vote casting failed"); }
    finally { setCasting(false); }
  }

  async function tallyResults() {
    if (!tallyElectionId) return;
    setTallying(true); setMessage(null);
    try {
      const r = await apiPost<ElectionResult[]>(`/api/elections/${tallyElectionId}/tally`);
      setResults(r);
      setMessage("success: Results tallied");
    } catch { setMessage("error: Tally failed"); }
    finally { setTallying(false); }
  }

  async function certifyResult(position: string) {
    setCertifyingId(position);
    try {
      await apiPost(`/api/elections/certify`, { position });
      setMessage("success: Result certified");
      setResults(prev => prev.map(r => r.position === position ? { ...r, certified: true } : r));
    } catch { setMessage("error: Certification failed"); }
    finally { setCertifyingId(null); }
  }

  if (loading) return (
    <AppShell>
      <main style={{ minHeight: "100vh", display: "grid", placeItems: "center", gap: 16, color: "var(--muted)" }}>
        <div style={{ width: 40, height: 4, borderRadius: 99, overflow: "hidden", background: "var(--line)" }}>
          <div style={{ width: "50%", height: "100%", borderRadius: "inherit", background: "var(--blue-500)", animation: "launch 1s ease-in-out infinite alternate" }} />
        </div>
        Loading elections portal...
      </main>
    </AppShell>
  );

  if (studentOnly) return (
    <AppShell>
      <main style={{ minHeight: "100vh", maxWidth: 1480, margin: "0 auto", padding: "42px 34px 64px", background: "var(--canvas)" }}>
        <header style={{ marginBottom: 28 }}>
          <p style={{ margin: "0 0 7px", color: "var(--blue-600)", fontSize: 11, fontWeight: 800, letterSpacing: ".8px", textTransform: "uppercase" }}>Student Governance</p>
          <h1 style={{ margin: 0, fontSize: "clamp(27px, 3vw, 36px)", lineHeight: 1.15, letterSpacing: "-1px" }}>Student Elections</h1>
          <span style={{ display: "block", marginTop: 9, color: "var(--muted)" }}>Elections will appear here when scheduled by administration. You can view candidates, cast votes, and check results during active elections.</span>
        </header>
        <div style={{ padding: "30px 0", textAlign: "center", color: "var(--muted)" }}>
          <Trophy style={{ width: 40, opacity: 0.3 }} />
          <p style={{ marginTop: 12, fontSize: 14 }}>No active elections at this time.</p>
        </div>
      </main>
    </AppShell>
  );

  if (error) return (
    <AppShell>
      <main style={{ minHeight: "100vh", display: "grid", placeItems: "center", alignContent: "center", gap: 12, color: "var(--red)" }}>
        <AlertCircle style={{ width: 32 }} />
        <strong>{error}</strong>
        <button onClick={() => window.location.reload()} style={{ minHeight: 40, padding: "0 18px", border: "1px solid var(--line)", borderRadius: 8, color: "var(--blue-600)", background: "var(--surface)", cursor: "pointer", fontWeight: 700 }}>Retry</button>
      </main>
    </AppShell>
  );

  return (
    <AppShell>
      <main style={{ minHeight: "100vh", maxWidth: 1480, margin: "0 auto", padding: "42px 34px 64px", background: "var(--canvas)" }}>
      <header style={{ display: "flex", alignItems: "end", justifyContent: "space-between", gap: 20, marginBottom: 28 }}>
        <div>
          <p style={{ margin: "0 0 7px", color: "var(--blue-600)", fontSize: 11, fontWeight: 800, letterSpacing: ".8px", textTransform: "uppercase" }}>Student Governance</p>
          <h1 style={{ margin: 0, fontSize: "clamp(27px, 3vw, 36px)", lineHeight: 1.15, letterSpacing: "-1px" }}>{t("elections") ?? "Student Elections"}</h1>
          <span style={{ display: "block", marginTop: 9, color: "var(--muted)" }}>Secure online elections, candidate nominations, encrypted balloting, and result certification</span>
        </div>
      </header>

      {message && (
        <div style={{ display: "flex", alignItems: "center", gap: 9, padding: "11px 14px", marginBottom: 18, border: message.startsWith("error") ? "1px solid #ecc0c6" : "1px solid #aaddc9", borderRadius: 10, color: message.startsWith("error") ? "var(--red)" : "var(--green)", background: message.startsWith("error") ? "var(--red-soft)" : "var(--green-soft)", fontSize: 11 }}>
          {message.startsWith("error") ? <AlertCircle /> : <CheckCircle2 />}{message.replace(/^[^:]+: /, "")}
        </div>
      )}

      {elections.length > 0 && (
        <section style={{ marginBottom: 18, padding: 18, border: "1px solid var(--line)", borderRadius: 14, background: "var(--surface)", boxShadow: "var(--shadow)" }}>
          <div style={{ display: "flex", alignItems: "center", gap: 8, marginBottom: 12, justifyContent: "space-between" }}>
            <div><h2 style={{ margin: 0, fontSize: 14 }}>Election Timeline</h2><p style={{ margin: "3px 0 0", color: "var(--muted)", fontSize: 11 }}>{elections[0]?.title}</p></div>
          </div>
          <div style={{ display: "flex", gap: 4 }}>
            {statusOrder.map((s, i) => {
              const current = elections[0]?.status;
              const idx = statusOrder.indexOf(current);
              const done = i <= idx;
              const active = i === idx;
              return (
                <div key={s} style={{ flex: 1, textAlign: "center", minWidth: 0 }}>
                  <div style={{ height: 4, borderRadius: 99, background: done ? "var(--blue-600)" : "var(--line)", marginBottom: 8 }} />
                  <div style={{ width: 28, height: 28, margin: "0 auto 5px", display: "grid", placeItems: "center", borderRadius: "50%", border: `2px solid ${done ? "var(--blue-600)" : "var(--line)"}`, background: active ? "var(--blue-600)" : done ? "var(--blue-100)" : "var(--surface)" }}>
                    <span style={{ fontSize: 10, fontWeight: 800, color: active ? "#fff" : done ? "var(--blue-600)" : "var(--muted)" }}>{i + 1}</span>
                  </div>
                  <small style={{ color: active ? "var(--blue-600)" : "var(--muted)", fontSize: 9, fontWeight: active ? 800 : 600, display: "block" }}>{s}</small>
                </div>
              );
            })}
          </div>
        </section>
      )}

      <section style={{ display: "grid", gridTemplateColumns: "minmax(0, 1.3fr) minmax(360px, 0.8fr)", gap: 18, marginBottom: 18 }}>
        <div style={{ display: "grid", gap: 18, alignContent: "start" }}>
          {!studentOnly && (
          <div className="surface" style={{ overflow: "hidden" }}>
            <div className="surface-heading"><div><h2>Create Election</h2><p>Define a new election with full timeline</p></div><ClipboardList /></div>
            <form onSubmit={createElection} style={{ display: "grid", gap: 12, padding: 18 }}>
              <div style={{ display: "grid", gridTemplateColumns: "repeat(2, minmax(0, 1fr))", gap: 12 }}>
                <label style={labelS}>
                  Title <b style={{ color: "var(--red)" }}>*</b>
                  <input placeholder="e.g. Student Union 2026" value={elecTitle} onChange={e => setElecTitle(e.target.value)} style={inputS} required />
                </label>
                <label style={labelS}>
                  Type <b style={{ color: "var(--red)" }}>*</b>
                  <select value={elecType} onChange={e => setElecType(e.target.value as ElectionType)} style={inputS}>
                    <option value="StudentUnion">Student Union</option>
                    <option value="ClassRep">Class Representative</option>
                    <option value="SocietyHead">Society Head</option>
                    <option value="SportsCaptain">Sports Captain</option>
                  </select>
                </label>
                <label style={labelS}>
                  Nomination Start <b style={{ color: "var(--red)" }}>*</b>
                  <input type="date" value={nomStart} onChange={e => setNomStart(e.target.value)} style={inputS} required />
                </label>
                <label style={labelS}>
                  Campaigning Start <b style={{ color: "var(--red)" }}>*</b>
                  <input type="date" value={campStart} onChange={e => setCampStart(e.target.value)} style={inputS} required />
                </label>
                <label style={labelS}>
                  Voting Start <b style={{ color: "var(--red)" }}>*</b>
                  <input type="date" value={voteStart} onChange={e => setVoteStart(e.target.value)} style={inputS} required />
                </label>
                <label style={labelS}>
                  Results Date <b style={{ color: "var(--red)" }}>*</b>
                  <input type="date" value={resultsDate} onChange={e => setResultsDate(e.target.value)} style={inputS} required />
                </label>
              </div>
              <button type="submit" disabled={creating}
                style={{ padding: "0.7rem", background: "var(--blue-600)", color: "#fff", border: "none", borderRadius: 8, cursor: "pointer", fontWeight: 700, fontSize: 12, opacity: creating ? 0.6 : 1 }}>
                {creating ? "Creating..." : "Create Election"}
              </button>
            </form>
          </div>
          )}

          {!studentOnly && (
          <div className="surface" style={{ overflow: "hidden" }}>
            <div className="surface-heading"><div><h2>Voter Token Issuance</h2><p>Issue secure voting tokens to students</p></div><Key /></div>
            <div style={{ padding: 18 }}>
              <form onSubmit={issueToken} style={{ display: "grid", gap: 12 }}>
                <label style={labelS}>
                  Student ID <b style={{ color: "var(--red)" }}>*</b>
                  <input placeholder="e.g. STU-2024-001" value={tokenStudentId} onChange={e => setTokenStudentId(e.target.value)} style={inputS} required />
                </label>
                <button type="submit" disabled={issuing}
                  style={{ padding: "0.7rem", background: "var(--green)", color: "#fff", border: "none", borderRadius: 8, cursor: "pointer", fontWeight: 700, fontSize: 12, opacity: issuing ? 0.6 : 1 }}>
                  {issuing ? "Issuing..." : "Issue Token"}
                </button>
              </form>
              {issuedToken && (
                <div style={{ marginTop: 12, padding: 12, borderRadius: 8, border: "1px solid #aaddc9", background: "var(--green-soft)" }}>
                  <small style={{ color: "var(--green)", fontSize: 9, fontWeight: 700, display: "block", marginBottom: 4 }}>ISSUED TOKEN</small>
                  <code style={{ font: "11px ui-monospace, monospace", color: "var(--ink)", wordBreak: "break-all" }}>{issuedToken}</code>
                </div>
              )}
            </div>
          </div>
          )}
        </div>

        <div style={{ display: "grid", gap: 18, alignContent: "start" }}>
          {!studentOnly && (
          <div className="surface" style={{ overflow: "hidden" }}>
            <div className="surface-heading"><div><h2>Candidate Nomination</h2><p>Register candidates for an election</p></div><UserPlus /></div>
            <form onSubmit={nominateCandidate} style={{ display: "grid", gap: 12, padding: 18 }}>
              <label style={labelS}>
                Election <b style={{ color: "var(--red)" }}>*</b>
                <select value={nomElectionId} onChange={e => setNomElectionId(e.target.value)} style={inputS} required>
                  <option value="">Select election...</option>
                  {elections.map(e => <option key={e.id} value={e.id}>{e.title} ({e.type})</option>)}
                </select>
              </label>
              <label style={labelS}>
                Student ID <b style={{ color: "var(--red)" }}>*</b>
                <input placeholder="e.g. STU-2024-001" value={nomStudentId} onChange={e => setNomStudentId(e.target.value)} style={inputS} required />
              </label>
              <label style={labelS}>
                Position Name <b style={{ color: "var(--red)" }}>*</b>
                <input placeholder="e.g. President, Treasurer" value={nomPosition} onChange={e => setNomPosition(e.target.value)} style={inputS} required />
              </label>
              <button type="submit" disabled={nominating}
                style={{ padding: "0.7rem", background: "var(--blue-600)", color: "#fff", border: "none", borderRadius: 8, cursor: "pointer", fontWeight: 700, fontSize: 12, opacity: nominating ? 0.6 : 1 }}>
                {nominating ? "Nominating..." : "Nominate Candidate"}
              </button>
            </form>
          </div>
          )}

          <div className="surface" style={{ overflow: "hidden" }}>
            <div className="surface-heading"><div><h2>Cast Vote</h2><p>Submit encrypted ballot with token</p></div><Vote /></div>
            <form onSubmit={castVote} style={{ display: "grid", gap: 12, padding: 18 }}>
              <label style={labelS}>
                Election <b style={{ color: "var(--red)" }}>*</b>
                <select value={voteElectionId} onChange={e => setVoteElectionId(e.target.value)} style={inputS} required>
                  <option value="">Select election...</option>
                  {elections.map(e => <option key={e.id} value={e.id}>{e.title}</option>)}
                </select>
              </label>
              <label style={labelS}>
                Voting Token <b style={{ color: "var(--red)" }}>*</b>
                <input placeholder="Enter issued token" value={voteToken} onChange={e => setVoteToken(e.target.value)} style={inputS} required />
              </label>
              <label style={labelS}>
                Vote Data <b style={{ color: "var(--red)" }}>*</b>
                <textarea placeholder='JSON: {"position": "President", "candidateId": "..."}' value={voteData} onChange={e => setVoteData(e.target.value)} rows={3} style={{ ...inputS, minHeight: 70, padding: 9, resize: "vertical" }} required />
              </label>
              <button type="submit" disabled={casting}
                style={{ padding: "0.7rem", background: "var(--blue-600)", color: "#fff", border: "none", borderRadius: 8, cursor: "pointer", fontWeight: 700, fontSize: 12, opacity: casting ? 0.6 : 1 }}>
                {casting ? "Casting..." : "Cast Vote"}
              </button>
            </form>
          </div>
        </div>
      </section>

      <section className="surface" style={{ overflow: "hidden" }}>
        <div className="surface-heading">
          <div><h2>Election Results</h2><p>{results.length} position results</p></div>
          {!studentOnly && (
          <div style={{ display: "flex", gap: 8 }}>
            <select value={tallyElectionId} onChange={e => setTallyElectionId(e.target.value)} style={{ minHeight: 33, padding: "0 8px", border: "1px solid var(--line)", borderRadius: 7, color: "var(--ink)", background: "var(--surface)", fontSize: 11 }}>
              <option value="">Select election...</option>
              {elections.map(e => <option key={e.id} value={e.id}>{e.title}</option>)}
            </select>
            <button onClick={tallyResults} disabled={tallying || !tallyElectionId}
              style={{ minHeight: 33, padding: "0 11px", border: "1px solid var(--blue-600)", borderRadius: 7, color: "#fff", background: "var(--blue-600)", cursor: "pointer", fontWeight: 700, fontSize: 11, opacity: tallying || !tallyElectionId ? 0.6 : 1 }}>
              {tallying ? "Tallying..." : "Tally Results"}
            </button>
          </div>
          )}
        </div>
        {results.length === 0 ? (
          <div style={{ minHeight: 200, display: "grid", placeItems: "center", alignContent: "center", gap: 8, padding: 30, color: "var(--muted)", textAlign: "center" }}>
            <Trophy style={{ width: 28, opacity: 0.5 }} />
            <strong>No results available</strong>
            <span>Select an election and tally results to view voting outcomes</span>
          </div>
        ) : (
          <div style={{ overflow: "auto" }}>
            <table style={{ width: "100%", minWidth: 600, borderCollapse: "collapse" }}>
              <thead>
                <tr style={{ background: "var(--surface-soft)" }}>
                  {["Position", "Winner", "Vote Count", "Percentage", "Certified", ""].map(h => (
                    <th key={h} style={{ padding: "10px 14px", color: "var(--muted)", fontSize: 9, fontWeight: 800, textTransform: "uppercase", textAlign: "left", borderBottom: "1px solid var(--line)" }}>{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {results.map(r => (
                  <tr key={r.position} style={{ borderBottom: "1px solid var(--line)" }}>
                    <td style={{ padding: "10px 14px", fontSize: 11, fontWeight: 600 }}>{r.position}</td>
                    <td style={{ padding: "10px 14px", fontSize: 11 }}>{r.winnerName}</td>
                    <td style={{ padding: "10px 14px", fontSize: 11, fontFamily: "monospace", fontWeight: 700 }}>{r.voteCount}</td>
                    <td style={{ padding: "10px 14px" }}>
                      <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
                        <div style={{ flex: 1, height: 5, borderRadius: 99, overflow: "hidden", background: "var(--line)", minWidth: 60 }}>
                          <div style={{ width: `${r.percentage}%`, height: "100%", borderRadius: 99, background: "var(--green)" }} />
                        </div>
                        <small style={{ fontSize: 10, fontWeight: 700 }}>{r.percentage}%</small>
                      </div>
                    </td>
                    <td style={{ padding: "10px 14px" }}>
                      <span style={{ display: "inline-flex", padding: "3px 8px", borderRadius: 99, fontSize: 10, fontWeight: 700, background: r.certified ? "var(--green-soft)" : "var(--amber-soft)", color: r.certified ? "var(--green)" : "var(--amber)" }}>{r.certified ? "Certified" : "Pending"}</span>
                    </td>
                    <td style={{ padding: "10px 14px" }}>
                      {!studentOnly && !r.certified && (
                        <button onClick={() => certifyResult(r.position)} disabled={certifyingId === r.position}
                          style={{ padding: "4px 10px", border: "1px solid #aaddc9", borderRadius: 6, color: "var(--green)", background: "var(--green-soft)", cursor: "pointer", fontSize: 10, fontWeight: 700 }}>Certify</button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </main>
    </AppShell>
  );
}
