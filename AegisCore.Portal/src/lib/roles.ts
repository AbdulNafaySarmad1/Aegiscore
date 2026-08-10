// ─── AegisCore Role Engine ─── src/lib/roles.ts ───
// Maps session.roles[] from GET /api/session to complete role profiles.

export type QuickAction = {
  id: string;
  label: string;
  icon: string;
  route: string;
};

// Verified live metrics from GET /api/finance/v2/dashboard/executive
// (the only KPI source confirmed against the backend contract — see
// docs/FRONTEND_API_HANDOFF.md §KPI Contracts). KPIs without a `metric`
// render a deliberate "unavailable" state; values are never fabricated.
export type LiveMetric = "revenue" | "collectionRate" | "outstanding" | "cashPosition";

export type KPIDefinition = {
  id: string;
  label: string;
  value: string | number;
  trend?: "up" | "down" | "neutral";
  trendLabel?: string;
  format?: "number" | "currency" | "percentage" | "text";
  icon: string;
  iconColor: "blue" | "green" | "amber" | "violet" | "red";
  route?: string;
  metric?: LiveMetric;
};

export type DashboardModule = {
  id: string;
  label: string;
  icon: string;
  route: string;
  color: string;
  description: string;
  badge?: string;
  priority: number;
  domain: "academic" | "finance" | "operations" | "compliance" | "welfare";
};

export type RoleProfile = {
  role: string;
  displayName: string;
  allowedRoutes: string[];
  dashboardModules: DashboardModule[];
  kpis: KPIDefinition[];
  quickActions: QuickAction[];
};

// ─── Role priority (highest to lowest) ───
const ROLE_PRIORITY: Record<string, number> = {
  SuperAdmin: 100,
  VC: 90,
  Registrar: 80,
  AdmissionsHead: 78,
  ExamController: 75,
  OBECoordinator: 72,
  Finance: 70,
  "Finance Controller": 70,
  CFO: 70,
  HR: 60,
  HRManager: 60,
  Auditor: 58,
  IT: 55,
  ITHead: 55,
  HOD: 50,
  Dean: 45,
  Faculty: 40,
  Employee: 30,
  Student: 20,
};

function topRole(roles: string[]): string {
  return roles
    .slice()
    .sort((a, b) => (ROLE_PRIORITY[b] ?? 0) - (ROLE_PRIORITY[a] ?? 0))[0]
    ?? "Student";
}

// ─── Profile definitions ───
const profiles: Record<string, RoleProfile> = {
  Student: {
    role: "Student",
    displayName: "Student",
    allowedRoutes: [
      "/student/*", "/attendance", "/mental-health",
      "/elections", "/cafeteria", "/placement", "/achievements",
      "/examinations", "/voice", "/telemetry",
    ],
    dashboardModules: [
      { id: "dashboard", label: "My Dashboard", icon: "LayoutDashboard", route: "/student/dashboard", color: "blue", description: "Your academic overview and shortcuts.", priority: 1, domain: "academic", badge: "HOME" },
      { id: "telemetry", label: "Performance", icon: "Gauge", route: "/telemetry", color: "blue", description: "Academic sectors, trends, personal bests, and privacy-safe cohort comparisons.", priority: 2, domain: "academic", badge: "LIVE" },
      { id: "attendance", label: "Attendance", icon: "ClipboardCheck", route: "/attendance", color: "blue", description: "Per-course attendance records and percentage.", priority: 2, domain: "academic" },
      { id: "examinations", label: "Examinations", icon: "GraduationCap", route: "/examinations", color: "blue", description: "Exam schedules and published results.", priority: 3, domain: "academic" },
      { id: "transcript", label: "Transcript", icon: "FileText", route: "/student/transcript", color: "blue", description: "Official transcript view and request.", priority: 4, domain: "academic" },
      { id: "profile", label: "My Profile", icon: "UserRound", route: "/student/profile", color: "blue", description: "Account details and preferences.", priority: 5, domain: "academic" },
      { id: "mentalhealth", label: "Mental Health", icon: "ShieldCheck", route: "/mental-health", color: "violet", description: "Anonymous support tickets and counselor access.", priority: 6, domain: "welfare" },
      { id: "cafeteria", label: "Cafeteria", icon: "Utensils", route: "/cafeteria", color: "amber", description: "Today's menu, meal plans, and balance.", priority: 7, domain: "operations" },
      { id: "elections", label: "Elections", icon: "Vote", route: "/elections", color: "violet", description: "Student council elections and voting.", priority: 8, domain: "welfare" },
      { id: "achievements", label: "Achievements", icon: "GraduationCap", route: "/achievements", color: "violet", description: "Badges, Dean's List, and certificates.", priority: 9, domain: "welfare" },
      { id: "voice", label: "Voice Assistant", icon: "UserRound", route: "/voice", color: "violet", description: "Ask questions in Urdu or English.", priority: 10, domain: "welfare" },
    ],
    kpis: [],
    quickActions: [
      { id: "attendance", label: "My Attendance", icon: "ClipboardCheck", route: "/attendance" },
      { id: "transcript", label: "View Transcript", icon: "FileText", route: "/student/transcript" },
      { id: "exams", label: "Examinations", icon: "GraduationCap", route: "/examinations" },
      { id: "support", label: "Get Support", icon: "ShieldCheck", route: "/mental-health" },
    ],
  },

  Faculty: {
    role: "Faculty",
    displayName: "Faculty",
    allowedRoutes: [
      "/classes", "/examinations", "/grade-distributions",
      "/plagiarism", "/visiting-faculty", "/achievements", "/catalog",
      "/attendance", "/grade-appeals",
    ],
    dashboardModules: [
      { id: "my-classes", label: "My Classes", icon: "BookOpenCheck", route: "/classes", color: "blue", description: "Course sections, rosters, and LMS links.", priority: 1, domain: "academic", badge: "ACTIVE" },
      { id: "grade-entry", label: "Grade Submission", icon: "ClipboardCheck", route: "/examinations", color: "blue", description: "Enter marks, apply grace, and submit for moderation.", priority: 2, domain: "academic" },
      { id: "attendance-mark", label: "Attendance Marking", icon: "CalendarDays", route: "/attendance", color: "blue", description: "Daily attendance for all assigned courses.", priority: 3, domain: "academic" },
      { id: "appeals", label: "Grade Appeals", icon: "Scale", route: "/grade-appeals", color: "amber", description: "Respond to student grade appeals.", priority: 4, domain: "compliance" },
      { id: "plagiarism", label: "Plagiarism Check", icon: "FileSearch", route: "/plagiarism", color: "red", description: "Scan student submissions for similarity.", priority: 5, domain: "compliance" },
      { id: "visiting", label: "Visiting Faculty", icon: "UserRound", route: "/visiting-faculty", color: "amber", description: "Guest lecturer marketplace.", priority: 6, domain: "operations" },
    ],
    kpis: [],
    quickActions: [
      { id: "mark-attend", label: "Mark Attendance", icon: "ClipboardCheck", route: "/attendance" },
      { id: "grades", label: "Submit Grades", icon: "GraduationCap", route: "/examinations" },
      { id: "classes", label: "My Classes", icon: "BookOpenCheck", route: "/classes" },
      { id: "plag-check", label: "Check Plagiarism", icon: "FileSearch", route: "/plagiarism" },
    ],
  },

  ExamController: {
    role: "ExamController",
    displayName: "Exam Controller",
    allowedRoutes: [
      "/examinations", "/grade-distributions", "/freezes",
      "/grade-appeals", "/prerequisite-conflicts", "/proctoring",
      "/convocations", "/analytics", "/telemetry",
    ],
    dashboardModules: [
      { id: "exam-schedule", label: "Exam Schedule", icon: "CalendarDays", route: "/examinations", color: "blue", description: "Schedule, seating plans, and invigilation.", priority: 1, domain: "academic", badge: "ACTIVE" },
      { id: "grade-dist", label: "Grade Distribution", icon: "FileSearch", route: "/grade-distributions", color: "blue", description: "Statistical analysis of grade patterns.", priority: 2, domain: "academic" },
      { id: "model-analytics", label: "Model Analytics", icon: "Activity", route: "/analytics", color: "violet", description: "Model quality and global explanation snapshots.", priority: 3, domain: "compliance" },
      { id: "freezes", label: "Freezes", icon: "CalendarDays", route: "/freezes", color: "amber", description: "Semester freeze and deferral management.", priority: 3, domain: "academic" },
      { id: "appeals", label: "Grade Appeals", icon: "Scale", route: "/grade-appeals", color: "amber", description: "Student appeals, evidence review, committee decisions.", priority: 4, domain: "compliance" },
      { id: "prereqs", label: "Prerequisite Conflicts", icon: "FileSearch", route: "/prerequisite-conflicts", color: "amber", description: "Students enrolled without meeting prerequisites.", priority: 5, domain: "compliance" },
      { id: "proctoring", label: "Proctoring", icon: "ShieldCheck", route: "/proctoring", color: "red", description: "Online exam proctoring and integrity management.", priority: 6, domain: "compliance" },
      { id: "convocations", label: "Convocations", icon: "GraduationCap", route: "/convocations", color: "violet", description: "Graduation ceremony management.", priority: 7, domain: "welfare" },
    ],
    kpis: [],
    quickActions: [
      { id: "schedule", label: "Schedule Exam", icon: "CalendarDays", route: "/examinations" },
      { id: "appeal", label: "Process Appeal", icon: "Scale", route: "/grade-appeals" },
      { id: "freeze", label: "Manage Freezes", icon: "CalendarDays", route: "/freezes" },
      { id: "results", label: "Generate Results", icon: "FileText", route: "/grade-distributions" },
    ],
  },

  Registrar: {
    role: "Registrar",
    displayName: "Registrar",
    allowedRoutes: [
      "/examinations", "/foreign-students", "/convocations",
      "/catalog", "/classes", "/prerequisite-conflicts", "/data-import",
      "/readiness", "/achievements", "/analytics", "/telemetry",
    ],
    dashboardModules: [
      { id: "enrollment", label: "Enrollment Overview", icon: "BookOpenCheck", route: "/classes", color: "blue", description: "Current enrollment stats, sections, and capacity.", priority: 1, domain: "academic", badge: "LIVE" },
      { id: "model-analytics", label: "Model Analytics", icon: "Activity", route: "/analytics", color: "violet", description: "Model quality snapshots and admissions decision explanations.", priority: 8, domain: "compliance" },
      { id: "foreign", label: "Foreign Students", icon: "UserRound", route: "/foreign-students", color: "blue", description: "Visa tracking, immigration, and international student records.", priority: 2, domain: "academic" },
      { id: "convocations", label: "Convocations", icon: "GraduationCap", route: "/convocations", color: "violet", description: "Graduation ceremony planning.", priority: 3, domain: "operations" },
      { id: "catalog", label: "Catalog Ingestion", icon: "FileText", route: "/catalog", color: "blue", description: "Course catalog import and management.", priority: 4, domain: "academic" },
      { id: "classes-gen", label: "Class Generation", icon: "Building2", route: "/classes", color: "blue", description: "Auto-generate sections for new semesters.", priority: 5, domain: "academic" },
      { id: "readiness", label: "Accreditation Readiness", icon: "ShieldCheck", route: "/readiness", color: "green", description: "Self-assessment score and criteria tracking.", priority: 6, domain: "compliance" },
      { id: "data-import", label: "Data Import", icon: "FileSearch", route: "/data-import", color: "amber", description: "Bulk student/faculty data import.", priority: 7, domain: "operations" },
    ],
    kpis: [],
    quickActions: [
      { id: "enroll", label: "Enroll Student", icon: "UserRound", route: "/data-import" },
      { id: "gen-classes", label: "Generate Classes", icon: "Building2", route: "/classes" },
      { id: "import", label: "Import Data", icon: "FileSearch", route: "/data-import" },
      { id: "readiness", label: "View Readiness", icon: "ShieldCheck", route: "/readiness" },
    ],
  },

  Finance: {
    role: "Finance",
    displayName: "Finance Head",
    allowedRoutes: [
      "/finance/*", "/defaulter-prediction", "/audit",
      "/student/finance/*", "/data-import",
    ],
    dashboardModules: [
      { id: "finance-dash", label: "Finance Dashboard", icon: "Landmark", route: "/finance", color: "green", description: "Executive dashboard with KPIs, revenue, and cash flow.", priority: 1, domain: "finance", badge: "LIVE" },
      { id: "finance-ops", label: "Finance Operations", icon: "FileText", route: "/finance/operations", color: "green", description: "Voucher pipeline, approvals, and gateway events.", priority: 2, domain: "finance" },
      { id: "gateways", label: "Payment Gateways", icon: "CreditCard", route: "/finance/gateways", color: "green", description: "Configure and test payment gateway integrations.", priority: 3, domain: "finance" },
      { id: "defaulters", label: "Fee Defaulter AI", icon: "FileSearch", route: "/defaulter-prediction", color: "amber", description: "AI-powered defaulter risk prediction.", priority: 4, domain: "finance" },
      { id: "audit", label: "Audit & Compliance", icon: "ShieldCheck", route: "/audit", color: "red", description: "Audit trails, control monitoring, SOC2 evidence.", priority: 5, domain: "compliance" },
    ],
    kpis: [
      { id: "revenue", label: "Revenue", value: "--", format: "currency", icon: "Landmark", iconColor: "green", metric: "revenue" },
      { id: "collection-rate", label: "Collection Rate %", value: "--", format: "percentage", icon: "CreditCard", iconColor: "blue", metric: "collectionRate" },
      { id: "cash", label: "Cash Position", value: "--", format: "currency", icon: "Landmark", iconColor: "violet", metric: "cashPosition" },
      { id: "outstanding", label: "Overdue (PKR)", value: "--", format: "currency", icon: "Landmark", iconColor: "red", metric: "outstanding" },
    ],
    quickActions: [
      { id: "challan", label: "Generate Challan", icon: "FileText", route: "/finance" },
      { id: "export", label: "Export IFRS Report", icon: "FileSearch", route: "/finance" },
      { id: "defaulters", label: "View Defaulters", icon: "FileSearch", route: "/defaulter-prediction" },
      { id: "reconcile", label: "Reconcile Payments", icon: "CreditCard", route: "/finance/operations" },
    ],
  },

  HR: {
    role: "HR",
    displayName: "HR Head",
    allowedRoutes: [
      "/visiting-faculty", "/foreign-students", "/transport",
      "/data-import", "/placement",
    ],
    dashboardModules: [
      { id: "visiting", label: "Visiting Faculty", icon: "UserRound", route: "/visiting-faculty", color: "amber", description: "Guest lecturer marketplace.", priority: 1, domain: "operations" },
      { id: "transport", label: "Staff Transport", icon: "Briefcase", route: "/transport", color: "amber", description: "Fleet management and route planning.", priority: 2, domain: "operations" },
      { id: "foreign", label: "Foreign Students", icon: "UserRound", route: "/foreign-students", color: "blue", description: "Visa tracking and international records.", priority: 3, domain: "academic" },
      { id: "data-import", label: "Data Import", icon: "FileSearch", route: "/data-import", color: "amber", description: "Bulk HR data import.", priority: 4, domain: "operations" },
      { id: "placement", label: "Placement", icon: "Briefcase", route: "/placement", color: "green", description: "Internship and job placement tracking.", priority: 5, domain: "welfare" },
    ],
    kpis: [],
    quickActions: [
      { id: "add-visiting", label: "Add Visiting Faculty", icon: "UserRound", route: "/visiting-faculty" },
      { id: "transport", label: "Manage Transport", icon: "Briefcase", route: "/transport" },
      { id: "placement", label: "Process Placement", icon: "Briefcase", route: "/placement" },
      { id: "import", label: "Import Staff Data", icon: "FileSearch", route: "/data-import" },
    ],
  },

  IT: {
    role: "IT",
    displayName: "IT Head",
    allowedRoutes: [
      "/sync-lag", "/replication",
      "/audit", "/data-import", "/imports", "/setup", "/document-vault",
    ],
    dashboardModules: [
      { id: "sync-lag", label: "Sync Lag Monitor", icon: "FileSearch", route: "/sync-lag", color: "amber", description: "Data synchronization lag across replicas.", priority: 1, domain: "operations", badge: "MONITOR" },
      { id: "replication", label: "Data Replication", icon: "FileSearch", route: "/replication", color: "amber", description: "Replica status and failover management.", priority: 2, domain: "operations" },
      { id: "audit", label: "Audit Logs", icon: "ShieldCheck", route: "/audit", color: "red", description: "Security audit logs and access trails.", priority: 3, domain: "compliance" },
      { id: "import-jobs", label: "Import Jobs", icon: "FileSearch", route: "/imports", color: "amber", description: "Data import job status and history.", priority: 4, domain: "operations" },
      { id: "document-vault", label: "Document Vault", icon: "FileText", route: "/document-vault", color: "blue", description: "Secure document storage tiers and verification.", priority: 5, domain: "operations" },
      { id: "setup", label: "Setup", icon: "Settings", route: "/setup", color: "blue", description: "System configuration and installation.", priority: 6, domain: "operations" },
    ],
    kpis: [],
    quickActions: [
      { id: "audit", label: "View Audit Log", icon: "ShieldCheck", route: "/audit" },
      { id: "sync", label: "Check Sync Status", icon: "FileSearch", route: "/sync-lag" },
      { id: "import", label: "Run Import", icon: "FileSearch", route: "/data-import" },
      { id: "replication", label: "Replication", icon: "FileSearch", route: "/replication" },
    ],
  },

  Dean: {
    role: "Dean",
    displayName: "Dean",
    allowedRoutes: [
      "/classes", "/examinations", "/grade-distributions",
      "/grade-appeals", "/prerequisite-conflicts",
      "/readiness", "/visiting-faculty", "/analytics", "/telemetry",
    ],
    dashboardModules: [
      { id: "dept-overview", label: "Department Overview", icon: "Building2", route: "/classes", color: "blue", description: "All departments, programmes, and faculty under your faculty.", priority: 1, domain: "academic" },
      { id: "model-analytics", label: "Model Analytics", icon: "Activity", route: "/analytics", color: "violet", description: "Model quality and global explanation snapshots.", priority: 5, domain: "compliance" },
      { id: "grade-dist", label: "Grade Distribution", icon: "FileSearch", route: "/grade-distributions", color: "blue", description: "Faculty-wide grade analysis and anomalies.", priority: 2, domain: "academic" },
      { id: "appeals", label: "Grade Appeals", icon: "Scale", route: "/grade-appeals", color: "amber", description: "Appeal pipeline and committee review.", priority: 3, domain: "compliance" },
      { id: "readiness", label: "Accreditation Readiness", icon: "ShieldCheck", route: "/readiness", color: "green", description: "Programme accreditation status.", priority: 4, domain: "compliance" },
    ],
    kpis: [],
    quickActions: [
      { id: "review-appeals", label: "Review Appeals", icon: "Scale", route: "/grade-appeals" },
      { id: "grades", label: "View Grade Distributions", icon: "FileSearch", route: "/grade-distributions" },
      { id: "readiness", label: "Check Readiness", icon: "ShieldCheck", route: "/readiness" },
    ],
  },

  HOD: {
    role: "HOD",
    displayName: "Head of Department",
    allowedRoutes: [
      "/classes", "/examinations", "/grade-distributions",
      "/grade-appeals", "/visiting-faculty", "/attendance",
      "/analytics", "/telemetry",
    ],
    dashboardModules: [
      { id: "dept-classes", label: "Department Classes", icon: "BookOpenCheck", route: "/classes", color: "blue", description: "Active courses, sections, and enrollment in your department.", priority: 1, domain: "academic" },
      { id: "model-analytics", label: "Model Analytics", icon: "Activity", route: "/analytics", color: "violet", description: "Model quality and global explanation snapshots.", priority: 5, domain: "compliance" },
      { id: "grades", label: "Grade Submission", icon: "ClipboardCheck", route: "/examinations", color: "blue", description: "Faculty grade submissions and moderation status.", priority: 2, domain: "academic" },
      { id: "attendance", label: "Attendance Overview", icon: "CalendarDays", route: "/attendance", color: "blue", description: "Department-wide attendance summary.", priority: 3, domain: "academic" },
      { id: "visiting", label: "Visiting Faculty", icon: "UserRound", route: "/visiting-faculty", color: "amber", description: "Request and manage visiting lecturers.", priority: 4, domain: "operations" },
    ],
    kpis: [],
    quickActions: [
      { id: "review-grades", label: "Review Grades", icon: "ClipboardCheck", route: "/examinations" },
      { id: "manage-courses", label: "Manage Courses", icon: "BookOpenCheck", route: "/classes" },
      { id: "add-visiting", label: "Add Visiting Faculty", icon: "UserRound", route: "/visiting-faculty" },
    ],
  },

  Admin: {
    role: "Admin",
    displayName: "Administrator",
    allowedRoutes: ["/*"],
    dashboardModules: [
      { id: "command-center", label: "Command Center", icon: "LayoutDashboard", route: "/", color: "blue", description: "Executive overview of all university operations.", priority: 1, domain: "academic", badge: "ALL ACCESS" },
      { id: "finance-dash", label: "Finance Dashboard", icon: "Landmark", route: "/finance", color: "green", description: "Complete financial oversight.", priority: 2, domain: "finance" },
      { id: "enrollment", label: "Enrollment", icon: "BookOpenCheck", route: "/classes", color: "blue", description: "Enrollment statistics and class management.", priority: 3, domain: "academic" },
      { id: "audit", label: "Audit & Compliance", icon: "ShieldCheck", route: "/audit", color: "red", description: "Control monitoring and compliance tracking.", priority: 4, domain: "compliance" },
      { id: "defaulters", label: "Fee Defaulter AI", icon: "FileSearch", route: "/defaulter-prediction", color: "amber", description: "AI risk prediction for fee defaulters.", priority: 5, domain: "finance" },
      { id: "readiness", label: "Accreditation", icon: "ShieldCheck", route: "/readiness", color: "green", description: "Accreditation readiness scoring.", priority: 6, domain: "compliance" },
      { id: "examinations", label: "Examinations", icon: "ClipboardCheck", route: "/examinations", color: "blue", description: "Exam management and result processing.", priority: 7, domain: "academic" },
      { id: "placement", label: "Placement", icon: "Briefcase", route: "/placement", color: "violet", description: "Internship and placement tracking.", priority: 8, domain: "welfare" },
      { id: "mental-health", label: "Mental Health", icon: "ShieldCheck", route: "/mental-health", color: "violet", description: "Counselor queue and crisis management.", priority: 9, domain: "welfare" },
      { id: "achievements", label: "Achievements", icon: "GraduationCap", route: "/achievements", color: "violet", description: "Student achievement tracking.", priority: 10, domain: "welfare" },
      { id: "portal-health", label: "Portal Health", icon: "ShieldCheck", route: "/portal-health", color: "green", description: "System health monitoring.", priority: 11, domain: "operations" },
      { id: "data-import", label: "Data Import", icon: "FileSearch", route: "/data-import", color: "amber", description: "Bulk data import management.", priority: 12, domain: "operations" },
      { id: "model-analytics", label: "Model Analytics", icon: "Activity", route: "/analytics", color: "violet", description: "Model quality and global explanation snapshots; decision transparency.", priority: 13, domain: "compliance" },
    ],
    kpis: [
      { id: "collection", label: "Fee Collection Rate %", value: "--", format: "percentage", icon: "CreditCard", iconColor: "green", metric: "collectionRate" },
    ],
    quickActions: [
      { id: "exec-report", label: "View Executive Report", icon: "FileText", route: "/" },
      { id: "board", label: "Board Portal", icon: "LayoutDashboard", route: "/board" },
      { id: "accreditation", label: "Accreditation Status", icon: "ShieldCheck", route: "/readiness" },
      { id: "health", label: "System Health", icon: "ShieldCheck", route: "/portal-health" },
    ],
  },

  VC: {
    role: "VC",
    displayName: "Vice Chancellor",
    allowedRoutes: ["/*"],
    dashboardModules: [
      { id: "command-center", label: "Command Center", icon: "LayoutDashboard", route: "/", color: "blue", description: "Executive overview of all university operations.", priority: 1, domain: "academic", badge: "EXECUTIVE" },
      { id: "finance-dash", label: "Finance Dashboard", icon: "Landmark", route: "/finance", color: "green", description: "Revenue, expenses, cash flow, and budgets.", priority: 2, domain: "finance" },
      { id: "board", label: "Board Portal", icon: "LayoutDashboard", route: "/board", color: "violet", description: "Board of Governors meeting materials.", priority: 3, domain: "compliance" },
      { id: "enrollment", label: "Enrollment Overview", icon: "BookOpenCheck", route: "/classes", color: "blue", description: "Enrollment trends and programme health.", priority: 4, domain: "academic" },
      { id: "readiness", label: "Accreditation", icon: "ShieldCheck", route: "/readiness", color: "green", description: "Accreditation readiness across all programmes.", priority: 5, domain: "compliance" },
      { id: "defaulters", label: "Fee Defaulter AI", icon: "FileSearch", route: "/defaulter-prediction", color: "amber", description: "Risk prediction and intervention tracking.", priority: 6, domain: "finance" },
      { id: "audit", label: "Audit & Compliance", icon: "ShieldCheck", route: "/audit", color: "red", description: "Control monitoring and audit evidence.", priority: 7, domain: "compliance" },
      { id: "placements", label: "Placement", icon: "Briefcase", route: "/placement", color: "violet", description: "Placement rates and partner companies.", priority: 8, domain: "welfare" },
      { id: "achievements", label: "Achievements", icon: "GraduationCap", route: "/achievements", color: "violet", description: "Student and faculty achievements.", priority: 9, domain: "welfare" },
      { id: "mental-health", label: "Mental Health", icon: "ShieldCheck", route: "/mental-health", color: "violet", description: "Crisis monitoring and counselor load.", priority: 10, domain: "welfare" },
      { id: "portal-health", label: "Portal Health", icon: "ShieldCheck", route: "/portal-health", color: "green", description: "System health and performance.", priority: 11, domain: "operations" },
      { id: "transport", label: "Transport", icon: "Briefcase", route: "/transport", color: "amber", description: "Fleet status and route management.", priority: 12, domain: "operations" },
      { id: "model-analytics", label: "Model Analytics", icon: "Activity", route: "/analytics", color: "violet", description: "Model quality and global explanation snapshots.", priority: 13, domain: "compliance" },
    ],
    kpis: [
      { id: "collection", label: "Fee Collection Rate %", value: "--", format: "percentage", icon: "CreditCard", iconColor: "green", metric: "collectionRate" },
    ],
    quickActions: [
      { id: "exec-report", label: "View Executive Report", icon: "FileText", route: "/" },
      { id: "board", label: "Board Portal", icon: "LayoutDashboard", route: "/board" },
      { id: "accreditation", label: "Accreditation Status", icon: "ShieldCheck", route: "/readiness" },
      { id: "health", label: "System Health", icon: "ShieldCheck", route: "/portal-health" },
    ],
  },

  SuperAdmin: {
    role: "SuperAdmin",
    displayName: "Super Admin",
    allowedRoutes: ["/*"],
    dashboardModules: [], // Inherits from Admin + VC combined
    kpis: [],
    quickActions: [],
  },
};

// ─── Exported functions ───

export function getRoleProfile(roles: string[]): RoleProfile {
  const primary = topRole(roles);

  // SuperAdmin inherits everything + Admin modules + VC KPIs
  if (primary === "SuperAdmin") {
    const adminProfile = profiles["Admin"]!;
    const vcProfile = profiles["VC"]!;
    return {
      role: "SuperAdmin",
      displayName: "Super Admin",
      allowedRoutes: ["/*"],
      dashboardModules: adminProfile.dashboardModules,
      kpis: vcProfile.kpis,
      quickActions: vcProfile.quickActions,
    };
  }

  // VC inherits from Admin
  if (primary === "VC") {
    return profiles["VC"]!;
  }

  // Admin inherits
  if (primary === "Admin") {
    return profiles["Admin"]!;
  }

  if (primary === "AdmissionsHead") {
    const registrar = profiles["Registrar"]!;
    return {
      ...registrar,
      role: primary,
      displayName: "Admissions Head",
      dashboardModules: [
        { id: "admissions-decisions", label: "Admissions Decisions", icon: "FileSearch", route: "/telemetry", color: "violet", description: "Review published merit decisions and their recorded arithmetic.", priority: 1, domain: "compliance", badge: "LIVE" },
        ...registrar.dashboardModules,
      ],
      quickActions: [
        { id: "admissions-decisions", label: "Review Decisions", icon: "FileSearch", route: "/telemetry" },
        ...registrar.quickActions,
      ],
    };
  }

  if (primary === "OBECoordinator") {
    return {
      ...profiles["Dean"]!,
      role: primary,
      displayName: "OBE / QA Officer",
      allowedRoutes: ["/obe", "/readiness"],
      dashboardModules: [
        { id: "obe", label: "OBE Evidence", icon: "BookOpenCheck", route: "/obe", color: "blue", description: "Review recorded CLO-to-PO mappings and outcome evidence.", priority: 1, domain: "academic", badge: "LIVE" },
        { id: "readiness", label: "Accreditation Readiness", icon: "ShieldCheck", route: "/readiness", color: "green", description: "Review accreditation readiness and criteria.", priority: 2, domain: "compliance" },
      ],
      quickActions: [{ id: "obe", label: "Review OBE Evidence", icon: "BookOpenCheck", route: "/obe" }],
    };
  }

  if (primary === "Auditor") {
    const auditModule = profiles["IT"]!.dashboardModules.filter(module => module.id === "audit");
    return {
      ...profiles["IT"]!,
      role: primary,
      displayName: "Auditor / Compliance Officer",
      allowedRoutes: ["/audit", "/auditor"],
      dashboardModules: auditModule,
      quickActions: profiles["IT"]!.quickActions.filter(action => action.id === "audit"),
    };
  }

  // Map role names to profile keys
  const profileKey = primary === "Finance Controller" || primary === "CFO" ? "Finance"
    : primary === "HRManager" || primary === "HRHead" ? "HR"
    : primary === "ITHead" ? "IT"
    : primary === "Dean" ? "Dean"
    : primary === "HOD" ? "HOD"
    : primary === "ExamController" ? "ExamController"
    : primary === "Registrar" ? "Registrar"
    : primary === "Faculty" || primary === "Employee" ? "Faculty"
    : primary === "Student" ? "Student"
    : "Student";

  return profiles[profileKey] ?? profiles["Student"]!;
}

export function canAccessRoute(roles: string[], route: string): boolean {
  const profile = getRoleProfile(roles);
  return profile.allowedRoutes.some(pattern => {
    if (pattern === "/*") return true;
    if (pattern.endsWith("/*")) {
      return route.startsWith(pattern.slice(0, -2));
    }
    return route === pattern || route.startsWith(pattern + "/");
  });
}

export function getDomainColor(domain: string): string {
  const colors: Record<string, string> = {
    academic: "var(--domain-academic, #315fd5)",
    finance: "var(--domain-finance, #17835a)",
    operations: "var(--domain-operations, #a76413)",
    compliance: "var(--domain-compliance, #b23b4a)",
    welfare: "var(--domain-welfare, #7251c7)",
  };
  return colors[domain] ?? colors["academic"]!;
}

export function getGreeting(name: string): { greeting: string; context: string } {
  const hour = new Date().getHours();
  const timeGreeting = hour < 12 ? "morning" : hour < 17 ? "afternoon" : "evening";
  const firstName = name.split(" ")[0] ?? name;

  return {
    greeting: `Good ${timeGreeting}, ${firstName}.`,
    context: "",
  };
}
