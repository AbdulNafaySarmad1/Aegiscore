"use client";

import { useEffect, useState } from "react";
import { AppShell } from "@/components/layout/AppShell";
import { apiGet } from "@/lib/api-client";
import { GraduationCap, TrendingUp } from "lucide-react";

type Semester = {
  semesterIndex: number;
  courses: { courseId: string; courseTitle: string; letterGrade: string; gradePoints: number }[];
  sgpa: number;
};

export default function StudentGrades() {
  const [data, setData] = useState<{ semesters: Semester[]; cgpa: number; studentName: string } | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    apiGet<{ semesters: Semester[]; cgpa: number; studentName: string }>("/api/student-portal/transcript")
      .then(setData)
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  return (
    <AppShell>
      <div className="content-page">
        <div className="page-header">
          <div>
            <div className="page-header-breadcrumb"><span>Academic</span><span>/</span><span>Grades</span></div>
            <h1>My Grades</h1>
            <p>Semester-wise grades and GPA</p>
          </div>
        </div>

        {loading ? <div className="skeleton" style={{height:120}} /> : data ? (
          <>
            <div className="kpi-grid" style={{marginBottom:16}}>
              <article className="kpi-card">
                <div className="kpi-card-icon kpi-accent blue"><GraduationCap size={18} /></div>
                <span className="kpi-label">CGPA</span>
                <strong className="kpi-number">{data.cgpa.toFixed(2)}</strong>
              </article>
              <article className="kpi-card">
                <div className="kpi-card-icon kpi-accent green"><TrendingUp size={18} /></div>
                <span className="kpi-label">Semesters</span>
                <strong className="kpi-number">{data.semesters.length}</strong>
              </article>
            </div>

            {data.semesters.map((sem, idx) => (
              <div key={idx} className="card" style={{marginBottom:12}}>
                <div className="card-header">
                  <h3 style={{fontSize:14}}>Semester {sem.semesterIndex}</h3>
                  <span className="badge badge-success">SGPA: {sem.sgpa.toFixed(2)}</span>
                </div>
                <div className="card-body" style={{padding:0}}>
                  <table style={{width:"100%",borderCollapse:"collapse"}}>
                    <thead>
                      <tr style={{background:"var(--surface-muted)"}}>
                        <th style={{padding:"10px 16px",textAlign:"left",fontSize:11,color:"var(--muted)"}}>Course</th>
                        <th style={{padding:"10px 16px",textAlign:"center",fontSize:11,color:"var(--muted)"}}>Grade</th>
                        <th style={{padding:"10px 16px",textAlign:"center",fontSize:11,color:"var(--muted)"}}>Points</th>
                      </tr>
                    </thead>
                    <tbody>
                      {sem.courses.map((c, i) => (
                        <tr key={i} style={{borderBottom:i<sem.courses.length-1?"1px solid var(--border)":"none"}}>
                          <td style={{padding:"12px 16px",fontSize:13}}>{c.courseTitle}</td>
                          <td style={{padding:"12px 16px",textAlign:"center"}}>
                            <span className={`badge ${c.letterGrade==="A"||c.letterGrade==="A-"?"badge-success":c.letterGrade==="B+"||c.letterGrade==="B"?"badge-primary":"badge-warning"}`}>{c.letterGrade}</span>
                          </td>
                          <td style={{padding:"12px 16px",textAlign:"center",fontSize:13}}>{c.gradePoints.toFixed(2)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            ))}
          </>
        ) : <p style={{color:"var(--muted)"}}>No grades available yet.</p>}
      </div>
    </AppShell>
  );
}
