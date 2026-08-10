"use client";

import { createContext, useContext, useEffect, useMemo, useRef, useState } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { AnimatePresence, motion } from "framer-motion";
import {
  Accessibility, Bell, BookOpenCheck, Briefcase, Building2, CalendarDays,
  ChevronDown, ChevronLeft, ChevronRight, CircleHelp, ClipboardCheck, CreditCard,
  FileSearch, FileText, GraduationCap, Landmark, LayoutDashboard, LogOut, Menu,
  Moon, Scale, Search, Settings, ShieldCheck, Sun, UserRound, Utensils, Vote,
} from "lucide-react";
import { apiGet, apiPost, clearTokens, getRefreshToken } from "@/lib/api-client";
import { getRoleProfile, getDomainColor } from "@/lib/roles";
import type { DashboardModule } from "@/lib/roles";

type Session = { userId: string; tenantId: string; studentId: string | null; departmentId: string | null; name: string; roles: string[] };

const ShellContext = createContext(false);

const iconMap: Record<string, typeof LayoutDashboard> = {
  LayoutDashboard, Landmark, FileText, CreditCard, BookOpenCheck, GraduationCap,
  ClipboardCheck, CalendarDays, ShieldCheck, Building2, Scale, FileSearch,
  Briefcase, UserRound, Vote, Utensils, Settings, CircleHelp, Search, LogOut,
};
function Icon({ name }: { name: string }) { const C = iconMap[name] ?? CircleHelp; return <C size={20} />; }

const studentAcademicExtras: DashboardModule[] = [
  { id:"student-courses", label:"My Courses", icon:"BookOpenCheck", route:"/student/courses", color:"blue", description:"Registered and completed courses, timings and status.", priority:4.1, domain:"academic" },
  { id:"student-workflow", label:"Workflow", icon:"ClipboardCheck", route:"/student/workflow", color:"blue", description:"Academic lifecycle and request progress.", priority:4.2, domain:"academic" },
  { id:"student-roadmap", label:"Road Map", icon:"FileSearch", route:"/student/roadmap", color:"blue", description:"Degree progress and graduation path.", priority:4.3, domain:"academic" },
  { id:"student-outcomes", label:"PLO Attainment", icon:"FileSearch", route:"/student/outcomes", color:"blue", description:"Student CLO/PLO outcome attainment matrix.", priority:4.4, domain:"academic", badge:"OBE" },
  { id:"student-survey", label:"Online Survey", icon:"ClipboardCheck", route:"/student/survey", color:"blue", description:"Course and faculty feedback surveys.", priority:4.5, domain:"academic" },
];

export function AppShell({ children }: { children: React.ReactNode }) {
  const nested = useContext(ShellContext);
  if (nested) return <>{children}</>;
  return <ShellContext.Provider value={true}><AppShellRoot>{children}</AppShellRoot></ShellContext.Provider>;
}

function AppShellRoot({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const [session, setSession] = useState<Session | null>(null);
  const [dark, setDark] = useState(false);
  const [collapsed, setCollapsed] = useState(false);
  const [menuOpen, setMenuOpen] = useState(false);
  const [fontScale, setFontScale] = useState(() => { try { return localStorage.getItem("aegis-font-scale") ?? "medium"; } catch { return "medium"; } });
  const [highContrast, setHighContrast] = useState(false);
  const searchRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    apiGet<Session>("/api/session")
      .then(s => {
        setSession(s);
        if (s.roles.includes("Student")) {
          apiGet<{ studentId: string; name: string }>("/api/student-portal/me").then(me => {
            setSession(prev => prev ? { ...prev, studentId: me.studentId, name: me.name || prev.name } : prev);
          }).catch(() => {});
        }
      })
      .catch(() => { clearTokens(); window.location.href = "/login"; });
  }, []);
  useEffect(() => { document.documentElement.dataset.theme = dark ? "dark" : "light"; }, [dark]);
  useEffect(() => { document.documentElement.dataset.fontScale = fontScale; localStorage.setItem("aegis-font-scale", fontScale); }, [fontScale]);
  useEffect(() => { document.documentElement.dataset.highContrast = highContrast ? "true" : "false"; }, [highContrast]);
  useEffect(() => {
    const handler = (e: KeyboardEvent) => { if ((e.metaKey || e.ctrlKey) && e.key === "k") { e.preventDefault(); searchRef.current?.focus(); } };
    document.addEventListener("keydown", handler);
    return () => document.removeEventListener("keydown", handler);
  }, []);

  const profile = useMemo(() => session ? getRoleProfile(session.roles) : null, [session]);
  const modules = useMemo(() => {
    const base = profile?.dashboardModules ?? [];
    return session?.roles.includes("Student") ? [...base, ...studentAcademicExtras] : base;
  }, [profile, session]);
  const grouped = useMemo(() => {
    const groups = new Map<string, DashboardModule[]>();
    for (const m of modules) { if (!groups.has(m.domain)) groups.set(m.domain, []); groups.get(m.domain)!.push(m); }
    const order = ["academic", "finance", "operations", "compliance", "welfare"];
    return [...groups.entries()].sort((a, b) => order.indexOf(a[0]) - order.indexOf(b[0]));
  }, [modules]);

  if (!session) return <main className="launch-screen"><img src="/aegiscore-logo.png" alt="AegisCore" style={{ width: 48, borderRadius: 10 }} /><div className="launch-pulse" /><p style={{ color: "var(--muted)", fontSize: 14 }}>Loading your university workspace…</p></main>;

  return (
    <div className="shell">
      <aside className={`sidebar ${collapsed ? "collapsed" : ""} ${menuOpen ? "open" : ""}`}>
        <div className="sidebar-logo"><img src="/aegiscore-logo.png" alt="AegisCore" />{!collapsed && <div><strong>AegisCore</strong><small>University ERP</small></div>}</div>
        <nav className="sidebar-nav">
          {grouped.map(([domain, items]) => <div key={domain} className="sidebar-nav-group">
            {!collapsed && <div className="sidebar-nav-label" style={{ color: getDomainColor(domain) }}>{domain}</div>}
            {items.sort((a,b)=>a.priority-b.priority).map(m => {
              const active = pathname === m.route || pathname?.startsWith(m.route + "/");
              return <Link key={m.id} href={m.route} className={active ? "active" : ""} onClick={() => setMenuOpen(false)}><Icon name={m.icon} />{!collapsed && <span>{m.label}</span>}{!collapsed && m.badge && <span className="badge badge-info" style={{ marginLeft: "auto", fontSize: 9 }}>{m.badge}</span>}</Link>;
            })}
          </div>)}
        </nav>
        <div className="sidebar-footer">
          <button onClick={() => { apiPost("/api/auth/logout", { refreshToken: getRefreshToken() }).catch(()=>{}); clearTokens(); window.location.href = "/login"; }} aria-label="Logout"><LogOut size={18} />{!collapsed && <span>Logout</span>}</button>
          <button onClick={() => setCollapsed(v => !v)} aria-label={collapsed ? "Expand" : "Collapse"}>{collapsed ? <ChevronRight size={18} /> : <ChevronLeft size={18} />}{!collapsed && <span>Collapse</span>}</button>
        </div>
      </aside>
      {menuOpen && <div className="sidebar-scrim" onClick={() => setMenuOpen(false)} />}
      <main className="shell-main">
        <header className="topbar">
          <button className="btn btn-ghost btn-sm" onClick={() => setMenuOpen(true)}><Menu size={20} /></button>
          <span className="topbar-title">{pathname === "/" ? "Command Center" : modules.find(m => pathname?.startsWith(m.route))?.label ?? "AegisCore"}</span>
          <div className="topbar-search"><Search size={16} /><input ref={searchRef} placeholder="Search anything… (⌘K)" /></div>
          <div className="topbar-actions">
            <button className="btn btn-ghost btn-sm" onClick={() => setFontScale(s => s === "medium" ? "large" : s === "large" ? "xlarge" : "medium")} title="Font size"><Accessibility size={18} /></button>
            <button className="btn btn-ghost btn-sm" onClick={() => setHighContrast(v => !v)} title="High contrast"><span style={{ fontSize: 14, fontWeight: 700 }}>{highContrast ? "HC" : "Aa"}</span></button>
            <button className="btn btn-ghost btn-sm" onClick={() => setDark(v => !v)} title="Toggle theme">{dark ? <Sun size={18} /> : <Moon size={18} />}</button>
            <div className="notification-bell"><Bell size={18} /></div>
            <div className="avatar-menu"><div className="topbar-avatar">{session.name[0]?.toUpperCase()}</div><div className="avatar-menu-info"><strong>{session.name.split(" ")[0]}</strong><span>{profile?.displayName}</span></div><ChevronDown size={14} /></div>
          </div>
        </header>
        <AnimatePresence mode="wait"><motion.div key={pathname} initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0, y: -8 }} transition={{ type: "spring", stiffness: 200, damping: 22 }}>{children}</motion.div></AnimatePresence>
      </main>
    </div>
  );
}
