"use client";

import {
  Bell,
  BookOpenCheck,
  Building2,
  CalendarDays,
  ChevronDown,
  CircleHelp,
  ClipboardCheck,
  Command,
  CreditCard,
  Landmark,
  FileText,
  GraduationCap,
  LayoutDashboard,
  Menu,
  Moon,
  Search,
  Settings,
  ShieldCheck,
  Sparkles,
  Sun,
  UserRound,
  UsersRound,
  X
} from "lucide-react";
import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { apiGet } from "@/lib/api-client";

type Role = "Student" | "Faculty" | "Registrar";
type NavItem = { label: string; icon: typeof LayoutDashboard; badge?: string };
type NavGroup = { label: string; items: NavItem[] };

const navigation: Record<Role, NavGroup[]> = {
  Student: [
    { label: "Workspace", items: [
      { label: "Command center", icon: LayoutDashboard },
      { label: "My courses", icon: BookOpenCheck },
      { label: "Timetable", icon: CalendarDays }
    ] },
    { label: "Academics", items: [
      { label: "Attendance", icon: ClipboardCheck, badge: "82%" },
      { label: "Results & transcript", icon: FileText },
      { label: "Registration", icon: GraduationCap }
    ] },
    { label: "University", items: [
      { label: "Fees & aid", icon: CreditCard, badge: "1" },
      { label: "Requests", icon: CircleHelp },
      { label: "Profile", icon: UserRound }
    ] }
  ],
  Faculty: [
    { label: "Workspace", items: [
      { label: "Command center", icon: LayoutDashboard },
      { label: "Teaching schedule", icon: CalendarDays },
      { label: "My sections", icon: UsersRound }
    ] },
    { label: "Teaching", items: [
      { label: "Attendance", icon: ClipboardCheck },
      { label: "Marks & moderation", icon: FileText, badge: "3" },
      { label: "CLO / PLO mapping", icon: BookOpenCheck }
    ] },
    { label: "Faculty services", items: [
      { label: "Workload", icon: Building2 },
      { label: "Requests", icon: CircleHelp },
      { label: "Profile", icon: UserRound }
    ] }
  ],
  Registrar: [
    { label: "Operations", items: [
      { label: "Command center", icon: LayoutDashboard },
      { label: "Student lifecycle", icon: UsersRound },
      { label: "Academic calendar", icon: CalendarDays }
    ] },
    { label: "Governance", items: [
      { label: "Programs & courses", icon: BookOpenCheck },
      { label: "Examinations", icon: FileText, badge: "12" },
      { label: "Approvals", icon: ClipboardCheck, badge: "8" }
    ] },
    { label: "Administration", items: [
      { label: "Finance", icon: Landmark },
      { label: "Organization", icon: Building2 },
      { label: "Access control", icon: ShieldCheck },
      { label: "Configuration", icon: Settings }
    ] }
  ]
};

const roleData: Record<Role, {
  eyebrow: string;
  title: string;
  subtitle: string;
  metrics: Array<{ label: string; value: string; trend: string; tone: string }>;
  queue: Array<{ item: string; owner: string; status: string; due: string }>;
}> = {
  Student: {
    eyebrow: "Spring 2026 · Week 12",
    title: "Good evening, Abdul",
    subtitle: "You are on track. Two academic items need your attention this week.",
    metrics: [
      { label: "Current CGPA", value: "3.31", trend: "+0.08 this term", tone: "blue" },
      { label: "Attendance", value: "82%", trend: "Above 75% policy", tone: "green" },
      { label: "Earned credits", value: "91", trend: "33 remaining", tone: "violet" },
      { label: "Outstanding fees", value: "PKR 25k", trend: "Due 12 August", tone: "amber" }
    ],
    queue: [
      { item: "Course registration confirmation", owner: "Registrar", status: "Action required", due: "Today" },
      { item: "Data Structures assignment", owner: "Dr. Ahmed", status: "Due soon", due: "31 Jul" },
      { item: "Transcript request #TR-2041", owner: "Examinations", status: "In review", due: "2 Aug" },
      { item: "Fee installment plan", owner: "Finance", status: "Approved", due: "12 Aug" }
    ]
  },
  Faculty: {
    eyebrow: "Faculty workspace · Computer Science",
    title: "Teaching command center",
    subtitle: "Three sections need marking or attendance updates before the next review.",
    metrics: [
      { label: "Active sections", value: "4", trend: "126 students", tone: "blue" },
      { label: "Pending marking", value: "3", trend: "2 due this week", tone: "amber" },
      { label: "At-risk students", value: "7", trend: "Early warning active", tone: "violet" },
      { label: "Workload", value: "12 hrs", trend: "Within allocation", tone: "green" }
    ],
    queue: [
      { item: "Finalize OOP midterm marks", owner: "CS-210 A", status: "Action required", due: "Today" },
      { item: "Review attendance appeal", owner: "CS-102 B", status: "Pending", due: "31 Jul" },
      { item: "CLO attainment review", owner: "OBE Cell", status: "In review", due: "3 Aug" },
      { item: "Course file completeness", owner: "QA Office", status: "84% complete", due: "7 Aug" }
    ]
  },
  Registrar: {
    eyebrow: "Institution operations · Live",
    title: "University command center",
    subtitle: "A consolidated view of academic operations, approvals, and service health.",
    metrics: [
      { label: "Active students", value: "8,412", trend: "+3.2% YoY", tone: "blue" },
      { label: "Open approvals", value: "28", trend: "8 need attention", tone: "amber" },
      { label: "Sections running", value: "614", trend: "97% scheduled", tone: "violet" },
      { label: "Platform health", value: "99.98%", trend: "All systems normal", tone: "green" }
    ],
    queue: [
      { item: "Spring result publication batch", owner: "Examinations", status: "Approval required", due: "Today" },
      { item: "Course capacity exception", owner: "Computer Science", status: "8 requests", due: "31 Jul" },
      { item: "Graduation clearance batch", owner: "Registrar", status: "142 records", due: "2 Aug" },
      { item: "HEC data quality exceptions", owner: "QA Office", status: "11 unresolved", due: "5 Aug" }
    ]
  }
};

export function PortalShell() {
  const [role, setRole] = useState<Role>("Student");
  const [active, setActive] = useState("Command center");
  const [dark, setDark] = useState(false);
  const [menuOpen, setMenuOpen] = useState(false);
  const [checking, setChecking] = useState(true);
  const [connectionError, setConnectionError] = useState(false);
  const data = roleData[role];

  useEffect(() => {
    const controller = new AbortController();
    apiGet<{ isInstalled: boolean }>("/health/installation", controller.signal)
      .then(({ isInstalled }) => {
        if (!isInstalled) window.location.replace("/setup/");
      })
      .catch(() => setConnectionError(true))
      .finally(() => setChecking(false));
    return () => controller.abort();
  }, []);

  useEffect(() => {
    document.documentElement.dataset.theme = dark ? "dark" : "light";
  }, [dark]);

  const groups = useMemo(() => navigation[role], [role]);

  if (checking) {
    return (
      <main className="launch-screen">
        <Brand />
        <div className="launch-pulse" />
        <p>Connecting to your university workspace…</p>
      </main>
    );
  }

  return (
    <div className="portal-shell">
      <aside className={`portal-sidebar ${menuOpen ? "open" : ""}`}>
        <div className="sidebar-top">
          <Brand />
          <button className="icon-button mobile-only" onClick={() => setMenuOpen(false)} aria-label="Close navigation"><X /></button>
        </div>
        <div className="institution-switcher">
          <span className="institution-mark">SS</span>
          <span><strong>SSUET</strong><small>Main campus</small></span>
          <ChevronDown />
        </div>
        <nav className="primary-nav" aria-label={`${role} navigation`}>
          {groups.map(group => (
            <section key={group.label}>
              <h2>{group.label}</h2>
              {group.items.map(item => (
                <button
                  key={item.label}
                  className={active === item.label ? "active" : ""}
                  onClick={() => {
                    if (item.label === "Finance") window.location.assign("/finance/");
                    else { setActive(item.label); setMenuOpen(false); }
                  }}
                >
                  <item.icon aria-hidden />
                  <span>{item.label}</span>
                  {item.badge && <b>{item.badge}</b>}
                </button>
              ))}
            </section>
          ))}
        </nav>
        <div className="sidebar-footer">
          <div className="service-health"><i /><span><strong>All systems operational</strong><small>Last checked just now</small></span></div>
          <button><CircleHelp /> Help & support</button>
        </div>
      </aside>

      {menuOpen && <button className="sidebar-scrim" onClick={() => setMenuOpen(false)} aria-label="Close navigation" />}

      <main className="portal-main">
        <header className="portal-header">
          <button className="icon-button mobile-only" onClick={() => setMenuOpen(true)} aria-label="Open navigation"><Menu /></button>
          <div className="global-search">
            <Search aria-hidden />
            <input aria-label="Search AegisCore" placeholder="Search students, courses, forms, or commands…" />
            <kbd><Command /> K</kbd>
          </div>
          <div className="header-actions">
            <button className="icon-button" onClick={() => setDark(!dark)} aria-label="Toggle color theme">{dark ? <Sun /> : <Moon />}</button>
            <button className="icon-button notification-button" aria-label="3 notifications"><Bell /><b>3</b></button>
            <div className="profile-chip">
              <span>AN</span>
              <span><strong>Abdul Nafay</strong><small>{role}</small></span>
              <ChevronDown />
            </div>
          </div>
        </header>

        {connectionError && (
          <div className="connection-banner">
            The portal is running, but the API did not answer. Check the API container before using live workflows.
          </div>
        )}

        <section className="command-page">
          <div className="page-heading">
            <div>
              <p>{data.eyebrow}</p>
              <h1>{active === "Command center" ? data.title : active}</h1>
              <span>{active === "Command center" ? data.subtitle : `Your ${active.toLowerCase()} workspace is ready for live API integration.`}</span>
            </div>
            <div className="role-switcher">
              {(["Student", "Faculty", "Registrar"] as Role[]).map(option => (
                <button key={option} className={role === option ? "active" : ""} onClick={() => { setRole(option); setActive("Command center"); }}>
                  {option}
                </button>
              ))}
            </div>
          </div>

          <section className="metric-grid" aria-label="Key indicators">
            {data.metrics.map(metric => (
              <article key={metric.label}>
                <div className={`metric-icon ${metric.tone}`}><Sparkles /></div>
                <span>{metric.label}</span>
                <strong>{metric.value}</strong>
                <small>{metric.trend}</small>
              </article>
            ))}
          </section>

          <div className="workspace-grid">
            <section className="surface work-queue">
              <div className="surface-heading">
                <div><h2>Work queue</h2><p>Priority items across your university workflows</p></div>
                <button>View all</button>
              </div>
              <div className="enterprise-table" role="table" aria-label="Work queue">
                <div className="table-row table-head" role="row">
                  <span>Item</span><span>Owner</span><span>Status</span><span>Due</span>
                </div>
                {data.queue.map(row => (
                  <div className="table-row" role="row" key={row.item}>
                    <strong>{row.item}</strong>
                    <span>{row.owner}</span>
                    <span><i className={row.status.includes("Approved") || row.status.includes("complete") ? "status-ok" : "status-warn"}>{row.status}</i></span>
                    <span>{row.due}</span>
                  </div>
                ))}
              </div>
            </section>

            <aside className="surface today-card">
              <div className="surface-heading"><div><h2>Today</h2><p>Wednesday, 29 July</p></div><CalendarDays /></div>
              <ol>
                <li><time>09:00</time><span><strong>Data Structures</strong><small>Room C-204 · 60 min</small></span></li>
                <li><time>11:30</time><span><strong>Academic review</strong><small>Online · 30 min</small></span></li>
                <li><time>14:00</time><span><strong>OOP Lab</strong><small>Lab 3 · 120 min</small></span></li>
              </ol>
              <button className="secondary-action">Open full timetable</button>
            </aside>
          </div>

          <section className="quick-actions" aria-label="Quick actions">
            <h2>Quick actions</h2>
            <div>
              <button><ClipboardCheck /><span><strong>Mark attendance</strong><small>Open today&apos;s class</small></span></button>
              <button><FileText /><span><strong>Request document</strong><small>Transcript or certificate</small></span></button>
              <button><CreditCard /><span><strong>Pay university fees</strong><small>View dues and receipts</small></span></button>
              <button><CircleHelp /><span><strong>Ask Aegis</strong><small>AI and human support</small></span></button>
            </div>
          </section>
        </section>
      </main>
    </div>
  );
}

function Brand() {
  return (
    <Link className="aegis-brand" href="/" aria-label="AegisCore home">
      <span className="brand-glyph"><ShieldCheck /></span>
      <span><strong>AegisCore</strong><small>by Nocturne Systems</small></span>
    </Link>
  );
}
