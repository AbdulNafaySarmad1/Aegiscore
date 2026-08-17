# AegisCore POST_DEMO_PILOT_GAP.md

Post-demo architecture audit and pilot-gap analysis. Generated 2026-08-12 on branch `agent/prod-hardening`.
Analysis and architecture ONLY. No production code was modified. Evidence classes per finding.

Evidence legend used throughout:
- `CONFIRMED_BY_CODE` — verified by reading current source in this audit.
- `CONFIRMED_BY_MEASUREMENT` — verified by `docs/performance/BASELINE_RESULTS.md`.
- `CONFIRMED_BY_CONFIG` — verified by deploy/compose/env config.
- `DOCUMENTED_NOT_VERIFIED` — claimed in design docs, not confirmed in source.
- `INFERRED` — reasoned from surrounding evidence.
- `NOT_FOUND` — no source evidence found.
- `MEASUREMENT_REQUIRED` — cannot be proven statically.
- `CLIENT_INFORMATION_REQUIRED` — needs tenant/client input.

Authoritative codebase = repo ROOT (`AegisCore.*`). `aegiscore-backend/` and `aegiscore-frontend/` are STALE and are IGNORED.

---

## 1. Executive summary

AegisCore is a functionally rich .NET 10 / Next.js 16 modular-monolith university ERP with a working demo. It is a **strong demo**, not yet a **repeatable multi-tenant product platform**.

The single most important architectural gap for the business objective (one canonical product, zero-fork tenant deployment) is **tenant configuration**. Today "tenants" are:

1. A single `TENANT_ID` environment variable per deployment (`Program.cs:332`).
2. A config-center key/value store scoped by key string (`ConfigurationCenterService.cs:89`), which accepts a `tenantId` parameter but is consumed through `TenantConfigService` that resolves **without a tenant** (`TenantConfigService.cs:17-47` — all `Resolve(...)` calls pass no tenant).
3. Per-entity `TenantId` columns — **~145 entities carry TenantId without the EF global filter and ~40 core SIS entities carry no TenantId at all** (`DATA_MODEL.md` §2, `CONFIRMED_BY_CODE`).

There is no tenant manifest, no tenant configuration schema, no tenant branding pipeline beyond a single client-logo endpoint, and no tenant-aware provider selection. A deployment today is effectively **one full instance per university** — workable for a first pilot, but it means 10 universities = 10 nearly-identical installations driven by env vars + separate databases. This is acceptable as a **deployment topology** (isolated instances) but is NOT yet the **canonical release + tenant-config** model. The gap to zero-fork is real but closable.

Secondary critical findings:

- **License per-request DB read** — `LicenseService.cs:238-242` `db.LicenseStates.ToListAsync()` on every non-demo request. `CONFIRMED_BY_CODE`; +2-4ms isolated, compounds at scale (`BASELINE_RESULTS.md` §3).
- **Test project does not compile** — `AegisCore.Tests` references deleted `AegisCore.Application.Analytics` / `AegisCore.Infrastructure.Analytics` namespaces. CI's `dotnet test` step would fail. `CONFIRMED_BY_CODE` (build reproduced: 10 errors).
- **OBE N+1 + in-memory aggregation** — `ObeServices.cs:83-126`. `CONFIRMED_BY_CODE`.
- **No origin-side rate limiting** — rate-limit middleware exists but is not registered (`Program.cs:173-176`). `CONFIRMED_BY_CODE`.
- **Tenant isolation incomplete** for core SIS entities (`DATA_MODEL.md` §2). `CONFIRMED_BY_CODE`.
- **No OpenTelemetry** — observability gaps prevent measurement-driven decisions (`SYSTEM_MAP.md` §6). `CONFIRMED_BY_CODE`.

---

## 2. Full pilot feature audit (64 capabilities)

Status classes: `READY` / `IMPLEMENTED_NEEDS_HARDENING` / `PARTIAL` / `STUB` / `PLANNED_ONLY` / `NOT_FOUND`.

Fields per capability: FEATURE / STATUS / EXACT SOURCE FILES / BACKEND / FRONTEND / DATABASE / AUTHORIZATION / TENANT AWARENESS / CLIENT CONFIG / EXTERNAL DEPENDENCY / KNOWN PROBLEMS / MISSING WORK / PILOT BLOCKER / CONFIDENCE.

### 2.1 Admissions
- **FEATURE:** Admissions (enquiries, applications, documents, verification, tracking)
- **STATUS:** IMPLEMENTED_NEEDS_HARDENING
- **FILES:** `AegisCore.API/Controllers/S2AdmissionsController.cs`, `AegisCore.Infrastructure/Admissions/AdmissionsServices.cs`, `AegisCore.Application/Admissions/AdmissionsContracts.cs`, `AegisCore.Domain/Entities/AdmissionsEntities.cs`
- **BACKEND:** IMPLEMENTED (enquiries, applications, documents, verify, IBCC, track by token) `CONFIRMED_BY_CODE`
- **FRONTEND:** NOT_FOUND (no admissions page in `AegisCore.Portal/src/app/`)
- **DATABASE:** Present; `AdmissionApplication`/`MeritList` have **no TenantId** (`DATA_MODEL.md` §2) — indirectly scoped. `CONFIRMED_BY_CODE`
- **AUTHORIZATION:** `[Authorize]` on controller; application ownership checks present but thin. `CONFIRMED_BY_CODE`
- **TENANT AWARENESS:** INDIRECT / INCOMPLETE (no TenantId on admission entities)
- **CLIENT CONFIG REQUIRED:** quotas, weights (merit weights via `policies.GetAsync("admissions","merit_weight_*")`), eligibility rules
- **EXTERNAL DEPENDENCY:** none (IBCC endpoint is a lookup stub — verify)
- **KNOWN PROBLEMS:** no frontend; application↔student linking is new migration; merit scoring is simple weighted formula, not AI
- **MISSING WORK:** portal UI, tenant scoping of admission entities, IBCC endpoint behavior verification
- **PILOT BLOCKER:** NO (backend usable; UI needed for full pilot)

### 2.2 Merit list / AI merit processing
- **STATUS:** PARTIAL
- **FILES:** `AegisCore.Infrastructure/Admissions/AdmissionsServices.cs:15-100` (`MeritCalculationService`, `GenerateMeritListAsync`, `AllocateAsync`), `S2AdmissionsController.cs:194-237`
- **BACKEND:** IMPLEMENTED — weighted merit formula (matric/fsc/entry test via policy weights), merit list generation, seat allocation, quota types. `CONFIRMED_BY_CODE`
- **FRONTEND:** NOT_FOUND
- **DATABASE:** `MeritList`/`MeritListEntry`/`SeatAllocation` — no TenantId. `CONFIRMED_BY_CODE`
- **AUTHORIZATION:** `[Authorize]`; no fine-grained admission-committee roles seen
- **TENANT AWARENESS:** INCOMPLETE
- **CLIENT CONFIG REQUIRED:** merit weight policies, seat counts, quota definitions, entry-test source
- **EXTERNAL DEPENDENCY:** none required
- **KNOWN PROBLEMS:** "AI merit processing" (SHAP-based) is NOT implemented — `REPO_INTELLIGENCE.md` §5 documents SHAP merit as PROPOSED. Current engine is deterministic weighted scoring with an explanation object. `CONFIRMED_BY_CODE`
- **MISSING WORK:** decision on whether "AI merit" means SHAP/ML or weighted formula; if weighted is acceptable for pilot, this is READY-ish; frontend; N+1 in `GenerateMeritListAsync` (loads all applications then scores in a loop — line 47-59)
- **PILOT BLOCKER:** NO (weighted merit is functional; AI merit is a differentiator, not a pilot requirement)
- **CONFIDENCE:** 90

### 2.3 Student onboarding
- **STATUS:** NOT_FOUND (as a product feature)
- **FILES:** Only setup/install onboarding exists: `AegisCore.API/Controllers/SetupController.cs`, `WizardController.cs`, `Onboarding/` (`OnboardingService.cs`, `EnhancedSetupService.cs`). Portal `/onboarding`, `/completion`, `/imports` are aliases to `SetupWizard` (`ROUTE_MATRIX.md` §2).
- **BACKEND:** NOT_FOUND (no "admit student → create account → profile setup → send credentials" lifecycle endpoint seen)
- **FRONTEND:** NOT_FOUND (onboarding pages are the install wizard, not student onboarding)
- **DATABASE:** N/A
- **AUTHORIZATION:** N/A
- **TENANT AWARENESS:** N/A
- **CLIENT CONFIG REQUIRED:** admission→student conversion rules, credential delivery, documents checklist
- **EXTERNAL DEPENDENCY:** email (for credentials), possibly SMS
- **KNOWN PROBLEMS:** Student creation exists via `StudentLifecycle`/seeders but no guided onboarding flow
- **MISSING WORK:** student onboarding workflow (account creation, JIT provisioning from admission, ID card data, docs upload)
- **PILOT BLOCKER:** NO for internal admin use; YES if the pilot requires student self-service account activation

### 2.4 Faculty onboarding
- **STATUS:** NOT_FOUND
- **FILES:** N/A. Faculty appear via `HrController.cs`, `VisitingFacultyController`, identity CRUD (`IdentityController`), SCIM (`ScimController.cs`).
- **BACKEND:** PARTIAL — faculty records + identity + SCIM exist, but no guided onboarding workflow
- **FRONTEND:** NOT_FOUND
- **DATABASE:** Faculty entity has no TenantId (`DATA_MODEL.md` §2)
- **AUTHORIZATION:** N/A
- **TENANT AWARENESS:** INCOMPLETE
- **CLIENT CONFIG REQUIRED:** HR policies, department mapping, ID generation
- **EXTERNAL DEPENDENCY:** email
- **KNOWN PROBLEMS:** none critical; capability simply not productized
- **MISSING WORK:** faculty onboarding workflow + portal
- **PILOT BLOCKER:** NO (admin can provision via identity/SCIM/HR APIs)

### 2.5 Student profiles
- **STATUS:** IMPLEMENTED_NEEDS_HARDENING
- **FILES:** `AegisCore.API/Controllers/StudentPortalController.cs` (`/api/student-portal/me`), `StudentProfileMediaController.cs`, `SelfProfileController.cs` (`/api/me/profile`), `ProfilePictureController.cs`
- **BACKEND:** IMPLEMENTED — profile read, media/avatar, self-profile update `CONFIRMED_BY_CODE`
- **FRONTEND:** IMPLEMENTED — `/student/profile`, `/profile` (`ROUTE_MATRIX.md` §2)
- **DATABASE:** `StudentProfile` has TenantId but NO ITenantScoped filter (`DATA_MODEL.md` §2); `Student` has NO TenantId. `CONFIRMED_BY_CODE`
- **AUTHORIZATION:** `[R=Student,...]` + self-scoping via JWT `nameidentifier`
- **TENANT AWARENESS:** INCOMPLETE (core student entity unscoped)
- **CLIENT CONFIG REQUIRED:** profile field set, enrollment statuses
- **EXTERNAL DEPENDENCY:** none
- **KNOWN PROBLEMS:** tenant filter gap; portal over-fetch (StudentGeneralPage fetches 4 endpoints)
- **MISSING WORK:** tenant-scoping audit, profile field configuration
- **PILOT BLOCKER:** NO

### 2.6 Faculty profiles
- **STATUS:** IMPLEMENTED_NEEDS_HARDENING
- **FILES:** `HrController.cs` (employees), `VisitingFacultyController` (`api/visiting-faculty`), `SelfProfileController.cs`
- **BACKEND:** IMPLEMENTED (HR employee CRUD, visiting faculty registration/match/contracts/rating) `CONFIRMED_BY_CODE`
- **FRONTEND:** NOT_FOUND for faculty profile UI (only `/visiting-faculty` page exists)
- **DATABASE:** Employee/Faculty entities not in ITenantScoped list
- **AUTHORIZATION:** role-gated
- **TENANT AWARENESS:** INCOMPLETE
- **CLIENT CONFIG REQUIRED:** HR structure, departments, job codes
- **KNOWN PROBLEMS:** no consolidated faculty profile page
- **MISSING WORK:** faculty profile portal page
- **PILOT BLOCKER:** NO

### 2.7 Course registration
- **STATUS:** IMPLEMENTED_NEEDS_HARDENING
- **FILES:** `AegisCore.API/Controllers/StudentRegistrationController.cs`, `AegisCore.Infrastructure` registration services, `AegisCore.Domain/Entities/Enrollment.cs`
- **BACKEND:** IMPLEMENTED — enrollment with faculty/section selection, seat checks, fee computation `CONFIRMED_BY_CODE`
- **FRONTEND:** IMPLEMENTED — `/student/courses` (`ROUTE_MATRIX.md` §2)
- **DATABASE:** `Enrollment` NO TenantId (`DATA_MODEL.md` §2)
- **AUTHORIZATION:** `[R=Student,SuperAdmin,Admin]`
- **TENANT AWARENESS:** INCOMPLETE
- **CLIENT CONFIG REQUIRED:** registration windows, seat quotas, fee-per-credit rules, credit-hour limits (`MaxCreditHoursPerSemester` in `DefaultTenantConfigTemplate.cs`)
- **KNOWN PROBLEMS:** N queries per registration (conflict/seat/fee/enroll) — `MEASUREMENT_REQUIRED`; race on seat double-booking (STRIX §6)
- **MISSING WORK:** seat reservation/transactional registration, tenant scoping
- **PILOT BLOCKER:** NO

### 2.8 Multiple faculty/section choices
- **STATUS:** IMPLEMENTED
- **FILES:** `AegisCore.API/Controllers/StudentRegistrationController.cs` — `FacultyOption(Id, Name, Section, Schedule, Room, SeatsRemaining)` per course offering; registration picks a faculty+section; seat-count guard (`:125-126`) `CONFIRMED_BY_CODE`
- **BACKEND:** IMPLEMENTED
- **FRONTEND:** IMPLEMENTED (course registration UI lists sections)
- **DATABASE:** enrollment→course only; section is a display attribute on the offering (no standalone Section table with TenantId confirmed)
- **AUTHORIZATION:** student-scoped
- **TENANT AWARENESS:** INCOMPLETE (Course/Enrollment unscoped)
- **KNOWN PROBLEMS:** none confirmed beyond registration hot-path
- **MISSING WORK:** none blocking
- **PILOT BLOCKER:** NO

### 2.9 Timetable
- **STATUS:** IMPLEMENTED_NEEDS_HARDENING
- **FILES:** `AegisCore.API/Controllers/TimetableController.cs`, `AegisCore.Domain/Entities/Timetable.cs`, `/api/scheduler` (`PlatformControllers.cs`)
- **BACKEND:** IMPLEMENTED — timetable CRUD, faculty/course/batch/room `CONFIRMED_BY_CODE`
- **FRONTEND:** IMPLEMENTED — `/student/timetable` (my-timetable), `/classes`
- **DATABASE:** `Timetable` NO TenantId; missing day/time/room composite index (`DATA_MODEL.md` §6)
- **AUTHORIZATION:** `[R=Faculty,HOD,Registrar,Admin,SuperAdmin]`
- **TENANT AWARENESS:** INCOMPLETE
- **CLIENT CONFIG REQUIRED:** room inventory, time slots, clash rules
- **KNOWN PROBLEMS:** missing index; conflict detection depth unverified
- **MISSING WORK:** index, tenant scoping
- **PILOT BLOCKER:** NO

### 2.10 Student attendance
- **STATUS:** IMPLEMENTED_NEEDS_HARDENING
- **FILES:** `AegisCore.API/Controllers/AttendanceController.cs` (roster/mark/report), `StudentAttendanceController.cs` (history/check-in w/ geolocation), `AegisCore.Infrastructure/Repositories/AttendanceRepository.cs`
- **BACKEND:** IMPLEMENTED `CONFIRMED_BY_CODE`
- **FRONTEND:** IMPLEMENTED — `/attendance`, `/student/attendance`
- **DATABASE:** `Attendance` NO TenantId; missing indexes `Attendance(Date)`, `Attendance(EnrollmentId,Status)` (`DATA_MODEL.md` §6)
- **AUTHORIZATION:** Faculty marks own roster; student self check-in
- **TENANT AWARENESS:** INCOMPLETE
- **CLIENT CONFIG REQUIRED:** campus geo-coordinates/radius (already in compose: `Attendance__CampusLatitude` etc.)
- **KNOWN PROBLEMS:** attendance percentage loads full row set (`AttendanceRepository.cs:13-24`) — `CONFIRMED_BY_CODE`; hardcoded `markedByFacultyId` zero-GUID in frontend (`ROUTE_MATRIX.md` §6)
- **MISSING WORK:** COUNT aggregate, indexes, remove zero-GUID
- **PILOT BLOCKER:** NO

### 2.11 Faculty/employee attendance
- **STATUS:** PARTIAL
- **FILES:** `AegisCore.API/Controllers/HrController.cs` — leave approval auto-marks `AttendanceRecord` excused (`:89`); `AttendanceRecords` entity; `AttendanceController` is student-oriented
- **BACKEND:** PARTIAL — employee attendance is emergent from leave handling; no dedicated punch/time-clock/schedule
- **FRONTEND:** NOT_FOUND
- **DATABASE:** AttendanceRecord exists
- **AUTHORIZATION:** HR roles
- **TENANT AWARENESS:** INCOMPLETE
- **CLIENT CONFIG REQUIRED:** employee schedules, shift policy, geo/clocking rules
- **KNOWN PROBLEMS:** no faculty clock-in workflow, no faculty attendance UI
- **MISSING WORK:** staff attendance module (clock-in, leave calendar, reports) or explicit out-of-scope decision
- **PILOT BLOCKER:** NO for academic pilot; YES if HR attendance is a pilot requirement

### 2.12 Attendance correction / request workflows
- **STATUS:** NOT_FOUND
- **FILES:** none found. `MarkAttendanceRequest.cs` is the faculty marking DTO; no student-correction request/approval flow
- **BACKEND:** NOT_FOUND
- **FRONTEND:** NOT_FOUND
- **DATABASE:** no correction entity found
- **AUTHORIZATION:** N/A
- **KNOWN PROBLEMS:** absent capability
- **MISSING WORK:** attendance-correction request + HOD approval workflow (could ride the existing WorkflowEngine)
- **PILOT BLOCKER:** NO (operational nicety); YES if the pilot explicitly needs corrections

### 2.13 Examinations
- **STATUS:** IMPLEMENTED_NEEDS_HARDENING
- **FILES:** `AegisCore.API/Controllers/S3ExaminationController.cs` (schemes, marks, moderation, publish, SGPA/CGPA, transcript request), `ExamController` (`CoreErpControllers.cs`), `ConvocationsController`
- **BACKEND:** IMPLEMENTED — assessment schemes, mark entry, moderation submit/review, result publish, GPA `CONFIRMED_BY_CODE`
- **FRONTEND:** IMPLEMENTED — `/examinations`, `/convocations`, `/student/examinations`
- **DATABASE:** `CourseMark`/`CourseGrade`/`ExamSchedule` NO TenantId (`DATA_MODEL.md` §2)
- **AUTHORIZATION:** faculty own-course entry; moderation review `[R=ExamController,HOD,Admin,SuperAdmin]`
- **TENANT AWARENESS:** INCOMPLETE
- **CLIENT CONFIG REQUIRED:** grading scale/bands, GPA policy, exam calendar
- **KNOWN PROBLEMS:** marks entry queries per request; moderation string-scan `ReviewComments.StartsWith("GRADE_CHANGE:")` (HOT_PATH T7)
- **MISSING WORK:** indexes, tenant scoping, batch grade calc
- **PILOT BLOCKER:** NO

### 2.14 Examination seating
- **STATUS:** PARTIAL
- **FILES:** `AegisCore.Domain/Entities/ExamSeating.cs` (SeatNumber, Classroom, Row/Column), `ExamSeatingRepository.cs`, `S3ExaminationController` (no seating endpoints seen); QR payload builder in `SsoHitlServices.cs` (`exam:{id}:student:{id}`)
- **BACKEND:** PARTIAL — seating entity/repo exist; generation/allocation endpoint not confirmed
- **FRONTEND:** NOT_FOUND
- **DATABASE:** `ExamSeating` no TenantId
- **KNOWN PROBLEMS:** endpoint coverage unclear
- **MISSING WORK:** seating-plan generation API + UI
- **PILOT BLOCKER:** NO for semester-1; needed before large exams

### 2.15 Admit cards
- **STATUS:** NOT_FOUND (as document generator)
- **FILES:** only a WhatsApp template `wwwroot/templates/whatsapp-templates.json` mentions "hall ticket ready"; no admit-card generation endpoint
- **BACKEND:** NOT_FOUND
- **FRONTEND:** NOT_FOUND
- **DATABASE:** N/A
- **KNOWN PROBLEMS:** absent
- **MISSING WORK:** admit-card PDF generation (reuse transcript/report pipeline) + QR
- **PILOT BLOCKER:** NO for MVP; YES if pilot exams require admit cards

### 2.16 Grade submission
- **STATUS:** IMPLEMENTED_NEEDS_HARDENING
- **FILES:** `AegisCore.API/Controllers/FacultyGradeEntryController.cs` (`api/faculty-grade-entry`), `S3ExaminationController.EnterMarks`, `GradingService.cs`
- **BACKEND:** IMPLEMENTED `CONFIRMED_BY_CODE`
- **FRONTEND:** IMPLEMENTED — `/examinations` (marks PUT, import preview/commit, template.csv)
- **DATABASE:** CourseMarks/CourseGrades no TenantId; indexes partial (`DATA_MODEL.md` §5)
- **AUTHORIZATION:** `[R=Faculty,HOD,ExamController,Admin,SuperAdmin]`
- **TENANT AWARENESS:** INCOMPLETE
- **KNOWN PROBLEMS:** grading N+1 (`GradingService.cs:93-116`); moderation string-scan
- **MISSING WORK:** batch evaluation, indexes
- **PILOT BLOCKER:** NO

### 2.17 Manual grade entry
- **STATUS:** IMPLEMENTED
- **FILES:** `FacultyGradeEntryController.cs` (manual mode), `S3ExaminationController.EnterMarks`, portal `/examinations`
- **BACKEND:** IMPLEMENTED `CONFIRMED_BY_CODE`
- **FRONTEND:** IMPLEMENTED
- **KNOWN PROBLEMS:** N+1; moderation lock semantics OK
- **PILOT BLOCKER:** NO

### 2.18 Excel grade import
- **STATUS:** IMPLEMENTED
- **FILES:** `FacultyGradeEntryController.cs` import-preview/import-commit (CSV/XLSX/PDF parsing), template.csv; portal `/examinations` two-phase import
- **BACKEND:** IMPLEMENTED `CONFIRMED_BY_CODE`
- **FRONTEND:** IMPLEMENTED
- **KNOWN PROBLEMS:** XLSX OOXML parser surface (SEC-11); two-phase OK
- **PILOT BLOCKER:** NO

### 2.19 PDF grade import
- **STATUS:** IMPLEMENTED
- **FILES:** `FacultyGradeEntryController.cs` (PDF parse), `AI/DocumentOcrService.cs`, `AzureDocumentIntelligenceService.cs` (OCR)
- **BACKEND:** IMPLEMENTED `CONFIRMED_BY_CODE`
- **FRONTEND:** IMPLEMENTED
- **KNOWN PROBLEMS:** OCR parser abuse surface; Azure dependency optional
- **PILOT BLOCKER:** NO (Excel/CSV primary; PDF/OCR secondary)

### 2.20 Sensitive grade-change detection
- **STATUS:** IMPLEMENTED_NEEDS_HARDENING
- **FILES:** `MarkModeration` with `ReviewComments = "GRADE_CHANGE:..."` (HOT_PATH T7), `GradeApprovalDiscoveryController.Parse`, `GradeAppealsController`
- **BACKEND:** IMPLEMENTED — grade changes routed to moderation + parsed for approval queue `CONFIRMED_BY_CODE`
- **FRONTEND:** IMPLEMENTED — `/grade-appeals`, `/grade-approvals`, `/grade-distributions`
- **KNOWN PROBLEMS:** string-prefix scan is fragile (index on computed column advised); audit relies on moderation comments
- **MISSING WORK:** structured change reason + audit index
- **PILOT BLOCKER:** NO

### 2.21 HOD approval
- **STATUS:** IMPLEMENTED
- **FILES:** `AegisCore.API/Controllers/GradeApprovalDiscoveryController.cs` (department-scoped queue, `:72-80`), `S3ExaminationController.ReviewModeration`
- **BACKEND:** IMPLEMENTED — department-scoped HOD queue `CONFIRMED_BY_CODE`
- **FRONTEND:** IMPLEMENTED — `/grade-approvals`
- **KNOWN PROBLEMS:** `AllowedCourses` loads full `List<Guid>` (fine at 10k; monitor)
- **PILOT BLOCKER:** NO

### 2.22 Examination-controller approval
- **STATUS:** IMPLEMENTED
- **FILES:** `GradeApprovalDiscoveryController.cs` (ExamController sees all courses `:74-75`), `S3ExaminationController.ReviewModeration`
- **BACKEND:** IMPLEMENTED `CONFIRMED_BY_CODE`
- **FRONTEND:** IMPLEMENTED — `/grade-approvals`
- **PILOT BLOCKER:** NO

### 2.23 OBE
- **STATUS:** IMPLEMENTED_NEEDS_HARDENING
- **FILES:** `AegisCore.Infrastructure/Obe/ObeServices.cs`, `AegisCore.API/Controllers/S5ObeController.cs`, `FacultyObeController.cs`, `StudentObeController.cs`
- **BACKEND:** IMPLEMENTED — CIS upload/process/approve, CLO→PO mapping, attainment, HEMIS export `CONFIRMED_BY_CODE`
- **FRONTEND:** IMPLEMENTED — `/obe`, `/student/outcomes`
- **DATABASE:** CLOAttainment/CloPlomapping no TenantId; missing composite indexes (`DATA_MODEL.md` §6)
- **AUTHORIZATION:** `[R=Faculty,OBECoordinator,Dean,Registrar,Admin,SuperAdmin]`
- **TENANT AWARENESS:** INCOMPLETE
- **KNOWN PROBLEMS:** N+1 + in-memory AVG (`ObeServices.cs:83-126`) — top P0 optimization; demo dataset has 0 CLO data so ceiling unmeasured (`BASELINE_RESULTS.md` §8)
- **MISSING WORK:** SQL aggregation, indexes, seeded-load measurement
- **PILOT BLOCKER:** NO for functional pilot; YES for accreditation-scale reporting until optimized

### 2.24 CLO management
- **STATUS:** IMPLEMENTED
- **FILES:** `S5ObeController` mappings endpoint, `CoreErpEntities1.cs` (`CourseLearningOutcome`), CIS pipeline
- **BACKEND:** IMPLEMENTED `CONFIRMED_BY_CODE`
- **KNOWN PROBLEMS:** rides the OBE N+1 path
- **PILOT BLOCKER:** NO

### 2.25 PLO/PO management
- **STATUS:** IMPLEMENTED
- **FILES:** same as OBE; `ProgramOutcome` entity, `CloPlomapping`
- **BACKEND:** IMPLEMENTED `CONFIRMED_BY_CODE`
- **PILOT BLOCKER:** NO

### 2.26 Student CLO/PLO attainment
- **STATUS:** IMPLEMENTED_NEEDS_HARDENING
- **FILES:** `ObeServices.cs` (CalculateCLOAttainmentAsync/CalculatePOAttainmentAsync), `StudentObeController` (`/api/student-portal/obe`)
- **BACKEND:** IMPLEMENTED `CONFIRMED_BY_CODE`; performance poor at scale (N+1)
- **FRONTEND:** IMPLEMENTED — `/student/outcomes`
- **PILOT BLOCKER:** NO functionally; performance blocked until A2 (SQL aggregation)
- **CONFIDENCE:** 90

### 2.27 Accreditation reporting
- **STATUS:** PARTIAL
- **FILES:** `AegisCore.Infrastructure/Governance/Accreditation*` (via DOMAIN_MAP), `AccreditationController`, `ComplianceController`, `S5ObeController.Hemis`, `HemisExportService` (`ObeServices.cs:129-148`)
- **BACKEND:** PARTIAL — HEMIS export exists (XML via `S5ObeController.cs:71-73`); broader accreditation evidence/reporting depth unverified (`DOCUMENTED_NOT_VERIFIED` for full HEC self-assessment)
- **FRONTEND:** NOT_FOUND (no accreditation page)
- **KNOWN PROBLEMS:** HEMIS export returns `application/xml` (confirmed) — earlier docs described an Excel workbook (stale claim); SSR evidence collection is thin
- **MISSING WORK:** accreditation evidence management UI, report pack, HEMIS workbook if required
- **PILOT BLOCKER:** NO for pilot ops; YES if accreditation reporting is a pilot KPI
- **CONFIDENCE:** 70

### 2.28 Transcripts
- **STATUS:** IMPLEMENTED
- **FILES:** `AegisCore.API/Controllers/StudentDocumentsController.cs` (official PDF `:28-109`, download `:112-130`, verify `:133-158`), `Application` PdfSharpCore transcript builder
- **BACKEND:** IMPLEMENTED `CONFIRMED_BY_CODE`
- **FRONTEND:** IMPLEMENTED — `/student/transcript`
- **KNOWN PROBLEMS:** synchronous PDF+PQC+QR (`BASELINE_RESULTS.md` §7: 3s single-user, 30s at 50+ VUs)
- **MISSING WORK:** async generation off request path
- **PILOT BLOCKER:** NO at low volume; YES at exam-result season until async
- **CONFIDENCE:** 95

### 2.29 PDFs/documents (generation)
- **STATUS:** IMPLEMENTED
- **FILES:** PdfSharpCore (Application), `FinanceModuleEndpoints.cs` report builders (CSV/XLSX/PDF/XBRL/iXBRL), DocumentVault
- **BACKEND:** IMPLEMENTED `CONFIRMED_BY_CODE`
- **KNOWN PROBLEMS:** synchronous + double serialization (LB-04); template fixed in code (no per-tenant templates)
- **PILOT BLOCKER:** NO; template customization is a product gap (see CLIENT_DOCUMENT_CUSTOMIZATION.md)

### 2.30 QR verification
- **STATUS:** IMPLEMENTED
- **FILES:** QRCoder (Application), `StudentDocumentsController.cs:133-158`, finance report QR, `Transcript.VerificationHash` UNIQUE index
- **BACKEND:** IMPLEMENTED `CONFIRMED_BY_CODE`
- **KNOWN PROBLEMS:** public verify endpoints unprotected by rate limiting (SEC-02)
- **PILOT BLOCKER:** NO

### 2.31 PQC signatures
- **STATUS:** IMPLEMENTED
- **FILES:** `AegisCore.Infrastructure/Security/Pqc/*` (SLH-DSA/ML-DSA/ML-KEM, `PqcSigningQueue`, key ceremony), `HybridPqcTransportMiddleware`, `LedgerVerifier`
- **BACKEND:** IMPLEMENTED `CONFIRMED_BY_CODE`
- **KNOWN PROBLEMS:** per-document synchronous signing; transport enforcement is config-gated; key ceremony 12h cycle
- **PILOT BLOCKER:** NO (differentiator, working)
- **CONFIDENCE:** 90

### 2.32 Finance
- **STATUS:** IMPLEMENTED_NEEDS_HARDENING
- **FILES:** `AegisCore.Infrastructure/Finance/` (largest module), `AegisCore.API/Finance/FinanceModuleEndpoints.cs` (~130 endpoints), `FinanceController.cs`, `FinanceWebhookController.cs`
- **BACKEND:** IMPLEMENTED `CONFIRMED_BY_CODE`
- **FRONTEND:** IMPLEMENTED — `/finance`, `/finance/operations`, `/finance/gateways`, `/finance/department/[id]`, `/audit`, `/auditor`
- **DATABASE:** finance entities mostly TenantId-scoped (44 migrations, cost-center/tenant isolation)
- **AUTHORIZATION:** role-gated; auditor opaque token; webhooks HMAC+PQC
- **KNOWN PROBLEMS:** finance report builders synchronous + double serialization (LB-04); payment-plan sweep per-installment queries (HP-07); pre-existing 500 on executive dashboard LINQ join (BASELINE §6)
- **MISSING WORK:** report async, sweep batching, LINQ fix
- **PILOT BLOCKER:** NO (module is the most complete)
- **CONFIDENCE:** 90

### 2.33 Fee management
- **STATUS:** IMPLEMENTED
- **FILES:** fee demands/installments/payment plans in Finance module; challan PDF (`/api/student-portal/finance/challan/:demandId`); `DefaultTenantConfigTemplate.FeeDefaults`
- **BACKEND:** IMPLEMENTED `CONFIRMED_BY_CODE`
- **FRONTEND:** IMPLEMENTED — `/student/finance`
- **PILOT BLOCKER:** NO

### 2.34 Audit trail
- **STATUS:** IMPLEMENTED_NEEDS_HARDENING
- **FILES:** `Security/AuditLogService.cs`, `LoginAuditService.cs` (+NDJSON archive), finance audit seals (Merkle-rooted, 15min), ledger hash-chain
- **BACKEND:** IMPLEMENTED `CONFIRMED_BY_CODE`
- **KNOWN PROBLEMS:** frontend zero-GUID actor IDs pollute audit (`ROUTE_MATRIX.md` §6); tenant-scoping gaps mean some audit entries may be unscoped
- **PILOT BLOCKER:** NO (functional; fix zero-GUIDs before external audit claims)

### 2.35 HR
- **STATUS:** IMPLEMENTED_NEEDS_HARDENING
- **FILES:** `AegisCore.API/Controllers/HrController.cs` (employees, jobs, leaves+approval, payroll run, bank export, gratuity)
- **BACKEND:** IMPLEMENTED `CONFIRMED_BY_CODE`
- **FRONTEND:** NOT_FOUND (no HR page)
- **KNOWN PROBLEMS:** leave approval auto-excuses attendance (design decision to confirm); no HR UI
- **PILOT BLOCKER:** NO via API; UI gap for admin pilots
- **CONFIDENCE:** 85

### 2.36 Faculty management
- **STATUS:** PARTIAL
- **FILES:** `HrController.cs`, `VisitingFacultyController`, `FacultyCoursesController.cs`, `CoreErpEntities2.cs`
- **BACKEND:** PARTIAL — HR employee + visiting faculty; no full faculty-employment lifecycle
- **FRONTEND:** NOT_FOUND
- **PILOT BLOCKER:** NO

### 2.37 Workflow engine
- **STATUS:** IMPLEMENTED
- **FILES:** `AegisCore.Infrastructure/Workflow/WorkflowEngine.cs` (definition/instance/step, transitions JSON, start/advance), `ApprovalService.cs`, `StateMachine/StateMachineService.cs`, `SignalR StateMachineHub`, `api/workflow`
- **BACKEND:** IMPLEMENTED `CONFIRMED_BY_CODE`
- **FRONTEND:** IMPLEMENTED — `/student/workflow` (via StudentGeneralPage)
- **KNOWN PROBLEMS:** generic engine; adoption across modules uneven (grade approval has its own bespoke flow)
- **PILOT BLOCKER:** NO

### 2.38 Surveys
- **STATUS:** IMPLEMENTED (forms/surveys)
- **FILES:** `AegisCore.API/Controllers/FormsController.cs` (create/get/submit/draft/submissions/export/templates), `AegisCore.Infrastructure/Forms/FormService.cs`, `FormDefinition/FormFieldConfig`
- **BACKEND:** IMPLEMENTED `CONFIRMED_BY_CODE`
- **FRONTEND:** IMPLEMENTED — `/student/survey` (StudentGeneralPage mode="survey")
- **PILOT BLOCKER:** NO

### 2.39 Student welfare
- **STATUS:** PARTIAL (mental-health focused)
- **FILES:** `MentalHealthController` (`api/mental-health`), `StudentLifecycleModuleEndpoints` (`api/student/v2/mental-health`), `StudentLifecycleController.cs`
- **BACKEND:** IMPLEMENTED for mental-health tickets (submit/classify/assign/resolve/mandatory-report); welfare broader (counseling, financial aid aid-links) unverified
- **FRONTEND:** IMPLEMENTED — `/mental-health`
- **PILOT BLOCKER:** NO (mental health module exists)

### 2.40 Cafeteria
- **STATUS:** IMPLEMENTED
- **FILES:** `CafeteriaController` (`api/cafeteria` — units/menu/orders/forecast/vendor-eval/inventory alerts), `CafeteriaService.cs`, `CafeteriaEntities.cs`
- **BACKEND:** IMPLEMENTED `CONFIRMED_BY_CODE`
- **FRONTEND:** IMPLEMENTED — `/cafeteria`
- **KNOWN PROBLEMS:** demand forecast is a stub-like analytic (validate) — `MEASUREMENT_REQUIRED`
- **PILOT BLOCKER:** NO

### 2.41 Analytics
- **STATUS:** PARTIAL / STUB
- **FILES:** `AIAnalyticsService` (`ExtendedPlatformServices.cs:96-101` — AI feedback/token stats), `SupportController.Analytics` (`api/support/analytics`), `AIAnalyticsController` (`api/ai/analytics`), finance dashboards
- **BACKEND:** PARTIAL — per-domain AI feedback analytics + finance dashboards; **no general statistical analytics engine** (the `AegisCore.Infrastructure.Analytics` namespace was deleted in commit f05397dc; tests referencing it do not compile)
- **FRONTEND:** PARTIAL — finance dashboards; no analytics/reporting workspace
- **KNOWN PROBLEMS:** **Analytics test suite broken** (`AnalyticsIntelligenceTests.cs`, `StatisticalAnalyticsTests.cs`, `GoldenDemoSeederTests.cs` reference deleted namespaces) — CI `dotnet test` fails
- **MISSING WORK:** decision: rebuild statistical analytics in .NET (recommended) or R worker (see POLYGLOT_DECISION_MATRIX.md); fix/remove broken tests
- **PILOT BLOCKER:** NO (finance analytics exist); YES if institutional analytics is a pilot KPI
- **CONFIDENCE:** 90 (broken tests CONFIRMED_BY_CODE build failure)

### 2.42 Power BI
- **STATUS:** PARTIAL (embed only)
- **FILES:** `PowerBiEmbeddingService` (`MicrosoftEcosystemServices.cs:113-200`), `MicrosoftEcosystemController` (`GET /api/microsoft-ecosystem/powerbi/embed`), `PowerBiReports` entity
- **BACKEND:** PARTIAL — returns embed config + token (static token or Azure AD client-credentials); works behind config `CONFIRMED_BY_CODE`
- **FRONTEND:** NOT_FOUND (no Power BI page; Caddy CSP allows `app.powerbi.com` frames)
- **KNOWN PROBLEMS:** token via client-credentials only (no row-level-security per user beyond role filter); raw HttpClient in `AzureAdPolicy` (`new HttpClient()` at `:169`)
- **MISSING WORK:** frontend embed page, RLS design, IHttpClientFactory
- **PILOT BLOCKER:** NO (can be demonstrated behind config); integration depth is vendor work
- **CONFIDENCE:** 80

### 2.43 Notifications
- **STATUS:** IMPLEMENTED
- **FILES:** `AegisCore.Infrastructure/Workflow/EmailNotificationService.cs` (`INotificationService`), `PlatformControllers.cs` (`api/notifications` send/templates/deliveries), `NotificationHub`, `SignalRNotificationRealtimePublisher.cs`
- **BACKEND:** IMPLEMENTED `CONFIRMED_BY_CODE`
- **FRONTEND:** IMPLEMENTED (in-app bell is non-functional; `/api/notifications` API works) `ROUTE_MATRIX.md` §2
- **KNOWN PROBLEMS:** single monolithic `INotificationService` with channel switch — NOT a pluggable provider abstraction; raw HttpClients; notification bell/search non-functional
- **MISSING WORK:** `INotificationProvider` abstraction (see PROVIDER_ADAPTERS.md), notification UI
- **PILOT BLOCKER:** NO

### 2.44 Microsoft Teams
- **STATUS:** PARTIAL (basic provisioning)
- **FILES:** `TeamsProvisioningService` (`MicrosoftEcosystemServices.cs:11-63`) — creates education class from timetable entry via Graph; `api/microsoft-ecosystem/teams/timetable/{id}/provision`
- **BACKEND:** PARTIAL — single-tenant-class creation works behind `MicrosoftGraph:AccessToken` config `CONFIRMED_BY_CODE`; no auto-provisioning batch, no membership sync
- **FRONTEND:** NOT_FOUND
- **KNOWN PROBLEMS:** uses a single static Graph token; no token acquisition/refresh; no membership/team-channel management
- **MISSING WORK:** Graph auth (client-credentials), batch provisioning, membership sync, Teams admin consent documentation
- **PILOT BLOCKER:** NO (config-driven, but maturity low — treat as PARTIAL, not READY)
- **CONFIDENCE:** 75

### 2.45 Microsoft Graph
- **STATUS:** PARTIAL
- **FILES:** `MicrosoftEcosystemServices.cs`, `MicrosoftEcosystemServiceExtensions.cs` (registers `graph` HttpClient), `MicrosoftEcosystemController.cs`
- **BACKEND:** PARTIAL — Teams + Moodle + PowerBI use Graph/token; no general Graph data service (mail, calendar, files) `CONFIRMED_BY_CODE`
- **KNOWN PROBLEMS:** static token config; no OAuth client-credentials helper shared
- **MISSING WORK:** token service (client-credentials + delegated), scopes management, consent
- **PILOT BLOCKER:** NO; depth depends on pilot ask (Teams auto-provisioning is the likely pilot item)
- **CONFIDENCE:** 75

### 2.46 Microsoft Entra ID
- **STATUS:** PARTIAL (identity via SSO redirect)
- **FILES:** `SsoHitlServices.cs:39` builds Microsoft OAuth2 authorize URL; `SamlService.cs` (SAML); `UniversitySsoProfile` entity
- **BACKEND:** PARTIAL — redirect-URL generation for Microsoft login works; **no OIDC code-exchange/callback/claims-to-JWT flow confirmed**; SAML ACS validation exists (`SamlService.cs:90-198`) `CONFIRMED_BY_CODE`
- **FRONTEND:** NOT_FOUND (login page has no SSO buttons — email/password only) `CONFIRMED_BY_CODE`
- **KNOWN PROBLEMS:** the SSO flow stops at building a redirect URL; no callback endpoint, no token exchange, no JWT issuance for SSO users, no claims/role mapping, no JIT provisioning. SAML validation loads SignedXml but does not appear to call CheckSignature (signature verification step incomplete) — `INFERRED`, verify in hardening.
- **MISSING WORK:** full OIDC provider (callback, token validation, claims→role/dept/program/ID mapping, JWT issuance, JIT provisioning), SAML signature verification completion
- **PILOT BLOCKER:** YES if pilot requires Entra SSO; NO if pilot uses local auth only. This is the largest identity gap (see IDENTITY_FEDERATION.md)
- **CONFIDENCE:** 85

### 2.47 Google identity
- **STATUS:** STUB
- **FILES:** `SsoHitlServices.cs:40` Google OAuth2 authorize URL; `UniversitySsoProfile`
- **BACKEND:** STUB — redirect URL only, no callback/token exchange `CONFIRMED_BY_CODE`
- **FRONTEND:** NOT_FOUND
- **KNOWN PROBLEMS:** same as Entra (no flow beyond redirect)
- **MISSING WORK:** OIDC completion (covered by the same provider-neutral OIDC adapter)
- **PILOT BLOCKER:** NO unless Google SSO is required
- **CONFIDENCE:** 80

### 2.48 Okta
- **STATUS:** STUB
- **FILES:** `SsoHitlServices.cs:41` Okta authorize URL (SsoTenantId used as base URL)
- **BACKEND:** STUB — redirect URL only `CONFIRMED_BY_CODE`
- **KNOWN PROBLEMS:** same as above
- **MISSING WORK:** OIDC completion
- **PILOT BLOCKER:** NO unless Okta is the pilot IdP
- **CONFIDENCE:** 80

### 2.49 SAML
- **STATUS:** IMPLEMENTED_NEEDS_HARDENING
- **FILES:** `AegisCore.Infrastructure/Identity/SamlService.cs` (metadata gen, AuthnRequest, response validation w/ XXE guard, replay cache, NotOnOrAfter, issuer lookup), `AegisCore.API/Controllers/SamlController.cs` (metadata/login/acs/config), `SamlConfiguration.cs` entity
- **BACKEND:** IMPLEMENTED `CONFIRMED_BY_CODE` — but signature verification appears incomplete (`INFERRED`: SignedXml.LoadXml without confirmed CheckSignature)
- **FRONTEND:** NOT_FOUND
- **KNOWN PROBLEMS:** process-local replay cache (`SamlService.cs:126-129` — breaks with multiple replicas); signature check must be verified; no ACS→JWT issuance/link confirmed
- **MISSING WORK:** verify/complete signature validation, Redis replay store, ACS→session→JWT completion, account linking
- **PILOT BLOCKER:** YES if SAML SSO is a pilot requirement until hardened; the SAML engine itself is the strongest identity piece present
- **CONFIDENCE:** 70 (validation present but not end-to-end verified)

### 2.50 OIDC
- **STATUS:** STUB
- **FILES:** no OIDC library (`Microsoft.AspNetCore.Authentication.OpenIdConnect` not registered); only manual authorize-URL builders (`SsoHitlServices.cs`), JwtBearer with optional `Auth:Authority` for token validation
- **BACKEND:** STUB — no OIDC handler, no code exchange, no userinfo, no callback. `CONFIRMED_BY_CODE`
- **FRONTEND:** NOT_FOUND
- **KNOWN PROBLEMS:** JwtBearer can validate third-party-issued JWTs IF `Auth:Authority` is set (that's validation, not login flow)
- **MISSING WORK:** full OIDC login adapter (provider-neutral) — THE primary identity work item
- **PILOT BLOCKER:** YES if institutional SSO required
- **CONFIDENCE:** 90

### 2.51 Local authentication
- **STATUS:** IMPLEMENTED
- **FILES:** `AuthController.cs` (login/refresh/logout), `JwtTokenService.cs`, Argon2id (Konscious), refresh-family in Redis, `LoginAuditService.cs`
- **BACKEND:** IMPLEMENTED `CONFIRMED_BY_CODE` (Argon2 CPU cost + GeoIP external call are hot-path costs — LB-01)
- **FRONTEND:** IMPLEMENTED — `/login` (demo accounts)
- **KNOWN PROBLEMS:** demo credentials hardcoded; no origin rate limit on login; localStorage token exposure (SEC-09)
- **PILOT BLOCKER:** NO (local auth is the pilot fallback — important given email-hosting ≠ SSO)
- **CONFIDENCE:** 95

### 2.52 WhatsApp
- **STATUS:** IMPLEMENTED
- **FILES:** `EmailNotificationService.cs:144-181` (WhatsApp Business Graph API, `WhatsApp:AccessToken`, `WhatsApp:PhoneNumberId`), `CommunicationsController` (`/api/communications/whatsapp/webhook`), `wwwroot/templates/whatsapp-templates.json`
- **BACKEND:** IMPLEMENTED `CONFIRMED_BY_CODE`
- **FRONTEND:** PARTIAL (notices/grievances UI exists; WhatsApp deep-linking unverified)
- **KNOWN PROBLEMS:** provider logic embedded in monolithic notification service (no provider abstraction); no template/consent management
- **MISSING WORK:** move to `INotificationProvider`; template approval workflow
- **PILOT BLOCKER:** NO
- **CONFIDENCE:** 85

### 2.53 Email
- **STATUS:** IMPLEMENTED
- **FILES:** `EmailNotificationService.cs:39-77` (System.Net.Mail SMTP), `TenantConfigService` SMTP getters, wizard SMTP test (`OnboardingService.TestSmtpAsync`)
- **BACKEND:** IMPLEMENTED `CONFIRMED_BY_CODE`
- **KNOWN PROBLEMS:** synchronous SMTP on request path; `SmtpClient` (legacy); no queue; no HTML template system
- **MISSING WORK:** background delivery queue, provider abstraction (SMTP/Graph)
- **PILOT BLOCKER:** NO
- **CONFIDENCE:** 90

### 2.54 Tenant configuration
- **STATUS:** PARTIAL
- **FILES:** `AegisCore.Infrastructure/Config/TenantConfigService.cs`, `DefaultTenantConfigTemplate.cs`, `ConfigurationCenterService.cs` (`api/config`), `FeatureFlagController` (tenant overrides), `WizardController`/`OnboardingService` (writes `.env.generated` + config)
- **BACKEND:** PARTIAL — config-center key/value + feature flags + env vars exist; NO tenant manifest schema, NO tenant-scoped config resolution (TenantConfigService ignores tenantId), NO branding/config UI beyond wizard `CONFIRMED_BY_CODE`
- **FRONTEND:** NOT_FOUND (no settings/tenant page)
- **DATABASE:** ConfigCenter entries keyed by string; tenant scoping of config values unverified
- **CLIENT CONFIG REQUIRED:** everything (this is the productization core — see TENANT_CONFIGURATION.md)
- **KNOWN PROBLEMS:** config keys are ad-hoc dotted strings with code-coded fallbacks; secrets mixed into config paths (SMTP password etc. read from env); no separation of config vs secrets at runtime for SSO/payment config
- **MISSING WORK:** tenant manifest schema, config/secrets separation, per-tenant resolution, UI, migration of env → manifest
- **PILOT BLOCKER:** NO for a single pilot (env vars suffice); YES for multi-tenant zero-fork (P0 productization)
- **CONFIDENCE:** 90

### 2.55 Multi-department configuration
- **STATUS:** PARTIAL
- **FILES:** `Department` entity, HOD department-scoping (`GradeApprovalDiscoveryController.cs:77-80`), curriculum dept mapping
- **BACKEND:** PARTIAL — departments exist and are used for scoping; no configurable department setup workflow beyond admin APIs
- **FRONTEND:** NOT_FOUND
- **CLIENT CONFIG REQUIRED:** department tree, chairs, HODs
- **PILOT BLOCKER:** NO (seeded via admin)

### 2.56 Multi-program configuration
- **STATUS:** PARTIAL
- **FILES:** `DegreeProgram`, `Batch`, curriculum, catalog ingestion (`/api/catalog/ingest`)
- **BACKEND:** PARTIAL — program entities + catalog ingestion implemented; program setup UI not confirmed
- **FRONTEND:** NOT_FOUND (catalog page exists)
- **PILOT BLOCKER:** NO (admin/import driven)

### 2.57 Institution branding
- **STATUS:** PARTIAL
- **FILES:** `BrandingController.cs` (client-logo get/upload, aegiscore-logo), `UniversitySsoProfile.LogoUrl/PrimaryColor`, login page hardcodes "Institute of Business Management" (`login/page.tsx:85`), setup wizard branding upload (`WizardController`)
- **BACKEND:** PARTIAL — logo storage + SSO profile color/logo fields exist `CONFIRMED_BY_CODE`
- **FRONTEND:** PARTIAL — login page branding is IoBM-specific; portal header uses client logo endpoint
- **KNOWN PROBLEMS:** IoBM hard-coded text in login page + testimonials; no full theming (colors/typography/name) pipeline; branding config not tenant-scoped
- **MISSING WORK:** tenant branding bundle (name, short name, logo, colors, footer, locale defaults) resolved per-tenant and applied consistently
- **PILOT BLOCKER:** NO (cosmetic); YES if the pilot university must not see IoBM branding
- **CONFIDENCE:** 90 (hard-coded IoBM text CONFIRMED_BY_CODE)

### 2.58 Institution-specific documents
- **STATUS:** NOT_FOUND (as tenant-customizable)
- **FILES:** transcripts/reports/challans are generated with fixed in-code layouts (`StudentDocumentsController.cs`, `FinanceModuleEndpoints.cs`); document-vault tiers exist (`DocumentVaultService.cs`)
- **BACKEND:** NOT_FOUND — no template engine, no per-tenant template storage, no template versioning
- **FRONTEND:** N/A
- **KNOWN PROBLEMS:** every university would need code changes to restyle documents — the core zero-fork blocker for document customization
- **MISSING WORK:** configurable template system (see CLIENT_DOCUMENT_CUSTOMIZATION.md)
- **PILOT BLOCKER:** NO for pilot-1 if stock layouts acceptable; YES when the pilot demands its own transcript/letterhead
- **CONFIDENCE:** 95

### 2.59 Backups
- **STATUS:** IMPLEMENTED
- **FILES:** `AegisCore.Infrastructure/Backup/` (`BackupService`, `AutomatedBackupJob` hourly Quartz, retention), `deploy/scripts/backup.sh` (pg_dump + WAL + S3 + encryption), `deploy/config/pgbouncer.ini`
- **BACKEND:** IMPLEMENTED `CONFIRMED_BY_CODE`; ops scripts `CONFIRMED_BY_CONFIG`
- **KNOWN PROBLEMS:** hourly backup on same Postgres instance (`SERVICE_DEPENDENCIES.md` §2); multi-replica worker duplication of backup (R12)
- **PILOT BLOCKER:** NO
- **CONFIDENCE:** 85

### 2.60 Restore
- **STATUS:** IMPLEMENTED (partial)
- **FILES:** `BackupService.RestoreFromBackupAsync` (`:174-201`), pg_restore; **PITR throws NotSupportedException** (`:228-230` — "requires WAL archive orchestration outside pg_restore"); `deploy/runbooks/full-recovery.md`
- **BACKEND:** IMPLEMENTED for full restore; PITR NOT implemented `CONFIRMED_BY_CODE`
- **KNOWN PROBLEMS:** PITR unsupported in code; restore untested at pilot scale
- **MISSING WORK:** restore drill/test, PITR orchestration if required
- **PILOT BLOCKER:** NO for full-restore; test restore before go-live (P0 ops item)
- **CONFIDENCE:** 80

### 2.61 Health/readiness
- **STATUS:** IMPLEMENTED
- **FILES:** `/health`, `/health/live`, `/health/ready`, `/health/detail` (auth), `/health/deep` (auth), `/health/installation`, `/portal-health`; startup readiness gate `RunDeploymentReadinessCheckAsync`; ExtendedHealthChecks
- **BACKEND:** IMPLEMENTED `CONFIRMED_BY_CODE`
- **KNOWN PROBLEMS:** readiness gate can fail startup (R10); health detail/deep require auth (ops friction)
- **PILOT BLOCKER:** NO
- **CONFIDENCE:** 90

### 2.62 Observability
- **STATUS:** PARTIAL
- **FILES:** Serilog→console (JSON prod), prometheus-net HTTP metrics + tenant label, `/metrics` anonymous; **NO OpenTelemetry** (`SYSTEM_MAP.md` §6)
- **BACKEND:** PARTIAL `CONFIRMED_BY_CODE`
- **KNOWN PROBLEMS:** no tracing → cannot measure most performance claims; console-only logging (PostgreSQL sink unused); no custom metrics
- **MISSING WORK:** OpenTelemetry, structured logging to central sink, process/GC metrics, p95 dashboards
- **PILOT BLOCKER:** NO for functionality; YES for operations confidence during pilot (P1)
- **CONFIDENCE:** 95

### 2.63 Deployment automation
- **STATUS:** PARTIAL
- **FILES:** GitHub Actions (`ci.yml`, `deploy.yml`, `release.yml`, `deploy-cloud.yml`), Dockerfile, docker-compose (local/cloud/hybrid/uni/local-demo), k8s manifests (`api-deployment`, HPA, PDB, network-policy, ingress, postgres statefulset), `deploy/scripts/{deploy,rollback,backup,warm-cache}.sh`, Caddy config
- **BACKEND:** N/A — ops layer
- **STATUS:** PARTIAL — CI/CD exists and deploys; but deployment is per-instance/env-var driven, not tenant-factory driven; k8s HPA assumes single-tenant-ish scaling; no deployment record/version tracking; rollback script is image re-pin
- **KNOWN PROBLEMS:** no tenant-provisioning automation; images published to GHCR (`deploy.yml`); versioned immutable images exist via release.yml? verify (`DOCUMENTED_NOT_VERIFIED` for image tags)
- **MISSING WORK:** Deployment Factory (see DEPLOYMENT_FACTORY.md)
- **PILOT BLOCKER:** NO for one pilot (compose + scripts work); YES for repeatable multi-university
- **CONFIDENCE:** 80

### 2.64 Licensing
- **STATUS:** IMPLEMENTED_NEEDS_HARDENING
- **FILES:** `AegisCore.API/Middleware/LicenseLockoutMiddleware.cs`, `AegisCore.Infrastructure/License/LicenseService.cs` (heartbeat `:105-174`, status `:238-242`), `LicenseHeartbeatHostedService.cs`, `LicenseController.cs`, `PlatformControllers.cs` (marketplace license issuance)
- **BACKEND:** IMPLEMENTED `CONFIRMED_BY_CODE`
- **KNOWN PROBLEMS:** **per-request full LicenseStates table read** (top performance finding, HP-01); heartbeat external call (fail-closed after 2 misses); offline = global lockout risk for on-prem pilots; wizard validation 15s external
- **MISSING WORK:** cached/signed lease check; offline tolerance design for on-prem; license authority (MEASURE_FIRST)
- **PILOT BLOCKER:** NO (works); operations risk if external heartbeat can't reach license server (on-prem universities)
- **CONFIDENCE:** 90

---

## 3. Pilot-blocker summary (what actually blocks a 120-day pilot)

| # | Blocker | Severity | Category |
|---|---|---|---|
| B1 | OIDC login flow not implemented (stub beyond redirect URLs) | HIGH if SSO required | Identity |
| B2 | Entra/Google/Okta SSO incomplete; SAML signature verification unverified | HIGH if SSO required | Identity |
| B3 | Login page + portal hard-coded to IoBM branding | MEDIUM (perception) | Branding |
| B4 | License full-table read per request (HP-01) | HIGH (perf ceiling) | Performance |
| B5 | OBE N+1 / in-memory aggregation (HP-03) | HIGH at scale | Performance |
| B6 | Tenant isolation incomplete (~185 entities) — cross-tenant risk | HIGH (security) | Security |
| B7 | No origin-side rate limiting (SEC-02) | HIGH | Security |
| B8 | Test project does not compile → CI test gate red | MEDIUM (process) | Quality |
| B9 | No tenant configuration manifest (env-var-driven single-tenant installs) | HIGH (productization) | Product |
| B10 | No per-tenant document templates (fixed code layouts) | MEDIUM (productization) | Product |
| B11 | No student/faculty self-onboarding flows | MEDIUM (ops) | Product |
| B12 | Observability (no OTel) — cannot measure pilot health | MEDIUM | Ops |
| B13 | Attendance correction workflow absent | LOW-MEDIUM | Product |
| B14 | Admit cards / seating generation not productized | LOW | Product |
| B15 | HR/faculty-management UI absent | MEDIUM | Product |
| B16 | **No unified service desk** — no department-agnostic ticketing core; maintenance-ticket engine is orphaned/unwired; no HR/IT service-desk queue | MEDIUM (feature gap; NOT a pilot blocker) | Product |

> **ADDED 2026-08-12 (service-desk audit):** AegisCore has specialized ticket systems (mental-health/counseling, grievances, AI support, maintenance/facilities — the last orphaned, no controller/DI), but NO general enterprise service desk. Full audit + architecture: `docs/architecture/UNIFIED_SERVICE_DESK.md`. Pilot classification: **P2** (during pilot) — not required before the 120-day pilot, but a high-value fast-follow. The maintenance-ticket domain model (`MaintenanceTicket`/`TicketComment`/`TicketEscalation`/`SLADefinition`) is a reusable seed; the existing WorkflowEngine can run the ticket lifecycle + approvals rather than building a new engine.

Pilot-viability verdict: **A single pilot with local auth, admin-provisioned users, stock branding replaced, and the P0 perf/security items done can proceed.** Multi-university zero-fork deployment requires the tenant-manifest + identity + template work regardless. Unified service desk is P2, not pilot-blocking.

---

## 4. Productionization vs feature vs client vs integration separation

See `PILOT_BACKLOG.md` for the fully-prioritized task list. High-level split:

- **A. PRODUCTIZATION** (software): tenant manifest + config/secrets split, tenant branding bundle, document template engine, deployment factory, per-tenant config resolution, licensing lease cache, tenant-scoping audit.
- **B. CLIENT CONFIG** (tenant data, not software): academic structure, departments/programs, grading policy, fee rules, quotas, campus geofence, email/SMTP, branding assets, document templates, IdP endpoints/claims.
- **C. MISSING PRODUCT FEATURES**: OIDC/SAML completion, student/faculty onboarding, attendance correction, admit cards, seating generation, HR/faculty UI, institutional analytics engine, **unified service desk / ticketing core** (added 2026-08-12 — see `UNIFIED_SERVICE_DESK.md`; specialized tickets exist but no department-agnostic core).
- **D. INTEGRATIONS** (external providers): Entra/Google/Okta SSO, Teams auto-provisioning, Graph mail, Power BI embedding UI, WhatsApp template approval, payment gateway onboarding.
- **E. PERFORMANCE**: license cache, OBE SQL, attendance aggregate, indexes, async documents, login path, connection pool, frontend waterfalls.
- **F. SECURITY**: tenant-scoping audit, origin rate limits, webhook replay verification, upload/parser hardening, SAML signature verification, zero-GUID audit integrity, NEST EOL migration.

---

## 5. Key evidence file index

- `AegisCore.API/Program.cs` — middleware order (161-193), rate-limit deferral (173-176), TENANT_ID seed (332)
- `AegisCore.Infrastructure/License/LicenseService.cs:238-242` — per-request license DB read
- `AegisCore.API/Middleware/LicenseLockoutMiddleware.cs:28` — license check per request
- `AegisCore.Infrastructure/Obe/ObeServices.cs:83-126` — OBE N+1 + in-memory AVG
- `AegisCore.Infrastructure/Repositories/AttendanceRepository.cs:13-34` — load-all attendance
- `AegisCore.Infrastructure/Grading/…` (via `GradingService.cs:93-116`) — grading N+1
- `AegisCore.Infrastructure/SsoHitl/SsoHitlServices.cs:36-44` — SSO redirect-URL builders only
- `AegisCore.Infrastructure/Identity/SamlService.cs` — SAML validation (replay cache `:126-129`)
- `AegisCore.Infrastructure/MicrosoftEcosystem/MicrosoftEcosystemServices.cs` — Teams/Moodle/PowerBI partial
- `AegisCore.Infrastructure/Workflow/EmailNotificationService.cs` — monolithic notification service
- `AegisCore.Infrastructure/Config/TenantConfigService.cs` — non-tenant-scoped config resolution
- `AegisCore.API/Controllers/BrandingController.cs` — single-logo branding
- `AegisCore.Portal/src/app/login/page.tsx:85` — IoBM hard-coded branding
- `AegisCore.Tests/AnalyticsIntelligenceTests.cs` — references deleted analytics namespaces (does not compile)
