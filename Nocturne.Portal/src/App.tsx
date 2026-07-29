import { Bell, CalendarDays, ClipboardCheck, DollarSign, FileText, GitBranch, GraduationCap, Menu, Moon, Package, Search, ShieldAlert, Sun, UserRound } from "lucide-react";
import { useEffect, useState } from "react";
import { useDispatch, useSelector } from "react-redux";
import { apiGet, apiPost, connectNotifications } from "./api";
import { CurriculumBuilder } from "./CurriculumBuilder";
import { RootState, setPage, setRole, toggleDark } from "./store";
import { Ticketing } from "./Ticketing";
import "./support.css";

type PortalPage = { id: string; label: string; icon: typeof Bell; summary: string; actions: string[]; metrics: [string, string][] };

const studentPages: PortalPage[] = [
  page("dashboard", "Dashboard", GraduationCap, "Semester GPA, attendance, fees, and active courses.", [["SGPA", "3.42"], ["Attendance", "82%"], ["Fees", "Pending"]], ["View current courses", "Open alerts"]),
  page("attendance", "Attendance", ClipboardCheck, "Course-wise attendance with shortage alerts and appeal entry.", [["Overall", "82%"], ["Policy", "75%"], ["Appeals", "1 open"]], ["Request appeal", "Export CSV"]),
  page("grades", "Grades", FileText, "Published results, transcript requests, grade distribution, and section rank.", [["CGPA", "3.31"], ["Credits", "91"], ["Rank", "12/84"]], ["Request transcript", "Download PDF"]),
  page("fees", "Fees", DollarSign, "Installments, scholarships, receipts, and payment entry.", [["Outstanding", "PKR 25,000"], ["Scholarship", "20%"], ["Receipts", "6"]], ["Pay now", "Download receipt"]),
  page("registration", "Registration", CalendarDays, "Add/drop courses with prerequisite and timetable clash checks.", [["Window", "Open"], ["Selected", "5"], ["Clashes", "0"]], ["Submit registration", "Validate prerequisites"]),
  page("transcript", "Transcript", FileText, "Certificate and transcript request workflow routed to Registrar approval.", [["Requests", "2"], ["Approved", "1"], ["Pending", "1"]], ["New request", "Download approved"]),
  page("timetable", "Timetable", CalendarDays, "Calendar and list views with venue, faculty, and iCal export.", [["Classes", "18/week"], ["Labs", "3/week"], ["Today", "4"]], ["Export iCal", "Print timetable"]),
  page("notices", "Notices", Bell, "University, department, and program announcements with read tracking.", [["Unread", "3"], ["Pinned", "2"], ["Expired", "0"]], ["Mark read", "Filter notices"]),
  page("support", "AI Support", ShieldAlert, "Instant AI help with safe human escalation and status tracking.", [["AI target", "80%"], ["CSAT target", "85+"], ["Threshold", "70%"]], []),
  page("profile", "Profile", UserRound, "Personal data, preferences, notification settings, and security status.", [["2FA", "Enabled"], ["Theme", "System"], ["Language", "English"]], ["Update profile", "Set preferences"])
];

const facultyPages: PortalPage[] = [
  page("dashboard", "Dashboard", GraduationCap, "Courses, pending marking, at-risk students, and grievances.", [["Courses", "4"], ["At risk", "7"], ["Pending", "2"]], ["Open course", "Message class"]),
  page("marks", "Marks Entry", FileText, "Assessment component marks with moderation and CSV upload.", [["Drafts", "2"], ["Submitted", "6"], ["Moderation", "1"]], ["Upload CSV", "Submit marks"]),
  page("attendance", "Attendance", ClipboardCheck, "Date-wise class attendance grid with bulk marking and locking.", [["Today", "3 classes"], ["Locked", "14"], ["Open", "1"]], ["Mark all present", "Export CSV"]),
  page("assessments", "Assessments", FileText, "Assessment scheme submission, CLO links, and HOD review state.", [["Schemes", "4"], ["Approved", "3"], ["Returned", "1"]], ["Submit scheme", "View comments"]),
  page("feedback", "Feedback", Bell, "Student feedback aggregates without traceable student identity.", [["Average", "4.1"], ["Responses", "126"], ["Themes", "5"]], ["View themes", "Export summary"]),
  page("leave", "Leave", CalendarDays, "Leave application, balances, and approval history.", [["Balance", "18 days"], ["Pending", "1"], ["Approved", "3"]], ["Apply leave", "View history"]),
  page("timetable", "Timetable", CalendarDays, "Teaching schedule, rooms, and workload impact.", [["Hours", "12/week"], ["Sections", "4"], ["Conflicts", "0"]], ["Export iCal", "Print"]),
  page("curriculum", "Curriculum", GitBranch, "Visual curriculum tree, prerequisites, CLO links, and version approval.", [["Version", "Draft"], ["Credits", "124"], ["Blocks", "0"]], ["Approve version", "Export graph"]),
  page("materials", "Materials", FileText, "Course material uploads, folders, access control, and Moodle sync.", [["Files", "42"], ["Downloads", "318"], ["Sync", "Healthy"]], ["Upload", "Sync Moodle"]),
  page("moderation", "Moderation", ShieldAlert, "Grade moderation queue and reopen requests.", [["Queue", "3"], ["Approved", "9"], ["Rejected", "1"]], ["Review", "Approve selected"]),
  page("performance", "Performance", GraduationCap, "Teaching, research, appraisal, publication, and grant metrics.", [["Score", "87"], ["Papers", "3"], ["Grants", "1"]], ["Open appraisal", "Add publication"]),
  page("plugins", "Plugins", Package, "Marketplace, developer uploads, review queue, installs, and earnings.", [["Available", "12"], ["Review", "3"], ["Share", "70%"]], ["Upload plugin", "Review queue"]),
  page("support", "Support Operations", ShieldAlert, "Human escalation queue, knowledge capture, CSAT, and AI performance.", [["AI target", "80%"], ["CSAT target", "85+"], ["Human SLA", "<2h"]], []),
  page("profile", "Profile", UserRound, "Profile, notification preferences, security, and connected accounts.", [["2FA", "Enabled"], ["Office", "B-214"], ["Status", "Active"]], ["Update profile", "Security"])
];

export function App() {
  const dispatch = useDispatch();
  const { role, page: selected, dark, unread } = useSelector((s: RootState) => s.ui);
  const pages = role === "student" ? studentPages : facultyPages;
  const current = pages.find(p => p.id === selected) ?? pages[0];

  useEffect(() => {
    const connection = connectNotifications(dispatch);
    return () => { void connection.stop(); };
  }, [dispatch]);

  return <div className={dark ? "shell dark" : "shell"}>
    <aside aria-label="Portal navigation">
      <div className="brand"><GraduationCap aria-hidden /> Nocturne</div>
      <nav>{pages.map(p => <button key={p.id} className={p.id === current.id ? "active" : ""} onClick={() => dispatch(setPage(p.id))}><p.icon aria-hidden />{p.label}</button>)}</nav>
    </aside>
    <main>
      <header>
        <button className="icon" aria-label="Open menu"><Menu /></button>
        <div className="search"><Search aria-hidden /><input aria-label="Search portal" placeholder="Search" /></div>
        <div className="segments" role="group" aria-label="Portal role">
          <button className={role === "student" ? "active" : ""} onClick={() => dispatch(setRole("student"))}>Student</button>
          <button className={role === "faculty" ? "active" : ""} onClick={() => dispatch(setRole("faculty"))}>Faculty</button>
        </div>
        <button className="icon" aria-label={`${unread} unread notifications`}><Bell /><span>{unread}</span></button>
        <button className="icon" aria-label="Toggle dark mode" onClick={() => dispatch(toggleDark())}>{dark ? <Sun /> : <Moon />}</button>
      </header>
      <section className="hero" aria-labelledby="page-title"><div><p>{role === "student" ? "Student Portal" : "Faculty Portal"}</p><h1 id="page-title">{current.label}</h1><span>{current.summary}</span></div></section>
      <section className="metrics" aria-label="Key metrics">{current.metrics.map(([k, v]) => <article key={k}><strong>{v}</strong><span>{k}</span></article>)}</section>
      <section className={current.id === "support" ? "workspace support-workspace" : "workspace"}>
        <div className="panel"><h2>Workflow</h2><ScreenBody page={current} staff={role === "faculty"} /></div>
        {current.actions.length > 0 && <div className="panel compact"><h2>Actions</h2>{current.actions.map(a => <button key={a}>{a}</button>)}</div>}
      </section>
    </main>
  </div>;
}

function page(id: string, label: string, icon: typeof Bell, summary: string, metrics: [string, string][], actions: string[]): PortalPage {
  return { id, label, icon, summary, metrics, actions };
}

function ScreenBody({ page, staff }: { page: PortalPage; staff: boolean }) {
  if (page.id === "curriculum") return <CurriculumBuilder />;
  if (page.id === "plugins") return <PluginMarketplace />;
  if (page.id === "support") return <Ticketing staff={staff} />;
  return <div className="table" role="table" aria-label={`${page.label} records`}>
    {["Course / Item", "Status", "Updated"].map(h => <b role="columnheader" key={h}>{h}</b>)}
    {[1, 2, 3, 4].map(i => <Row key={i} name={`${page.label} ${i}`} status={i % 2 ? "Ready" : "Pending"} />)}
  </div>;
}

function Row({ name, status }: { name: string; status: string }) {
  return <><span>{name}</span><span className={status === "Ready" ? "ok" : "warn"}>{status}</span><span>{new Date().toLocaleDateString()}</span></>;
}

type CatalogResponse = { extensions: { id: string; name: string; developerName: string; pricingModel: string; price: number; status: string }[] };
type EarningsResponse = { gross: number; nocturne: number; developer: number };

function PluginMarketplace() {
  const [catalog, setCatalog] = useState<CatalogResponse["extensions"]>([]);
  const [earnings, setEarnings] = useState<EarningsResponse>({ gross: 0, nocturne: 0, developer: 0 });
  const [status, setStatus] = useState("Ready");

  useEffect(() => {
    let live = true;
    Promise.all([
      apiGet<CatalogResponse>("/api/marketplace/catalog"),
      apiGet<EarningsResponse>("/api/marketplace/developers/plugins@nocturne.systems/earnings")
    ]).then(([c, e]) => {
      if (!live) return; setCatalog(c.extensions); setEarnings(e);
    }).catch(() => {
      if (!live) return;
      setCatalog([{ id: "sample", name: "fee-reconciliation", developerName: "Nocturne Systems", pricingModel: "RevenueShare", price: 0, status: "Published" }]);
      setEarnings({ gross: 100000, nocturne: 30000, developer: 70000 });
    });
    return () => { live = false; };
  }, []);

  async function install(extensionId: string) {
    setStatus("Installing");
    try { await apiPost("/api/marketplace/install", { extensionId }); setStatus("Installed"); }
    catch { setStatus("Queued for review"); }
  }

  return <div className="plugin-grid">
    <section aria-label="Plugin marketplace"><h3>Marketplace</h3><div className="plugin-list">{catalog.map(plugin => <article key={plugin.id}>
      <strong>{plugin.name}</strong><span>{plugin.developerName}</span><span>{plugin.pricingModel} · PKR {plugin.price.toLocaleString()}</span><button onClick={() => install(plugin.id)}>Install</button>
    </article>)}</div></section>
    <section aria-label="Developer earnings"><h3>Developer</h3><div className="earnings">
      <b>PKR {earnings.gross.toLocaleString()}</b><span>Gross</span><b>PKR {earnings.nocturne.toLocaleString()}</b><span>Nocturne 30%</span><b>PKR {earnings.developer.toLocaleString()}</b><span>Developer 70%</span>
    </div><div className="review-queue"><span className="warn">{status}</span><span>Upload, version review, install, and earnings workflow.</span></div></section>
  </div>;
}
