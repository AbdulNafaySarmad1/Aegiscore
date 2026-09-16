"use client";
import { loadSession } from "@/lib/session";

import { useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { getRoleProfile } from "@/lib/roles";
import { AppShell } from "@/components/layout/AppShell";
import { useTenantFormatter } from "@/components/TenantPreferencesProvider";
import { RegisterHead, RuledLoading, Mark, Figures, type Figure } from "@/components/register";
import { hasKpiSource, readKpi, type KpiReading } from "@/lib/kpi-sources";
import {
  BookOpenCheck, Briefcase, Building2, CalendarDays, ClipboardCheck, CreditCard,
  FileSearch, FileText, GraduationCap, Landmark, LayoutDashboard, Scale, ShieldCheck,
  UserRound, Vote, Utensils, Settings, CircleHelp,
} from "lucide-react";
import type { DashboardModule } from "@/lib/roles";

type Session = { userId: string; tenantId: string; studentId: string | null; departmentId: string | null; name: string; roles: string[] };

const icons: Record<string, typeof LayoutDashboard> = {
  LayoutDashboard, Landmark, FileText, CreditCard, BookOpenCheck, GraduationCap, ClipboardCheck, CalendarDays,
  ShieldCheck, Building2, Scale, FileSearch, Briefcase, UserRound, Vote, Utensils, Settings, CircleHelp,
};
const Icon = ({ name }: { name: string }) => { const C = icons[name] ?? LayoutDashboard; return <C aria-hidden="true" />; };

const DOMAIN_ORDER = ["academic", "finance", "operations", "compliance", "welfare"];
const DOMAIN_NAME: Record<string, string> = { academic: "Academic", finance: "Finance", operations: "Operations", compliance: "Compliance", welfare: "Student welfare" };

export default function DashboardPage() {
  const { date } = useTenantFormatter();
  const [session, setSession] = useState<Session | null>(null);
  const [readings, setReadings] = useState<Record<string, KpiReading>>({});

  useEffect(() => {
    loadSession().then(setSession).catch(() => window.location.assign("/login"));
  }, []);

  const profile = useMemo(() => session ? getRoleProfile(session.roles) : null, [session]);
  const sourced = useMemo(() => (profile?.kpis ?? []).filter(k => hasKpiSource(k.id)), [profile]);

  useEffect(() => {
    if (sourced.length === 0) return;
    const abort = new AbortController();
    for (const kpi of sourced) {
      readKpi(kpi.id, abort.signal).then(reading => { if (!abort.signal.aborted) setReadings(r => ({ ...r, [kpi.id]: reading })); });
    }
    return () => abort.abort();
  }, [sourced]);
  const grouped = useMemo(() => {
    const groups = new Map<string, DashboardModule[]>();
    for (const m of [...(profile?.dashboardModules ?? [])].sort((a, b) => a.priority - b.priority)) {
      if (!groups.has(m.domain)) groups.set(m.domain, []);
      groups.get(m.domain)!.push(m);
    }
    return [...groups.entries()].sort((a, b) => DOMAIN_ORDER.indexOf(a[0]) - DOMAIN_ORDER.indexOf(b[0]));
  }, [profile]);

  if (!session || !profile) return <AppShell><div className="page"><RuledLoading label="Opening your register" rows={5} /></div></AppShell>;

  const today = new Date();
  const hour = today.getHours();
  const greeting = `Good ${hour < 12 ? "morning" : hour < 17 ? "afternoon" : "evening"}, ${session.name}.`;

  return (
    <AppShell>
      <div className="page">
        <RegisterHead
          title={greeting}
          ident={<><strong>{date(today)}</strong><br />{profile.displayName}</>}
          meta={<span>{profile.dashboardModules.length} modules available to your role.</span>}
        />

        {sourced.length > 0 && (
          <Figures aria-label="Figures" items={sourced.map<Figure>(kpi => {
            const reading = readings[kpi.id];
            if (!reading) return { label: kpi.label, value: "…", note: "reading" };
            if (reading.state === "value") return { label: kpi.label, value: reading.value, note: reading.note, tone: reading.value === "—" ? undefined : "laurel" };
            return { label: kpi.label, value: "—", note: reading.note, tone: reading.state === "forbidden" ? "brass" : undefined };
          })} />
        )}

        {profile.quickActions.length > 0 && (
          <section aria-labelledby="quick-actions-title" className="panel-stack">
            <div className="chapter"><h2 id="quick-actions-title">Today</h2><span>Frequent tasks</span></div>
            <div className="quick-actions">
              {profile.quickActions.map(a => (
                <Link key={a.id} href={a.route} className="quick-action-btn"><Icon name={a.icon} />{a.label}</Link>
              ))}
            </div>
          </section>
        )}

        <section aria-labelledby="index-title" className="panel-stack">
          <div className="chapter"><h2 id="index-title">Index</h2><span>Every module, by office</span></div>
          <div className="index">
            {grouped.map(([domain, items]) => (
              <div key={domain} className="index-group">
                <div className="index-group-name">{DOMAIN_NAME[domain] ?? domain}</div>
                <div className="index-group-items">
                  {items.map(m => (
                    <Link key={m.id} href={m.route} className="index-item">
                      <Icon name={m.icon} />
                      <span>
                        <strong>{m.label}{m.badge && <Mark tone="brass">{m.badge.charAt(0) + m.badge.slice(1).toLowerCase()}</Mark>}</strong>
                        <p>{m.description}</p>
                      </span>
                    </Link>
                  ))}
                </div>
              </div>
            ))}
          </div>
        </section>
      </div>
    </AppShell>
  );
}
