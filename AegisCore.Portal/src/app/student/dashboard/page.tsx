"use client";

// ─── Student Dashboard ───
// Student-prioritized landing. No fabricated academic metrics: student-self
// grade/attendance/finance endpoints require an authoritative studentId that
// the session model does not yet provide (docs/FRONTEND_API_HANDOFF.md).
// Until then this page is an honest workspace hub, not a wall of "--".

import Link from "next/link";
import { motion } from "framer-motion";
import { AppShell } from "@/components/layout/AppShell";
import { useSession } from "@/components/session-provider";
import { getGreeting } from "@/lib/roles";
import {
  CalendarDays, ClipboardCheck, CreditCard, FileText, Gauge, GraduationCap,
  ShieldCheck, Trophy, Utensils, Vote,
} from "lucide-react";

const shortcuts = [
  { href: "/telemetry", icon: Gauge, label: "Performance", desc: "Academic sectors, trends, and personal bests.", color: "blue" },
  { href: "/attendance", icon: ClipboardCheck, label: "Attendance", desc: "Per-course presence records.", color: "blue" },
  { href: "/examinations", icon: GraduationCap, label: "Examinations", desc: "Schedules and published results.", color: "blue" },
  { href: "/student/transcript", icon: FileText, label: "Transcript", desc: "Official record and requests.", color: "blue" },
  { href: "/student/finance", icon: CreditCard, label: "Fees", desc: "Dues, challans, and receipts.", color: "green", dynamicStudentFinance: true },
  { href: "/achievements", icon: Trophy, label: "Achievements", desc: "Badges and Dean's List.", color: "violet" },
  { href: "/elections", icon: Vote, label: "Elections", desc: "Student council voting.", color: "violet" },
  { href: "/cafeteria", icon: Utensils, label: "Cafeteria", desc: "Menus and meal orders.", color: "amber" },
  { href: "/mental-health", icon: ShieldCheck, label: "Wellbeing", desc: "Confidential support, anonymously.", color: "violet" },
] as const;

export default function StudentDashboard() {
  const { session } = useSession();
  const { greeting } = getGreeting(session?.name ?? "Student");

  return (
    <AppShell>
      <div className="content-page">
        <motion.div className="hero-greeting" initial={{ opacity: 0 }} animate={{ opacity: 1 }} transition={{ duration: 0.2 }}>
          <h1>{greeting}</h1>
          <p>Your student workspace</p>
          <div className="hero-time">
            <CalendarDays size={14} style={{ display: "inline", verticalAlign: "-2px", marginRight: 6 }} />
            {new Date().toLocaleDateString("en-PK", { weekday: "long", year: "numeric", month: "long", day: "numeric" })}
          </div>
        </motion.div>

        <div className="module-grid">
          {shortcuts.map((s, i) => {
            const needsStudentId = "dynamicStudentFinance" in s && s.dynamicStudentFinance;
            const disabled = needsStudentId && !session?.studentId;
            const href = needsStudentId ? `/student/finance/${session?.studentId}` : s.href;
            return (
              <motion.article
                key={s.href}
                className="module-card"
                initial={{ opacity: 0, y: 16 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: Math.min(i, 8) * .03, duration: 0.2 }}
                style={disabled ? { opacity: 0.6 } : undefined}
              >
                <div className="module-card-body">
                  <div className="module-card-icon" style={{ color: `var(--domain-${s.color === "blue" ? "academic" : s.color === "green" ? "finance" : s.color === "amber" ? "operations" : "welfare"})`, background: "var(--surface-muted)" }}>
                    <s.icon size={20} />
                  </div>
                  <strong>{s.label}</strong>
                  <p>{s.desc}</p>
                  {disabled ? (
                    <span className="kpi-unavailable-note">Available once your student record is linked</span>
                  ) : (
                    <Link href={href} className="module-card-link">Open →</Link>
                  )}
                </div>
              </motion.article>
            );
          })}
        </div>

        <div className="card" style={{ marginTop: 24, maxWidth: 720 }}>
          <div className="card-body" style={{ display: "flex", gap: 12, alignItems: "flex-start" }}>
            <ShieldCheck size={18} style={{ color: "var(--primary)", flexShrink: 0, marginTop: 2 }} />
            <p style={{ fontSize: 13, color: "var(--muted)", margin: 0 }}>
              Live CGPA, credit-hour, and fee-balance summaries appear here automatically once the Registrar
              links your student record to this account. No placeholder figures are shown in the meantime.
            </p>
          </div>
        </div>
      </div>
    </AppShell>
  );
}
