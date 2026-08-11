# AegisCore ARCHITECTURE_RISKS.md

Repository Intelligence artifact. Generated 2026-08-11, branch `agent/prod-hardening`.
Ranked risk register. Every item: classification, evidence, remediation direction (NOT implemented in this phase).

## R1 — License enforcement on every request (PERFORMANCE + AVAILABILITY)

- **Component**: `LicenseLockoutMiddleware` + `LicenseService`.
- **Evidence**: `LicenseLockoutMiddleware.cs:28` calls `GetStatusAsync` on every request → `CurrentAsync` → `db.LicenseStates.ToListAsync()` (`LicenseService.cs:238-242`). No cache.
- **Classification**: `LIKELY_BOTTLENECK` at scale; `CONFIRMED_BY_CODE` for the DB round trip.
- **Failure behavior**: two consecutive heartbeat failures → global 403 lockout; offline = fail-closed after 2 misses.
- **Future design (evaluated, NOT implemented)**: central license authority → periodic refresh (background) → signed license lease/token → local verification/cache → normal requests perform cheap local validation (cached boolean). The ERP should NOT require a synchronous external license-server round trip for ordinary requests — the current design already avoids that (heartbeat is background); only the **local DB read** should be cached.
- **GO_CANDIDATE assessment**: the *central license authority* (issuing/revoking/heartbeat endpoint) is a plausible Go candidate later (lightweight, independent scaling, network service). Confidence < 80 → `MEASURE_FIRST`. The in-process middleware stays .NET.
- **Severity**: HIGH (per-request DB cost, DoS amplifier).

## R2 — Tenant isolation depends on app code in places (SECURITY)

- **Evidence**: 145 entities carry `TenantId` without `ITenantScoped` (no EF filter); ~40 core SIS entities have no TenantId (Student, Course, Enrollment, Attendance, Grades, CLOAttainment); `IgnoreQueryFilters()` used at `AuthController.cs:43`, `FinanceWebhookController.cs:53`, `TransportService.cs:12`, `GradeApprovalDiscoveryController.cs:32`.
- **Classification**: `CONFIRMED_BY_CODE` (surface exists); actual breach = `NEEDS_RUNTIME_MEASUREMENT` / pentest.
- **Remediation direction**: audit every `IgnoreQueryFilters()` call site; add explicit tenant predicates; consider adding TenantId to core SIS entities or enforcing RLS via the NOBYPASSRLS role (infra already prepared).
- **Severity**: HIGH.

## R3 — No origin-side rate limiting (ABUSE RESISTANCE)

- **Evidence**: rate-limit middleware exists but unregistered (`GlobalRateLimitMiddleware`, `RateLimitAuthMiddleware`, `RateLimitingMiddleware`); `Program.cs:173-176` delegates to Cloudflare.
- **Classification**: `CONFIRMED_BY_CODE`.
- **Remediation direction**: targeted application-level limits on login, refresh, transcript/OBE/report generation, upload, webhook — NOT aggressive global limits.
- **Severity**: HIGH for brute-force/DoS of expensive endpoints.

## R4 — OBE report N+1 + in-memory aggregation (PERFORMANCE)

- **Evidence**: `ObeServices.cs:85-126` (loops + `ToListAsync` of CourseGrades + C# average).
- **Classification**: `CONFIRMED_BY_CODE`.
- **Severity**: HIGH at 10k students.

## R5 — Frontend role gating is cosmetic (SECURITY UX)

- **Evidence**: no `middleware.ts`; `canAccessRoute` defined but unused; shell only hides menu items; backend is enforcement.
- **Classification**: `CONFIRMED_BY_CODE`. Backend holds, so risk is UX/latency, not breach — BUT any backend endpoint that trusts client presence would be a breach (none confirmed).
- **Severity**: MEDIUM.

## R6 — Raw HttpClient in AI providers (RESOURCE)

- **Evidence**: `OpenAiLlmProvider`, `OllamaLlmProvider`, embedding providers construct `HttpClient` per instance.
- **Severity**: MEDIUM (socket exhaustion under load).

## R7 — EOL Elasticsearch client

- **Evidence**: NEST 7.17.5 (docs/DEPENDENCY_AUDIT.md) CRITICAL/EOL.
- **Severity**: MEDIUM (search optional profile).

## R8 — Synchronous document generation in request path

- **Evidence**: transcript PDF + PQC + QR synchronous (`StudentDocumentsController.cs:237-357`); finance reports synchronous (`FinanceModuleEndpoints.cs:1815-1985`).
- **Classification**: `LIKELY_BOTTLENECK`.
- **Severity**: MEDIUM (long-tail latency; blocks Kestrel threads).

## R9 — Missing indexes on hot lookups

- **Evidence**: `AttendanceRepository.cs:26-33` date-range; OBE/CLO attainment; Result/CourseGrades composites (see DATA_MODEL §6).
- **Severity**: MEDIUM.

## R10 — Startup/operational coupling

- **Evidence**: readiness gate `RunDeploymentReadinessCheckAsync` at startup (fail-on-error option); license heartbeat daily single point; setup branch avoids DB/Redis (good). Quartz hourly backup on same Postgres.
- **Severity**: LOW-MEDIUM.

## R11 — Observability gaps

- **Evidence**: no OpenTelemetry; console-only Serilog (PostgreSQL sink unused); no custom prometheus metrics; health `/detail`/`/deep` require auth.
- **Severity**: MEDIUM (cannot measure p95/GC/allocations today → performance classification limited).

## R12 — Multi-replica worker duplication

- **Evidence**: payment-plan sweep, ledger-signature worker, audit-seal worker run in every replica (3 pods); no distributed lock identified.
- **Severity**: MEDIUM (idempotency mitigates, but duplicate work + row contention).

## R13 — Frontend data-fetch waterfalls

- **Evidence**: `/` session→me→[attendance,finance]; `StudentGeneralPage` over-fetch; no SWR/react-query.
- **Severity**: MEDIUM (perceived latency; N+1 requests from browser).

## R14 — Stale duplicate codebase in-repo

- **Evidence**: `aegiscore-backend/` (6.3MB) and `aegiscore-frontend/` tracked in git; future agents must be told to ignore them (see REPO_INTELLIGENCE).
- **Severity**: LOW (maintenance/dead weight), HIGH (agent-confusion risk).

## Top-5 implementation priority (future queues)

1. **R1** — cache license status (Queue A/G).
2. **R4** — OBE SQL aggregation (Queue A).
3. **R2** — tenant-scoping audit (Queue F).
4. **R3** — targeted origin rate limits (Queue F).
5. **R8** — async/background document generation (Queue A, later).
