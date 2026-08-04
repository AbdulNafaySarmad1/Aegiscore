"use client";

import { useEffect, useState } from "react";
import { apiGet, apiPost } from "@/lib/api-client";
import { useTranslations } from "next-intl";
import {
  AlertCircle, Award, CheckCircle2, DollarSign, FileText, GraduationCap,
  MapPin, Search, Send, Star, Upload, UserCheck, UserPlus, Users
} from "lucide-react";

type SlotMatch = {
  id: string; facultyId: string; facultyName: string; courseName: string;
  compositeScore: number; rank: number; matchRationale: string;
};

type Faculty = {
  id: string; name: string; cnic: string; email: string;
  qualification: string; specializations: string[];
  compensation: number; hecApproved: boolean; rating: number;
  totalAssignments: number;
};

type Contract = {
  id: string; facultyId: string; facultyName: string; courseName: string;
  creditHours: number; compensation: number; startDate: string; endDate: string;
  status: "Draft" | "Active" | "Completed" | "Terminated";
};

const hecBadge = (approved: boolean) => (
  <span style={{ display: "inline-flex", padding: "3px 8px", borderRadius: 99, fontSize: 10, fontWeight: 700, background: approved ? "var(--green-soft)" : "var(--amber-soft)", color: approved ? "var(--green)" : "var(--amber)" }}>
    {approved ? "HEC Approved" : "Pending HEC"}
  </span>
);

const contractBadge = (s: string) => {
  const map: Record<string, { c: string; bg: string }> = {
    Draft: { c: "var(--muted)", bg: "var(--surface-soft)" },
    Active: { c: "var(--green)", bg: "var(--green-soft)" },
    Completed: { c: "var(--blue-600)", bg: "var(--blue-100)" },
    Terminated: { c: "var(--red)", bg: "var(--red-soft)" }
  };
  const v = map[s] ?? { c: "var(--muted)", bg: "var(--surface-soft)" };
  return <span style={{ display: "inline-flex", padding: "3px 8px", borderRadius: 99, fontSize: 10, fontWeight: 700, background: v.bg, color: v.c }}>{s}</span>;
};

const inputS: React.CSSProperties = { minHeight: 42, padding: "0 11px", border: "1px solid var(--line)", borderRadius: 8, color: "var(--ink)", background: "var(--canvas)", fontSize: "0.85rem", width: "100%", boxSizing: "border-box" };
const labelS: React.CSSProperties = { display: "grid", gap: 5, color: "var(--muted)", fontSize: 10, fontWeight: 700 };

export default function VisitingFacultyPage() {
  const t = useTranslations("academic");

  const [faculty, setFaculty] = useState<Faculty[]>([]);
  const [matches, setMatches] = useState<SlotMatch[]>([]);
  const [contracts, setContracts] = useState<Contract[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const [name, setName] = useState("");
  const [cnic, setCnic] = useState("");
  const [email, setEmail] = useState("");
  const [qualification, setQualification] = useState("");
  const [specializations, setSpecializations] = useState("");
  const [compensation, setCompensation] = useState("");
  const [registering, setRegistering] = useState(false);

  const [courseName, setCourseName] = useState("");
  const [matchingCourse, setMatchingCourse] = useState("");
  const [creditHours, setCreditHours] = useState("3");
  const [contractFaculty, setContractFaculty] = useState("");
  const [contractStart, setContractStart] = useState("");
  const [contractEnd, setContractEnd] = useState("");
  const [contractCompensation, setContractCompensation] = useState("");
  const [generatingContract, setGeneratingContract] = useState(false);

  const [ratingFaculty, setRatingFaculty] = useState("");
  const [ratingScore, setRatingScore] = useState("3");
  const [submittingRating, setSubmittingRating] = useState(false);

  useEffect(() => {
    const ctrl = new AbortController();
    Promise.all([
      apiGet<Faculty[]>("/api/visiting-faculty/list", ctrl.signal).catch(() => [] as Faculty[]),
      apiGet<SlotMatch[]>("/api/visiting-faculty/matches", ctrl.signal).catch(() => [] as SlotMatch[]),
      apiGet<Contract[]>("/api/visiting-faculty/contracts", ctrl.signal).catch(() => [] as Contract[])
    ]).then(([fc, mt, ct]) => {
      setFaculty(fc); setMatches(mt); setContracts(ct);
      setError(null); setLoading(false);
    }).catch(() => { setError("Failed to load visiting faculty data"); setLoading(false); });
    return () => { ctrl.abort(); };
  }, []);

  const refresh = () => {
    Promise.all([
      apiGet<Faculty[]>("/api/visiting-faculty/list").catch(() => [] as Faculty[]),
      apiGet<SlotMatch[]>("/api/visiting-faculty/matches").catch(() => [] as SlotMatch[]),
      apiGet<Contract[]>("/api/visiting-faculty/contracts").catch(() => [] as Contract[])
    ]).then(([fc, mt, ct]) => { setFaculty(fc); setMatches(mt); setContracts(ct); });
  };

  async function registerFaculty(e: React.FormEvent) {
    e.preventDefault();
    if (!name || !cnic || !email) return;
    setRegistering(true); setMessage(null);
    try {
      await apiPost("/api/visiting-faculty/register", {
        name, cnic, email, qualification,
        specializations: specializations.split(",").map(s => s.trim()).filter(Boolean),
        compensation: Number(compensation)
      });
      setMessage("success: Faculty registered");
      setName(""); setCnic(""); setEmail(""); setQualification(""); setSpecializations(""); setCompensation("");
      refresh();
    } catch { setMessage("error: Registration failed"); }
    finally { setRegistering(false); }
  }

  async function generateContract(e: React.FormEvent) {
    e.preventDefault();
    if (!contractFaculty || !matchingCourse || !contractStart || !contractEnd) return;
    setGeneratingContract(true); setMessage(null);
    try {
      await apiPost("/api/visiting-faculty/contracts", {
        facultyId: contractFaculty, courseName: matchingCourse,
        creditHours: Number(creditHours), compensation: Number(contractCompensation),
        startDate: contractStart, endDate: contractEnd
      });
      setMessage("success: Contract generated");
      setContractFaculty(""); setMatchingCourse(""); setContractStart(""); setContractEnd(""); setContractCompensation("");
      refresh();
    } catch { setMessage("error: Contract generation failed"); }
    finally { setGeneratingContract(false); }
  }

  async function submitRating(e: React.FormEvent) {
    e.preventDefault();
    if (!ratingFaculty) return;
    setSubmittingRating(true); setMessage(null);
    try {
      await apiPost(`/api/visiting-faculty/${ratingFaculty}/rate`, { score: Number(ratingScore) });
      setMessage("success: Rating submitted");
      setRatingFaculty(""); setRatingScore("3");
      refresh();
    } catch { setMessage("error: Rating submission failed"); }
    finally { setSubmittingRating(false); }
  }

  if (loading) return (
    <main style={{ minHeight: "100vh", display: "grid", placeItems: "center", gap: 16, color: "var(--muted)" }}>
      <div style={{ width: 40, height: 4, borderRadius: 99, overflow: "hidden", background: "var(--line)" }}>
        <div style={{ width: "50%", height: "100%", borderRadius: "inherit", background: "var(--blue-500)", animation: "launch 1s ease-in-out infinite alternate" }} />
      </div>
      Loading visiting faculty marketplace...
    </main>
  );

  if (error) return (
    <main style={{ minHeight: "100vh", display: "grid", placeItems: "center", alignContent: "center", gap: 12, color: "var(--red)" }}>
      <AlertCircle style={{ width: 32 }} />
      <strong>{error}</strong>
      <button onClick={refresh} style={{ minHeight: 40, padding: "0 18px", border: "1px solid var(--line)", borderRadius: 8, color: "var(--blue-600)", background: "var(--surface)", cursor: "pointer", fontWeight: 700 }}>Retry</button>
    </main>
  );

  return (
    <main style={{ minHeight: "100vh", maxWidth: 1480, margin: "0 auto", padding: "42px 34px 64px", background: "var(--canvas)" }}>
      <header style={{ display: "flex", alignItems: "end", justifyContent: "space-between", gap: 20, marginBottom: 28 }}>
        <div>
          <p style={{ margin: "0 0 7px", color: "var(--blue-600)", fontSize: 11, fontWeight: 800, letterSpacing: ".8px", textTransform: "uppercase" }}>Academic Operations</p>
          <h1 style={{ margin: 0, fontSize: "clamp(27px, 3vw, 36px)", lineHeight: 1.15, letterSpacing: "-1px" }}>{t("visitingFaculty") ?? "Visiting Faculty Marketplace"}</h1>
          <span style={{ display: "block", marginTop: 9, color: "var(--muted)" }}>Register, match, contract, and evaluate visiting faculty</span>
        </div>
      </header>

      {message && (
        <div style={{ display: "flex", alignItems: "center", gap: 9, padding: "11px 14px", marginBottom: 18, border: message.startsWith("error") ? "1px solid #ecc0c6" : "1px solid #aaddc9", borderRadius: 10, color: message.startsWith("error") ? "var(--red)" : "var(--green)", background: message.startsWith("error") ? "var(--red-soft)" : "var(--green-soft)", fontSize: 11 }}>
          {message.startsWith("error") ? <AlertCircle /> : <CheckCircle2 />}{message.replace(/^[^:]+: /, "")}
        </div>
      )}

      <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(180px, 1fr))", gap: 14, marginBottom: 18 }}>
        <article style={{ minHeight: 110, display: "grid", alignContent: "center", padding: 20, border: "1px solid var(--line)", borderRadius: 14, background: "var(--surface)", boxShadow: "var(--shadow)" }}>
          <span style={{ color: "var(--muted)", fontSize: 11, fontWeight: 700, textTransform: "uppercase" }}>Registered Faculty</span>
          <strong style={{ margin: "7px 0 3px", fontSize: 28 }}>{faculty.length}</strong>
          <small style={{ color: "var(--muted)" }}>{faculty.filter(f => f.hecApproved).length} HEC approved</small>
        </article>
        <article style={{ minHeight: 110, display: "grid", alignContent: "center", padding: 20, border: "1px solid var(--line)", borderRadius: 14, background: "var(--surface)", boxShadow: "var(--shadow)" }}>
          <span style={{ color: "var(--muted)", fontSize: 11, fontWeight: 700, textTransform: "uppercase" }}>Slot Matches</span>
          <strong style={{ margin: "7px 0 3px", fontSize: 28, color: "var(--blue-600)" }}>{matches.length}</strong>
          <small style={{ color: "var(--muted)" }}>potential assignments</small>
        </article>
        <article style={{ minHeight: 110, display: "grid", alignContent: "center", padding: 20, border: "1px solid var(--line)", borderRadius: 14, background: "var(--surface)", boxShadow: "var(--shadow)" }}>
          <span style={{ color: "var(--muted)", fontSize: 11, fontWeight: 700, textTransform: "uppercase" }}>Active Contracts</span>
          <strong style={{ margin: "7px 0 3px", fontSize: 28, color: "var(--green)" }}>{contracts.filter(c => c.status === "Active").length}</strong>
          <small style={{ color: "var(--muted)" }}>{contracts.length} total contracts</small>
        </article>
        <article style={{ minHeight: 110, display: "grid", alignContent: "center", padding: 20, border: "1px solid var(--line)", borderRadius: 14, background: "var(--surface)", boxShadow: "var(--shadow)" }}>
          <span style={{ color: "var(--muted)", fontSize: 11, fontWeight: 700, textTransform: "uppercase" }}>Avg Rating</span>
          <strong style={{ margin: "7px 0 3px", fontSize: 28, color: "var(--amber)" }}>
            {faculty.length > 0 ? (faculty.reduce((s, f) => s + f.rating, 0) / faculty.length).toFixed(1) : "—"}/5
          </strong>
          <small style={{ color: "var(--muted)" }}>{faculty.filter(f => f.rating >= 4).length} top-rated</small>
        </article>
      </div>

      <section style={{ display: "grid", gridTemplateColumns: "minmax(0, 1.3fr) minmax(380px, 0.8fr)", gap: 18, marginBottom: 18 }}>
        <div className="surface" style={{ overflow: "hidden" }}>
          <div className="surface-heading"><div><h2>Faculty Registration</h2><p>Register a new visiting faculty member</p></div><UserPlus /></div>
          <form onSubmit={registerFaculty} style={{ display: "grid", gap: 12, padding: 18 }}>
            <div style={{ display: "grid", gridTemplateColumns: "repeat(2, minmax(0, 1fr))", gap: 12 }}>
              <label style={labelS}>
                Full Name <b style={{ color: "var(--red)" }}>*</b>
                <input placeholder="e.g. Dr. Ali Khan" value={name} onChange={e => setName(e.target.value)} style={inputS} required />
              </label>
              <label style={labelS}>
                CNIC <b style={{ color: "var(--red)" }}>*</b>
                <input placeholder="e.g. 35202-1234567-1" value={cnic} onChange={e => setCnic(e.target.value)} style={inputS} required />
              </label>
              <label style={labelS}>
                Email <b style={{ color: "var(--red)" }}>*</b>
                <input type="email" placeholder="e.g. ali.khan@example.com" value={email} onChange={e => setEmail(e.target.value)} style={inputS} required />
              </label>
              <label style={labelS}>
                Qualification <b style={{ color: "var(--red)" }}>*</b>
                <input placeholder="e.g. Ph.D. Computer Science" value={qualification} onChange={e => setQualification(e.target.value)} style={inputS} required />
              </label>
              <label style={labelS}>
                Specializations (comma-separated)
                <input placeholder="e.g. AI, Machine Learning" value={specializations} onChange={e => setSpecializations(e.target.value)} style={inputS} />
              </label>
              <label style={labelS}>
                Hourly Rate (PKR)
                <input type="number" placeholder="e.g. 5000" value={compensation} onChange={e => setCompensation(e.target.value)} style={inputS} />
              </label>
            </div>
            <button type="submit" disabled={registering} style={{ padding: "0.7rem", background: "var(--blue-600)", color: "#fff", border: "none", borderRadius: 8, cursor: "pointer", fontWeight: 700, fontSize: 12, opacity: registering ? 0.6 : 1, justifySelf: "start", minWidth: 180 }}>
              {registering ? "Registering..." : <><UserPlus style={{ width: 14, verticalAlign: "middle", marginRight: 6 }} />Register Faculty</>}
            </button>
          </form>
        </div>

        <div style={{ display: "grid", gap: 18, alignContent: "start" }}>
          <div className="surface" style={{ overflow: "hidden" }}>
            <div className="surface-heading"><div><h2>Contract Generation</h2><p>Create a teaching contract</p></div><FileText /></div>
            <form onSubmit={generateContract} style={{ display: "grid", gap: 12, padding: 18 }}>
              <label style={labelS}>
                Faculty <b style={{ color: "var(--red)" }}>*</b>
                <select value={contractFaculty} onChange={e => setContractFaculty(e.target.value)} style={inputS} required>
                  <option value="">Select faculty...</option>
                  {faculty.map(f => <option key={f.id} value={f.id}>{f.name}</option>)}
                </select>
              </label>
              <div style={{ display: "grid", gridTemplateColumns: "repeat(2, minmax(0, 1fr))", gap: 12 }}>
                <label style={labelS}>
                  Course <b style={{ color: "var(--red)" }}>*</b>
                  <input placeholder="e.g. CS-401" value={matchingCourse} onChange={e => setMatchingCourse(e.target.value)} style={inputS} required />
                </label>
                <label style={labelS}>
                  Credit Hours <b style={{ color: "var(--red)" }}>*</b>
                  <input type="number" value={creditHours} onChange={e => setCreditHours(e.target.value)} style={inputS} required />
                </label>
              </div>
              <div style={{ display: "grid", gridTemplateColumns: "repeat(2, minmax(0, 1fr))", gap: 12 }}>
                <label style={labelS}>
                  Start Date <b style={{ color: "var(--red)" }}>*</b>
                  <input type="date" value={contractStart} onChange={e => setContractStart(e.target.value)} style={inputS} required />
                </label>
                <label style={labelS}>
                  End Date <b style={{ color: "var(--red)" }}>*</b>
                  <input type="date" value={contractEnd} onChange={e => setContractEnd(e.target.value)} style={inputS} required />
                </label>
              </div>
              <label style={labelS}>
                Compensation (PKR)
                <input type="number" placeholder="e.g. 150000" value={contractCompensation} onChange={e => setContractCompensation(e.target.value)} style={inputS} />
              </label>
              <button type="submit" disabled={generatingContract} style={{ padding: "0.7rem", background: "var(--green)", color: "#fff", border: "none", borderRadius: 8, cursor: "pointer", fontWeight: 700, fontSize: 12, opacity: generatingContract ? 0.6 : 1 }}>
                {generatingContract ? "Generating..." : <><FileText style={{ width: 14, verticalAlign: "middle", marginRight: 6 }} />Generate Contract</>}
              </button>
            </form>
          </div>

          <div className="surface" style={{ overflow: "hidden" }}>
            <div className="surface-heading"><div><h2>Performance Rating</h2><p>Rate a visiting faculty member</p></div><Star /></div>
            <form onSubmit={submitRating} style={{ display: "grid", gap: 12, padding: 18 }}>
              <label style={labelS}>
                Faculty <b style={{ color: "var(--red)" }}>*</b>
                <select value={ratingFaculty} onChange={e => setRatingFaculty(e.target.value)} style={inputS} required>
                  <option value="">Select faculty...</option>
                  {faculty.map(f => <option key={f.id} value={f.id}>{f.name}</option>)}
                </select>
              </label>
              <label style={labelS}>
                Rating (1-5)
                <div style={{ display: "flex", gap: 6, alignItems: "center" }}>
                  {[1, 2, 3, 4, 5].map(n => (
                    <button key={n} type="button" onClick={() => setRatingScore(String(n))}
                      style={{ width: 38, height: 38, display: "grid", placeItems: "center", border: `2px solid ${Number(ratingScore) >= n ? "var(--amber)" : "var(--line)"}`, borderRadius: 8, background: Number(ratingScore) >= n ? "var(--amber-soft)" : "var(--surface)", cursor: "pointer", fontSize: 14, fontWeight: 700, color: Number(ratingScore) >= n ? "var(--amber)" : "var(--muted)" }}>
                      {n}
                    </button>
                  ))}
                </div>
              </label>
              <button type="submit" disabled={submittingRating} style={{ padding: "0.7rem", background: "var(--amber)", color: "#fff", border: "none", borderRadius: 8, cursor: "pointer", fontWeight: 700, fontSize: 12, opacity: submittingRating ? 0.6 : 1 }}>
                {submittingRating ? "Submitting..." : <><Star style={{ width: 14, verticalAlign: "middle", marginRight: 6 }} />Submit Rating</>}
              </button>
            </form>
          </div>
        </div>
      </section>

      <section className="surface" style={{ overflow: "hidden", marginBottom: 18 }}>
        <div className="surface-heading"><div><h2>Slot Matching Results</h2><p>{matches.length} potential matches</p></div><Search /></div>
        {matches.length === 0 ? (
          <div style={{ minHeight: 180, display: "grid", placeItems: "center", alignContent: "center", gap: 8, padding: 30, color: "var(--muted)", textAlign: "center" }}>
            <Search style={{ width: 28 }} />
            <strong>No slot matches found</strong>
            <span>Register faculty and create course slots to get matching results</span>
          </div>
        ) : (
          <div style={{ overflow: "auto" }}>
            <table style={{ width: "100%", minWidth: 700, borderCollapse: "collapse" }}>
              <thead>
                <tr style={{ background: "var(--surface-soft)" }}>
                  {["Rank", "Faculty", "Course", "Score", "Rationale"].map(h => (
                    <th key={h} style={{ padding: "10px 14px", color: "var(--muted)", fontSize: 9, fontWeight: 800, textTransform: "uppercase", textAlign: "left", borderBottom: "1px solid var(--line)" }}>{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {matches.map(m => (
                  <tr key={m.id} style={{ borderBottom: "1px solid var(--line)" }}>
                    <td style={{ padding: "10px 14px", fontSize: 14, fontWeight: 800, color: m.rank <= 3 ? "var(--green)" : "var(--muted)" }}>#{m.rank}</td>
                    <td style={{ padding: "10px 14px", fontSize: 11, fontWeight: 600 }}>{m.facultyName}</td>
                    <td style={{ padding: "10px 14px", fontSize: 11, color: "var(--muted)" }}>{m.courseName}</td>
                    <td style={{ padding: "10px 14px" }}>
                      <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
                        <div style={{ width: 80, height: 5, borderRadius: 99, background: "var(--line)", overflow: "hidden" }}>
                          <div style={{ height: "100%", borderRadius: "inherit", background: m.compositeScore >= 0.7 ? "var(--green)" : m.compositeScore >= 0.4 ? "var(--amber)" : "var(--red)", width: `${Math.round(m.compositeScore * 100)}%` }} />
                        </div>
                        <span style={{ fontSize: 11, fontWeight: 700 }}>{Math.round(m.compositeScore * 100)}%</span>
                      </div>
                    </td>
                    <td style={{ padding: "10px 14px", fontSize: 10, color: "var(--muted)", maxWidth: 250 }}>{m.matchRationale}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <section className="surface" style={{ overflow: "hidden" }}>
        <div className="surface-heading"><div><h2>Eligible Faculty List</h2><p>{faculty.length} registered faculty</p></div><GraduationCap /></div>
        {faculty.length === 0 ? (
          <div style={{ minHeight: 200, display: "grid", placeItems: "center", alignContent: "center", gap: 8, padding: 30, color: "var(--muted)", textAlign: "center" }}>
            <Users style={{ width: 28 }} />
            <strong>No faculty registered</strong>
            <span>Use the registration form to add visiting faculty</span>
          </div>
        ) : (
          <div style={{ overflow: "auto" }}>
            <table style={{ width: "100%", minWidth: 800, borderCollapse: "collapse" }}>
              <thead>
                <tr style={{ background: "var(--surface-soft)" }}>
                  {["Name", "CNIC", "Qualification", "Specializations", "HEC Status", "Rate/hr", "Rating", "Assignments"].map(h => (
                    <th key={h} style={{ padding: "10px 14px", color: "var(--muted)", fontSize: 9, fontWeight: 800, textTransform: "uppercase", textAlign: "left", borderBottom: "1px solid var(--line)" }}>{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {faculty.map(f => (
                  <tr key={f.id} style={{ borderBottom: "1px solid var(--line)" }}>
                    <td style={{ padding: "10px 14px", fontSize: 11, fontWeight: 600 }}>{f.name}</td>
                    <td style={{ padding: "10px 14px", fontSize: 10, fontFamily: "monospace", color: "var(--muted)" }}>{f.cnic}</td>
                    <td style={{ padding: "10px 14px", fontSize: 11, color: "var(--muted)" }}>{f.qualification}</td>
                    <td style={{ padding: "10px 14px", fontSize: 10, color: "var(--muted)" }}>{f.specializations.join(", ")}</td>
                    <td style={{ padding: "10px 14px" }}>{hecBadge(f.hecApproved)}</td>
                    <td style={{ padding: "10px 14px", fontSize: 11, fontWeight: 600 }}>PKR {f.compensation.toLocaleString()}</td>
                    <td style={{ padding: "10px 14px", fontSize: 11, fontWeight: 600, color: f.rating >= 4 ? "var(--green)" : f.rating >= 3 ? "var(--amber)" : "var(--muted)" }}>{f.rating}/5</td>
                    <td style={{ padding: "10px 14px", fontSize: 11 }}>{f.totalAssignments}</td>
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
