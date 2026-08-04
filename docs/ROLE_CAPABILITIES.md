# AegisCore — Role Capabilities & Module Access Matrix

---

## STUDENT
**The end user the entire institution exists for.**

| Module | What They Can Do |
|--------|-----------------|
| **Dashboard** | Personal home screen — GPA summary, fee balance, attendance %, next class |
| **Grades** | View all grades per semester with component breakdown (midterm, final, assignments). See letter grade + marks. Filter by semester. |
| **Grade Appeals** | Submit an appeal with grounds and evidence. Track appeal status (Submitted → Under Review → Upheld/Dismissed). Cannot see committee members' identities. |
| **Fees & Payments** | View outstanding balance, fee breakdown per semester. Pay via RAAST, credit card, or bank challan. Download PQC-verified receipts. Report payment issues. |
| **Attendance** | View attendance percentage per course. See daily status (Present/Late/Absent/Excused). Request leave. |
| **Timetable** | View weekly class schedule. Download as PDF. |
| **Documents** | Request transcripts, enrollment letters, roll number slips. Track request status. |
| **Achievements** | View earned badges, Dean's List entries, leaderboard rank. See next-badge recommendations with progress bars. |
| **SafeSpace (Mental Health)** | Submit anonymous concern with severity (Routine/Urgent/Crisis). Receive counselor response. Track your case via anonymous token. Crisis button bypasses queue for immediate help. |
| **NidaBot (Voice)** | Ask questions in Urdu or English: "Mera result kya hai?", "Fees kitni baki hai?", "Schedule kab hai?". Get spoken and text responses. |
| **Elections** | View candidates. Cast encrypted ballot. View election results. |
| **Profile** | Upload profile picture (face-validated, no editing allowed). Update contact info. Change password. Toggle dark mode. Toggle personality mode (fun/professional voice). |
| **Transport** | View bus route, stop location, live GPS tracking if enabled. Receive "bus is 10 min away" notification. |

**Cannot access:** Other students' data, financial records beyond their own, faculty dashboards, administrative functions, disciplinary case details.

---

## FACULTY (Professor / Associate Professor / Lecturer / Lab Instructor)
**Academic first, administrator second.**

| Module | What They Can Do |
|--------|-----------------|
| **My Classes** | View assigned courses and sections. See enrolled student list with photos. |
| **Attendance** | Take attendance per class session (Present/Late/Absent). Works offline — syncs when reconnected. View attendance history per student. |
| **Grading** | Enter marks for midterms, finals, quizzes, assignments, projects, labs. View grade submission deadlines. Submit final grades. Draft/save before submitting. |
| **Grade Appeals** | Receive notification when a student appeals. Review student's grounds and evidence. Submit response within 5 working days. Cannot override committee decision. |
| **Timetable** | View personal teaching schedule. |
| **Workload Dashboard** | See teaching hours, contact hours, enrollment load. View status (Underloaded/Optimal/Overloaded/Critical). Flag overload to HOD. |
| **Payslip** | View monthly payslip with breakdown. Download PDF. |
| **Leave** | Apply for leave with reason. Track approval status. See remaining leave balance. |
| **Claims** | Submit travel/conference claims with receipts. Track reimbursement status. |
| **Internship Supervision** | Evaluate assigned interns. Submit supervisor evaluation (40% weight). Monitor intern reports and attendance. |
| **Research** | Register research projects, publications, patents. Apply for grants. Log conference travel. |
| **Visiting Faculty** | (If visiting) View contract, compensation details, performance evaluation. |
| **Profile** | Upload profile picture. Update contact info. Toggle personality mode. |

**Cannot access:** Student financial data, other faculty's payslips, HR/payroll administration, fee collection, disciplinary sanctions.

---

## HOD (Head of Department)
**Manages a department. Faculty first, budget second.**

*Everything FACULTY can do, plus:*

| Module | What They Can Do |
|--------|-----------------|
| **Department Budget** | View department allocation vs spend. Approve faculty claims. Track petty cash. See YTD spend by category. Budget expiry warnings. |
| **Faculty Management** | View all department faculty workloads. Identify overloaded/underloaded staff. Recommend visiting faculty hires. |
| **Grade Distribution** | View grade distributions across all department courses. See anomaly alerts (grade inflation, high failure rates). Compare sections. |
| **Curriculum** | Propose curriculum changes. View curriculum tree with accreditation overlay. See prerequisite chains. |
| **Visiting Faculty Marketplace** | Post open teaching slots. Review matched candidates. Approve contracts. |
| **Placement Analytics** | View department placement rates, average stipend, company retention. Partner company health. |
| **Accreditation** | View department accreditation readiness score. See PLO/CLO attainment data. Respond to accreditation criteria gaps. |

**Cannot access:** Other departments' budgets, institution-wide financial data, board-level reports, disciplinary case outcomes for other departments.

---

## DEAN
**Academic leader. Runs a faculty/school. Strategic, not operational.**

*Everything HOD can do, plus:*

| Module | What They Can Do |
|--------|-----------------|
| **School Budget** | View consolidated budget for all departments. Approve major expenditures. |
| **Faculty Quality** | View PhD ratio, research output, student satisfaction across school. Faculty recruitment approval. |
| **Academic Performance** | View CGPA distributions across all programs. Identify at-risk programs. |
| **Curriculum Governance** | Approve/reject curriculum proposals. View full curriculum version history. |
| **Accreditation** | View school-wide accreditation readiness. Prioritize gap closures. |
| **Disciplinary Cases** | Review severe cases escalated from departments. Approve sanctions. |
| **Convocation** | Approve graduating cohort. Review eligibility exceptions. |

**Cannot access:** Institution-wide financial reports, other schools' data, board-level strategic plans, HR/payroll.

---

## REGISTRAR
**Process is their profession. Lives in forms, deadlines, and student records.**

| Module | What They Can Do |
|--------|-----------------|
| **Enrollment** | Manage registration windows. Override credit limits. Process enrollment exceptions. Handle waitlists. |
| **Student Records** | View complete student profiles. Process transfers between programs. Manage academic standing (Good/Probation/Warning/Dismissed). |
| **Grade Appeals** | Route appeals to committees. Process final decisions. Authorize grade changes. |
| **Transcripts** | Generate official transcripts. Verify transcript hashes for external validation. Process transcript requests. |
| **Convocation** | Run eligibility checks. Generate degree certificates. Manage ceremony logistics. Process HEC attestation. |
| **Semester Freeze** | Review and approve/reject freeze requests. Set return schedules. Process withdrawals. |
| **Timetable** | Manage room allocations. Resolve scheduling conflicts. Publish final timetable. |
| **Degree Audit** | Run degree audits for any student. Process course waivers. Run what-if scenarios. |
| **Foreign Students** | Manage international student registry. Process MORA submissions. Generate enrollment letters for visa renewal. Monitor compliance. |
| **Results** | Lock semester results (irreversible — requires confirmation). Publish results. Process grade revisions. |

**Cannot access:** Financial transactions, HR/payroll, board-level reports, counseling records.

---

## CONTROLLER OF EXAMINATIONS
**Gatekeeper of academic integrity.**

| Module | What They Can Do |
|--------|-----------------|
| **Exam Scheduling** | Create exam timetables. Allocate rooms and invigilators. Set exam rules. |
| **Online Proctoring** | Configure proctoring sessions. Review flagged recordings. Escalate integrity violations. |
| **Results** | Verify grade distributions before publication. Flag anomalies (bimodal distributions, clustering). Hold results for investigation. |
| **Plagiarism** | Review similarity reports. Initiate academic misconduct cases. Compare submissions across semesters. |
| **Transcripts** | Verify transcript accuracy. Authorize official transcript issuance. |
| **Degree Verification** | Generate verification tokens. Respond to external verification requests. |

**Cannot access:** Financial data, HR, budget, counseling records.

---

## FINANCE OFFICER / ACCOUNTS OFFICER
**Precision is their identity. Ambiguity is an insult.**

| Module | What They Can Do |
|--------|-----------------|
| **Fee Management** | Configure fee structures per program. Generate bulk challans. Set installment schedules. Apply late fees. |
| **Payment Processing** | Record payments. Issue receipts. Reconcile bank statements. Handle bounced payments. Process refunds. |
| **General Ledger** | Create journal entries (PQC-signed, immutable after posting). View chart of accounts. Close fiscal periods. Run trial balance. |
| **Procurement** | Create purchase requisitions. Issue purchase orders. Process goods receipts. Match invoices. |
| **Petty Cash** | Manage imprest accounts. Record petty cash transactions. Reconcile. |
| **Payroll** | Process monthly payroll. Handle EOBI, income tax deductions. Generate payslips. |
| **Scholarships** | Process scholarship applications. Disburse awards. Track renewals. Handle scholarship lifecycle. |
| **Defaulter Prediction** | View risk scores. Generate interventions (reminders, payment plans, escalation). Track outcomes. Run cash flow forecasts. |
| **Budget** | View department vs actual spending. Flag budget variances. |
| **Reports** | Generate fee collection reports. Export to Excel. Schedule automatic reports. |

**Personality mode:** LOCKED OFF permanently. No toggle available.

**Cannot access:** Student grades, attendance, academic records, counseling data, HR hiring decisions.

---

## FINANCE MANAGER / FINANCE CONTROLLER / CFO
**Strategic financial leadership.**

*Everything FINANCE OFFICER can do, plus:*

| Module | What They Can Do |
|--------|-----------------|
| **Financial Dashboard** | Executive finance view: revenue, expenses, net surplus/deficit, cash position. Currency exposure. Treasury. |
| **Budget Approval** | Approve department budgets. Set institution-wide fee policies. Authorize major expenditures. |
| **Audit** | View complete audit trail. Verify PQC seal chain. Run SoD (Segregation of Duties) checks. |
| **Investments** | Manage institutional investments. Track portfolio performance. |
| **Reporting** | Generate HEC financial reports. PIFRA compliance. External audit packages. |

**Personality mode:** LOCKED OFF permanently.

**Cannot access:** Student academic records, HR/payroll details, counseling data.

---

## HR MANAGER
**People operations.**

| Module | What They Can Do |
|--------|-----------------|
| **Employee Management** | Full employee lifecycle: hiring, contracts, promotions, terminations. Manage leave policies. |
| **Recruitment** | Post job openings. Manage applications. Schedule interviews. Generate offer letters. |
| **Payroll Configuration** | Set salary structures. Configure allowances and deductions. Set EOBI/income tax rules. |
| **Performance** | Manage appraisal cycles. Track training records. |
| **Visiting Faculty** | Manage visiting faculty pool. Vet credentials. Generate contracts. Process compensation. Rate performance. |
| **Attendance** | View staff attendance. Process leave requests. |
| **Reports** | Generate headcount reports. Workforce analytics. |

**Cannot access:** Student data, academic records, financial transactions beyond payroll.

---

## VICE CHANCELLOR / RECTOR
**Strategic operator. Bridges governance and academia.**

| Module | What They Can Do |
|--------|-----------------|
| **Executive Dashboard** | Academic Excellence Index, Financial Stability Score, Accreditation Compliance, Student Success Score, Faculty Quality Index, Composite Ranking. |
| **Strategic Plan** | View strategic goals with progress bars. See OnTrack/AtRisk/OffTrack status. |
| **Benchmarking** | Compare institution against national average and peer institutions. |
| **Accreditation** | View institutional readiness scores across all frameworks. |
| **Alerts** | Escalated alerts: financial (<60% collection), academic (accreditation at risk), HR (critical shortage), compliance, reputational. |
| **Reports** | HEC ranking indicator reports. Institutional performance reports. |

**Personality mode:** LOCKED OFF permanently.

**Cannot access:** Individual student records, individual faculty salaries, disciplinary case details, counseling records.

---

## CHANCELLOR / BOARD OF GOVERNORS
**Rarely logs in. When they do, it matters. Read-only. Summaries, not data.**

| Module | What They Can Do |
|--------|-----------------|
| **Board Dashboard** | Institutional health score with letter grades (A-F). Composite ranking. |
| **Board Meeting Pack** | Auto-generated executive summary since last meeting. Frozen dashboard snapshot. Agenda items with data. Decision briefs with options + recommendations + impact + risk. |
| **Strategic Plan** | Goals with % achieved, trajectory projection. |
| **Alerts** | Board-relevant escalated alerts only (financial, accreditation, compliance, reputational). |
| **Benchmarks** | Peer institution comparison. National average comparison. HEC ranking factor gap analysis. |

**Hard rule:** NEVER exposed to individual student records, individual faculty salaries, or disciplinary case details.

**Personality mode:** LOCKED OFF permanently. No toggle shown. Ever.

---

## IT ADMIN / SUPER ADMIN
**Root access. Can see and configure everything. Does not see counseling data.**

| Module | What They Can Do |
|--------|-----------------|
| **Tenant Setup** | Create/manage tenants (universities). Run setup wizard. Configure branding (logos, colors). |
| **User Management** | Create/edit/delete users. Assign roles. Set permissions. Manage API keys. |
| **Configuration** | Configure fiscal year, currency format, locale, calendar type, grading schemes, credit rules, security settings. |
| **Replication** | Configure read replicas (1-10). Set scaling thresholds. Monitor replication lag. Manage sync-lag correction windows. |
| **Document Vault** | Configure storage tiers (50/20/5 year). Monitor PQC verification. View access audit logs. |
| **Data Import** | Connect external databases. Run migrations. Upload format templates. |
| **Backup & DR** | Configure backup schedules. Trigger manual backups. Run restore (emergency only — irreversible). Execute failover. |
| **License** | View license status. Manage renewals. |
| **Security** | View brute force logs. Monitor rate limiting. Audit login attempts. Configure CORS origins. |
| **Logs** | Access all system logs. View audit trails. |
| **Feature Flags** | Enable/disable modules per tenant. |

**Cannot access:** Counseling case notes (private to counselor).

---

## COUNSELOR (Mental Health)
**Bound by strict confidentiality. Anonymized access.**

| Module | What They Can Do |
|--------|-----------------|
| **Ticket Queue** | View severity-sorted queue (Crisis → Urgent → Routine). See concern descriptions, not student identities (unless student opted in). |
| **Response** | Send secure messages to anonymous tokens. Schedule video calls. Share resources. |
| **Case Notes** | Write private case notes — visible ONLY to the counselor, NEVER to administration. |
| **Crisis Protocol** | One-click escalation triggers immediate alert to on-call counselor + campus security. |
| **Mandatory Reporting** | Trigger mandatory report for imminent harm or child safeguarding. System auto-notifies legal authority. |
| **Follow-up** | Schedule wellness check-ins (48h for urgent, 7d for routine). View opt-in weekly pulse survey responses. |
| **Analytics** | View aggregate stats (ticket volume by type, severity trends, response times, peak stress periods). NO individual data. |

---

## INTERNATIONAL OFFICE (Foreign Student Management)
**Visa and immigration compliance.**

| Module | What They Can Do |
|--------|-----------------|
| **Registry** | Manage foreign student records (passport, visa, entry date, MORA registration). |
| **Expiry Alerts** | View color-coded expiry dashboard (90/60/30/14/7 days, expired). |
| **MORA Reporting** | Submit semester enrollment verification. Process new student registrations. Handle graduation/departure notifications. |
| **Enrollment Letters** | Generate letters for visa renewal with digital signature. |
| **Visa Renewals** | Initiate renewal workflows. Track submitted → processing → approved. |
| **Compliance Breaches** | Report and track breaches. Freeze academic services if required. Notify embassies. |
| **Dashboard** | Students by nationality, compliance rate, expiry calendar, action queue. |

---

## PARENT / GUARDIAN
**Read-only. Limited to their own child.**

| Module | What They Can Do |
|--------|-----------------|
| **Fee Status** | View outstanding balance and payment history. |
| **Academic Progress** | View grades and CGPA (if student enabled sharing). |
| **Attendance** | View attendance percentage. |
| **Transport** | Track bus location. Receive "bus is 10 min from stop" notification. Get emergency "child not boarded" alert. |
| **Achievements** | Receive notification when child earns Dean's List or major badge (if enabled). |

**Cannot access:** Any other student's data. Cannot make changes. Read-only view of their child only.

---

## VISITOR
**Pre-registered external guests.**

| Module | What They Can Do |
|--------|-----------------|
| **Gate Entry** | QR code check-in/out. Vehicle pass. Host notification. |
| **Purpose Logging** | Record visit purpose and host. |

**Cannot access:** Any internal module, student data, or administrative functions.

---

*End of Role Capabilities Document*
