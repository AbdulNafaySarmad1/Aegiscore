"use client";

import { useEffect, useMemo, useState } from "react";
import { AppShell } from "@/components/layout/AppShell";
import { apiGet, apiPost } from "@/lib/api-client";
import { BookOpenCheck, CheckCircle2, RefreshCw, UserRound, Clock3, MapPin, UsersRound } from "lucide-react";

type FacultyOption = {
  id: string;
  name: string;
  section: string;
  schedule: string;
  room: string;
  seatsRemaining: number;
};

type Offering = {
  courseId: string;
  code: string;
  title: string;
  creditHours: number;
  registered: boolean;
  selectedFacultyOptionId?: string | null;
  facultyOptions: FacultyOption[];
};

type EnrolledCourse = {
  enrollmentId: string;
  courseId: string;
  code: string;
  title: string;
  creditHours: number;
  grade?: string | null;
};

type RegistrationResponse = {
  registrationOpen: boolean;
  term: string;
  maxFacultyOptionsPerCourse: number;
  enrolledCourses: EnrolledCourse[];
  offerings: Offering[];
};

type RegisterResult = {
  success: boolean;
  message: string;
};

const pageStyle: React.CSSProperties = { maxWidth: 1480, margin: "0 auto", padding: "36px 34px 64px" };
const card: React.CSSProperties = { border: "1px solid var(--line)", borderRadius: 14, background: "var(--surface)", boxShadow: "var(--shadow)", overflow: "hidden" };

export default function Page() {
  const [data, setData] = useState<RegistrationResponse | null>(null);
  const [selected, setSelected] = useState<Record<string, string>>({});
  const [busy, setBusy] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const response = await apiGet<RegistrationResponse>("/api/student-portal/registration");
      setData(response);
      const defaults: Record<string, string> = {};
      response.offerings.forEach(o => {
        defaults[o.courseId] = o.selectedFacultyOptionId ?? o.facultyOptions[0]?.id ?? "";
      });
      setSelected(defaults);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Could not load course registration.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { void load(); }, []);

  const registeredIds = useMemo(() => new Set(data?.enrolledCourses.map(c => c.courseId) ?? []), [data]);

  const register = async (course: Offering) => {
    const facultyOptionId = selected[course.courseId];
    if (!facultyOptionId) {
      setError("Choose a faculty/section option first.");
      return;
    }

    setBusy(course.courseId);
    setError(null);
    setMessage(null);
    try {
      const result = await apiPost<RegisterResult>("/api/student-portal/registration", {
        courseId: course.courseId,
        facultyOptionId,
      });
      setMessage(result.message);
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Course registration failed.");
    } finally {
      setBusy(null);
    }
  };

  return <AppShell><main style={pageStyle}>
    <header style={{ marginBottom: 24, display: "flex", justifyContent: "space-between", gap: 20, alignItems: "end", flexWrap: "wrap" }}>
      <div>
        <p style={{ margin:"0 0 7px", color:"var(--blue-600)", fontSize:11, fontWeight:800, textTransform:"uppercase", letterSpacing:".8px" }}>Student Academic Portal</p>
        <h1 style={{ margin:0, fontSize:34, letterSpacing:"-1px" }}>Course Registration</h1>
        <p style={{ color:"var(--muted)", marginTop:8 }}>Select your next-term courses and choose from up to four available faculty/section options.</p>
      </div>
      <button onClick={() => void load()} disabled={loading} style={buttonStyle(false)}><RefreshCw size={15}/> Refresh</button>
    </header>

    {error && <div style={{ ...card, padding:16, marginBottom:16, color:"var(--red)", borderColor:"var(--red)" }}>{error}</div>}
    {message && <div style={{ ...card, padding:16, marginBottom:16, color:"var(--green)" }}>{message}</div>}

    {loading ? <div style={{ ...card, padding:30, color:"var(--muted)" }}>Loading registration catalog…</div> : <>
      <section style={{ ...card, marginBottom:22 }}>
        <div style={{ padding:"18px 20px", borderBottom:"1px solid var(--line)", display:"flex", justifyContent:"space-between", gap:16, alignItems:"center", flexWrap:"wrap" }}>
          <div style={{ display:"flex", gap:10, alignItems:"center" }}><BookOpenCheck size={20}/><div><strong>Current Registrations</strong><div style={{ color:"var(--muted)", fontSize:12 }}>Active and completed course records already attached to your student account.</div></div></div>
          <span className="badge badge-info">{data?.enrolledCourses.length ?? 0} courses</span>
        </div>
        <div style={{ overflowX:"auto" }}><table style={{ width:"100%", borderCollapse:"collapse", fontSize:13 }}>
          <thead><tr style={{ background:"var(--surface-soft)", textAlign:"left" }}><Th>Course</Th><Th>Credits</Th><Th>Status / Grade</Th></tr></thead>
          <tbody>{(data?.enrolledCourses ?? []).map(c => <tr key={c.enrollmentId} style={{ borderTop:"1px solid var(--line)" }}><Td><strong>{c.code}</strong><div style={{ color:"var(--muted)", marginTop:3 }}>{c.title}</div></Td><Td>{c.creditHours}</Td><Td><span className="badge badge-success">{c.grade ? `Completed · ${c.grade}` : "Registered"}</span></Td></tr>)}</tbody>
        </table></div>
      </section>

      <section>
        <div style={{ display:"flex", justifyContent:"space-between", alignItems:"end", gap:16, marginBottom:12, flexWrap:"wrap" }}>
          <div><h2 style={{ margin:"0 0 4px", fontSize:22 }}>Available Courses</h2><div style={{ color:"var(--muted)", fontSize:12 }}>{data?.term ?? "Upcoming term"} · maximum {data?.maxFacultyOptionsPerCourse ?? 4} faculty choices per course</div></div>
          <span className={`badge ${data?.registrationOpen ? "badge-success" : "badge-info"}`}>{data?.registrationOpen ? "Registration open" : "Registration closed"}</span>
        </div>

        <div style={{ display:"grid", gap:16 }}>
          {(data?.offerings ?? []).map(course => {
            const chosen = selected[course.courseId];
            const alreadyRegistered = course.registered || registeredIds.has(course.courseId);
            return <article key={course.courseId} style={card}>
              <div style={{ padding:"18px 20px", borderBottom:"1px solid var(--line)", display:"flex", justifyContent:"space-between", gap:16, alignItems:"center", flexWrap:"wrap" }}>
                <div><div style={{ fontSize:11, color:"var(--blue-600)", fontWeight:800, letterSpacing:".5px" }}>{course.code}</div><h3 style={{ margin:"4px 0 0", fontSize:18 }}>{course.title}</h3></div>
                <div style={{ display:"flex", gap:8, alignItems:"center" }}><span className="badge badge-info">{course.creditHours} credits</span>{alreadyRegistered && <span className="badge badge-success"><CheckCircle2 size={12}/> Registered</span>}</div>
              </div>

              <div style={{ padding:18, display:"grid", gridTemplateColumns:"repeat(auto-fit,minmax(245px,1fr))", gap:12 }}>
                {course.facultyOptions.slice(0,4).map(option => {
                  const active = chosen === option.id;
                  return <button key={option.id} type="button" onClick={() => setSelected(s => ({ ...s, [course.courseId]: option.id }))} style={{ textAlign:"left", padding:16, borderRadius:12, border:active?"2px solid var(--blue-600)":"1px solid var(--line)", background:active?"var(--blue-soft)":"var(--surface)", cursor:"pointer", color:"var(--ink)" }}>
                    <div style={{ display:"flex", justifyContent:"space-between", gap:10, alignItems:"start" }}><div style={{ display:"flex", gap:8, alignItems:"center" }}><UserRound size={17}/><strong>{option.name}</strong></div><span className="badge badge-info">Section {option.section}</span></div>
                    <div style={{ marginTop:12, display:"grid", gap:7, fontSize:12, color:"var(--muted)" }}>
                      <span style={{ display:"flex", gap:7, alignItems:"center" }}><Clock3 size={14}/>{option.schedule}</span>
                      <span style={{ display:"flex", gap:7, alignItems:"center" }}><MapPin size={14}/>{option.room}</span>
                      <span style={{ display:"flex", gap:7, alignItems:"center" }}><UsersRound size={14}/>{option.seatsRemaining} seats remaining</span>
                    </div>
                  </button>;
                })}
              </div>

              <div style={{ padding:"14px 18px", borderTop:"1px solid var(--line)", display:"flex", justifyContent:"flex-end" }}>
                <button onClick={() => void register(course)} disabled={!data?.registrationOpen || !chosen || busy === course.courseId} style={buttonStyle(true)}>
                  {busy === course.courseId ? "Saving…" : alreadyRegistered ? "Update Faculty / Section" : "Register Course"}
                </button>
              </div>
            </article>;
          })}
        </div>
      </section>
    </>}
  </main></AppShell>;
}

function Th({children}:{children:React.ReactNode}) { return <th style={{ padding:"12px 14px", whiteSpace:"nowrap", fontSize:11, color:"var(--muted)", textAlign:"left" }}>{children}</th>; }
function Td({children}:{children:React.ReactNode}) { return <td style={{ padding:"12px 14px", verticalAlign:"top" }}>{children}</td>; }
function buttonStyle(primary:boolean): React.CSSProperties { return { display:"inline-flex", alignItems:"center", gap:7, borderRadius:9, padding:"10px 14px", border:primary?"1px solid var(--blue-600)":"1px solid var(--line)", background:primary?"var(--blue-600)":"var(--surface)", color:primary?"white":"var(--ink)", fontWeight:700, fontSize:12, cursor:"pointer" }; }
