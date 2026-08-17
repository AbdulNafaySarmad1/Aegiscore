"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { apiGet, apiPost } from "@/lib/api-client";
import { BookOpenCheck, CalendarDays, ClipboardCheck, GraduationCap, RefreshCw, Users } from "lucide-react";

type Session = { roles?: string[] };
type Course = { id: string; code: string; title: string };
type Student = { enrollmentId: string; studentId: string; name: string; universityId: string };
type Roster = { students: Student[] };
type Offering = { id: string; name: string; courseCode: string; courseName: string; targetSections: number; generatedSections: number; totalCapacity: number };
type ClassSection = { id: string; offeringId: string; offeringName: string; sectionName: string; capacity: number; enrolled: number; lmsProvider: string; lmsStatus: string };

export default function ClassesPage() {
  const [roles, setRoles] = useState<string[]>([]);
  const [courses, setCourses] = useState<Course[]>([]);
  const [selectedCourse, setSelectedCourse] = useState<string>("");
  const [roster, setRoster] = useState<Student[]>([]);
  const [offerings, setOfferings] = useState<Offering[]>([]);
  const [sections, setSections] = useState<ClassSection[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [academicYear, setAcademicYear] = useState("2026-2027");
  const [semester, setSemester] = useState("Fall");
  const [generating, setGenerating] = useState(false);

  const isFaculty = useMemo(() => roles.includes("Faculty") && !roles.some(r => ["Admin", "SuperAdmin", "Registrar", "HOD", "Dean"].includes(r)), [roles]);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const session = await apiGet<Session>("/api/session");
        if (cancelled) return;
        const nextRoles = session.roles ?? [];
        setRoles(nextRoles);

        const facultyOnly = nextRoles.includes("Faculty") && !nextRoles.some(r => ["Admin", "SuperAdmin", "Registrar", "HOD", "Dean"].includes(r));
        if (facultyOnly) {
          const mine = await apiGet<Course[]>("/api/courses/my");
          if (!cancelled) setCourses(mine);
        } else {
          const [o, s] = await Promise.all([
            apiGet<Offering[]>("/api/classes/offerings").catch(() => []),
            apiGet<ClassSection[]>("/api/classes/sections").catch(() => []),
          ]);
          if (!cancelled) { setOfferings(o); setSections(s); }
        }
      } catch (e) {
        if (!cancelled) setError(e instanceof Error ? e.message : "Failed to load classes.");
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => { cancelled = true; };
  }, []);

  useEffect(() => {
    if (!selectedCourse || !isFaculty) { setRoster([]); return; }
    apiGet<Roster>(`/api/attendance/roster/${selectedCourse}`)
      .then(r => setRoster(r.students ?? []))
      .catch(() => setRoster([]));
  }, [selectedCourse, isFaculty]);

  async function generateClasses(e: React.FormEvent) {
    e.preventDefault();
    setGenerating(true); setMessage(null);
    try {
      await apiPost("/api/classes/generate", { academicYear, semester });
      setMessage("Class generation initiated.");
    } catch (e) {
      setMessage(e instanceof Error ? e.message : "Class generation failed.");
    } finally { setGenerating(false); }
  }

  if (loading) return <main className="content-page"><div className="skeleton" style={{ height: 120 }} /></main>;

  if (error) return (
    <main className="content-page">
      <div className="error-state"><p>{error}</p><button className="btn" onClick={() => location.reload()}><RefreshCw size={15}/> Retry</button></div>
    </main>
  );

  if (isFaculty) {
    const selected = courses.find(c => c.id === selectedCourse);
    return (
      <main className="content-page">
        <div className="page-header">
          <div>
            <div className="page-header-breadcrumb"><span>Academic</span><span>/</span><span>My Classes</span></div>
            <h1>My Classes</h1>
            <p>Your assigned courses, rosters, attendance, and assessment tools.</p>
          </div>
        </div>

        <section style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit,minmax(220px,1fr))", gap: 14, marginBottom: 18 }}>
          {courses.map(course => (
            <button key={course.id} onClick={() => setSelectedCourse(course.id)} className="card" style={{ textAlign: "left", padding: 18, cursor: "pointer", border: selectedCourse === course.id ? "2px solid var(--blue-500)" : "1px solid var(--border)", background: "var(--surface)" }}>
              <BookOpenCheck size={20} style={{ color: "var(--blue-600)", marginBottom: 10 }} />
              <strong style={{ display: "block", fontSize: 15 }}>{course.code}</strong>
              <span style={{ color: "var(--muted)", fontSize: 13 }}>{course.title}</span>
            </button>
          ))}
          {courses.length === 0 && <div className="card"><div className="card-body" style={{ color: "var(--muted)" }}>No courses are assigned to this faculty account yet.</div></div>}
        </section>

        {selected && (
          <>
            <section style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit,minmax(190px,1fr))", gap: 12, marginBottom: 18 }}>
              <Link href="/attendance" className="card" style={{ padding: 16, textDecoration: "none", color: "inherit" }}><ClipboardCheck size={20}/><strong style={{ display:"block", marginTop:8 }}>Mark Attendance</strong></Link>
              <Link href="/examinations" className="card" style={{ padding: 16, textDecoration: "none", color: "inherit" }}><GraduationCap size={20}/><strong style={{ display:"block", marginTop:8 }}>Enter Grades</strong></Link>
              <Link href="/obe" className="card" style={{ padding: 16, textDecoration: "none", color: "inherit" }}><CalendarDays size={20}/><strong style={{ display:"block", marginTop:8 }}>CLO / PLO</strong></Link>
            </section>

            <div className="card">
              <div className="card-header" style={{ display:"flex", justifyContent:"space-between", alignItems:"center" }}>
                <div><h3>{selected.code} — Class Roster</h3><p style={{ color:"var(--muted)", margin:0 }}>{selected.title}</p></div>
                <span className="badge badge-primary"><Users size={13}/> {roster.length} students</span>
              </div>
              <div className="card-body" style={{ padding: 0 }}>
                {roster.map(s => (
                  <div key={s.enrollmentId} style={{ display:"flex", justifyContent:"space-between", gap:12, padding:"12px 16px", borderBottom:"1px solid var(--border)" }}>
                    <strong>{s.name}</strong><span style={{ color:"var(--muted)", fontSize:12 }}>{s.universityId}</span>
                  </div>
                ))}
                {roster.length === 0 && <div style={{ padding:16, color:"var(--muted)" }}>No enrolled students found for this course.</div>}
              </div>
            </div>
          </>
        )}
      </main>
    );
  }

  return (
    <main className="content-page">
      <div className="page-header">
        <div><div className="page-header-breadcrumb"><span>Academic Operations</span><span>/</span><span>Classes</span></div><h1>Class Generation & LMS</h1><p>Administrative class and section provisioning.</p></div>
      </div>

      <form onSubmit={generateClasses} className="card" style={{ marginBottom: 18 }}>
        <div className="card-body" style={{ display:"grid", gridTemplateColumns:"1fr 1fr auto", gap:12, alignItems:"end" }}>
          <label>Academic Year<select value={academicYear} onChange={e=>setAcademicYear(e.target.value)}><option>2025-2026</option><option>2026-2027</option><option>2027-2028</option></select></label>
          <label>Semester<select value={semester} onChange={e=>setSemester(e.target.value)}><option>Fall</option><option>Spring</option><option>Summer</option></select></label>
          <button className="btn btn-primary" disabled={generating}>{generating ? "Generating..." : "Generate Classes"}</button>
        </div>
      </form>
      {message && <div className="card"><div className="card-body">{message}</div></div>}
      <div className="card"><div className="card-header"><h3>Current Provisioning</h3></div><div className="card-body"><p>{offerings.length} offerings · {sections.length} sections</p></div></div>
    </main>
  );
}
