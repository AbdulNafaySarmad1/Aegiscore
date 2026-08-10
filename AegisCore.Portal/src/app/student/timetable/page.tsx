"use client";

import { useEffect, useState } from "react";
import { AppShell } from "@/components/layout/AppShell";
import { apiGet } from "@/lib/api-client";
import { CalendarDays, Clock, MapPin } from "lucide-react";

type TimetableEntry = {
  id: string; dayOfWeek: number; dayName: string;
  startTime: string; endTime: string; room: string | null;
  courseId: string; courseTitle: string; courseCode: string; isToday: boolean;
};

export default function StudentTimetable() {
  const [entries, setEntries] = useState<TimetableEntry[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    apiGet<{ today: string; entries: TimetableEntry[] }>("/api/student-portal/my-timetable")
      .then(d => setEntries(d.entries))
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  const days = ["Monday","Tuesday","Wednesday","Thursday","Friday","Saturday","Sunday"];
  const grouped = days.map(d => ({
    day: d,
    entries: entries.filter(e => e.dayName === d),
  }));

  return (
    <AppShell>
      <div className="content-page">
        <div className="page-header">
          <div>
            <div className="page-header-breadcrumb"><span>Academic</span><span>/</span><span>Timetable</span></div>
            <h1>My Timetable</h1>
            <p>Weekly class schedule</p>
          </div>
        </div>
        {loading ? <div className="skeleton" style={{height:200}} /> : (
          <div style={{display:"grid",gap:12}}>
            {grouped.filter(g => g.entries.length > 0).map(group => (
              <div key={group.day} className="card" style={group.entries.some(e => e.isToday) ? {border:"1px solid var(--primary)"} : {}}>
                <div className="card-header">
                  <h3 style={{fontSize:14}}>{group.day}{group.entries.some(e => e.isToday) ? " (Today)" : ""}</h3>
                </div>
                <div className="card-body" style={{display:"grid",gap:8}}>
                  {group.entries.map(e => (
                    <div key={e.id} style={{display:"flex",gap:16,alignItems:"center",padding:"8px 0",borderBottom:"1px solid var(--border)"}}>
                      <CalendarDays size={16} style={{color:"var(--muted)"}} />
                      <div style={{flex:1}}>
                        <strong style={{fontSize:13}}>{e.courseTitle}</strong>
                        <span style={{display:"block",fontSize:11,color:"var(--muted)"}}>{e.courseCode}</span>
                      </div>
                      <Clock size={14} style={{color:"var(--muted)"}} />
                      <span style={{fontSize:12,minWidth:80}}>{e.startTime} - {e.endTime}</span>
                      {e.room && <><MapPin size={14} style={{color:"var(--muted)"}} /><span style={{fontSize:12,color:"var(--muted)"}}>{e.room}</span></>}
                    </div>
                  ))}
                </div>
              </div>
            ))}
            {!loading && entries.length === 0 && <p style={{color:"var(--muted)"}}>No timetable entries found. Check with your department.</p>}
          </div>
        )}
      </div>
    </AppShell>
  );
}
