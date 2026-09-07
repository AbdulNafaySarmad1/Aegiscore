"use client";
import { loadSession } from "@/lib/session";

import {
  BookOpenCheck, Briefcase, Building2, CalendarDays, ChevronDown, ChevronLeft, ChevronRight,
  CircleHelp, ClipboardCheck, CreditCard, FileSearch, FileText, GraduationCap,
  Landmark, LayoutDashboard, LogOut, Menu, Moon, Scale, Search, Settings,
  ShieldCheck, Sun, UserRound, Utensils, Vote, X,
} from "lucide-react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useMemo, useState, useCallback, useRef } from "react";
import { AnimatePresence, motion } from "framer-motion";
import { apiGet } from "@/lib/api-client";
import { LocaleSwitcher } from "@/components/locale-switcher";
import { PersonalityToggle } from "@/components/personality-toggle";
import { PersonalityModeProvider } from "@/lib/personality-context";
import { useTranslations } from "next-intl";
import type { DashboardModule } from "@/lib/roles";
import { getRoleProfile, getDomainColor } from "@/lib/roles";

type Session = { userId: string; tenantId: string; studentId: string | null; departmentId: string | null; name: string; roles: string[] };

const iconMap: Record<string, typeof LayoutDashboard> = {
  LayoutDashboard, Landmark, FileText, CreditCard, BookOpenCheck, GraduationCap,
  ClipboardCheck, CalendarDays, ShieldCheck, Building2, Scale, FileSearch,
  Briefcase, UserRound, Vote, Utensils, Settings, CircleHelp, Search,
};

function Icon({ name }: { name: string }) {
  const C = iconMap[name] ?? CircleHelp;
  return <C />;
}

function useTracker() {
  const [recents, setRecents] = useState<string[]>(() => {
    try { return JSON.parse(localStorage.getItem("aegis_recents") ?? "[]"); } catch { return []; }
  });
  const [pinned, setPinned] = useState<string[]>(() => {
    try { return JSON.parse(localStorage.getItem("aegis_pinned") ?? "[]"); } catch { return []; }
  });

  const track = useCallback((route: string) => {
    setRecents(prev => {
      const next = [route, ...prev.filter(r => r !== route)].slice(0, 8);
      localStorage.setItem("aegis_recents", JSON.stringify(next));
      return next;
    });
  }, []);

  const togglePin = useCallback((moduleId: string) => {
    setPinned(prev => {
      const next = prev.includes(moduleId) ? prev.filter(p => p !== moduleId) : [...prev, moduleId];
      localStorage.setItem("aegis_pinned", JSON.stringify(next));
      return next;
    });
  }, []);

  return { recents, pinned, track, togglePin };
}

export function PortalShell({ children }: { children?: React.ReactNode }) {
  const t = useTranslations("shell");
  const pathname = usePathname();
  const [session, setSession] = useState<Session | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [dark, setDark] = useState(false);
  const [menuOpen, setMenuOpen] = useState(false);
  const [collapsed, setCollapsed] = useState(false);
  const [demoStatus, setDemoStatus] = useState<{ isDemo: boolean; remainingDays: number } | null>(null);
  const [search, setSearch] = useState("");
  const [searchOpen, setSearchOpen] = useState(false);
  const searchRef = useRef<HTMLInputElement>(null);
  const { recents, pinned, track, togglePin } = useTracker();

  useEffect(() => { apiGet<{ isDemo: boolean; remainingDays: number }>("/api/wizard/demo-status").then(setDemoStatus).catch(() => {}); }, []);
  useEffect(() => {
    loadSession().then(setSession).catch(() => window.location.assign("/login"));
  }, []);
  useEffect(() => { document.documentElement.dataset.theme = dark ? "dark" : "light"; }, [dark]);

  useEffect(() => { track(pathname ?? "/"); }, [pathname, track]);

  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key === "k") { e.preventDefault(); setSearchOpen(true); setTimeout(() => searchRef.current?.focus(), 50); }
      if (e.key === "Escape") setSearchOpen(false);
    };
    document.addEventListener("keydown", handler);
    return () => document.removeEventListener("keydown", handler);
  }, []);

  const profile = useMemo(() => session ? getRoleProfile(session.roles) : null, [session]);
  const modules = profile?.dashboardModules ?? [];
  const grouped = useMemo(() => {
    const groups = new Map<string, DashboardModule[]>();
    for (const m of modules) {
      const key = m.domain;
      if (!groups.has(key)) groups.set(key, []);
      groups.get(key)!.push(m);
    }
    return [...groups.entries()].sort((a, b) => {
      const order = ["academic", "finance", "operations", "compliance", "welfare"];
      return order.indexOf(a[0]) - order.indexOf(b[0]);
    });
  }, [modules]);

  const filteredModules = useMemo(() => {
    if (!search) return modules;
    const q = search.toLowerCase();
    return modules.filter(m => m.label.toLowerCase().includes(q) || m.description.toLowerCase().includes(q));
  }, [modules, search]);

  if (!session) return (
    <main className="launch-screen">
      <div className="aegis-brand"><img src="/aegiscore-logo.png" alt="AegisCore" style={{ height: 32, borderRadius: 6 }} /><div className="brand-text"><strong>AegisCore</strong><small>University ERP</small></div></div>
      <div className="launch-pulse" />
      {error ? <p role="alert">{t(error)}</p> : <p>{t("loading")}</p>}
    </main>
  );

  const activeRoute = (pathname ?? "/") === "/" ? "/" : (pathname ?? "/").split("/").slice(0, 2).join("/");

  return (
    <PersonalityModeProvider userId={session.userId}>
      <div className="shell-root">
        {/* Sidebar */}
        <motion.aside
          className={`shell-sidebar ${menuOpen ? "open" : ""}`}
          animate={{ width: collapsed ? 64 : 240 }}
          transition={{ type: "spring", stiffness: 280, damping: 26 }}
        >
          <div className="sidebar-top">
            <Link href="/" className="aegis-brand inverse" style={{ textDecoration: "none", overflow: "hidden" }}>
              <img src="/aegiscore-logo.png" alt="AegisCore" style={{ height: collapsed ? 28 : 32, borderRadius: 6 }} />
              {!collapsed && <div className="brand-text"><strong>AegisCore</strong><small>University ERP</small></div>}
            </Link>
            <button className="icon-button mobile-only" style={{ color: "#fff", borderColor: "#344463" }} onClick={() => setMenuOpen(false)} aria-label={t("closeNavigation")}><X /></button>
          </div>

          {!collapsed && (
            <motion.div className="sidebar-user" initial={{ opacity: 0 }} animate={{ opacity: 1 }} transition={{ delay: 0.1 }}>
              <div className="institution-mark">{(session.name[0] ?? "A").toUpperCase()}</div>
              <div>
                <strong>{session.name}</strong>
                <small>{profile?.displayName}</small>
              </div>
            </motion.div>
          )}

          <nav className="sidebar-nav" aria-label={t("primaryNavigation")}>
            {grouped.map(([domain, items]) => (
              <div key={domain} className="nav-group">
                {!collapsed && <h2 className="nav-heading" style={{ color: getDomainColor(domain) }}>{domain.toUpperCase()}</h2>}
                {items.map(m => (
                  <Link
                    key={m.id}
                    href={m.route}
                    className={`nav-item ${activeRoute === m.route || activeRoute === m.route + "/" ? "active" : ""}`}
                    onClick={() => setMenuOpen(false)}
                    title={m.label}
                  >
                    <span className="nav-accent" style={{ background: activeRoute === m.route ? getDomainColor(domain) : "transparent" }} />
                    <Icon name={m.icon} />
                    {!collapsed && <span>{m.label}</span>}
                    {!collapsed && m.badge && <b className="nav-badge">{m.badge}</b>}
                  </Link>
                ))}
              </div>
            ))}
          </nav>

          <div className="sidebar-footer">
            <Link href="/support" className="nav-item" title={t("help")}><Icon name="CircleHelp" />{!collapsed && <span>{t("help")}</span>}</Link>
            <Link href="/settings" className="nav-item" title={t("settings")}><Icon name="Settings" />{!collapsed && <span>{t("settings")}</span>}</Link>
            <button className="nav-item" onClick={() => setCollapsed(v => !v)} title={collapsed ? "Expand sidebar" : "Collapse sidebar"}>
              {collapsed ? <ChevronRight /> : <ChevronLeft />}
            </button>
          </div>
        </motion.aside>

        {/* Scrim */}
        {menuOpen && <button className="sidebar-scrim" onClick={() => setMenuOpen(false)} aria-label={t("closeNavigation")} />}

        {/* Main */}
        <main className="shell-main">
          {/* Topbar */}
          <header className="shell-topbar">
            <button className="icon-button mobile-only" onClick={() => setMenuOpen(true)} aria-label={t("openNavigation")}><Menu /></button>
            <span className="topbar-title">{pathname === "/" ? "Command Center" : modules.find(m => m.route === activeRoute)?.label ?? "AegisCore"}</span>

            {/* Search */}
            <div className={`global-search ${searchOpen ? "open" : ""}`}>
              <Search />
              <input ref={searchRef} placeholder="Search modules…" value={search} onChange={e => setSearch(e.target.value)} onFocus={() => setSearchOpen(true)} onBlur={() => setTimeout(() => setSearchOpen(false), 200)} />
              <kbd><span>⌘</span>K</kbd>
            </div>

            <div className="header-actions">
              <LocaleSwitcher label={t("locale")} />
              <PersonalityToggle />
              <button className="icon-button" onClick={() => setDark(v => !v)} aria-label={t("toggleTheme")}>{dark ? <Sun /> : <Moon />}</button>
              <div className="profile-chip">
                <span className="profile-avatar">{session.name[0]?.toUpperCase() ?? "A"}</span>
                {!collapsed && (
                  <>
                    <span><strong>{session.name}</strong><small>{session.roles[0]}</small></span>
                    <ChevronDown size={15} />
                  </>
                )}
              </div>
            </div>
          </header>

          {/* Demo banner */}
          {demoStatus?.isDemo && (
            <div className="connection-banner" style={{ background: "var(--amber-soft)", color: "var(--amber)", borderColor: "#f4d27d" }}>
              DEMO MODE — {Math.max(0, Math.ceil(demoStatus.remainingDays))} days remaining. This instance will shut down when the demo period ends.
            </div>
          )}

          {/* Search overlay */}
          <AnimatePresence>
            {searchOpen && search && (
              <motion.div className="search-overlay" initial={{ opacity: 0, y: -10 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0, y: -10 }} transition={{ duration: 0.15 }}>
                <div className="search-results">
                  {filteredModules.length === 0 ? (
                    <p className="search-empty">No modules match &quot;<strong>{search}</strong>&quot;</p>
                  ) : (
                    filteredModules.map(m => (
                      <Link key={m.id} href={m.route} className="search-result-item" onClick={() => setSearchOpen(false)}>
                        <Icon name={m.icon} />
                        <span><strong>{m.label}</strong><small>{m.description}</small></span>
                        <span className="domain-tag" style={{ borderColor: getDomainColor(m.domain), color: getDomainColor(m.domain) }}>{m.domain}</span>
                      </Link>
                    ))
                  )}
                </div>
              </motion.div>
            )}
          </AnimatePresence>

          {/* Content */}
          <AnimatePresence mode="wait">
            <motion.div
              key={pathname}
              className="shell-content"
              initial={{ opacity: 0, y: 12 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0, y: -8 }}
              transition={{ type: "spring", stiffness: 200, damping: 22 }}
            >
              {children}
            </motion.div>
          </AnimatePresence>
        </main>

        {/* Recents sidebar (desktop) */}
        {!collapsed && (
          <aside className="shell-recents">
            <h3>Recent</h3>
            {recents.length === 0 ? <p className="empty-hint">No recent pages.</p> : (
              <ol className="recents-list">
                {recents.slice(0, 5).map(route => {
                  const mod = modules.find(m => m.route === route || route.startsWith(m.route));
                  return mod ? (
                    <li key={route}>
                      <Link href={route} className="recent-item">
                        <Icon name={mod.icon} />
                        <span>{mod.label}</span>
                      </Link>
                    </li>
                  ) : null;
                }).filter(Boolean)}
              </ol>
            )}
            <h3 style={{ marginTop: 20 }}>Pinned</h3>
            {pinned.length === 0 ? <p className="empty-hint">Pin a module from the sidebar to keep it here.</p> : (
              <ol className="recents-list">
                {pinned.map(id => {
                  const mod = modules.find(m => m.id === id);
                  return mod ? (
                    <li key={id}>
                      <Link href={mod.route} className="recent-item">
                        <Icon name={mod.icon} />
                        <span>{mod.label}</span>
                      </Link>
                    </li>
                  ) : null;
                }).filter(Boolean)}
              </ol>
            )}
          </aside>
        )}
      </div>
    </PersonalityModeProvider>
  );
}
