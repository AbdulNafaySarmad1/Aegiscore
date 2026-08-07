"use client";

import { useEffect, useState, useMemo } from "react";
import Link from "next/link";
import { motion } from "framer-motion";
import { apiGet } from "@/lib/api-client";
import { getRoleProfile, getDomainColor, getGreeting } from "@/lib/roles";
import { AppShell } from "@/components/layout/AppShell";
import {
  BookOpenCheck, Briefcase, Building2, CalendarDays, ClipboardCheck, CreditCard,
  FileSearch, FileText, GraduationCap, Landmark, LayoutDashboard, Scale, ShieldCheck,
  UserRound, ArrowUpRight, ArrowDownRight, Minus,
} from "lucide-react";
import type { KPIDefinition, QuickAction, DashboardModule } from "@/lib/roles";

type Session = { userId: string; tenantId: string; studentId: string | null; departmentId: string | null; name: string; roles: string[] };

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

function KpiCard({ kpi, value }: { kpi: KPIDefinition; value: string }) {
  const formatted = kpi.format === "currency" ? `₨ ${Number(value).toLocaleString("en-PK")}`
    : kpi.format === "percentage" ? `${value}%` : String(value);
  return (
    <motion.article className="kpi-card" whileHover={{ y: -2 }} initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} transition={{ type: "spring", stiffness: 180, damping: 20 }}>
      <div className={`kpi-card-icon kpi-accent ${kpi.iconColor}`}><Icon name={kpi.icon} /></div>
      <span className="kpi-label">{kpi.label}</span>
      <strong className="kpi-number">{formatted}</strong>
      {kpi.trend && <span className="kpi-trend"><TrendArrow trend={kpi.trend} />{kpi.trendLabel}</span>}
    </motion.article>
  );
}

function QuickAction({ action }: { action: QuickAction }) {
  return (
    <Link href={action.route} className="quick-action-btn"><ActionIcon name={action.icon} />{action.label}</Link>
  );
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

  useEffect(() => {
    try {
      const raw = localStorage.getItem("aegiscore_demo_session");
      if (raw) {
        const acc = JSON.parse(raw);
        setSession({
          userId: acc.email,
          tenantId: "iobm-demo-tenant",
          studentId: acc.label === "Student" ? "STU-2024-001" : null,
          departmentId: "CS-DEPT-001",
          name: acc.desc,
          roles: [acc.label],
        });
      } else {
        window.location.href = "/login";
      }
    } catch {
      window.location.href = "/login";
    }
    setLoading(false);
  }, []);

  if (!session) return <AppShell><div className="content-page"><div className="skeleton" style={{ height: 200 }} /></div></AppShell>;

  const profile = getRoleProfile(session.roles);
  const { greeting } = getGreeting(session.name);
  const isExecutive = ["SuperAdmin", "VC", "Admin", "Finance"].includes(profile.role);
  const modules = profile.dashboardModules.sort((a, b) => a.priority - b.priority);

  return (
    <AppShell>
      <div className="content-page">
        {/* Hero */}
        <motion.div className="hero-greeting" initial={{ opacity: 0, y: -12 }} animate={{ opacity: 1, y: 0 }}>
          <h1>{greeting}</h1>
          <p>{profile.displayName} · {session.tenantId.slice(0, 8)}</p>
          <div className="hero-time">{new Date().toLocaleDateString("en-PK", { weekday: "long", year: "numeric", month: "long", day: "numeric" })}</div>
        </motion.div>

        {/* KPIs */}
        <div className="kpi-grid" style={{ marginBottom: 24 }}>
          {profile.kpis.map(kpi => (
            <KpiCard key={kpi.id} kpi={kpi} value={loading ? "--" : String(kpi.value)} />
          ))}
        </div>

        {/* Quick Actions */}
        {profile.quickActions.length > 0 && (
          <div className="quick-actions" style={{ marginBottom: 28 }}>
            {profile.quickActions.map(a => <QuickAction key={a.id} action={a} />)}
          </div>
        )}

        {/* Module Grid */}
        <div className="page-header" style={{ paddingTop: 0 }}>
          <div><h2>Workspace</h2></div>
        </div>
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
