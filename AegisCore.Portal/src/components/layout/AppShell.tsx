"use client";

// ─── AegisCore Application Shell ───
// Canonical authenticated shell: role-aware sidebar navigation, top bar with
// session controls, responsive drawer, route gating.
// Frontend hiding is UX only — the backend remains the security authority.

import { useEffect, useMemo, useRef, useState } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { AnimatePresence, motion } from "framer-motion";
import {
  BookOpenCheck, Briefcase, Building2, CalendarDays, ChevronDown, ChevronLeft,
  ChevronRight, CircleHelp, ClipboardCheck, CreditCard, FileSearch, FileText,
  GraduationCap, Landmark, LayoutDashboard, LogOut, Menu, Moon, Scale, Search,
  Settings, ShieldCheck, Sun, UserRound, Utensils, Vote, X, Bell, Accessibility,
} from "lucide-react";
import { canAccessRoute, getDomainColor, getRoleProfile } from "@/lib/roles";
import type { DashboardModule } from "@/lib/roles";
import { apiGet } from "@/lib/api-client";
import { useSession } from "@/components/session-provider";
import { ForbiddenState, Spinner } from "@/components/async/states";

// Delivery shape is rendered defensively — only fields present in the
// backend payload are shown; nothing is fabricated.
type NotificationDelivery = {
  id?: string;
  subject?: string;
  title?: string;
  message?: string;
  body?: string;
  channel?: string;
  status?: string;
  createdAt?: string;
  sentAt?: string;
};

const iconMap: Record<string, typeof LayoutDashboard> = {
  LayoutDashboard, Landmark, FileText, CreditCard, BookOpenCheck, GraduationCap,
  ClipboardCheck, CalendarDays, ShieldCheck, Building2, Scale, FileSearch,
  Briefcase, UserRound, Vote, Utensils, Settings, CircleHelp, Search, LogOut,
};
function Icon({ name }: { name: string }) { const C = iconMap[name] ?? CircleHelp; return <C size={20} />; }

const DOMAIN_ORDER = ["academic", "finance", "operations", "compliance", "welfare"] as const;

function readStored(key: string, fallback: string): string {
  try { return window.localStorage.getItem(key) ?? fallback; } catch { return fallback; }
}

export function AppShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname() ?? "/";
  const { status, session, isDemo, signOut } = useSession();
  const [dark, setDark] = useState(() => readStored("aegis-theme", "light") === "dark");
  const [collapsed, setCollapsed] = useState(false);
  const [menuOpen, setMenuOpen] = useState(false);
  const [accountOpen, setAccountOpen] = useState(false);
  const [fontScale, setFontScale] = useState(() => readStored("aegis-font-scale", "medium"));
  const [highContrast, setHighContrast] = useState(() => readStored("aegis-contrast", "false") === "true");
  const searchRef = useRef<HTMLInputElement>(null);
  const accountRef = useRef<HTMLDivElement>(null);
  const notifRef = useRef<HTMLDivElement>(null);
  const [notifOpen, setNotifOpen] = useState(false);
  const [notifState, setNotifState] = useState<{ loading: boolean; items: NotificationDelivery[]; failed: boolean }>({ loading: false, items: [], failed: false });

  // ── Theme / accessibility preferences (persisted, system-aware default) ──
  useEffect(() => {
    document.documentElement.dataset.theme = dark ? "dark" : "light";
    try { window.localStorage.setItem("aegis-theme", dark ? "dark" : "light"); } catch { /* ignore */ }
  }, [dark]);
  useEffect(() => {
    document.documentElement.dataset.fontScale = fontScale;
    try { window.localStorage.setItem("aegis-font-scale", fontScale); } catch { /* ignore */ }
  }, [fontScale]);
  useEffect(() => {
    document.documentElement.dataset.highContrast = highContrast ? "true" : "false";
    try { window.localStorage.setItem("aegis-contrast", String(highContrast)); } catch { /* ignore */ }
  }, [highContrast]);

  // ── Keyboard: ⌘K focuses search, Escape closes drawer/menus ──
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key === "k") { e.preventDefault(); searchRef.current?.focus(); }
      if (e.key === "Escape") { setMenuOpen(false); setAccountOpen(false); setNotifOpen(false); }
    };
    document.addEventListener("keydown", handler);
    return () => document.removeEventListener("keydown", handler);
  }, []);

  // ── Close account/notification menus on outside click ──
  useEffect(() => {
    if (!accountOpen && !notifOpen) return;
    const handler = (e: MouseEvent) => {
      if (!accountRef.current?.contains(e.target as Node)) setAccountOpen(false);
      if (!notifRef.current?.contains(e.target as Node)) setNotifOpen(false);
    };
    document.addEventListener("mousedown", handler);
    return () => document.removeEventListener("mousedown", handler);
  }, [accountOpen, notifOpen]);

  // ── Notifications: lazy-load real deliveries when the panel opens ──
  // (loading state is set in the bell's click handler, not in this effect)
  useEffect(() => {
    if (!notifOpen || status !== "authenticated" || isDemo) return;
    const ctrl = new AbortController();
    apiGet<{ total: number; deliveries: NotificationDelivery[] }>("/api/notifications/deliveries", ctrl.signal)
      .then(d => setNotifState({ loading: false, items: Array.isArray(d.deliveries) ? d.deliveries.slice(0, 12) : [], failed: false }))
      .catch(() => { if (!ctrl.signal.aborted) setNotifState({ loading: false, items: [], failed: true }); });
    return () => ctrl.abort();
  }, [notifOpen, status, isDemo]);

  const openNotifications = () => {
    setNotifOpen(v => !v);
    setNotifState(s => ({ ...s, loading: true, failed: false }));
  };

  // ── Close the mobile drawer on navigation (render-adjust pattern) ──
  const [prevPath, setPrevPath] = useState(pathname);
  if (prevPath !== pathname) {
    setPrevPath(pathname);
    setMenuOpen(false);
  }

  // ── Unauthenticated → login ──
  useEffect(() => {
    if (status === "unauthenticated") window.location.assign("/login");
  }, [status]);

  const profile = useMemo(() => (session ? getRoleProfile(session.roles) : null), [session]);
  const grouped = useMemo(() => {
    const groups = new Map<string, DashboardModule[]>();
    for (const m of profile?.dashboardModules ?? []) {
      const list = groups.get(m.domain) ?? [];
      list.push(m);
      groups.set(m.domain, list);
    }
    for (const list of groups.values()) list.sort((a, b) => a.priority - b.priority);
    return [...groups.entries()].sort(
      (a, b) => DOMAIN_ORDER.indexOf(a[0] as (typeof DOMAIN_ORDER)[number]) - DOMAIN_ORDER.indexOf(b[0] as (typeof DOMAIN_ORDER)[number])
    );
  }, [profile]);

  if (status !== "authenticated" || !session || !profile) {
    return (
      <main className="launch-screen">
        <img src="/aegiscore-logo.png" alt="AegisCore" style={{ width: 48, borderRadius: 10 }} />
        <Spinner size={22} />
        <p style={{ color: "var(--muted)", fontSize: 14 }}>Loading your university workspace…</p>
      </main>
    );
  }

  // Route gating: UX-level only; the backend enforces authorization.
  const routeAllowed = pathname === "/" || canAccessRoute(session.roles, pathname);
  const pageTitle = pathname === "/"
    ? "Command Center"
    : profile.dashboardModules.find(m => pathname === m.route || pathname.startsWith(m.route + "/"))?.label ?? "AegisCore";

  return (
    <div className="shell">
      {/* Sidebar */}
      <aside className={`sidebar ${collapsed ? "collapsed" : ""} ${menuOpen ? "open" : ""}`} aria-label="Primary navigation">
        <div className="sidebar-logo">
          <img src="/aegiscore-logo.png" alt="" />
          {!collapsed && <div><strong>AegisCore</strong><small>University ERP</small></div>}
          <button className="sidebar-close" onClick={() => setMenuOpen(false)} aria-label="Close navigation">
            <X size={18} />
          </button>
        </div>

        <nav className="sidebar-nav">
          {grouped.map(([domain, items]) => (
            <div key={domain} className="sidebar-nav-group">
              {!collapsed && <div className="sidebar-nav-label" style={{ color: getDomainColor(domain) }}>{domain}</div>}
              {items.map(m => {
                const active = pathname === m.route || (m.route !== "/" && pathname.startsWith(m.route + "/"));
                return (
                  <Link key={m.id} href={m.route} className={active ? "active" : ""} aria-current={active ? "page" : undefined} title={collapsed ? m.label : undefined}>
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
          <button onClick={() => setCollapsed(v => !v)} aria-label={collapsed ? "Expand sidebar" : "Collapse sidebar"} aria-expanded={!collapsed}>
            {collapsed ? <ChevronRight size={18} /> : <ChevronLeft size={18} />}
            {!collapsed && <span>Collapse</span>}
          </button>
        </div>
      </aside>
      {menuOpen && <div className="sidebar-scrim" onClick={() => setMenuOpen(false)} aria-hidden="true" />}

      {/* Main */}
      <main className="shell-main">
        {/* Topbar */}
        <header className="topbar">
          <button className="btn btn-ghost btn-sm topbar-menu-btn" onClick={() => setMenuOpen(true)} aria-label="Open navigation">
            <Menu size={20} />
          </button>
          <span className="topbar-title">{pageTitle}</span>

          <div className="topbar-search">
            <Search size={16} />
            <input ref={searchRef} placeholder="Search… (⌘K)" aria-label="Search" />
          </div>

          <div className="topbar-actions">
            {isDemo && <span className="badge badge-warning" title="Preview identity — not a backend session">Demo</span>}
            <button className="btn btn-ghost btn-sm" onClick={() => setFontScale(s => s === "medium" ? "large" : s === "large" ? "xlarge" : "medium")} title="Font size" aria-label="Cycle font size">
              <Accessibility size={18} />
            </button>
            <button className="btn btn-ghost btn-sm" onClick={() => setHighContrast(v => !v)} title="High contrast" aria-label="Toggle high contrast" aria-pressed={highContrast}>
              <span style={{ fontSize: 14, fontWeight: 700 }}>{highContrast ? "HC" : "Aa"}</span>
            </button>
            <button className="btn btn-ghost btn-sm" onClick={() => setDark(v => !v)} title="Toggle theme" aria-label="Toggle dark theme" aria-pressed={dark}>
              {dark ? <Sun size={18} /> : <Moon size={18} />}
            </button>
            <div className="account" ref={notifRef}>
              <button
                className="notification-bell"
                onClick={openNotifications}
                aria-label="Notifications"
                aria-haspopup="menu"
                aria-expanded={notifOpen}
              >
                <Bell size={18} />
                {notifState.items.length > 0 && <span className="notification-bell-badge">{notifState.items.length}</span>}
              </button>
              {notifOpen && (
                <div className="dropdown-menu notification-dropdown" role="menu" aria-label="Notifications">
                  <div className="dropdown-header"><strong>Notifications</strong><span>Latest delivery activity</span></div>
                  {notifState.loading && <div className="dropdown-item" role="note"><Spinner size={14} label="Loading…" /></div>}
                  {!notifState.loading && notifState.failed && <div className="dropdown-item dropdown-muted" role="note">Notifications are unavailable right now.</div>}
                  {!notifState.loading && !notifState.failed && notifState.items.length === 0 && (
                    <div className="dropdown-item dropdown-muted" role="note">No notifications yet.</div>
                  )}
                  {!notifState.loading && notifState.items.map((n, i) => (
                    <div key={n.id ?? i} className="dropdown-item notification-item" role="menuitem">
                      <span className="notification-text">{n.subject ?? n.title ?? n.message ?? n.body ?? "Notification"}</span>
                      <span className="notification-meta">
                        {[n.channel, n.status, n.createdAt ?? n.sentAt ? new Date((n.createdAt ?? n.sentAt)!).toLocaleDateString() : null].filter(Boolean).join(" · ")}
                      </span>
                    </div>
                  ))}
                </div>
              )}
            </div>

            <div className="account" ref={accountRef}>
              <button className="avatar-menu" onClick={() => setAccountOpen(v => !v)} aria-haspopup="menu" aria-expanded={accountOpen} aria-label="Account menu">
                <span className="topbar-avatar">{session.name[0]?.toUpperCase()}</span>
                <span className="avatar-menu-info">
                  <strong>{session.name.split(" ")[0]}</strong>
                  <span>{profile.displayName}</span>
                </span>
                <ChevronDown size={14} />
              </button>
              {accountOpen && (
                <div className="dropdown-menu account-dropdown" role="menu">
                  <div className="dropdown-header">
                    <strong>{session.name}</strong>
                    <span>{profile.displayName}</span>
                  </div>
                  <button role="menuitem" className="dropdown-item dropdown-danger" onClick={() => void signOut()}>
                    <LogOut size={16} /> Sign out
                  </button>
                </div>
              )}
            </div>
          </div>
        </header>

        {routeAllowed ? (
          <AnimatePresence mode="wait">
            <motion.div key={pathname} initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }} transition={{ duration: 0.15 }}>
              {children}
            </motion.div>
          </AnimatePresence>
        ) : (
          <div className="content-page">
            <ForbiddenState />
            <p style={{ textAlign: "center" }}>
              <Link href="/" className="btn btn-secondary btn-sm">Back to dashboard</Link>
            </p>
          </div>
        )}
      </main>
    </div>
  );
}
