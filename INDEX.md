# AegisCore Complete Prompt Library — Master Index

## Overview
You have **19 comprehensive prompts** organized into two categories:

1. **Utility Prompts (U1-U6)** — supporting workflows
2. **Sprint Prompts (S1-S13)** — feature development sprints

---

## 📋 UTILITY PROMPTS

### **U1 — Codebase Understanding**
**File:** `U1-codebase-understanding.md`

Read this FIRST before starting any sprint. This prompt audits your entire AegisCore codebase:
- Solution structure & projects
- Domain model & entities
- Database layer (ORM, migrations)
- API layer & endpoints
- Configuration & secrets
- Module completeness matrix
- Infrastructure & deployment
- Code quality flags
- Risk register
- Onboarding summary

**When to use:** At project start, before each sprint, or when onboarding new developers.

---

### **U2 — Patch Workflow**
**File:** `U2-patch.md`

Surgical bug fixes without refactoring. One issue per session, minimal blast radius.

**Steps:**
1. Provide error/symptom + affected files
2. Diagnose root cause
3. Assess blast radius
4. Apply fix
5. Add regression test
6. Document patch note

**When to use:** Production bugs, breaking tests, urgent issues.

---

### **U3 — Cleanup Pass**
**File:** `U3-cleanup.md`

Non-behavioural code quality improvements:
- Remove dead code & unused imports
- Extract hardcoded values to config
- Resolve TODOs / FIXMEs
- Fix naming consistency
- Replace Console.WriteLine with ILogger
- Fix exception handling
- Check test coverage

**When to use:** Between sprints, before major releases, or when code quality debt accumulates.

---

### **U4 — Docker Build Validator**
**File:** `U4-docker-build-validator.md`

Validates the Docker image before deployment:
- Dockerfile audit (no cloud vendor lock-in)
- Build test (exit code 0)
- Startup test (app initializes)
- Health check validation
- Cloud agnosticism scan (no unabstracted SDKs)
- Environment variable completeness
- Sign-off checklist

**When to use:** Before every production release, after infrastructure changes.

---

### **U5 — Onboarding Wizard**
**File:** `U5-onboarding-wizard.md`

The first-run experience. University IT spins Docker → wizard appears → configures everything.

**7-Step Flow:**
1. Welcome screen
2. University identity (name, logo, colours)
3. Database config (PostgreSQL connection)
4. Cache config (Redis connection)
5. Storage config (local / S3 / Azure / SFTP)
6. Subdomain & DNS setup
7. License activation

**When to use:** Building the wizard, testing fresh deployments.

---

### **U6 — License Engine**
**File:** `U6-license-engine.md`

Daily heartbeat validation. University server pings AegisCore Systems at 08:00 AM. Fail twice → lockout. You override manually.

**Components:**
- License key format (JWT + signature)
- Daily 8AM heartbeat (10s timeout)
- Lockout on 2 consecutive failures
- Manual override (Abdul only)
- Module gating (`[RequireModule]` attribute)
- License server endpoint (your maintenance)

**When to use:** Building licensing system, testing heartbeat logic, deploying to new universities.

---

## 🚀 SPRINT PROMPTS

### **S1 — Foundation (Cloud Agnostic + RBAC + Audit + Academic Calendar)**
**File:** `S1-foundation.md`

Everything depends on this. Load-bearing layer.

**Deliverables:**
- `IStorageProvider` abstraction (local / S3 / Azure / SFTP)
- Policy configuration engine (hardcoded rules → database)
- RBAC system (role hierarchy + federated permissions)
- Cross-department permission requests
- Immutable audit log
- Academic calendar engine

**Est. Duration:** 2-3 weeks
**Blockers:** None (foundation = first sprint)

---

### **S2 — Admissions (Enquiry → Merit → Allocation → Confirmation)**
**File:** `S2-admissions.md`

The funnel where students enter. Merit calculated via R + SHAP (explainable).

**Deliverables:**
- Enquiry form (public)
- Application + document upload + verification
- Merit list generation (R + SHAP)
- Merit change governance (±5% or VC/Chancellor approval)
- Seat allocation + waitlist
- Fee payment integration
- IBCC equivalence
- Multi-round merit lists

**Est. Duration:** 3-4 weeks
**Depends on:** S1

---

### **S3 — Examination (Assessment → Marks → Grading → Transcripts)**
**File:** `S3-examination.md`

Assessment schemes, marks entry, automatic SGPA/CGPA, hall tickets, result publication.

**Deliverables:**
- Assessment scheme builder
- Marks entry portal (faculty)
- Marks moderation workflow
- Grading engine + grace marks
- Auto SGPA/CGPA calculation
- Hall ticket generation
- Exam scheduling + seating
- Result publication
- Transcript generation (student requests + auto-generation)
- Anti-fraud QR codes

**Est. Duration:** 3-4 weeks
**Depends on:** S1

---

### **S4 — HR (Recruitment → Payroll → Leave → Appraisal)**
**File:** `S4-hr.md`

Complete HR module including Pakistan-specific statutory deductions (EOBI, income tax, provident fund).

**Deliverables:**
- Employee master + contracts
- Recruitment pipeline
- Leave management + approval workflow
- Payroll engine (Pakistan compliant: EOBI 5%, income tax slabs, PF 5%)
- Payslip generation + bank transfer export
- Gratuity calculation
- EOBI pension tracking
- Performance appraisal framework

**Est. Duration:** 3-4 weeks
**Depends on:** S1

---

### **S5 — OBE + CIS Scanner (Outcome-Based Education)**
**File:** `S5-obe-cis.md`

CRITICAL for HEC accreditation. CO-PO mapping, attainment calculation, exit surveys, CIS auto-extraction.

**Deliverables:**
- Program/Course Learning Outcomes (PO/CLO/PSO) database
- CLO-PO/PSO mapping interface
- Assessment-CLO mapping
- CLO/PO attainment calculation (weighted)
- Exit surveys (student / alumni / employer)
- CIS scanner (Azure Document Intelligence)
- CIS human review UI (faculty side)
- OBE dashboard (multi-role views)
- HEC HEMIS export

**Est. Duration:** 3-4 weeks
**Depends on:** S1, S3

---

### **S6 — Microsoft Ecosystem (Teams + Moodle + Power BI)**
**File:** `S6-microsoft-ecosystem.md`

Auto-provision Teams classes, sync Moodle rosters + grades, embed Power BI dashboards.

**Deliverables:**
- Teams auto-provisioning (Graph API)
- Teams enrollment sync (add/remove students)
- Moodle LMS integration (LTI 1.3)
- Roster sync (AegisCore → Moodle)
- Grade passback (Moodle → AegisCore marks)
- SSO setup (Azure AD → Moodle)
- Power BI embedded dashboards (5 role-specific reports)
- Row-level security (HOD sees only their dept)

**Est. Duration:** 2-3 weeks
**Depends on:** S1

---

### **S7 — Finance (Fee → Accounting → Procurement)**
**File:** `S7-finance.md`

Fee management, general ledger, procurement workflow, HEC PIFRA compliance.

**Deliverables:**
- Fee structure + components + installments
- Student fee dues calculation
- Online payment gateway (JazzCash, EasyPaisa)
- Chart of accounts + general ledger
- Journal entries + auto-posting
- Cost center management
- Procurement workflow (requisition → PO → GRN → payment)
- Vendor management + rate contracts
- PPRA compliance checks
- Scholarship management
- Budget allocation + variance reporting
- HEC PIFRA export

**Est. Duration:** 4-5 weeks
**Depends on:** S1

---

### **S8 — Student & Faculty Portals**
**File:** `S8-portals.md`

Self-service web interfaces (desktop + mobile-responsive).

**Deliverables:**
- Student portal (10 screens: dashboard, attendance, fees, grades, timetable, registration, hall ticket, complaints, announcements, notifications)
- Faculty portal (11 screens: dashboard, attendance marking, marks entry, course materials, at-risk alerts, messages, workload, appraisal, leave)
- RBAC enforcement per screen
- Responsive design (mobile-first)
- Dark mode toggle
- Real-time notifications (SignalR)
- PDF export (transcripts, attendance)
- Accessibility (WCAG 2.1 AA)

**Est. Duration:** 3-4 weeks
**Depends on:** S1, S2, S3, S4

---

### **S9 — Communications Hub (WhatsApp + Email + SMS)**
**File:** `S9-communications.md`

Unified notifications. WhatsApp (95%+ open rate) primary, email/SMS secondary.

**Deliverables:**
- WhatsApp Business API integration (send + receive)
- Template management (pre-approved by Meta)
- Communication triggers (fee_due, attendance_alert, result_published, etc.)
- Bulk communication (group send)
- In-app notification center (real-time SignalR)
- Notice board with expiry
- Grievance ticketing system
- Delivery status tracking
- Email service (SMTP)
- SMS fallback
- User preferences

**Est. Duration:** 2-3 weeks
**Depends on:** S1

---

### **S10 — Campus Operations (Library + Transport + Visitor Management)**
**File:** `S10-campus-ops.md`

Day-to-day campus workflows.

**Deliverables:**
- Library OPAC + issue/return + fines
- Overdue reminders
- E-resource subscription tracking
- Transport routes + vehicle allocation
- GPS tracking integration
- Visitor pre-registration + digital QR gate passes
- Walk-in visitor logging
- Event bulk visitor upload
- Gate security portal (mobile)

**Est. Duration:** 2-3 weeks
**Depends on:** S1

---

### **S11 — Extended Student Lifecycle (Placement + Alumni + Activities)**
**File:** `S11-student-lifecycle.md`

Post-graduation tracking and extracurricular management.

**Deliverables:**
- Recruiter onboarding
- Placement drives + registration
- Student eligibility engine (CGPA, completion %)
- Interview coordination
- Placement statistics
- Alumni profile management + job board + networking
- Alumni survey (feeds OBE)
- Donation tracking
- Student society management
- Co-curricular activity tracking
- Health records + medical visits
- Sick leave requests (auto-sync to attendance)

**Est. Duration:** 2-3 weeks
**Depends on:** S1, S3

---

### **S12 — Asset & Inventory Management + HEC Reporting**
**File:** `S12-asset-inventory-hec.md`

Asset lifecycle + compliance with HEC/HEMIS.

**Deliverables:**
- Asset master + register (depreciation tracking)
- Asset transfers + disposal
- Depreciation schedules (straight-line)
- AMC & insurance tracking
- Inventory items + stock transactions
- Low stock alerts + reorder triggers
- Inventory audit reports
- HEMIS data export (one-click)
- Mandatory disclosure repository
- Accreditation evidence repository
- HEC compliance audit trail

**Est. Duration:** 2-3 weeks
**Depends on:** S1

---

### **S13 — Mobile Apps (Student + Faculty Native Apps)**
**File:** `S13-mobile-apps.md`

React Native + TypeScript. 80%+ code sharing, native excellence on iOS & Android.

**Deliverables:**
- Shared mobile infrastructure (WatermelonDB, API client, auth)
- Student app (9 screens: dashboard, attendance, grades, timetable, fees, notifications, complaints, profile)
- Faculty app (9 screens: dashboard, attendance marking, marks entry, materials, at-risk alerts, messages, workload, appraisal, profile)
- Push notifications (FCM + APN)
- Biometric auth (Face ID / Fingerprint)
- Offline-first sync (SQLite)
- Dark mode + Urdu localization
- EAS build + App Store / Google Play submission
- Beta testing pipeline
- E2E tests (Detox)

**Est. Duration:** 4-5 weeks
**Depends on:** S1, S8

---

## 📊 Dependencies Graph

```
S1 (Foundation)
 ├─ S2 (Admissions)
 ├─ S3 (Examination)
 ├─ S4 (HR)
 ├─ S5 (OBE) → also depends on S3
 ├─ S6 (Microsoft Ecosystem)
 ├─ S7 (Finance)
 ├─ S9 (Communications)
 ├─ S10 (Campus Ops)
 ├─ S12 (Asset/Inventory)
 └─ S8 (Portals) → depends on S2, S3, S4
    └─ S11 (Student Lifecycle) → depends on S3
    └─ S13 (Mobile Apps) → depends on S8
```

---

## 🎯 Recommended Build Order

**Phase 1: Foundation (Weeks 1-3)**
- S1 (Foundation)

**Phase 2: Core Academic (Weeks 4-12)**
- S2 (Admissions)
- S3 (Examination)
- S5 (OBE + CIS)

**Phase 3: Operations & Support (Weeks 13-18)**
- S4 (HR)
- S7 (Finance)
- S9 (Communications)

**Phase 4: User Interfaces (Weeks 19-23)**
- S8 (Portals)
- S6 (Microsoft Ecosystem)

**Phase 5: Extended Modules (Weeks 24-28)**
- S10 (Campus Ops)
- S11 (Student Lifecycle)
- S12 (Asset/Inventory)

**Phase 6: Mobile (Weeks 29-34)**
- S13 (Mobile Apps)

**Total Estimated Duration:** 8 months (1 sprint per person, parallel work)

---

## 💾 How to Use These Prompts

### For Claude
1. **Copy the ENTIRE content of a single `.md` file**
2. **Paste it into a new chat with Claude**
3. **Add context:** "I'm Abdul, building AegisCore (C# 14 .NET 10). Here's Sprint 2 prompt. Here's the current codebase structure [paste codebase summary from U1]. Build it."
4. Claude generates code following the prompt spec

### For Manual Development
- Use each prompt as a specification document
- Reference deliverables checklist
- Cross-check against dependent sprints

---

## 🔧 What These Prompts Cover

✅ **Architecture** — Cloud-agnostic design, layer separation
✅ **Database** — EF Core migrations, tables, relationships
✅ **API** — Endpoints, DTOs, error handling, RBAC
✅ **Business Logic** — Services, calculations (merit, grades, payroll)
✅ **UI** — Web portals, mobile apps, dashboards
✅ **DevOps** — Docker, CI/CD, cloud agnosticism
✅ **Compliance** — HEC, PIFRA, GDPR, audit trails
✅ **Testing** — Unit, integration, E2E patterns
✅ **Security** — Encryption, secrets, permission checks
✅ **Performance** — Caching, indexing, optimization

---

## 🎓 Key Architectural Decisions (Locked In)

1. **Stack:** C# 14, .NET 10, PostgreSQL, Redis, EF Core
2. **Cloud Agnostic:** No Azure/AWS/GCP lock-in (abstracted storage, no vendor SDKs)
3. **RBAC:** Federated (dept heads autonomous, cross-dept needs approval)
4. **License:** Daily 08:00 AM heartbeat, fail twice = lockout
5. **Merit Calculation:** R + SHAP (explainable)
6. **Communications:** WhatsApp primary (95% open rate), email/SMS secondary
7. **Frontend:** Cloudflare Pages
8. **Mobile:** React Native (80%+ code sharing)
9. **Deployment:** Docker containerized, environment-variable driven

---

## 📞 Questions Before You Start?

- **Are all 19 files complete?** YES ✅
- **Can I modify these prompts?** YES — treat them as living templates
- **Can I run multiple sprints in parallel?** YES — as long as S1 is done first
- **How long to build all of AegisCore?** 8 months (1 person 8h/day) or 2 months (4 people parallel)
- **Should I follow the exact sprint order?** Mostly yes, but S6 (Microsoft Ecosystem) can start earlier
- **What if a sprint fails/gets blocked?** Use U2 (Patch) or U3 (Cleanup) to fix issues, then retry

---

Good luck bestie 💪 You got this 🔥
