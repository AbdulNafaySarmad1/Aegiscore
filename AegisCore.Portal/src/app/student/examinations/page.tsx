"use client";

import { useEffect, useState } from "react";
import { AppShell } from "@/components/layout/AppShell";
import { apiGet } from "@/lib/api-client";
import { CalendarDays, Clock, MapPin, GraduationCap, FileText } from "lucide-react";

type UpcomingExam = {
  id: string; courseId: string; courseTitle: string; courseCode: string;
  examType: string; scheduledDateTime: string; venue: string | null;
};

type PublishedGrade = {
  id: string; courseId: string; letterGrade: string | null;
  gradePoints: number; percentageObtained: number;
  isPassing: boolean; resultStatus: string; publishedAt: string | null;
};

export default function StudentExaminations() {
  const [upcoming, setUpcoming] = useState<UpcomingExam[]>([]);
  const [grades, setGrades] = useState<PublishedGrade[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    apiGet<{ upcomingExams: UpcomingExam[]; publishedGrades: PublishedGrade[] }>("/api/student-portal/examinations")
      .then(d => { setUpcoming(d.upcomingExams ?? []); setGrades(d.publishedGrades ?? []); })
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  const fmtDate = (iso: string) => new Date(iso).toLocaleDateString(undefined, { weekday: "short", year: "numeric", month: "short", day: "numeric" });
  const fmtTime = (iso: string) => new Date(iso).toLocaleTimeString(undefined, { hour: "2-digit", minute: "2-digit" });

  return (
    <AppShell>
      <div className="content-page">
        <div className="page-header">
          <div>
            <div className="page-header-breadcrumb"><span>Academic</span><span>/</span><span>Examinations</span></div>
            <h1>My Examinations</h1>
            <p>Upcoming exams and published results</p>
          </div>
        </div>

        {loading ? <div className="skeleton" style={{height:120}} /> : (
          <>
            <div className="card" style={{marginBottom:20}}>
              <div className="card-header">
                <h3 style={{fontSize:14}}><CalendarDays size={16} /> Upcoming Exams ({upcoming.length})</h3>
              </div>
              <div className="card-body" style={{padding:0}}>
                {upcoming.length === 0 ? <p style={{padding:16,color:"var(--muted)",fontSize:13}}>No upcoming exams scheduled.</p> :
                upcoming.map((e, i) => (
                  <div key={e.id} style={{display:"flex",alignItems:"center",gap:12,padding:"12px 16px",borderBottom:i<upcoming.length-1?"1px solid var(--border)":"none",fontSize:13}}>
                    <CalendarDays size={16} style={{color:"var(--primary)"}} />
                    <div style={{flex:1}}>
                      <strong>{e.courseTitle}</strong>
                      <span style={{display:"block",fontSize:11,color:"var(--muted)"}}>{e.courseCode} — {e.examType}</span>
                    </div>
                    <div style={{textAlign:"right"}}>
                      <p style={{margin:0,fontSize:12}}><Clock size={12} /> {fmtDate(e.scheduledDateTime)} at {fmtTime(e.scheduledDateTime)}</p>
                      {e.venue && <p style={{margin:0,fontSize:11,color:"var(--muted)"}}><MapPin size={11} /> {e.venue}</p>}
                    </div>
                  </div>
                ))}
              </div>
            </div>

            <div className="card">
              <div className="card-header">
                <h3 style={{fontSize:14}}><GraduationCap size={16} /> Published Results ({grades.length})</h3>
              </div>
              <div className="card-body" style={{padding:0}}>
                {grades.length === 0 ? <p style={{padding:16,color:"var(--muted)",fontSize:13}}>No published grades yet.</p> :
                <table style={{width:"100%",borderCollapse:"collapse"}}>
                  <thead>
                    <tr style={{background:"var(--surface-muted)"}}>
                      <th style={{padding:"10px 16px",textAlign:"left",fontSize:11,color:"var(--muted)"}}>Course</th>
                      <th style={{padding:"10px 16px",textAlign:"center",fontSize:11,color:"var(--muted)"}}>Grade</th>
                      <th style={{padding:"10px 16px",textAlign:"center",fontSize:11,color:"var(--muted)"}}>Points</th>
                      <th style={{padding:"10px 16px",textAlign:"center",fontSize:11,color:"var(--muted)"}}>%</th>
                      <th style={{padding:"10px 16px",textAlign:"center",fontSize:11,color:"var(--muted)"}}>Status</th>
                    </tr>
                  </thead>
                  <tbody>
                    {grades.map((g, i) => (
                      <tr key={g.id} style={{borderBottom:i<grades.length-1?"1px solid var(--border)":"none"}}>
                        <td style={{padding:"12px 16px",fontSize:13}}>{g.courseId}</td>
                        <td style={{padding:"12px 16px",textAlign:"center"}}>
                          <span className={`badge ${g.isPassing ? (g.letterGrade==="A"||g.letterGrade==="A-"?"badge-success":"badge-primary") : "badge-danger"}`}>
                            {g.letterGrade ?? "N/A"}
                          </span>
                        </td>
                        <td style={{padding:"12px 16px",textAlign:"center",fontSize:13}}>{g.gradePoints.toFixed(2)}</td>
                        <td style={{padding:"12px 16px",textAlign:"center",fontSize:13}}>{g.percentageObtained}%</td>
                        <td style={{padding:"12px 16px",textAlign:"center"}}>
                          <span className={`badge ${g.resultStatus === "published" ? "badge-success" : "badge-neutral"}`}>{g.resultStatus}</span>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>}
              </div>
            </div>
          </>
        )}
      </div>
    </AppShell>
  );
}
