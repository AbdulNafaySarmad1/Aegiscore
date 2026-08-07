"use client";

import { useEffect, useState } from "react";
import { apiGet, apiPost } from "@/lib/api-client";
import { usePersonalityTranslations } from "@/lib/personality-translator";
import {
  AlertCircle, Award, Calendar, CheckCircle2, ClipboardCheck, Download,
  FileText, GraduationCap, MapPin, PlusCircle, QrCode, Search, Send,
  Ticket, Upload, Users, XCircle
} from "lucide-react";

type CeremonyStatus = "Scheduled" | "InProgress" | "Completed" | "Cancelled";
type Eligibility = "Eligible" | "ConditionallyEligible" | "Ineligible";
type CertificateStatus = "Queued" | "Printing" | "Ready" | "Dispatched";
type AttendeeStatus = "Registered" | "CheckedIn" | "Absent";

type Ceremony = {
  id: string; title: string; date: string; venue: string;
  guestTickets: number; status: CeremonyStatus; attendees: number;
};

type StudentEligibility = {
  studentId: string; studentName: string; program: string;
  eligibility: Eligibility; reason: string | null;
};

type Certificate = {
  id: string; studentId: string; studentName: string; ceremonyId: string;
  certificateType: string; status: CertificateStatus; trackingNumber: string;
};

type Attendee = {
  id: string; studentId: string; studentName: string; ceremonyId: string;
  registrationTime: string; checkInTime: string | null; status: AttendeeStatus;
  seatingSection: string;
};

const eligibilityBadge = (s: Eligibility) => {
  const map: Record<Eligibility, { c: string; bg: string }> = {
    Eligible: { c: "var(--green)", bg: "var(--green-soft)" },
    ConditionallyEligible: { c: "var(--amber)", bg: "var(--amber-soft)" },
    Ineligible: { c: "var(--red)", bg: "var(--red-soft)" }
  };
  const v = map[s];
  return <span style={{ display: "inline-flex", padding: "3px 8px", borderRadius: 99, fontSize: 10, fontWeight: 700, background: v.bg, color: v.c }}>{s === "ConditionallyEligible" ? "Conditional" : s}</span>;
};

const certStatusBadge = (s: CertificateStatus) => {
  const map: Record<CertificateStatus, { c: string; bg: string }> = {
    Queued: { c: "var(--muted)", bg: "var(--surface-soft)" },
    Printing: { c: "var(--blue-600)", bg: "var(--blue-100)" },
    Ready: { c: "var(--amber)", bg: "var(--amber-soft)" },
    Dispatched: { c: "var(--green)", bg: "var(--green-soft)" }
  };
  const v = map[s];
  return <span style={{ display: "inline-flex", padding: "3px 8px", borderRadius: 99, fontSize: 10, fontWeight: 700, background: v.bg, color: v.c }}>{s}</span>;
};

const ceremonyStatusBadge = (s: CeremonyStatus) => {
  const map: Record<CeremonyStatus, { c: string; bg: string }> = {
    Scheduled: { c: "var(--blue-600)", bg: "var(--blue-100)" },
    InProgress: { c: "var(--amber)", bg: "var(--amber-soft)" },
    Completed: { c: "var(--green)", bg: "var(--green-soft)" },
    Cancelled: { c: "var(--red)", bg: "var(--red-soft)" }
  };
  const v = map[s];
  return <span style={{ display: "inline-flex", padding: "3px 8px", borderRadius: 99, fontSize: 10, fontWeight: 700, background: v.bg, color: v.c }}>{s}</span>;
};

const attendeeBadge = (s: AttendeeStatus) => {
  const map: Record<AttendeeStatus, { c: string; bg: string }> = {
    Registered: { c: "var(--muted)", bg: "var(--surface-soft)" },
    CheckedIn: { c: "var(--green)", bg: "var(--green-soft)" },
    Absent: { c: "var(--red)", bg: "var(--red-soft)" }
  };
  const v = map[s];
  return <span style={{ display: "inline-flex", padding: "3px 8px", borderRadius: 99, fontSize: 10, fontWeight: 700, background: v.bg, color: v.c }}>{s}</span>;
};

const inputS: React.CSSProperties = { minHeight: 42, padding: "0 11px", border: "1px solid var(--line)", borderRadius: 8, color: "var(--ink)", background: "var(--canvas)", fontSize: "0.85rem", width: "100%", boxSizing: "border-box" };
const labelS: React.CSSProperties = { display: "grid", gap: 5, color: "var(--muted)", fontSize: 10, fontWeight: 700 };

export default function ConvocationsPage() {
  const t = usePersonalityTranslations("academic");

  const [ceremonies, setCeremonies] = useState<Ceremony[]>([]);
  const [eligibility, setEligibility] = useState<StudentEligibility[]>([]);
  const [certificates, setCertificates] = useState<Certificate[]>([]);
  const [attendees, setAttendees] = useState<Attendee[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const [newTitle, setNewTitle] = useState("");
  const [newDate, setNewDate] = useState("");
  const [newVenue, setNewVenue] = useState("");
  const [newTickets, setNewTickets] = useState("2");
  const [creating, setCreating] = useState(false);

  const [eligibilityFilter, setEligibilityFilter] = useState("");
  const [searchStudentId, setSearchStudentId] = useState("");

  const [checkinId, setCheckinId] = useState("");
  const [checkinStudent, setCheckinStudent] = useState("");
  const [checking, setChecking] = useState(false);

  const [generatingCert, setGeneratingCert] = useState<string | null>(null);

  useEffect(() => {
    const ctrl = new AbortController();
    Promise.all([
      apiGet<Ceremony[]>("/api/convocations", ctrl.signal).catch(() => [] as Ceremony[]),
      apiGet<StudentEligibility[]>("/api/convocations/eligibility", ctrl.signal).catch(() => [] as StudentEligibility[]),
      apiGet<Certificate[]>("/api/convocations/certificates", ctrl.signal).catch(() => [] as Certificate[]),
      apiGet<Attendee[]>("/api/convocations/attendees", ctrl.signal).catch(() => [] as Attendee[])
    ]).then(([ce, el, ct, at]) => {
      setCeremonies(ce); setEligibility(el); setCertificates(ct); setAttendees(at);
      setError(null); setLoading(false);
    }).catch(() => { setError("Failed to load convocation data"); setLoading(false); });
    return () => { ctrl.abort(); };
  }, []);

  const refreshAll = () => {
    Promise.all([
      apiGet<Ceremony[]>("/api/convocations").catch(() => [] as Ceremony[]),
      apiGet<StudentEligibility[]>("/api/convocations/eligibility").catch(() => [] as StudentEligibility[]),
      apiGet<Certificate[]>("/api/convocations/certificates").catch(() => [] as Certificate[]),
      apiGet<Attendee[]>("/api/convocations/attendees").catch(() => [] as Attendee[])
    ]).then(([ce, el, ct, at]) => { setCeremonies(ce); setEligibility(el); setCertificates(ct); setAttendees(at); });
  };

  async function createCeremony(e: React.FormEvent) {
    e.preventDefault();
    if (!newTitle || !newDate || !newVenue) return;
    setCreating(true); setMessage(null);
    try {
      await apiPost("/api/convocations", { title: newTitle, date: newDate, venue: newVenue, guestTickets: Number(newTickets) });
      setMessage("success: Ceremony created");
      setNewTitle(""); setNewDate(""); setNewVenue(""); setNewTickets("2");
      refreshAll();
    } catch { setMessage("error: Failed to create ceremony"); }
    finally { setCreating(false); }
  }

  async function generateCertificate(studentId: string, ceremonyId: string) {
    setGeneratingCert(studentId);
    try {
      await apiPost("/api/convocations/certificates/generate", { studentId, ceremonyId });
      setMessage("success: Certificate generation started");
      refreshAll();
    } catch { setMessage("error: Certificate generation failed"); }
    finally { setGeneratingCert(null); }
  }

  async function checkInAttendee() {
    if (!checkinId || !checkinStudent) return;
    setChecking(true);
    try {
      await apiPost(`/api/convocations/attendees/${checkinId}/checkin`, { studentId: checkinStudent });
      setMessage("success: Check-in recorded");
      setCheckinId(""); setCheckinStudent("");
      refreshAll();
    } catch { setMessage("error: Check-in failed"); }
    finally { setChecking(false); }
  }

  const filteredEligibility = eligibilityFilter
    ? eligibility.filter(e => e.eligibility === eligibilityFilter)
    : eligibility;
  const scheduledCeremonies = ceremonies.filter(c => c.status === "Scheduled" || c.status === "InProgress");
  const nextCeremony = scheduledCeremonies[0];

  if (loading) return (
    <main style={{ minHeight: "100vh", display: "grid", placeItems: "center", gap: 16, color: "var(--muted)" }}>
      <div style={{ width: 40, height: 4, borderRadius: 99, overflow: "hidden", background: "var(--line)" }}>
        <div style={{ width: "50%", height: "100%", borderRadius: "inherit", background: "var(--blue-500)", animation: "launch 1s ease-in-out infinite alternate" }} />
      </div>
      Loading convocation management...
    </main>
  );

  if (error) return (
    <main style={{ minHeight: "100vh", display: "grid", placeItems: "center", alignContent: "center", gap: 12, color: "var(--red)" }}>
      <AlertCircle style={{ width: 32 }} />
      <strong>{error}</strong>
      <button onClick={refreshAll} style={{ minHeight: 40, padding: "0 18px", border: "1px solid var(--line)", borderRadius: 8, color: "var(--blue-600)", background: "var(--surface)", cursor: "pointer", fontWeight: 700 }}>Retry</button>
    </main>
  );

  return (
    <main style={{ minHeight: "100vh", maxWidth: 1480, margin: "0 auto", padding: "42px 34px 64px", background: "var(--canvas)" }}>
      <header style={{ display: "flex", alignItems: "end", justifyContent: "space-between", gap: 20, marginBottom: 28 }}>
        <div>
          <p style={{ margin: "0 0 7px", color: "var(--blue-600)", fontSize: 11, fontWeight: 800, letterSpacing: ".8px", textTransform: "uppercase" }}>Academic Governance</p>
          <h1 style={{ margin: 0, fontSize: "clamp(27px, 3vw, 36px)", lineHeight: 1.15, letterSpacing: "-1px" }}>{t("convocations") ?? "Convocation Management"}</h1>
          <span style={{ display: "block", marginTop: 9, color: "var(--muted)" }}>Eligibility checks, ceremony organization, certificate generation, and attendance tracking</span>
        </div>
      </header>

      {message && (
        <div style={{ display: "flex", alignItems: "center", gap: 9, padding: "11px 14px", marginBottom: 18, border: message.startsWith("error") ? "1px solid #ecc0c6" : "1px solid #aaddc9", borderRadius: 10, color: message.startsWith("error") ? "var(--red)" : "var(--green)", background: message.startsWith("error") ? "var(--red-soft)" : "var(--green-soft)", fontSize: 11 }}>
          {message.startsWith("error") ? <AlertCircle /> : <CheckCircle2 />}{message.replace(/^[^:]+: /, "")}
        </div>
      )}

      <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(180px, 1fr))", gap: 14, marginBottom: 18 }}>
        <article style={{ minHeight: 110, position: "relative", display: "grid", alignContent: "center", padding: 20, border: "1px solid var(--line)", borderRadius: 14, background: "var(--surface)", boxShadow: "var(--shadow)", overflow: "hidden" }}>
          <span style={{ color: "var(--muted)", fontSize: 11, fontWeight: 700, textTransform: "uppercase" }}>Total Ceremonies</span>
          <strong style={{ margin: "7px 0 3px", fontSize: 28 }}>{ceremonies.length}</strong>
          <small style={{ color: "var(--muted)" }}>{scheduledCeremonies.length} upcoming</small>
        </article>
        <article style={{ minHeight: 110, position: "relative", display: "grid", alignContent: "center", padding: 20, border: "1px solid var(--line)", borderRadius: 14, background: "var(--surface)", boxShadow: "var(--shadow)", overflow: "hidden" }}>
          <span style={{ color: "var(--muted)", fontSize: 11, fontWeight: 700, textTransform: "uppercase" }}>Eligible Students</span>
          <strong style={{ margin: "7px 0 3px", fontSize: 28, color: "var(--green)" }}>{eligibility.filter(e => e.eligibility === "Eligible").length}</strong>
          <small style={{ color: "var(--muted)" }}>of {eligibility.length} total</small>
        </article>
        <article style={{ minHeight: 110, position: "relative", display: "grid", alignContent: "center", padding: 20, border: "1px solid var(--line)", borderRadius: 14, background: "var(--surface)", boxShadow: "var(--shadow)", overflow: "hidden" }}>
          <span style={{ color: "var(--muted)", fontSize: 11, fontWeight: 700, textTransform: "uppercase" }}>Certificates Issued</span>
          <strong style={{ margin: "7px 0 3px", fontSize: 28, color: "var(--blue-600)" }}>{certificates.filter(c => c.status === "Dispatched").length}</strong>
          <small style={{ color: "var(--muted)" }}>{certificates.length} in pipeline</small>
        </article>
        <article style={{ minHeight: 110, position: "relative", display: "grid", alignContent: "center", padding: 20, border: "1px solid var(--line)", borderRadius: 14, background: "var(--surface)", boxShadow: "var(--shadow)", overflow: "hidden" }}>
          <span style={{ color: "var(--muted)", fontSize: 11, fontWeight: 700, textTransform: "uppercase" }}>Checked In</span>
          <strong style={{ margin: "7px 0 3px", fontSize: 28, color: "var(--green)" }}>{attendees.filter(a => a.status === "CheckedIn").length}</strong>
          <small style={{ color: "var(--muted)" }}>of {attendees.length} registered</small>
        </article>
      </div>

      <section style={{ display: "grid", gridTemplateColumns: "minmax(0, 1.4fr) minmax(380px, 0.8fr)", gap: 18, marginBottom: 18 }}>
        <div className="surface" style={{ overflow: "hidden" }}>
          <div className="surface-heading"><div><h2>Create Ceremony</h2><p>Schedule a new convocation ceremony</p></div><PlusCircle /></div>
          <form onSubmit={createCeremony} style={{ display: "grid", gap: 12, padding: 18 }}>
            <div style={{ display: "grid", gridTemplateColumns: "repeat(2, minmax(0, 1fr))", gap: 12 }}>
              <label style={labelS}>
                Ceremony Title <b style={{ color: "var(--red)" }}>*</b>
                <input placeholder="e.g. Class of 2025 Commencement" value={newTitle} onChange={e => setNewTitle(e.target.value)} style={inputS} required />
              </label>
              <label style={labelS}>
                Date <b style={{ color: "var(--red)" }}>*</b>
                <input type="date" value={newDate} onChange={e => setNewDate(e.target.value)} style={inputS} required />
              </label>
              <label style={labelS}>
                Venue <b style={{ color: "var(--red)" }}>*</b>
                <input placeholder="e.g. Jinnah Convention Centre" value={newVenue} onChange={e => setNewVenue(e.target.value)} style={inputS} required />
              </label>
              <label style={labelS}>
                Guest Tickets per Student
                <input type="number" min="1" max="5" value={newTickets} onChange={e => setNewTickets(e.target.value)} style={inputS} />
              </label>
            </div>
            <button type="submit" disabled={creating} style={{ padding: "0.7rem", background: "var(--blue-600)", color: "#fff", border: "none", borderRadius: 8, cursor: "pointer", fontWeight: 700, fontSize: 12, opacity: creating ? 0.6 : 1, justifySelf: "start", minWidth: 180 }}>
              {creating ? "Creating..." : <><Send style={{ width: 14, verticalAlign: "middle", marginRight: 6 }} />Create Ceremony</>}
            </button>
          </form>
        </div>

        <div className="surface" style={{ overflow: "hidden" }}>
          <div className="surface-heading"><div><h2>Quick Check-in</h2><p>Record student attendance at ceremony</p></div><QrCode /></div>
          <div style={{ display: "grid", gap: 12, padding: 18 }}>
            <div style={{ display: "grid", gridTemplateColumns: "repeat(2, minmax(0, 1fr))", gap: 12 }}>
              <label style={labelS}>
                Ceremony ID
                <input placeholder="e.g. CER-2025" value={checkinId} onChange={e => setCheckinId(e.target.value)} style={inputS} />
              </label>
              <label style={labelS}>
                Student ID
                <input placeholder="e.g. STU-2024-001" value={checkinStudent} onChange={e => setCheckinStudent(e.target.value)} style={inputS} />
              </label>
            </div>
            <button onClick={checkInAttendee} disabled={checking || !checkinId || !checkinStudent} style={{ padding: "0.7rem", background: "var(--green)", color: "#fff", border: "none", borderRadius: 8, cursor: "pointer", fontWeight: 700, fontSize: 12, opacity: checking ? 0.6 : 1, justifySelf: "start", minWidth: 160 }}>
              {checking ? "Checking in..." : <><ClipboardCheck style={{ width: 14, verticalAlign: "middle", marginRight: 6 }} />Check In</>}
            </button>
          </div>
        </div>
      </section>

      <section style={{ display: "grid", gridTemplateColumns: "minmax(0, 1fr) minmax(380px, 0.7fr)", gap: 18, marginBottom: 18 }}>
        <div className="surface" style={{ overflow: "hidden" }}>
          <div className="surface-heading"><div><h2>Upcoming Ceremonies</h2><p>{scheduledCeremonies.length} scheduled</p></div><Calendar /></div>
          {scheduledCeremonies.length === 0 ? (
            <div style={{ minHeight: 180, display: "grid", placeItems: "center", alignContent: "center", gap: 8, padding: 30, color: "var(--muted)", textAlign: "center" }}>
              <Calendar style={{ width: 28, color: "var(--muted)" }} />
              <strong>No upcoming ceremonies</strong>
              <span>Create a new ceremony to get started</span>
            </div>
          ) : (
            <div style={{ overflow: "auto" }}>
              <table style={{ width: "100%", minWidth: 600, borderCollapse: "collapse" }}>
                <thead>
                  <tr style={{ background: "var(--surface-soft)" }}>
                    {["Title", "Date", "Venue", "Tickets", "Attendees", "Status"].map(h => (
                      <th key={h} style={{ padding: "10px 14px", color: "var(--muted)", fontSize: 9, fontWeight: 800, textTransform: "uppercase", textAlign: "left", borderBottom: "1px solid var(--line)" }}>{h}</th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {scheduledCeremonies.map(c => (
                    <tr key={c.id} style={{ borderBottom: "1px solid var(--line)" }}>
                      <td style={{ padding: "10px 14px", fontSize: 12, fontWeight: 600 }}>{c.title}</td>
                      <td style={{ padding: "10px 14px", fontSize: 11, color: "var(--muted)" }}>{new Date(c.date).toLocaleDateString()}</td>
                      <td style={{ padding: "10px 14px", fontSize: 11, color: "var(--muted)" }}>{c.venue}</td>
                      <td style={{ padding: "10px 14px", fontSize: 11 }}>{c.guestTickets}/person</td>
                      <td style={{ padding: "10px 14px", fontSize: 11 }}>{c.attendees}</td>
                      <td style={{ padding: "10px 14px" }}>{ceremonyStatusBadge(c.status)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>

        <div className="surface" style={{ overflow: "hidden" }}>
          <div className="surface-heading"><div><h2>Certificate Pipeline</h2><p>Track generation and dispatch</p></div><Award /></div>
          {certificates.length === 0 ? (
            <div style={{ minHeight: 180, display: "grid", placeItems: "center", alignContent: "center", gap: 8, padding: 30, color: "var(--muted)", textAlign: "center" }}>
              <Award style={{ width: 28 }} />
              <strong>No certificates in pipeline</strong>
              <span>Generate certificates from the eligibility grid below</span>
            </div>
          ) : (
            <div style={{ overflow: "auto" }}>
              <table style={{ width: "100%", minWidth: 500, borderCollapse: "collapse" }}>
                <thead>
                  <tr style={{ background: "var(--surface-soft)" }}>
                    {["Student", "Type", "Status", "Tracking", "Action"].map(h => (
                      <th key={h} style={{ padding: "10px 14px", color: "var(--muted)", fontSize: 9, fontWeight: 800, textTransform: "uppercase", textAlign: "left", borderBottom: "1px solid var(--line)" }}>{h}</th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {certificates.map(ct => (
                    <tr key={ct.id} style={{ borderBottom: "1px solid var(--line)" }}>
                      <td style={{ padding: "10px 14px", fontSize: 11, fontWeight: 600 }}>{ct.studentName}</td>
                      <td style={{ padding: "10px 14px", fontSize: 11, color: "var(--muted)" }}>{ct.certificateType}</td>
                      <td style={{ padding: "10px 14px" }}>{certStatusBadge(ct.status)}</td>
                      <td style={{ padding: "10px 14px", fontSize: 10, fontFamily: "monospace", color: "var(--muted)" }}>{ct.trackingNumber || "—"}</td>
                      <td style={{ padding: "10px 14px" }}>
                        {ct.status === "Ready" && (
                          <button style={{ padding: "4px 8px", border: "1px solid var(--line)", borderRadius: 6, color: "var(--green)", background: "var(--surface)", cursor: "pointer", fontSize: 10, fontWeight: 700, display: "inline-flex", alignItems: "center", gap: 4 }}>
                            <Download style={{ width: 12 }} /> Download
                          </button>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </section>

      <section className="surface" style={{ overflow: "hidden", marginBottom: 18 }}>
        <div className="surface-heading">
          <div><h2>Eligibility Status</h2><p>{eligibility.length} students assessed</p></div>
          <div style={{ display: "flex", gap: 8 }}>
            <select value={eligibilityFilter} onChange={e => setEligibilityFilter(e.target.value)} style={{ minHeight: 33, padding: "0 9px", border: "1px solid var(--line)", borderRadius: 7, color: "var(--ink)", background: "var(--surface)", fontSize: 11 }}>
              <option value="">All Statuses</option>
              <option value="Eligible">Eligible</option>
              <option value="ConditionallyEligible">Conditionally Eligible</option>
              <option value="Ineligible">Ineligible</option>
            </select>
          </div>
        </div>
        {filteredEligibility.length === 0 ? (
          <div style={{ minHeight: 200, display: "grid", placeItems: "center", alignContent: "center", gap: 8, padding: 30, color: "var(--muted)", textAlign: "center" }}>
            <Users style={{ width: 28 }} />
            <strong>No eligibility records found</strong>
            <span>Upload student data to assess convocation eligibility</span>
          </div>
        ) : (
          <div style={{ overflow: "auto" }}>
            <table style={{ width: "100%", minWidth: 700, borderCollapse: "collapse" }}>
              <thead>
                <tr style={{ background: "var(--surface-soft)" }}>
                  {["Student ID", "Name", "Program", "Eligibility", "Reason", "Generate"].map(h => (
                    <th key={h} style={{ padding: "10px 14px", color: "var(--muted)", fontSize: 9, fontWeight: 800, textTransform: "uppercase", textAlign: "left", borderBottom: "1px solid var(--line)" }}>{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {filteredEligibility.map(e => (
                  <tr key={e.studentId} style={{ borderBottom: "1px solid var(--line)" }}>
                    <td style={{ padding: "10px 14px", fontSize: 10, fontFamily: "monospace", color: "var(--muted)" }}>{e.studentId}</td>
                    <td style={{ padding: "10px 14px", fontSize: 11, fontWeight: 600 }}>{e.studentName}</td>
                    <td style={{ padding: "10px 14px", fontSize: 11, color: "var(--muted)" }}>{e.program}</td>
                    <td style={{ padding: "10px 14px" }}>{eligibilityBadge(e.eligibility)}</td>
                    <td style={{ padding: "10px 14px", fontSize: 10, color: "var(--muted)", maxWidth: 200 }}>{e.reason || "—"}</td>
                    <td style={{ padding: "10px 14px" }}>
                      {e.eligibility !== "Ineligible" && nextCeremony && (
                        <button onClick={() => generateCertificate(e.studentId, nextCeremony.id)} disabled={generatingCert === e.studentId} style={{ padding: "4px 10px", border: "1px solid var(--line)", borderRadius: 6, color: "var(--blue-600)", background: "var(--surface)", cursor: "pointer", fontSize: 10, fontWeight: 700 }}>
                          {generatingCert === e.studentId ? "..." : "Generate"}
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <section className="surface" style={{ overflow: "hidden" }}>
        <div className="surface-heading"><div><h2>Attendance Registry</h2><p>{attendees.length} registered attendees</p></div><Users /></div>
        {attendees.length === 0 ? (
          <div style={{ minHeight: 200, display: "grid", placeItems: "center", alignContent: "center", gap: 8, padding: 30, color: "var(--muted)", textAlign: "center" }}>
            <QrCode style={{ width: 28 }} />
            <strong>No attendance records</strong>
            <span>Use the check-in form to start registering attendees</span>
          </div>
        ) : (
          <div style={{ overflow: "auto" }}>
            <table style={{ width: "100%", minWidth: 600, borderCollapse: "collapse" }}>
              <thead>
                <tr style={{ background: "var(--surface-soft)" }}>
                  {["Student", "Registration", "Check-in", "Section", "Status"].map(h => (
                    <th key={h} style={{ padding: "10px 14px", color: "var(--muted)", fontSize: 9, fontWeight: 800, textTransform: "uppercase", textAlign: "left", borderBottom: "1px solid var(--line)" }}>{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {attendees.map(a => (
                  <tr key={a.id} style={{ borderBottom: "1px solid var(--line)" }}>
                    <td style={{ padding: "10px 14px", fontSize: 11, fontWeight: 600 }}>{a.studentName}</td>
                    <td style={{ padding: "10px 14px", fontSize: 11, color: "var(--muted)" }}>{new Date(a.registrationTime).toLocaleString()}</td>
                    <td style={{ padding: "10px 14px", fontSize: 11, color: "var(--muted)" }}>{a.checkInTime ? new Date(a.checkInTime).toLocaleString() : "—"}</td>
                    <td style={{ padding: "10px 14px", fontSize: 11, color: "var(--muted)" }}>{a.seatingSection}</td>
                    <td style={{ padding: "10px 14px" }}>{attendeeBadge(a.status)}</td>
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
