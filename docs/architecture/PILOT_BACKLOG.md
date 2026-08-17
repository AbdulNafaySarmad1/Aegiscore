# AegisCore PILOT_BACKLOG.md

Prioritized 120-day pilot backlog. Generated 2026-08-12 on branch `agent/prod-hardening`.
Analysis ONLY. No implementation. No monetary estimates.

Priorities:
- **P0** = before pilot deployment (blockers / correctness / security)
- **P1** = first 2 weeks of pilot
- **P2** = during pilot
- **P3** = optional / polish

Owners: BACKEND / FRONTEND / DEVOPS / SECURITY / DATA.
Complexity: XS / S / M / L / XL.
`PILOT BLOCKER` = YES means the 120-day pilot cannot start cleanly without it.

Evidence references: `AEGISCORE_TECHNICAL_PROFILE.md` (prof), `ARCHITECTURE_RISKS.md` (R#), `SECURITY_BOUNDARIES.md` (SEC-#), `PERFORMANCE_MAP.md` (HP-#), `AGENT_HANDOFF.md` (A#/B#/F#/G#), `BASELINE_RESULTS.md` (BR#).

---

## P0 — Before pilot deployment

| ID | PRIORITY | FEATURE | CURRENT STATE | EXACT FILES | REQUIRED WORK | OWNER | COMPLEXITY | DEPENDENCIES | ACCEPTANCE CRITERIA | PILOT BLOCKER |
|---|---|---|---|---|---|---|---|---|---|---|
| PB-01 | P0 | Tenant scoping audit + hardening | ~145 entities TenantId-without-filter; ~40 core SIS no TenantId; `IgnoreQueryFilters()` at AuthController:43, FinanceWebhookController:53, TransportService:12, GradeApprovalDiscoveryController:32 | `DATA_MODEL.md` §2; search `IgnoreQueryFilters`; `AegisDbContext.cs:1949-1962` | Audit every call site + non-filtered entity; add tenant predicates or TenantId to core SIS entities (or enforce RLS via NOBYPASSRLS role); fix confirmed escapes | SECURITY | L | none | No cross-tenant read/write reproducible in a disposable staging test; every `IgnoreQueryFilters` justified | YES (security) |
| PB-02 | P0 | Origin rate limiting | middleware exists, NOT wired; edge-delegated | `Program.cs:173-176`; `AspNetCoreRateLimit` (unused); `Middleware/GlobalRateLimitMiddleware.cs` etc. | Register targeted origin limits on login, refresh, transcript/OBE/report generation, uploads, webhooks, verify endpoints | SECURITY | M | none | Auth/expensive endpoints enforce limits at origin; brute-force test blocked; no false positives on normal pilot traffic | YES (security) |
| PB-03 | P0 | License per-request DB read | every non-demo request → `db.LicenseStates.ToListAsync()` | `Infrastructure/License/LicenseService.cs:238-242`; `API/Middleware/LicenseLockoutMiddleware.cs:28` | Cache IsLocked (memory/Redis, 15-60s TTL, invalidated by heartbeat/override) or signed local lease; keep fail-closed | BACKEND | S | none | 0 license DB queries per request in steady state; lockout still applies ≤TTL; golden demo unaffected | YES (perf ceiling) |
| PB-04 | P0 | OBE SQL aggregation | N+1 + in-memory AVG | `Infrastructure/Obe/ObeServices.cs:83-126` | `SELECT AVG/COUNT` server-side; batch CLOPOMapping→CLO; single report query | BACKEND | M | PB-06 (indexes) | Program report ≤3 SQL statements; identical results on golden demo | YES (accreditation-scale) |
| PB-05 | P0 | Test project compiles + CI green | `AegisCore.Tests` references deleted Analytics namespaces (10 errors) | `AegisCore.Tests/AnalyticsIntelligenceTests.cs`, `StatisticalAnalyticsTests.cs`, `GoldenDemoSeederTests.cs`; deleted `Application/Analytics`, `Infrastructure/Analytics` | Either restore the analytics feature (recommended: rebuild in .NET per POLYGLOT matrix) OR remove/adjust the broken tests; fix CI `dotnet test` | BACKEND | M | decision on analytics | `dotnet test AegisCore.Tests` passes in CI; tests reference existing namespaces only | YES (process gate) |
| PB-06 | P0 | Missing indexes | hot lookups unscanned | `AegisDbContext.cs` OnModelCreating (~2099-3428); new migration | Attendance(Date), Attendance(EnrollmentId,Status), CourseGrades(CourseId,SemesterId,PercentageObtained), CLOAttainment(CLO,Student,Course,Semester), Result(CourseId,StudentId), Timetable(day/time/room) | DATA | S | none | EXPLAIN ANALYZE shows index scans on traced queries; no regression | YES (with PB-04) |
| PB-07 | P0 | SAML signature verification + ACS→JWT | SignedXml loaded, CheckSignature NOT called; ACS upserts user without JWT | `Identity/SamlService.cs:147-160`; `SamlController.cs:46-69` | Implement `CheckSignature` vs `IdpCertificate`; Redis replay store; ACS→session→JWT issuance + account linking | BACKEND | M | none | Forged/unsigned SAML rejected; valid IdP login yields AegisCore JWT | YES if SAML required |
| PB-08 | P0 | OIDC login flow (provider-neutral) | STUB (authorize URLs only) | `SsoHitl/SsoHitlServices.cs:36-44`; new OIDC adapter | OIDC handler: callback, code exchange, ID-token validation, PKCE/state/nonce, claims→role/dept/program/studentID/employeeID mapping, account linking, JIT provisioning, JWT issuance | BACKEND | L | none | Entra OIDC login → authenticated AegisCore user with correct roles/dept; local auth still works | YES if institutional SSO required |
| PB-09 | P0 | Local-auth hardening | Argon2 + GeoIP on login; no origin limit; localStorage tokens | `AuthController.cs`; `LoginAuditService.cs:44-47`; portal `src/lib/api-client.ts` | rate limit (PB-02); make GeoIP non-blocking/async; keep refresh-family rotation; verify refresh reuse semantics | BACKEND + SECURITY | S | PB-02 | Login brute-force limited at origin; refresh reuse → 401; tokens still valid flow | NO |
| PB-10 | P0 | Remove hard-coded IoBM branding | login page + testimonials IoBM-specific | `AegisCore.Portal/src/app/login/page.tsx:63-87` | Drive institution name/logo/colors from tenant branding endpoint (`/api/branding/*` + config); remove IoBM text/testimonials behind config | FRONTEND | S | branding endpoint exists (`BrandingController`) | Non-IoBM university sees its own name/logo; no IoBM strings in visible UI | YES (perception) |
| PB-11 | P0 | Zero-GUID actor IDs removed | frontend sends bogus actor IDs | `src/app/grade-appeals/page.tsx:82`, `attendance/page.tsx:46`, `document-vault/page.tsx:41`, `sync-lag/page.tsx:42,46` | Omit actor fields; server derives from JWT; backend stops trusting client actor IDs | FRONTEND + BACKEND | S | none | No zero-GUID in any request payload; audit uses JWT identity | YES (audit integrity) |
| PB-12 | P0 | Restore drill + backup/restore verification | full restore implemented; PITR unsupported | `Infrastructure/Backup/BackupService.cs:174-230`; `deploy/scripts/backup.sh`; `deploy/runbooks/full-recovery.md` | Test full restore on a disposable clone; document RTO/RPO; confirm WAL + object-lock flow | DEVOPS | M | none | Full restore verified on disposable staging in < target RTO; runbook updated | YES (ops confidence) |
| PB-13 | P0 | Webhook replay window + HMAC verification test | anonymous + HMAC + optional PQC; Redis idempotency | `FinanceWebhookController.cs:53`; `PaymentGatewayAdapters.cs` | Verify replay window enforced; HMAC/PQC signature verified; gateway slug tenant-scoped | SECURITY | M | none | Replayed webhook rejected; signature verified; tenant-scoped routing | NO |
| PB-14 | P0 | Finance executive dashboard 500 | LINQ join cannot be translated | `AegisCore.API/Finance/FinanceModuleEndpoints.cs` (executive dashboard) | Fix EF translation (BR §6) | BACKEND | M | none | `/api/finance/v2/dashboard/executive` returns 200 with data on demo | NO (correctness) |

---

## P1 — First 2 weeks of pilot

| ID | PRIORITY | FEATURE | CURRENT STATE | EXACT FILES | REQUIRED WORK | OWNER | COMPLEXITY | DEPENDENCIES | ACCEPTANCE CRITERIA | PILOT BLOCKER |
|---|---|---|---|---|---|---|---|---|---|---|
| PB-20 | P1 | Tenant manifest schema + validation | no manifest; env/config only | new (docs `TENANT_CONFIGURATION.md`); `Config/TenantConfigService.cs` | Define manifest schema, add per-tenant resolution, migrate key knobs (identity/email/branding/locale/attendance geofence) with env fallback | BACKEND | L | PB-08, PB-10 | A tenant profile (manifest + assets + secrets refs) deploys a branded, configured instance of the same images | NO (productization) |
| PB-21 | P1 | Tenant branding bundle | single logo endpoint | `BrandingController.cs`; portal AppShell/layout | Per-tenant name/logo/colors/footer resolved from manifest; portal consumes it consistently | FRONTEND + BACKEND | M | PB-20 | Branding switches per tenant config without code change | NO |
| PB-22 | P1 | Observability: OpenTelemetry | none | `API/Program.cs`; new OTel config | Instrument HTTP + DB + Redis + external calls; structured logs to central sink; process/GC metrics; p95 dashboards | BACKEND/DEVOPS | M | none | p50/p95/p99 and GC/alloc observable per tenant; dashboards live | NO (ops health) |
| PB-23 | P1 | Attendance aggregates | load-all then count | `Infrastructure/Repositories/AttendanceRepository.cs:13-24` | COUNT/SUM aggregate + index (PB-06) | BACKEND | S | PB-06 | No full-table load; same numbers | NO |
| PB-24 | P1 | Grading batch evaluation | per-student requery | `Application/Grading/GradingService.cs:93-116` | batch load scheme/marks/bands; evaluate in memory | BACKEND | M | PB-06 | constant queries per batch; identical grades | NO |
| PB-25 | P1 | Email provider abstraction + background delivery | sync SMTP in request path | `Workflow/EmailNotificationService.cs:39-77` | Extract IEmailProvider; add delivery queue/background worker; keep SMTP first | BACKEND | M | none | Email sent off-request; provider swappable (SMTP→Graph) | NO |
| PB-26 | P1 | Student/faculty onboarding workflows | NOT_FOUND as product flow | new (StudentLifecycle, Identity) | Admission→student account creation (JIT), credential delivery, ID-card data, docs upload; faculty provisioning flow | BACKEND + FRONTEND | L | PB-08 (linking) | Admin can onboard admitted student to a usable account with credentials; faculty likewise | NO |
| PB-27 | P1 | Frontend data-fetch consolidation | waterfalls; no SWR | `AegisCore.Portal/src/app/page.tsx:81-103`, `StudentGeneralPage.tsx:44-49` | parallelize/consolidate dashboard fetches; adopt SWR/react-query; server-component prefetch where possible | FRONTEND | M | none | dashboard p95 TTFB reduced; no duplicate requests | NO |
| PB-28 | P1 | Attendance correction workflow | NOT_FOUND | new on WorkflowEngine | correction request + HOD approval using existing engine + notifications | BACKEND + FRONTEND | M | PB-02 | Student can request correction; HOD approves/rejects; audit trail | NO |
| PB-29 | P1 | Upload/parser hardening | vault size-only; PDF/XLSX/OCR parser surface | `DocumentVaultController.cs:49`; `UploadValidationService.cs`; `AI/DocumentOcrService.cs` | content validation on vault; parser hardening (decompression-bomb guard, PDF object limits); optional sandbox | SECURITY | M | none | Malicious file set rejected without crash; no parser DoS in staging test | NO (SEC-11) |
| PB-30 | P1 | Power BI embed frontend | backend token service only | `MicrosoftEcosystemServices.cs:113-200`; new portal page | embed page with powerbi-client; role-aware; RLS decision | FRONTEND + BACKEND | M | client AAD creds | Dashboard embeds for authorized roles only | NO |
| PB-31 | P1 | Teams auto-provisioning batch | single-class provision; static token | `MicrosoftEcosystemServices.cs:11-63` | Graph token service (client-credentials), batch provision all classes, membership sync, status UI | BACKEND + FRONTEND | L | client consent | All classes for a semester provision in one run; membership matches roster | NO |
| PB-32 | P1 | Notification provider refactor | monolithic channel switch | `Workflow/EmailNotificationService.cs:183-215` | `INotificationProvider` interface + router; wire email/WhatsApp/SMS; template per tenant | BACKEND | M | PB-25 | New provider = add adapter; domain emits intent | NO |
| PB-33 | P1 | Exam seating + admit cards | entities exist; generators NOT_FOUND | `ExamSeating.cs`; `ExamSeatingRepository.cs`; new generation | seating plan generation API + UI; admit-card PDF on document pipeline | BACKEND + FRONTEND | L | document pipeline | Seating generated per exam; student downloads QR admit card | NO |
| PB-34 | P1 | AI providers → IHttpClientFactory | raw new HttpClient in ctors | `Infrastructure/AI/*` (OpenAi/Ollama/embeddings) | named/typed clients via DI | BACKEND | S | none | single shared handler; no socket churn | NO |
| PB-35 | P1 | NEST EOL migration | NEST 7.17.5 | search profile; `docs/DEPENDENCY_AUDIT.md` | migrate to Elastic.Clients.Elasticsearch | BACKEND | M | none | search profile works on new client; dependency audit clean | NO (SEC-18) |
| PB-36 | P1 | Frontend route guards + auth hygiene | client-only gating; no middleware.ts | `AegisCore.Portal/src/middleware.ts` (new), `src/lib/api-client.ts` | lightweight middleware redirect for authenticated shell; fix raw fetches without auth header (document-vault:41, personality-context:41) | FRONTEND | S | none | guarded routes redirect on 401; uploads carry auth | NO (B2/B4) |
| PB-37 | P1 | Document template system (transcript first) | fixed in-code layout | `StudentDocumentsController.cs:237-357`; new template store/renderer | HTML→PDF template for transcript; branding overlay; QR/PQC/metadata binding preserved; verify URL | BACKEND | L | PB-20 | University-supplied transcript template renders branded, signed, verifiable PDF from same release | NO (productization) |

---

## P2 — During pilot

| ID | PRIORITY | FEATURE | CURRENT STATE | EXACT FILES | REQUIRED WORK | OWNER | COMPLEXITY | DEPENDENCIES | ACCEPTANCE CRITERIA | PILOT BLOCKER |
|---|---|---|---|---|---|---|---|---|---|---|
| PB-40 | P2 | Deployment record + rollback integration | no deployment state | new (factory); `deploy/scripts/rollback.sh` | record tenant/release/digest/config-hash; pre-deploy snapshot; integrated rollback | DEVOPS | M | PB-20 | every deploy recorded; rollback restores previous release + DB dump | NO |
| PB-41 | P2 | Tenant registry | NOT_FOUND | new | registry of tenants, versions, statuses | DEVOPS | S | PB-40 | factory + registry give per-tenant deploy state | NO |
| PB-42 | P2 | HR/faculty management UI | backend APIs only | `HrController.cs`; portal new pages | employee records, leaves, payroll UI; faculty management pages | FRONTEND | L | none | HR flows usable in portal | NO |
| PB-43 | P2 | Document templates for challan/grade sheet/OBE | fixed layouts | finance + OBE generators | extend template pipeline; validation/lint gate on upload; versioning | BACKEND | L | PB-37 | challan/grade-sheet/OBE render from templates | NO |
| PB-44 | P2 | Institutional analytics engine (rebuild in .NET) | deleted; tests broken | new `Application/Analytics`, `Infrastructure/Analytics` | rebuild statistical analytics (cohort, retention, OBE stats, forecasting) in .NET; fix tests | BACKEND | XL | PB-05 decision | analytics endpoints return stats; tests green | NO (unless analytics is KPI) |
| PB-45 | P2 | Seeded OBE performance measurement | demo has 0 CLO data | `docs/performance/AWS_LOAD_TEST_PLAN.md` | seed realistic OBE dataset; measure post-A2 ceiling | DEVOPS/DATA | M | PB-04 | OBE ceiling measured on seeded data; targets in BR §9 met | NO |
| PB-46 | P2 | Frontend lint hygiene | 37 errors | `AegisCore.Portal/src` | address react-hooks/set-state-in-effect, unescaped entities, no-explicit-any | FRONTEND | S | none | `npm run lint` 0 errors | NO |
| PB-47 | P2 | Dead code removal | unused middleware/package/components | `GlobalRateLimitMiddleware`, `RateLimitAuthMiddleware`, `AspNetCoreRateLimit`, `AutoLockBackgroundService`, NChart, PaginatedTable | delete after confirming unreferenced (keep rate-limit if reused by PB-02) | BACKEND/FRONTEND | S | PB-02 | build/typecheck pass; no behavior change | NO |
| PB-48 | P2 | Admission portal UI | backend only | new portal pages (admissions/merit) | admissions enquiry/applications/merit-list/seat-allocation UI | FRONTEND | L | none | admission cycle usable in portal | NO |
| PB-49 | P2 | Multi-replica worker dedup | workers run in every replica | payment-plan sweep, ledger-signature, audit-seal workers | distributed lock / leader election; idempotency audit | BACKEND | M | none | no duplicate sweep work across replicas | NO |
| PB-50 | P2 | Data-import SSRF guard | ADO.NET external connections | `DataImportService.cs:107`; `DataImportController.cs:21-34` | connection allow-list/validation; SSRF review | SECURITY | M | none | external connections restricted to allowed hosts | NO |
| PB-51 | P2 | Finance report async generation | synchronous + double serialize | `FinanceModuleEndpoints.cs:1815-1985` | single-pass serialization; async/queue generation off request path | BACKEND | M | none | reports generated off-path; latency on request removed | NO |
| PB-52 | P2 | Unified service desk — core ticket engine | NOT_FOUND; specialized tickets only (mental-health, grievances, AI support, orphaned maintenance) | new `ServiceDesk` module; `Domain/Entities/ServiceTicket.cs` etc.; `AegisDbContext.cs` | entities (all `ITenantScoped`), lifecycle service, comments, attachments (via `IStorageService`), status/assignment history, SLA targets, notifications via `INotificationService`, audit via `IAuditLogService` | BACKEND | XL | PB-01 (tenant audit), workflow engine | employee opens HR/IT ticket → queue → assign → comment → escalate → resolve → close with full audit | NO |
| PB-53 | P2 | Service-desk configuration (categories/teams/SLA per tenant) | none | new config service; `ConfigCenter`, feature flags | configurable departments/categories/teams/SLA policies per tenant manifest | BACKEND | M | PB-52, PB-20 | admin configures queues/categories/SLA without code | NO |
| PB-54 | P2 | Service-desk portal UI | no ticket UI (reference: `/mental-health`, `/grade-approvals`) | new `/service-desk*` pages | requester create/my/view + staff queue/assigned/reports + admin config | FRONTEND | XL | PB-52, PB-53 | faculty creates ticket; HR staff queues/assigns/resolves; requester sees status | NO |
| PB-55 | P2 | Workflow-engine integration for ticket lifecycle + approvals | WorkflowEngine exists (configurable) | `Workflow/WorkflowEngine.cs`, `ApprovalService.cs`; new adapter | run lifecycle + approval steps via engine; approval-bound tickets (salary/fee/privileged-access/record) | BACKEND | L | PB-52, existing workflow | ticket lifecycle runs on engine; approval tickets require approval | NO |
| PB-56 | P3 | Escalation + SLA breach engine (generalize orphaned `SLAEngineService`) | orphaned facilities-scoped | `Application/Maintenance/SLAEngineService.cs`, `TicketEscalation.cs` | periodic breach sweep, escalation records + notifications, queue metrics | BACKEND | M | PB-52, PB-53 | overdue → escalation created + notified; SLA metrics correct | NO |
| PB-57 | P3 | Consolidate orphaned maintenance ticketing onto core | unwired orphan | `Application/Maintenance/*`, repos | retire orphan services; map facilities categories onto core | BACKEND | XL | PB-52..PB-56 | facilities ticketing runs on core; orphan code removed | NO |

> **ADDED 2026-08-12 (service-desk audit):** PB-52..PB-57 cover the unified service desk identified as missing in `docs/architecture/UNIFIED_SERVICE_DESK.md`. Specialized ticket systems (mental-health, grievances, AI support) remain protected and are NOT consolidated; only maintenance/facilities is a consolidation candidate. Pilot priority = **P2** (during pilot) — not a 120-day blocker. The maintenance-ticket domain model and existing WorkflowEngine are reusable seeds.

---

## P3 — Optional / polish

| ID | PRIORITY | FEATURE | CURRENT STATE | EXACT FILES | REQUIRED WORK | OWNER | COMPLEXITY | DEPENDENCIES | ACCEPTANCE CRITERIA | PILOT BLOCKER |
|---|---|---|---|---|---|---|---|---|---|---|
| PB-60 | P3 | SAML second IdP + Google/Okta adapters | STUB | OIDC adapter (PB-08) | config-only adapters after generic OIDC | BACKEND | M | PB-08 | Google/Okta login from tenant config | NO |
| PB-61 | P3 | SCIM tenant-scoping + hardening | Guid.Empty tenant | `ScimController.cs:95` | tenant-scoped SCIM; password-field compliance | BACKEND | M | none | SCIM operates within tenant | NO |
| PB-62 | P3 | Identity-conflict admin UI | NOT_FOUND | new | queue + resolution UI | FRONTEND | M | PB-08 | conflicts resolved manually, audited | NO |
| PB-63 | P3 | Cafeteria demand-forecast validation | stub-like analytic | `CafeteriaService.cs` | validate/replace with real model | BACKEND | S | none | forecast matches historical demand within tolerance | NO |
| PB-64 | P3 | PITR restore orchestration | NotSupportedException | `BackupService.cs:228-230` | WAL-archive orchestration for PITR | DEVOPS | XL | none | point-in-time restore available | NO |
| PB-65 | P3 | Portal notification bell/search | non-functional | AppShell | wire NotificationHub + search | FRONTEND | M | PB-32 | in-app notifications delivered in realtime | NO |
| PB-66 | P3 | License authority (Go?) | MEASURE_FIRST | prof §15 GO-1 | evaluate only after license traffic data | DEVOPS/ARCH | M | data | decision documented with evidence | NO |
| PB-67 | P3 | Rust parser boundary | MEASURE_FIRST | prof §16 | only if pentest shows unmitigable parser exploit | SECURITY | XL | pentest | decision documented | NO |
| PB-68 | P3 | Template designer/approval UI | NOT_FOUND | PB-43 | visual template editor/approval workflow | FRONTEND | XL | PB-43 | staff edits templates without code | NO |

---

## Critical-path summary

```
Pilot start is blocked on (must be done first):
  SECURITY:  PB-01 (tenant audit), PB-02 (rate limits), PB-07 (SAML sig) / PB-08 (OIDC) if SSO
  PERFORMANCE: PB-03 (license), PB-04 (OBE) + PB-06 (indexes), PB-05 (CI green)
  CORRECTNESS: PB-14 (finance 500), PB-11 (zero-GUID)
  OPS:         PB-12 (restore drill)
  BRANDING:    PB-10 (IoBM removal)

First-2-weeks: PB-20 (manifest), PB-21 (branding), PB-22 (OTel), PB-37 (document templates)
  — these are the PRODUCTIZATION core that makes later universities cheap.

During pilot: PB-40..PB-51 (factory, HR UI, templates, analytics, admissions UI, worker dedup)
  + PB-52..PB-55 (service desk core, config, portal UI, workflow integration) — P2, high-value fast-follow.

Deferred: P3 (Google/Okta adapters, SCIM, PITR, Rust/Go decisions, PB-56/PB-57 service-desk consolidation) — evidence-gated.
```
