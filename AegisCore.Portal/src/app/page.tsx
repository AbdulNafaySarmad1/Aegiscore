"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { motion } from "framer-motion";
import { apiGet, clearTokens } from "@/lib/api-client";
import { getRoleProfile, getDomainColor, getGreeting } from "@/lib/roles";
import { AppShell } from "@/components/layout/AppShell";
import {
  BookOpenCheck, Briefcase, Building2, CalendarDays, ClipboardCheck, CreditCard,
  FileSearch, FileText, GraduationCap, Landmark, LayoutDashboard, Scale, ShieldCheck,
  UserRound, ArrowUpRight, ArrowDownRight, Minus,
} from "lucide-react";
import type { KPIDefinition, QuickAction, DashboardModule } from "@/lib/roles";

type Session = { userId: string; tenantId: string; studentId: string | null; departmentId: string | null; name: string; roles: string[] };
type StudentMe = { studentId: string; name: string; cgpa?: string | null; creditsCompleted?: string | null };
type AttendanceHistory = { courses: Array<{ totalSessions: number; presentSessions: number }> };
type StudentFinance = { demands: Array<{ currency: string; outstandingAmount: number }> };

const kpiIcons: Record<string, typeof LayoutDashboard> = {
  LayoutDashboard, Landmark, FileText, CreditCard, BookOpenCheck, GraduationCap,
  ClipboardCheck, CalendarDays, ShieldCheck, Building2, Scale, FileSearch,
  Briefcase, UserRound,
};
const Icon = ({ name }: { name: string }) => { const C = kpiIcons[name] ?? LayoutDashboard; return <C size={18} />; };
const ActionIcon = ({ name }: { name: string }) => { const C = kpiIcons[name] ?? LayoutDashboard; return <C size={16} />; };

function TrendArrow({ trend }: { trend?: string }) {
  if (trend === "up") return <ArrowUpRight size={14} className="kpi-trend up" />;
  if (trend === "down") return <ArrowDownRight size={14} className="kpi-trend down" />;
  return <Minus size={14} className="kpi-trend" />;
}

function KpiCard({ kpi, value }: { kpi: KPIDefinition; value: string | number }) {
  const formatted = kpi.format === "currency" && Number.isFinite(Number(value)) ? `₨ ${Number(value).toLocaleString("en-PK")}`
    : kpi.format === "percentage" && value !== "--" ? `${value}%` : String(value);
  const longValue = formatted.length > 12;
  return (
    <motion.article className="kpi-card" whileHover={{ y: -2 }} initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} transition={{ type: "spring", stiffness: 180, damping: 20 }}>
      <div className={`kpi-card-icon kpi-accent ${kpi.iconColor}`}><Icon name={kpi.icon} /></div>
      <span className="kpi-label" style={{ fontSize: 14, fontWeight: 700, letterSpacing: ".02em", lineHeight: 1.25 }}>{kpi.label}</span>
      <strong className="kpi-number" style={{ fontSize: longValue ? 24 : 28, lineHeight: 1.12, marginTop: 8, overflowWrap: "anywhere" }}>{formatted}</strong>
      {kpi.trend && <span className="kpi-trend"><TrendArrow trend={kpi.trend} />{kpi.trendLabel}</span>}
    </motion.article>
  );
}

function QuickAction({ action }: { action: QuickAction }) {
  return <Link href={action.route} className="quick-action-btn"><ActionIcon name={action.icon} />{action.label}</Link>;
}

function ModuleCard({ module: m, index }: { module: DashboardModule; index: number }) {
  return (
    <motion.article className="module-card" initial={{ opacity: 0, y: 24 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: index * .04 }}>
      <div className="module-card-accent" style={{ background: getDomainColor(m.domain) }} />
      <div className="module-card-body">
        <div className="module-card-icon" style={{ color: getDomainColor(m.domain), background: `${getDomainColor(m.domain)}14` }}><Icon name={m.icon} /></div>
        <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
          <strong>{m.label}</strong>
          {m.badge && <span className="badge badge-primary" style={{ fontSize: 9 }}>{m.badge}</span>}
        </div>
        <p>{m.description}</p>
        <Link href={m.route} className="module-card-link">Open →</Link>
      </div>
    </motion.article>
  );
}

export default function DashboardPage() {
  const [session, setSession] = useState<Session | null>(null);
  const [loading, setLoading] = useState(true);
  const [kpiValues, setKpiValues] = useState<Record<string, string | number>>({});

  useEffect(() => {
    let cancelled = false;

    apiGet<Session>("/api/session")
      .then(async (s) => {
        if (cancelled) return;
        let resolved = s;

        if (s.roles.includes("Student")) {
          try {
            const me = await apiGet<StudentMe>("/api/student-portal/me");
            resolved = { ...s, studentId: me.studentId, name: me.name || s.name };

            const [attendance, finance] = await Promise.all([
              apiGet<AttendanceHistory>("/api/student-portal/attendance/history").catch(() => ({ courses: [] })),
              apiGet<StudentFinance>(`/api/finance/v2/student-portal/${encodeURIComponent(me.studentId)}`).catch(() => ({ demands: [] })),
            ]);
            const totalSessions = attendance.courses.reduce((sum, course) => sum + course.totalSessions, 0);
            const presentSessions = attendance.courses.reduce((sum, course) => sum + course.presentSessions, 0);
            const attendancePercent = totalSessions > 0 ? Math.round((presentSessions / totalSessions) * 100) : 0;
            const outstanding = finance.demands.reduce((sum, demand) => sum + Number(demand.outstandingAmount || 0), 0);
            const currency = finance.demands[0]?.currency ?? "PKR";

            if (!cancelled) {
              setKpiValues({
                cgpa: me.cgpa ?? "--",
                credits: me.creditsCompleted ?? "--",
                attendance: attendancePercent,
                fees: outstanding > 0 ? `${currency} ${outstanding.toLocaleString("en-PK")} due` : "Paid",
              });
            }
          } catch {
            // A valid session still renders even if one student enrichment endpoint is unavailable.
          }
        }

        if (!cancelled) {
          setSession(resolved);
          setLoading(false);
        }
      })
      .catch(() => {
        if (!cancelled) {
          clearTokens();
          window.location.href = "/login";
        }
      });

    return () => { cancelled = true; };
  }, []);

  if (!session) return <AppShell><div className="content-page"><div className="skeleton" style={{ height: 200 }} /></div></AppShell>;

  const profile = getRoleProfile(session.roles);
  const { greeting } = getGreeting(session.name);
  const isExecutive = ["SuperAdmin", "VC", "Admin", "Finance"].includes(profile.role);
  const modules = [...profile.dashboardModules].sort((a, b) => a.priority - b.priority);

  return (
    <AppShell>
      <div className="content-page">
        <motion.div className="hero-greeting" initial={{ opacity: 0, y: -12 }} animate={{ opacity: 1, y: 0 }}>
          <h1>{greeting}</h1>
          <p>{profile.displayName} · {session.tenantId.slice(0, 8)}</p>
          <div className="hero-time">{new Date().toLocaleDateString("en-PK", { weekday: "long", year: "numeric", month: "long", day: "numeric" })}</div>
        </motion.div>

        <div className="kpi-grid" style={{ marginBottom: 24 }}>
          {profile.kpis.map(kpi => (
            <KpiCard key={kpi.id} kpi={kpi} value={loading ? "--" : (kpiValues[kpi.id] ?? kpi.value)} />
          ))}
        </div>

        {profile.quickActions.length > 0 && (
          <div className="quick-actions" style={{ marginBottom: 28 }}>
            {profile.quickActions.map(a => <QuickAction key={a.id} action={a} />)}
          </div>
        )}

        <div className="page-header" style={{ paddingTop: 0 }}><div><h2>Workspace</h2></div></div>
        <div className={`module-grid ${isExecutive ? "grid-cols-2" : ""}`} style={{ gridTemplateColumns: isExecutive ? "minmax(0,1.6fr) 320px" : undefined }}>
          <div style={{ display: "grid", gap: 16, gridTemplateColumns: "repeat(auto-fill,minmax(280px,1fr))", alignContent: "start" }}>
            {modules.slice(0, isExecutive ? 8 : 12).map((m, i) => <ModuleCard key={m.id} module={m} index={i} />)}
          </div>
          {isExecutive && (
            <div className="card" style={{ height: "fit-content" }}>
              <div className="card-header"><h3 style={{ fontSize: 14 }}>Priority Alerts</h3></div>
              <div className="card-body" style={{ display: "grid", gap: 8 }}>
                <div className="error-state" style={{ background: "var(--warning-soft)", border: "none", color: "var(--warning)", padding: 12 }}>
                  <ShieldCheck size={18} /><p style={{ fontSize: 12 }}>All clear — no critical escalations.</p>
                </div>
                {modules.filter(m => m.badge === "LIVE" || m.badge === "EXECUTIVE" || m.badge === "ACTIVE").slice(0, 4).map(m => (
                  <Link key={m.id} href={m.route} style={{ display: "flex", alignItems: "center", gap: 8, padding: "8px 0", textDecoration: "none", color: "inherit", fontSize: 13 }}>
                    <span style={{ width: 8, height: 8, borderRadius: "50%", background: getDomainColor(m.domain), flexShrink: 0 }} />
                    <span>{m.label}</span>
                    <span className="badge badge-primary" style={{ marginLeft: "auto", fontSize: 9 }}>{m.domain}</span>
                  </Link>
                ))}
              </div>
            </div>
          )}
        </div>
      </div>
    </AppShell>
  );
}
