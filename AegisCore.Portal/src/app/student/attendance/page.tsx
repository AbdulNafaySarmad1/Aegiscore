"use client";

import { useEffect, useState } from "react";
import { AppShell } from "@/components/layout/AppShell";
import { apiGet, apiPost } from "@/lib/api-client";
import { ClipboardCheck, CheckCircle, XCircle } from "lucide-react";

type CourseAttendance = {
  courseId: string; courseTitle: string; totalSessions: number;
  presentSessions: number; absentSessions: number; percentage: number;
  recent: { date: string; status: string }[];
};

export default function StudentAttendance() {
  const [courses, setCourses] = useState<CourseAttendance[]>([]);
  const [loading, setLoading] = useState(true);
  const [checkInMsg, setCheckInMsg] = useState("");
  const [checkingIn, setCheckingIn] = useState(false);

  useEffect(() => {
    apiGet<{ courses: CourseAttendance[] }>("/api/student-portal/attendance/history")
      .then(d => setCourses(d.courses))
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  const handleCheckIn = async (courseId: string) => {
    setCheckingIn(true); setCheckInMsg("");
    try {
      const res = await apiPost<{ success: boolean; message: string }>("/api/student-portal/attendance/check-in", { courseId, latitude: null, longitude: null });
      setCheckInMsg(res.message);
    } catch (err) {
      setCheckInMsg(err instanceof Error ? err.message : "Check-in failed.");
    } finally { setCheckingIn(false); }
  };

  const overallPct = courses.length > 0 ? Math.round(courses.reduce((s,c)=>s+c.presentSessions,0) / Math.max(1, courses.reduce((s,c)=>s+c.totalSessions,0)) * 100) : 0;

  return (
    <AppShell>
      <div className="content-page">
        <div className="page-header">
          <div>
            <div className="page-header-breadcrumb"><span>Academic</span><span>/</span><span>Attendance</span></div>
            <h1>My Attendance</h1>
            <p>Overall: {overallPct}% | {courses.length} courses</p>
          </div>
        </div>

        {loading ? <div className="skeleton" style={{height:120}} /> : courses.map(c => (
          <div key={c.courseId} className="card" style={{marginBottom:12}}>
            <div className="card-header" style={{display:"flex",justifyContent:"space-between",alignItems:"center"}}>
              <div>
                <h3 style={{fontSize:14,margin:0}}>{c.courseTitle}</h3>
                <span style={{fontSize:12,color:"var(--muted)"}}>{c.presentSessions}/{c.totalSessions} sessions ({c.percentage}%)</span>
              </div>
              <button className="btn btn-primary btn-sm" onClick={() => handleCheckIn(c.courseId)} disabled={checkingIn}>
                <ClipboardCheck size={14} /> Check In
              </button>
            </div>
            <div className="card-body" style={{padding:0}}>
              {c.recent.map((r, i) => (
                <div key={i} style={{display:"flex",justifyContent:"space-between",padding:"8px 16px",borderBottom:i<c.recent.length-1?"1px solid var(--border)":"none",fontSize:13}}>
                  <span>{r.date}</span>
                  <span className={r.status === "Present" ? "badge badge-success" : "badge badge-danger"}>
                    {r.status === "Present" ? <CheckCircle size={12} /> : <XCircle size={12} />} {r.status}
                  </span>
                </div>
              ))}
              {c.recent.length === 0 && <p style={{padding:16,color:"var(--muted)",fontSize:13}}>No attendance records yet.</p>}
            </div>
          </div>
        ))}
        {!loading && courses.length === 0 && <p style={{color:"var(--muted)"}}>No attendance records found.</p>}
        {checkInMsg && <div className="error-state" style={{marginTop:12,background:"var(--success-soft)",border:"1px solid var(--success)",color:"var(--success)"}}><ClipboardCheck size={20} /><p>{checkInMsg}</p></div>}
      </div>
    </AppShell>
  );
}
