"use client";

import { useEffect, useState } from "react";
import { motion } from "framer-motion";
import Link from "next/link";
import { AppShell } from "@/components/layout/AppShell";
import { apiGet } from "@/lib/api-client";
import { BookOpenCheck, CalendarDays, ClipboardCheck, CreditCard, FileText, GraduationCap } from "lucide-react";

type Profile = { studentId: string; name: string; program: string; currentSemester: string; cgpa: string; creditsCompleted: string; totalCredits: string };
type AttendanceData = { courses: { courseTitle: string; percentage: number }[] };
type FinanceData = { demands: { totalAmount: number; paidAmount: number; outstandingAmount: number }[] };

export default function StudentDashboard() {
  const [profile, setProfile] = useState<Profile | null>(null);
  const [attendance, setAttendance] = useState<AttendanceData | null>(null);
  const [finance, setFinance] = useState<FinanceData | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    Promise.allSettled([
      apiGet<Profile>("/api/student-portal/me").catch(() => null),
      apiGet<{ courses: { courseTitle: string; percentage: number }[] }>("/api/student-portal/attendance/history").catch(() => null),
      apiGet<{ demands: { totalAmount: number; paidAmount: number; outstandingAmount: number }[] }>("/api/student-portal/me").then(async (me) => {
        const mid = me as unknown as Profile;
        try { return await apiGet<{ demands: { totalAmount: number; paidAmount: number; outstandingAmount: number }[] }>(`/api/finance/v2/student-portal/${mid.studentId}`); } catch { return null; }
      }).catch(() => null),
    ]).then(([p, a, f]) => {
      if (p.status === "fulfilled" && p.value) setProfile(p.value);
      if (a.status === "fulfilled" && a.value) setAttendance(a.value);
      if (f.status === "fulfilled" && f.value) setFinance(f.value);
      setLoading(false);
    });
  }, []);

  const avgAttendance = attendance?.courses?.length
    ? Math.round(attendance.courses.reduce((s, c) => s + c.percentage, 0) / attendance.courses.length)
    : null;

  const feeOutstanding = finance?.demands?.reduce((s, d) => s + d.outstandingAmount, 0) ?? null;

  const kpis = [
    { label: "CGPA", value: profile?.cgpa ?? "--", icon: GraduationCap, color: "blue" },
    { label: "Attendance %", value: avgAttendance !== null ? `${avgAttendance}%` : "--", icon: ClipboardCheck, color: "green" },
    { label: "Credits", value: profile ? `${profile.creditsCompleted}/${profile.totalCredits}` : "--", icon: BookOpenCheck, color: "blue" },
    { label: "Fee Status", value: feeOutstanding !== null ? (feeOutstanding > 0 ? "Overdue" : "Current") : "--", icon: CreditCard, color: feeOutstanding !== null && feeOutstanding > 0 ? "amber" : "green" },
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
            <div className="card-header"><h3 style={{ fontSize: 14 }}>Program</h3></div>
            <div className="card-body">
              {loading ? <div className="skeleton" style={{ height: 80 }} /> : (
                <div style={{ display: "grid", gap: 4 }}>
                  <p style={{ fontSize: 15, fontWeight: 600 }}>{profile?.program ?? "N/A"}</p>
                  <p style={{ fontSize: 13, color: "var(--muted)" }}>Semester: {profile?.currentSemester ?? "N/A"}</p>
                  <p style={{ fontSize: 13, color: "var(--muted)" }}>{profile?.name ?? "Student"}</p>
                </div>
              )}
            </div>
          </div>

          <div className="card">
            <div className="card-header"><h3 style={{ fontSize: 14 }}>Attendance</h3></div>
            <div className="card-body">
              {loading ? <div className="skeleton" style={{ height: 80 }} /> : (
                <div style={{ display: "grid", gap: 8 }}>
                  {attendance?.courses?.slice(0, 5).map((c, i) => (
                    <div key={i} style={{ display: "flex", alignItems: "center", gap: 12 }}>
                      <span style={{ fontSize: 13, minWidth: 80 }}>{c.courseTitle}</span>
                      <div className="progress" style={{ flex: 1 }}><div className={`progress-bar ${c.percentage > 85 ? "success" : c.percentage > 70 ? "warning" : "danger"}`} style={{ width: `${c.percentage}%` }} /></div>
                      <span style={{ fontSize: 12, fontWeight: 600 }}>{c.percentage}%</span>
                    </div>
                  ))}
                  {(!attendance?.courses || attendance.courses.length === 0) && <p style={{ color: "var(--muted)", fontSize: 13 }}>No records yet.</p>}
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </AppShell>
  );
}
