# MASTER ORCHESTRATION PROMPT FOR CODEX (GPT-5.5)
## AegisCore Full Build Pipeline

---

## INSTRUCTION SET FOR CODEX

You are orchestrating the complete build of **AegisCore Systems** — a multi-tenant university ERP for Pakistani universities.

**Your Role:** Follow the build order below EXACTLY. For each step, read the referenced prompt file, execute it with the provided codebase/context, and output production-ready code + architecture.

**Your Tools:**
- Read markdown files (I will provide paths)
- Generate C# .NET 10 code
- Generate React/React Native code
- Generate deployment configs (Docker, Kubernetes)
- Generate database schemas (PostgreSQL)
- Reference previous outputs (maintain continuity)

**Your Output Standard:**
- Production-ready code (no TODOs, no placeholders)
- Full implementation (not pseudocode)
- Error handling + logging
- Security hardening (AES-256, no hardcoded secrets)
- Unit test stubs

---

## BUILD ORDER (26 STEPS - EXECUTE IN THIS SEQUENCE)

### PHASE 1: FOUNDATION (Weeks 1-3)

#### STEP 1: U1 — Codebase Understanding
**File:** `aegiscore-prompts/utility/U1-codebase-understanding.md`

**Action:**
```
1. Read U1 prompt
2. I will provide my current codebase structure
3. You audit it and identify:
   - Current architecture
   - Tech stack (confirm: C# 14, .NET 10, PostgreSQL, Redis, React Native)
   - Existing RBAC patterns
   - Database schema current state
   - Docker setup current state
4. Output:
   - Codebase audit report (1 page)
   - Architecture diagram (ASCII)
   - Gaps vs AegisCore architecture
   - Cleanup tasks for next step
```

**Checkpoint:** ✅ Confirm codebase is ready for S1

---

#### STEP 2: S1 — Foundation
**File:** `aegiscore-prompts/sprints/S1-foundation.md`

**Action:**
```
1. Read S1 prompt
2. Reference U1 audit output
3. Build:
   - IStorageProvider interface (abstraction)
   - IEncryptionService (AES-256)
   - RBAC matrix (6 levels: Student, Faculty, HOD, VC, Admin, SuperAdmin)
   - Immutable audit log entity + EF migrations
   - Academic calendar module
   - API gateway + middleware
   - Health check endpoint
4. Output:
   - C# entities (20+ classes)
   - EF Core DbContext
   - PostgreSQL migrations (V1.0)
   - Unit tests (50+ tests)
   - Docker Compose (local dev setup)
   - README for S1 only
```

**Checkpoint:** ✅ Confirm S1 builds without errors, all tests pass

---

#### STEP 3: U6 — License Engine
**File:** `aegiscore-prompts/utility/U6-license-engine.md`

**Action:**
```
1. Read U6 prompt
2. Reference S1 output
3. Build:
   - License verification service (daily 08:00 AM heartbeat)
   - License lockout mechanism (2 failed heartbeats = system lockout)
   - SuperAdmin override endpoint (/api/admin/license/override)
   - Module gating based on license attributes
   - License configuration UI
4. Output:
   - C# license service (LicenseVerificationService.cs)
   - Background job (Hangfire integration)
   - Database schema additions (license_config table)
   - API endpoints (3 endpoints)
   - Integration tests (10+ tests)
```

**Checkpoint:** ✅ Confirm license engine integrates into S1

---

### PHASE 2: CORE ACADEMIC (Weeks 4-12)

#### STEP 4: S2 — Admissions
**File:** `aegiscore-prompts/sprints/S2-admissions.md`

**Action:**
```
1. Read S2 prompt
2. Reference S1 output
3. Build:
   - Enquiry CRM (lead tracking)
   - Application form (multi-step)
   - Document verification workflow
   - SHAP merit scoring (explainable AI)
   - Quota allocation engine
   - Seat confirmation
   - Payment gateway (JazzCash + EasyPaisa)
   - Offer letter generation
4. Output:
   - C# entities (15+ classes)
   - EF migrations
   - React portals (Student app + Admin dashboard)
   - Unit tests (40+ tests)
   - Integration tests (database)
   - Dockerfile with S2 included
```

**Checkpoint:** ✅ Confirm S2 merges cleanly with S1, payment gateway authenticated

---

#### STEP 5: S3 — Examination
**File:** `aegiscore-prompts/sprints/S3-examination.md`

**Action:**
```
1. Read S3 prompt
2. Reference S1 + S2 output
3. Build:
   - Assessment scheme builder (problem config)
   - Marks entry portal (with moderation workflow)
   - Grading engine (SGPA, CGPA, weightage calculations)
   - Grace marks approval (HOD + VC levels)
   - Hall ticket generation (QR codes)
   - Result publication + transcript generation
   - Retake management
4. Output:
   - C# entities (20+ classes)
   - EF migrations
   - React portals (Student view, Faculty marks entry, HOD approval)
   - Grading algorithms (tested)
   - QR code generation (QRCoder library)
   - Unit tests (50+ tests)
   - Dockerfile update
```

**Checkpoint:** ✅ Confirm S3 CGPA calculations match university regulations, QR codes generate correctly

---

#### STEP 6: S5 — OBE & CIS
**File:** `aegiscore-prompts/sprints/S5-obe-cis.md`

**Action:**
```
1. Read S5 prompt
2. Reference S1 + S3 output
3. Build:
   - Program Outcomes (PO) + Course Learning Outcomes (CLO)
   - CLO mapping per course
   - Attainment calculation (avg student CLO achievement)
   - CIS PDF scanner (extract CLOs from course syllabus)
   - HEC HEMIS export (XML format)
   - Accreditation dashboard
4. Output:
   - C# OBE models (10+ entities)
   - PDF parsing service (iTextSharp)
   - CLO extraction AI (Claude API integration)
   - HEMIS export module
   - React OBE dashboard
   - Unit tests (30+ tests)
   - Dockerfile update
```

**Checkpoint:** ✅ Confirm CLO extraction works, HEMIS export is valid XML

---

### PHASE 3: OPERATIONS (Weeks 13-18)

#### STEP 7: S4 — HR & Payroll
**File:** `aegiscore-prompts/sprints/S4-hr.md`

**Action:**
```
1. Read S4 prompt
2. Reference S1 output
3. Build:
   - HR master (employee register)
   - Recruitment pipeline (job posting → application → interview → offer)
   - Leave management (apply → HOD approval → finance deduction)
   - Pakistan payroll (EOBI 5%, FBR tax slabs, PF 5%)
   - Payslip generation
   - Bank export (for automated transfers)
   - Gratuity calculation (based on years of service)
4. Output:
   - C# HR entities (25+ classes)
   - Payroll calculation engine (with tax tables)
   - Leave workflow (state machine)
   - React HR portal (self-service)
   - Bank export formatter
   - Unit tests (60+ tests, especially payroll edge cases)
   - Dockerfile update
```

**Checkpoint:** ✅ Confirm payroll calculations match Pakistan regulations (EOBI, FBR slabs), gratuity correct

---

#### STEP 8: S7 — Finance
**File:** `aegiscore-prompts/sprints/S7-finance.md`

**Action:**
```
1. Read S7 prompt
2. Reference S1 + S2 + S4 output
3. Build:
   - Fee structure (tuition + components + scholarships)
   - General ledger (chart of accounts)
   - Journal entries (automated from transactions)
   - Cost centers (per department)
   - Procurement workflow (requisition → PO → GRN → invoice)
   - Vendor management + rating
   - PIFRA export (regulatory compliance)
4. Output:
   - C# finance entities (30+ classes)
   - GL journal entry engine
   - Double-entry booking logic
   - Procurement state machine
   - PIFRA export module (XML)
   - React finance dashboards (CFO view)
   - Unit tests (70+ tests)
   - Dockerfile update
```

**Checkpoint:** ✅ Confirm GL balances, PIFRA export valid

---

#### STEP 9: S9 — Communications
**File:** `aegiscore-prompts/sprints/S9-communications.md`

**Action:**
```
1. Read S9 prompt
2. Reference S1 output
3. Build:
   - WhatsApp Business API integration (5 templates)
   - Email service (SMTP, templating)
   - SMS gateway (fallback)
   - In-app notifications (SignalR real-time)
   - Notification preferences (per user)
   - Grievance ticketing system
   - Notification audit trail
4. Output:
   - C# notification service (5+ classes)
   - WhatsApp template definitions (JSON)
   - SignalR hub + clients
   - Grievance ticket entity + workflow
   - Email templates (Liquid.NET)
   - Unit tests (40+ tests)
   - Docker Compose with Redis (for SignalR backplane)
```

**Checkpoint:** ✅ Confirm WhatsApp templates approved by Meta, SignalR hub connects

---

### PHASE 4: INTERFACES (Weeks 19-23)

#### STEP 10: S8 — Portals
**File:** `aegiscore-prompts/sprints/S8-portals.md`

**Action:**
```
1. Read S8 prompt
2. Reference S1 + S2 + S3 + S4 output
3. Build:
   - Student portal (10 screens):
     ├─ Dashboard
     ├─ Attendance view
     ├─ Grades view
     ├─ Fee view + pay button
     ├─ Course registration
     ├─ Transcript download
     ├─ Timetable
     ├─ Notices
     ├─ Support ticket
     └─ Profile
   - Faculty portal (11 screens):
     ├─ Dashboard
     ├─ Marks entry
     ├─ Class attendance
     ├─ Assessment submission
     ├─ Student feedback
     ├─ Leave apply
     ├─ Timetable
     ├─ Course material upload
     ├─ Grade moderation
     ├─ Performance metrics
     └─ Profile
   - SignalR real-time updates
   - WCAG 2.1 AA accessibility
4. Output:
   - React components (40+ components)
   - Redux state management
   - API integration layer
   - Cypress E2E tests (20+ tests)
   - Accessibility audit report
   - Dark mode support
   - Mobile responsive CSS
   - Storybook stories
```

**Checkpoint:** ✅ Confirm all 21 screens render, WCAG audit passes, accessibility report < 5 issues

---

#### STEP 11: S6 — Microsoft Ecosystem
**File:** `aegiscore-prompts/sprints/S6-microsoft-ecosystem.md`

**Action:**
```
1. Read S6 prompt
2. Reference S1 + S8 output
3. Build:
   - Teams auto-provisioning (Graph API)
     ├─ Auto-create team per course
     ├─ Add faculty as owners
     ├─ Add students as members
   - Moodle LTI 1.3 integration
     ├─ Grade passback to AegisCore
     ├─ Deep linking
   - Power BI embedded dashboards (5 role-specific)
     ├─ VC: University KPIs
     ├─ HOD: Department analytics
     ├─ Faculty: Class performance
     ├─ Admin: System health
     ├─ Finance: Revenue tracking
4. Output:
   - C# Graph API service
   - LTI tool provider (conforming to LTI 1.3 spec)
   - Power BI embedding service
   - React dashboard wrapper
   - Unit tests (30+ tests)
   - Integration tests (mock Teams/Graph responses)
```

**Checkpoint:** ✅ Confirm Teams provisioning works, Power BI embeds, LTI grade passback functional

---

### PHASE 5: EXTENDED MODULES (Weeks 24-28)

#### STEP 12: S10 — Campus Operations
**File:** `aegiscore-prompts/sprints/S10-campus-ops.md`

**Action:**
```
1. Read S10 prompt
2. Reference S1 + S8 output
3. Build:
   - Library OPAC (online catalog)
     ├─ Book search + filter
     ├─ Issue/return
     ├─ Fine calculation
   - Transport management
     ├─ Route registration
     ├─ GPS tracking (optional)
     ├─ Student ridership tracking
   - Visitor management
     ├─ Pre-registration form
     ├─ QR pass generation
     ├─ Gate check-in/out
4. Output:
   - C# library management entities (10+ classes)
   - Transport service
   - Visitor QR service
   - React management portals
   - Unit tests (25+ tests)
```

**Checkpoint:** ✅ Confirm library fine calculation, QR generation, visitor check-in works

---

#### STEP 13: S11 — Student Lifecycle
**File:** `aegiscore-prompts/sprints/S11-student-lifecycle.md`

**Action:**
```
1. Read S11 prompt
2. Reference S1 + S2 + S3 output
3. Build:
   - Placement drive management
     ├─ Company registration
     ├─ Drive scheduling
     ├─ Student signup
     ├─ Offer tracking
   - Alumni management
     ├─ Alumni profile
     ├─ Job board
     ├─ Donation tracking
   - Student societies
     ├─ Society registration
     ├─ Activity logging
     ├─ Event management
   - Student health records (secure, encrypted)
4. Output:
   - C# entities (15+ classes)
   - Placement workflow
   - Alumni portal
   - React management dashboards
   - Unit tests (20+ tests)
```

**Checkpoint:** ✅ Confirm placement drive tracking, alumni job postings visible

---

#### STEP 14: S12 — Asset & Inventory + HEC
**File:** `aegiscore-prompts/sprints/S12-asset-inventory-hec.md`

**Action:**
```
1. Read S12 prompt
2. Reference S1 + S5 output
3. Build:
   - Asset register
     ├─ Asset tracking
     ├─ Depreciation calculation (straight-line)
     ├─ Disposal tracking
   - Inventory management
     ├─ Stock in/out
     ├─ Reorder level alerts
     ├─ Physical count reconciliation
   - HEC HEMIS export
     ├─ One-click export to HEC
     ├─ Asset data
     ├─ OBE data (from S5)
     ├─ Accreditation evidence
4. Output:
   - C# asset entities (12+ classes)
   - Depreciation engine
   - Inventory state machine
   - HEMIS export builder (XML generation)
   - React inventory dashboard
   - Unit tests (30+ tests)
```

**Checkpoint:** ✅ Confirm depreciation calculations, HEMIS export valid, HEC accepts it

---

### PHASE 6: MOBILE APPS (Weeks 29-34)

#### STEP 15: S13 — Mobile Apps
**File:** `aegiscore-prompts/sprints/S13-mobile-apps.md`

**Action:**
```
1. Read S13 prompt
2. Reference S1 + S8 output + U13 (reference only)
3. Build React Native (Expo) apps:
   - Student app (9 screens)
     ├─ Login
     ├─ Dashboard
     ├─ Attendance
     ├─ Grades
     ├─ Schedule
     ├─ Fees + payment
     ├─ Notifications
     ├─ Profile
     └─ Support
   - Faculty app (9 screens)
     ├─ Login
     ├─ Dashboard
     ├─ Marks entry (offline support)
     ├─ Attendance marking (offline first)
     ├─ Messages
     ├─ Grades view
     ├─ Schedule
     ├─ Profile
     └─ Support
   - Offline-first sync (WatermelonDB)
   - Biometric auth (react-native-biometrics)
   - FCM + APN push notifications
   - 30-minute offline attendance buffer
4. Output:
   - Expo managed workflow
   - 18 screens (React Native components)
   - WatermelonDB schema + sync logic
   - FCM/APN integration
   - Biometric auth service
   - E2E tests (Detox framework)
   - App.json (Expo config)
   - eas.json (EAS Build config for CI/CD)
```

**Checkpoint:** ✅ Confirm app builds in Expo, offline sync works, push notifications send

---

### PHASE 7: GOVERNANCE & ADVANCED FEATURES

#### STEP 16: U10 — Governance, Overrides & Feedback
**File:** `aegiscore-prompts/utility/U10-governance-overrides-feedback.md`

**Action:**
```
1. Read U10 prompt
2. Reference S1 + S3 + S7 output
3. Build:
   - Replication lag configuration (1-72 hours, default 12)
   - Grade override workflow
     ├─ HOD override (±5%)
     ├─ HOD + VC override (±15%)
     ├─ VC + Chancellor override (>15%)
   - Anonymous student feedback system
     ├─ Zero traceability (no student_id stored)
     ├─ AES-256 encrypted
     ├─ Aggregated stats only (avg, sentiment %, themes)
   - Section override capability
     ├─ Capacity checks (regular + exam)
     ├─ Automatic fee adjustment
   - Incomplete course re-enrollment
     ├─ Auto re-enroll next semester
     ├─ Auto fee removal for that course
   - Dynamic fee calculation
     ├─ SQL GENERATED columns
     ├─ Real-time recalculation on credit hour changes
4. Output:
   - C# override approval workflow (5+ classes)
   - Anonymous feedback service (encryption key management)
   - Fee calculation update (SQL migration)
   - React approval dashboard
   - Unit tests (40+ tests, especially edge cases)
   - Integration into S3 + S7
```

**Checkpoint:** ✅ Confirm overrides respect hierarchy, feedback is truly anonymous, fee recalc is instant

---

#### STEP 17: U11 — Course Trees, Workload & Offline
**File:** `aegiscore-prompts/utility/U11-course-trees-workload-offline.md`

**Action:**
```
1. Read U11 prompt
2. Reference S1 + S8 output
3. Build:
   - Visual curriculum editor
     ├─ vis-network graph (node-link diagram)
     ├─ Drag-drop course nodes
     ├─ Color-coded semesters
     ├─ Prerequisite arrows
   - Curriculum versioning
     ├─ Draft → Approved → Active
     ├─ Change history
     ├─ Approval workflow
   - CLO mapping per course
   - Teacher workload management
     ├─ Max hours per semester (e.g., 12 hours)
     ├─ Max sections per semester (e.g., 4 sections)
     ├─ Assignment blocking if exceeded
   - Offline attendance marking
     ├─ 30-minute IndexedDB buffer (web)
     ├─ Service Worker + sync
     ├─ AsyncStorage fallback (React Native)
     ├─ Duplicate prevention on sync
     ├─ wasOfflineAt metadata
4. Output:
   - C# curriculum entities (12+ classes)
   - EF migrations
   - workload validation service
   - React curriculum editor (vis-network integration)
   - Service Worker code (JavaScript)
   - Offline attendance sync logic
   - Unit tests (50+ tests, especially sync edge cases)
   - Integration into S1 + S8
```

**Checkpoint:** ✅ Confirm curriculum versioning works, workload assignment blocked when exceeded, offline sync handles duplicates

---

#### STEP 18: U14 — Multi-Tenant SSO, 2FA & Agentic HITL Exams
**File:** `aegiscore-prompts/utility/U14-multitenant-sso-hitl-exams.md`

**Action:**
```
1. Read U14 prompt
2. Reference S1 + S3 output
3. Build:
   - Multi-tenant SSO
     ├─ Microsoft Entra ID (Office 365)
     ├─ Google Workspace
     ├─ Okta
     ├─ LDAP
     ├─ AegisCore native auth fallback
     ├─ Auto-detect via email domain
   - Per-university branding
     ├─ Logo per university
     ├─ Colors per university
     ├─ Dynamic login page
   - Mandatory 2FA on first login
     ├─ 16-char complex temporary password
     ├─ TOTP setup (Google Authenticator / Microsoft Authenticator)
     ├─ QR code generation
     ├─ Backup codes (10 codes for recovery)
     ├─ Password change enforcement
   - Agentic exam paper grading pipeline
     ├─ Physical paper steps:
     │  ├─ Remove fasteners
     │  ├─ Batch into groups (50 papers per batch)
     │  ├─ Embed QR codes (per student)
     │  ├─ High-speed duplex scanning (ADF)
     │  ├─ Scanner audit trail (stamp back of pages)
     │  ├─ Auto-split by QR code (PDF per student)
     │  ├─ Page count reconciliation
     │  ├─ Blur + blank detection (computer vision)
     │  ├─ OCR extraction (Azure Computer Vision)
     │  ├─ Destruction manifest generation
     │  ├─ Paper incineration + logging
     ├─ Digital paper steps:
     │  ├─ Agentic grading (specialized AI agents per problem)
     │  ├─ Confidence scoring (0.0-1.0)
     │  ├─ Human review queue (flag if confidence < 0.75)
     │  ├─ Faculty review dashboard
     │  ├─ Approve/adjust/reject workflow
     │  └─ Final grade locked
4. Output:
   - C# SSO controller + gateway (20+ methods)
   - 2FA service (TOTP + backup codes)
   - Agentic grading orchestrator
   - Paper scanning pipeline (QR detection, OCR integration)
   - Destruction manifest + audit trail logging
   - Human review React dashboard
   - Database schema (20+ tables)
   - Unit tests (80+ tests)
   - Integration into S1 (auth) + S3 (exam grading)
```

**Checkpoint:** ✅ Confirm SSO works for all 5 providers, 2FA enforced, agentic grading produces consistent grades, human review UI functional

---

### PHASE 8: NATIVE MOBILE (OPTIONAL BUT RECOMMENDED)

#### STEP 19: U13 — Native Mobile & Docker Integration
**File:** `aegiscore-prompts/utility/U13-native-mobile-docker-integration.md`

**Action:**
```
1. Read U13 prompt
2. Reference S1 output (API contract)
3. Build NATIVE Android app (Kotlin):
   - Project setup (Android Studio)
   - MVVM architecture
   - Retrofit HTTP client
   - Room offline database
   - Biometric auth (BiometricPrompt)
   - FCM push notifications
   - 9 screens (same as S13 React Native version)
   - Gradle build config
4. Build NATIVE iOS app (Swift):
   - Project setup (Xcode)
   - SwiftUI views
   - URLSession HTTP client
   - Core Data offline cache
   - Keychain secure storage
   - BiometricKit (Face ID / Touch ID)
   - APNs push notifications
   - 9 screens (same as S13 React Native version)
   - Build settings
5. Docker integration explanation:
   - Clarify: Docker runs on backend only
   - Mobile apps = native binaries on phone
   - HTTP API calls to Docker-hosted backend
   - No direct Docker-mobile connection
6. Output:
   - Kotlin Android project (complete, runnable)
   - Swift iOS project (complete, runnable)
   - Android Studio build.gradle.kts
   - Xcode project config
   - Architecture diagram (phone ↔️ HTTP ↔️ Kubernetes/Docker)
   - Build + deployment instructions (App Store + Play Store)
```

**Checkpoint:** ✅ Confirm Kotlin APK builds, Swift IPA builds, both sync with backend API correctly

---

### PHASE 9: MAINTENANCE & UTILITIES (USE BETWEEN SPRINTS)

#### STEP 20: U2 — Patch (Use After Each Sprint)
**File:** `aegiscore-prompts/utility/U2-patch.md`

**Action:**
```
After each sprint, review output for bugs:
1. Read U2 prompt
2. Identify critical bugs from sprint output
3. Generate minimal surgical fix
4. Include regression tests
5. Verify fix doesn't break other modules
```

---

#### STEP 21: U3 — Cleanup (Use After Every 2 Sprints)
**File:** `aegiscore-prompts/utility/U3-cleanup.md`

**Action:**
```
After every 2 sprints, clean up codebase:
1. Read U3 prompt
2. Remove dead code, TODOs, hardcoded values
3. Improve logging + error handling
4. Refactor duplicated logic
5. Update documentation
```

---

#### STEP 22: U4 — Docker Build Validator (Use Before EVERY Deployment)
**File:** `aegiscore-prompts/utility/U4-docker-build-validator.md`

**Action:**
```
Before deploying any sprint:
1. Read U4 prompt
2. Test Docker build locally
3. Test container startup
4. Test health check endpoint (/health)
5. Verify no secrets in image
6. Verify cloud-agnosticism (no hardcoded regions)
```

---

### PHASE 10: INFRASTRUCTURE & DEPLOYMENT

#### STEP 23: U7 — Cloud Agnosticism Verification
**File:** `aegiscore-prompts/utility/U7-cloud-agnosticism.md`

**Action:**
```
After S1 complete, verify cloud-agnosticism:
1. Read U7 prompt
2. Audit all storage references → Use IStorageProvider
3. Audit all secret references → Use ISecretProvider
4. Audit all encryption → Use IEncryptionService
5. Test: Local disk backend works
6. Test: Azure Blob backend works
7. Test: S3 backend works
8. Test: SFTP backend works
9. Output:
   - Cloud-agnosticism audit report
   - Deployment sign-off checklist
   - University pitch deck (addressing cloud concerns)
```

**Checkpoint:** ✅ Confirm multiple storage backends work, no cloud vendor lock-in

---

#### STEP 24: U12 — Deployment & Distribution
**File:** `aegiscore-prompts/utility/U12-deployment-distribution.md`

**Action:**
```
When all sprints complete (S13 mobile done), deploy:

1. iOS App Store Submission
   ├─ Apple Developer account creation ($99/year)
   ├─ Code signing certificates
   ├─ Screenshot preparation (6 screens)
   ├─ Privacy policy + terms
   ├─ Demo account for reviewer
   └─ Submit via App Store Connect (24-48hr review)

2. Google Play Store Submission
   ├─ Google Play developer account ($25 one-time)
   ├─ Build Android App Bundle (AAB)
   ├─ Content rating questionnaire
   ├─ Screenshots (6 screens)
   ├─ Privacy policy + terms
   └─ Submit for review (2-4hr review)

3. Web Deployment (Portal)
   ├─ Deploy to Vercel or Cloudflare Pages
   ├─ Custom domain: aegiscore.university.edu.pk
   ├─ SSL/TLS via Cloudflare
   ├─ CDN caching enabled

4. Backend Deployment (Choose One)
   
   OPTION A: Azure Kubernetes Service (Cloud)
   ├─ az aks create --location southeastasia
   ├─ Azure Container Registry for images
   ├─ kubectl apply -f k8s/manifests/
   ├─ HPA autoscaling (3-10 pods)
   ├─ Azure Database for PostgreSQL
   └─ Azure Cache for Redis
   
   OPTION B: Bare-Metal On-Prem Kubernetes
   ├─ Install kubeadm on 3 servers
   ├─ kubectl init --pod-network-cidr=10.244.0.0/16
   ├─ Install Flannel networking
   ├─ kubectl apply -f k8s/manifests/
   ├─ MinIO for object storage (S3-compatible)
   ├─ Velero for backup/restore
   └─ Nginx ingress controller

5. CI/CD Pipeline (GitHub Actions)
   ├─ On push to main:
   ├─ Run all tests
   ├─ Build Docker image
   ├─ Push to registry
   ├─ Deploy to AKS (if chosen)
   ├─ Submit to App Store + Play Store
   └─ Verify deployment (smoke tests)

6. Output:
   - Kubernetes YAML manifests (10+ files)
   - Docker image building config
   - GitHub Actions workflow
   - Deployment checklist
   - Runbooks (how to deploy, rollback, scale)
```

**Checkpoint:** ✅ Confirm app live on App Store + Play Store, backend pods healthy, CI/CD passes

---

### PHASE 11: ECOSYSTEM & GO-TO-MARKET

#### STEP 25: U8 — Competitive Analysis
**File:** `aegiscore-prompts/utility/U8-competitive-analysis.md`

**Action:**
```
After deployment, market positioning:
1. Read U8 prompt
2. Analyze competitive landscape
3. Identify AegisCore's unique advantages:
   - SHAP-explainable merit (no competitor has this)
   - OBE + CIS scanner (no competitor has this)
   - 100% cloud-agnostic (EduSuite/Futuresol are cloud-only)
   - Offline-first mobile (Pakistan connectivity reality)
   - Plugin ecosystem with revenue sharing
   - Pakistan-native payroll + compliance
4. Output:
   - Competitive analysis report
   - Positioning memo
   - Pitch deck (15 slides)
   - Go-to-market strategy
   - University outreach list (top 50 Pakistani universities)
```

---

#### STEP 26: U9 — Community Plugins
**File:** `aegiscore-prompts/utility/U9-community-plugins.md`

**Action:**
```
To build ecosystem:
1. Read U9 prompt
2. Build plugin SDK + CLI
3. Build developer dashboard
   ├─ Plugin upload
   ├─ Version management
   ├─ Review queue
   ├─ Earnings dashboard (30% AegisCore, 70% developer)
4. Build plugin marketplace
   ├─ Discover plugins
   ├─ Install/uninstall
   ├─ Revenue sharing
5. Output:
   - Plugin SDK (NuGet package)
   - CLI tool (dotnet aegiscore-plugin)
   - Developer portal (React)
   - Revenue tracking service
   - Sample plugin (reference implementation)
```

---

## MASTER CHECKLIST FOR CODEX

Track these checkpoints as you build:

- [ ] STEP 1: U1 audit complete, codebase gaps identified
- [ ] STEP 2: S1 builds, all tests pass, Docker runs locally
- [ ] STEP 3: U6 license engine integrated, heartbeat works
- [ ] STEP 4: S2 admissions complete, payment gateway authenticated
- [ ] STEP 5: S3 exams complete, CGPA calculations verified
- [ ] STEP 6: S5 OBE complete, HEMIS export valid
- [ ] STEP 7: S4 payroll complete, taxes calculated correctly
- [ ] STEP 8: S7 finance complete, GL balances, PIFRA export valid
- [ ] STEP 9: S9 communications complete, WhatsApp API approved
- [ ] STEP 10: S8 portals complete, 21 screens render, WCAG AA pass
- [ ] STEP 11: S6 Microsoft complete, Teams provision, Power BI embeds
- [ ] STEP 12: S10 campus ops complete, library/transport/visitor working
- [ ] STEP 13: S11 student lifecycle complete, placement tracking works
- [ ] STEP 14: S12 asset/inventory complete, HEC export valid
- [ ] STEP 15: S13 mobile apps complete, offline sync works, push notifications send
- [ ] STEP 16: U10 governance complete, overrides respect hierarchy, feedback anonymous
- [ ] STEP 17: U11 curriculum complete, workload blocking works, offline attendance syncs
- [ ] STEP 18: U14 SSO complete, 2FA enforced, agentic grading functional
- [ ] STEP 19: U13 native mobile complete, Kotlin + Swift apps build
- [ ] STEP 20: U2 patch applied, critical bugs fixed
- [ ] STEP 21: U3 cleanup applied, code quality improved
- [ ] STEP 22: U4 Docker validation passed before every deployment
- [ ] STEP 23: U7 cloud-agnosticism verified, multiple backends work
- [ ] STEP 24: U12 deployed to App Store + Play Store + Kubernetes
- [ ] STEP 25: U8 competitive analysis completed
- [ ] STEP 26: U9 plugin ecosystem launched

---

## OUTPUT STANDARDS FOR CODEX

For EVERY step, deliver:

1. **Production Code**
   - No TODOs, no placeholders
   - Error handling + logging
   - Security hardened (AES-256, no hardcoded secrets)
   - Unit tests (60-80% coverage minimum)

2. **Database Schema**
   - EF Core entities
   - Migrations (numbered: V1.0, V1.1, etc.)
   - Indexes on foreign keys + search fields
   - Constraints enforced

3. **API Endpoints**
   - OpenAPI/Swagger spec
   - Request/response examples
   - Error codes documented

4. **Frontend Code**
   - React components with TypeScript
   - Unit tests (Jest)
   - E2E tests (Cypress)
   - Accessibility checks (axe DevTools)

5. **Documentation**
   - README per sprint
   - Architecture diagram (ASCII or Mermaid)
   - Deployment instructions
   - Troubleshooting guide

6. **Docker**
   - Dockerfile optimized (multi-stage)
   - Health checks
   - Environment variables documented
   - No secrets in image

---

## HOW TO RUN THIS PROMPT

**For Codex (GPT-5.5):**

1. Copy this entire file
2. Paste into Claude/GPT-5.5
3. Add: "My current codebase is at: [paste your GitHub repo link or code structure]"
4. Add: "I'm ready to start STEP 1. Proceed with U1."
5. Codex reads U1, audits your code, outputs audit report
6. Then you say: "Great. STEP 2: Proceed with S1-foundation.md"
7. Codex reads S1, outputs all S1 code + schema + tests
8. Continue through all 26 steps
9. After STEP 15 (S13 mobile complete), give to **Terra Ultra (GPT-5.6)** for final review + quality assurance

---

## FINAL NOTE

This is enterprise-grade software engineering. Every output must be production-ready. No shortcuts. No "future optimization." Ship quality or don't ship.

**You've got this. Let's build AegisCore. 🚀**
