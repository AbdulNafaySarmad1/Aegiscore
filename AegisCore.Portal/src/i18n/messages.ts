import { personalityMessages } from "./personality-messages";

export const portalLocales = ["en-PK", "en-US", "en-GB", "ar-AE", "ms-MY"] as const;
export type PortalLocale = (typeof portalLocales)[number];

const enNchart = {
  insightTrendUp: "{measure} up {percentage} this period",
  insightTrendDown: "{measure} down {percentage} this period",
  insightTrendFlat: "{measure} holding steady",
  insightTrendGeneric: "{measure} changed {percentage}",
  insightRecoveryYes: "Recovered {percentage}% from trough",
  insightRecoveryNo: "Still {percentage}% below peak",
  insightDominant: "{segment} dominates at {share}%",
  measureValue: "Trend",
  legend: "Chart legend",
  toggleSeries: "Toggle series",
  sortBy: "Sort by",
  dataTable: "Data table",
  exportCsv: "Export CSV",
  exportPng: "Export as PNG",
  export: "Export chart",
  exportUnavailable: "Export unavailable at this time",
  toggleDataTable: "Toggle data table",
  errorMessage: "Unable to render chart",
  retry: "Retry",
  emptyTitle: "No data to display",
  emptySubtitle: "Add data to see your chart here",
  gauge: "Gauge value",
  gaugeLabel: "Current value",
  gaugeLow: "Low",
  gaugeMedium: "Medium",
  gaugeHigh: "High",
  gaugeCritical: "Critical",
  heatmap: "Activity heatmap",
  heatmapMore: "More",
  heatmapLess: "Less",
  tooltipAboveMean: "Above average ({sigma})",
  tooltipBelowMean: "Below average ({sigma})",
  ganttPlaceholder: "Gantt chart will be available soon",
} satisfies Record<string, string>;

const en = {
  shell: {
    commandCenter: "Command center", feesAid: "Fees and aid", academics: "Academics",
    financeDashboard: "Finance dashboard", financeOperations: "Finance operations", departmentFinance: "Department finance", paymentGateways: "Payment gateways",
    auditCompliance: "Audit and compliance", universityStructure: "University structure", settings: "Settings",
    signIn: "Sign in to access your AegisCore workspace.", loading: "Loading your university workspace...",
    closeNavigation: "Close navigation", openNavigation: "Open navigation", primaryNavigation: "Primary navigation",
    workspace: "Workspace", help: "Help and support", toggleTheme: "Toggle color theme", user: "User",
    tenantWorkspace: "Tenant workspace", title: "AegisCore command center",
    subtitle: "Tools shown here are derived from your authenticated roles.", available: "Available workspaces",
    openWorkspace: "Open workspace", sessionSecurity: "Session security",
    sessionDetail: "Tenant and role access is enforced by the API.", authenticated: "Authenticated",
    roleNavigation: "Role-scoped navigation", assignedRoles: "{count} assigned role(s)",
    liveData: "Live data only", noSimulation: "No simulated dashboard records", locale: "Language and locale"
  },
  finance: {
    unavailable: "Finance data is unavailable or your role is not authorized.", department: "Finance department",
    executive: "Executive finance", executiveDetail: "Posted ledger performance, collections, and cryptographic posture.",
    refresh: "Refresh dashboard", operations: "Operations", gateways: "Gateways", metrics: "Finance metrics",
    revenue: "Revenue", expenses: "Expenses", net: "Net surplus / deficit", cash: "Cash position",
    revenueExpense: "Revenue and expenses", topExpenses: "Top expense categories", accessibleTable: "Accessible data table",
    month: "Month", postings: "Postings", category: "Category", amount: "Amount", feeFunnel: "Fee collection funnel",
    feeFunnelDetail: "Demanded, collected, and overdue balances", demanded: "Demanded", collected: "Collected",
    overdue: "Overdue", openDemands: "Open demands", securityPosture: "Security posture",
    securityDetail: "Ledger chain and audit seal status", lastSeal: "Last seal", notSealed: "Not sealed",
    merkleRoot: "Merkle root", key: "Key", unavailableValue: "unavailable", verifySignature: "Verify voucher signature",
    verifyDetail: "Retrieve its stored ML-DSA proof.", voucherId: "Voucher ID", verify: "Verify", voucher: "Voucher",
    status: "Status", proofAvailable: "PQC verified proof available", unsigned: "Unsigned", algorithm: "Algorithm",
    proofLoadError: "Voucher proof could not be loaded.", proofVerified: "PQC verified", proofFailed: "Verification failed",
    keySchedule: "Key rotation schedule", purpose: "Purpose", daysToExpiry: "Days to expiry", verificationAlerts: "Verification alerts",
    entry: "Entry", reason: "Reason"
    ,operationsUnavailable: "Finance operations are unavailable or your role is not authorized.", operationsTitle: "Voucher pipeline",
    operationsDetail: "Oldest items first, with live gateway activity.", executiveDashboard: "Executive dashboard",
    approvalQueue: "Approval queue", priorityAge: "Priority ordered by age", gatewayActivity: "Gateway activity",
    recentEvents: "Most recent tenant events", sandbox: "SANDBOX", sandboxHelp: "Simulated - no real transaction",
    collections: "Daily collections", collectionVolume: "Collection volume for the last 30 days", date: "Date", count: "Count",
    studentUnavailable: "This student finance statement is unavailable.", studentPortal: "Student portal",
    feesAid: "Fees and financial aid", studentDetail: "Your demands, instalments, payments, and awards.", feeStatement: "Fee statement",
    due: "Due {date}", total: "Total", paid: "Paid", outstanding: "Outstanding", installment: "Instalment {sequence}",
    installmentProgress: "Instalment {sequence} payment progress", payments: "Payments",
    receiptDetail: "Confirmed receipts carry a PQC verification reference.", receiptAvailable: "PQC receipt available",
    scholarships: "Scholarships and aid", scholarshipsDetail: "Active awards and renewal dates", noDemands: "No fee demands",
    noPayments: "No payments recorded", noAwards: "No scholarship awards", feesStatusLoading: "Loading fee status\u2026"
    ,payRaast: "Pay via Raast", payGateway: "Pay securely", requestingPayment: "Requesting payment...",
    paymentRequested: "Payment request sent. Approve it in your payment app.", paymentRequestFailed: "Payment request could not be started.",
    openPaymentApp: "Open payment app", cashFlowWaterfall: "Cash flow waterfall", budgetBurn: "Budget burn",
    departmentLabel: "Department", consumed: "Consumed", spent: "Spent", allocated: "Allocated", committed: "Committed", available: "Available",
    currencyExposure: "Currency exposure", currencyExposureDetail: "Open foreign-currency balances by settlement currency.", currency: "Currency",
    foreignAmount: "Foreign amount", functionalAmount: "Functional amount", approve: "Approve", reject: "Reject", decisionNote: "Decision note",
    rejectionNoteRequired: "A rejection note is required.", decisionFailed: "The voucher decision could not be saved.", departmentUnavailable: "Department finance data is unavailable.",
    departmentDashboardDetail: "Budget, commitments, petty cash, and approvals for your department.", myBudget: "My budget", pettyCash: "Petty cash",
    pettyCashBalance: "Petty cash balance", imprestThreshold: "Imprest threshold", noPettyCash: "No active petty cash fund", spendByCategory: "YTD spend by category",
    myPendingApprovals: "My pending approvals", noPendingApprovals: "No pending approvals"
  },
  academic: {
    gradeAppeals: "Grade Appeals", gradeAppealsDesc: "Manage student grade appeals, evidence, and committee decisions",
    underReview: "Under review", submitted: "Submitted", resolved: "Resolved", pendingFaculty: "Pending faculty response",
    gradeDistributions: "Grade Distribution Analysis", plagiarism: "Plagiarism Detection",
    convocations: "Convocation Management", freezes: "Semester Freeze & Deferral",
    proctoring: "Online Exam Proctoring", prereqConflicts: "Prerequisite Conflicts",
    defaulterPrediction: "Fee Default Risk Prediction", visitingFaculty: "Visiting Faculty Marketplace",
    readiness: "Accreditation Readiness Meter", achievements: "Achievements",
    mentalHealth: "Mental Health Support", foreignStudents: "Foreign Student Management",
    voiceAssistant: "Voice Assistant (NidaBot)", elections: "Student Elections",
    boardPortal: "Board of Governors Portal", classGeneration: "Class Generation & LMS",
    catalogIngestion: "Course Catalog Ingestion", cafeteria: "Cafeteria Management",
    examinations: "Examinations", placement: "Placement",
    transport: "Transport Management",
    appealPipeline: "Appeal Pipeline", appealSubmissions: "Appeal Submissions",
    appealsRegistry: "Appeals Registry", noAppealsFiled: "No appeals filed",
    noAppealsDesc: "Submit the first grade appeal using the form above",
    submitAppeal: "Submit Appeal", submitting: "Submitting...",
    attachDoc: "Attach supporting document (PDF, max 10MB)",
    generateBrief: "Generate Brief", generatingBrief: "Generating committee brief...",
    loadingAppeals: "Loading appeals...",
    runAnalysis: "Run Analysis", analyzing: "Analyzing...",
    selectCourse: "Select course...", allSemesters: "All semesters",
    mean: "Mean", median: "Median", stdDev: "Std Deviation",
    passRate: "Pass Rate", failureRate: "Failure Rate",
    highVariance: "High variance", normalSpread: "Normal spread",
    needsAttention: "Needs attention", withinBounds: "Within bounds",
    centralTendency: "Central tendency",
    anomalyAlerts: "Anomaly Alerts", noAnomaliesDetected: "No anomalies detected",
    noAnomaliesDesc: "Grade distributions appear normal across all courses",
    loadingAnomalies: "Loading anomalies...",
    scanDocument: "Scan Document", scanning: "Scanning...",
    similarityScore: "Similarity Score",
    lowRisk: "Low Risk", moderate: "Moderate", highRisk: "High Risk",
    collusionGraph: "Collusion Graph", noCollusionPatterns: "No collusion patterns detected",
    documentScanner: "Document Scanner",
    matchedSources: "Matched Sources",
    aiWarning: "AI-generation probability exceeds threshold. Review recommended.",
    createSession: "Create Proctoring Session", startSession: "Start Session",
    faceAnalysis: "Face Analysis", analyzeFace: "Analyze Face",
    lockdownValidation: "Lockdown Validation", validateLockdownStatus: "Validate Lockdown Status",
    studentIntegrityGrid: "Student Integrity Grid",
    noStudentsInSessions: "No students in proctoring sessions",
    proctorEventLog: "Proctor Event Log", noEventsRecorded: "No events recorded",
    noEventsDesc: "All sessions operating normally",
    loadingProctoring: "Loading proctoring dashboard...",
    studentRiskLookup: "Student Risk Lookup", lookUp: "Look Up", looking: "Looking...",
    riskDrivers: "Risk Drivers",
    generateInterventions: "Generate Interventions",
    generating: "Generating...", generatePlan: "Generate Plan",
    activeInterventionTemplates: "Active Intervention Templates",
    cashFlowForecast: "Cash Flow Forecast", noForecastData: "No forecast data available",
    healthy: "Healthy", caution: "Caution", atRisk: "At Risk",
    loadingDefaulter: "Loading defaulter prediction data...",
    computingReadiness: "Computing readiness score...",
    checkFreshness: "Check Freshness", checkingFreshness: "Checking...",
    overallScore: "Overall Score", daysUntilAudit: "Days until audit",
    complianceVelocity: "Compliance velocity (weekly)",
    criteriaFullyMet: "Criteria fully met",
    alertsFeed: "Alerts Feed", allClear: "All clear — no alerts",
    criteriaBreakdown: "Criteria Breakdown",
    gapAnalysis: "Gap Analysis", noGapsIdentified: "No gaps identified",
    noGapsDesc: "All criteria are met or in progress",
    anonymousTicket: "Anonymous Ticket Submission",
    submitAnonymousTicket: "Submit Anonymous Ticket",
    mandatoryReportPanel: "Mandatory Report Panel",
    fileMandatoryReport: "File Mandatory Report",
    noTicketsRequireReport: "No tickets require mandatory reporting",
    counselorQueue: "Counselor Queue", queueIsClear: "Queue is clear",
    noPendingTickets: "No pending mental health tickets",
    crisisTickets: "Crisis Tickets", noActiveCrises: "No active crises",
    immediateAttention: "Immediate attention!",
    loadingMentalHealth: "Loading mental health dashboard...",
    marksEntry: "Marks Entry", recordMarks: "Record Marks", recording: "Recording...",
    resultPublication: "Result Publication", publishResults: "Publish Results", publishing: "Publishing...",
    assessmentSchemes: "Assessment Schemes",
    noSchemesDefined: "No assessment schemes defined",
    noSchemesDesc: "Configure assessment components for your courses",
    loadingExaminations: "Loading examination dashboard...",
    noPublicationSlots: "No publication slots configured",
    placementRate: "Placement Rate",
    avgStipend: "Avg Stipend", timeToPlace: "Time to Place",
    partnerCompanies: "Partner Companies",
    offersMade: "Offers Made", inPipeline: "in pipeline",
    studentReadiness: "Student Readiness Assessment", checkReadiness: "Check Readiness",
    readinessScore: "Readiness Score", skillAssessment: "Skill Assessment",
    recommendations: "Recommendations",
    partnerManagement: "Partner Management",
    noPartnerCompanies: "No partner companies configured",
    activePlacements: "Active Placements",
    noActivePlacements: "No active placements",
    noActivePlacementsDesc: "No student placements are currently tracked",
    loadingPlacement: "Loading placement dashboard...",
    studentsPlaced: "students placed",
    perMonth: "per month", averageFromSearch: "average from search",
    activeEngagements: "active engagements",
    fleetStatus: "Fleet Status", routeManagement: "Route Management",
    gpsTracking: "GPS Tracking", studentBoardingLog: "Student Boarding Log",
    complianceAlerts: "Compliance Alerts", transportFeeSummary: "Transport Fee Summary",
    activeFleet: "Active Fleet", activeRoutes: "Active Routes",
    studentsRegistered: "Students Registered",
    complianceRate: "Compliance Rate",
    vehicle: "Vehicle", registration: "Registration", capacity: "Capacity", driver: "Driver",
    route: "Route", stops: "Stops", assignedVehicle: "Assigned Vehicle",
    qrScan: "QR Scan", scanStudentCard: "Scan student card QR to log boarding",
    scanSuccess: "Boarding logged: {name}",
  },
  nchart: enNchart
} satisfies Record<string, Record<string, string>>;

const ar = {
  shell: { ...en.shell, commandCenter: "\u0645\u0631\u0643\u0632 \u0627\u0644\u0642\u064a\u0627\u062f\u0629", feesAid: "\u0627\u0644\u0631\u0633\u0648\u0645 \u0648\u0627\u0644\u0645\u0633\u0627\u0639\u062f\u0627\u062a", academics: "\u0627\u0644\u0634\u0624\u0648\u0646 \u0627\u0644\u0623\u0643\u0627\u062f\u064a\u0645\u064a\u0629", financeDashboard: "\u0644\u0648\u062d\u0629 \u0627\u0644\u0645\u0627\u0644\u064a\u0629", financeOperations: "\u0639\u0645\u0644\u064a\u0627\u062a \u0627\u0644\u0645\u0627\u0644\u064a\u0629", paymentGateways: "\u0628\u0648\u0627\u0628\u0627\u062a \u0627\u0644\u062f\u0641\u0639", auditCompliance: "\u0627\u0644\u062a\u062f\u0642\u064a\u0642 \u0648\u0627\u0644\u0627\u0645\u062a\u062b\u0627\u0644", universityStructure: "\u0647\u064a\u0643\u0644 \u0627\u0644\u062c\u0627\u0645\u0639\u0629", settings: "\u0627\u0644\u0625\u0639\u062f\u0627\u062f\u0627\u062a", loading: "\u062c\u0627\u0631 \u062a\u062d\u0645\u064a\u0644 \u0645\u0633\u0627\u062d\u0629 \u0639\u0645\u0644 \u0627\u0644\u062c\u0627\u0645\u0639\u0629...", workspace: "\u0645\u0633\u0627\u062d\u0629 \u0627\u0644\u0639\u0645\u0644", help: "\u0627\u0644\u0645\u0633\u0627\u0639\u062f\u0629 \u0648\u0627\u0644\u062f\u0639\u0645", tenantWorkspace: "\u0645\u0633\u0627\u062d\u0629 \u0639\u0645\u0644 \u0627\u0644\u0645\u0624\u0633\u0633\u0629", title: "\u0645\u0631\u0643\u0632 \u0642\u064a\u0627\u062f\u0629 AegisCore", subtitle: "\u062a\u0638\u0647\u0631 \u0627\u0644\u0623\u062f\u0648\u0627\u062a \u0648\u0641\u0642\u0627\u064b \u0644\u0635\u0644\u0627\u062d\u064a\u0627\u062a\u0643 \u0627\u0644\u0645\u0648\u062b\u0642\u0629.", available: "\u0645\u0633\u0627\u062d\u0627\u062a \u0627\u0644\u0639\u0645\u0644 \u0627\u0644\u0645\u062a\u0627\u062d\u0629", openWorkspace: "\u0641\u062a\u062d \u0645\u0633\u0627\u062d\u0629 \u0627\u0644\u0639\u0645\u0644", sessionSecurity: "\u0623\u0645\u0646 \u0627\u0644\u062c\u0644\u0633\u0629", authenticated: "\u062a\u0645 \u0627\u0644\u062a\u062d\u0642\u0642", roleNavigation: "\u062a\u0646\u0642\u0644 \u062d\u0633\u0628 \u0627\u0644\u062f\u0648\u0631", liveData: "\u0628\u064a\u0627\u0646\u0627\u062a \u0645\u0628\u0627\u0634\u0631\u0629 \u0641\u0642\u0637", locale: "\u0627\u0644\u0644\u063a\u0629 \u0648\u0627\u0644\u0645\u0646\u0637\u0642\u0629" },
  finance: { ...en.finance, unavailable: "\u0628\u064a\u0627\u0646\u0627\u062a \u0627\u0644\u0645\u0627\u0644\u064a\u0629 \u063a\u064a\u0631 \u0645\u062a\u0627\u062d\u0629 \u0623\u0648 \u0623\u0646 \u062f\u0648\u0631\u0643 \u063a\u064a\u0631 \u0645\u062e\u0648\u0644.", department: "\u0625\u062f\u0627\u0631\u0629 \u0627\u0644\u0645\u0627\u0644\u064a\u0629", executive: "\u0627\u0644\u0645\u0627\u0644\u064a\u0629 \u0627\u0644\u062a\u0646\u0641\u064a\u0630\u064a\u0629", executiveDetail: "\u0623\u062f\u0627\u0621 \u062f\u0641\u062a\u0631 \u0627\u0644\u0623\u0633\u062a\u0627\u0630 \u0648\u0627\u0644\u062a\u062d\u0635\u064a\u0644 \u0648\u0627\u0644\u0648\u0636\u0639 \u0627\u0644\u062a\u0634\u0641\u064a\u0631\u064a.", refresh: "\u062a\u062d\u062f\u064a\u062b \u0627\u0644\u0644\u0648\u062d\u0629", operations: "\u0627\u0644\u0639\u0645\u0644\u064a\u0627\u062a", gateways: "\u0628\u0648\u0627\u0628\u0627\u062a \u0627\u0644\u062f\u0641\u0639", metrics: "\u0645\u0624\u0634\u0631\u0627\u062a \u0627\u0644\u0645\u0627\u0644\u064a\u0629", revenue: "\u0627\u0644\u0625\u064a\u0631\u0627\u062f\u0627\u062a", expenses: "\u0627\u0644\u0645\u0635\u0631\u0648\u0641\u0627\u062a", net: "\u0635\u0627\u0641\u064a \u0627\u0644\u0641\u0627\u0626\u0636 \u0623\u0648 \u0627\u0644\u0639\u062c\u0632", cash: "\u0627\u0644\u0645\u0631\u0643\u0632 \u0627\u0644\u0646\u0642\u062f\u064a", revenueExpense: "\u0627\u0644\u0625\u064a\u0631\u0627\u062f\u0627\u062a \u0648\u0627\u0644\u0645\u0635\u0631\u0648\u0641\u0627\u062a", topExpenses: "\u0623\u0639\u0644\u0649 \u0641\u0626\u0627\u062a \u0627\u0644\u0645\u0635\u0631\u0648\u0641\u0627\u062a", accessibleTable: "\u062c\u062f\u0648\u0644 \u0628\u064a\u0627\u0646\u0627\u062a \u0645\u064a\u0633\u0631", month: "\u0627\u0644\u0634\u0647\u0631", postings: "\u0627\u0644\u0642\u064a\u0648\u062f", category: "\u0627\u0644\u0641\u0626\u0629", amount: "\u0627\u0644\u0645\u0628\u0644\u063a", feeFunnel: "\u0645\u0633\u0627\u0631 \u062a\u062d\u0635\u064a\u0644 \u0627\u0644\u0631\u0633\u0648\u0645", demanded: "\u0627\u0644\u0645\u0637\u0644\u0648\u0628", collected: "\u0627\u0644\u0645\u062d\u0635\u0644", overdue: "\u0645\u062a\u0623\u062e\u0631", openDemands: "\u0645\u0637\u0627\u0644\u0628\u0627\u062a \u0645\u0641\u062a\u0648\u062d\u0629", securityPosture: "\u0627\u0644\u0648\u0636\u0639 \u0627\u0644\u0623\u0645\u0646\u064a", lastSeal: "\u0622\u062e\u0631 \u062e\u062a\u0645", notSealed: "\u063a\u064a\u0631 \u0645\u062e\u062a\u0648\u0645", merkleRoot: "\u062c\u0630\u0631 \u0645\u064a\u0631\u0643\u0644", key: "\u0627\u0644\u0645\u0641\u062a\u0627\u062d", verifySignature: "\u0627\u0644\u062a\u062d\u0642\u0642 \u0645\u0646 \u062a\u0648\u0642\u064a\u0639 \u0627\u0644\u0633\u0646\u062f", voucherId: "\u0645\u0639\u0631\u0641 \u0627\u0644\u0633\u0646\u062f", verify: "\u062a\u062d\u0642\u0642", voucher: "\u0627\u0644\u0633\u0646\u062f", status: "\u0627\u0644\u062d\u0627\u0644\u0629", unsigned: "\u063a\u064a\u0631 \u0645\u0648\u0642\u0639", algorithm: "\u0627\u0644\u062e\u0648\u0627\u0631\u0632\u0645\u064a\u0629" },
  academic: { ...en.academic },
  nchart: enNchart
};

const ms = {
  shell: { ...en.shell, commandCenter: "Pusat arahan", feesAid: "Yuran dan bantuan", academics: "Akademik", financeDashboard: "Papan pemuka kewangan", financeOperations: "Operasi kewangan", paymentGateways: "Gerbang pembayaran", auditCompliance: "Audit dan pematuhan", settings: "Tetapan", loading: "Memuatkan ruang kerja universiti...", workspace: "Ruang kerja", help: "Bantuan dan sokongan", tenantWorkspace: "Ruang kerja institusi", title: "Pusat arahan AegisCore", available: "Ruang kerja tersedia", openWorkspace: "Buka ruang kerja", sessionSecurity: "Keselamatan sesi", authenticated: "Disahkan", roleNavigation: "Navigasi mengikut peranan", liveData: "Data langsung sahaja", locale: "Bahasa dan lokal" },
  finance: { ...en.finance, unavailable: "Data kewangan tidak tersedia atau peranan anda tidak dibenarkan.", department: "Jabatan kewangan", executive: "Kewangan eksekutif", refresh: "Muat semula papan pemuka", operations: "Operasi", gateways: "Gerbang", metrics: "Metrik kewangan", revenue: "Hasil", expenses: "Perbelanjaan", net: "Lebihan / defisit bersih", cash: "Kedudukan tunai", revenueExpense: "Hasil dan perbelanjaan", topExpenses: "Kategori perbelanjaan teratas", accessibleTable: "Jadual data boleh akses", month: "Bulan", postings: "Catatan", category: "Kategori", amount: "Jumlah", feeFunnel: "Corong kutipan yuran", demanded: "Dituntut", collected: "Dikutip", overdue: "Tertunggak", openDemands: "Tuntutan terbuka", securityPosture: "Postur keselamatan", lastSeal: "Meterai terakhir", notSealed: "Belum dimeterai", merkleRoot: "Akar Merkle", key: "Kunci", verifySignature: "Sahkan tandatangan baucar", voucherId: "ID baucar", verify: "Sahkan", voucher: "Baucar", status: "Status", unsigned: "Tidak ditandatangani", algorithm: "Algoritma" },
  academic: { ...en.academic },
  nchart: enNchart
};

function deepMerge(target: Record<string, any>, source: Record<string, any>): Record<string, any> {
  const result = { ...target };
  for (const key of Object.keys(source)) {
    if (source[key] && typeof source[key] === "object" && !Array.isArray(source[key]) && typeof result[key] === "object" && !Array.isArray(result[key])) {
      result[key] = deepMerge(result[key], source[key]);
    } else {
      result[key] = source[key];
    }
  }
  return result;
}

const baseByLocale: Record<PortalLocale, typeof en> = {
  "en-PK": en, "en-US": en, "en-GB": en, "ar-AE": ar, "ms-MY": ms
};

export const portalMessages: Record<PortalLocale, typeof en> = (() => {
  const merged: Record<string, typeof en> = {};
  for (const loc of portalLocales) {
    merged[loc] = deepMerge(baseByLocale[loc], personalityMessages[loc] ?? {}) as typeof en;
  }
  return merged as Record<PortalLocale, typeof en>;
})();
