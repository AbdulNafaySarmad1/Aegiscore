"use client";
import { loadSession, logout } from "@/lib/session";

import { useEffect, useState, useMemo, useRef, useCallback } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  BookOpenCheck, Briefcase, Building2, CalendarDays, ChevronLeft, ChevronRight, CircleHelp,
  ClipboardCheck, CreditCard, FileSearch, FileText, GraduationCap, Landmark, LayoutDashboard,
  LogOut, Menu, Moon, Scale, Search, Settings, ShieldCheck, Sun, UserRound, Utensils, Vote, X,
  Contrast, ALargeSmall,
} from "lucide-react";
import { apiGet } from "@/lib/api-client";
import { getRoleProfile } from "@/lib/roles";
import type { DashboardModule } from "@/lib/roles";
import { useTenantFormatter } from "@/components/TenantPreferencesProvider";

type Session = { userId: string; tenantId: string; studentId: string | null; departmentId: string | null; name: string; roles: string[] };

const iconMap: Record<string, typeof LayoutDashboard> = {
  LayoutDashboard, Landmark, FileText, CreditCard, BookOpenCheck, GraduationCap,
  ClipboardCheck, CalendarDays, ShieldCheck, Building2, Scale, FileSearch,
  Briefcase, UserRound, Vote, Utensils, Settings, CircleHelp, Search, LogOut,
};
function Icon({ name }: { name: string }) { const C = iconMap[name] ?? CircleHelp; return <C size={18} aria-hidden="true" />; }

const DOMAIN_ORDER = ["academic", "finance", "operations", "compliance", "welfare"];
const DOMAIN_NAME: Record<string, string> = { academic: "Academic", finance: "Finance", operations: "Operations", compliance: "Compliance", welfare: "Student welfare" };

// Last human-readable path segment, for routes not listed as modules for this role.
function titleFromPath(path: string) {
  const parts = path.split("/").filter(Boolean).filter(p => !/\d/.test(p) && p.length < 40);
  const last = parts[parts.length - 1] ?? "";
  return last ? last.replace(/-/g, " ").replace(/^\w/, c => c.toUpperCase()) : "AegisCore";
}
function readPref(key: string, fallback: string) { try { return localStorage.getItem(key) ?? fallback; } catch { return fallback; } }
function writePref(key: string, value: string) { try { localStorage.setItem(key, value); } catch { /* private mode */ } }

export function AppShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const { date } = useTenantFormatter();
  const [session, setSession] = useState<Session | null>(null);
  const [failed, setFailed] = useState(false);
  const [theme, setTheme] = useState<"light" | "dark">(() => readPref("aegis-theme", "light") === "dark" ? "dark" : "light");
  const [fontScale, setFontScale] = useState(() => readPref("aegis-font-scale", "medium"));
  const [highContrast, setHighContrast] = useState(() => readPref("aegis-high-contrast", "false") === "true");
  const [narrow, setNarrow] = useState(() => readPref("aegis-rail", "wide") === "narrow");
  const [menuOpen, setMenuOpen] = useState(false);
  const [demo, setDemo] = useState<{ isDemo: boolean; remainingDays: number } | null>(null);
  const searchRef = useRef<HTMLInputElement>(null);
  const railRef = useRef<HTMLElement>(null);
  const [query, setQuery] = useState("");

  useEffect(() => {
    loadSession().then(setSession).catch(() => { setFailed(true); window.location.assign("/login"); });
    apiGet<{ isDemo: boolean; remainingDays: number }>("/api/wizard/demo-status").then(setDemo).catch(() => {});
  }, []);
  useEffect(() => { document.documentElement.dataset.theme = theme; writePref("aegis-theme", theme); }, [theme]);
  useEffect(() => {
    if (fontScale === "medium") delete document.documentElement.dataset.fontScale; else document.documentElement.dataset.fontScale = fontScale;
    writePref("aegis-font-scale", fontScale);
  }, [fontScale]);
  useEffect(() => {
    if (highContrast) document.documentElement.dataset.highContrast = "true"; else delete document.documentElement.dataset.highContrast;
    writePref("aegis-high-contrast", String(highContrast));
  }, [highContrast]);
  useEffect(() => { writePref("aegis-rail", narrow ? "narrow" : "wide"); }, [narrow]);
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key === "k") { e.preventDefault(); searchRef.current?.focus(); }
      if (e.key === "Escape" && menuOpen) setMenuOpen(false);
    };
    document.addEventListener("keydown", handler);
    return () => document.removeEventListener("keydown", handler);
  }, [menuOpen]);
  // Close the drawer when the route changes and keep the page from scrolling under it.
  const [seenPath, setSeenPath] = useState(pathname);
  if (seenPath !== pathname) { setSeenPath(pathname); if (menuOpen) setMenuOpen(false); }
  useEffect(() => { document.body.style.overflow = menuOpen ? "hidden" : ""; return () => { document.body.style.overflow = ""; }; }, [menuOpen]);

  const profile = useMemo(() => session ? getRoleProfile(session.roles) : null, [session]);
  const modules = useMemo(() => profile?.dashboardModules ?? [], [profile]);
  const grouped = useMemo(() => {
    const q = query.trim().toLowerCase();
    const groups = new Map<string, DashboardModule[]>();
    for (const m of modules) {
      if (q && !m.label.toLowerCase().includes(q) && !m.description.toLowerCase().includes(q)) continue;
      if (!groups.has(m.domain)) groups.set(m.domain, []);
      groups.get(m.domain)!.push(m);
    }
    return [...groups.entries()].sort((a, b) => DOMAIN_ORDER.indexOf(a[0]) - DOMAIN_ORDER.indexOf(b[0]));
  }, [modules, query]);

  const isActive = useCallback((route: string) => { const p = pathname ?? ""; return p === route || p.startsWith(route + "/"); }, [pathname]);
  const current = modules.find(m => isActive(m.route));
  const title = (pathname ?? "/") === "/" ? "Register" : current?.label ?? titleFromPath(pathname ?? "/");

  if (!session) return (
    <main id="main-content" className="launch-screen" tabIndex={-1} aria-busy="true">
      <span className="wordmark">AegisCore</span>
      <div className="launch-pulse" aria-hidden="true" />
      <p role="status">{failed ? "Redirecting to sign in…" : "Opening the university register…"}</p>
    </main>
  );

  const initial = (session.name.trim()[0] ?? "A").toUpperCase();

  return (
    <div className="shell" data-rail={narrow ? "narrow" : undefined}>
      <aside ref={railRef} id="primary-rail" className="rail" data-open={menuOpen ? "true" : undefined} aria-label="Application navigation">
        <Link href="/" className="rail-brand" aria-label="AegisCore home">
          <img src="/aegiscore-logo.png" alt="" />
          <span className="rail-brand-text"><strong>AegisCore</strong><small>{profile?.displayName ?? "University register"}</small></span>
        </Link>
        <button type="button" className="icon-button masthead-menu-button" style={{ position: "absolute", top: 10, right: 10, color: "var(--on-ink)", borderColor: "var(--ink-rule)", background: "transparent" }} onClick={() => setMenuOpen(false)} aria-label="Close navigation" hidden={!menuOpen}><X size={18} aria-hidden="true" /></button>

        <nav className="rail-nav sidebar-nav" aria-label="Primary navigation">
          {grouped.length === 0 && <p className="rail-group-name">No modules match.</p>}
          {grouped.map(([domain, items]) => (
            <div key={domain} className="rail-group">
              <div className="rail-group-name">{DOMAIN_NAME[domain] ?? domain}</div>
              {items.map(m => {
                const active = isActive(m.route);
                return (
                  <Link key={m.id} href={m.route} className="rail-link" aria-current={active ? "page" : undefined} title={narrow ? m.label : undefined}>
                    <Icon name={m.icon} />
                    <span>{m.label}</span>
                    {m.badge && <span className="rail-badge">{m.badge.charAt(0) + m.badge.slice(1).toLowerCase()}</span>}
                  </Link>
                );
              })}
            </div>
          ))}
        </nav>

        <div className="rail-foot">
          <button type="button" className="rail-link" onClick={() => void logout()}><LogOut size={18} aria-hidden="true" /><span>Sign out</span></button>
          <button type="button" className="rail-link" onClick={() => setNarrow(v => !v)} aria-label={narrow ? "Expand navigation" : "Collapse navigation"} aria-expanded={!narrow}>
            {narrow ? <ChevronRight size={18} aria-hidden="true" /> : <ChevronLeft size={18} aria-hidden="true" />}
            <span>Collapse</span>
          </button>
        </div>
      </aside>
      {menuOpen && <button type="button" className="rail-scrim" onClick={() => setMenuOpen(false)} aria-label="Close navigation" tabIndex={-1} />}

      <main id="main-content" className="shell-main" tabIndex={-1}>
        <header className="masthead">
          <button type="button" className="icon-button masthead-menu-button" aria-label="Open navigation" aria-expanded={menuOpen} aria-controls="primary-rail" onClick={() => setMenuOpen(true)}><Menu size={18} aria-hidden="true" /></button>
          <span className="masthead-title">{title}</span>

          <div className="masthead-search" role="search">
            <Search aria-hidden="true" />
            <input ref={searchRef} type="search" aria-label="Search modules" placeholder="Find a module" value={query} onChange={e => setQuery(e.target.value)} />
            <kbd aria-hidden="true">⌘K</kbd>
          </div>

          <div className="masthead-actions">
            <span className="masthead-date" aria-label="Today">{date(new Date())}</span>
            <button type="button" className="icon-button" data-optional onClick={() => setFontScale(s => s === "medium" ? "large" : s === "large" ? "xlarge" : "medium")} aria-label={`Text size: ${fontScale}. Change text size`} title="Text size">
              <ALargeSmall size={18} aria-hidden="true" />
            </button>
            <button type="button" className="icon-button" data-optional onClick={() => setHighContrast(v => !v)} aria-label="High contrast" aria-pressed={highContrast} title="High contrast">
              <Contrast size={18} aria-hidden="true" />
            </button>
            <button type="button" className="icon-button" onClick={() => setTheme(t => t === "dark" ? "light" : "dark")} aria-label="Toggle dark theme" aria-pressed={theme === "dark"} title="Dark scheme">
              {theme === "dark" ? <Sun size={18} aria-hidden="true" /> : <Moon size={18} aria-hidden="true" />}
            </button>
            <div className="masthead-user">
              <span className="masthead-user-mark" aria-hidden="true">{initial}</span>
              <span className="masthead-user-text"><strong>{session.name}</strong><span>{profile?.displayName}</span></span>
            </div>
          </div>
        </header>

        {demo?.isDemo && (
          <div className="demo-banner" role="status">
            <strong>Demo instance</strong>
            <span>{Math.max(0, Math.ceil(demo.remainingDays))} days remaining. Records entered here are discarded when the demo ends.</span>
          </div>
        )}

        <div className="shell-content">{children}</div>
      </main>
    </div>
  );
}
