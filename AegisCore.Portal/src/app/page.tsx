"use client";

// ─── Role-aware Command Center ───
// Live KPI values come exclusively from verified backend contracts
// (docs/FRONTEND_API_HANDOFF.md). Metrics without a verified source are
// omitted from role profiles rather than fabricated.

import { useMemo } from "react";
import Link from "next/link";
import { motion } from "framer-motion";
import { apiGet } from "@/lib/api-client";
import { getDomainColor, getGreeting, getRoleProfile } from "@/lib/roles";
import type { DashboardModule, KPIDefinition, LiveMetric, QuickAction } from "@/lib/roles";
import { AppShell } from "@/components/layout/AppShell";
import { useSession } from "@/components/session-provider";
import { useAsyncData } from "@/hooks/use-async-data";
import {
  BookOpenCheck, Briefcase, Building2, CalendarDays, ClipboardCheck, CreditCard,
  FileSearch, FileText, GraduationCap, Landmark, LayoutDashboard, Scale, ShieldCheck,
  UserRound, ArrowUpRight, ArrowDownRight, Minus,
} from "lucide-react";

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

// Verified contract: GET /api/finance/v2/dashboard/executive
type ExecutiveDashboard = {
  kpis?: { revenue?: number; expenses?: number; cashPosition?: number };
  feeFunnel?: { demanded?: number; collected?: number; overdue?: number; open?: number; paid?: number };
};

function extractMetric(data: ExecutiveDashboard | null, metric: LiveMetric): number | null {
  if (!data) return null;
  switch (metric) {
    case "revenue": return data.kpis?.revenue ?? null;
    case "cashPosition": return data.kpis?.cashPosition ?? null;
    case "outstanding": return data.feeFunnel?.overdue ?? null;
    case "collectionRate": {
      const demanded = data.feeFunnel?.demanded;
      const collected = data.feeFunnel?.collected;
      if (!demanded || demanded <= 0 || collected == null) return null;
      return Math.round((collected / demanded) * 1000) / 10;
    }
  }
}

function formatKpi(kpi: KPIDefinition, value: number): string {
  if (kpi.format === "currency") {
    return `₨ ${new Intl.NumberFormat("en-PK", { notation: "compact", maximumFractionDigits: 1 }).format(value)}`;
  }
  if (kpi.format === "percentage") return `${value}%`;
  return new Intl.NumberFormat("en-PK").format(value);
}

function KpiCard({ kpi, value, loading }: { kpi: KPIDefinition; value: number | null; loading: boolean }) {
  return (
    <motion.article className="kpi-card" initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.2 }}>
      <div className={`kpi-card-icon kpi-accent ${kpi.iconColor}`}><Icon name={kpi.icon} /></div>
      <span className="kpi-label">{kpi.label}</span>
      {loading ? (
        <div className="skeleton" style={{ height: 32, width: "60%", margin: "4px 0" }} />
      ) : value == null ? (
        <>
          <strong className="kpi-number unavailable">—</strong>
          <span className="kpi-unavailable-note">Not available</span>
        </>
      ) : (
        <strong className="kpi-number">{formatKpi(kpi, value)}</strong>
      )}
      {!loading && value != null && kpi.trend && <span className="kpi-trend"><TrendArrow trend={kpi.trend} />{kpi.trendLabel}</span>}
    </motion.article>
  );
}

function QuickActionLink({ action }: { action: QuickAction }) {
  return <Link href={action.route} className="quick-action-btn"><ActionIcon name={action.icon} />{action.label}</Link>;
}

function ModuleCard({ module: m, index }: { module: DashboardModule; index: number }) {
  return (
    <motion.article className="module-card" initial={{ opacity: 0, y: 16 }} animate={{ opacity: 1, y: 0 }} transition={{ delay: Math.min(index, 8) * .03, duration: 0.2 }}>
      <div className="module-card-accent" style={{ background: getDomainColor(m.domain) }} />
      <div className="module-card-body">
        <div className="module-card-icon" style={{ color: getDomainColor(m.domain), background: "var(--surface-muted)" }}><Icon name={m.icon} /></div>
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
  const { session } = useSession();
  const profile = useMemo(() => (session ? getRoleProfile(session.roles) : null), [session]);

  const needsExecutive = profile?.kpis.some(k => k.metric) ?? false;
  const executive = useAsyncData<ExecutiveDashboard | null>(
    signal => (needsExecutive
      ? apiGet<ExecutiveDashboard>("/api/finance/v2/dashboard/executive", signal)
      : Promise.resolve(null)),
    [needsExecutive]
  );
  const executiveData = needsExecutive && !executive.error ? executive.data : null;
  const kpisLoading = needsExecutive && executive.loading;

  if (!session || !profile) {
    return <AppShell><div className="content-page"><div className="skeleton" style={{ height: 200 }} /></div></AppShell>;
  }

  const { greeting } = getGreeting(session.name);
  const modules = profile.dashboardModules.slice().sort((a, b) => a.priority - b.priority);

  return (
    <AppShell>
      <div className="content-page">
        {/* Hero */}
        <motion.div className="hero-greeting" initial={{ opacity: 0 }} animate={{ opacity: 1 }} transition={{ duration: 0.2 }}>
          <h1>{greeting}</h1>
          <p>{profile.displayName} workspace</p>
          <div className="hero-time">{new Date().toLocaleDateString("en-PK", { weekday: "long", year: "numeric", month: "long", day: "numeric" })}</div>
        </motion.div>

        {/* KPIs — only roles with verified live metrics render this section */}
        {profile.kpis.length > 0 && (
          <div className="kpi-grid" style={{ marginBottom: 24 }}>
            {profile.kpis.map(kpi => (
              <KpiCard
                key={kpi.id}
                kpi={kpi}
                loading={kpisLoading}
                value={kpi.metric ? extractMetric(executiveData, kpi.metric) : null}
              />
            ))}
          </div>
        )}

        {/* Quick Actions */}
        {profile.quickActions.length > 0 && (
          <div className="quick-actions" style={{ marginBottom: 28 }}>
            {profile.quickActions.map(a => <QuickActionLink key={a.id} action={a} />)}
          </div>
        )}

        {/* Module Grid */}
        <div className="page-header" style={{ paddingTop: 0 }}>
          <div><h2>Workspace</h2></div>
        </div>
        <div className="module-grid">
          {modules.map((m, i) => <ModuleCard key={m.id} module={m} index={i} />)}
        </div>
      </div>
    </AppShell>
  );
}
