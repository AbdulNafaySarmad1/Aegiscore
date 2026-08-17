"use client";

import { useEffect, useMemo, useState } from "react";
import { AppShell } from "@/components/layout/AppShell";
import { apiGet } from "@/lib/api-client";
import { BookOpenCheck, CheckCircle2, FileSearch, Map as MapIcon, MessageSquareText } from "lucide-react";

type Mode = "courses" | "workflow" | "roadmap" | "outcomes" | "survey";
type Profile = {
  name?: string; program?: string; currentSemester?: string; cgpa?: string;
  creditsCompleted?: string; totalCredits?: string; enrollmentStatus?: string; universityId?: string;
};
type Transcript = {
  cgpa: number;
  semesters: Array<{ semesterIndex: number; sgpa: number; courses: Array<{ courseId: string; courseTitle: string; letterGrade: string; gradePoints: number; percentageObtained?: number }> }>;
};
type Timetable = { entries: Array<{ courseId: string; courseTitle: string; courseCode: string; dayName: string; startTime: string; endTime: string; room: string }> };
type ObeAttainment = {
  mappingSource: string;
  threshold: number;
  ploLabels: string[];
  overall: Record<string, number | null>;
  courses: Array<{ courseId:string; courseCode?:string; courseTitle:string; creditHours:number; percentage:number; letterGrade?:string; mappedClos:string[]; ploAttainment:Record<string,number> }>;
};

const pageStyle: React.CSSProperties = { maxWidth: 1480, margin: "0 auto", padding: "36px 34px 64px" };
const card: React.CSSProperties = { border: "1px solid var(--line)", borderRadius: 14, background: "var(--surface)", boxShadow: "var(--shadow)", overflow: "hidden" };
const titleMap: Record<Mode, [string,string]> = {
  courses: ["My Courses", "Course registrations, class information, timings and academic status."],
  workflow: ["Academic Workflow", "Your student lifecycle milestones and university requests."],
  roadmap: ["Degree Road Map", "Program progress, completed credits and the path to graduation."],
  outcomes: ["Student PLO Attainment", "Outcome-based education view across your completed courses."],
  survey: ["Online Survey", "Course and faculty feedback assigned to you by the university."],
};

export function StudentGeneralPage({ mode }: { mode: Mode }) {
  const [profile, setProfile] = useState<Profile | null>(null);
  const [transcript, setTranscript] = useState<Transcript | null>(null);
  const [timetable, setTimetable] = useState<Timetable | null>(null);
  const [obe, setObe] = useState<ObeAttainment | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    Promise.all([
      apiGet<Profile>("/api/student-portal/me").catch(() => null),
      apiGet<Transcript>("/api/student-portal/transcript").catch(() => null),
      apiGet<Timetable>("/api/student-portal/my-timetable").catch(() => null),
      mode === "outcomes" ? apiGet<ObeAttainment>("/api/student-portal/obe/attainment").catch(() => null) : Promise.resolve(null),
    ]).then(([p,t,tt,o]) => { setProfile(p); setTranscript(t); setTimetable(tt); setObe(o); }).finally(() => setLoading(false));
  }, [mode]);

  const courses = useMemo(() => {
    const seen = new Map<string, { id:string; title:string; grade?:string; points?:number; percentage?:number; semester?:number }>();
    transcript?.semesters.forEach(s => s.courses.forEach(c => seen.set(c.courseId, { id:c.courseId, title:c.courseTitle, grade:c.letterGrade, points:c.gradePoints, percentage:c.percentageObtained, semester:s.semesterIndex })));
    timetable?.entries.forEach(c => { if (!seen.has(c.courseId)) seen.set(c.courseId, { id:c.courseId, title:c.courseTitle }); });
    return [...seen.values()];
  }, [transcript, timetable]);

  const [title, subtitle] = titleMap[mode];

  return <AppShell><main style={pageStyle}>
    <header style={{ marginBottom: 24 }}><p style={{ margin:"0 0 7px", color:"var(--blue-600)", fontSize:11, fontWeight:800, textTransform:"uppercase", letterSpacing:".8px" }}>Student Academic Portal</p><h1 style={{ margin:0, fontSize:34, letterSpacing:"-1px" }}>{title}</h1><p style={{ color:"var(--muted)", marginTop:8 }}>{subtitle}</p></header>
    {loading ? <div style={{ ...card, padding:30, color:"var(--muted)" }}>Loading live academic data…</div> : <Content mode={mode} profile={profile} transcript={transcript} timetable={timetable} courses={courses} obe={obe} />}
  </main></AppShell>;
}

function Content({ mode, profile, transcript, timetable, courses, obe }: { mode:Mode; profile:Profile|null; transcript:Transcript|null; timetable:Timetable|null; courses:Array<{id:string;title:string;grade?:string;points?:number;percentage?:number;semester?:number}>; obe:ObeAttainment|null }) {
  if (mode === "courses") {
    return <div style={card}><div style={{ padding:"18px 20px", borderBottom:"1px solid var(--line)", display:"flex", gap:10, alignItems:"center" }}><BookOpenCheck size={20}/><strong>Registered & Completed Courses</strong></div><div style={{ overflowX:"auto" }}><table style={{ width:"100%", borderCollapse:"collapse", fontSize:13 }}><thead><tr style={{ background:"var(--surface-soft)", textAlign:"left" }}><Th>Course</Th><Th>Semester</Th><Th>Grade</Th><Th>Class Schedule</Th></tr></thead><tbody>{courses.map(c => { const slots = timetable?.entries.filter(t=>t.courseId===c.id) ?? []; return <tr key={c.id} style={{ borderTop:"1px solid var(--line)" }}><Td><strong>{c.title}</strong></Td><Td>{c.semester ? `Semester ${c.semester}` : "Current"}</Td><Td>{c.grade ?? "In progress"}</Td><Td>{slots.length ? slots.map(s=>`${s.dayName} ${s.startTime}-${s.endTime} · ${s.room}`).join(" | ") : "—"}</Td></tr>; })}</tbody></table></div></div>;
  }
  if (mode === "workflow") {
    const completed = Number(profile?.creditsCompleted ?? 0); const total = Number(profile?.totalCredits ?? 130); const progress = total ? Math.round(completed/total*100) : 0;
    const steps = [
      ["Admission & Identity", true, profile?.universityId ?? "Student record active"],
      ["Course Registration", courses.length > 0, `${courses.length} course records available`],
      ["Academic Assessment", (transcript?.semesters.length ?? 0) > 0, `${transcript?.semesters.length ?? 0} semester result set(s)`],
      ["OBE Attainment", (transcript?.semesters.length ?? 0) > 0, "CLO/PLO evidence generated from published academic results"],
      ["Degree Completion", progress >= 100, `${progress}% credit progress`],
    ];
    return <div style={{ display:"grid", gap:12 }}>{steps.map(([name,done,desc],i)=><div key={String(name)} style={{ ...card, padding:18, display:"grid", gridTemplateColumns:"44px 1fr auto", gap:14, alignItems:"center" }}><div style={{ width:34,height:34,borderRadius:"50%",display:"grid",placeItems:"center",background:done?"var(--green-soft)":"var(--surface-soft)",color:done?"var(--green)":"var(--muted)" }}>{done?<CheckCircle2 size={19}/>:i+1}</div><div><strong>{name}</strong><div style={{ color:"var(--muted)", fontSize:12, marginTop:3 }}>{desc}</div></div><span className={`badge ${done?"badge-success":"badge-info"}`}>{done?"Complete":"In progress"}</span></div>)}</div>;
  }
  if (mode === "roadmap") {
    const completed = Number(profile?.creditsCompleted ?? 0); const total = Number(profile?.totalCredits ?? 130); const progress = total ? Math.round(completed/total*100) : 0;
    return <div style={{ display:"grid", gap:18 }}><div style={{ ...card, padding:22 }}><div style={{ display:"flex", justifyContent:"space-between", gap:20, alignItems:"end" }}><div><MapIcon size={22}/><h2 style={{ margin:"10px 0 3px" }}>{profile?.program ?? "Degree Program"}</h2><span style={{ color:"var(--muted)" }}>Current semester: {profile?.currentSemester ?? "—"}</span></div><strong style={{ fontSize:28 }}>{progress}%</strong></div><div style={{ height:10,borderRadius:99,background:"var(--line)",marginTop:18,overflow:"hidden" }}><div style={{ width:`${Math.min(100,progress)}%`,height:"100%",background:"var(--blue-600)" }}/></div><p style={{ color:"var(--muted)", fontSize:12 }}>{completed} of {total} credits completed · CGPA {profile?.cgpa ?? transcript?.cgpa ?? "—"}</p></div></div>;
  }
  if (mode === "survey") {
    return <div style={{ ...card, padding:32, textAlign:"center" }}><MessageSquareText size={34} style={{ opacity:.45 }}/><h2>No active surveys</h2><p style={{ color:"var(--muted)", maxWidth:560, margin:"8px auto" }}>When a course or faculty feedback survey is assigned to your account, it will appear here with its session, course, faculty, due dates and completion status.</p></div>;
  }

  const plos = obe?.ploLabels ?? Array.from({length:12},(_,i)=>`PLO${i+1}`);
  const threshold = obe?.threshold ?? 50;
  return <div style={{ display:"grid", gap:18 }}>
    <div style={{ ...card, padding:18, display:"flex", justifyContent:"space-between", gap:16, flexWrap:"wrap" }}><div><strong>{profile?.name ?? "Student"}</strong><div style={{ color:"var(--muted)",fontSize:12 }}>{profile?.universityId ?? "—"} · {profile?.program ?? "Program"}</div></div><div><strong>CGPA {profile?.cgpa ?? transcript?.cgpa ?? "—"}</strong><div style={{color:"var(--muted)",fontSize:12}}>Student-level attainment view</div></div></div>
    <div style={{ display:"grid", gridTemplateColumns:"repeat(6,minmax(110px,1fr))", gap:10 }}>{plos.map(p=>{const v=obe?.overall?.[p] ?? null; return <div key={p} style={{...card,padding:14}}><div style={{fontSize:11,color:"var(--muted)",fontWeight:700}}>{p}</div><div style={{fontSize:22,fontWeight:800,color:v!==null&&v<threshold?"var(--red)":"var(--ink)",marginTop:5}}>{v===null?"—":`${v}%`}</div></div>})}</div>
    <div style={card}><div style={{ padding:"18px 20px", borderBottom:"1px solid var(--line)", display:"flex", gap:10, alignItems:"center" }}><FileSearch size={20}/><div><strong>Course-to-PLO Attainment Matrix</strong><div style={{ color:"var(--muted)",fontSize:11 }}>Backend CLO→PLO mappings plus published student results. Values below {threshold}% are highlighted.</div></div></div><div style={{ overflowX:"auto" }}><table style={{ minWidth:1450,width:"100%",borderCollapse:"collapse",fontSize:12 }}><thead><tr style={{background:"var(--surface-soft)"}}><Th>Course</Th><Th>Credits</Th><Th>CLOs</Th>{plos.map(p=><Th key={p}>{p}</Th>)}</tr></thead><tbody>{(obe?.courses ?? []).map(c=><tr key={c.courseId} style={{borderTop:"1px solid var(--line)"}}><Td><strong>{c.courseCode ? `${c.courseCode} · ` : ""}{c.courseTitle}</strong><div style={{color:"var(--muted)",fontSize:11}}>{c.letterGrade ?? "—"} · {c.percentage}%</div></Td><Td>{c.creditHours}</Td><Td>{c.mappedClos.length?c.mappedClos.join(", "):"—"}</Td>{plos.map(p=>{const val=c.ploAttainment[p]; return <Td key={p}><span style={{color:typeof val==="number"&&val<threshold?"var(--red)":"var(--ink)",fontWeight:typeof val==="number"&&val<threshold?800:500}}>{typeof val==="number"?`${val}%`:"—"}</span></Td>})}</tr>)}</tbody></table></div></div>
    {!obe && <div style={{...card,padding:18,color:"var(--red)",fontSize:12}}>OBE attainment could not be loaded. The rest of the student portal remains available.</div>}
    {obe?.mappingSource !== "authoritative_clo_plo_mapping" && <div style={{...card,padding:18,color:"var(--muted)",fontSize:12}}>This demo database predates complete CLO→PLO mapping seed data for some courses, so those rows use an explicit backend demo fallback. Production tenants use their configured CLO/PLO mappings.</div>}
  </div>;
}

function Th({children}:{children:React.ReactNode}) { return <th style={{ padding:"12px 14px", whiteSpace:"nowrap", fontSize:11, color:"var(--muted)", textAlign:"left" }}>{children}</th>; }
function Td({children}:{children:React.ReactNode}) { return <td style={{ padding:"12px 14px", verticalAlign:"top" }}>{children}</td>; }
