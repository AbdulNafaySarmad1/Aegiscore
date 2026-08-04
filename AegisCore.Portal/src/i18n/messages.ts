export const portalLocales = ["en-PK", "en-US", "en-GB", "ar-AE", "ms-MY"] as const;
export type PortalLocale = (typeof portalLocales)[number];

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
    noPayments: "No payments recorded", noAwards: "No scholarship awards"
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
  }
} satisfies Record<string, Record<string, string>>;

const ar = {
  shell: { ...en.shell, commandCenter: "مركز القيادة", feesAid: "الرسوم والمساعدات", academics: "الشؤون الأكاديمية", financeDashboard: "لوحة المالية", financeOperations: "عمليات المالية", paymentGateways: "بوابات الدفع", auditCompliance: "التدقيق والامتثال", universityStructure: "هيكل الجامعة", settings: "الإعدادات", loading: "جار تحميل مساحة عمل الجامعة...", workspace: "مساحة العمل", help: "المساعدة والدعم", tenantWorkspace: "مساحة عمل المؤسسة", title: "مركز قيادة AegisCore", subtitle: "تظهر الأدوات وفقاً لصلاحياتك الموثقة.", available: "مساحات العمل المتاحة", openWorkspace: "فتح مساحة العمل", sessionSecurity: "أمن الجلسة", authenticated: "تم التحقق", roleNavigation: "تنقل حسب الدور", liveData: "بيانات مباشرة فقط", locale: "اللغة والمنطقة" },
  finance: { ...en.finance, unavailable: "بيانات المالية غير متاحة أو أن دورك غير مخول.", department: "إدارة المالية", executive: "المالية التنفيذية", executiveDetail: "أداء دفتر الأستاذ والتحصيل والوضع التشفيري.", refresh: "تحديث اللوحة", operations: "العمليات", gateways: "بوابات الدفع", metrics: "مؤشرات المالية", revenue: "الإيرادات", expenses: "المصروفات", net: "صافي الفائض أو العجز", cash: "المركز النقدي", revenueExpense: "الإيرادات والمصروفات", topExpenses: "أعلى فئات المصروفات", accessibleTable: "جدول بيانات ميسر", month: "الشهر", postings: "القيود", category: "الفئة", amount: "المبلغ", feeFunnel: "مسار تحصيل الرسوم", demanded: "المطلوب", collected: "المحصل", overdue: "متأخر", openDemands: "مطالبات مفتوحة", securityPosture: "الوضع الأمني", lastSeal: "آخر ختم", notSealed: "غير مختوم", merkleRoot: "جذر ميركل", key: "المفتاح", verifySignature: "التحقق من توقيع السند", voucherId: "معرف السند", verify: "تحقق", voucher: "السند", status: "الحالة", unsigned: "غير موقع", algorithm: "الخوارزمية" }
};

const ms = {
  shell: { ...en.shell, commandCenter: "Pusat arahan", feesAid: "Yuran dan bantuan", academics: "Akademik", financeDashboard: "Papan pemuka kewangan", financeOperations: "Operasi kewangan", paymentGateways: "Gerbang pembayaran", auditCompliance: "Audit dan pematuhan", settings: "Tetapan", loading: "Memuatkan ruang kerja universiti...", workspace: "Ruang kerja", help: "Bantuan dan sokongan", tenantWorkspace: "Ruang kerja institusi", title: "Pusat arahan AegisCore", available: "Ruang kerja tersedia", openWorkspace: "Buka ruang kerja", sessionSecurity: "Keselamatan sesi", authenticated: "Disahkan", roleNavigation: "Navigasi mengikut peranan", liveData: "Data langsung sahaja", locale: "Bahasa dan lokal" },
  finance: { ...en.finance, unavailable: "Data kewangan tidak tersedia atau peranan anda tidak dibenarkan.", department: "Jabatan kewangan", executive: "Kewangan eksekutif", refresh: "Muat semula papan pemuka", operations: "Operasi", gateways: "Gerbang", metrics: "Metrik kewangan", revenue: "Hasil", expenses: "Perbelanjaan", net: "Lebihan / defisit bersih", cash: "Kedudukan tunai", revenueExpense: "Hasil dan perbelanjaan", topExpenses: "Kategori perbelanjaan teratas", accessibleTable: "Jadual data boleh akses", month: "Bulan", postings: "Catatan", category: "Kategori", amount: "Jumlah", feeFunnel: "Corong kutipan yuran", demanded: "Dituntut", collected: "Dikutip", overdue: "Tertunggak", openDemands: "Tuntutan terbuka", securityPosture: "Postur keselamatan", lastSeal: "Meterai terakhir", notSealed: "Belum dimeterai", merkleRoot: "Akar Merkle", key: "Kunci", verifySignature: "Sahkan tandatangan baucar", voucherId: "ID baucar", verify: "Sahkan", voucher: "Baucar", status: "Status", unsigned: "Tidak ditandatangani", algorithm: "Algoritma" }
};

export const portalMessages: Record<PortalLocale, typeof en> = {
  "en-PK": en, "en-US": en, "en-GB": en, "ar-AE": ar, "ms-MY": ms
};
