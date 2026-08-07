# AEGISCORE — Quantum-Safe University ERP

**Built by Nocturne Systems (Pvt.) Ltd.**

---

AegisCore is a complete, production-ready university ERP. It handles every operation a university performs — admissions to graduation, fee collection to board governance, classroom attendance to mental health crisis response, exam proctoring to cafeteria menu planning. It serves every human being in a university: the first-year student paying their challan on a phone, the faculty member taking attendance offline in a dead-zone classroom, the finance officer chasing defaulters, the registrar preparing for an HEC audit, the Chancellor reviewing endowment performance, the counselor handling a crisis ticket, the parent checking if their child boarded the bus home.

It does all of this on one system, with one consistent interface, across web and mobile, in English and Urdu and Arabic and Malay, with post-quantum encryption protecting every byte.

---

## What AegisCore Replaces

| The old way | AegisCore |
|---|---|
| 8 different vendor systems that don't talk to each other | One system. Every module shares the same database, the same auth, the same tenant. |
| Excel sheets emailed around for attendance | Faculty opens their phone, taps names, hits save. Done. Works without internet. |
| Paper challans lost in bags | Student opens app, sees exactly what they owe, pays via RAAST in 30 seconds, gets a PQC-verified receipt. |
| "We'll get to that manually" when HEC visits | One-click accreditation visit mode — every criterion mapped, every piece of evidence compiled, every gap highlighted. |
| Counselor spreadsheets that violate privacy | Anonymous mental health ticketing — not even the admin knows who submitted. |
| "The ERP is down" during exams | Auto-scaling replicas, automatic failover, DCR with <60 second recovery. |
| Faculty scrambling for visiting instructors last minute | Marketplace of vetted, HEC-eligible visiting faculty, matched to slots by specialization. |
| "Where is the bus?" — parents calling constantly | Live GPS tracking, ETA per stop, automatic "your child has not boarded" alerts. |
| Manual fee defaulter hunting | AI predicts who will default before they do — and suggests how to help them pay, not punish them. |

---

## The Complete Module Suite

### ADMISSIONS & ENROLLMENT

**SHAP Merit Engine** — Pakistan's inter-board merit calculation (SSC + HSSC + entry test weighting), IBCC equivalence for O/A-Levels, automatic merit list generation with configurable seat allocation. Handles multiple admission categories, quotas, and waitlists simultaneously.

**Application Pipeline** — From online application form to fee challan generation to enrollment confirmation. Document upload with verification workflow. Application status tracking visible to applicants in real time.

**Enrollment Management** — Course registration with real-time prerequisite validation. Section distribution by configurable strategy (alphabetical, random, balanced by gender/CGPA, manual). Credit hour limit enforcement. Waitlist management with auto-promotion on drops.

---

### ACADEMICS — THE CORE

**Curriculum Tree (Obsidian-Style)** — Every program visualized as a force-directed knowledge graph. Courses as nodes sized by credit hours, colored by type, connected by prerequisite edges. Click any course to see CLOs, PLOs, faculty, enrollment history. Trace prerequisite chains forward ("What does this unlock?") and backward ("What do I need first?"). Filter by PLO, Bloom's level, semester. Import curricula from PDF, Excel, Word, or scanned images — the ingestion engine auto-detects tables, credit hour formats (3+0, 0+1, 3+0/2+1), prerequisite chains, and removed courses.

**OBE Framework** — Outcomes-Based Education engine. Program Learning Outcomes (PLOs) mapped to Course Learning Outcomes (CLOs) at Introduced/Developed/Achieved levels. Bloom's taxonomy assigned to every CLO. CIS scanner auto-extracts CLOs from uploaded course information sheets. Attainment calculation with threshold analysis. HEMIS export for HEC compliance. Rubric builder with criterion-level scoring.

**Grade Book & Appeals** — Marks entry per assessment component (midterm, final, quiz, assignment, project, lab). Automatic GPA/CGPA calculation. Grade distribution analysis with anomaly detection (bimodal distributions, grade inflation, clustering anomalies, section-to-section variance). Full grade appeal workflow: eligibility gate → grounds classification → evidence audit → faculty response → committee brief generation → decision processing (Upheld, Partially Upheld, Dismissed, Deferred).

**Plagiarism Detection** — Multi-layer scan across the entire institutional submission corpus. Verbatim match (≥5 consecutive words), paraphrase detection (cosine similarity on embeddings), structural fingerprinting (paragraph ordering, citation placement), cross-semester ghost-writing detection (Burrows' Delta style consistency), collusion ring detection via similarity graph clustering, AI-generated content probability (burstiness/perplexity analysis). Turnitin/iThenticate API integration for external verification.

**Online Exam Proctoring** — Pre-exam environment verification (system check, ID verification via face match + liveness detection, 360° room scan, browser lockdown). Real-time monitoring (face absent >10s alert, multiple faces critical flag, eye gaze off-screen suspicion, speech/whisper detection, tab switching, copy-paste). Adaptive response (INFO log only, WARNING in-exam notification, CRITICAL proctor alert + possible exam pause). Post-exam forensics (answer pattern analysis, collusion detection, time-per-question anomaly).

**Convocation Management** — End-to-end ceremony pipeline. Eligibility determination (credits, CGPA, financial clearance, thesis approval, library clearance, disciplinary clearance). Certificate production with tamper-evident serial numbers and QR verification. HEC attestation workflow. Live day-of dashboard with QR check-in, procession tracking, MC cue sheet auto-generation from attendance data.

**Disciplinary Case Management** — Full due process. Intake and triage (severity: Minor/Moderate/Severe/Critical). Investigation management with evidence checklist, witness interview schedule, document chain of custody. Charge formulation mapping evidence to specific policy clauses. Hearing orchestration with committee pack auto-generation. Sanctions engine with escalation logic based on prior cases. Appeals processing with separate appeal panel. Immutable audit trail.

**Semester Freeze & Deferral** — All lifecycle pause events. Eligibility analysis (freeze quota remaining, week-of-semester timing rules, academic standing impact). Financial impact calculation (refund schedule, credit adjustment, aid impact). Academic continuity plan (per-course snapshot, prerequisite gap detection). Stakeholder notification chain (student, all faculty, finance, hostel, library, international office, scholarship body). Return management (45-day advance notification, re-orientation checklist).

**Prerequisite Conflict Detection** — Live ripple effect simulation. Six conflict types: prerequisite not met, circular (DFS cycle detection), offering sequence conflict, credit load conflict, orphaned prerequisite, graduation bottleneck. Change impact simulation before any curriculum modification — shows exactly how many students, registrations, and graduation plans are affected. Registration guard: real-time prerequisite check at enrollment time.

**Student Progress Overlay** — Every student's personal GPS for graduation. Course status per student (Completed/InProgress/Unlocked/Locked/Blocked/Failed/Waived) projected onto the curriculum tree. Graduation pathway analysis with best/base/at-risk scenarios. Risk early warning (CGPA below threshold, scholarship at risk, failed prerequisite cascade, course only offered annually). Personalized recommendations (next semester course selection, GPA improvement strategy, probation recovery targets).

---

### FINANCE — THE ENGINE ROOM

**General Ledger** — Double-entry accounting with chart of accounts, journal entries with ML-DSA post-quantum signatures, fiscal periods, automated reversals. Merkle root ledger chain for cryptographic audit trail. Every entry is signed. Every seal is verifiable. You cannot tamper with the books.

**Fee Management** — Configurable fee structures per program per semester. Component-level fees (tuition, admission, lab, library, sports, transport, hostel, examination). Installment plans with configurable schedules. Late fee calculation with configurable grace periods. Bulk challan generation. Student fee statement with real-time outstanding balance.

**Payment Collection** — Multi-gateway support: RAAST (Pakistan's instant payment system), Stripe, Bank Challan, EasyPaisa, JazzCash, Billplz, DuitNow. PQC-verified payment receipts. Automatic reconciliation. Bounce handling with retry logic.

**Defaulter Prediction (DefaultSense AI)** — Predictive financial intelligence. Per-student feature engineering (payment behavior trends, academic correlation, financial profile, contextual features). Risk tier classification (GREEN 0-0.3, YELLOW 0.3-0.6, ORANGE 0.6-0.8, RED 0.8-1.0) with identified risk drivers. Automated intervention strategy per tier (standard reminder → personal outreach → payment plan offer → financial aid referral → director escalation → registration block). Cash flow forecasting with weekly projected inflows and shortfall alerts. Ethical guardrails: never predict based on gender/ethnicity/religion, support-first escalation, dignity language enforcement.

**Procurement** — Purchase requisition → purchase order → goods receipt → invoice matching → payment. Configurable approval chains. Budget check at requisition time. Vendor management with performance tracking.

**Treasury** — Cash management, investments, foreign currency with exposure tracking, bank reconciliation, petty cash with imprest system, fleet finance (depreciation, disposal), library fine processing.

**Payroll** — Pakistan-compliant: EOBI contribution, income tax withholding, provident fund. Configurable salary structures, allowances, deductions. Automatic payslip generation. Gratuity calculation.

**Scholarship Lifecycle** — Application, approval, disbursement, renewal tracking. Multiple scholarship types with configurable criteria. Automatic CGPA check for renewal eligibility.

---

### HUMAN RESOURCES

**Employee Management** — Full employee lifecycle: recruitment pipeline (job posting → application → shortlisting → interviews → offer → onboarding), contract management, leave management (configurable leave types and balances), attendance tracking, performance reviews, training records.

**Visiting Faculty Marketplace** — Verified talent pool with HEC eligibility determination per course level. Intelligent matching engine scoring candidates on specialization alignment, qualification, experience, availability, prior performance, compensation fit, industry experience. Weighted composite score → ranked top-5 shortlist per open slot. Automated contract generation with IP clause, confidentiality, deliverables. Performance monitoring (attendance, grade submission compliance, student feedback). Compensation processing with tax withholding. Pool health analytics with shortage flagging.

**Payroll** — Faculty/staff payroll with Pakistan-specific compliance (EOBI, income tax, PF). Configurable salary structures per designation. Automatic payslip generation. Leave encashment. Gratuity and pension calculation.

---

### MANAGEMENT & GOVERNANCE

**Accreditation Readiness (Live HEC/PEC Meter)** — Real-time compliance scoring. Criterion-level evaluation (Fully Met 1.0, Substantially Met 0.75, Partially Met 0.5, Minimally Met 0.25, Not Met 0.0) weighted by framework-defined importance. Category rollup to framework sections. Live meter: circular gauge with color zones, trend sparkline over 12 months, days-until-audit countdown, compliance velocity (at this improvement rate, will we hit 85%?). Gap analysis drill-down: specific gap, evidence needed, responsible owner, effort estimate, impact on score, priority rank. Evidence staleness monitoring. Predictive readiness forecast. Executive alert system (score drops >5%, NOT_MET criterion, <30 days to audit with <75%).

**Accreditation Visit Mode** — One-click read-only toggle. Time-limited access tokens for external reviewers. Evidence compilation engine (enrollment trends, faculty qualification matrix, curriculum compliance map, CLO/PLO attainment, infrastructure reports, financial health, graduate employment). Prior findings response dashboard. Program-specific drill-down (CLO/PLO heat maps, assessment compliance, faculty ratios, sample student work). Real-time natural language query support for reviewers.

**Board of Governors Portal (BoardView)** — Executive intelligence layer. Institutional health score (Academic Excellence, Financial Stability, Accreditation, Student Success, Faculty Quality, Composite). KPIs (enrollment vs target vs last year, fee collection %, CGPA distribution, faculty ratio, accreditation status, employment rate, publications). Role-based filtering (Chancellor sees strategic; Board Member sees financial + governance — NEVER individual student/faculty data). Board meeting intelligence pack (auto-generated executive summary, frozen dashboard snapshot, decision briefs with options/recommendations/impact/risk). Strategic plan monitoring (current vs target, % achieved, trajectory projection, at-risk flags). Benchmarking (peer comparison, national average, HEC ranking factor gap analysis). Escalated alerts (financial <60%, accreditation at risk, critical shortage, compliance non-compliance, reputational).

**Curriculum Governance** — Propose → Department Review → Board Review → Approve/Reject workflow. Full version history with change tracking. Regulatory impact assessment (does this change require HEC notification?). Student cohort impact analysis (who is affected by this curriculum change?).

---

### STUDENT LIFECYCLE

**Achievements & Gamification (AchieveCore)** — Badge system (Academic, Engagement, Lifecycle, Community, Leadership categories × Bronze/Silver/Gold/Platinum/Diamond tiers). Automatic detection of: Dean's List qualification, CGPA milestones (3.0/3.5/3.7/4.0), perfect semester, most-improved, course top-performer, perfect attendance, first submission, research publication, student council, graduation. Dean's List processing with Registrar verification, formal letter generation, transcript notation, parent notification. Leaderboard engine (by CGPA, improvement rate, badge count) with anonymization option. Personalized roadmap: next badge recommendations with progress bars, Dean's List predictor, goal setting with required grade calculation.

**Mental Health Support (SafeSpace)** — Anonymous, trauma-informed ticketing. Zero identity linkage unless student opts in. NLP keyword detection for crisis flags (explicit: "want to die", "kill myself"; implicit: "nobody would miss me", "won't be here for long"). Severity classification (ROUTINE → 24h, URGENT → 4h, CRISIS → immediate bypass). Intelligent counselor routing by specialization, language, and caseload. Counselor dashboard with severity-sorted queue, secure messaging, case notes (private — invisible to admin). Mandatory reporting protocol (imminent harm → campus security + Dean within 15 minutes; child safeguarding → legal authority). Follow-up: 48h URGENT check-in, 7-day ROUTINE, opt-in weekly wellness pulse. Aggregate reporting (volume by type, severity trend, response time, peak stress periods) — NO individual data.

**Foreign Student Compliance (VisaVault)** — Complete immigration management. Registry: nationality, passport, visa type/expiry, entry date, MORA registration, compliance status. Tiered document expiry alerts (90/60/30/14/7 days before, expired — color-coded by urgency). MORA reporting (semester enrollment verification, new registration on enrollment, graduation/departure notification). Auto-generated enrollment verification letters with Registrar digital signature and QR code. Visa renewal support (nationality-specific document checklist, timeline calculation, pre-filled forms). Compliance breach management with embassy notification.

**Placement & Internship (CareerLedger)** — Readiness assessment (skills gap analysis, resume scoring, LinkedIn audit). Opportunity matching engine (skill alignment, geographic preference, stipend filter). Placement tracking with milestone calendar. Monitoring (overdue reports, exploitative placement flagging). Assessment (supervisor 40% + report 30% + presentation 20% + attendance 10%). Analytics (placement rate, time-to-placement, stipend distribution, company retention).

**Voice Assistant (NidaBot)** — Multilingual voice interface (Urdu, Roman Urdu, English, code-switched). Natural Urdu understanding without formal phrasing. "Mera result kya hai?" → shows grades. "Fees kitni baki hai?" → shows balance. "Schedule kab hai?" → shows timetable. Student, faculty, and parent intents. Accessibility: slow speech ("Dhire bolein"), repeat ("Dobara bolein"), simplified language ("Aasan alfaaz mein"). Human escalation on emotional distress or explicit request.

**Student Elections (ElectCore)** — Democratic governance engine. Election setup with milestone reminders. Candidate nomination with eligibility verification (CGPA, discipline, endorsements). Voter token issuance with anonymous ballot linkage. Encrypted ballot with cryptographic chain and tamper detection. Result tabulation (simple/preferential/runoff). Certification with tamper-evident document. Automatic recount at <2% margin. Post-election audit archive.

---

### CAMPUS OPERATIONS

**Transport Management (TransitCore)** — Fleet registry with specs and compliance status. Route optimization by student home stop clustering. Seat allocation by proximity. Real-time GPS tracking with live map, ETA per stop, geofence alerts, speed monitoring (>60 km/h instant notification), stop arrival auto-log. Student boarding via QR ID scan → attendance log → no-show parent alert → unauthorized boarding driver alert. Safety compliance: vehicle registration/fitness/insurance expiry alerts, driver license/medical/training expiry, HARD RULE expired = grounded, daily digital pre-trip inspection (brakes, tires, lights, emergency exits, first aid kit). Financial: fee collection integration, revenue per route viability, cost per km/student, fuel consumption, depreciation. Parent communication: real-time location sharing, "Bus is 10 min away", "Your child has not boarded".

**Cafeteria Management (FoodCore)** — Digital menu (daily publication, pre-order 15 min advance, nutritional info, allergen flagging against student health profile). Cashless payment (student wallet, QR scan, meal plan deduction, split payment, low balance alert, digital receipt). Demand forecasting (historical sales → predicted daily demand adjusted for exam/holiday/event, prep quantity recommendations, real-time sell-through). Inventory (raw material vs sales tracking, low stock/expiry alerts, auto purchase orders, perishable expiry, waste log). Vendor performance (hygiene compliance digital checklist, complaint tracking, halal certification renewal, monthly scorecard).

**Asset & Inventory** — Full lifecycle: acquisition, depreciation, maintenance scheduling, transfer, disposal, verification. HEMIS export for HEC reporting. Consumable stock tracking with reorder points.

**Library** — Books catalog, issue/return, reservations, fine calculation, budget tracking, e-resource management.

**Hostel** — Room allocation, occupancy tracking, discipline logging, fee integration.

**Visitor Management** — Gate entry with vehicle pass, purpose logging, host notification.

**Maintenance** — Ticket submission → work order → contractor assignment → completion → verification. Preventive maintenance schedules. IoT sensor integration (occupancy, energy).

---

### CROSS-CUTTING POWER

**Multi-Tenant Architecture** — Every university is a separate tenant. Same code, zero data leakage. TenantId global query filter at the database level. Tenant-specific: currency format (South Asian PKR 1,20,000 or Western PKR 120,000), fiscal year (any start/end month), calendar (Gregorian or Hijri), locale (en-PK, en-US, ur-PK, ar-AE, ms-MY), branding (CSS variables injected at runtime), course code patterns, credit hour limits, grading schemes, anomaly thresholds, feature flags.

**Personality Mode** — Toggleable voice layer. Standard: professional, precise, neutral. Personality: Gen Z Pakistani English (Sadapay energy), Urdu phrases where natural ("ho gaya", "set hai", "bhai"), max one emoji on celebration, never on warnings/errors, never sarcastic about money. 272-line i18n file across 5 locales with 121 `.personality` key variants. Finance/Governance/Executive roles permanently locked OFF. Per-user toggle with global tenant lock-off capability. Never activates on errors, irreversible confirmations, or legal/compliance copy.

**Post-Quantum Security** — ML-DSA (FIPS 204) post-quantum signatures for all cryptographic operations. Hybrid PQC transport layer on top of TLS 1.3. Every journal entry signed. Every payment receipt verifiable. Brute force protection. Login audit trail. Casbin ABAC + RBAC hybrid authorization. Multi-tenant isolation. Data at rest encrypted. Certificate lifecycle management. Infisical/Azure/AWS/GCP/HashiCorp vault integration.

**Smart Replication** — Up to 10 read replicas auto-scaled based on connection load and QPS thresholds. Three distribution strategies (Round Robin, Weighted, Least Loaded). Lag-aware routing — replicas exceeding MaxLagSeconds are excluded from reads. Sync-lag correction window: data written through replicas is held pending for a configurable grace period before committing to central. Mistakes can be corrected or reverted before locking. IT-configurable: grace period (any value, no cap), auto-commit toggle, correction permissions.

**Disaster Recovery** — Streaming replication to warm standby. WAL archiving to S3-compatible storage. Automated snapshots on schedule. RTO <60 seconds (auto-failover), RPO <5 seconds (streaming). Point-in-time recovery from base snapshot + WAL replay. Full runbook: pre-flight checks, manual failover, PITR, snapshot management, health monitoring script.

**Course Catalog Ingestion** — Accept any document (scanned PDF via OCR, digital PDF, Excel, CSV, Word, image) → auto-detect degree programs, semesters, tables → extract course codes, titles, credit hours (handles 3+0, 0+1, 3+0/2+1 formats) → normalize → validate (code patterns, prerequisite chains, circular/orphan/forward detection, duplicate detection) → review queue with confidence scoring → transactional insert per program.

**Auto Class Generation & LMS Integration** — Auto-generate classes at semester start from course offerings and enrollment data. Distribute students across sections (alphabetical/random/balanced/manual). Assign faculty by configurable rules. Provision in LMS platforms simultaneously: Google Classroom (Workspace Admin SDK), Microsoft Teams (Graph Education API), Moodle (Web Services API with coexistence modes: provision_only, bidirectional sync, readonly import, SSO only). Custom LMS adapter via HTTP endpoints. Directory sync (Google Workspace, Azure AD, LDAP, local). Roster sync with conflict resolution.

**Data Import & Migration** — Connect to any external database (PostgreSQL, MySQL, MSSQL, Oracle, SQLite). Browse tables and columns from legacy systems. Map source columns → AegisCore fields. Batch import (100 rows per transaction) with per-row error logging. 15 CSV format templates shipped in Docker image — downloadable from the portal. Built-in mapping templates for PeopleSoft → Students, Moodle → Courses, QuickBooks → Finance.

**Nocturne Chart System** — Omniscient hover tooltips (value + context + meaning, not just the number). Native zoom (scroll/pinch/drag-select). Insight chips computed from live data — never hardcoded. Accessible: aria-labels, keyboard navigation, screen-reader data table toggle, high contrast, prefers-reduced-motion. SVG circular gauge for readiness scores. Calendar heatmap for workload distribution. Zero hardcoded colors — everything from the design token system.

**Deployment Flexibility** — Same Docker image, three modes (env-driven): LOCAL (everything in Docker on one machine), CLOUD (managed Postgres/Redis + app containers), HYBRID (on-prem primary + cloud replica). Self-hosted secrets via local AES-256-GCM encrypted vault for clients with no vault infrastructure. In-wizard secret generation — zero terminal needed. Nginx reverse proxy + Let's Encrypt SSL in 3 commands. Universal deploy script: `./scripts/deploy.sh local|cloud|hybrid`.

---

## Who Uses What

| Persona | What They Do |
|---|---|
| **Student** | View grades, pay fees via RAAST, check attendance, see timetable, submit grade appeals, use SafeSpace (anonymous mental health), ask NidaBot questions in Urdu, see achievement badges, vote in elections |
| **Faculty** | Take attendance (offline works), enter grades with deadline tracking, respond to grade appeals, view workload heatmap, evaluate interns, review visiting faculty performance |
| **Finance Officer** | Record payments, issue receipts, run collection reports, view defaulter predictions with intervention strategy, manage procurement, process payroll |
| **Registrar** | Manage enrollment, process grade appeals, run convocation pipeline, handle disciplinary cases, set academic calendar, manage foreign student compliance |
| **HOD / Dean** | View department budget, approve faculty claims, see grade distributions with anomaly alerts, review curriculum proposals, manage visiting faculty slots |
| **VC / Rector** | Executive dashboard (academic, financial, accreditation, student success, faculty quality scores), strategic plan monitoring, escalated alerts, benchmarking |
| **Chancellor / Board** | Board meeting packs, institutional health score, strategic goals with progress bars, alerts feed (Financial/Academic/HR/Compliance), benchmark comparisons — NO individual data |
| **IT Admin** | Configure replication (1-10 replicas, auto-scaling, lag thresholds), set sync-lag correction windows, manage secrets providers, run backups, monitor health, deploy updates |
| **Counselor** | Severity-sorted mental health ticket queue, anonymous case notes, wellness check-in scheduling, mandatory report triggers |
| **Parent** | Check if child boarded bus (GPS tracking), view fee payment status, receive achievement notifications, emergency alerts |
| **Visitor** | Pre-register, QR gate entry, host notification |
| **International Student** | Visa expiry alerts (90/60/30/14/7 days), MORA compliance, enrollment letter generation, renewal workflow |
| **Accreditation Reviewer** | Read-only audit mode, time-limited access, evidence compilation, real-time natural language query, all data viewable with zero edit capability |

---

## Technical Summary

| Dimension | Specification |
|---|---|
| Backend | .NET 10 (C# 14), ASP.NET Core, Entity Framework Core 10 |
| Frontend | Next.js 16 (React 19), TypeScript, Recharts, next-intl |
| Database | PostgreSQL 17 with pgvector extension |
| Cache/Queue | Redis 7 |
| Auth | JWT Bearer + Casbin ABAC + RBAC hybrid, Argon2 hashing |
| Encryption | ML-DSA post-quantum signatures, AES-256-GCM, TLS 1.3 |
| Deployment | Docker (Alpine multi-stage, non-root, <300MB image), K8s manifests |
| Locales | en-PK, en-US, en-GB, ar-AE, ms-MY with RTL support |
| Test Coverage | 508 unit/integration tests, 0 skipped |
| Build | dotnet 0 errors, tsc 0 errors |

---

*Nocturne Systems (Pvt.) Ltd. — All Rights Reserved*
