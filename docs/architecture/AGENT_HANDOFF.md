# AegisCore AGENT_HANDOFF.md

Repository Intelligence artifact. Generated 2026-08-11, branch `agent/prod-hardening`.
Work is divided into queues. Every task has: PRIORITY, COMPONENT, EXACT FILES, PROBLEM, EVIDENCE, RECOMMENDATION, DEPENDENCIES, ACCEPTANCE CRITERIA. Nothing here is implemented yet.

Cross-cutting rules for implementing agents:
- Authoritative codebase is repo ROOT (`AegisCore.*`). **Ignore `aegiscore-backend/` and `aegiscore-frontend/`.**
- Do not destabilize the working demo. One behavioral change per patch. Preserve the golden demo (`docs/GOLDEN_DEMO_RUNBOOK.md`).
- Backend build gate: `dotnet build AegisCore.API/AegisCore.API.csproj -c Release` (0 errors). Frontend: `cd AegisCore.Portal && rm -rf .next && npm run typecheck` then `npm run lint`.
- Keep PQC/tenant/RBAC invariants intact. Do not weaken authorization to fix performance.

---

## QUEUE A — .NET BACKEND

### A1 (CRITICAL) Cache license lockout state — stop per-request DB read
- COMPONENT: Licensing middleware/service
- FILES: `AegisCore.API/Middleware/LicenseLockoutMiddleware.cs`, `AegisCore.Infrastructure/License/LicenseService.cs`
- PROBLEM: `GetStatusAsync` → `db.LicenseStates.ToListAsync()` on every request (no cache/filter).
- EVIDENCE: `LicenseService.cs:238-242`; `LicenseLockoutMiddleware.cs:28`. CONFIRMED_BY_CODE.
- RECOMMENDATION: Memory/Redis cache of `IsLocked` with short TTL (e.g. 15-60s) invalidated by heartbeat/override; or signed local lease. Preserve fail-closed semantics.
- DEPENDENCIES: none.
- ACCEPTANCE: license middleware makes 0 DB queries per request in steady state (validate via logging/OTel); lockout still applies ≤TTL after failure.

### A2 (CRITICAL) OBE attainment → SQL aggregation
- FILES: `AegisCore.Infrastructure/Obe/ObeServices.cs:83-126`
- PROBLEM: N+1 + in-memory CourseGrades materialization (HP-03).
- EVIDENCE: `ObeServices.cs:85-95` loads all grades then `.Average()`.
- RECOMMENDATION: `SELECT AVG(PercentageObtained), COUNT(*) FILTER (WHERE >= 70)`; batch CLOPOMapping→CLO joins; single report query.
- DEPENDENCIES: A4 indexes.
- ACCEPTANCE: program report issues ≤3 SQL statements; results identical on golden demo.

### A3 (HIGH) Attendance aggregates
- FILES: `AegisCore.Infrastructure/Repositories/AttendanceRepository.cs:13-24`
- PROBLEM: loads all rows to count Present.
- RECOMMENDATION: `CountAsync(a=>...)`/`SumAsync` aggregate; add index.
- DEPENDENCIES: A4.
- ACCEPTANCE: no full-table load; same numbers.

### A4 (HIGH) Missing indexes
- FILES: `AegisDbContext.cs` OnModelCreating (index config ~2099-3428); migration
- RECOMMENDATION: `Attendance(Date)`, `Attendance(EnrollmentId, Status)`, `CourseGrades(CourseId, SemesterId, PercentageObtained)`, `CLOAttainment(CourseLearningOutcomeId, StudentId, CourseId, Semester)`, `Result(CourseId, StudentId)`, `Timetable(day/time/room)`.
- EVIDENCE: DATA_MODEL §6.
- ACCEPTANCE: `EXPLAIN ANALYZE` shows index scans on the traced queries; no regression.

### A5 (HIGH) Grading grace marks batch evaluation
- FILES: `AegisCore.Application/Grading/GradingService.cs:93-116`, `:12-70`
- PROBLEM: per-student re-query (HP-05).
- RECOMMENDATION: batch load scheme/marks/bands; evaluate in memory.
- ACCEPTANCE: constant number of queries per batch.

### A6 (MEDIUM) AI providers use IHttpClientFactory
- FILES: `AegisCore.Infrastructure/AI/*` (OpenAiLlmProvider, OllamaLlmProvider, embedding providers)
- PROBLEM: raw `new HttpClient()` per instance (SEC-05).
- RECOMMENDATION: named typed clients via DI.
- ACCEPTANCE: single shared handler; no socket churn.

### A7 (MEDIUM) Finance payment-plan sweep query reduction
- FILES: `AegisCore.Infrastructure/Finance/FinanceModuleService.cs:558-587`
- PROBLEM: per-installment AnyAsync + per-account queries.
- RECOMMENDATION: batch/journal-bulk operations.
- ACCEPTANCE: constant queries; idempotency preserved.

### A8 (LOW) Remove dead code
- FILES: unused middleware (`GlobalRateLimitMiddleware`, `RateLimitAuthMiddleware`, `RateLimitingMiddleware`, `GlobalExceptionHandlerMiddleware`, `ValidationExceptionMiddleware`), `AspNetCoreRateLimit` pkg, `AutoLockBackgroundService`, unused NChart/paginated components (frontend)
- RECOMMENDATION: delete after confirming unreferenced (except keep rate-limit middleware if reused for A9-style targeted limits).
- ACCEPTANCE: build/typecheck pass; no behavior change.

### A9 (MEDIUM) Origin abuse controls (backend side)
- See Queue F (SEC). Backend implementation would register targeted middleware on auth/expensive endpoints. Keep OUT of Queue A unless F decides.

---

## QUEUE B — FRONTEND

### B1 (HIGH) Eliminate dashboard waterfalls
- FILES: `AegisCore.Portal/src/app/page.tsx:81-103`, `src/app/student/dashboard/page.tsx:21-27`, `src/components/student/StudentGeneralPage.tsx:44-49`
- PROBLEM: session→me→[attendance,finance] serial; over-fetch on 4 routes (ROUTE §4).
- RECOMMENDATION: server-component prefetch or a consolidated `/api/student-portal/dashboard` endpoint; cache session; use `Promise.all` at top.
- ACCEPTANCE: p95 TTFB for dashboard reduced (k6/perf trace).

### B2 (HIGH) Fix unauthenticated raw fetches
- FILES: `src/app/document-vault/page.tsx:41`, `src/lib/personality-context.tsx:41`
- PROBLEM: upload/preference calls without auth header (ROUTE §6).
- RECOMMENDATION: route through `api-client.ts` (Bearer) or confirm cookie-auth path.
- ACCEPTANCE: requests carry auth; runtime 401s eliminated.

### B3 (HIGH) Remove hardcoded zero-GUIDs
- FILES: `src/app/grade-appeals/page.tsx:82`, `src/app/attendance/page.tsx:46`, `src/app/document-vault/page.tsx:41`, `src/app/sync-lag/page.tsx:42,46`
- PROBLEM: bogus actor IDs in payloads (audit integrity).
- RECOMMENDATION: omit fields; let server derive from JWT.
- ACCEPTANCE: no zero-GUID in any request payload; backend audit uses JWT identity.

### B4 (MEDIUM) Add SWR/react-query + route guards
- FILES: `AegisCore.Portal/src/lib/api-client.ts`, new `middleware.ts`
- PROBLEM: no caching/refetch dedup; no server-side guard.
- RECOMMENDATION: adopt SWR; add lightweight middleware redirect for authenticated shell routes (client session check acceptable given backend enforcement).
- ACCEPTANCE: fewer duplicate requests; guarded routes redirect to /login on 401.

### B5 (MEDIUM) Fix lint hygiene
- FILES: 37 errors (react-hooks/set-state-in-effect, unescaped entities, no-explicit-any) across pages
- RECOMMENDATION: address per react rules; wrap setState-in-effect where intentional.
- ACCEPTANCE: `npm run lint` ≤ 0 errors.

### B6 (LOW) Dead components cleanup / wire toggles
- FILES: NChart, PaginatedTable, personality toggle, locale switcher, notification bell/search
- RECOMMENDATION: wire or remove.
- ACCEPTANCE: no unreferenced component in `src/components`.

---

## QUEUE C — DOCUMENTATION

- C1: Sample upload templates per `FILE_UPLOAD_FORMATS.md` §4 (CSV templates already in `/formats`).
- C2: Operator manual for document-vault tiers + PQC verify workflow.
- C3: Integration guides for payment gateways + webhook HMAC signing.
- C4: Deployment docs already exist (`docs/deployment/*`) — keep in sync with new config keys.
- ACCEPTANCE: docs reference only IMPLEMENTED features; no invented behavior.

---

## QUEUE D — GO

- No component meets GO_CANDIDATE evidence today.
- `MEASURE_FIRST`: central license authority (see AEGISCORE_TECHNICAL_PROFILE GO-1) and telemetry ingestion (GO-2) — only after measurement.
- DO NOT extract any ERP domain to Go.

---

## QUEUE E — RUST

- No component meets RUST_CANDIDATE evidence today.
- If upload/parser hardening is pursued for hostile PDF/XLSX/OCR parsing, evaluate a Rust high-assurance parser boundary — requires load/security evidence first.
- DO NOT rewrite any existing .NET parser without evidence.

---

## QUEUE F — SECURITY (dedicated review/pentest)

- F1 (CRITICAL): Tenant-scoping audit — every `IgnoreQueryFilters()` call site + 145 non-filtered TenantId entities + 40 no-TenantId core SIS entities. Validate IDOR/BOLA. Files: search `IgnoreQueryFilters`, `DATA_MODEL.md` §2.
- F2 (CRITICAL): Origin rate limiting for login/refresh/expensive endpoints (auth brute force + DoS). Target limits, not global.
- F3 (HIGH): Webhook replay window + HMAC verification test (`FinanceWebhookController.cs`).
- F4 (HIGH): Upload/parser abuse (PDF/XLSX/OCR/DocumentIntelligence) — see STRIX scope.
- F5 (HIGH): `X-Tenant-Id`/ABAC semantics + Casbin allow-list correctness (incl. `/api/session`, verify endpoints).
- F6 (MEDIUM): Refresh-token abuse, logout semantics, localStorage exposure (CSP review).
- F7 (MEDIUM): NEST EOL client migration (Elastic.Clients.Elasticsearch).
- F8 (MEDIUM): Auditor opaque-token path authorization review.
- F9 (MEDIUM): Data-import external connections SSRF review.
- ACCEPTANCE: each produces a finding report with severity, evidence, exploitability; fixes in separate patches.

---

## QUEUE G — PERFORMANCE (profiling/load-test)

- G1: Build runtime profiling (OpenTelemetry + dotnet-counters + pg_stat_statements + k6) per PERFORMANCE_MAP §4.
- G2: Execute staged AWS load test 100→10,000 users per `docs/performance/AWS_LOAD_TEST_PLAN.md`.
- G3: Measure p50/p95/p99, CPU, memory, GC, allocations, DB/Redis latency, pool usage; fill all `MEASUREMENT_REQUIRED`/`NEEDS_RUNTIME_MEASUREMENT` gaps.
- G4: Validate license-read elimination (A1) impact at scale.
- ACCEPTANCE: baseline numbers recorded; regression gate (no p95 regression after A-queue changes).
