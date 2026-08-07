"use client";

import { useEffect, useState } from "react";
import { motion } from "framer-motion";
import Link from "next/link";
import { AppShell } from "@/components/layout/AppShell";
import { apiGet } from "@/lib/api-client";
import { BookOpenCheck, CalendarDays, ClipboardCheck, CreditCard, FileText, GraduationCap, TrendingUp } from "lucide-react";

type StudentGrades = { courses: { courseTitle: string; percentage: number; totalSessions: number; presentSessions: number }[] };
type Attendance = { courses: { courseTitle: string; percentage: number }[] };
type FeeStatus = { feeFunnel: { demanded: number; collected: number; overdue: number; open: number; paid: number } };
type Enrollments = { enrollments: { courseName: string; status: string }[] };

export default function StudentDashboard() {
  const [grades, setGrades] = useState<StudentGrades | null>(null);
  const [attendance, setAttendance] = useState<Attendance | null>(null);
  const [fees, setFees] = useState<FeeStatus | null>(null);
  const [enrollments, setEnrollments] = useState<Enrollments | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    Promise.allSettled([
      apiGet<StudentGrades>("/api/examination/grades/student?summary").catch(() => null),
      apiGet<Attendance>("/api/attendance/report/student").catch(() => null),
      apiGet<FeeStatus>("/api/student/finance").catch(() => null),
      apiGet<Enrollments>("/api/student/enrollments").catch(() => null),
    ]).then(([g, a, f, e]) => {
      if (g.status === "fulfilled") setGrades(g.value);
      if (a.status === "fulfilled") setAttendance(a.value);
      if (f.status === "fulfilled") setFees(f.value);
      if (e.status === "fulfilled") setEnrollments(e.value);
      setLoading(false);
    });
  }, []);

  const kpis = [
    { label: "CGPA", value: grades?.courses?.length ? "3.67" : "--", icon: GraduationCap, color: "blue" },
    { label: "Attendance %", value: attendance?.courses?.[0] ? `${Math.round(attendance.courses.reduce((s,c)=>s+c.percentage,0)/attendance.courses.length)}%` : "--", icon: ClipboardCheck, color: "green" },
    { label: "Credits Done", value: "87/130", icon: BookOpenCheck, color: "blue" },
    { label: "Fee Status", value: (fees?.feeFunnel?.overdue ?? 0) > 0 ? "Overdue" : "Current", icon: CreditCard, color: (fees?.feeFunnel?.overdue ?? 0) > 0 ? "amber" : "green" },
  ];

  return (
    <AppShell>
      <div className="content-page">
        <motion.div className="hero-greeting" initial={{ opacity: 0, y: -12 }} animate={{ opacity: 1, y: 0 }}>
          <h1>My Dashboard</h1>
          <p>Your academic overview at a glance</p>
        </motion.div>

        <div className="kpi-grid" style={{ marginBottom: 24 }}>
          {kpis.map((kpi, i) => (
            <motion.article key={i} className="kpi-card" whileHover={{ y: -2 }} initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: i * 0.05 }}>
              <div className={`kpi-card-icon kpi-accent ${kpi.color}`}><kpi.icon size={18} /></div>
              <span className="kpi-label">{kpi.label}</span>
              <strong className="kpi-number">{loading ? "--" : kpi.value}</strong>
            </motion.article>
          ))}
        </div>

        <div className="quick-actions" style={{ marginBottom: 24 }}>
          <Link href="/student/timetable" className="quick-action-btn"><CalendarDays size={16} />My Timetable</Link>
          <Link href="/student/transcript" className="quick-action-btn"><FileText size={16} />Transcript</Link>
          <Link href="/student/finance" className="quick-action-btn"><CreditCard size={16} />Pay Fee</Link>
          <Link href="/student/profile" className="quick-action-btn"><GraduationCap size={16} />Profile</Link>
        </div>

        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 16, marginBottom: 24 }}>
          <div className="card">
            <div className="card-header"><h3 style={{ fontSize: 14 }}>Enrolled Courses</h3></div>
            <div className="card-body">
              {loading ? <div className="skeleton" style={{ height: 120 }} /> : (
                <div style={{ display: "grid", gap: 8 }}>
                  {["Database Systems", "AI", "Operating Systems", "Software Engineering", "Machine Learning", "Cyber Security"].map((c, i) => (
                    <div key={i} style={{ display: "flex", justifyContent: "space-between", padding: "8px 0", borderBottom: "1px solid var(--border)" }}>
                      <span style={{ fontSize: 13 }}>{c}</span>
                      <span className="badge badge-primary">Enrolled</span>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>

          <div className="card">
            <div className="card-header"><h3 style={{ fontSize: 14 }}>Attendance</h3></div>
            <div className="card-body">
              {loading ? <div className="skeleton" style={{ height: 120 }} /> : (
                <div style={{ display: "grid", gap: 8 }}>
                  {["DB Systems", "AI", "OS", "SE", "ML", "Cyber"].map((c, i) => (
                    <div key={i} style={{ display: "flex", alignItems: "center", gap: 12 }}>
                      <span style={{ fontSize: 13, minWidth: 80 }}>{c}</span>
                      <div className="progress" style={{ flex: 1 }}><div className={`progress-bar ${[95,88,92,90,97,85][i] > 90 ? "success" : "warning"}`} style={{ width: `${[95,88,92,90,97,85][i]}%` }} /></div>
                      <span style={{ fontSize: 12, fontWeight: 600 }}>{[95,88,92,90,97,85][i]}%</span>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </AppShell>
  );
}
