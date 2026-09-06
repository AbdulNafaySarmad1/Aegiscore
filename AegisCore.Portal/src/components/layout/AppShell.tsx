"use client";

import { useEffect, useState, useMemo, useCallback, useRef } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { AnimatePresence, motion } from "framer-motion";
import {
  BookOpenCheck, Briefcase, Building2, CalendarDays, ChevronDown, ChevronLeft,
  ChevronRight, CircleHelp, ClipboardCheck, CreditCard, FileSearch, FileText,
  GraduationCap, Landmark, LayoutDashboard, LogOut, Menu, Moon, Scale, Search,
  Settings, ShieldCheck, Sun, UserRound, Utensils, Vote, X, Bell, Accessibility,
} from "lucide-react";
import { apiGet } from "@/lib/api-client";
import { getRoleProfile, getGreeting } from "@/lib/roles";
import type { DashboardModule } from "@/lib/roles";

type Session = { userId: string; tenantId: string; studentId: string | null; departmentId: string | null; name: string; roles: string[] };

const iconMap: Record<string, typeof LayoutDashboard> = {
  LayoutDashboard, Landmark, FileText, CreditCard, BookOpenCheck, GraduationCap,
  ClipboardCheck, CalendarDays, ShieldCheck, Building2, Scale, FileSearch,
  Briefcase, UserRound, Vote, Utensils, Settings, CircleHelp, Search, LogOut,
};
function Icon({ name }: { name: string }) { const C = iconMap[name] ?? CircleHelp; return <C size={20} aria-hidden="true" />; }

export function AppShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const [session, setSession] = useState<Session | null>(null);
  const [dark, setDark] = useState(false);
  const [collapsed, setCollapsed] = useState(() => typeof window !== "undefined" && window.matchMedia("(min-width: 768px) and (max-width: 1023px)").matches);
  const [menuOpen, setMenuOpen] = useState(false);
  const [fontScale, setFontScale] = useState(() => { try { return localStorage.getItem("aegis-font-scale") ?? "medium"; } catch { return "medium"; } });
  const [highContrast, setHighContrast] = useState(false);
  const searchRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    try {
      const raw = localStorage.getItem("aegiscore_demo_session");
      if (raw) {
        const acc = JSON.parse(raw);
        // eslint-disable-next-line react-hooks/set-state-in-effect -- client-only session hydration.
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
    } catch { window.location.href = "/login"; }
  }, []);
  useEffect(() => { document.documentElement.dataset.theme = dark ? "dark" : "light"; }, [dark]);
  useEffect(() => {
    document.documentElement.dataset.fontScale = fontScale;
    localStorage.setItem("aegis-font-scale", fontScale);
  }, [fontScale]);
  useEffect(() => {
    document.documentElement.dataset.highContrast = highContrast ? "true" : "false";
  }, [highContrast]);
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key === "k") { e.preventDefault(); searchRef.current?.focus(); }
    };
    document.addEventListener("keydown", handler);
    return () => document.removeEventListener("keydown", handler);
  }, []);

  const profile = useMemo(() => session ? getRoleProfile(session.roles) : null, [session]);
  const modules = profile?.dashboardModules ?? [];
  const grouped = useMemo(() => {
    const groups = new Map<string, DashboardModule[]>();
    for (const m of modules) { const k = m.domain; if (!groups.has(k)) groups.set(k, []); groups.get(k)!.push(m); }
    return [...groups.entries()].sort((a, b) => ["academic","finance","operations","compliance","welfare"].indexOf(a[0])-["academic","finance","operations","compliance","welfare"].indexOf(b[0]));
  }, [modules]);

  if (!session) return (
    <main id="main-content" className="launch-screen" tabIndex={-1}>
      <img src="/aegiscore-logo.png" alt="AegisCore" style={{ width: 48, borderRadius: 10 }} />
      <div className="launch-pulse" />
      <p style={{ color: "var(--muted)", fontSize: 14 }}>Loading your university workspace…</p>
    </main>
  );

  return (
    <div className="shell">
      {/* Sidebar */}
      <aside className={`sidebar ${collapsed ? "collapsed" : ""} ${menuOpen ? "open" : ""}`} aria-label="Application navigation">
        <div className="sidebar-logo">
          <img src="/aegiscore-logo.png" alt="AegisCore" />
          {!collapsed && <div><strong>AegisCore</strong><small>University ERP</small></div>}
        </div>

        <nav className="sidebar-nav" aria-label="Primary navigation">
          {grouped.map(([domain, items]) => (
            <div key={domain} className="sidebar-nav-group">
              {!collapsed && <div className="sidebar-nav-label" data-domain={domain}>{domain}</div>}
              {items.map(m => {
                const active = (pathname ?? "") === m.route || (pathname ?? "").startsWith(m.route + "/");
                return (
                  <Link key={m.id} href={m.route} className={active ? "active" : ""} aria-label={collapsed ? m.label : undefined} aria-current={active ? "page" : undefined} onClick={() => setMenuOpen(false)}>
                    <Icon name={m.icon} />
                    {!collapsed && <span>{m.label}</span>}
                    {!collapsed && m.badge && <span className="badge badge-info" style={{ marginLeft: "auto", fontSize: 9 }}>{m.badge}</span>}
                  </Link>
                );
              })}
            </div>
          ))}
        </nav>

        <div className="sidebar-footer">
          <button onClick={() => setCollapsed(v => !v)} aria-label={collapsed ? "Expand navigation" : "Collapse navigation"} aria-expanded={!collapsed}>
            {collapsed ? <ChevronRight size={18} /> : <ChevronLeft size={18} />}
            {!collapsed && <span>Collapse</span>}
          </button>
        </div>
      </aside>
      {menuOpen && <button type="button" className="sidebar-scrim" onClick={() => setMenuOpen(false)} aria-label="Close navigation" />}

      {/* Main */}
      <main id="main-content" className="shell-main" tabIndex={-1}>
        {/* Topbar */}
        <header className="topbar">
          <button className="btn btn-ghost btn-sm mobile-menu-button" aria-label="Open navigation" onClick={() => setMenuOpen(true)}><Menu size={20} /></button>
          <span className="topbar-title">{(pathname ?? "/") === "/" ? "Command Center" : modules.find(m => (pathname ?? "").startsWith(m.route))?.label ?? "AegisCore"}</span>

          <div className="topbar-search">
            <Search size={16} />
            <input ref={searchRef} type="search" aria-label="Search modules" placeholder="Search anything… (⌘K)" />
          </div>

          <div className="topbar-actions">
            <button className="btn btn-ghost btn-sm" onClick={() => setFontScale(s => s === "medium" ? "large" : s === "large" ? "xlarge" : "medium")} aria-label="Change text size" title="Font size">
              <Accessibility size={18} />
            </button>
            <button className="btn btn-ghost btn-sm" onClick={() => setHighContrast(v => !v)} aria-label="Toggle high contrast" aria-pressed={highContrast} title="High contrast">
              <span style={{ fontSize: 14, fontWeight: 700 }}>{highContrast ? "HC" : "Aa"}</span>
            </button>
            <button className="btn btn-ghost btn-sm" onClick={() => setDark(v => !v)} aria-label="Toggle dark theme" aria-pressed={dark} title="Toggle theme">
              {dark ? <Sun size={18} /> : <Moon size={18} />}
            </button>
            <button type="button" className="notification-bell" aria-label="Open notifications"><Bell size={18} aria-hidden="true" /></button>
            <div className="avatar-menu">
              <div className="topbar-avatar">{session.name[0]?.toUpperCase()}</div>
              <div className="avatar-menu-info">
                <strong>{session.name.split(" ")[0]}</strong>
                <span>{profile?.displayName}</span>
              </div>
              <ChevronDown size={14} />
            </div>
          </div>
        </header>

        <AnimatePresence mode="wait">
          <motion.div key={pathname} initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0, y: -8 }} transition={{ type: "spring", stiffness: 200, damping: 22 }}>
            {children}
          </motion.div>
        </AnimatePresence>
      </main>
    </div>
  );
}
