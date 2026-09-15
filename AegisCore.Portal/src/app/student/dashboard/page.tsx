"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { AppShell } from "@/components/layout/AppShell";
import { apiGet } from "@/lib/api-client";
import { BookOpenCheck, CalendarDays, ClipboardCheck, CreditCard, FileText, GraduationCap } from "lucide-react";

type Overview = {
  student: { id: string; name: string; universityId: string; rollNumber: string; status: string };
  admission: { applicationNumber: string; status: string; approvedDocuments: number; testScore: number; testPassed: boolean } | null;
  academics: { program: string; cgpa: number; earnedCredits: number; requiredCredits: number; completedCourses: number; totalCourses: number; courses: { code: string; title: string; credits: number; grade: string | null; status: string }[] };
  attendance: { course: string; total: number; present: number; percentage: number }[];
  finance: { demanded: number; paid: number; outstanding: number; status: string };
  timetable: { day: string; startTime: string; endTime: string; room: string; course: string }[];
};

export default function StudentDashboard() {
  const [data, setData] = useState<Overview | null>(null);
  const [error, setError] = useState("");
  useEffect(() => { apiGet<Overview>("/api/student/overview").then(setData).catch(reason => setError(reason.message)); }, []);
  const attendance = data?.attendance.length ? data.attendance.reduce((sum, row) => sum + row.percentage, 0) / data.attendance.length : 0;
  const kpis = [
    { label: "CGPA", value: data ? data.academics.cgpa.toFixed(2) : "--", icon: GraduationCap },
    { label: "Attendance", value: data ? `${Math.round(attendance)}%` : "--", icon: ClipboardCheck },
    { label: "Credits earned", value: data ? `${data.academics.earnedCredits} / ${data.academics.requiredCredits}` : "--", icon: BookOpenCheck },
    { label: "Fees", value: data?.finance.status ?? "--", icon: CreditCard },
  ];
  return <AppShell><div className="content-page">
    <div className="page-header"><div><h1>{data ? `Welcome, ${data.student.name}` : "Student dashboard"}</h1><p>{data ? `${data.academics.program} · ${data.student.rollNumber}` : "Loading your verified academic record…"}</p></div></div>
    {error && <div role="alert" className="error-state">{error}</div>}
    <div className="kpi-grid">{kpis.map(row => <article className="kpi-card" key={row.label}><div className="kpi-card-icon kpi-accent blue"><row.icon size={18}/></div><span className="kpi-label">{row.label}</span><strong className="kpi-number">{row.value}</strong></article>)}</div>
    {data && <>
      <div className="card" style={{ marginTop: 20 }}><div className="card-header"><h3>Admission to enrolment</h3><span className="badge badge-success">Persisted record</span></div><div className="card-body" style={{ display: "grid", gridTemplateColumns: "repeat(4,1fr)", gap: 18 }}>
        <div><small>Application</small><strong style={{display:"block"}}>{data.admission?.applicationNumber}</strong></div><div><small>Verified documents</small><strong style={{display:"block"}}>{data.admission?.approvedDocuments}</strong></div><div><small>Admission test</small><strong style={{display:"block"}}>{data.admission?.testScore}% · Passed</strong></div><div><small>Student number</small><strong style={{display:"block"}}>{data.student.universityId}</strong></div>
      </div></div>
      <div style={{ display:"grid", gridTemplateColumns:"1.35fr 1fr", gap:16, marginTop:16 }}>
        <div className="card"><div className="card-header"><h3>Latest academic record</h3><span>{data.academics.completedCourses} courses completed</span></div><div className="card-body" style={{padding:0}}><table className="data-table"><thead><tr><th>Course</th><th>Credits</th><th>Grade</th></tr></thead><tbody>{data.academics.courses.slice(-6).map(course => <tr key={course.code}><td>{course.code} · {course.title}</td><td>{course.credits}</td><td><span className="badge badge-success">{course.grade ?? "In progress"}</span></td></tr>)}</tbody></table></div></div>
        <div className="card"><div className="card-header"><h3>Current timetable</h3></div><div className="card-body">{data.timetable.slice(0,5).map(row => <div key={`${row.day}-${row.course}`} style={{padding:"9px 0",borderBottom:"1px solid var(--border)"}}><strong>{row.day} · {row.course}</strong><div style={{fontSize:12,color:"var(--muted)"}}>{row.startTime.slice(0,5)}–{row.endTime.slice(0,5)} · {row.room}</div></div>)}</div></div>
      </div>
      <div className="quick-actions" style={{marginTop:16}}><Link href="/student/transcript" className="quick-action-btn"><FileText size={16}/>Verified transcript</Link><Link href={`/graduation/${data.student.id}`} className="quick-action-btn"><GraduationCap size={16}/>Degree progress</Link><Link href="/alumni" className="quick-action-btn"><GraduationCap size={16}/>Alumni access</Link><Link href="/student/profile" className="quick-action-btn"><CalendarDays size={16}/>Student profile</Link></div>
    </>}
  </div></AppShell>;
}
