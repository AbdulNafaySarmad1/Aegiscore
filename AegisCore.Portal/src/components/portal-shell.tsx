"use client";

import { BookOpenCheck, Building2, CalendarDays, CircleHelp, CreditCard, FileText, GraduationCap, Landmark, LayoutDashboard, Menu, Moon, Settings, ShieldCheck, Sun, UserRound, X } from "lucide-react";
import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { apiGet } from "@/lib/api-client";
import { LocaleSwitcher } from "@/components/locale-switcher";
import { useTranslations } from "next-intl";

type Session = { userId: string; tenantId: string; studentId: string | null; departmentId: string | null; name: string; roles: string[] };
type LinkItem = { label: string; href: string; icon: typeof LayoutDashboard };

const financeRoles = new Set(["Finance", "Finance Controller", "CFO", "Admin", "SuperAdmin"]);

export function PortalShell() {
  const t = useTranslations("shell");
  const [session, setSession] = useState<Session | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [dark, setDark] = useState(false);
  const [menuOpen, setMenuOpen] = useState(false);

  useEffect(() => { apiGet<Session>("/api/session").then(setSession).catch(() => setError(t("signIn"))); }, [t]);
  useEffect(() => { document.documentElement.dataset.theme = dark ? "dark" : "light"; }, [dark]);
  const links = useMemo<LinkItem[]>(() => {
    if (!session) return [];
    const result: LinkItem[] = [{ label: t("commandCenter"), href: "/", icon: LayoutDashboard }];
    if (session.studentId) result.push({ label: t("feesAid"), href: `/student/finance/${session.studentId}`, icon: CreditCard }, { label: t("academics"), href: "/academics", icon: GraduationCap });
    if (session.departmentId) result.push({ label: t("departmentFinance"), href: `/finance/department/${session.departmentId}`, icon: Landmark });
    if (session.roles.some(role => financeRoles.has(role))) result.push({ label: t("financeDashboard"), href: "/finance", icon: Landmark }, { label: t("financeOperations"), href: "/finance/operations", icon: FileText }, { label: t("paymentGateways"), href: "/finance/gateways", icon: CreditCard });
    if (session.roles.some(role => role === "Auditor" || financeRoles.has(role))) result.push({ label: t("auditCompliance"), href: "/audit", icon: ShieldCheck });
    if (session.roles.some(role => role === "Registrar" || role === "Admin" || role === "SuperAdmin")) result.push({ label: t("universityStructure"), href: "/administration", icon: Building2 }, { label: t("settings"), href: "/settings", icon: Settings });
    return result;
  }, [session, t]);

  if (!session) return <main className="launch-screen"><Brand /><div className="launch-pulse" />{error ? <p role="alert">{error}</p> : <p>{t("loading")}</p>}</main>;
  return <div className="portal-shell">
    <aside className={`portal-sidebar ${menuOpen ? "open" : ""}`}><div className="sidebar-top"><Brand /><button className="icon-button mobile-only" onClick={() => setMenuOpen(false)} aria-label={t("closeNavigation")}><X /></button></div><nav className="primary-nav" aria-label={t("primaryNavigation")}><section><h2>{t("workspace")}</h2>{links.map(item => <Link key={item.href} href={item.href} onClick={() => setMenuOpen(false)}><item.icon aria-hidden /><span>{item.label}</span></Link>)}</section></nav><div className="sidebar-footer"><Link href="/support"><CircleHelp />{t("help")}</Link></div></aside>
    {menuOpen && <button className="sidebar-scrim" onClick={() => setMenuOpen(false)} aria-label={t("closeNavigation")} />}
    <main className="portal-main"><header className="portal-header"><button className="icon-button mobile-only" onClick={() => setMenuOpen(true)} aria-label={t("openNavigation")}><Menu /></button><Brand /><div className="header-actions"><LocaleSwitcher label={t("locale")} /><button className="icon-button" onClick={() => setDark(value => !value)} aria-label={t("toggleTheme")}>{dark ? <Sun /> : <Moon />}</button><div className="profile-chip"><UserRound /><span><strong>{session.name}</strong><small>{session.roles.join(", ") || t("user")}</small></span></div></div></header>
      <section className="command-page"><div className="page-heading"><div><p>{t("tenantWorkspace")}</p><h1>{t("title")}</h1><span>{t("subtitle")}</span></div></div><section className="quick-actions" aria-label={t("available")}><h2>{t("available")}</h2><div>{links.slice(1).map(item => <Link key={item.href} href={item.href}><item.icon /><span><strong>{item.label}</strong><small>{t("openWorkspace")}</small></span></Link>)}</div></section><section className="surface finance-panel"><div className="surface-heading"><div><h2>{t("sessionSecurity")}</h2><p>{t("sessionDetail")}</p></div><ShieldCheck /></div><div className="finance-control-list"><div className="finance-control"><ShieldCheck /><span><strong>{t("authenticated")}</strong><small>Tenant {session.tenantId}</small></span></div><div className="finance-control"><BookOpenCheck /><span><strong>{t("roleNavigation")}</strong><small>{t("assignedRoles", { count: session.roles.length })}</small></span></div><div className="finance-control"><CalendarDays /><span><strong>{t("liveData")}</strong><small>{t("noSimulation")}</small></span></div></div></section></section>
    </main>
  </div>;
}

function Brand() { return <Link className="aegis-brand" href="/" aria-label="AegisCore home"><span className="brand-glyph"><ShieldCheck /></span><span><strong>AegisCore</strong><small>University ERP</small></span></Link>; }
